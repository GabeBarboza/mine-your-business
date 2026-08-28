namespace MineYourBusiness.Domain;

public readonly record struct CardId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct PlayerId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct BoardPosition(int X, int Y)
{
    public BoardPosition Move(Direction direction) => direction switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.East => this with { X = X + 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.West => this with { X = X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}

public enum Direction
{
    North,
    East,
    South,
    West,
}

[Flags]
public enum EdgeMask
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8,
    All = North | East | South | West,
}

public static class EdgeMaskExtensions
{
    public static EdgeMask ToEdge(this Direction direction) => direction switch
    {
        Direction.North => EdgeMask.North,
        Direction.East => EdgeMask.East,
        Direction.South => EdgeMask.South,
        Direction.West => EdgeMask.West,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public static bool IsOpen(this EdgeMask mask, Direction direction) =>
        (mask & direction.ToEdge()) != EdgeMask.None;
}
