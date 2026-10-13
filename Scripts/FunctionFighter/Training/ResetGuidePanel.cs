using Godot;
using SakugaEngine.UI;

namespace FunctionFighter.Training
{
    /// <summary>
    /// "Reset & Reposition" card to the right of the Training pause menu (2XKO style): one row per Back + direction
    /// reset, with a little stage showing where P1 (red) and P2 (blue) end up. Visual only; created by TrainingMenu.
    /// </summary>
    public partial class ResetGuidePanel : Control
    {
        public static Color P1Color = new Color(1f, 0.35f, 0.35f);
        public static Color P2Color = new Color(0.35f, 0.6f, 1f);
        public static Color Panel = new Color(0.03f, 0.04f, 0.07f, 0.88f);
        public static float Width = 250, RowHeight = 30, Gap = 14;

        // Direction (numpad), P1 position, P2 position: 0 = left wall .. 1 = right wall
        static readonly (int dir, float p1, float p2)[] Rows =
        {
            (5, 0.38f, 0.62f), // midscreen
            (4, 0.04f, 0.2f),  // left corner, P1 inside
            (1, 0.2f, 0.04f),  // left corner, P2 inside
            (2, 0.62f, 0.38f), // midscreen, sides switched
            (3, 0.96f, 0.8f),  // right corner, P1 inside
            (6, 0.8f, 0.96f),  // right corner, P2 inside
        };

        PauseMenu _pause;

        public static ResetGuidePanel Attach(PauseMenu pause)
        {
            if (pause == null) return null;
            var p = new ResetGuidePanel { Name = "ResetGuidePanel", _pause = pause, MouseFilter = MouseFilterEnum.Ignore };
            pause.AddChild(p);
            return p;
        }

        public override void _Process(double delta)
        {
            if (_pause == null) return;
            // Centre the pause menu + this card together, so the card fits on screen.
            float total = _pause.Size.X + Gap + Width;
            float wantLeft = (_pause.GetParentAreaSize().X - total) / 2f;
            float dx = wantLeft - _pause.Position.X;
            if (Mathf.Abs(dx) > 1f) { _pause.OffsetLeft += dx; _pause.OffsetRight += dx; }
            Position = new Vector2(_pause.Size.X + Gap, 0);
            Size = new Vector2(Width, 34 + Rows.Length * RowHeight + 8);
            QueueRedraw();
        }

        public override void _Draw()
        {
            var font = ThemeDB.FallbackFont;
            DrawRect(new Rect2(Vector2.Zero, Size), Panel);
            DrawString(font, new Vector2(12, 22), "RESET & REPOSITION", HorizontalAlignment.Left, -1, 15, Colors.White);
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                float y = 34 + i * RowHeight;
                float cy = y + RowHeight / 2;
                // "Back" key
                var key = new Rect2(10, cy - 9, 40, 18);
                DrawRect(key, new Color(1, 1, 1, 0.15f));
                DrawRect(key, new Color(1, 1, 1, 0.6f), false, 1);
                DrawString(font, new Vector2(14, cy + 5), "BACK", HorizontalAlignment.Left, -1, 11, Colors.White);
                DrawString(font, new Vector2(55, cy + 5), "+", HorizontalAlignment.Left, -1, 13, new Color(1, 1, 1, 0.7f));
                DrawArrow(new Vector2(78, cy), row.dir);
                // little stage: walls + floor + the two fighters
                float sx = 100, sw = Width - sx - 12;
                DrawLine(new Vector2(sx, cy + 8), new Vector2(sx + sw, cy + 8), new Color(1, 1, 1, 0.45f), 1);
                DrawLine(new Vector2(sx, cy - 8), new Vector2(sx, cy + 8), new Color(1, 1, 1, 0.45f), 1);
                DrawLine(new Vector2(sx + sw, cy - 8), new Vector2(sx + sw, cy + 8), new Color(1, 1, 1, 0.45f), 1);
                DrawFighter(new Vector2(sx + row.p1 * sw, cy), P1Color, "1");
                DrawFighter(new Vector2(sx + row.p2 * sw, cy), P2Color, "2");
            }
        }

        void DrawFighter(Vector2 p, Color c, string tag)
        {
            DrawCircle(p, 7, c);
            DrawString(ThemeDB.FallbackFont, p + new Vector2(-3.5f, 4.5f), tag, HorizontalAlignment.Left, -1, 11, Colors.Black);
        }

        void DrawArrow(Vector2 c, int dir)
        {
            var col = new Color(1, 1, 1, 0.9f);
            if (dir == 5) { DrawArc(c, 6, 0, Mathf.Tau, 16, col, 2); return; }
            int x = dir == 1 || dir == 4 || dir == 7 ? -1 : dir == 3 || dir == 6 || dir == 9 ? 1 : 0;
            int y = dir <= 3 ? 1 : dir >= 7 ? -1 : 0;
            var v = new Vector2(x, y).Normalized() * 9;
            var tip = c + v;
            DrawLine(c - v, tip, col, 2.5f);
            var side = new Vector2(-v.Y, v.X) * 0.5f;
            DrawLine(tip, tip - v * 0.6f + side, col, 2.5f);
            DrawLine(tip, tip - v * 0.6f - side, col, 2.5f);
        }
    }
}
