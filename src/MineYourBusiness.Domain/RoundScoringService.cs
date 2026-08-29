namespace MineYourBusiness.Domain;

public static class RoundScoringService
{
    public static IReadOnlyDictionary<PlayerId, int> Calculate(
        IReadOnlyList<PlayerState> players,
        bool goldReached)
    {
        Dictionary<PlayerId, int> awards = [];
        int eliminationBonus = players.Count(player => player.IsEliminated);
        PlayerState[] activeSaboteurs = players
            .Where(player => player.Role == PlayerRole.Saboteur && !player.IsEliminated)
            .ToArray();
        if (goldReached)
        {
            foreach (PlayerState miner in players.Where(player => player.Role == PlayerRole.Miner && !player.IsEliminated))
            {
                awards[miner.Id] = 1;
            }

            AwardEliminationBonus(awards, activeSaboteurs, eliminationBonus);
            return awards;
        }

        int amount = activeSaboteurs.Length switch
        {
            0 => 0,
            1 => 4,
            2 or 3 => 3,
            4 => 2,
            _ => throw new InvalidOperationException("Unexpected saboteur count."),
        };

        foreach (PlayerState saboteur in activeSaboteurs)
        {
            awards[saboteur.Id] = amount + eliminationBonus;
        }

        return awards;
    }

    private static void AwardEliminationBonus(
        Dictionary<PlayerId, int> awards,
        IEnumerable<PlayerState> saboteurs,
        int eliminationBonus)
    {
        if (eliminationBonus == 0)
        {
            return;
        }

        foreach (PlayerState saboteur in saboteurs)
        {
            awards[saboteur.Id] = awards.GetValueOrDefault(saboteur.Id) + eliminationBonus;
        }
    }
}
