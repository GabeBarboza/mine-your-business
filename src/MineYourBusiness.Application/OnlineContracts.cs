using MineYourBusiness.Domain;

namespace MineYourBusiness.Application;

public enum RoomPhase
{
    Lobby,
    Playing,
    Paused,
    Finished,
    Aborted,
}

public sealed record CommandEnvelope(
    string MatchId,
    PlayerId PlayerId,
    int TurnNumber,
    string CommandId,
    long ExpectedRevision,
    GameCommand Command);

public sealed record CommandReceipt(
    bool Accepted,
    bool IsDuplicate,
    string? Error,
    long Revision,
    IReadOnlyList<GameEvent> Events)
{
    public static CommandReceipt Rejected(string error, long revision) =>
        new(false, false, error, revision, []);
}

public sealed record RoomPlayerView(
    PlayerId Id,
    string Name,
    bool IsHost,
    bool IsReady,
    bool IsConnected,
    int CardCount,
    ToolType BrokenTools,
    PlayerRole? RevealedRole,
    int? RevealedGold);

public sealed record BoardCardView(
    BoardPosition Position,
    CardId CardId,
    EdgeMask Edges,
    bool IsStart,
    bool IsGoal,
    bool IsRevealed,
    GoalContent RevealedGoal);

public sealed record PublicMatchView(
    string MatchId,
    long Revision,
    RoomPhase RoomPhase,
    int RoundNumber,
    int TurnNumber,
    MatchPhase? MatchPhase,
    PlayerId? CurrentPlayerId,
    int DrawPileCount,
    int DiscardPileCount,
    DateTimeOffset? ReconnectDeadline,
    string? StatusMessage,
    IReadOnlyList<RoomPlayerView> Players,
    IReadOnlyList<BoardCardView> Board);

public sealed record PrivatePlayerView(
    PlayerId PlayerId,
    PlayerRole? Role,
    int? Gold,
    IReadOnlyList<CardDefinition> Hand,
    IReadOnlyList<GoalInspectionView> InspectedGoals);

public sealed record GoalInspectionView(BoardPosition Position, GoalContent Content);

public sealed record PlayerSnapshot(PublicMatchView Public, PrivatePlayerView Private);

public sealed record JoinRoomResult(
    bool Accepted,
    string? Error,
    string RoomCode,
    string? ReconnectToken,
    PlayerSnapshot? Snapshot);
