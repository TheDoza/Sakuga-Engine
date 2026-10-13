using System;
using Godot;
using SakugaEngine.Global;

namespace SakugaEngine.UI
{
    public partial class HealthHUD : Control
    {    
        [ExportCategory("Player 1")]
        [Export] private TextureRect P1Portrait;
        [Export] private TextureProgressBar P1Health;
        [Export] private TextureProgressBar P1LostHealth;
        [Export] private RoundsCounter P1Rounds;
        [Export] private ComboCounter P1Combo;
        [Export] private Label P1Name;
        [Export] private HitNotificationController P1Notif;
        
        [ExportCategory("Player 2")]
        [Export] private TextureRect P2Portrait;
        [Export] private TextureProgressBar P2Health;
        [Export] private TextureProgressBar P2LostHealth;
        [Export] private RoundsCounter P2Rounds;
        [Export] private ComboCounter P2Combo;
        [Export] private Label P2Name;
        [Export] private HitNotificationController P2Notif;

        // [FF PATCH: barrier] Barrier gauge under each health bar (optional: leave empty to hide).
        [ExportCategory("Barrier")]
        [Export] private Godot.Range P1Barrier;
        [Export] private Godot.Range P2Barrier;
        [Export] private Label P1Danger;
        [Export] private Label P2Danger;
        [Export] private Color BarrierColor = new Color(0.2f, 0.55f, 1f);
        [Export] private Color BarrierLowColor = new Color(1f, 0.8f, 0.1f);
        [Export] private Color DangerColor = new Color(0.6f, 0.05f, 0.08f);
        /// <summary>Below this percent the gauge turns from blue to yellow.</summary>
        [Export] private int BarrierLowPercent = 50;

        // [FF PATCH: burst] Burst gauge icons (optional).
        [ExportCategory("Burst")]
        [Export] private FunctionFighter.UI.BurstGaugeHUD P1Burst;
        [Export] private FunctionFighter.UI.BurstGaugeHUD P2Burst;

        [ExportCategory("Extra")]
        [Export] private Label Timer;
        [Export] private Label P1Debug;
        [Export] private Label P2Debug;

        public void Setup(SakugaActor[] fighters)
        {
            P1Health.MaxValue = fighters[0].Data.MaxHealth;
            P2Health.MaxValue = fighters[1].Data.MaxHealth;
            
            if (fighters[0].Data.Profile.Portrait != null)
            {
                P1Portrait.Texture = fighters[0].Data.Profile.Portrait;
                P1Name.Text = fighters[0].Data.Profile.ShortName;
            }

            if (fighters[1].Data.Profile.Portrait != null)
            {
                P2Portrait.Texture = fighters[1].Data.Profile.Portrait;
                P2Name.Text = fighters[1].Data.Profile.ShortName;
            }

            P1Rounds.Setup();
            P2Rounds.Setup();

            int barrierMax = FunctionFighter.Mechanics.BarrierSettings.Get().MaxValue; // [FF PATCH: barrier]
            if (P1Barrier != null) P1Barrier.MaxValue = barrierMax;
            if (P2Barrier != null) P2Barrier.MaxValue = barrierMax;
        }

        // [FF PATCH: barrier] blue when healthy, yellow when low, dark red in Danger
        private void UpdateBarrier(Godot.Range bar, Label danger, SakugaActor fighter)
        {
            if (bar == null) return;
            var b = fighter.Barrier;
            bar.Value = b.Value;
            bool low = b.Value * 100 < BarrierLowPercent * bar.MaxValue;
            bar.SelfModulate = b.Danger ? DangerColor : (low ? BarrierLowColor : BarrierColor);
            if (danger != null) danger.Visible = b.Danger;
        }

        public void UpdateHealthBars(SakugaActor[] fighters, GameMonitor monitor)
        {
            P1Health.Value = fighters[0].Parameters.Health.CurrentValue;
            P2Health.Value = fighters[1].Parameters.Health.CurrentValue;
            P1LostHealth.Value = fighters[0].Parameters.Health.LostValue;
            P2LostHealth.Value = fighters[1].Parameters.Health.LostValue;

            UpdateTimer(monitor);

            P1Rounds.ShowRounds(monitor.VictoryCounter[0]);
            P2Rounds.ShowRounds(monitor.VictoryCounter[1]);

            P1Combo.Visible = fighters[1].Parameters.Tracker.HitCombo > 0;
            P2Combo.Visible = fighters[0].Parameters.Tracker.HitCombo > 0;

            P1Combo.UpdateCounter((int)fighters[1].Hitstun.TimeLeft, fighters[1].Parameters.Tracker);
            P2Combo.UpdateCounter((int)fighters[0].Hitstun.TimeLeft, fighters[0].Parameters.Tracker);

            UpdateBarrier(P1Barrier, P1Danger, fighters[0]); // [FF PATCH: barrier]
            UpdateBarrier(P2Barrier, P2Danger, fighters[1]);
            if (P1Burst != null) P1Burst.UpdateGauge(fighters[0]); // [FF PATCH: burst]
            if (P2Burst != null) P2Burst.UpdateGauge(fighters[1]);

            UpdateDebug(fighters);
        }

        public void UpdateDebug(SakugaActor[] fighters)
        {
            P1Debug.Text = fighters[0].DebugInfo();
            P2Debug.Text = fighters[1].DebugInfo();
        }

        public void UpdateTimer(GameMonitor monitor)
        {
            if (monitor.ClockLimit < 0)
            {
                Timer.Text = "--";
                return;
            }
            int time = monitor.Clock / GlobalVariables.TicksPerSecond;
            time = Mathf.Clamp(time, 0, monitor.ClockLimit);
            Timer.Text = time.ToString();
        }

        public void UpdateHitNotifications(HitNotifications[] notifs)
        {
            P1Notif.View(notifs[0].Elements);
            P2Notif.View(notifs[1].Elements);
        }
    }
}
