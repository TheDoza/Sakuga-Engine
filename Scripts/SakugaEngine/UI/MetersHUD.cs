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
        }

        public void UpdateMeters(SakugaActor[] fighters)
        {
            P1Meter.Value = fighters[0].Parameters.SuperGauge.CurrentValue;
            P2Meter.Value = fighters[1].Parameters.SuperGauge.CurrentValue;

            GetFrameAdvantage(fighters);

            P1InputHistory.SetHistoryList(fighters[0].Inputs);
            P2InputHistory.SetHistoryList(fighters[1].Inputs);

            if (P1Panel != null) P1Panel.UpdateInfo(fighters[0], fighters[1]); // [FF PATCH: training]
            if (P2Panel != null) P2Panel.UpdateInfo(fighters[1], fighters[0]);
            if (P1TrainingInfo != null) P1TrainingInfo.Text = TrainingInfoText(fighters[0], fighters[1]);
            if (P2TrainingInfo != null) P2TrainingInfo.Text = TrainingInfoText(fighters[1], fighters[0]);
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