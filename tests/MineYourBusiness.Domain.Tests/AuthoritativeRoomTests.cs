using System.Text.Json;
using MineYourBusiness.Application;
using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class AuthoritativeRoomTests
{
    [Fact]
    public void PlayerSnapshotRoundTripsThroughTheNetworkSerializer()
    {
        RoomFixture fixture = RoomFixture.Started();
        PlayerSnapshot snapshot = fixture.Room.SnapshotForConnection("beto");
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        string json = JsonSerializer.Serialize(snapshot, options);
        PlayerSnapshot? restored = JsonSerializer.Deserialize<PlayerSnapshot>(json, options);

        Assert.NotNull(restored);
        Assert.Equal(snapshot.Private.PlayerId, restored.Private.PlayerId);
        Assert.Equal(snapshot.Private.Hand, restored.Private.Hand);
        Assert.Equal(snapshot.Public.Board, restored.Public.Board);
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
    public void SanitizedClientsCanCompleteAThreeRoundMatch(int playerCount)
    {
        AuthoritativeRoom room = new("c1", "Player 1", 99, "FULL99");
        Dictionary<PlayerId, string> connections = [];
        connections.Add(room.SnapshotForConnection("c1").Private.PlayerId, "c1");
        for (int index = 2; index <= playerCount; index++)
        {
            string connection = $"c{index}";
            JoinRoomResult joined = room.Join(connection, "FULL99", $"Player {index}");
            Assert.True(joined.Accepted, joined.Error);
            connections.Add(joined.Snapshot!.Private.PlayerId, connection);
            Assert.True(room.SetReady(connection, true, out string? readyError), readyError);
        }

        Assert.True(room.Start("c1", out string? startError), startError);
        int safety = 1000;
        while (room.Phase != RoomPhase.Finished && safety-- > 0)
        {
            PlayerSnapshot any = room.SnapshotForConnection("c1");
            PlayerId actor = any.Public.CurrentPlayerId!.Value;
            string connection = connections[actor];
            PlayerSnapshot actorSnapshot = room.SnapshotForConnection(connection);
            GameCommand command = actorSnapshot.Private.Hand.Count == 0
                ? new PassCommand(actor)
                : new DiscardCommand(actor, actorSnapshot.Private.Hand[0].Id);
            CommandReceipt receipt = room.Submit(connection, new(
                room.MatchId,
                actor,
                actorSnapshot.Public.TurnNumber,
                $"command-{safety}",
                actorSnapshot.Public.Revision,
                command));
            Assert.True(receipt.Accepted, receipt.Error);
        }

        Assert.True(safety > 0, "Online simulation exceeded its safety limit.");
        Assert.Equal(RoomPhase.Finished, room.Phase);
        Assert.All(connections.Values, connection =>
        {
            PlayerSnapshot final = room.SnapshotForConnection(connection);
            Assert.NotNull(final.Private.Gold);
            Assert.All(final.Public.Players, player =>
            {
                Assert.NotNull(player.RevealedRole);
                Assert.NotNull(player.RevealedGold);
            });
        });
    }

    [Fact]
    public void EachSnapshotContainsOnlyItsRecipientsSecrets()
    {
        RoomFixture fixture = RoomFixture.Started();

        PlayerSnapshot ana = fixture.Room.SnapshotForConnection("host");
        PlayerSnapshot beto = fixture.Room.SnapshotForConnection("beto");

        Assert.NotNull(ana.Private.Role);
        Assert.NotNull(beto.Private.Role);
        Assert.NotEqual(ana.Private.PlayerId, beto.Private.PlayerId);
        Assert.All(ana.Public.Players, player => Assert.Null(player.RevealedRole));
        Assert.All(beto.Public.Players, player => Assert.Null(player.RevealedRole));
        Assert.Null(ana.Private.Gold);
        Assert.Null(beto.Private.Gold);
        Assert.All(ana.Public.Board.Where(card => card.IsGoal && !card.IsRevealed), card =>
        {
            Assert.Equal(new CardId("goal-hidden"), card.CardId);
            Assert.Equal(GoalContent.None, card.RevealedGoal);
            Assert.Equal(EdgeMask.None, card.Edges);
        });
    }

    [Fact]
    public void DuplicateCommandReturnsOriginalReceiptWithoutApplyingAgain()
    {
        RoomFixture fixture = RoomFixture.Started();
        (string connection, PlayerSnapshot snapshot) = fixture.CurrentPlayer();
        CardDefinition card = snapshot.Private.Hand[0];
        CommandEnvelope envelope = new(
            fixture.Room.MatchId,
            snapshot.Private.PlayerId,
            snapshot.Public.TurnNumber,
            "command-1",
            snapshot.Public.Revision,
            new DiscardCommand(snapshot.Private.PlayerId, card.Id));

        CommandReceipt first = fixture.Room.Submit(connection, envelope);
        CommandReceipt duplicate = fixture.Room.Submit(connection, envelope);

        Assert.True(first.Accepted);
        Assert.False(first.IsDuplicate);
        Assert.True(duplicate.Accepted);
        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(first.Revision, duplicate.Revision);
        Assert.Equal(first.Events, duplicate.Events);
        Assert.Equal(first.Revision, fixture.Room.Revision);
    }

    [Fact]
    public void StaleRevisionAndWrongSeatAreRejectedWithoutMutation()
    {
        RoomFixture fixture = RoomFixture.Started();
        (string connection, PlayerSnapshot snapshot) = fixture.CurrentPlayer();
        CardDefinition card = snapshot.Private.Hand[0];
        long revision = fixture.Room.Revision;

        CommandReceipt stale = fixture.Room.Submit(connection, new(
            fixture.Room.MatchId,
            snapshot.Private.PlayerId,
            snapshot.Public.TurnNumber,
            "stale",
            revision - 1,
            new DiscardCommand(snapshot.Private.PlayerId, card.Id)));
        string otherConnection = fixture.Connections.First(item => item != connection);
        CommandReceipt impersonated = fixture.Room.Submit(otherConnection, new(
            fixture.Room.MatchId,
            snapshot.Private.PlayerId,
            snapshot.Public.TurnNumber,
            "impersonated",
            revision,
            new DiscardCommand(snapshot.Private.PlayerId, card.Id)));

        Assert.False(stale.Accepted);
        Assert.False(impersonated.Accepted);
        Assert.Equal(revision, fixture.Room.Revision);
    }

    [Fact]
    public void ReconnectRestoresTheSamePrivateSeatAndResumesTheMatch()
    {
        RoomFixture fixture = RoomFixture.Started();
        PlayerSnapshot before = fixture.Room.SnapshotForConnection("beto");

        fixture.Room.Disconnect("beto");
        Assert.Equal(RoomPhase.Paused, fixture.Room.Phase);
        JoinRoomResult joined = fixture.Room.Join("beto-new", fixture.Room.RoomCode, "Nome ignorado", fixture.BetoToken);
        PlayerSnapshot after = fixture.Room.SnapshotForConnection("beto-new");

        Assert.True(joined.Accepted, joined.Error);
        Assert.Equal(RoomPhase.Playing, fixture.Room.Phase);
        Assert.Equal(before.Private.PlayerId, after.Private.PlayerId);
        Assert.Equal(before.Private.Role, after.Private.Role);
        Assert.Equal(before.Private.Hand, after.Private.Hand);
    }

    [Fact]
    public void ExpiredReconnectWindowAbortsTheMatch()
    {
        ManualTimeProvider clock = new(new DateTimeOffset(2026, 8, 28, 20, 0, 0, TimeSpan.Zero));
        RoomFixture fixture = RoomFixture.Started(clock, TimeSpan.FromSeconds(10));

        fixture.Room.Disconnect("caio");
        clock.Advance(TimeSpan.FromSeconds(11));
        fixture.Room.Tick();

        Assert.Equal(RoomPhase.Aborted, fixture.Room.Phase);
        Assert.Contains("expirou", fixture.Room.StatusMessage);
    }

    private sealed class RoomFixture
    {
        private RoomFixture(AuthoritativeRoom room, string betoToken, string caioToken)
        {
            Room = room;
            BetoToken = betoToken;
            CaioToken = caioToken;
        }

        public AuthoritativeRoom Room { get; }
        public string BetoToken { get; }
        public string CaioToken { get; }
        public string[] Connections { get; } = ["host", "beto", "caio"];

        public static RoomFixture Started(
            TimeProvider? timeProvider = null,
            TimeSpan? reconnectWindow = null)
        {
            AuthoritativeRoom room = new(
                "host",
                "Ana",
                20260828,
                "MINERA",
                reconnectWindow,
                timeProvider);
            JoinRoomResult beto = room.Join("beto", "minera", "Beto");
            JoinRoomResult caio = room.Join("caio", "MINERA", "Caio");
            Assert.True(beto.Accepted, beto.Error);
            Assert.True(caio.Accepted, caio.Error);
            Assert.True(room.SetReady("beto", true, out string? betoReadyError), betoReadyError);
            Assert.True(room.SetReady("caio", true, out string? caioReadyError), caioReadyError);
            Assert.True(room.Start("host", out string? startError), startError);
            return new(room, beto.ReconnectToken!, caio.ReconnectToken!);
        }

        public (string Connection, PlayerSnapshot Snapshot) CurrentPlayer()
        {
            foreach (string connection in Connections)
            {
                PlayerSnapshot snapshot = Room.SnapshotForConnection(connection);
                if (snapshot.Public.CurrentPlayerId == snapshot.Private.PlayerId)
                {
                    return (connection, snapshot);
                }
            }

            throw new InvalidOperationException("Current player was not found.");
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
