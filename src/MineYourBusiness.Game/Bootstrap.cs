using Godot;
using MineYourBusiness.Application;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>Composition root for local debug play and private ENet rooms.</summary>
public partial class Bootstrap : Control
{
    private readonly Color _ink = new("e9edf2");
    private readonly Color _muted = new("9daaba");
    private readonly Color _accent = new("e2b84b");
    private LocalMatchController? _match;
    private BoardView? _board;
    private VBoxContainer? _players;
    private HBoxContainer? _hand;
    private Label? _turnLabel;
    private Label? _status;
    private Label? _selectionHelp;
    private OptionButton? _targetPicker;
    private OptionButton? _toolPicker;
    private Button? _playTargetButton;
    private Button? _discardButton;
    private VBoxContainer? _publicLog;
    private Control? _overlay;
    private CardDefinition? _selectedCard;
    private OnlineSessionNode? _online;
    private Label? _onlineStatus;

    public override void _Ready()
    {
        _online = new OnlineSessionNode { Name = "OnlineSession" };
        _online.SnapshotChanged += OnOnlineSnapshot;
        _online.StatusChanged += OnOnlineStatus;
        AddChild(_online);
        BuildStartScreen();
        GD.Print($"{ProjectMetadata.DisplayName} milestone 3 private multiplayer started.");
    }

