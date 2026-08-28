namespace MineYourBusiness.Domain;

public sealed record PlacementValidation(bool IsValid, string? Error)
{
    public static PlacementValidation Valid { get; } = new(true, null);
    public static PlacementValidation Invalid(string error) => new(false, error);
}

public sealed class BoardState
{
    public static BoardPosition StartPosition { get; } = new(0, 0);
    public static IReadOnlyList<BoardPosition> GoalPositions { get; } =
        [new(8, -2), new(8, 0), new(8, 2)];

    private readonly Dictionary<BoardPosition, PlacedCard> _cards = [];

    public IReadOnlyDictionary<BoardPosition, PlacedCard> Cards => _cards;

    public BoardState(IEnumerable<(BoardPosition Position, PlacedCard Card)>? cards = null)
    {
        if (cards is null)
        {
            return;
        }

        foreach ((BoardPosition position, PlacedCard card) in cards)
        {
            _cards.Add(position, card);
        }
    }

    public static BoardState CreateInitial(IReadOnlyList<GoalContent> goals)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(goals.Count, 3);
        BoardState board = new();
        board._cards.Add(StartPosition, new(CardCatalog.StartCard, IsStart: true));

        for (int i = 0; i < GoalPositions.Count; i++)
        {
            board._cards.Add(
                GoalPositions[i],
                new(CardCatalog.GoalCard(goals[i]), Goal: goals[i], IsRevealed: false));
        }

        return board;
    }

    public PlacementValidation ValidatePlacement(BoardPosition position, CardDefinition card)
    {
        if (card.Kind != CardKind.Path)
        {
            return PlacementValidation.Invalid("Only path cards can be placed on the board.");
        }

        if (_cards.ContainsKey(position))
        {
            return PlacementValidation.Invalid("The board position is occupied.");
        }

        bool hasNeighbor = false;
        bool connectsToReachablePath = false;
        HashSet<BoardPosition> reachable = FindReachableFromStart();

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            BoardPosition neighborPosition = position.Move(direction);
            if (!_cards.TryGetValue(neighborPosition, out PlacedCard? neighbor))
            {
                continue;
            }

            hasNeighbor = true;
            if (neighbor.IsGoal && !neighbor.IsRevealed)
            {
                continue;
            }

            bool edgeOpen = card.Edges.IsOpen(direction);
            bool neighborOpen = neighbor.Definition.Edges.IsOpen(direction.Opposite());
            if (edgeOpen != neighborOpen)
            {
                return PlacementValidation.Invalid("A path edge does not match its neighbor.");
            }

            if (edgeOpen && reachable.Contains(neighborPosition))
            {
                connectsToReachablePath = true;
            }
        }

        if (!hasNeighbor)
        {
            return PlacementValidation.Invalid("A path card must touch another card.");
        }

        return connectsToReachablePath
            ? PlacementValidation.Valid
            : PlacementValidation.Invalid("The path is not connected to the start card.");
    }

    public void Place(BoardPosition position, CardDefinition card)
    {
        PlacementValidation validation = ValidatePlacement(position, card);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Error);
        }

        _cards.Add(position, new(card));
    }

    public bool Remove(BoardPosition position)
    {
        if (!_cards.TryGetValue(position, out PlacedCard? card) || card.IsStart || card.IsGoal)
        {
            return false;
        }

        return _cards.Remove(position);
    }

    public bool RevealGoal(BoardPosition position)
    {
        if (!_cards.TryGetValue(position, out PlacedCard? goal) || !goal.IsGoal || goal.IsRevealed)
        {
            return false;
        }

        _cards[position] = goal with { IsRevealed = true };
        return true;
    }

    public IReadOnlyList<BoardPosition> FindConnectedHiddenGoals()
    {
        HashSet<BoardPosition> reachable = FindReachableFromStart();
        List<BoardPosition> connected = [];

        foreach (BoardPosition goalPosition in GoalPositions)
        {
            if (!_cards.TryGetValue(goalPosition, out PlacedCard? goal) || goal.IsRevealed)
            {
                continue;
            }

            if (Enum.GetValues<Direction>().Any(direction =>
                reachable.Contains(goalPosition.Move(direction)) &&
                _cards[goalPosition.Move(direction)].Definition.Edges.IsOpen(direction.Opposite())))
            {
                connected.Add(goalPosition);
            }
        }

        return connected;
    }

    public HashSet<BoardPosition> FindReachableFromStart()
    {
        HashSet<BoardPosition> visited = [];
        if (!_cards.TryGetValue(StartPosition, out PlacedCard? start) || !start.IsRevealed)
        {
            return visited;
        }

        Queue<BoardPosition> queue = new();
        visited.Add(StartPosition);
        queue.Enqueue(StartPosition);

        while (queue.TryDequeue(out BoardPosition currentPosition))
        {
            PlacedCard current = _cards[currentPosition];
            foreach (Direction direction in Enum.GetValues<Direction>())
            {
                if (!current.Definition.Edges.IsOpen(direction))
                {
                    continue;
                }

                BoardPosition nextPosition = currentPosition.Move(direction);
                if (visited.Contains(nextPosition) ||
                    !_cards.TryGetValue(nextPosition, out PlacedCard? next) ||
                    !next.IsRevealed ||
                    !next.Definition.Edges.IsOpen(direction.Opposite()))
                {
                    continue;
                }

                visited.Add(nextPosition);
                queue.Enqueue(nextPosition);
            }
        }

        return visited;
    }
}
