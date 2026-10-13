using Godot;
using SakugaEngine.Game;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Big on-screen Training indicators under the timer:
    /// - recording countdown "RECORDING IN 3 / 2 / 1", then "REC  2.4 s" (red) while recording;
    /// - "PLAYBACK" (green) while the dummy plays the recording;
    /// - "READY" / "GO!" after a reset with Play on Reset: After READY / GO!;
    /// - a short popup when the dummy does a reversal or a tech ("REVERSAL  5B Test Mid", "AIR TECH").
    /// Created by TrainingMenu in Training only. Visual only.
    /// </summary>
    public partial class TrainingStatusHUD : Control
    {
        public static Color RecordColor = new Color(1f, 0.3f, 0.3f);
        public static Color PlayColor = new Color(0.4f, 1f, 0.5f);
        public static Color CallColor = new Color(1f, 0.85f, 0.25f);
        public static Color ReversalColor = new Color(0.45f, 0.85f, 1f);
        public static Color FrameAdvanceColor = new Color(0.4f, 1f, 0.95f);

        Label _main, _popup;

        public static TrainingStatusHUD Attach(Node uiRoot)
        {
            if (uiRoot == null) return null;
            var hud = new TrainingStatusHUD { Name = "TrainingStatusHUD" };
            uiRoot.AddChild(hud);
            return hud;
        }

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _main = MakeLabel(34, 160);
            _popup = MakeLabel(20, 204);
        }

        Label MakeLabel(int size, float y)
        {
            var l = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            l.AddThemeFontSizeOverride("font_size", size);
            l.AddThemeConstantOverride("outline_size", 8);
            l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
            l.AnchorLeft = 0; l.AnchorRight = 1; l.AnchorTop = 0; l.AnchorBottom = 0;
            l.OffsetLeft = 0; l.OffsetRight = 0; l.OffsetTop = y; l.OffsetBottom = y + size + 12;
            AddChild(l);
            return l;
        }

        public override void _Process(double delta)
        {
            if (!TrainingController.Active) { Visible = false; return; }
            Visible = true;

            string text = "";
            Color color = Colors.White;
            int call = TrainingController.RoundCallLeft;
            switch (TrainingController.Recording)
            {
                case RecordState.Countdown:
                    int secs = (TrainingController.CountdownLeft + 29) / 30;
                    text = "RECORDING IN " + Mathf.Max(1, secs);
                    color = RecordColor;
                    break;
                case RecordState.Recording:
                    float t = TrainingController.RecordedFrames / 60f;
                    float max = TrainingController.MaxRecordFrames / 60f;
                    text = $"REC SLOT {TrainingController.RecordSlot + 1}   {t:0.0} s  /  {max:0} s";
                    // blink
                    color = RecordColor;
                    color.A = (Time.GetTicksMsec() / 400) % 2 == 0 ? 1f : 0.55f;
                    break;
                case RecordState.Playing:
                    text = TrainingController.PlayingSlot >= 0 ? $"PLAYBACK  SLOT {TrainingController.PlayingSlot + 1}" : "PLAYBACK";
                    color = PlayColor;
                    break;
            }
            if (FrameAdvance.Frozen)
            {
                text = $"FRAME ADVANCE  +{FrameAdvance.Stepped}" + (text != "" ? "   ·   " + text : "");
                color = FrameAdvanceColor;
            }
            if (call > 0)
            {
                text = call >= TrainingController.GoFrames ? "READY" : "GO!";
                color = CallColor;
            }
            _main.Text = text;
            _main.AddThemeColorOverride("font_color", color);

            float notice = SaveStates.NoticeAlpha;
            if (notice > 0)
            {
                _popup.Text = SaveStates.Notice;
                var c = SaveStates.NoticeColor;
                c.A = notice;
                _popup.AddThemeColorOverride("font_color", c);
                _popup.Visible = true;
            }
            else if (Reversal.PopupLeft > 0)
            {
                _popup.Text = Reversal.PopupText;
                var c = ReversalColor;
                c.A = Mathf.Clamp(Reversal.PopupLeft / 15f, 0f, 1f);
                _popup.AddThemeColorOverride("font_color", c);
                _popup.Visible = true;
            }
            else _popup.Visible = false;
        }
    }
}
