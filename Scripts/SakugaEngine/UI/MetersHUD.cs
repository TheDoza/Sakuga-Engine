using Godot;

namespace SakugaEngine.UI
{
    public partial class MetersHUD : Control
    {
        [Export] private TextureProgressBar P1Meter;
        [Export] private TextureProgressBar P2Meter;
        [Export] private Label P1TrainingInfo;
        [Export] private Label P2TrainingInfo;
        [Export] private InputHistory P1InputHistory;
        [Export] private InputHistory P2InputHistory;
        // [FF PATCH: training] aligned training data panels (replace the old two-label layout)
        [Export] private FunctionFighter.UI.TrainingInfoPanel P1Panel;
        [Export] private FunctionFighter.UI.TrainingInfoPanel P2Panel;

        [ExportGroup("Super Meter Colors")]
        /// <summary>0% up to (not including) BluePercent.</summary>
        [Export] public Color GreenTint = new Color(0.25f, 0.90f, 0.30f);
        [Export] public Color BlueTint = new Color(0.20f, 0.50f, 1.00f);
        [Export] public Color RedTint = new Color(1.00f, 0.25f, 0.25f);
        [Export] public Color PurpleTint = new Color(0.70f, 0.30f, 1.00f);
        [Export] public Color GoldTint = new Color(1.00f, 0.85f, 0.20f);
        /// <summary>Percent of max meter where each colour starts. Gold is only at 100 (full).</summary>
        [Export] public int BluePercent = 25;
        [Export] public int RedPercent = 50;
        [Export] public int PurplePercent = 75;
        [Export] public int GoldPercent = 100;

        // [FF PATCH: ex buff] Icon next to the super meter for fighters with a free-EX buff variable (Basic Bro's 5D).
        // Colored while the buff is stocked, grey while it isn't, hidden for fighters without the variable.
        [ExportGroup("EX Buff Icon")]
        [Export] public Texture2D BuffIconStocked;
        [Export] public Texture2D BuffIconEmpty;
        [Export] public string BuffVariableName = "EXFree";
        [Export] public Vector2 BuffIconSize = new Vector2(48, 48);
        /// <summary>Space between the meter bar and the icon (P1: left of the bar, P2: right of the bar).</summary>
        [Export] public float BuffIconGap = 10f;
        /// <summary>Extra nudge after placing the icon beside the bar (x is mirrored for P2, so + moves both away from the bar).</summary>
        [Export] public Vector2 BuffIconOffset = Vector2.Zero;
        private TextureRect P1BuffIcon;
        private TextureRect P2BuffIcon;

        private int CurrentFrameAdvantage;

        /*public override void _Ready()
        {
            P1Meter = GetNode<TextureProgressBar>("Meters/P1Meter");
            P2Meter = GetNode<TextureProgressBar>("Meters/P2Meter");
            P1TrainingInfo = GetNode<Label>("TrainingInfo/P1Info/Information");
            P2TrainingInfo = GetNode<Label>("TrainingInfo/P2Info/Information");
        }*/

        public void Setup(SakugaActor[] fighters)
        {
            P1Meter.MaxValue = fighters[0].Data.MaxSuperGauge;
            P2Meter.MaxValue = fighters[1].Data.MaxSuperGauge;

            // [FF PATCH: ex buff]
            BuffIconStocked ??= GD.Load<Texture2D>("res://Sprites/UI/BB_ExBuff.png");
            BuffIconEmpty ??= GD.Load<Texture2D>("res://Sprites/UI/BB_ExBuff_Grey.png");
            P1BuffIcon ??= MakeBuffIcon(P1Meter, false);
            P2BuffIcon ??= MakeBuffIcon(P2Meter, true);
        }

        // [FF PATCH: ex buff] The icon is a sibling of the meter, not a child: the meter bar is scaled down (0.14 x 0.07)
        // and a child would be scaled with it to almost nothing.
        private TextureRect MakeBuffIcon(TextureProgressBar meter, bool mirrored)
        {
            if (meter == null) return null;
            var icon = new TextureRect
            {
                Name = mirrored ? "P2EXBuffIcon" : "P1EXBuffIcon",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                Size = BuffIconSize,
                Visible = false,
            };
            Node parent = meter.GetParent() ?? this;
            parent.CallDeferred(Node.MethodName.AddChild, icon); // deferred: Setup can run while the HUD is still being built
            PlaceBuffIcon(icon, meter, mirrored);
            return icon;
        }

