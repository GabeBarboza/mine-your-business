namespace MineYourBusiness.Domain;

public enum CardKind
{
    Path,
    BreakTool,
    RepairTool,
    Collapse,
    Map,
}

[Flags]
public enum ToolType
{
    None = 0,
    Lamp = 1,
    Cart = 2,
    Pickaxe = 4,
}

public enum GoalContent
{
    None,
    Stone,
    Gold,
}

public sealed record CardDefinition(
    CardId Id,
    CardKind Kind,
    EdgeMask Edges = EdgeMask.None,
    ToolType Tools = ToolType.None);

public sealed record PlacedCard(
    CardDefinition Definition,
    bool IsStart = false,
    GoalContent Goal = GoalContent.None,
    bool IsRevealed = true)
{
    public bool IsGoal => Goal != GoalContent.None;
}

public static class CardCatalog
{
    private static readonly EdgeMask[] PathShapes =
    [
        EdgeMask.East | EdgeMask.West,
        EdgeMask.North | EdgeMask.South,
        EdgeMask.North | EdgeMask.East,
        EdgeMask.East | EdgeMask.South,
        EdgeMask.South | EdgeMask.West,
        EdgeMask.West | EdgeMask.North,
        EdgeMask.North | EdgeMask.East | EdgeMask.West,
        EdgeMask.North | EdgeMask.East | EdgeMask.South,
        EdgeMask.East | EdgeMask.South | EdgeMask.West,
        EdgeMask.South | EdgeMask.West | EdgeMask.North,
        EdgeMask.All,
        EdgeMask.North,
        EdgeMask.East,
        EdgeMask.South,
        EdgeMask.West,
    ];

    public static IReadOnlyList<CardDefinition> CreateDrawDeck()
    {
        List<CardDefinition> cards = [];

        for (int i = 0; i < 40; i++)
        {
            cards.Add(new(new($"path-{i + 1:00}"), CardKind.Path, PathShapes[i % PathShapes.Length]));
        }

        AddToolCards(cards, CardKind.BreakTool, 3, "break");
        AddToolCards(cards, CardKind.RepairTool, 3, "repair");

        cards.Add(new(new("repair-lamp-cart"), CardKind.RepairTool, Tools: ToolType.Lamp | ToolType.Cart));
        cards.Add(new(new("repair-lamp-pickaxe"), CardKind.RepairTool, Tools: ToolType.Lamp | ToolType.Pickaxe));
        cards.Add(new(new("repair-cart-pickaxe"), CardKind.RepairTool, Tools: ToolType.Cart | ToolType.Pickaxe));

        for (int i = 1; i <= 3; i++)
        {
            cards.Add(new(new($"collapse-{i}"), CardKind.Collapse));
            cards.Add(new(new($"map-{i}"), CardKind.Map));
        }

        return cards;
    }

    public static CardDefinition StartCard { get; } =
        new(new("start"), CardKind.Path, EdgeMask.All);

    public static CardDefinition GoalCard(GoalContent content) =>
        new(new(content == GoalContent.Gold ? "goal-gold" : "goal-stone"), CardKind.Path, EdgeMask.All);

    private static void AddToolCards(List<CardDefinition> cards, CardKind kind, int each, string prefix)
    {
        foreach (ToolType tool in new[] { ToolType.Lamp, ToolType.Cart, ToolType.Pickaxe })
        {
            for (int i = 1; i <= each; i++)
            {
                cards.Add(new(new($"{prefix}-{tool.ToString().ToLowerInvariant()}-{i}"), kind, Tools: tool));
            }
        }
    }
}
