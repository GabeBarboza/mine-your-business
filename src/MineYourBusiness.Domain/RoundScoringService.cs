namespace MineYourBusiness.Domain;

public static class RoundScoringService
{
    public static IReadOnlyDictionary<PlayerId, int> Calculate(
        IReadOnlyList<PlayerState> players,
        bool goldReached)
    {
        Dictionary<PlayerId, int> awards = [];
        if (goldReached)
        {
            foreach (PlayerState miner in players.Where(player => player.Role == PlayerRole.Miner))
            {
                awards[miner.Id] = 1;
            }

            return awards;
        }

        PlayerState[] saboteurs = players.Where(player => player.Role == PlayerRole.Saboteur).ToArray();
        int amount = saboteurs.Length switch
        {
            0 => 0,
            1 => 4,
            2 or 3 => 3,
            4 => 2,
            _ => throw new InvalidOperationException("Unexpected saboteur count."),
        };

        foreach (PlayerState saboteur in saboteurs)
        {
            awards[saboteur.Id] = amount;
        }

        return awards;
    }
}
