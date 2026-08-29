using Godot;
using MineYourBusiness.Application;

namespace MineYourBusiness;

public partial class Bootstrap
{
    private static readonly (string Title, string Body)[] TutorialPages =
    [
        ("O TRABALHO", "Vocês têm três rodadas para abrir uma rota da origem até o ouro. Mineradores cooperam; sabotadores tentam atrasar a equipe sem revelar o próprio papel."),
        ("SEU TURNO", "Faça exatamente uma ação: jogue uma carta válida ou descarte uma carta virada para baixo. Depois, compre automaticamente se ainda houver cartas no baralho."),
        ("CAMINHOS E AÇÕES", "Caminhos precisam encaixar em todas as bordas e continuar ligados à origem. Quebras impedem caminhos; consertos removem uma quebra; mapa revela um objetivo só para você; desmoronamento remove um caminho comum."),
        ("CONTROLES E PRIVACIDADE", "Mouse: clique, roda para zoom e botão do meio para mover. Teclado: Tab navega, 1–9 escolhem cartas; na mesa, use setas, Enter, +/− e F. Em jogo local, respeite a barreira de privacidade entre turnos."),
    ];

    private PlayerPreferences _preferences = new();
    private PrivacyTelemetry? _telemetry;
    private AudioCuePlayer? _audio;
    private string _preferencesPath = string.Empty;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse &&
            _roleCard is { Revealed: true } && !_roleCard.GetGlobalRect().HasPoint(mouse.Position))
        {
            _roleCard.Conceal();
        }

        if (@event is not InputEventKey { Pressed: true, Echo: false, CtrlPressed: true } key)
        {
            return;
        }

        double? requestedScale = key.Keycode switch
        {
            Key.Key0 or Key.Kp0 => 1.0,
            Key.Minus or Key.KpSubtract => _preferences.UiScale - 0.05,
            _ => null,
        };

        if (requestedScale is null)
        {
            return;
        }

        SetUiScale(requestedScale.Value);
        GetViewport().SetInputAsHandled();
    }

    private void InitializeExperience()
    {
        _preferencesPath = ProjectSettings.GlobalizePath("user://settings.json");
        string telemetryPath = ProjectSettings.GlobalizePath("user://beta-telemetry.jsonl");
        _preferences = PlayerPreferencesStore.Load(_preferencesPath);
        _telemetry = new PrivacyTelemetry(telemetryPath, _preferences.TelemetryEnabled);
        _audio = new AudioCuePlayer { Name = "AudioCues" };
        AddChild(_audio);
        ApplyPreferences();
        Track("app_started");
    }

    private void ApplyPreferences()
    {
        _ink = _preferences.HighContrast ? Colors.White : new Color("e9edf2");
        _muted = _preferences.HighContrast ? new Color("d8e0e8") : new Color("9daaba");
        _accent = _preferences.HighContrast ? new Color("ffd400") : new Color("e2b84b");
        _audio?.SetVolume(_preferences.MasterVolume);
        GetTree().Root.ContentScaleFactor = (float)_preferences.UiScale;
    }

    private void SetUiScale(double value)
    {
        double normalizedScale = Math.Round(Math.Clamp(value, 0.8, 1.5), 2);
        if (Math.Abs(normalizedScale - _preferences.UiScale) < 0.001)
        {
            return;
        }

        _preferences = _preferences with { UiScale = normalizedScale };
        ApplyPreferences();
        SavePreferences();
    }

    private void AddMenuBackdrop()
    {
        Texture2D? texture = GD.Load<Texture2D>("res://assets/art/main-menu-mine.png");
        if (texture is not null)
        {
            TextureRect backdrop = new()
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(backdrop);
        }

        AddChild(FullBackground(new Color(0.025f, 0.04f, 0.06f, _preferences.HighContrast ? 0.9f : 0.76f)));
    }

    private void BuildTutorial(int page = 0)
    {
        page = Math.Clamp(page, 0, TutorialPages.Length - 1);
        ClearScreen();
        AddMenuBackdrop();
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = Panel(new Color("151f2b"), 30);
        panel.CustomMinimumSize = new Vector2(650, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(18);
        panel.AddChild(content);
        content.AddChild(LabelText($"GUIA {page + 1}/{TutorialPages.Length}", 13, new Color("72dda9")));
        content.AddChild(Heading(TutorialPages[page].Title, 30, _accent));
        content.AddChild(LabelText(TutorialPages[page].Body, 17, _ink, true));

        HBoxContainer progress = HBox(7);
        for (int index = 0; index < TutorialPages.Length; index++)
        {
            ColorRect pip = new()
            {
                Color = index == page ? _accent : new Color("3b4855"),
                CustomMinimumSize = new Vector2(index == page ? 42 : 22, 5),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            progress.AddChild(pip);
        }

        content.AddChild(progress);
        HBoxContainer actions = HBox(8);
        Button exit = SecondaryButton(page == TutorialPages.Length - 1 ? "VOLTAR" : "PULAR GUIA");
        exit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exit.Pressed += CompleteTutorial;
        actions.AddChild(exit);
        if (page > 0)
        {
            Button previous = SecondaryButton("ANTERIOR");
            previous.Pressed += () => BuildTutorial(page - 1);
            actions.AddChild(previous);
        }

        Button next = PrimaryButton(page == TutorialPages.Length - 1 ? "PRONTO PARA JOGAR" : "PRÓXIMO");
        next.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        next.Pressed += page == TutorialPages.Length - 1 ? CompleteTutorial : () => BuildTutorial(page + 1);
        actions.AddChild(next);
        content.AddChild(actions);
    }

    private void CompleteTutorial()
    {
        _preferences = _preferences with { TutorialCompleted = true };
        SavePreferences();
        Track("tutorial_completed");
        BuildStartScreen();
    }

    private void BuildSettingsScreen()
    {
        ClearScreen();
        AddMenuBackdrop();
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = Panel(new Color("151f2b"), 25);
        panel.CustomMinimumSize = new Vector2(620, 0);
        center.AddChild(panel);
        VBoxContainer content = VBox(10);
        panel.AddChild(content);
        content.AddChild(Heading("CONFIGURAÇÕES", 28, _accent));

        Label scaleValue = LabelText($"Escala da interface: {_preferences.UiScale:0.00}×", 14, _ink);
        content.AddChild(scaleValue);
        content.AddChild(LabelText("Atalhos globais: Ctrl+0 restaura 1× · Ctrl+− reduz a escala", 11, _muted, true));
        HSlider scale = new() { MinValue = 0.8, MaxValue = 1.5, Step = 0.05, Value = _preferences.UiScale };
        scale.ValueChanged += value => scaleValue.Text = $"Escala da interface: {value:0.00}×";
        content.AddChild(scale);

        Label volumeValue = LabelText($"Volume dos avisos: {_preferences.MasterVolume:P0}", 14, _ink);
        content.AddChild(volumeValue);
        HSlider volume = new() { MinValue = 0, MaxValue = 1, Step = 0.05, Value = _preferences.MasterVolume };
        volume.ValueChanged += value => volumeValue.Text = $"Volume dos avisos: {value:P0}";
        content.AddChild(volume);

        CheckBox contrast = new() { Text = "Alto contraste e marcações redundantes", ButtonPressed = _preferences.HighContrast };
        content.AddChild(contrast);
        CheckBox reducedMotion = new() { Text = "Movimento reduzido (transições instantâneas)", ButtonPressed = _preferences.ReducedMotion };
        content.AddChild(reducedMotion);
        content.AddChild(Spacer(4));
        content.AddChild(Heading("PRIVACIDADE", 17, _muted));
        CheckBox telemetry = new() { Text = "Permitir telemetria beta local", ButtonPressed = _preferences.TelemetryEnabled };
        content.AddChild(telemetry);
        content.AddChild(LabelText("Desativada por padrão. Quando ativada, grava somente nomes fixos de eventos e contadores numéricos neste dispositivo. Nunca inclui nomes, IP, código da sala, papel, mão ou texto livre; nada é enviado pela rede.", 12, _muted, true));

        Button deleteTelemetry = SecondaryButton("APAGAR TELEMETRIA LOCAL");
        deleteTelemetry.Pressed += () =>
        {
            _telemetry?.DeleteStoredData();
            deleteTelemetry.Text = "DADOS LOCAIS APAGADOS";
            deleteTelemetry.Disabled = true;
        };
        content.AddChild(deleteTelemetry);

        HBoxContainer actions = HBox(8);
        Button cancel = SecondaryButton("CANCELAR");
        cancel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        cancel.Pressed += BuildStartScreen;
        actions.AddChild(cancel);
        Button save = PrimaryButton("SALVAR");
        save.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        save.Pressed += () =>
        {
            _preferences = _preferences with
            {
                UiScale = scale.Value,
                MasterVolume = volume.Value,
                HighContrast = contrast.ButtonPressed,
                ReducedMotion = reducedMotion.ButtonPressed,
                TelemetryEnabled = telemetry.ButtonPressed,
            };
            _telemetry?.SetEnabled(_preferences.TelemetryEnabled);
            ApplyPreferences();
            SavePreferences();
            Track("settings_saved");
            BuildStartScreen();
        };
        actions.AddChild(save);
        content.AddChild(actions);
    }

    private void SavePreferences()
    {
        try
        {
            PlayerPreferencesStore.Save(_preferencesPath, _preferences);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Não foi possível salvar as configurações: {exception.Message}");
        }
    }

    private void Track(string eventName, IReadOnlyDictionary<string, long>? counters = null)
    {
        try
        {
            _telemetry?.Track(eventName, counters);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Não foi possível gravar telemetria local: {exception.Message}");
        }
    }
}
