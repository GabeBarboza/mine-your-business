using System.Security.Cryptography;
using MineYourBusiness.Domain;

namespace MineYourBusiness.Application;

/// <summary>
/// Owns the complete match state. Transport connections are deliberately opaque so
/// this class can be exercised without Godot or a network socket.
/// </summary>
public sealed class AuthoritativeRoom
{
    private readonly Dictionary<string, Seat> _seatsByConnection = [];
    private readonly Dictionary<PlayerId, Seat> _seatsByPlayer = [];
    private readonly Dictionary<(PlayerId PlayerId, string CommandId), CommandReceipt> _receipts = [];
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _reconnectWindow;
    private GameState? _state;

    public AuthoritativeRoom(
        string hostConnectionId,
        string hostName,
        long seed,
        string? roomCode = null,
        TimeSpan? reconnectWindow = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostConnectionId);
        Seed = seed;
        RoomCode = NormalizeRoomCode(roomCode ?? CreateRoomCode());
        _reconnectWindow = reconnectWindow ?? TimeSpan.FromSeconds(45);
        _timeProvider = timeProvider ?? TimeProvider.System;
        Seat host = CreateSeat(hostConnectionId, hostName, isHost: true);
        host.IsReady = true;
    }

    public string RoomCode { get; }
    public string MatchId { get; } = Guid.NewGuid().ToString("N");
    public long Seed { get; }
    public long Revision { get; private set; }
    public RoomPhase Phase { get; private set; } = RoomPhase.Lobby;
    public DateTimeOffset? ReconnectDeadline { get; private set; }
    public string? StatusMessage { get; private set; }
    public int PlayerCount => _seatsByPlayer.Count;

    public JoinRoomResult Join(string connectionId, string roomCode, string name, string? reconnectToken = null)
    {
        Tick();
        if (!string.Equals(RoomCode, NormalizeRoomCode(roomCode), StringComparison.Ordinal))
        {
            return RejectedJoin("Código de sala inválido.");
        }

        if (_seatsByConnection.ContainsKey(connectionId))
        {
            return RejectedJoin("Esta conexão já ocupa um assento.");
        }

        if (!string.IsNullOrWhiteSpace(reconnectToken))
        {
            Seat? returning = _seatsByPlayer.Values.SingleOrDefault(
                seat => FixedEquals(seat.ReconnectToken, reconnectToken));
            if (returning is null || returning.IsConnected || Phase is RoomPhase.Finished or RoomPhase.Aborted)
            {
                return RejectedJoin("Token de reconexão inválido ou expirado.");
            }

            returning.ConnectionId = connectionId;
            returning.IsConnected = true;
            _seatsByConnection[connectionId] = returning;
            if (_seatsByPlayer.Values.All(seat => seat.IsConnected))
            {
                Phase = _state?.IsFinished == true ? RoomPhase.Finished : RoomPhase.Playing;
                ReconnectDeadline = null;
                StatusMessage = null;
            }

            Revision++;
            return AcceptedJoin(returning);
        }

        if (Phase != RoomPhase.Lobby)
        {
            return RejectedJoin("A partida já começou; use o token de reconexão do seu assento.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 24)
        {
            return RejectedJoin("O nome deve ter entre 1 e 24 caracteres.");
        }

        if (_seatsByPlayer.Count >= 10)
        {
            return RejectedJoin("A sala está cheia.");
        }

        Seat seat = CreateSeat(connectionId, name, isHost: false);
        Revision++;
        return AcceptedJoin(seat);
    }

    public bool SetReady(string connectionId, bool ready, out string? error)
    {
        if (!TryConnectedSeat(connectionId, out Seat seat, out error) || Phase != RoomPhase.Lobby)
        {
            error ??= "A prontidão só pode ser alterada no lobby.";
            return false;
        }

        seat.IsReady = seat.IsHost || ready;
        Revision++;
        return true;
    }

    public bool Start(string connectionId, out string? error)
    {
        if (!TryConnectedSeat(connectionId, out Seat seat, out error) || !seat.IsHost)
        {
            error ??= "Apenas o anfitrião pode iniciar a partida.";
            return false;
        }

        if (Phase != RoomPhase.Lobby)
        {
            error = "A sala não está no lobby.";
            return false;
        }

        if (_seatsByPlayer.Count is < 3 or > 10)
        {
            error = "São necessárias de 3 a 10 pessoas para iniciar.";
            return false;
        }

        if (_seatsByPlayer.Values.Any(player => !player.IsReady || !player.IsConnected))
        {
            error = "Todas as pessoas precisam estar conectadas e prontas.";
            return false;
        }

        _state = GameFactory.Create(
            _seatsByPlayer.Values.Select(player => (player.PlayerId, player.Name)),
            Seed);
        Phase = RoomPhase.Playing;
        Revision++;
        error = null;
        return true;
    }

    public CommandReceipt Submit(string connectionId, CommandEnvelope envelope)
    {
        Tick();
        if (!TryConnectedSeat(connectionId, out Seat seat, out string? error))
        {
            return CommandReceipt.Rejected(error!, Revision);
        }

        if (!string.Equals(envelope.MatchId, MatchId, StringComparison.Ordinal) ||
            envelope.PlayerId != seat.PlayerId || envelope.Command.PlayerId != seat.PlayerId)
        {
            return CommandReceipt.Rejected("O comando não pertence a esta partida e a este assento.", Revision);
        }

        if (string.IsNullOrWhiteSpace(envelope.CommandId) || envelope.CommandId.Length > 64)
        {
            return CommandReceipt.Rejected("O identificador do comando é inválido.", Revision);
        }

        var receiptKey = (seat.PlayerId, envelope.CommandId);
        if (_receipts.TryGetValue(receiptKey, out CommandReceipt? previous))
        {
            return previous with { IsDuplicate = true };
        }

        if (Phase != RoomPhase.Playing || _state is null)
        {
            return CommandReceipt.Rejected(
                Phase == RoomPhase.Paused ? "A partida está pausada aguardando reconexão." : "A partida não está em andamento.",
                Revision);
        }

        if (envelope.ExpectedRevision != Revision)
        {
            return CommandReceipt.Rejected("A revisão esperada está desatualizada; solicite um novo snapshot.", Revision);
        }

        if (envelope.TurnNumber != _state.TurnNumber)
        {
            return CommandReceipt.Rejected("O número do turno está desatualizado.", Revision);
        }

        CommandResult result = RulesEngine.Handle(_state, envelope.Command);
        if (!result.Accepted)
        {
            return CommandReceipt.Rejected(result.Error!, Revision);
        }

        Revision++;
        if (_state.IsFinished)
        {
            Phase = RoomPhase.Finished;
        }

        CommandReceipt receipt = new(true, false, null, Revision, result.Events);
        _receipts[receiptKey] = receipt;
        return receipt;
    }

    public void Disconnect(string connectionId)
    {
        if (!_seatsByConnection.Remove(connectionId, out Seat? seat))
        {
            return;
        }

        seat.IsConnected = false;
        seat.ConnectionId = null;
        Revision++;
        if (Phase == RoomPhase.Lobby)
        {
            if (seat.IsHost)
            {
                Abort("O anfitrião encerrou a sala.");
            }
            else
            {
                _seatsByPlayer.Remove(seat.PlayerId);
            }

            return;
        }

        if (Phase == RoomPhase.Playing)
        {
            Phase = RoomPhase.Paused;
            ReconnectDeadline = _timeProvider.GetUtcNow() + _reconnectWindow;
            StatusMessage = $"Aguardando a reconexão de {seat.Name}.";
        }
    }

    public void Tick()
    {
        if (Phase == RoomPhase.Paused && ReconnectDeadline is DateTimeOffset deadline &&
            _timeProvider.GetUtcNow() >= deadline)
        {
            Abort("A janela de reconexão expirou.");
        }
    }

    public PlayerSnapshot SnapshotForConnection(string connectionId)
    {
        Tick();
        if (!_seatsByConnection.TryGetValue(connectionId, out Seat? seat))
        {
            throw new InvalidOperationException("A conexão não pertence à sala.");
        }

        return Project(seat);
    }

    private PlayerSnapshot Project(Seat recipient)
    {
        bool revealRoles = _state?.Phase is MatchPhase.RoundSummary or MatchPhase.MatchSummary;
        IReadOnlyList<RoomPlayerView> players = _seatsByPlayer.Values.Select(seat =>
        {
            PlayerState? state = _state?.Players.Single(player => player.Id == seat.PlayerId);
            return new RoomPlayerView(
                seat.PlayerId,
                seat.Name,
                seat.IsHost,
                seat.IsReady,
                seat.IsConnected,
                state?.Hand.Count ?? 0,
                state?.BrokenTools ?? ToolType.None,
                state?.IsEliminated ?? false,
                revealRoles ? state?.Role : null,
                _state?.IsFinished == true ? state?.Gold : null);
        }).ToArray();

        IReadOnlyList<BoardCardView> board = _state?.Board.Cards.Select(pair =>
        {
            PlacedCard card = pair.Value;
            bool visibleGoal = card.IsGoal && card.IsRevealed;
            CardId publicId = card.IsGoal && !card.IsRevealed ? new CardId("goal-hidden") : card.Definition.Id;
            return new BoardCardView(
                pair.Key,
                publicId,
                card.IsGoal && !card.IsRevealed ? EdgeMask.None : card.Definition.Edges,
                card.IsStart,
                card.IsGoal,
                card.IsRevealed,
                visibleGoal ? card.Goal : GoalContent.None);
        }).ToArray() ?? [];

        PlayerState? privateState = _state?.Players.Single(player => player.Id == recipient.PlayerId);
        PrivatePlayerView privateView = new(
            recipient.PlayerId,
            privateState?.Role,
            _state?.IsFinished == true ? privateState?.Gold : null,
            privateState?.Hand.ToArray() ?? [],
            privateState?.InspectedGoals
                .Select(pair => new GoalInspectionView(pair.Key, pair.Value))
                .ToArray() ?? []);

        PublicMatchView publicView = new(
            MatchId,
            Revision,
            Phase,
            _state?.RoundNumber ?? 0,
            _state?.TurnNumber ?? 0,
            _state?.Phase,
            _state?.IsFinished == false ? _state.CurrentPlayer.Id : null,
            _state?.DrawPile.Count ?? 0,
            _state?.DiscardPile.Count ?? 0,
            ReconnectDeadline,
            StatusMessage,
            players,
            board);
        return new PlayerSnapshot(publicView, privateView);
    }

    private Seat CreateSeat(string connectionId, string name, bool isHost)
    {
        string cleanName = name.Trim();
        if (cleanName.Length is < 1 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "O nome deve ter entre 1 e 24 caracteres.");
        }

        Seat seat = new(
            new PlayerId($"p{_seatsByPlayer.Count + 1}"),
            cleanName,
            isHost,
            CreateToken(),
            connectionId);
        _seatsByPlayer.Add(seat.PlayerId, seat);
        _seatsByConnection.Add(connectionId, seat);
        return seat;
    }

    private bool TryConnectedSeat(string connectionId, out Seat seat, out string? error)
    {
        if (!_seatsByConnection.TryGetValue(connectionId, out Seat? found) || !found.IsConnected)
        {
            seat = null!;
            error = "A conexão não pertence a um assento ativo.";
            return false;
        }

        seat = found;
        error = null;
        return true;
    }

    private JoinRoomResult AcceptedJoin(Seat seat) =>
        new(true, null, RoomCode, seat.ReconnectToken, Project(seat));

    private JoinRoomResult RejectedJoin(string error) => new(false, error, RoomCode, null, null);

    private void Abort(string reason)
    {
        Phase = RoomPhase.Aborted;
        ReconnectDeadline = null;
        StatusMessage = reason;
        Revision++;
    }

    private static string NormalizeRoomCode(string roomCode) => roomCode.Trim().ToUpperInvariant();

    private static string CreateRoomCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(6, bytes.ToArray(), (characters, random) =>
        {
            for (int index = 0; index < characters.Length; index++)
            {
                characters[index] = alphabet[random[index] % alphabet.Length];
            }
        });
    }

    private static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private static bool FixedEquals(string expected, string supplied)
    {
        byte[] expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        byte[] suppliedBytes = System.Text.Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private sealed class Seat(
        PlayerId playerId,
        string name,
        bool isHost,
        string reconnectToken,
        string connectionId)
    {
        public PlayerId PlayerId { get; } = playerId;
        public string Name { get; } = name;
        public bool IsHost { get; } = isHost;
        public string ReconnectToken { get; } = reconnectToken;
        public string? ConnectionId { get; set; } = connectionId;
        public bool IsConnected { get; set; } = true;
        public bool IsReady { get; set; }
    }
}
