using MineYourBusiness.Domain;

int playerCount = ReadArgument("--players", 3);
long seed = ReadArgument("--seed", 20260828);

if (playerCount is < 3 or > 10)
{
    Console.Error.WriteLine("--players must be between 3 and 10.");
    return 2;
}

(PlayerId Id, string Name)[] players = Enumerable.Range(1, playerCount)
    .Select(index => (new PlayerId($"p{index}"), $"Player {index}"))
    .ToArray();
GameState game = GameFactory.Create(players, seed);

Console.WriteLine($"MINE YOUR BUSINESS | seed {seed} | {playerCount} players");
Console.WriteLine($"Round {game.RoundNumber} started with {game.CurrentPlayer.Name}.");

while (!game.IsFinished)
{
    PlayerState player = game.CurrentPlayer;
    GameCommand command = player.Hand.Count > 0
        ? new DiscardCommand(player.Id, player.Hand[0].Id)
        : new PassCommand(player.Id);
    CommandResult result = RulesEngine.Handle(game, command);

    if (!result.Accepted)
    {
        Console.Error.WriteLine($"Simulation stopped: {result.Error}");
        return 1;
    }

    foreach (GameEvent gameEvent in result.Events)
    {
        switch (gameEvent)
        {
            case RoundEnded round:
                Console.WriteLine($"Round {round.RoundNumber} ended. Gold reached: {round.GoldReached}.");
                break;
            case RoundStarted round:
                Console.WriteLine($"Round {round.RoundNumber} started with {round.StartingPlayerId}.");
                break;
            case GoldAwarded award:
                Console.WriteLine($"  {award.PlayerId} received {award.Amount} gold.");
                break;
            case MatchEnded match:
                Console.WriteLine($"Winners: {string.Join(", ", match.Winners)}");
                break;
        }
    }
}

Console.WriteLine("Final score:");
foreach (PlayerState player in game.Players.OrderByDescending(player => player.Gold))
{
    Console.WriteLine($"  {player.Name}: {player.Gold}");
}

return 0;

static int ReadArgument(string name, int fallback)
{
    int index = Array.IndexOf(Environment.GetCommandLineArgs(), name);
    return index >= 0 && index + 1 < Environment.GetCommandLineArgs().Length &&
        int.TryParse(Environment.GetCommandLineArgs()[index + 1], out int value)
        ? value
        : fallback;
}
