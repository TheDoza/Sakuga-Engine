using Godot;
using SakugaEngine;
using FunctionFighter.Mechanics;

namespace FunctionFighter.UI
{
    /// <summary>
    /// Round Burst gauge icon (BBCF-style): a ring that fills clockwise with the 10%-step number in the middle,
    /// solid orange with "BURST" when full, and crossed out in red when Burst is disabled.
    /// Set from HealthHUD every rendered frame.
    /// </summary>
    [GlobalClass]
    public partial class BurstGaugeHUD : Control
    {
        [Export] public Color BackColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        [Export] public Color FillColor = new Color(0.25f, 0.6f, 1f);
        [Export] public Color FullColor = new Color(1f, 0.55f, 0.1f);
        [Export] public Color DisabledColor = new Color(0.9f, 0.1f, 0.1f);
        [Export] public Color TextColor = new Color(1f, 1f, 1f);

        private int _value, _max = 1;
        private bool _disabled;

        public void UpdateGauge(SakugaActor fighter)
        {
            int value = fighter.Burst.Value;
            int max = BurstSettings.Get().MaxValue;
            bool disabled = Burst.IsDisabled(fighter);
            if (value == _value && max == _max && disabled == _disabled) return;
            _value = value; _max = Mathf.Max(1, max); _disabled = disabled;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 c = Size / 2f;
            float r = Mathf.Min(Size.X, Size.Y) / 2f - 2f;
            bool full = _value >= _max;
            float fraction = Mathf.Clamp(_value / (float)_max, 0f, 1f);

            DrawCircle(c, r, BackColor);
            if (full)
                DrawCircle(c, r * 0.82f, FullColor);
            else if (fraction > 0f)
                DrawArc(c, r * 0.78f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * fraction, 48, FillColor, r * 0.28f);
            DrawArc(c, r, 0, Mathf.Tau, 48, full ? FullColor : FillColor.Darkened(0.3f), 2f);

            Font font = ThemeDB.FallbackFont;
            string text = full ? "BURST" : (_value * 10 / _max).ToString();
            int fontSize = full ? Mathf.RoundToInt(r * 0.42f) : Mathf.RoundToInt(r * 0.9f);
            Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
            Vector2 textPos = c + new Vector2(-textSize.X / 2f, textSize.Y / 2f - font.GetDescent(fontSize));
            DrawStringOutline(font, textPos, text, HorizontalAlignment.Left, -1, fontSize, 4, Colors.Black);
            DrawString(font, textPos, text, HorizontalAlignment.Left, -1, fontSize, TextColor);

            if (_disabled)
            {
                float d = r * 0.7f;
                DrawLine(c + new Vector2(-d, -d), c + new Vector2(d, d), DisabledColor, r * 0.22f, true);
                DrawLine(c + new Vector2(d, -d), c + new Vector2(-d, d), DisabledColor, r * 0.22f, true);
            }
        }
    }
}
