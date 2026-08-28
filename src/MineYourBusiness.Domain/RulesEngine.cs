namespace MineYourBusiness.Domain;

public static class RulesEngine
{
    public static CommandResult Handle(GameState state, GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (state.Phase != MatchPhase.AwaitingAction)
        {
            return CommandResult.Rejected("The match is not awaiting an action.");
        }

        if (state.CurrentPlayer.Id != command.PlayerId)
        {
            return CommandResult.Rejected("The command was sent outside the player's turn.");
        }

        List<GameEvent> events = [];
        string? error = command switch
        {
            PlayPathCommand play => PlayPath(state, play, events),
            BreakToolCommand broken => BreakTool(state, broken, events),
            RepairToolCommand repair => RepairTool(state, repair, events),
            CollapsePathCommand collapse => CollapsePath(state, collapse, events),
            InspectGoalCommand inspect => InspectGoal(state, inspect, events),
            DiscardCommand discard => Discard(state, discard, events),
            PassCommand pass => Pass(state, pass, events),
            _ => "Unknown command.",
        };

        if (error is not null)
        {
            return CommandResult.Rejected(error);
        }

        FinishTurn(state, events);
        return CommandResult.Success(events);
    }

    private static string? PlayPath(GameState state, PlayPathCommand command, List<GameEvent> events)
    {
        PlayerState player = state.CurrentPlayer;
        if (!player.CanPlayPath)
        {
            return "A player with a broken tool cannot play path cards.";
        }

        CardDefinition? card = FindCard(player, command.CardId);
        if (card is null || card.Kind != CardKind.Path)
        {
            return "The path card is not in the player's hand.";
        }

        PlacementValidation validation = state.Board.ValidatePlacement(command.Position, card);
        if (!validation.IsValid)
        {
            return validation.Error;
        }

        state.Phase = MatchPhase.ResolvingAction;
        player.Hand.Remove(card);
        state.Board.Place(command.Position, card);
        state.LastPathPlayerId = player.Id;
        events.Add(new CardPlayed(player.Id, card.Id));
        events.Add(new PathPlaced(player.Id, card.Id, command.Position));

        foreach (BoardPosition position in state.Board.FindConnectedHiddenGoals())
        {
            PlacedCard goal = state.Board.Cards[position];
            state.Board.RevealGoal(position);
            events.Add(new GoalRevealed(position, goal.Goal));
            if (goal.Goal == GoalContent.Gold)
            {
                state.GoldWasReached = true;
            }
        }

        return null;
    }

