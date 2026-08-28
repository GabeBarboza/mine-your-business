using MineYourBusiness.Application;
using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class LocalMatchControllerTests
{
    [Fact]
    public void AdministrativeControlsCompleteAThreeRoundMatch()
    {
        LocalMatchController controller = new(["Ana", "Beto", "Caio"], 20260828);

        IReadOnlyList<GameEvent> events = controller.AdvanceToMatchEnd();

        Assert.True(controller.State.IsFinished);
        Assert.Equal(3, events.Count(item => item is RoundEnded));
        Assert.Equal(9, events.Count(item => item is RoleRevealed));
        Assert.Single(events, item => item is MatchEnded);
    }

    [Fact]
    public void AdvanceToNextRoundStopsAtTheFollowingSetup()
    {
        LocalMatchController controller = new(["Ana", "Beto", "Caio"], 91);

        IReadOnlyList<GameEvent> events = controller.AdvanceToNextRound();

        Assert.Equal(2, controller.State.RoundNumber);
        Assert.Contains(events, item => item is RoundEnded { RoundNumber: 1 });
        Assert.Contains(events, item => item is RoundStarted { RoundNumber: 2 });
    }

    [Fact]
    public void RoundEndPublishesRolesBeforeTheyAreReplaced()
    {
        LocalMatchController controller = new(["Ana", "Beto", "Caio"], 17);
        Dictionary<PlayerId, PlayerRole> originalRoles = controller.State.Players
            .ToDictionary(player => player.Id, player => player.Role);

        IReadOnlyList<GameEvent> events = controller.AdvanceToNextRound();
        Dictionary<PlayerId, PlayerRole> revealed = events
            .OfType<RoleRevealed>()
            .ToDictionary(item => item.PlayerId, item => item.Role);

        Assert.Equal(originalRoles, revealed);
    }
}
