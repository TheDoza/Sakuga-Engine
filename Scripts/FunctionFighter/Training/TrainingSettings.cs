using Godot;

namespace FunctionFighter.Training
{
    public enum DummyMode { Stand, Crouch, Jump, Controller, CPU, Playback, WalkForward, WalkBack, Run, Backdash, JumpForward, JumpBack, SuperJump, DoubleJump, AirDash, RandomMovement }
    /// <summary>New modes are added at the end so saved settings keep their meaning.</summary>
    public enum GuardMode { None, All, AfterFirstHit, Random, FirstHitOnly, BlockXHits, SwitchStandToCrouch, SwitchCrouchToStand }
    public enum ReversalMode { Off, Action, Recording }
    public enum AirTechMode { Off, Neutral, Forward, Back, Random }
    public enum GroundTechMode { Off, Neutral, ForwardRoll, BackRoll, Emergency, Random }
    public enum PlayOnResetMode { Off, Immediately, AfterRoundCall }
    public enum CounterMode { Normal, Counter, BigCounter, Off }
    public enum DummyBarrierMode { Normal, Infinite, Danger }
    public enum FunctionalCounterMode { Default, On, Off }
    /// <summary>LeftCorner / RightCorner = the dummy (P2) inside the corner; P1LeftCorner / P1RightCorner = P1 inside. CenterSwapped = P1 on the right.</summary>
    /// <summary>Normal = the meter works as in a match. Infinite = always full. RefillAfterCombo = goes back to the refill
    /// amount once both fighters have been free for a moment (after every combo / block string), so you can practise
    /// combos with an exact amount of meter.</summary>
    public enum MeterMode { Normal, Infinite, RefillAfterCombo }
    public enum ResetPosition { Center, LeftCorner, RightCorner, P1LeftCorner, P1RightCorner, CenterSwapped }

    /// <summary>
    /// The Training Settings page values (pause menu in Training). Saved to user://training_settings.cfg so they
    /// stay between sessions. Only used in a game mode with IsTraining ticked (GameModes/GM_Training.tres).
    /// The dummy is always player 2.
    /// </summary>
    public static class TrainingSettings
    {
        const string FilePath = "user://training_settings.cfg";
        const string Section = "training";

        // Dummy
        public static DummyMode Dummy = DummyMode.Controller;
        public static GuardMode Guard = GuardMode.None;
        public static CounterMode Counter = CounterMode.Normal;
        public static FunctionalCounterMode FunctionalCounter = FunctionalCounterMode.Default;
        public static DummyBarrierMode DummyBarrier = DummyBarrierMode.Normal;
        /// <summary>Hits blocked before the dummy stops guarding (Guard: Block X Hits).</summary>
        public static int GuardHits = 3;
        public const int MaxGuardHits = 10;

        // Reversal
        public static ReversalMode Reversal = ReversalMode.Off;
        /// <summary>MoveName of the reversal action (from the dummy's reversal list). Empty = the first one.</summary>
        public static string ReversalMove = "";
        public static bool ReversalOnWakeup = true;
        public static bool ReversalOnBlock = true;
        public static bool ReversalOnHit = true;
        public static bool ReversalOnAirTech = true;
        /// <summary>Frames to wait after the first possible frame (0 = frame 1).</summary>
        public static int ReversalDelay = 0;
        public const int MaxReversalDelay = 60;
        public static AirTechMode AirTech = AirTechMode.Off;
        public static GroundTechMode GroundTech = GroundTechMode.Off;
        /// <summary>Frames the dummy waits after a tech becomes possible (0 = first frame).</summary>
        public static int TechDelay = 0;
        public const int MaxTechDelay = 60;

        // Recording
        public static bool RecordCountdown = true;
        public static PlayOnResetMode PlayOnReset = PlayOnResetMode.Off;
        /// <summary>The slot that records / plays (0-based; the menu shows 1-5).</summary>
        public static int RecordSlot = 0;
        /// <summary>Playback picks a random filled slot each time instead of the selected one.</summary>
        public static bool PlaybackRandom = false;

        // Health / meters
        public static bool HealthRefill = true;
        public static int P1HealthPercent = 100;
        public static int DummyHealthPercent = 100;
        public static MeterMode SuperMode = MeterMode.Infinite;
        public static MeterMode BarrierMode = MeterMode.Normal;
        public static MeterMode BurstMode = MeterMode.Normal;
        /// <summary>Super meter amount Refill After Combo goes back to, in percent of the max (steps of SuperRefillStep).</summary>
        public static int SuperRefillPercent = 100;
        public const int SuperRefillStep = 10;
        /// <summary>Frames both fighters must be free before Refill After Combo refills.</summary>
        public static int RefillDelay = 30;