    private static string? BreakTool(GameState state, BreakToolCommand command, List<GameEvent> events)
    {
        PlayerState actor = state.CurrentPlayer;
        CardDefinition? card = FindCard(actor, command.CardId);
        if (card is null || card.Kind != CardKind.BreakTool)
        {
            return "The break-tool card is not in the player's hand.";
        }

        PlayerState? target = FindPlayer(state, command.TargetPlayerId);
        if (target is null)
        {
            return "The target player does not exist.";
        }

        if ((target.BrokenTools & card.Tools) != ToolType.None)
        {
            return "The target already has that tool broken.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        actor.Hand.Remove(card);
        state.Discard(card);
        target.BrokenTools |= card.Tools;
        events.Add(new CardPlayed(actor.Id, card.Id));
        events.Add(new ToolBroken(actor.Id, target.Id, card.Tools));
        return null;
    }

    private static string? RepairTool(GameState state, RepairToolCommand command, List<GameEvent> events)
    {
        PlayerState actor = state.CurrentPlayer;
        CardDefinition? card = FindCard(actor, command.CardId);
        if (card is null || card.Kind != CardKind.RepairTool)
        {
            return "The repair card is not in the player's hand.";
        }

        if (!IsSingleTool(command.Tool) || (card.Tools & command.Tool) == ToolType.None)
        {
            return "The repair card cannot repair the selected tool.";
        }

        PlayerState? target = FindPlayer(state, command.TargetPlayerId);
        if (target is null)
        {
            return "The target player does not exist.";
        }

        if ((target.BrokenTools & command.Tool) == ToolType.None)
        {
            return "The selected tool is not broken.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        actor.Hand.Remove(card);
        state.Discard(card);
        target.BrokenTools &= ~command.Tool;
        events.Add(new CardPlayed(actor.Id, card.Id));
        events.Add(new ToolRepaired(actor.Id, target.Id, command.Tool));
        return null;
    }

    private static string? CollapsePath(GameState state, CollapsePathCommand command, List<GameEvent> events)
    {
        PlayerState player = state.CurrentPlayer;
        CardDefinition? card = FindCard(player, command.CardId);
        if (card is null || card.Kind != CardKind.Collapse)
        {
            return "The collapse card is not in the player's hand.";
        }

        if (!state.Board.Cards.TryGetValue(command.Position, out PlacedCard? target) ||
            target.IsStart || target.IsGoal)
        {
            return "Only a common path card can be collapsed.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        player.Hand.Remove(card);
        state.Discard(card);
        state.Board.Remove(command.Position);
        events.Add(new CardPlayed(player.Id, card.Id));
        events.Add(new PathCollapsed(player.Id, command.Position));
        return null;
    }

    private static string? InspectGoal(GameState state, InspectGoalCommand command, List<GameEvent> events)
    {
        PlayerState player = state.CurrentPlayer;
        CardDefinition? card = FindCard(player, command.CardId);
        if (card is null || card.Kind != CardKind.Map)
        {
            return "The map card is not in the player's hand.";
        }

        if (!state.Board.Cards.TryGetValue(command.Position, out PlacedCard? goal) ||
            !goal.IsGoal || goal.IsRevealed)
        {
            return "A map can inspect only a hidden goal.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        player.Hand.Remove(card);
        state.Discard(card);
        player.InspectedGoals[command.Position] = goal.Goal;
        events.Add(new CardPlayed(player.Id, card.Id));
        events.Add(new GoalInspected(player.Id, command.Position, goal.Goal));
        return null;
    }

    private static string? Discard(GameState state, DiscardCommand command, List<GameEvent> events)
    {
        PlayerState player = state.CurrentPlayer;
        CardDefinition? card = FindCard(player, command.CardId);
        if (card is null)
        {
            return "The discarded card is not in the player's hand.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        player.Hand.Remove(card);
        state.Discard(card);
        events.Add(new CardDiscarded(player.Id));
        return null;
    }

    private static string? Pass(GameState state, PassCommand command, List<GameEvent> events)
    {
        if (state.CurrentPlayer.Hand.Count != 0)
        {
            return "A player with cards must discard one to pass.";
        }

        state.Phase = MatchPhase.ResolvingAction;
        events.Add(new PlayerPassed(command.PlayerId));
        return null;
    }

    private static void FinishTurn(GameState state, List<GameEvent> events)
    {
        state.Phase = MatchPhase.CheckingRoundEnd;
        bool allCardsExhausted = state.DrawPile.Count == 0 && state.Players.All(player => player.Hand.Count == 0);
        if (state.GoldWasReached || allCardsExhausted)
        {
            FinishRound(state, events);
            return;
        }

        state.Phase = MatchPhase.DrawingCard;
        CardDefinition? drawn = state.DrawTo(state.CurrentPlayer);
        if (drawn is not null)
        {
            events.Add(new CardDrawn(state.CurrentPlayer.Id, drawn.Id));
        }

        state.CurrentPlayerIndex = (state.CurrentPlayerIndex + 1) % state.Players.Count;
        state.TurnNumber++;
        state.Phase = MatchPhase.AwaitingAction;
        events.Add(new TurnAdvanced(state.TurnNumber, state.CurrentPlayer.Id));
    }

    private static void FinishRound(GameState state, List<GameEvent> events)
    {
        IReadOnlyDictionary<PlayerId, int> awards =
            RoundScoringService.Calculate(state.Players, state.GoldWasReached);
        foreach ((PlayerId playerId, int amount) in awards)
        {
            PlayerState player = state.Players.Single(candidate => candidate.Id == playerId);
            player.Gold += amount;
            events.Add(new GoldAwarded(playerId, amount));
        }

        state.Phase = MatchPhase.RoundSummary;
        events.Add(new RoundEnded(state.RoundNumber, state.GoldWasReached));

        int nextStarter = state.LastPathPlayerId is PlayerId lastPath
            ? (IndexOf(state, lastPath) + 1) % state.Players.Count
            : (state.CurrentPlayerIndex + 1) % state.Players.Count;

        if (state.RoundNumber >= 3)
        {
            state.Phase = MatchPhase.MatchSummary;
            int highestGold = state.Players.Max(player => player.Gold);
            PlayerId[] winners = state.Players
                .Where(player => player.Gold == highestGold)
                .Select(player => player.Id)
                .ToArray();
            events.Add(new MatchEnded(winners));
            return;
        }

        state.StartingPlayerIndex = nextStarter;
        state.BeginRound();
        events.Add(new RoundStarted(state.RoundNumber, state.CurrentPlayer.Id));
    }

    private static CardDefinition? FindCard(PlayerState player, CardId id) =>
        player.Hand.FirstOrDefault(card => card.Id == id);

    private static PlayerState? FindPlayer(GameState state, PlayerId id) =>
        state.Players.FirstOrDefault(player => player.Id == id);

    private static int IndexOf(GameState state, PlayerId id)
    {
        for (int i = 0; i < state.Players.Count; i++)
        {
            if (state.Players[i].Id == id)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Player not found.");
    }

    private static bool IsSingleTool(ToolType tool) =>
        tool is ToolType.Lamp or ToolType.Cart or ToolType.Pickaxe;
}
