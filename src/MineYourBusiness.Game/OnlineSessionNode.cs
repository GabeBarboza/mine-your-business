using System.Text.Json;
using Godot;
using MineYourBusiness.Application;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>Thin ENet adapter. All authority and secret projection remain in Application.</summary>
public partial class OnlineSessionNode : Node
{
    private const int ServerPeerId = 1;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private AuthoritativeRoom? _room;
    private ENetMultiplayerPeer? _peer;
    private string _name = string.Empty;
    private string _roomCode = string.Empty;
    private string _address = "127.0.0.1";
    private int _port = 24828;
    private string? _reconnectToken;

    public event Action<PlayerSnapshot>? SnapshotChanged;
    public event Action<string, bool>? StatusChanged;

    public PlayerSnapshot? Snapshot { get; private set; }
    public bool IsHost => _room is not null;
    public string RoomCode => _roomCode;
    public string? ReconnectToken => _reconnectToken;

    public Error Host(string name, int port, long seed)
    {
        Close();
        _name = name;
        _port = port;
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateServer(port, 9);
        if (error != Error.Ok)
        {
            _peer = null;
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        WireSignals();
        try
        {
            _room = new AuthoritativeRoom(ConnectionId(ServerPeerId), name, seed);
        }
        catch (ArgumentOutOfRangeException)
        {
            Close();
            return Error.InvalidParameter;
        }
        _roomCode = _room.RoomCode;
        PublishHostSnapshot();
        StatusChanged?.Invoke($"Sala {_roomCode} aberta na porta {port}.", false);
        return Error.Ok;
    }

    public Error Join(
        string address,
        int port,
        string roomCode,
        string name,
        string? reconnectToken = null)
    {
        Close();
        _address = address.Trim();
        _port = port;
        _roomCode = roomCode.Trim().ToUpperInvariant();
        _name = name.Trim();
        _reconnectToken = reconnectToken;
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateClient(_address, _port);
        if (error != Error.Ok)
        {
            _peer = null;
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        WireSignals();
        StatusChanged?.Invoke("Conectando ao anfitrião…", false);
        return Error.Ok;
    }

    public Error Reconnect() => Join(_address, _port, _roomCode, _name, _reconnectToken);

    public void SetReady(bool ready) => SendRequest(new WireRequest { Operation = "ready", Ready = ready });

    public void StartMatch() => SendRequest(new WireRequest { Operation = "start" });

    public void Submit(GameCommand command)
    {
        if (Snapshot is null)
        {
            return;
        }

        SendRequest(new WireRequest
        {
            Operation = "command",
            MatchId = Snapshot.Public.MatchId,
            PlayerId = command.PlayerId.Value,
            TurnNumber = Snapshot.Public.TurnNumber,
            ExpectedRevision = Snapshot.Public.Revision,
            CommandId = Guid.NewGuid().ToString("N"),
            Command = WireCommand.From(command),
        });
    }

    public void Close()
    {
        if (_peer is not null)
        {
            UnwireSignals();
            _peer.Close();
            _peer = null;
        }

        Multiplayer.MultiplayerPeer = null;
        _room = null;
        Snapshot = null;
    }

    public override void _Process(double delta)
    {
        if (_room is null)
        {
            return;
        }

        RoomPhase before = _room.Phase;
        _room.Tick();
        if (_room.Phase != before)
        {
            BroadcastSnapshots();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ServerRequest(string json)
    {
        if (_room is null || !Multiplayer.IsServer())
        {
            return;
        }

        int sender = Multiplayer.GetRemoteSenderId();
        HandleServerRequest(ConnectionId(sender), sender, json);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientMessage(string json)
    {
        WireResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<WireResponse>(json, _json);
        }
        catch (JsonException)
        {
            StatusChanged?.Invoke("O anfitrião enviou uma mensagem inválida.", true);
            return;
        }

        if (response is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(response.ReconnectToken))
        {
            _reconnectToken = response.ReconnectToken;
        }

        if (response.Snapshot is not null)
        {
            Publish(response.Snapshot);
        }

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            StatusChanged?.Invoke(response.Message, response.IsError);
        }
    }

    private void WireSignals()
    {
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    private void UnwireSignals()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ConnectionFailed -= OnConnectionFailed;
        Multiplayer.ServerDisconnected -= OnServerDisconnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    private void OnConnectedToServer() => RpcId(
        ServerPeerId,
        MethodName.ServerRequest,
        JsonSerializer.Serialize(new WireRequest
        {
            Operation = "join",
            RoomCode = _roomCode,
            Name = _name,
            ReconnectToken = _reconnectToken,
        }, _json));

    private void OnConnectionFailed() => StatusChanged?.Invoke("Não foi possível alcançar o anfitrião.", true);

    private void OnServerDisconnected() =>
        StatusChanged?.Invoke("Conexão perdida. Use RECONECTAR dentro da janela da sala.", true);

    private void OnPeerDisconnected(long peerId)
    {
        if (_room is null)
        {
            return;
        }

        _room.Disconnect(ConnectionId(peerId));
        BroadcastSnapshots();
    }

    private void SendRequest(WireRequest request)
    {
        string json = JsonSerializer.Serialize(request, _json);
        if (_room is not null)
        {
            HandleServerRequest(ConnectionId(ServerPeerId), ServerPeerId, json);
        }
        else
        {
            RpcId(ServerPeerId, MethodName.ServerRequest, json);
        }
    }

    private void HandleServerRequest(string connectionId, int peerId, string json)
    {
        WireRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<WireRequest>(json, _json);
        }
        catch (JsonException)
        {
            SendResponse(peerId, new WireResponse { IsError = true, Message = "Mensagem de rede inválida." });
            return;
        }

        if (request is null || _room is null)
        {
            return;
        }

        switch (request.Operation)
        {
            case "join":
                JoinRoomResult joined = _room.Join(
                    connectionId,
                    request.RoomCode ?? string.Empty,
                    request.Name ?? string.Empty,
                    request.ReconnectToken);
                SendResponse(peerId, new WireResponse
                {
                    IsError = !joined.Accepted,
                    Message = joined.Error,
                    ReconnectToken = joined.ReconnectToken,
                    Snapshot = joined.Snapshot,
                });
                if (joined.Accepted)
                {
                    BroadcastSnapshots();
                }

                break;
            case "ready":
                bool ready = _room.SetReady(connectionId, request.Ready, out string? readyError);
                RespondAndBroadcast(peerId, ready, readyError);
                break;
            case "start":
                bool started = _room.Start(connectionId, out string? startError);
                RespondAndBroadcast(peerId, started, startError);
                break;
            case "command" when request.Command is not null:
                GameCommand? command = request.Command.ToDomain();
                if (command is null)
                {
                    SendResponse(peerId, new WireResponse { IsError = true, Message = "Comando desconhecido." });
                    break;
                }

                CommandReceipt receipt = _room.Submit(connectionId, new CommandEnvelope(
                    request.MatchId ?? string.Empty,
                    new PlayerId(request.PlayerId ?? string.Empty),
                    request.TurnNumber,
                    request.CommandId ?? string.Empty,
                    request.ExpectedRevision,
                    command));
                RespondAndBroadcast(peerId, receipt.Accepted, receipt.Error);
                break;
            default:
                SendResponse(peerId, new WireResponse { IsError = true, Message = "Operação de rede desconhecida." });
                break;
        }
    }

    private void RespondAndBroadcast(int peerId, bool success, string? error)
    {
        if (!success)
        {
            SendResponse(peerId, new WireResponse { IsError = true, Message = error });
        }

        BroadcastSnapshots();
    }

    private void BroadcastSnapshots()
    {
        if (_room is null)
        {
            return;
        }

        foreach (long peerId in Multiplayer.GetPeers())
        {
            SendSnapshot((int)peerId);
        }

        PublishHostSnapshot();
    }

    private void PublishHostSnapshot()
    {
        if (_room is not null)
        {
            Publish(_room.SnapshotForConnection(ConnectionId(ServerPeerId)));
        }
    }

    private void SendSnapshot(int peerId)
    {
        if (_room is null)
        {
            return;
        }

        try
        {
            PlayerSnapshot snapshot = _room.SnapshotForConnection(ConnectionId(peerId));
            SendResponse(peerId, new WireResponse { Snapshot = snapshot });
        }
        catch (InvalidOperationException)
        {
            // A peer may be connected before it has successfully joined the room.
        }
    }

    private void SendResponse(int peerId, WireResponse response)
    {
        if (peerId == ServerPeerId)
        {
            if (response.Snapshot is not null)
            {
                Publish(response.Snapshot);
            }

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                StatusChanged?.Invoke(response.Message, response.IsError);
            }

            return;
        }

        RpcId(peerId, MethodName.ClientMessage, JsonSerializer.Serialize(response, _json));
    }

    private void Publish(PlayerSnapshot snapshot)
    {
        Snapshot = snapshot;
        SnapshotChanged?.Invoke(snapshot);
    }

    private static string ConnectionId(long peerId) => peerId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private sealed class WireRequest
    {
        public string Operation { get; set; } = string.Empty;
        public string? RoomCode { get; set; }
        public string? Name { get; set; }
        public string? ReconnectToken { get; set; }
        public bool Ready { get; set; }
        public string? MatchId { get; set; }
        public string? PlayerId { get; set; }
        public int TurnNumber { get; set; }
        public string? CommandId { get; set; }
        public long ExpectedRevision { get; set; }
        public WireCommand? Command { get; set; }
    }

    private sealed class WireResponse
    {
        public bool IsError { get; set; }
        public string? Message { get; set; }
        public string? ReconnectToken { get; set; }
        public PlayerSnapshot? Snapshot { get; set; }
    }

    private sealed class WireCommand
    {
        public string Type { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string? CardId { get; set; }
        public string? TargetPlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public ToolType Tool { get; set; }

        public static WireCommand From(GameCommand command) => command switch
        {
            PlayPathCommand value => New("path", value, value.CardId, position: value.Position),
            BreakToolCommand value => New("break", value, value.CardId, value.TargetPlayerId),
            RepairToolCommand value => New("repair", value, value.CardId, value.TargetPlayerId, tool: value.Tool),
            CollapsePathCommand value => New("collapse", value, value.CardId, position: value.Position),
            InspectGoalCommand value => New("map", value, value.CardId, position: value.Position),
            DiscardCommand value => New("discard", value, value.CardId),
            PassCommand value => New("pass", value),
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };

        public GameCommand? ToDomain()
        {
            MineYourBusiness.Domain.PlayerId actor = new(PlayerId);
            CardId card = new(CardId ?? string.Empty);
            MineYourBusiness.Domain.PlayerId target = new(TargetPlayerId ?? string.Empty);
            BoardPosition position = new(X, Y);
            return Type switch
            {
                "path" => new PlayPathCommand(actor, card, position),
                "break" => new BreakToolCommand(actor, card, target),
                "repair" => new RepairToolCommand(actor, card, target, Tool),
                "collapse" => new CollapsePathCommand(actor, card, position),
                "map" => new InspectGoalCommand(actor, card, position),
                "discard" => new DiscardCommand(actor, card),
                "pass" => new PassCommand(actor),
                _ => null,
            };
        }

        private static WireCommand New(
            string type,
            GameCommand command,
            CardId? cardId = null,
            MineYourBusiness.Domain.PlayerId? target = null,
            BoardPosition? position = null,
            ToolType tool = ToolType.None) => new()
            {
                Type = type,
                PlayerId = command.PlayerId.Value,
                CardId = cardId?.Value,
                TargetPlayerId = target?.Value,
                X = position?.X ?? 0,
                Y = position?.Y ?? 0,
                Tool = tool,
            };
    }
}
