using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class RulesEngineTests
{
    [Fact]
    public void CommandOutsideTurnIsRejectedWithoutMutation()
    {
        GameState game = CreateGame();
        PlayerState other = game.Players[1];
        int cards = other.Hand.Count;

        CommandResult result = RulesEngine.Handle(game, new DiscardCommand(other.Id, other.Hand[0].Id));

        Assert.False(result.Accepted);
        Assert.Equal(cards, other.Hand.Count);
    }

    [Fact]
    public void ValidPathProducesEventsAndAdvancesTurn()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        CardDefinition path = new(new("test-path"), CardKind.Path, EdgeMask.East | EdgeMask.West);
        actor.Hand.Clear();
        actor.Hand.Add(path);

        CommandResult result = RulesEngine.Handle(game, new PlayPathCommand(actor.Id, path.Id, new(1, 0)));

        Assert.True(result.Accepted);
        Assert.True(game.Board.Cards.ContainsKey(new(1, 0)));
        Assert.Contains(result.Events, item => item is PathPlaced);
        Assert.NotEqual(actor.Id, game.CurrentPlayer.Id);
    }

    [Fact]
    public void BrokenToolPreventsPathButDoesNotPreventDiscard()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        CardDefinition path = new(new("test-path"), CardKind.Path, EdgeMask.East | EdgeMask.West);
        actor.Hand.Clear();
        actor.Hand.Add(path);
        actor.BrokenTools = ToolType.Lamp;

        CommandResult play = RulesEngine.Handle(game, new PlayPathCommand(actor.Id, path.Id, new(1, 0)));
        CommandResult discard = RulesEngine.Handle(game, new DiscardCommand(actor.Id, path.Id));

        Assert.False(play.Accepted);
        Assert.True(discard.Accepted);
    }

    [Fact]
    public void DuplicateBreakOfSameToolIsRejected()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        PlayerState target = game.Players[1];
        CardDefinition card = new(new("break"), CardKind.BreakTool, Tools: ToolType.Cart);
        actor.Hand.Clear();
        actor.Hand.Add(card);
        target.BrokenTools = ToolType.Cart;

        CommandResult result = RulesEngine.Handle(game, new BreakToolCommand(actor.Id, card.Id, target.Id));

        Assert.False(result.Accepted);
        Assert.Contains(card, actor.Hand);
    }

    [Fact]
    public void DualRepairRemovesOnlySelectedTool()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        PlayerState target = game.Players[1];
        CardDefinition card = new(
            new("repair"),
            CardKind.RepairTool,
            Tools: ToolType.Lamp | ToolType.Pickaxe);
        actor.Hand.Clear();
        actor.Hand.Add(card);
        target.BrokenTools = ToolType.Lamp | ToolType.Pickaxe;

        CommandResult result = RulesEngine.Handle(
            game,
            new RepairToolCommand(actor.Id, card.Id, target.Id, ToolType.Pickaxe));

        Assert.True(result.Accepted);
        Assert.Equal(ToolType.Lamp, target.BrokenTools);
    }

    [Fact]
    public void CollapseCannotRemoveStartOrGoal()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        CardDefinition collapse = new(new("collapse"), CardKind.Collapse);
        actor.Hand.Clear();
        actor.Hand.Add(collapse);

        CommandResult start = RulesEngine.Handle(
            game,
            new CollapsePathCommand(actor.Id, collapse.Id, BoardState.StartPosition));
        CommandResult goal = RulesEngine.Handle(
            game,
            new CollapsePathCommand(actor.Id, collapse.Id, BoardState.GoalPositions[0]));

        Assert.False(start.Accepted);
        Assert.False(goal.Accepted);
    }

    [Fact]
    public void MapKnowledgeRemainsOnActingPlayer()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        CardDefinition map = new(new("map"), CardKind.Map);
        actor.Hand.Clear();
        actor.Hand.Add(map);
        BoardPosition goal = BoardState.GoalPositions[0];

        CommandResult result = RulesEngine.Handle(game, new InspectGoalCommand(actor.Id, map.Id, goal));

        Assert.True(result.Accepted);
        Assert.True(actor.InspectedGoals.ContainsKey(goal));
        Assert.All(game.Players.Where(player => player != actor),
            player => Assert.Empty(player.InspectedGoals));
        Assert.False(game.Board.Cards[goal].IsRevealed);
    }

    [Fact]
    public void PlayerStillFullyBrokenAfterActionIsEliminatedFromRound()
    {
        GameState game = CreateGame();
        foreach (PlayerState player in game.Players)
        {
            player.Role = PlayerRole.Miner;
        }

        PlayerState actor = game.CurrentPlayer;
        actor.BrokenTools = ToolType.All;
        CardDefinition discard = actor.Hand[0];

        CommandResult result = RulesEngine.Handle(game, new DiscardCommand(actor.Id, discard.Id));

        Assert.True(result.Accepted);
        Assert.True(actor.IsEliminated);
        Assert.Empty(actor.Hand);
        Assert.Contains(result.Events, item => item is PlayerEliminated eliminated && eliminated.PlayerId == actor.Id);
        Assert.NotEqual(actor.Id, game.CurrentPlayer.Id);
    }

    [Fact]
    public void RepairingOneToolPreventsElimination()
    {
        GameState game = CreateGame();
        PlayerState actor = game.CurrentPlayer;
        CardDefinition repair = new(new("self-repair"), CardKind.RepairTool, Tools: ToolType.Lamp);
        actor.Hand.Clear();
        actor.Hand.Add(repair);
        actor.BrokenTools = ToolType.All;

        CommandResult result = RulesEngine.Handle(
            game,
            new RepairToolCommand(actor.Id, repair.Id, actor.Id, ToolType.Lamp));

        Assert.True(result.Accepted);
        Assert.False(actor.IsEliminated);
        Assert.Equal(ToolType.Cart | ToolType.Pickaxe, actor.BrokenTools);
        Assert.DoesNotContain(result.Events, item => item is PlayerEliminated);
    }

    [Fact]
    public void SaboteursEndRoundWhenTheyMatchRemainingMiners()
    {
        GameState game = CreateGame();
        game.Players[0].Role = PlayerRole.Miner;
        game.Players[1].Role = PlayerRole.Miner;
        game.Players[2].Role = PlayerRole.Saboteur;
        PlayerState actor = game.CurrentPlayer;
        actor.BrokenTools = ToolType.All;
        CardDefinition discard = actor.Hand[0];

        CommandResult result = RulesEngine.Handle(game, new DiscardCommand(actor.Id, discard.Id));

        Assert.True(result.Accepted);
        Assert.Contains(result.Events, item => item is SaboteursDominated
        {
            SaboteursRemaining: 1,
            MinersRemaining: 1,
        });
        Assert.Contains(result.Events, item => item is RoundEnded { RoundNumber: 1 });
        Assert.Contains(result.Events, item => item is GoldAwarded awarded &&
            awarded.PlayerId == game.Players[2].Id && awarded.Amount == 5);
    }

    [Fact]
    public void TextPolicyCompletesThreeRoundsDeterministically()
    {
        GameState first = RunDiscardSimulation(3, 1234, out IReadOnlyList<GameEvent> firstEvents);
        GameState second = RunDiscardSimulation(3, 1234, out IReadOnlyList<GameEvent> secondEvents);

        Assert.True(first.IsFinished);
        Assert.Equal(3, first.RoundNumber);
        Assert.Equal(
            first.Players.Select(player => player.Gold),
            second.Players.Select(player => player.Gold));
        Assert.Equal(3, firstEvents.Count(item => item is RoundEnded));
        Assert.Single(firstEvents, item => item is MatchEnded);
        Assert.Equal(firstEvents.Select(item => item.GetType()), secondEvents.Select(item => item.GetType()));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void CompleteMatchWorksForEverySupportedPlayerCount(int playerCount)
    {
        GameState game = RunDiscardSimulation(playerCount, 99, out IReadOnlyList<GameEvent> events);

        Assert.True(game.IsFinished);
        Assert.Equal(3, events.Count(item => item is RoundEnded));
    }

    private static GameState RunDiscardSimulation(
        int playerCount,
        long seed,
        out IReadOnlyList<GameEvent> allEvents)
    {
        GameState game = CreateGame(playerCount, seed);
        List<GameEvent> events = [];
        int safety = 1000;
        while (!game.IsFinished && safety-- > 0)
        {
            AssertCardConservation(game);
            PlayerState player = game.CurrentPlayer;
            GameCommand command = player.Hand.Count == 0
                ? new PassCommand(player.Id)
                : new DiscardCommand(player.Id, player.Hand[0].Id);
            CommandResult result = RulesEngine.Handle(game, command);
            Assert.True(result.Accepted, result.Error);
            events.AddRange(result.Events);
        }

        Assert.True(safety > 0, "Simulation exceeded its safety limit.");
        allEvents = events;
        return game;
    }

    private static void AssertCardConservation(GameState game)
    {
        IEnumerable<CardDefinition> boardCards = game.Board.Cards.Values
            .Where(card => !card.IsStart && !card.IsGoal)
            .Select(card => card.Definition);
        CardDefinition[] cards =
        [
            .. game.DrawPile,
            .. game.DiscardPile,
            .. game.Players.SelectMany(player => player.Hand),
            .. boardCards,
        ];

        Assert.Equal(67, cards.Length);
        Assert.Equal(cards.Length, cards.Select(card => card.Id).Distinct().Count());
    }

    private static GameState CreateGame(long seed = 7) => GameFactory.Create(
    [
        (new PlayerId("p1"), "Player 1"),
        (new PlayerId("p2"), "Player 2"),
        (new PlayerId("p3"), "Player 3"),
    ],
    seed);

    private static GameState CreateGame(int playerCount, long seed) => GameFactory.Create(
        Enumerable.Range(1, playerCount)
            .Select(index => (new PlayerId($"p{index}"), $"Player {index}")),
        seed);
}
