using Godot;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Draws the frame meter (FrameMeter data): two rows of cells, P1 over P2, above the bottom meters, with a summary
    /// line for each player (Startup / Active / Recovery / Advantage). Colors:
    /// green startup · red active · blue recovery · yellow hitstun · orange blockstun · purple knockdown ·
    /// teal movement you can't act out of (prejump, backdash, techs, exits) · dark = free.
    /// White stripe on top = strike invincible, orange stripe at the bottom = throw invincible.
    /// Created by TrainingMenu; Display → Frame Meter turns it on/off. Visual only.
    /// </summary>
    public partial class FrameMeterHUD : Control
    {
        public static int CellWidth = 8, CellHeight = 14, RowGap = 4;
        /// <summary>Top of the P1 row, in 1280x720 HUD pixels.</summary>
        public static float Top = 574;

        public static Color FreeColor = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        public static Color MovementColor = new Color(0.27f, 0.75f, 0.72f);
        public static Color StartupColor = new Color(0.2f, 0.82f, 0.45f);
        public static Color ActiveColor = new Color(1f, 0.3f, 0.3f);
        public static Color RecoveryColor = new Color(0.25f, 0.55f, 1f);
        public static Color HitstunColor = new Color(1f, 0.85f, 0.3f);
        public static Color BlockstunColor = new Color(1f, 0.62f, 0.2f);
        public static Color KnockdownColor = new Color(0.68f, 0.42f, 1f);
        public static Color InvulnColor = new Color(1f, 1f, 1f);
        public static Color ThrowInvulnColor = new Color(1f, 0.6f, 0.15f);

        Label _p1, _p2;

        public static FrameMeterHUD Attach(Node uiRoot)
        {
            if (uiRoot == null) return null;
            var hud = new FrameMeterHUD { Name = "FrameMeterHUD" };
            uiRoot.AddChild(hud);
            return hud;
        }

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _p1 = MakeLabel(Top - 18);
            _p2 = MakeLabel(Top + 2 * CellHeight + RowGap + 2);
        }

        Label MakeLabel(float y)
        {
            var l = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            l.AddThemeFontSizeOverride("font_size", 13);
            l.AddThemeConstantOverride("outline_size", 5);
            l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
            l.AnchorLeft = 0; l.AnchorRight = 1; l.AnchorTop = 0; l.AnchorBottom = 0;
            l.OffsetLeft = 0; l.OffsetRight = 0; l.OffsetTop = y; l.OffsetBottom = y + 18;
            AddChild(l);
            return l;
        }

        public override void _Process(double delta)
        {
            Visible = TrainingController.Active && TrainingSettings.ShowFrameMeter;
            if (!Visible) return;
            _p1.Text = SummaryText(0);
            _p2.Text = SummaryText(1);
            QueueRedraw();
        }

        static string SummaryText(int p)
        {
            var s = FrameMeter.GetSummary(p);
            string text = p == 0 ? "P1" : "P2";
            if (s.HasAttack) text += $"   Startup {s.Startup}   Active {s.Active}   Recovery {s.Recovery}";
            if (s.HasAdvantage) text += "   Advantage " + (s.Advantage > 0 ? "+" : "") + s.Advantage;
            return text;
        }

        static Color KindColor(FrameKind k)
        {
            switch (k)
            {
                case FrameKind.Movement: return MovementColor;
                case FrameKind.Startup: return StartupColor;
                case FrameKind.Active: return ActiveColor;
                case FrameKind.Recovery: return RecoveryColor;
                case FrameKind.Hitstun: return HitstunColor;
                case FrameKind.Blockstun: return BlockstunColor;
                case FrameKind.Knockdown: return KnockdownColor;
                default: return FreeColor;
            }
        }

        public override void _Draw()
        {
            float width = FrameMeter.Columns * CellWidth;
            // Scale with the viewport so it stays centred at any resolution (the HUD is laid out for 1280x720).
            var vp = GetViewportRect().Size;
            float scale = vp.X / 1280f;
            float left = (vp.X - width * scale) / 2f;
            DrawRect(new Rect2(left - 3, Top * scale - 3, width * scale + 6, (2 * CellHeight + RowGap) * scale + 6), new Color(0, 0, 0, 0.55f));
            for (int p = 0; p < 2; p++)
            {
                float y = (Top + p * (CellHeight + RowGap)) * scale;
                for (int i = 0; i < FrameMeter.Columns; i++)
                {
                    var r = new Rect2(left + i * CellWidth * scale, y, (CellWidth - 1) * scale, CellHeight * scale);
                    if (i >= FrameMeter.Count) { DrawRect(r, FreeColor); continue; }
                    var c = FrameMeter.Cells[p][i];
                    DrawRect(r, KindColor(c.Kind));
                    if (c.Invuln) DrawRect(new Rect2(r.Position, new Vector2(r.Size.X, 3 * scale)), InvulnColor);
                    if (c.ThrowInvuln) DrawRect(new Rect2(r.Position + new Vector2(0, r.Size.Y - 3 * scale), new Vector2(r.Size.X, 3 * scale)), ThrowInvulnColor);
                }
            }
        }
    }
}