        // [FF PATCH: ex buff] P1: left of the bar, P2: right of the bar, centred on the bar's height. Done every update so it
        // follows the bar if the layout changes (window resize, anchors).
        private void PlaceBuffIcon(TextureRect icon, TextureProgressBar meter, bool mirrored)
        {
            if (icon == null || meter == null) return;
            Vector2 barSize = meter.Size * meter.Scale;
            Vector2 barPos = meter.Position;
            // A negative scale draws the bar to the left / above its position.
            if (barSize.X < 0) { barPos.X += barSize.X; barSize.X = -barSize.X; }
            if (barSize.Y < 0) { barPos.Y += barSize.Y; barSize.Y = -barSize.Y; }
            icon.Size = BuffIconSize;
            float x = mirrored ? barPos.X + barSize.X + BuffIconGap + BuffIconOffset.X
                               : barPos.X - BuffIconSize.X - BuffIconGap - BuffIconOffset.X;
            float y = barPos.Y + (barSize.Y - BuffIconSize.Y) * 0.5f + BuffIconOffset.Y;
            icon.Position = new Vector2(x, y);
        }

        // [FF PATCH: ex buff]
        private void UpdateBuffIcon(TextureRect icon, SakugaActor fighter)
        {
            if (icon == null) return;
            PlaceBuffIcon(icon, icon == P2BuffIcon ? P2Meter : P1Meter, icon == P2BuffIcon);
            var parameters = fighter?.Parameters;
            CustomVariable buff = parameters != null && parameters.HasVariables() ? parameters.GetVariable(BuffVariableName) : null;
            icon.Visible = buff != null;
            if (buff == null) return;
            icon.Texture = buff.CurrentValue > 0 ? BuffIconStocked : BuffIconEmpty;
        }

        public void UpdateMeters(SakugaActor[] fighters)
        {
            int p1Super = fighters[0].Parameters.SuperGauge.CurrentValue;
            int p2Super = fighters[1].Parameters.SuperGauge.CurrentValue;

            P1Meter.Value = p1Super;
            P2Meter.Value = p2Super;
            P1Meter.TintProgress = GetSuperTint(p1Super, fighters[0].Data.MaxSuperGauge);
            P2Meter.TintProgress = GetSuperTint(p2Super, fighters[1].Data.MaxSuperGauge);
            UpdateBuffIcon(P1BuffIcon, fighters[0]); // [FF PATCH: ex buff]
            UpdateBuffIcon(P2BuffIcon, fighters[1]);

            GetFrameAdvantage(fighters);

            P1InputHistory.SetHistoryList(fighters[0].Inputs);
            P2InputHistory.SetHistoryList(fighters[1].Inputs);

            if (P1Panel != null) P1Panel.UpdateInfo(fighters[0], fighters[1]); // [FF PATCH: training]
            if (P2Panel != null) P2Panel.UpdateInfo(fighters[1], fighters[0]);
            if (P1TrainingInfo != null) P1TrainingInfo.Text = TrainingInfoText(fighters[0], fighters[1]);
            if (P2TrainingInfo != null) P2TrainingInfo.Text = TrainingInfoText(fighters[1], fighters[0]);
        }

        /// <summary>Picks the meter colour from how full the super gauge is. Whole-number percent, so 24.9% counts as 24%.</summary>
        private Color GetSuperTint(int value, int max)
        {
            if (max <= 0) return GreenTint;

            int percent = System.Math.Clamp(value, 0, max) * 100 / max;

            if (percent >= GoldPercent) return GoldTint;
            if (percent >= PurplePercent) return PurpleTint;
            if (percent >= RedPercent) return RedTint;
            if (percent >= BluePercent) return BlueTint;
            return GreenTint;
        }

        void GetFrameAdvantage(SakugaActor[] fighters)
        {
            for (int i = 0; i < fighters.Length; i++)
            {
                if (fighters[i].Parameters.Tracker.FrameAdvantage != 0)
                    CurrentFrameAdvantage = fighters[i].Parameters.Tracker.FrameAdvantage;
            }
        }

        private string TrainingInfoText(SakugaActor owner, SakugaActor reference)
        {
            string hitTypeText = "";

            switch (reference.Parameters.Tracker.LastHitType)
            {
                case 0:
                    hitTypeText = "HIGH";
                    break;
                case 1:
                    hitTypeText = "MID";
                    break;
                case 2:
                    hitTypeText = "LOW";
                    break;
                case 3:
                    hitTypeText = "UNBL.";
                    break;
            }

            int finalFrameAdv = owner.Parameters.Tracker.FrameAdvantage;// != 0 ? CurrentFrameAdvantage : CurrentFrameAdvantage;

            string frameAdvantageInfo = finalFrameAdv >= 0 ?
                    ("+" + finalFrameAdv) : "" + finalFrameAdv;

            string frameAdvText = "(" + frameAdvantageInfo + ")";

            return reference.Parameters.Tracker.LastDamage + "\n" +
                    reference.Parameters.Tracker.CurrentCombo + "\n" +
                    reference.Parameters.Tracker.HighestCombo + "\n" +
                    hitTypeText + "\n" +
                    reference.Parameters.Prorations.CurrentDamageScaling + "%\n" +
                    owner.Parameters.Tracker.FrameData + frameAdvText;
        }
    }
}
