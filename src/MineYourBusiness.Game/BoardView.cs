using Godot;
using MineYourBusiness.Application;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>Expandable local board with mouse pan, zoom and coordinate selection.</summary>
public partial class BoardView : Control
{
    private const float CardWidth = 64.0f;
    private const float CardHeight = 84.0f;
    private const float CellSize = 92.0f;
    private readonly HashSet<BoardPosition> _validPositions = [];
    private BoardState? _board;
    private IReadOnlyList<BoardCardView>? _publicBoard;
    private Vector2 _pan = new(75.0f, 0.0f);
    private float _zoom = 0.72f;
    private bool _panning;

    public BoardView()
    {
        CustomMinimumSize = new(620.0f, 390.0f);
        MouseDefaultCursorShape = CursorShape.Cross;
        ClipContents = true;
    }

    public Action<BoardPosition>? PositionSelected { get; set; }

    public void Display(BoardState board, CardDefinition? selectedCard)
    {
        _board = board;
        _publicBoard = null;
        _validPositions.Clear();
        if (selectedCard?.Kind == CardKind.Path)
        {
            foreach (BoardPosition occupied in board.Cards.Keys)
            {
                foreach (Direction direction in Enum.GetValues<Direction>())
                {
                    BoardPosition candidate = occupied.Move(direction);
                    if (!board.Cards.ContainsKey(candidate) &&
                        board.ValidatePlacement(candidate, selectedCard).IsValid)
                    {
                        _validPositions.Add(candidate);
                    }
                }
            }
        }

        QueueRedraw();
    }

    public void Display(IReadOnlyList<BoardCardView> board)
    {
        _board = null;
        _publicBoard = board;
        _validPositions.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("101923"));
        if (_board is null && _publicBoard is null)
        {
            return;
        }

        foreach (BoardPosition position in _validPositions)
        {
            Rect2 rect = CardRect(position).Grow(4.0f * _zoom);
            DrawRect(rect, new Color(0.26f, 0.78f, 0.53f, 0.22f), true);
            DrawRect(rect, new Color("59d99b"), false, 2.0f);
        }

        if (_board is not null)
        {
            foreach ((BoardPosition position, PlacedCard card) in _board.Cards)
            {
                DrawCard(position, card.Definition.Edges, card.IsStart, card.IsGoal, card.IsRevealed, card.Goal);
            }
        }

        if (_publicBoard is not null)
        {
            foreach (BoardCardView card in _publicBoard)
            {
                DrawCard(card.Position, card.Edges, card.IsStart, card.IsGoal, card.IsRevealed, card.RevealedGoal);
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton wheel && wheel.Pressed &&
            wheel.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            float previous = _zoom;
            _zoom = Mathf.Clamp(
                _zoom + (wheel.ButtonIndex == MouseButton.WheelUp ? 0.08f : -0.08f),
                0.45f,
                1.35f);
            Vector2 anchor = wheel.Position - BoardOrigin();
            _pan -= anchor * ((_zoom / previous) - 1.0f);
            QueueRedraw();
            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseButton middle && middle.ButtonIndex == MouseButton.Middle)
        {
            _panning = middle.Pressed;
            MouseDefaultCursorShape = _panning ? CursorShape.Drag : CursorShape.Cross;
            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseMotion motion && _panning)
        {
            _pan += motion.Relative;
            QueueRedraw();
            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseButton left && left.ButtonIndex == MouseButton.Left && left.Pressed)
        {
            Vector2 local = (left.Position - BoardOrigin()) / (CellSize * _zoom);
            BoardPosition position = new(Mathf.RoundToInt(local.X), Mathf.RoundToInt(local.Y));
            PositionSelected?.Invoke(position);
            AcceptEvent();
        }
    }

    private void DrawCard(
        BoardPosition position,
        EdgeMask edges,
        bool isStart,
        bool isGoal,
        bool isRevealed,
        GoalContent goal)
    {
        Rect2 rect = CardRect(position);
        Color fill = isStart
            ? new Color("39735c")
            : isGoal && !isRevealed
                ? new Color("473a58")
                : goal == GoalContent.Gold
                    ? new Color("d5a62e")
                    : goal == GoalContent.Stone
                        ? new Color("6d7078")
                        : new Color("c5a56b");
        DrawStyleBox(MakeCardStyle(fill), rect);

        Vector2 center = rect.GetCenter();
        if (isGoal && !isRevealed)
        {
            Vector2[] diamond =
            [
                center + new Vector2(0, -18) * _zoom,
                center + new Vector2(15, 0) * _zoom,
                center + new Vector2(0, 18) * _zoom,
                center + new Vector2(-15, 0) * _zoom,
            ];
            DrawColoredPolygon(diamond, new Color("b8a4ce"));
            DrawCircle(center, 4.0f * _zoom, new Color("473a58"));
            return;
        }

        if (goal == GoalContent.Gold)
        {
            DrawCircle(center, 17.0f * _zoom, new Color("ffe176"));
            DrawCircle(center, 9.0f * _zoom, new Color("d99b23"));
            return;
        }

        DrawTunnels(rect, edges);
        if (isStart)
        {
            DrawCircle(center, 8.0f * _zoom, new Color("d8f2c4"));
        }
    }

    private void DrawTunnels(Rect2 rect, EdgeMask edges)
    {
        Vector2 center = rect.GetCenter();
        float width = 13.0f * _zoom;
        Color shadow = new("4a3827");
        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            if (!edges.IsOpen(direction))
            {
                continue;
            }

            Vector2 end = direction switch
            {
                Direction.North => new(center.X, rect.Position.Y),
                Direction.East => new(rect.End.X, center.Y),
                Direction.South => new(center.X, rect.End.Y),
                Direction.West => new(rect.Position.X, center.Y),
                _ => center,
            };
            DrawLine(center, end, shadow, width, true);
            DrawLine(center, end, new Color("6f5437"), width * 0.48f, true);
        }

        DrawCircle(center, width * 0.5f, shadow);
    }

    private Rect2 CardRect(BoardPosition position)
    {
        Vector2 center = BoardOrigin() + new Vector2(position.X, position.Y) * CellSize * _zoom;
        Vector2 size = new(CardWidth * _zoom, CardHeight * _zoom);
        return new Rect2(center - (size / 2.0f), size);
    }

    private Vector2 BoardOrigin() => new(_pan.X, (Size.Y / 2.0f) + _pan.Y);

    private static StyleBoxFlat MakeCardStyle(Color color) => new()
    {
        BgColor = color,
        BorderColor = color.Lightened(0.22f),
        BorderWidthLeft = 2,
        BorderWidthTop = 2,
        BorderWidthRight = 2,
        BorderWidthBottom = 2,
        CornerRadiusTopLeft = 7,
        CornerRadiusTopRight = 7,
        CornerRadiusBottomLeft = 7,
        CornerRadiusBottomRight = 7,
    };
}