        // Shorthands (older code and tests): true = Infinite, false = Normal.
        public static bool InfiniteSuper { get => SuperMode == MeterMode.Infinite; set => SuperMode = value ? MeterMode.Infinite : MeterMode.Normal; }
        public static bool InfiniteBarrier { get => BarrierMode == MeterMode.Infinite; set => BarrierMode = value ? MeterMode.Infinite : MeterMode.Normal; }
        public static bool InfiniteBurst { get => BurstMode == MeterMode.Infinite; set => BurstMode = value ? MeterMode.Infinite : MeterMode.Normal; }

        // Displays
        public static bool ShowInputHistory = true;
        public static bool ShowFrameMeter = true;
        public static bool ShowAdvantage = true;
        public static bool ShowStateFlash = true;
        public static bool ShowP1Inputs = true;
        public static bool ShowP2Inputs = true;
        public static bool ShowTrainingInfo = true;

        // Reset
        public static ResetPosition Position = ResetPosition.Center;

        public static readonly int[] HealthSteps = { 100, 75, 50, 25, 10 };

        static bool _loaded;

        public static void Defaults()
        {
            Dummy = DummyMode.Controller;
            Guard = GuardMode.None;
            Counter = CounterMode.Normal;
            FunctionalCounter = FunctionalCounterMode.Default;
            DummyBarrier = DummyBarrierMode.Normal;
            GuardHits = 3;
            Reversal = ReversalMode.Off;
            ReversalMove = "";
            ReversalOnWakeup = ReversalOnBlock = ReversalOnHit = ReversalOnAirTech = true;
            ReversalDelay = 0;
            AirTech = AirTechMode.Off;
            GroundTech = GroundTechMode.Off;
            TechDelay = 0;
            RecordCountdown = true;
            PlayOnReset = PlayOnResetMode.Off;
            HealthRefill = true;
            P1HealthPercent = 100;
            DummyHealthPercent = 100;
            SuperMode = MeterMode.Infinite;
            BarrierMode = MeterMode.Normal;
            BurstMode = MeterMode.Normal;
            SuperRefillPercent = 100;
            RecordSlot = 0;
            PlaybackRandom = false;
            ShowInputHistory = true;
            ShowFrameMeter = ShowP1Inputs = ShowP2Inputs = true;
            ShowAdvantage = ShowStateFlash = true;
            ShowTrainingInfo = true;
            Position = ResetPosition.Center;
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var cfg = new ConfigFile();
            if (cfg.Load(FilePath) != Error.Ok) return;
            Dummy = (DummyMode)(int)cfg.GetValue(Section, "dummy", (int)Dummy);
            Guard = (GuardMode)(int)cfg.GetValue(Section, "guard", (int)Guard);
            Counter = (CounterMode)(int)cfg.GetValue(Section, "counter", (int)Counter);
            FunctionalCounter = (FunctionalCounterMode)(int)cfg.GetValue(Section, "functional_counter", (int)FunctionalCounter);
            DummyBarrier = (DummyBarrierMode)(int)cfg.GetValue(Section, "dummy_barrier", (int)DummyBarrier);
            HealthRefill = (bool)cfg.GetValue(Section, "health_refill", HealthRefill);
            P1HealthPercent = (int)cfg.GetValue(Section, "p1_health", P1HealthPercent);
            DummyHealthPercent = (int)cfg.GetValue(Section, "dummy_health", DummyHealthPercent);
            // Older settings files saved Infinite on/off; newer ones save the mode.
            InfiniteSuper = (bool)cfg.GetValue(Section, "infinite_super", InfiniteSuper);
            InfiniteBarrier = (bool)cfg.GetValue(Section, "infinite_barrier", InfiniteBarrier);
            InfiniteBurst = (bool)cfg.GetValue(Section, "infinite_burst", InfiniteBurst);
            SuperMode = (MeterMode)(int)cfg.GetValue(Section, "super_mode", (int)SuperMode);
            BarrierMode = (MeterMode)(int)cfg.GetValue(Section, "barrier_mode", (int)BarrierMode);
            BurstMode = (MeterMode)(int)cfg.GetValue(Section, "burst_mode", (int)BurstMode);
            SuperRefillPercent = Mathf.Clamp((int)cfg.GetValue(Section, "super_refill", SuperRefillPercent), 0, 100);
            RecordSlot = Mathf.Clamp((int)cfg.GetValue(Section, "record_slot", RecordSlot), 0, TrainingController.Slots - 1);
            PlaybackRandom = (bool)cfg.GetValue(Section, "playback_random", PlaybackRandom);
            ShowInputHistory = (bool)cfg.GetValue(Section, "show_inputs", ShowInputHistory);
            ShowFrameMeter = (bool)cfg.GetValue(Section, "show_frame_meter", ShowFrameMeter);
            ShowAdvantage = (bool)cfg.GetValue(Section, "show_advantage", ShowAdvantage);
            ShowStateFlash = (bool)cfg.GetValue(Section, "show_state_flash", ShowStateFlash);
            ShowP1Inputs = (bool)cfg.GetValue(Section, "show_p1_inputs", ShowP1Inputs);
            ShowP2Inputs = (bool)cfg.GetValue(Section, "show_p2_inputs", ShowP2Inputs);
            ShowTrainingInfo = (bool)cfg.GetValue(Section, "show_info", ShowTrainingInfo);
            Position = (ResetPosition)(int)cfg.GetValue(Section, "position", (int)Position);
            GuardHits = Mathf.Clamp((int)cfg.GetValue(Section, "guard_hits", GuardHits), 1, MaxGuardHits);
            Reversal = (ReversalMode)(int)cfg.GetValue(Section, "reversal", (int)Reversal);
            ReversalMove = (string)cfg.GetValue(Section, "reversal_move", ReversalMove);
            ReversalOnWakeup = (bool)cfg.GetValue(Section, "reversal_wakeup", ReversalOnWakeup);
            ReversalOnBlock = (bool)cfg.GetValue(Section, "reversal_block", ReversalOnBlock);
            ReversalOnHit = (bool)cfg.GetValue(Section, "reversal_hit", ReversalOnHit);
            ReversalOnAirTech = (bool)cfg.GetValue(Section, "reversal_airtech", ReversalOnAirTech);
            ReversalDelay = Mathf.Clamp((int)cfg.GetValue(Section, "reversal_delay", ReversalDelay), 0, MaxReversalDelay);
            AirTech = (AirTechMode)(int)cfg.GetValue(Section, "air_tech", (int)AirTech);
            GroundTech = (GroundTechMode)(int)cfg.GetValue(Section, "ground_tech", (int)GroundTech);
            TechDelay = Mathf.Clamp((int)cfg.GetValue(Section, "tech_delay", TechDelay), 0, MaxTechDelay);
            RecordCountdown = (bool)cfg.GetValue(Section, "record_countdown", RecordCountdown);
            PlayOnReset = (PlayOnResetMode)(int)cfg.GetValue(Section, "play_on_reset", (int)PlayOnReset);
        }

