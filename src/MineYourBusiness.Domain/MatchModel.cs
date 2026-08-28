namespace MineYourBusiness.Domain;

public enum MatchPhase
{
    RoundSetup,
    AwaitingAction,
    ResolvingAction,
    DrawingCard,
    CheckingRoundEnd,
    RoundSummary,
    MatchSummary,
}

public sealed class PlayerState
{
    public PlayerState(PlayerId id, string name)
    {
        Id = id;
        Name = name;
    }

    public PlayerId Id { get; }
    public string Name { get; }
    public PlayerRole Role { get; internal set; }
    public List<CardDefinition> Hand { get; } = [];
    public ToolType BrokenTools { get; internal set; }
    public int Gold { get; internal set; }
    public Dictionary<BoardPosition, GoalContent> InspectedGoals { get; } = [];

    public bool CanPlayPath => BrokenTools == ToolType.None;
}

public sealed class GameState
{
    private readonly List<CardDefinition> _drawPile = [];
    private readonly List<CardDefinition> _discardPile = [];

    internal GameState(IEnumerable<(PlayerId Id, string Name)> players, long seed)
    {
        Players = players.Select(player => new PlayerState(player.Id, player.Name)).ToArray();
        if (Players.Count is < 3 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(players), "A match requires 3 to 10 players.");
        }

        if (Players.Select(player => player.Id).Distinct().Count() != Players.Count)
        {
            throw new ArgumentException("Player identifiers must be unique.", nameof(players));
        }

        Random = new(seed);
        Seed = seed;
    }

    public long Seed { get; }
    public int RoundNumber { get; internal set; }
    public int TurnNumber { get; internal set; }
    public int CurrentPlayerIndex { get; internal set; }
    public int StartingPlayerIndex { get; internal set; }
    public MatchPhase Phase { get; internal set; } = MatchPhase.RoundSetup;
    public IReadOnlyList<PlayerState> Players { get; }
    public BoardState Board { get; internal set; } = new();
    public IReadOnlyList<CardDefinition> DrawPile => _drawPile;
    public IReadOnlyList<CardDefinition> DiscardPile => _discardPile;
    public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
    public PlayerId? LastPathPlayerId { get; internal set; }
    public bool GoldWasReached { get; internal set; }
    public bool IsFinished => Phase == MatchPhase.MatchSummary;
    internal SeededRandom Random { get; }

    internal void BeginRound()
    {
        RoundNumber++;
        Phase = MatchPhase.RoundSetup;
        GoldWasReached = false;
        LastPathPlayerId = null;
        Board = BoardState.CreateInitial(RoundSetup.ShuffleGoals(Random));
        _discardPile.Clear();
        _drawPile.Clear();
        _drawPile.AddRange(CardCatalog.CreateDrawDeck());
        Random.Shuffle(_drawPile);

        IReadOnlyList<PlayerRole> roles = RoundSetup.DealRoles(Players.Count, Random);
        for (int i = 0; i < Players.Count; i++)
        {
            PlayerState player = Players[i];
            player.Role = roles[i];
            player.Hand.Clear();
            player.BrokenTools = ToolType.None;
            player.InspectedGoals.Clear();
        }

        int handSize = RoundSetup.InitialHandSize(Players.Count);
        for (int card = 0; card < handSize; card++)
        {
            foreach (PlayerState player in Players)
            {
                DrawTo(player);
            }
        }

        CurrentPlayerIndex = StartingPlayerIndex;
        TurnNumber = 1;
        Phase = MatchPhase.AwaitingAction;
    }

    internal CardDefinition? DrawTo(PlayerState player)
    {
        if (_drawPile.Count == 0)
        {
            return null;
        }

        int top = _drawPile.Count - 1;
        CardDefinition card = _drawPile[top];
        _drawPile.RemoveAt(top);
        player.Hand.Add(card);
        return card;
    }

    internal void Discard(CardDefinition card) => _discardPile.Add(card);
}

public static class GameFactory
{
    public static GameState Create(IEnumerable<(PlayerId Id, string Name)> players, long seed)
    {
        GameState state = new(players, seed);
        state.BeginRound();
        return state;
    }
}
