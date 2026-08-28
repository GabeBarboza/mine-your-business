using MineYourBusiness.Domain;

namespace MineYourBusiness.Application;

/// <summary>
/// Coordinates a hot-seat debug match without coupling the domain to Godot.
/// </summary>
public sealed class LocalMatchController
{
    private const int AdministrativeSafetyLimit = 500;
    private readonly List<GameEvent> _eventLog = [];

    public LocalMatchController(IEnumerable<string> playerNames, long seed)
    {
        string[] names = playerNames
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .ToArray();
        if (names.Length is < 3 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerNames),
                "A local match requires 3 to 10 named players.");
        }

        State = GameFactory.Create(
            names.Select((name, index) => (new PlayerId($"p{index + 1}"), name)),
            seed);
    }

    public GameState State { get; }
    public IReadOnlyList<GameEvent> EventLog => _eventLog;

    public CommandResult Execute(GameCommand command)
    {
        CommandResult result = RulesEngine.Handle(State, command);
        if (result.Accepted)
        {
            _eventLog.AddRange(result.Events);
        }

        return result;
    }

    public CommandResult PlayCard(
        CardDefinition card,
        BoardPosition? position = null,
        PlayerId? targetPlayerId = null,
        ToolType tool = ToolType.None)
    {
        PlayerId actor = State.CurrentPlayer.Id;
        GameCommand? command = card.Kind switch
        {
            CardKind.Path when position is BoardPosition boardPosition =>
                new PlayPathCommand(actor, card.Id, boardPosition),
            CardKind.BreakTool when targetPlayerId is PlayerId target =>
                new BreakToolCommand(actor, card.Id, target),
            CardKind.RepairTool when targetPlayerId is PlayerId target =>
                new RepairToolCommand(actor, card.Id, target, tool),
            CardKind.Collapse when position is BoardPosition boardPosition =>
                new CollapsePathCommand(actor, card.Id, boardPosition),
            CardKind.Map when position is BoardPosition boardPosition =>
                new InspectGoalCommand(actor, card.Id, boardPosition),
            _ => null,
        };

        return command is null
            ? CommandResult.Rejected("A jogada ainda precisa de um destino válido.")
            : Execute(command);
    }

    public CommandResult Discard(CardDefinition card) =>
        Execute(new DiscardCommand(State.CurrentPlayer.Id, card.Id));

    public IReadOnlyList<GameEvent> AdvanceAdministrativeTurn()
    {
        CommandResult result = Execute(ChooseAdministrativeCommand());
        if (!result.Accepted)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Events;
    }

    public IReadOnlyList<GameEvent> AdvanceToNextRound()
    {
        int round = State.RoundNumber;
        return AdvanceUntil(() => State.IsFinished || State.RoundNumber != round);
    }

    public IReadOnlyList<GameEvent> AdvanceToMatchEnd() => AdvanceUntil(() => State.IsFinished);

    private List<GameEvent> AdvanceUntil(Func<bool> stop)
    {
        List<GameEvent> events = [];
        int remaining = AdministrativeSafetyLimit;
        while (!stop() && remaining-- > 0)
        {
            events.AddRange(AdvanceAdministrativeTurn());
        }

        if (!stop())
        {
            throw new InvalidOperationException("Administrative advance exceeded its safety limit.");
        }

        return events;
    }

    private GameCommand ChooseAdministrativeCommand()
    {
        PlayerState player = State.CurrentPlayer;
        if (player.Hand.Count == 0)
        {
            return new PassCommand(player.Id);
        }

        if (player.CanPlayPath)
        {
            foreach (CardDefinition card in player.Hand.Where(card => card.Kind == CardKind.Path))
            {
                foreach (BoardPosition position in CandidatePositions())
                {
                    if (State.Board.ValidatePlacement(position, card).IsValid)
                    {
                        return new PlayPathCommand(player.Id, card.Id, position);
                    }
                }
            }
        }

        return new DiscardCommand(player.Id, player.Hand[0].Id);
    }

    private IEnumerable<BoardPosition> CandidatePositions()
    {
        HashSet<BoardPosition> candidates = [];
        foreach (BoardPosition occupied in State.Board.Cards.Keys)
        {
            foreach (Direction direction in Enum.GetValues<Direction>())
            {
                BoardPosition candidate = occupied.Move(direction);
                if (!State.Board.Cards.ContainsKey(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates.OrderBy(position => position.X).ThenBy(position => position.Y);
    }
}
