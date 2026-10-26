using Godot;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Tuning for the three counter hits. One shared file: res://Fighters/Shared/Counter/CounterSettings.tres.
    /// Integers only, so the mechanic stays deterministic for rollback.
    ///
    /// Counter Hit (BBCF): hitting a fighter during their attack's startup or active frames.
    /// Big Counter (BBCF Fatal Counter): a Counter Hit with a hitbox that has BigCounter ticked.
    /// Functional Counter (KOF XII Critical Counter-style): a Big Counter on a fighter in Danger.
    /// </summary>
    [GlobalClass]
    public partial class CounterSettings : Resource
    {
        public const string SettingsPath = "res://Fighters/Shared/Counter/CounterSettings.tres";

        [ExportGroup("Counter Hit")]
        /// <summary>Damage of a counter hit, in percent of normal (BBCF: +10%).</summary>
        [Export] public int DamagePercent = 110;
        /// <summary>Extra hitstun on a grounded opponent.</summary>
        [Export] public int ExtraGroundHitstun = 4;
        /// <summary>Extra untechable time on an airborne opponent.</summary>
        [Export] public int ExtraAirHitstun = 8;
        /// <summary>Extra hitstop for the opponent (more time to hit-confirm). Only on the hit that counters, never on carried hits.</summary>
        [Export] public int ExtraHitstop = 8;
        /// <summary>Counter Hit Carry: if the first hit of a multi-hit attack counters, the rest of that attack's hits do too
        /// (bonus damage and hitstun, not the extra hitstop).</summary>
        [Export] public bool CounterHitCarry = true;
        /// <summary>Notification for a counter hit (index in HitNotifs.tres). -1 = none.</summary>
        [Export] public int CounterNotificationIndex = 1;
        /// <summary>Flash on the opponent on a counter hit (index in Shared_VFXList.tres). -1 = none.</summary>
        [Export] public int CounterVFXIndex = 7;

        [ExportGroup("Big Counter")]
        /// <summary>Extra hitstun / untechable time for every later hit in the same combo. Doesn't stack.</summary>
        [Export] public int BigCounterComboHitstun = 3;
        [Export] public int BigCounterNotificationIndex = 10;

        [ExportGroup("Functional Counter")]
        /// <summary>Turn the mechanic on or off (the Training Settings can also override it).</summary>
        [Export] public bool FunctionalCounterEnabled = true;
        /// <summary>How long it lasts, in frames (300 = 5 s). The first hit after it runs out still counts, then ends it.</summary>
        [Export] public int Duration = 300;
        /// <summary>Hitstun of every hit during it, in percent of normal.</summary>
        [Export] public int HitstunPercent = 200;
        /// <summary>Gravity on an opponent knocked into the air during it, in percent of normal (lower = falls slower).</summary>
        [Export] public int GravityPercent = 35;
        /// <summary>Auto-dash after each attack that ends without being cancelled: distance and how many frames it takes.</summary>
        [Export] public int AutoDashDistance = 14000;
        [Export] public int AutoDashFrames = 10;
        /// <summary>The auto-dash stops when the fighters are this close.</summary>
        [Export] public int AutoDashMinGap = 7000;
        [Export] public int StartNotificationIndex = 11;
        [Export] public int EndNotificationIndex = 12;
        /// <summary>VFX when it starts and the green spark on the hit that ends it (indexes in Shared_VFXList.tres).</summary>
        [Export] public int StartVFXIndex = 8;
        [Export] public int EndVFXIndex = 9;
        [Export] public int VFXHeight = 9000;

        [ExportGroup("Punish & Reversal indicators")]
        /// <summary>[FF PATCH: indicators] Notification / flash VFX for a Punish (a hit on an opponent in recovery). -1 = none.</summary>
        [Export] public int PunishNotificationIndex = 2;
        [Export] public int PunishVFXIndex = 10;
        /// <summary>Notification / flash VFX for a Reversal (a move on the first frame after blockstun, hitstun or wakeup). -1 = none.</summary>
        [Export] public int ReversalNotificationIndex = 13;
        [Export] public int ReversalVFXIndex = 11;
        /// <summary>Notification when the opponent lands in a hard knockdown (index in HitNotifs.tres; 7 = "Hard
        /// Knockdown"). -1 = none.</summary>
        [Export] public int HardKnockdownNotificationIndex = 7;

        private static CounterSettings _cached;

        public static CounterSettings Get()
        {
            if (_cached != null) return _cached;
            if (ResourceLoader.Exists(SettingsPath)) _cached = ResourceLoader.Load<CounterSettings>(SettingsPath);
            if (_cached == null)
            {
                GD.PushWarning("[Counter] " + SettingsPath + " not found, using default values.");
                _cached = new CounterSettings();
            }
            return _cached;
        }
    }
}