        public static void Save()
        {
            var cfg = new ConfigFile();
            cfg.SetValue(Section, "dummy", (int)Dummy);
            cfg.SetValue(Section, "guard", (int)Guard);
            cfg.SetValue(Section, "counter", (int)Counter);
            cfg.SetValue(Section, "functional_counter", (int)FunctionalCounter);
            cfg.SetValue(Section, "dummy_barrier", (int)DummyBarrier);
            cfg.SetValue(Section, "health_refill", HealthRefill);
            cfg.SetValue(Section, "p1_health", P1HealthPercent);
            cfg.SetValue(Section, "dummy_health", DummyHealthPercent);
            cfg.SetValue(Section, "infinite_super", InfiniteSuper);
            cfg.SetValue(Section, "infinite_barrier", InfiniteBarrier);
            cfg.SetValue(Section, "infinite_burst", InfiniteBurst);
            cfg.SetValue(Section, "super_mode", (int)SuperMode);
            cfg.SetValue(Section, "barrier_mode", (int)BarrierMode);
            cfg.SetValue(Section, "burst_mode", (int)BurstMode);
            cfg.SetValue(Section, "super_refill", SuperRefillPercent);
            cfg.SetValue(Section, "record_slot", RecordSlot);
            cfg.SetValue(Section, "playback_random", PlaybackRandom);
            cfg.SetValue(Section, "show_inputs", ShowInputHistory);
            cfg.SetValue(Section, "show_frame_meter", ShowFrameMeter);
            cfg.SetValue(Section, "show_advantage", ShowAdvantage);
            cfg.SetValue(Section, "show_state_flash", ShowStateFlash);
            cfg.SetValue(Section, "show_p1_inputs", ShowP1Inputs);
            cfg.SetValue(Section, "show_p2_inputs", ShowP2Inputs);
            cfg.SetValue(Section, "show_info", ShowTrainingInfo);
            cfg.SetValue(Section, "position", (int)Position);
            cfg.SetValue(Section, "guard_hits", GuardHits);
            cfg.SetValue(Section, "reversal", (int)Reversal);
            cfg.SetValue(Section, "reversal_move", ReversalMove ?? "");
            cfg.SetValue(Section, "reversal_wakeup", ReversalOnWakeup);
            cfg.SetValue(Section, "reversal_block", ReversalOnBlock);
            cfg.SetValue(Section, "reversal_hit", ReversalOnHit);
            cfg.SetValue(Section, "reversal_airtech", ReversalOnAirTech);
            cfg.SetValue(Section, "reversal_delay", ReversalDelay);
            cfg.SetValue(Section, "air_tech", (int)AirTech);
            cfg.SetValue(Section, "ground_tech", (int)GroundTech);
            cfg.SetValue(Section, "tech_delay", TechDelay);
            cfg.SetValue(Section, "record_countdown", RecordCountdown);
            cfg.SetValue(Section, "play_on_reset", (int)PlayOnReset);
            cfg.Save(FilePath);
        }
    }
}
