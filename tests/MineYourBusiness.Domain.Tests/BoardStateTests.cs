using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class BoardStateTests
{
    [Fact]
    public void InitialBoardUsesPlannedCoordinates()
    {
        BoardState board = BoardState.CreateInitial(
            [GoalContent.Stone, GoalContent.Gold, GoalContent.Stone]);

        Assert.True(board.Cards[BoardState.StartPosition].IsStart);
        Assert.Equal([new(8, -2), new(8, 0), new(8, 2)], BoardState.GoalPositions);
        Assert.All(BoardState.GoalPositions, position => Assert.False(board.Cards[position].IsRevealed));
    }

    [Fact]
    public void PlacementRequiresMatchingEdgesAndConnectionToStart()
    {
        BoardState board = BoardState.CreateInitial(
            [GoalContent.Stone, GoalContent.Gold, GoalContent.Stone]);
        CardDefinition horizontal = Path("horizontal", EdgeMask.East | EdgeMask.West);
        CardDefinition vertical = Path("vertical", EdgeMask.North | EdgeMask.South);

        Assert.True(board.ValidatePlacement(new(1, 0), horizontal).IsValid);
        Assert.False(board.ValidatePlacement(new(1, 0), vertical).IsValid);
        Assert.Contains("match", board.ValidatePlacement(new(1, 0), vertical).Error);
    }

    [Fact]
    public void PlacementBesideDisconnectedIslandIsRejected()
    {
        CardDefinition horizontal = Path("horizontal", EdgeMask.East | EdgeMask.West);
        BoardState board = new(
        [
            (BoardState.StartPosition, new PlacedCard(CardCatalog.StartCard, IsStart: true)),
            (new BoardPosition(3, 0), new PlacedCard(horizontal)),
        ]);

        PlacementValidation result = board.ValidatePlacement(new(4, 0), horizontal);

        Assert.False(result.IsValid);
        Assert.Contains("start", result.Error);
    }

    [Fact]
    public void ConnectivityIsRecalculatedAfterCollapse()
    {
        CardDefinition horizontal = Path("horizontal", EdgeMask.East | EdgeMask.West);
        BoardState board = new(
        [
            (BoardState.StartPosition, new PlacedCard(CardCatalog.StartCard, IsStart: true)),
            (new BoardPosition(1, 0), new PlacedCard(horizontal)),
            (new BoardPosition(2, 0), new PlacedCard(horizontal)),
        ]);

        Assert.Contains(new BoardPosition(2, 0), board.FindReachableFromStart());
        Assert.True(board.Remove(new(1, 0)));
        Assert.DoesNotContain(new BoardPosition(2, 0), board.FindReachableFromStart());
        Assert.False(board.Remove(BoardState.StartPosition));
    }

    [Fact]
    public void OnePlacementCanRevealTwoGoals()
    {
        CardDefinition horizontal = Path("horizontal", EdgeMask.East | EdgeMask.West);
        List<(BoardPosition, PlacedCard)> cards =
        [
            (BoardState.StartPosition, new(CardCatalog.StartCard, IsStart: true)),
            (new(1, 0), new(Path("corner", EdgeMask.West | EdgeMask.North))),
            (new(1, -1), new(Path("corner-2", EdgeMask.South | EdgeMask.East))),
        ];
        cards.AddRange(Enumerable.Range(2, 6).Select(x =>
            (new BoardPosition(x, -1), new PlacedCard(horizontal))));
        cards.Add((new(8, -2), new(CardCatalog.GoalCard(GoalContent.Stone), Goal: GoalContent.Stone, IsRevealed: false)));
        cards.Add((new(8, 0), new(CardCatalog.GoalCard(GoalContent.Gold), Goal: GoalContent.Gold, IsRevealed: false)));
        cards.Add((new(8, 2), new(CardCatalog.GoalCard(GoalContent.Stone), Goal: GoalContent.Stone, IsRevealed: false)));
        BoardState board = new(cards);

        board.Place(new(8, -1), Path("fork", EdgeMask.West | EdgeMask.North | EdgeMask.South));

        Assert.Equal(2, board.FindConnectedHiddenGoals().Count);
    }

    private static CardDefinition Path(string id, EdgeMask edges) =>
        new(new(id), CardKind.Path, edges);
}
