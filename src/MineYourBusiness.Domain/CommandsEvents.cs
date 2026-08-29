namespace MineYourBusiness.Domain;

public abstract record GameCommand(PlayerId PlayerId);

public sealed record PlayPathCommand(PlayerId PlayerId, CardId CardId, BoardPosition Position)
    : GameCommand(PlayerId);

public sealed record BreakToolCommand(PlayerId PlayerId, CardId CardId, PlayerId TargetPlayerId)
    : GameCommand(PlayerId);

public sealed record RepairToolCommand(
    PlayerId PlayerId,
    CardId CardId,
    PlayerId TargetPlayerId,
    ToolType Tool)
    : GameCommand(PlayerId);

public sealed record CollapsePathCommand(PlayerId PlayerId, CardId CardId, BoardPosition Position)
    : GameCommand(PlayerId);

public sealed record InspectGoalCommand(PlayerId PlayerId, CardId CardId, BoardPosition Position)
    : GameCommand(PlayerId);

public sealed record DiscardCommand(PlayerId PlayerId, CardId CardId)
    : GameCommand(PlayerId);

public sealed record PassCommand(PlayerId PlayerId) : GameCommand(PlayerId);

public abstract record GameEvent;

public sealed record RoundStarted(int RoundNumber, PlayerId StartingPlayerId) : GameEvent;
public sealed record CardPlayed(PlayerId PlayerId, CardId CardId) : GameEvent;
public sealed record PathPlaced(PlayerId PlayerId, CardId CardId, BoardPosition Position) : GameEvent;
public sealed record CardDiscarded(PlayerId PlayerId) : GameEvent;
public sealed record PlayerPassed(PlayerId PlayerId) : GameEvent;
public sealed record ToolBroken(PlayerId PlayerId, PlayerId TargetPlayerId, ToolType Tool) : GameEvent;
public sealed record ToolRepaired(PlayerId PlayerId, PlayerId TargetPlayerId, ToolType Tool) : GameEvent;
public sealed record PathCollapsed(PlayerId PlayerId, BoardPosition Position) : GameEvent;
public sealed record GoalInspected(PlayerId PlayerId, BoardPosition Position, GoalContent Content) : GameEvent;
public sealed record GoalRevealed(BoardPosition Position, GoalContent Content) : GameEvent;
public sealed record CardDrawn(PlayerId PlayerId, CardId CardId) : GameEvent;
public sealed record TurnAdvanced(int TurnNumber, PlayerId PlayerId) : GameEvent;
public sealed record PlayerEliminated(PlayerId PlayerId) : GameEvent;
public sealed record SaboteursDominated(int SaboteursRemaining, int MinersRemaining) : GameEvent;
public sealed record GoldAwarded(PlayerId PlayerId, int Amount) : GameEvent;
public sealed record RoleRevealed(PlayerId PlayerId, PlayerRole Role) : GameEvent;
public sealed record RoundEnded(int RoundNumber, bool GoldReached, int EliminatedPlayers = 0) : GameEvent;
public sealed record MatchEnded(IReadOnlyList<PlayerId> Winners) : GameEvent;

public sealed record CommandResult(bool Accepted, string? Error, IReadOnlyList<GameEvent> Events)
{
    public static CommandResult Rejected(string error) => new(false, error, []);
    public static CommandResult Success(IReadOnlyList<GameEvent> events) => new(true, null, events);
}
