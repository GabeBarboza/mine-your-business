using Godot;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>Font-independent thumbnail of a path card for the player's hand.</summary>
public partial class PathCardPreview : Control
{
    public EdgeMask Edges { get; init; }

    public bool HighContrast { get; init; }

    public PathCardPreview()
    {
        CustomMinimumSize = new Vector2(0, 48);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        Vector2 center = Size / 2.0f;
        float halfWidth = MathF.Min(31, (Size.X / 2.0f) - 4);
        float halfHeight = MathF.Min(22, (Size.Y / 2.0f) - 2);
        Rect2 card = new(center - new Vector2(halfWidth, halfHeight), new Vector2(halfWidth * 2, halfHeight * 2));
        Color border = HighContrast ? new Color("101820") : new Color("6f5437");
        Color tunnel = HighContrast ? new Color("111111") : new Color("4a3827");
        Color highlight = HighContrast ? new Color("f8f0d4") : new Color("8b6843");

        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = HighContrast ? new Color("f7cf7a") : new Color("d9ba7d"),
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
        }, card);

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            if (!Edges.IsOpen(direction))
            {
                continue;
            }

            Vector2 end = direction switch
            {
                Direction.North => new Vector2(center.X, card.Position.Y),
                Direction.East => new Vector2(card.End.X, center.Y),
                Direction.South => new Vector2(center.X, card.End.Y),
                Direction.West => new Vector2(card.Position.X, center.Y),
                _ => center,
            };
            DrawLine(center, end, tunnel, 11, true);
            DrawLine(center, end, highlight, 4, true);
        }

        DrawCircle(center, 5.5f, tunnel);
        DrawCircle(center, 2.0f, highlight);
    }
}
