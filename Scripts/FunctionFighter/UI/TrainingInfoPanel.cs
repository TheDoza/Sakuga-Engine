using Godot;
using SakugaEngine;
using FunctionFighter.Mechanics;

namespace FunctionFighter.UI
{
    /// <summary>
    /// Training data panel (one per player): damage, combo damage, hit type, scaling, frame data, advantage and
    /// counter type of the last hit. Builds its own rows: names left-aligned, values right-aligned on the same line.
    /// Lives at Sakuga_UI.tscn → GameHUD_Foreground/TrainingInfo/P1Info and P2Info; MetersHUD fills it every frame.
    /// </summary>
    [GlobalClass]
    public partial class TrainingInfoPanel : Control
    {
        [Export] public int FontSize = 14;
        [Export] public int RowHeight = 18;
        [Export] public int PanelWidth = 190;
        [Export] public Color BackgroundColor = new Color(0, 0, 0, 0.55f);
        [Export] public Color PlusColor = new Color(0.45f, 1f, 0.45f);
        [Export] public Color MinusColor = new Color(1f, 0.45f, 0.4f);
        [Export] public Color CounterColor = new Color(1f, 0.55f, 0.2f);

        static readonly string[] RowNames = { "Damage", "Combo damage", "Max combo", "Hit type", "Scaling", "Frame data", "Advantage", "Counter" };

        Label[] _values;

        public override void _Ready()
        {
            Build();
        }

        void Build()
        {
            if (_values != null) return;
            const int pad = 6;
            CustomMinimumSize = new Vector2(PanelWidth, RowNames.Length * RowHeight + pad * 2);
            Size = CustomMinimumSize;

            var bg = new ColorRect { Color = BackgroundColor, MouseFilter = MouseFilterEnum.Ignore };
            bg.Position = Vector2.Zero;
            bg.Size = Size;
            AddChild(bg);

            _values = new Label[RowNames.Length];
            for (int i = 0; i < RowNames.Length; i++)
            {
                float y = pad + i * RowHeight;
                var name = MakeLabel(RowNames[i], HorizontalAlignment.Left);
                name.Position = new Vector2(pad + 2, y);
                name.Size = new Vector2(PanelWidth * 0.6f, RowHeight);
                AddChild(name);

                var value = MakeLabel("0", HorizontalAlignment.Right);
                value.Position = new Vector2(PanelWidth * 0.55f, y);
                value.Size = new Vector2(PanelWidth * 0.45f - pad - 2, RowHeight);
                AddChild(value);
                _values[i] = value;
            }
        }

        Label MakeLabel(string text, HorizontalAlignment align)
        {
            var l = new Label { Text = text, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            l.AddThemeFontSizeOverride("font_size", FontSize);
            l.AddThemeConstantOverride("outline_size", 3);
            l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
            return l;
        }

        /// <param name="owner">The player this panel belongs to (their frame data / advantage).</param>
        /// <param name="reference">The opponent (the hits this player landed on them).</param>
        public void UpdateInfo(SakugaActor owner, SakugaActor reference)
        {
            if (_values == null) Build();
            if (owner == null || reference == null || owner.Parameters == null || reference.Parameters == null) return;
            var t = reference.Parameters.Tracker;

            _values[0].Text = t.LastDamage.ToString();
            _values[1].Text = t.CurrentCombo.ToString();
            _values[2].Text = t.HighestCombo.ToString();
            _values[3].Text = HitTypeName(t.LastHitType);
            _values[4].Text = reference.Parameters.Prorations.CurrentDamageScaling + "%";
            _values[5].Text = owner.Parameters.Tracker.FrameData.ToString();

            // The frame meter's advantage counts the frames neither side can act (block / getup exits), so it's the real
            // "who moves first" number; the engine tracker is the fallback before the meter has one.
            int adv = owner.Parameters.Tracker.FrameAdvantage;
            int id = (int)owner.playerID;
            if (id >= 0 && id < 2)
            {
                var m = FunctionFighter.Training.FrameMeter.GetSummary(id);
                if (m.HasAdvantage) adv = m.Advantage;
            }
            _values[6].Text = adv > 0 ? "+" + adv : adv.ToString();
            _values[6].AddThemeColorOverride("font_color", adv > 0 ? PlusColor : adv < 0 ? MinusColor : Colors.White);

            CounterKind k = reference.Counter.LastKind;
            _values[7].Text = CounterName(k);
            _values[7].AddThemeColorOverride("font_color", k == CounterKind.None ? Colors.White : CounterColor);
        }

        static string HitTypeName(int t)
        {
            switch (t)
            {
                case 0: return "HIGH";
                case 1: return "MID";
                case 2: return "LOW";
                case 3: return "UNBL.";
                default: return "-";
            }
        }

        static string CounterName(CounterKind k)
        {
            switch (k)
            {
                case CounterKind.Counter: return "Counter";
                case CounterKind.Carry: return "Carry";
                case CounterKind.BigCounter: return "Big";
                case CounterKind.Functional: return "Functional";
                case CounterKind.Punish: return "Punish";
                default: return "-";
            }
        }
    }
}
