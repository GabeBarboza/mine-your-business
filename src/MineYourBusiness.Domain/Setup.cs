namespace MineYourBusiness.Domain;

public sealed class SeededRandom
{
    private ulong _state;

    public SeededRandom(long seed)
    {
        _state = unchecked((ulong)seed) + 0x9E3779B97F4A7C15UL;
        if (_state == 0)
        {
            _state = 0xA0761D6478BD642FUL;
        }
    }

    public int Next(int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        ulong value = _state * 0x2545F4914F6CDD1DUL;
        return (int)(value % (uint)exclusiveMaximum);
    }

    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int target = Next(i + 1);
            (items[i], items[target]) = (items[target], items[i]);
        }
    }
}

public enum PlayerRole
{
    Miner,
    Saboteur,
}

public static class RoundSetup
{
    private static readonly Dictionary<int, (int Saboteurs, int Miners)> RoleCounts =
        new Dictionary<int, (int, int)>
        {
            [3] = (1, 3),
            [4] = (1, 4),
            [5] = (2, 4),
            [6] = (2, 5),
            [7] = (3, 5),
            [8] = (3, 6),
            [9] = (3, 7),
            [10] = (4, 7),
        };

    public static int InitialHandSize(int playerCount) => playerCount switch
    {
        >= 3 and <= 5 => 6,
        >= 6 and <= 7 => 5,
        >= 8 and <= 10 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(playerCount), "A match requires 3 to 10 players."),
    };

    public static IReadOnlyList<PlayerRole> DealRoles(int playerCount, SeededRandom random)
    {
        List<PlayerRole> deck = [.. CreateRoleDeck(playerCount)];
        random.Shuffle(deck);
        return deck.Take(playerCount).ToArray();
    }

    public static IReadOnlyList<PlayerRole> CreateRoleDeck(int playerCount)
    {
        if (!RoleCounts.TryGetValue(playerCount, out (int Saboteurs, int Miners) counts))
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), "A match requires 3 to 10 players.");
        }

        return
        [
            .. Enumerable.Repeat(PlayerRole.Saboteur, counts.Saboteurs),
            .. Enumerable.Repeat(PlayerRole.Miner, counts.Miners),
        ];
    }

    public static IReadOnlyList<GoalContent> ShuffleGoals(SeededRandom random)
    {
        List<GoalContent> goals = [GoalContent.Gold, GoalContent.Stone, GoalContent.Stone];
        random.Shuffle(goals);
        return goals;
    }
}
