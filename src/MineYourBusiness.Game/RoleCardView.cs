using Godot;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>A role card kept inside a table pocket and revealed only by an intentional click.</summary>
public partial class RoleCardView : Control
{
    private Texture2D? _art;

    public PlayerRole Role { get; private set; } = PlayerRole.Miner;

    public bool Revealed { get; private set; }

    public bool IsInteractive { get; private set; }

    public event Action<bool>? RevealChanged;

    public RoleCardView()
    {
        CustomMinimumSize = new Vector2(150, 166);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        TooltipText = "Clique para revelar ou ocultar sua função.";
        FocusMode = FocusModeEnum.All;
    }

    public override void _Ready()
    {
        _art = GD.Load<Texture2D>("res://assets/art/role-card-miner.png");
        QueueRedraw();
    }

    public void Display(PlayerRole role, bool interactive)
    {
        Role = role;
        IsInteractive = interactive;
        MouseDefaultCursorShape = interactive ? CursorShape.PointingHand : CursorShape.Arrow;
        QueueRedraw();
    }

    public void Conceal()
    {
        if (!Revealed)
        {
            return;
        }

        Revealed = false;
        RevealChanged?.Invoke(false);
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        bool clicked = @event is InputEventMouseButton
        {
            ButtonIndex: MouseButton.Left,
            Pressed: true,
        };
        bool activatedByKeyboard = @event is InputEventKey
        {
            Pressed: true,
            Echo: false,
            Keycode: Key.Enter or Key.Space,
        };
        if (!IsInteractive || (!clicked && !activatedByKeyboard))
        {
            return;
        }

        Revealed = !Revealed;
        RevealChanged?.Invoke(Revealed);
        QueueRedraw();
        AcceptEvent();
    }

    public override void _Draw()
    {
        StyleBoxFlat pocket = new()
        {
            BgColor = new Color("111a24"),
            BorderColor = new Color("314151"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
        };
        DrawStyleBox(pocket, new Rect2(Vector2.Zero, Size));

        float cardWidth = MathF.Min(112, Size.X - 18);
        float cardHeight = 154;
        float cardX = (Size.X - cardWidth) / 2.0f;
        float cardY = Revealed ? 7 : 61;
        Rect2 cardRect = new(new Vector2(cardX, cardY), new Vector2(cardWidth, cardHeight));
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = new Color("172432"),
            BorderColor = Revealed ? new Color("e2b84b") : new Color("536576"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        }, cardRect);

        if (_art is not null)
        {
            DrawTextureRect(_art, cardRect.Grow(-3), false);
        }

        if (Revealed)
        {
            Rect2 labelRect = new(cardRect.Position + new Vector2(3, cardHeight - 36), new Vector2(cardWidth - 6, 33));
            DrawRect(labelRect, new Color(0.03f, 0.05f, 0.07f, 0.94f));
            string role = Role == PlayerRole.Miner ? "MINERADOR" : "SABOTADOR";
            DrawString(ThemeDB.FallbackFont, labelRect.Position + new Vector2(0, 22), role,
                HorizontalAlignment.Center, labelRect.Size.X, 13, Role == PlayerRole.Miner ? new Color("72dda9") : new Color("ff887a"));
        }

        if (!Revealed)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(0, 24), "CLIQUE PARA REVELAR",
                HorizontalAlignment.Center, Size.X, 10, new Color("9daaba"));
        }
    }
}
