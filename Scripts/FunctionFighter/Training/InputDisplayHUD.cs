using Godot;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Live input display for each player: an arcade-stick gate with the ball where the stick is (as seen on screen),
    /// and A / B / C / D buttons that light up while held (macro buttons E–H light the buttons they press).
    /// Sits above each player's training data panel. Display → P1 / P2 Input Display turn each one on/off.
    /// Shows the input the fighter actually received (so the dummy's recording / reversal inputs show too).
    /// </summary>
    public partial class InputDisplayHUD : Control
    {
        public static Color GateColor = new Color(0f, 0f, 0f, 0.6f);
        public static Color GateEdge = new Color(1f, 1f, 1f, 0.35f);
        public static Color BallColor = new Color(1f, 1f, 1f);
        public static Color OffColor = new Color(0.2f, 0.2f, 0.22f, 0.8f);
        public static Color[] ButtonColors =
        {
            new Color(1f, 0.35f, 0.35f),  // A
            new Color(1f, 0.85f, 0.3f),   // B
            new Color(0.35f, 0.9f, 0.45f), // C
            new Color(0.35f, 0.6f, 1f),   // D
        };
        /// <summary>Centre of each player's display, in 1280x720 HUD pixels.</summary>
        public static Vector2 P1Center = new Vector2(223, 440), P2Center = new Vector2(1057, 440);

        public static InputDisplayHUD Attach(Node uiRoot)
        {
            if (uiRoot == null) return null;
            var hud = new InputDisplayHUD { Name = "InputDisplayHUD" };
            uiRoot.AddChild(hud);
            return hud;
        }

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }

        public override void _Process(double delta)
        {
            Visible = TrainingController.Active && (TrainingSettings.ShowP1Inputs || TrainingSettings.ShowP2Inputs);
            if (Visible) QueueRedraw();
        }

        static PlayerInputs Current(SakugaActor a)
        {
            if (a?.Inputs?.InputHistory == null) return 0;
            var i = a.Inputs.InputHistory[a.Inputs.CurrentHistory].rawInput;
            // Macro buttons light the buttons they stand for (E = A+B, F = B+C, G = A+B+C, H = A+B+C+D).
            return FunctionFighter.Input.ButtonMacros.Expand(i);
        }

        public override void _Draw()
        {
            var f = GameManager.Instance?.GetFighters();
            if (f == null || f.Length < 2) return;
            float scale = GetViewportRect().Size.X / 1280f;
            if (TrainingSettings.ShowP1Inputs) DrawPlayer(Current(f[0]), P1Center * scale, scale, "P1");
            if (TrainingSettings.ShowP2Inputs) DrawPlayer(Current(f[1]), P2Center * scale, scale, "P2");
        }

        void DrawPlayer(PlayerInputs i, Vector2 c, float s, string tag)
        {
            // Stick gate
            Vector2 gate = c + new Vector2(-48, 0) * s;
            float r = 24 * s;
            DrawCircle(gate, r, GateColor);
            DrawArc(gate, r, 0, Mathf.Tau, 32, GateEdge, 2 * s);
            int x = ((i & PlayerInputs.RIGHT) != 0 ? 1 : 0) - ((i & PlayerInputs.LEFT) != 0 ? 1 : 0);
            int y = ((i & PlayerInputs.DOWN) != 0 ? 1 : 0) - ((i & PlayerInputs.UP) != 0 ? 1 : 0);
            var dir = new Vector2(x, y);
            if (dir != Vector2.Zero) dir = dir.Normalized();
            Vector2 ball = gate + dir * r * 0.62f;
            DrawLine(gate, ball, new Color(1, 1, 1, 0.5f), 3 * s);
            DrawCircle(ball, 8 * s, BallColor);

            // Buttons: A B on top, C D underneath (arcade layout)
            PlayerInputs[] bits = { PlayerInputs.FACE_A, PlayerInputs.FACE_B, PlayerInputs.FACE_C, PlayerInputs.FACE_D };
            string[] names = { "A", "B", "C", "D" };
            Vector2[] pos = { new(-6, -12), new(18, -14), new(-2, 12), new(22, 10) };
            var font = ThemeDB.FallbackFont;
            for (int b = 0; b < 4; b++)
            {
                bool on = (i & bits[b]) != 0;
                Vector2 p = c + pos[b] * s + new Vector2(10, 0) * s;
                DrawCircle(p, 10 * s, on ? ButtonColors[b] : OffColor);
                DrawArc(p, 10 * s, 0, Mathf.Tau, 24, on ? Colors.White : ButtonColors[b] * new Color(1, 1, 1, 0.6f), 2 * s);
                DrawString(font, p + new Vector2(-4, 5) * s, names[b], HorizontalAlignment.Left, -1, (int)(12 * s), on ? Colors.Black : new Color(1, 1, 1, 0.8f));
            }
            DrawString(font, c + new Vector2(-84, -22) * s, tag, HorizontalAlignment.Left, -1, (int)(12 * s), new Color(1, 1, 1, 0.8f));
        }
    }
}
