using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class SetupAndScoringTests
{
    public static TheoryData<int, int, int> RoleCompositions => new()
    {
        { 3, 1, 3 },
        { 4, 1, 4 },
        { 5, 2, 4 },
        { 6, 2, 5 },
        { 7, 3, 5 },
        { 8, 3, 6 },
        { 9, 3, 7 },
        { 10, 4, 7 },
    };

    [Theory]
    [MemberData(nameof(RoleCompositions))]
    public void RoleDeckHasOneExtraCard(int players, int saboteurs, int miners)
    {
        IReadOnlyList<PlayerRole> roles = RoundSetup.CreateRoleDeck(players);

        Assert.Equal(players + 1, roles.Count);
        Assert.Equal(saboteurs, roles.Count(role => role == PlayerRole.Saboteur));
        Assert.Equal(miners, roles.Count(role => role == PlayerRole.Miner));
    }

    [Theory]
    [InlineData(3, 6)]
    [InlineData(5, 6)]
    [InlineData(6, 5)]
    [InlineData(7, 5)]
    [InlineData(8, 4)]
    [InlineData(10, 4)]
    public void InitialHandSizeFollowsPlayerCount(int players, int expected)
    {
        Assert.Equal(expected, RoundSetup.InitialHandSize(players));
    }

    [Fact]
    public void SeededShuffleIsReproducible()
    {
        PlayerRole[] first = RoundSetup.DealRoles(8, new(42)).ToArray();
        PlayerRole[] second = RoundSetup.DealRoles(8, new(42)).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void DrawDeckContainsFortyPathsAndTwentySevenActions()
    {
        IReadOnlyList<CardDefinition> deck = CardCatalog.CreateDrawDeck();

        Assert.Equal(67, deck.Count);
        Assert.Equal(40, deck.Count(card => card.Kind == CardKind.Path));
        Assert.Equal(27, deck.Count(card => card.Kind != CardKind.Path));
        Assert.Equal(deck.Count, deck.Select(card => card.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 4)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    public void SaboteurRewardMatchesTeamSize(int saboteurCount, int expected)
    {
        List<PlayerState> players = [];
        for (int i = 0; i < saboteurCount; i++)
        {
            players.Add(new(new($"s{i}"), $"S{i}") { Role = PlayerRole.Saboteur });
        }

        players.Add(new(new("m"), "Miner") { Role = PlayerRole.Miner });

        IReadOnlyDictionary<PlayerId, int> awards = RoundScoringService.Calculate(players, false);

        Assert.Equal(saboteurCount, awards.Count);
        Assert.All(awards.Values, amount => Assert.Equal(expected, amount));
    }

    [Fact]
    public void OnlyMinersScoreWhenGoldIsReached()
    {
        PlayerState miner = new(new("m"), "Miner") { Role = PlayerRole.Miner };
        PlayerState saboteur = new(new("s"), "Saboteur") { Role = PlayerRole.Saboteur };

        IReadOnlyDictionary<PlayerId, int> awards =
            RoundScoringService.Calculate([miner, saboteur], true);

        Assert.Equal(1, awards[miner.Id]);
        Assert.DoesNotContain(saboteur.Id, awards.Keys);
    }

    [Fact]
    public void EliminationBonusIsAwardedAtRoundScoringEvenWhenGoldIsReached()
    {
        PlayerState activeMiner = new(new("m1"), "Active miner") { Role = PlayerRole.Miner };
        PlayerState eliminatedMiner = new(new("m2"), "Eliminated miner")
        {
            Role = PlayerRole.Miner,
            IsEliminated = true,
        };
        PlayerState saboteur = new(new("s"), "Saboteur") { Role = PlayerRole.Saboteur };

        IReadOnlyDictionary<PlayerId, int> awards =
            RoundScoringService.Calculate([activeMiner, eliminatedMiner, saboteur], true);

        Assert.Equal(1, awards[activeMiner.Id]);
        Assert.Equal(1, awards[saboteur.Id]);
        Assert.DoesNotContain(eliminatedMiner.Id, awards.Keys);
    }
}