    private void BuildStartScreen()
    {
        ClearScreen();
        AddChild(FullBackground(new Color("0c1118")));
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = Panel(new Color("151f2b"), 26);
        panel.CustomMinimumSize = new(520, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(13);
        panel.AddChild(content);
        content.AddChild(Heading(ProjectMetadata.DisplayName, 38, _accent));
        content.AddChild(LabelText(ProjectMetadata.Tagline, 18, _muted));
        content.AddChild(LabelText("MULTIPLAYER PRIVADO · MARCO 3", 13, new Color("67d5a4")));
        content.AddChild(Spacer(10));
        content.AddChild(LabelText("Três pessoas compartilham esta instância. A barreira de privacidade protege papel, mão e mapas entre turnos.", 15, _ink, true));

        LineEdit[] names = new LineEdit[3];
        string[] defaults = ["Ana", "Beto", "Caio"];
        for (int index = 0; index < names.Length; index++)
        {
            names[index] = new LineEdit { Text = defaults[index], PlaceholderText = $"Jogador {index + 1}" };
            content.AddChild(names[index]);
        }

        SpinBox seed = new() { MinValue = 1, MaxValue = 999999999, Value = 20260828, Prefix = "Seed  " };
        content.AddChild(seed);
        Button start = PrimaryButton("INICIAR PARTIDA LOCAL");
        start.Pressed += () => StartMatch(names.Select(field => field.Text), (long)seed.Value);
        content.AddChild(start);
        Button online = SecondaryButton("CRIAR OU ENTRAR EM SALA ONLINE");
        online.Pressed += BuildOnlineMenu;
        content.AddChild(online);
        content.AddChild(LabelText("Clique para escolher · roda para zoom · botão do meio para mover a mesa", 12, _muted, true));
    }

    private void BuildOnlineMenu()
    {
        ClearScreen();
        AddChild(FullBackground(new Color("0c1118")));
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = Panel(new Color("151f2b"), 26);
        panel.CustomMinimumSize = new(560, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(11);
        panel.AddChild(content);
        content.AddChild(Heading("SALA PRIVADA", 30, _accent));
        content.AddChild(LabelText("O anfitrião é autoritativo. Em redes diferentes, encaminhe a porta UDP escolhida no roteador do anfitrião.", 13, _muted, true));

        LineEdit name = new() { Text = "Ana", PlaceholderText = "Seu nome" };
        LineEdit address = new() { Text = "127.0.0.1", PlaceholderText = "Endereço do anfitrião" };
        LineEdit code = new() { PlaceholderText = "Código da sala" };
        SpinBox port = new() { MinValue = 1024, MaxValue = 65535, Value = 24828, Prefix = "Porta UDP  " };
        SpinBox seed = new() { MinValue = 1, MaxValue = 999999999, Value = 20260828, Prefix = "Seed  " };
        content.AddChild(name);
        content.AddChild(address);
        content.AddChild(code);
        content.AddChild(port);
        content.AddChild(seed);

        HBoxContainer actions = HBox(8);
        Button host = PrimaryButton("CRIAR SALA");
        host.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        host.Pressed += () =>
        {
            Error error = _online!.Host(name.Text, (int)port.Value, (long)seed.Value);
            if (error != Error.Ok)
            {
                OnOnlineStatus($"Não foi possível abrir a sala: {error}.", true);
            }
        };
        actions.AddChild(host);
        Button join = PrimaryButton("ENTRAR");
        join.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        join.Pressed += () =>
        {
            Error error = _online!.Join(address.Text, (int)port.Value, code.Text, name.Text);
            if (error != Error.Ok)
            {
                OnOnlineStatus($"Não foi possível iniciar a conexão: {error}.", true);
            }
        };
        actions.AddChild(join);
        content.AddChild(actions);
        Button back = SecondaryButton("VOLTAR");
        back.Pressed += BuildStartScreen;
        content.AddChild(back);
        _onlineStatus = LabelText("Informe os dados da sala.", 12, _muted, true);
        content.AddChild(_onlineStatus);
    }

    private void OnOnlineSnapshot(PlayerSnapshot snapshot)
    {
        if (snapshot.Public.RoomPhase == RoomPhase.Lobby)
        {
            BuildOnlineLobby(snapshot);
        }
        else
        {
            BuildOnlineMatch(snapshot);
        }
    }

    private void BuildOnlineLobby(PlayerSnapshot snapshot)
    {
        ClearScreen();
        AddChild(FullBackground(new Color("0c1118")));
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = Panel(new Color("151f2b"), 26);
        panel.CustomMinimumSize = new(600, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(11);
        panel.AddChild(content);
        content.AddChild(Heading("SALA " + (_online?.IsHost == true ? _online.Snapshot!.Public.MatchId[..6].ToUpperInvariant() : "PRIVADA"), 28, _accent));
        if (_online?.IsHost == true)
        {
            content.AddChild(LabelText("Código de convite: " + _online.RoomCode, 18, new Color("72dda9")));
        }

        content.AddChild(LabelText("Compartilhe endereço público, porta UDP e código apenas com as pessoas convidadas.", 12, _muted, true));
        foreach (RoomPlayerView player in snapshot.Public.Players)
        {
            string state = player.IsConnected ? (player.IsReady ? "PRONTO" : "AGUARDANDO") : "DESCONECTADO";
            content.AddChild(LabelText($"{(player.IsHost ? "★" : "•")} {player.Name} — {state}", 15, player.IsReady ? new Color("72dda9") : _ink));
        }

        RoomPlayerView me = snapshot.Public.Players.Single(player => player.Id == snapshot.Private.PlayerId);
        if (!me.IsHost)
        {
            Button ready = PrimaryButton(me.IsReady ? "CANCELAR PRONTIDÃO" : "ESTOU PRONTO");
            ready.Pressed += () => _online!.SetReady(!me.IsReady);
            content.AddChild(ready);
        }
        else
        {
            Button start = PrimaryButton("INICIAR PARTIDA");
            start.Disabled = snapshot.Public.Players.Count is < 3 or > 10 || snapshot.Public.Players.Any(player => !player.IsReady || !player.IsConnected);
            start.Pressed += () => _online!.StartMatch();
            content.AddChild(start);
        }

        Button reconnect = SecondaryButton("RECONECTAR");
        reconnect.Visible = !_online!.IsHost;
        reconnect.Pressed += () => _online.Reconnect();
        content.AddChild(reconnect);
        Button leave = SecondaryButton("SAIR DA SALA");
        leave.Pressed += () =>
        {
            _online.Close();
            BuildStartScreen();
        };
        content.AddChild(leave);
        _onlineStatus = LabelText(snapshot.Public.StatusMessage ?? "Aguardando participantes…", 12, _muted, true);
        content.AddChild(_onlineStatus);
    }

    private void BuildOnlineMatch(PlayerSnapshot snapshot)
    {
        SpinBox positionX = new() { MinValue = -99, MaxValue = 99, Value = 1, Prefix = "X " };
        SpinBox positionY = new() { MinValue = -99, MaxValue = 99, Value = 0, Prefix = "Y " };
        ClearScreen();
        AddChild(FullBackground(new Color("0b1118")));
        MarginContainer margin = new();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        AddChild(margin);
        HBoxContainer shell = HBox(14);
        margin.AddChild(shell);

        PanelContainer publicPanel = Panel(new Color("131d28"), 18);
        publicPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        VBoxContainer publicContent = VBox(9);
        publicPanel.AddChild(publicContent);
        publicContent.AddChild(Heading("PARTIDA ONLINE", 25, _accent));
        publicContent.AddChild(LabelText(
            snapshot.Public.RoomPhase == RoomPhase.Paused
                ? "PARTIDA PAUSADA PARA RECONEXÃO"
                : snapshot.Public.RoomPhase == RoomPhase.Aborted
                    ? "PARTIDA ENCERRADA"
                    : $"Rodada {snapshot.Public.RoundNumber}/3 · turno {snapshot.Public.TurnNumber}",
            14,
            snapshot.Public.RoomPhase == RoomPhase.Playing ? new Color("72dda9") : new Color("ef806f")));
        foreach (RoomPlayerView player in snapshot.Public.Players)
        {
            bool current = player.Id == snapshot.Public.CurrentPlayerId;
            string finalScore = player.RevealedGold is int gold
                ? $" · {gold} pepitas · {player.RevealedRole}"
                : string.Empty;
            publicContent.AddChild(LabelText($"{(current ? "▶" : "•")} {player.Name} · mão {player.CardCount} · {ToolLabel(player.BrokenTools)}{finalScore}{(player.IsConnected ? "" : " · OFFLINE")}", 14, current ? new Color("72dda9") : _ink, true));
        }

        BoardView onlineBoard = new()
        {
            CustomMinimumSize = new(620, 330),
            PositionSelected = position =>
            {
                positionX.Value = position.X;
                positionY.Value = position.Y;
            },
        };
        onlineBoard.Display(snapshot.Public.Board);
        publicContent.AddChild(onlineBoard);
        publicContent.AddChild(Spacer(8));
        publicContent.AddChild(LabelText($"Mesa: {snapshot.Public.Board.Count} cartas · compra {snapshot.Public.DrawPileCount} · descarte {snapshot.Public.DiscardPileCount}", 13, _muted));
        if (snapshot.Public.StatusMessage is not null)
        {
            publicContent.AddChild(LabelText(snapshot.Public.StatusMessage, 13, new Color("ef806f"), true));
        }

        Button reconnect = SecondaryButton("RECONECTAR");
        reconnect.Visible = !_online!.IsHost;
        reconnect.Pressed += () => _online.Reconnect();
        publicContent.AddChild(reconnect);
        shell.AddChild(publicPanel);

        PanelContainer privatePanel = Panel(new Color("192531"), 18);
        privatePanel.CustomMinimumSize = new(380, 0);
        VBoxContainer privateContent = VBox(9);
        privatePanel.AddChild(privateContent);
        privateContent.AddChild(Heading("SUAS INFORMAÇÕES", 18, _accent));
        privateContent.AddChild(LabelText("Papel: " + (snapshot.Private.Role?.ToString() ?? "aguardando"), 16, _ink));
        if (snapshot.Private.Gold is int privateGold)
        {
            privateContent.AddChild(LabelText($"Placar final: {privateGold} pepitas", 16, new Color("72dda9")));
        }
        foreach (GoalInspectionView goal in snapshot.Private.InspectedGoals)
        {
            privateContent.AddChild(LabelText($"Mapa ({goal.Position.X}, {goal.Position.Y}): {GoalName(goal.Content)}", 12, _muted));
        }

        bool myTurn = snapshot.Public.RoomPhase == RoomPhase.Playing && snapshot.Public.CurrentPlayerId == snapshot.Private.PlayerId;
        privateContent.AddChild(LabelText(myTurn ? "É A SUA VEZ" : "Aguardando sua vez", 14, myTurn ? new Color("72dda9") : _muted));
        HBoxContainer destination = HBox(5);
        OptionButton target = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (RoomPlayerView player in snapshot.Public.Players)
        {
            target.AddItem(player.Name);
        }

        OptionButton tool = new();
        foreach (ToolType item in new[] { ToolType.Lamp, ToolType.Cart, ToolType.Pickaxe })
        {
            tool.AddItem(ToolName(item));
            tool.SetItemMetadata(tool.ItemCount - 1, (int)item);
        }

        destination.AddChild(positionX);
        destination.AddChild(positionY);
        destination.AddChild(target);
        destination.AddChild(tool);
        privateContent.AddChild(destination);
        privateContent.AddChild(LabelText("X/Y para mesa · jogador/ferramenta para ações", 11, _muted));
        ScrollContainer handScroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        VBoxContainer hand = VBox(6);
        handScroll.AddChild(hand);
        privateContent.AddChild(handScroll);
        foreach (CardDefinition card in snapshot.Private.Hand)
        {
            HBoxContainer cardActions = HBox(5);
            Button play = SecondaryButton("JOGAR · " + CardName(card).Replace('\n', ' '));
            play.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            play.Disabled = !myTurn;
            play.Pressed += () =>
            {
                RoomPlayerView selectedTarget = snapshot.Public.Players[target.Selected];
                ToolType selectedTool = (ToolType)(int)tool.GetItemMetadata(tool.Selected);
                GameCommand command = card.Kind switch
                {
                    CardKind.Path => new PlayPathCommand(snapshot.Private.PlayerId, card.Id, new((int)positionX.Value, (int)positionY.Value)),
                    CardKind.BreakTool => new BreakToolCommand(snapshot.Private.PlayerId, card.Id, selectedTarget.Id),
                    CardKind.RepairTool => new RepairToolCommand(snapshot.Private.PlayerId, card.Id, selectedTarget.Id, selectedTool),
                    CardKind.Collapse => new CollapsePathCommand(snapshot.Private.PlayerId, card.Id, new((int)positionX.Value, (int)positionY.Value)),
                    CardKind.Map => new InspectGoalCommand(snapshot.Private.PlayerId, card.Id, new((int)positionX.Value, (int)positionY.Value)),
                    _ => throw new InvalidOperationException("Tipo de carta online desconhecido."),
                };
                _online.Submit(command);
            };
            cardActions.AddChild(play);
            Button discard = SecondaryButton("DESCARTAR");
            discard.Disabled = !myTurn;
            discard.Pressed += () => _online.Submit(new DiscardCommand(snapshot.Private.PlayerId, card.Id));
            cardActions.AddChild(discard);
            hand.AddChild(cardActions);
        }

        if (snapshot.Private.Hand.Count == 0)
        {
            Button pass = PrimaryButton("PASSAR");
            pass.Disabled = !myTurn;
            pass.Pressed += () => _online.Submit(new PassCommand(snapshot.Private.PlayerId));
            hand.AddChild(pass);
        }

        if (snapshot.Public.RoomPhase is RoomPhase.Finished or RoomPhase.Aborted)
        {
            Button leave = PrimaryButton("VOLTAR AO MENU");
            leave.Pressed += () =>
            {
                _online.Close();
                BuildStartScreen();
            };
            privateContent.AddChild(leave);
        }

        _onlineStatus = LabelText("Cada cliente recebe somente seu papel, sua mão e seus mapas.", 11, _muted, true);
        privateContent.AddChild(_onlineStatus);
        shell.AddChild(privatePanel);
    }

    private void OnOnlineStatus(string message, bool isError)
    {
        if (_onlineStatus is not null)
        {
            _onlineStatus.Text = message;
            _onlineStatus.Modulate = isError ? new Color("ef806f") : _muted;
        }

        if (isError)
        {
            GD.PushWarning(message);
        }
    }

    private void StartMatch(IEnumerable<string> names, long seed)
    {
        try
        {
            _match = new LocalMatchController(names, seed);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            GD.PushWarning(exception.Message);
            return;
        }

        BuildMatchScreen();
        Refresh();
        ShowTurnPrivacy();
    }

    private void BuildMatchScreen()
    {
        ClearScreen();
        AddChild(FullBackground(new Color("0b1118")));
        MarginContainer margin = new();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 22);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        AddChild(margin);
        VBoxContainer shell = VBox(10);
        margin.AddChild(shell);
        shell.AddChild(BuildTopBar());
        HBoxContainer body = HBox(12);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        shell.AddChild(body);
        body.AddChild(BuildPlayerRail());
        body.AddChild(BuildTableArea());
        body.AddChild(BuildLogRail());
        _status = LabelText("Selecione uma carta.", 13, _muted, true);
        shell.AddChild(_status);
    }

    private HBoxContainer BuildTopBar()
    {
        HBoxContainer bar = HBox(8);
        bar.CustomMinimumSize = new(0, 48);
        VBoxContainer title = VBox(0);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.AddChild(Heading(ProjectMetadata.DisplayName, 24, _accent));
        _turnLabel = LabelText("Preparando partida…", 13, _muted);
        title.AddChild(_turnLabel);
        bar.AddChild(title);
        Button nextTurn = SecondaryButton("JOGADA AUTO");
        nextTurn.TooltipText = "Executa uma jogada determinística válida para testes.";
        nextTurn.Pressed += () => RunAdministrative(() => _match!.AdvanceAdministrativeTurn());
        bar.AddChild(nextTurn);
        Button nextRound = SecondaryButton("FIM DA RODADA");
        nextRound.TooltipText = "Avança automaticamente até o resumo da rodada.";
        nextRound.Pressed += () => RunAdministrative(() => _match!.AdvanceToNextRound());
        bar.AddChild(nextRound);
        Button finish = SecondaryButton("PLACAR FINAL");
        finish.TooltipText = "Conclui as três rodadas para validar o fluxo completo.";
        finish.Pressed += () => RunAdministrative(() => _match!.AdvanceToMatchEnd());
        bar.AddChild(finish);
        return bar;
    }

    private PanelContainer BuildPlayerRail()
    {
        PanelContainer panel = Panel(new Color("131d28"), 15);
        panel.CustomMinimumSize = new(210, 0);
        VBoxContainer rail = VBox(10);
        panel.AddChild(rail);
        rail.AddChild(Heading("EQUIPE", 14, _muted));
        _players = VBox(7);
        rail.AddChild(_players);
        rail.AddChild(Spacer(4, true));
        rail.AddChild(LabelText("O ouro fica secreto até o placar final.", 12, _muted, true));
        return panel;
    }

    private VBoxContainer BuildTableArea()
    {
        VBoxContainer area = VBox(9);
        area.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        PanelContainer boardPanel = Panel(new Color("101923"), 8);
        boardPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _board = new BoardView { PositionSelected = OnBoardPositionSelected };
        boardPanel.AddChild(_board);
        area.AddChild(boardPanel);
        PanelContainer handPanel = Panel(new Color("131d28"), 12);
        VBoxContainer handStack = VBox(7);
        handPanel.AddChild(handStack);
        HBoxContainer prompt = HBox(6);
        _selectionHelp = LabelText("SUA MÃO", 13, _muted, true);
        _selectionHelp.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        prompt.AddChild(_selectionHelp);
        _targetPicker = new OptionButton { Visible = false, CustomMinimumSize = new(130, 0) };
        prompt.AddChild(_targetPicker);
        _toolPicker = new OptionButton { Visible = false, CustomMinimumSize = new(105, 0) };
        prompt.AddChild(_toolPicker);
        _playTargetButton = PrimaryButton("APLICAR");
        _playTargetButton.Visible = false;
        _playTargetButton.Pressed += PlaySelectedOnTarget;
        prompt.AddChild(_playTargetButton);
        _discardButton = SecondaryButton("DESCARTAR");
        _discardButton.Disabled = true;
        _discardButton.Pressed += DiscardSelected;
        prompt.AddChild(_discardButton);
        handStack.AddChild(prompt);
        ScrollContainer scroll = new() { CustomMinimumSize = new(0, 112) };
        _hand = HBox(7);
        scroll.AddChild(_hand);
        handStack.AddChild(scroll);
        area.AddChild(handPanel);
        return area;
    }

    private PanelContainer BuildLogRail()
    {
        PanelContainer panel = Panel(new Color("131d28"), 15);
        panel.CustomMinimumSize = new(220, 0);
        VBoxContainer rail = VBox(9);
        panel.AddChild(rail);
        rail.AddChild(Heading("REGISTRO PÚBLICO", 14, _muted));
        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        _publicLog = VBox(6);
        _publicLog.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_publicLog);
        rail.AddChild(scroll);
        return panel;
    }

    private void Refresh()
    {
        if (_match is null || _board is null || _players is null || _hand is null)
        {
            return;
        }

        GameState state = _match.State;
        _turnLabel!.Text = state.IsFinished
            ? "Partida concluída"
            : $"Rodada {state.RoundNumber}/3 · Turno {state.TurnNumber} · {state.CurrentPlayer.Name}";
        ClearChildren(_players);
        foreach (PlayerState player in state.Players)
        {
            bool current = !state.IsFinished && player.Id == state.CurrentPlayer.Id;
            PanelContainer badge = Panel(current ? new Color("23413a") : new Color("192531"), 9);
            VBoxContainer details = VBox(2);
            details.AddChild(LabelText((current ? "● " : "") + player.Name, 14, current ? new Color("72dda9") : _ink));
            details.AddChild(LabelText($"Mão {player.Hand.Count} · {ToolLabel(player.BrokenTools)}", 11, _muted, true));
            badge.AddChild(details);
            _players.AddChild(badge);
        }

        ClearChildren(_hand);
        if (!state.IsFinished)
        {
            foreach (CardDefinition card in state.CurrentPlayer.Hand)
            {
                Button button = CardButton(card, card == _selectedCard);
                button.Pressed += () => SelectCard(card);
                _hand.AddChild(button);
            }
        }

        if (_selectedCard is not null && !state.CurrentPlayer.Hand.Contains(_selectedCard))
        {
            _selectedCard = null;
        }

        UpdateSelectionControls();
        _board.Display(state.Board, _selectedCard);
    }

    private void SelectCard(CardDefinition card)
    {
        _selectedCard = _selectedCard == card ? null : card;
        Refresh();
    }

    private void UpdateSelectionControls()
    {
        if (_match is null || _targetPicker is null || _toolPicker is null ||
            _playTargetButton is null || _discardButton is null || _selectionHelp is null)
        {
            return;
        }

        _targetPicker.Visible = false;
        _toolPicker.Visible = false;
        _playTargetButton.Visible = false;
        _discardButton.Disabled = _selectedCard is null;
        if (_selectedCard is null)
        {
            _selectionHelp.Text = "SUA MÃO · escolha uma carta";
            return;
        }

        _selectionHelp.Text = _selectedCard.Kind switch
        {
            CardKind.Path => "Clique em um destino verde na mesa",
            CardKind.Map => "Clique em um objetivo fechado",
            CardKind.Collapse => "Clique em um caminho comum",
            CardKind.BreakTool => "Escolha quem terá a ferramenta quebrada",
            CardKind.RepairTool => "Escolha jogador e ferramenta para consertar",
            _ => "Escolha o destino",
        };
        if (_selectedCard.Kind is CardKind.BreakTool or CardKind.RepairTool)
        {
            _targetPicker.Clear();
            foreach (PlayerState player in _match.State.Players)
            {
                _targetPicker.AddItem(player.Name);
            }

            _targetPicker.Visible = true;
            _playTargetButton.Visible = true;
        }

        if (_selectedCard.Kind == CardKind.RepairTool)
        {
            _toolPicker.Clear();
            foreach (ToolType tool in SingleTools(_selectedCard.Tools))
            {
                _toolPicker.AddItem(ToolName(tool));
                _toolPicker.SetItemMetadata(_toolPicker.ItemCount - 1, (int)tool);
            }

            _toolPicker.Visible = true;
        }
    }

    private void OnBoardPositionSelected(BoardPosition position)
    {
        if (_match is null || _selectedCard is null ||
            _selectedCard.Kind is CardKind.BreakTool or CardKind.RepairTool)
        {
            SetStatus("Escolha primeiro uma carta que usa a mesa.", true);
            return;
        }

        Resolve(_match.PlayCard(_selectedCard, position));
    }

    private void PlaySelectedOnTarget()
    {
        if (_match is null || _selectedCard is null || _targetPicker is null)
        {
            return;
        }

        PlayerId target = _match.State.Players[_targetPicker.Selected].Id;
        ToolType tool = _selectedCard.Kind == CardKind.RepairTool && _toolPicker is not null && _toolPicker.ItemCount > 0
            ? (ToolType)(int)_toolPicker.GetItemMetadata(_toolPicker.Selected)
            : ToolType.None;
        Resolve(_match.PlayCard(_selectedCard, targetPlayerId: target, tool: tool));
    }

    private void DiscardSelected()
    {
        if (_match is not null && _selectedCard is not null)
        {
            Resolve(_match.Discard(_selectedCard));
        }
    }

    private void Resolve(CommandResult result)
    {
        if (!result.Accepted)
        {
            SetStatus(TranslateError(result.Error), true);
            return;
        }

        _selectedCard = null;
        AppendPublicEvents(result.Events);
        Refresh();
        ShowEventOverlayOrNextTurn(result.Events);
    }

    private void RunAdministrative(Func<IReadOnlyList<GameEvent>> operation)
    {
        if (_match is null || _match.State.IsFinished)
        {
            return;
        }

        IReadOnlyList<GameEvent> events = operation();
        _selectedCard = null;
        AppendPublicEvents(events);
        Refresh();
        ShowEventOverlayOrNextTurn(events);
    }

    private void ShowEventOverlayOrNextTurn(IReadOnlyList<GameEvent> events)
    {
        GoalInspected? inspection = events.OfType<GoalInspected>().LastOrDefault();
        if (inspection is not null)
        {
            ShowMessageOverlay("INFORMAÇÃO PRIVADA", $"O objetivo em ({inspection.Position.X}, {inspection.Position.Y}) contém {GoalName(inspection.Content)}.", "OCULTAR E PASSAR", ShowTurnPrivacy);
            return;
        }

        if (_match!.State.IsFinished)
        {
            ShowFinalScore();
            return;
        }

        RoundEnded? round = events.OfType<RoundEnded>().LastOrDefault();
        if (round is not null)
        {
            string roles = string.Join("\n", events.OfType<RoleRevealed>()
                .GroupBy(item => item.PlayerId)
                .Select(group => group.Last())
                .Select(item => $"{PlayerName(item.PlayerId)} — {RoleName(item.Role)}"));
            ShowMessageOverlay($"RODADA {round.RoundNumber} ENCERRADA", (round.GoldReached ? "O ouro foi alcançado." : "O ouro não foi alcançado.") + "\n\n" + roles, "COMEÇAR PRÓXIMA RODADA", ShowTurnPrivacy);
            return;
        }

        ShowTurnPrivacy();
    }

    private void ShowTurnPrivacy()
    {
        if (_match is null || _match.State.IsFinished)
        {
            return;
        }

        PlayerState player = _match.State.CurrentPlayer;
        ShowMessageOverlay("TROCA DE TURNO", $"Passe o controle para {player.Name}.\nOs demais jogadores devem desviar o olhar.", "REVELAR MINHAS INFORMAÇÕES", () =>
            ShowMessageOverlay(player.Name.ToUpperInvariant(), $"Seu papel: {RoleName(player.Role)}\n\nSua mão já está disponível na mesa." + InspectedGoalText(player), "COMEÇAR TURNO", CloseOverlay));
    }

    private void ShowFinalScore()
    {
        GameState state = _match!.State;
        int best = state.Players.Max(player => player.Gold);
        string scores = string.Join("\n", state.Players.OrderByDescending(player => player.Gold)
            .Select(player => $"{(player.Gold == best ? "★" : " ")} {player.Name} — {player.Gold} pepitas"));
        string result = state.Players.Count(player => player.Gold == best) > 1 ? "Vitória compartilhada." : "Temos uma pessoa vencedora.";
        ShowMessageOverlay("PLACAR FINAL", scores + "\n\n" + result, "NOVA PARTIDA", BuildStartScreen);
    }

    private void ShowMessageOverlay(string title, string body, string buttonText, Action action)
    {
        CloseOverlay();
        ColorRect veil = FullBackground(new Color(0.02f, 0.035f, 0.05f, 0.96f));
        veil.MouseFilter = MouseFilterEnum.Stop;
        AddChild(veil);
        _overlay = veil;
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        veil.AddChild(center);
        PanelContainer panel = Panel(new Color("172432"), 28);
        panel.CustomMinimumSize = new(500, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(17);
        panel.AddChild(content);
        Label heading = Heading(title, 25, _accent);
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(heading);
        Label message = LabelText(body, 17, _ink, true);
        message.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(message);
        Button button = PrimaryButton(buttonText);
        button.Pressed += action;
        content.AddChild(button);
    }

    private void CloseOverlay()
    {
        _overlay?.QueueFree();
        _overlay = null;
    }

    private void AppendPublicEvents(IEnumerable<GameEvent> events)
    {
        if (_publicLog is null)
        {
            return;
        }

        foreach (GameEvent gameEvent in events)
        {
            string? text = gameEvent switch
            {
                PathPlaced placed => $"{PlayerName(placed.PlayerId)} colocou um caminho em ({placed.Position.X}, {placed.Position.Y}).",
                CardDiscarded discarded => $"{PlayerName(discarded.PlayerId)} descartou uma carta virada para baixo.",
                ToolBroken broken => $"{PlayerName(broken.PlayerId)} quebrou {ToolName(broken.Tool)} de {PlayerName(broken.TargetPlayerId)}.",
                ToolRepaired repaired => $"{PlayerName(repaired.PlayerId)} consertou {ToolName(repaired.Tool)} de {PlayerName(repaired.TargetPlayerId)}.",
                PathCollapsed collapsed => $"{PlayerName(collapsed.PlayerId)} removeu um caminho.",
                GoalRevealed revealed => $"Objetivo revelado: {GoalName(revealed.Content)}.",
                RoundEnded ended => $"Rodada {ended.RoundNumber} encerrada.",
                RoundStarted started => $"Rodada {started.RoundNumber} iniciada.",
                _ => null,
            };
            if (text is not null)
            {
                _publicLog.AddChild(LabelText(text, 11, _muted, true));
            }
        }
    }

    private string PlayerName(PlayerId id) => _match!.State.Players.First(player => player.Id == id).Name;

    private void SetStatus(string message, bool error = false)
    {
        if (_status is not null)
        {
            _status.Text = message;
            _status.Modulate = error ? new Color("ff887a") : _muted;
        }
    }

    private static string TranslateError(string? error) => error switch
    {
        "A path edge does not match its neighbor." => "As bordas desta carta não combinam com as vizinhas.",
        "The board position is occupied." => "Esta posição já está ocupada.",
        "The path is not connected to the start card." => "O caminho precisa continuar uma rota alcançável desde a origem.",
        "A player with a broken tool cannot play path cards." => "Uma ferramenta quebrada impede jogar caminhos.",
        "Only a common path card can be collapsed." => "Escolha uma carta de caminho comum.",
        "A map can inspect only a hidden goal." => "O mapa só pode olhar um objetivo ainda fechado.",
        "The target already has that tool broken." => "Essa ferramenta já está quebrada nesse alvo.",
        "The selected tool is not broken." => "A ferramenta escolhida não está quebrada.",
        _ => error ?? "Jogada inválida.",
    };

    private static string InspectedGoalText(PlayerState player) => player.InspectedGoals.Count == 0
        ? string.Empty
        : "\n\nMapas vistos:\n" + string.Join("\n", player.InspectedGoals.Select(item => $"({item.Key.X}, {item.Key.Y}) — {GoalName(item.Value)}"));

    private static string RoleName(PlayerRole role) => role == PlayerRole.Miner ? "MINERADOR" : "SABOTADOR";
    private static string GoalName(GoalContent goal) => goal == GoalContent.Gold ? "OURO" : "PEDRA";
    private static string ToolName(ToolType tool) => tool switch
    {
        ToolType.Lamp => "lamparina",
        ToolType.Cart => "carrinho",
        ToolType.Pickaxe => "picareta",
        _ => "ferramenta",
    };

    private static string ToolLabel(ToolType tools) => tools == ToolType.None
        ? "ferramentas íntegras"
        : "quebrado: " + string.Join(", ", SingleTools(tools).Select(ToolName));

    private static IEnumerable<ToolType> SingleTools(ToolType tools)
    {
        foreach (ToolType tool in new[] { ToolType.Lamp, ToolType.Cart, ToolType.Pickaxe })
        {
            if ((tools & tool) != ToolType.None)
            {
                yield return tool;
            }
        }
    }

    private static string CardName(CardDefinition card) => card.Kind switch
    {
        CardKind.Path => "CAMINHO\n" + EdgeGlyph(card.Edges),
        CardKind.BreakTool => "QUEBRAR\n" + string.Join("/", SingleTools(card.Tools).Select(ToolName)),
        CardKind.RepairTool => "CONSERTAR\n" + string.Join("/", SingleTools(card.Tools).Select(ToolName)),
        CardKind.Collapse => "DESMORONAR\n⌁",
        CardKind.Map => "MAPA\n◇",
        _ => card.Id.ToString(),
    };

    private static string EdgeGlyph(EdgeMask edges) => edges switch
    {
        EdgeMask.All => "╋",
        EdgeMask.North | EdgeMask.South => "┃",
        EdgeMask.East | EdgeMask.West => "━",
        EdgeMask.North | EdgeMask.East => "┗",
        EdgeMask.East | EdgeMask.South => "┏",
        EdgeMask.South | EdgeMask.West => "┓",
        EdgeMask.West | EdgeMask.North => "┛",
        _ => "◆",
    };

    private static Button CardButton(CardDefinition card, bool selected)
    {
        Button button = new() { Text = CardName(card), CustomMinimumSize = new(116, 96), ToggleMode = true, ButtonPressed = selected };
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeColorOverride("font_color", new Color("17202a"));
        button.AddThemeStyleboxOverride("normal", RoundedStyle(new Color("c9aa70"), 8));
        button.AddThemeStyleboxOverride("hover", RoundedStyle(new Color("e0c58d"), 8));
        button.AddThemeStyleboxOverride("pressed", RoundedStyle(new Color("68d4a3"), 8));
        return button;
    }

    private static PanelContainer Panel(Color color, int padding)
    {
        PanelContainer panel = new();
        StyleBoxFlat style = RoundedStyle(color, 10);
        style.ContentMarginLeft = padding;
        style.ContentMarginTop = padding;
        style.ContentMarginRight = padding;
        style.ContentMarginBottom = padding;
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Button PrimaryButton(string text)
    {
        Button button = new() { Text = text, CustomMinimumSize = new(0, 42) };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", new Color("15201c"));
        button.AddThemeStyleboxOverride("normal", RoundedStyle(new Color("e2b84b"), 7));
        button.AddThemeStyleboxOverride("hover", RoundedStyle(new Color("f1cd68"), 7));
        button.AddThemeStyleboxOverride("pressed", RoundedStyle(new Color("c89a31"), 7));
        return button;
    }

    private Button SecondaryButton(string text)
    {
        Button button = new() { Text = text, CustomMinimumSize = new(0, 38) };
        button.AddThemeFontSizeOverride("font_size", 11);
        button.AddThemeColorOverride("font_color", _ink);
        button.AddThemeStyleboxOverride("normal", RoundedStyle(new Color("263442"), 7));
        button.AddThemeStyleboxOverride("hover", RoundedStyle(new Color("35485a"), 7));
        button.AddThemeStyleboxOverride("pressed", RoundedStyle(new Color("1c2834"), 7));
        return button;
    }

    private static StyleBoxFlat RoundedStyle(Color color, int radius) => new()
    {
        BgColor = color,
        CornerRadiusTopLeft = radius,
        CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius,
        CornerRadiusBottomRight = radius,
    };

    private static VBoxContainer VBox(int separation)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", separation);
        return box;
    }

    private static HBoxContainer HBox(int separation)
    {
        HBoxContainer box = new();
        box.AddThemeConstantOverride("separation", separation);
        return box;
    }

    private static Label Heading(string text, int size, Color color) => LabelText(text, size, color);

    private static Label LabelText(string text, int size, Color color, bool wrap = false)
    {
        Label label = new() { Text = text, Modulate = color };
        label.AddThemeFontSizeOverride("font_size", size);
        if (wrap)
        {
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        }

        return label;
    }

    private static Control Spacer(float height = 0, bool expand = false) => new()
    {
        CustomMinimumSize = new(0, height),
        SizeFlagsVertical = expand ? SizeFlags.ExpandFill : SizeFlags.Fill,
    };

    private static ColorRect FullBackground(Color color)
    {
        ColorRect background = new() { Color = color, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return background;
    }

    private void ClearScreen()
    {
        _overlay = null;
        foreach (Node child in GetChildren())
        {
            if (child == _online)
            {
                continue;
            }

            RemoveChild(child);
            child.QueueFree();
        }
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
