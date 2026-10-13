using Godot;

namespace FunctionFighter.Mechanics
{
    public enum BurstGainMode
    {
        /// <summary>100 + damage x 10 x (1 - combo damage scaling): long, scaled combos give back more Burst.</summary>
        BBCF,
        /// <summary>100 + damage x 10 per hit, ignoring combo scaling.</summary>
        Simple,
        /// <summary>No gain from getting hit; only the passive refill.</summary>
        PassiveOnly,
    }

    /// <summary>
    /// Tuning for Burst (BBCF-style). One shared file: res://Fighters/Shared/Burst/BurstSettings.tres.
    /// Integers only, so the mechanic stays deterministic for rollback.
    /// </summary>
    [GlobalClass]
    public partial class BurstSettings : Resource
    {
        public const string SettingsPath = "res://Fighters/Shared/Burst/BurstSettings.tres";

        [ExportGroup("Gauge")]
        /// <summary>Size of the gauge. Full at the start of the match; it carries over between rounds. A Burst costs all of it.</summary>
        [Export] public int MaxValue = 10000;

        [ExportGroup("Passive refill (per frame, keeps running during hitstop)")]
        /// <summary>Health at or above this percent refills slowly...</summary>
        [Export] public int HighHealthPercent = 55;
        [Export] public int HighHealthGain = 10;
        /// <summary>...between the two thresholds a bit faster...</summary>
        [Export] public int MidHealthGain = 15;
        /// <summary>...and at or below this percent fastest.</summary>
        [Export] public int LowHealthPercent = 40;
        [Export] public int LowHealthGain = 20;
        /// <summary>Speed of the passive refill, in percent of the gains above. 100 = BBCF speed, 50 = half speed (twice as long to fill).
        /// The easiest way to make the gauge come back slower or faster.</summary>
        [Export] public int PassiveSpeedPercent = 50;

        [ExportGroup("Gain from getting hit")]
        [Export] public BurstGainMode GainMode = BurstGainMode.BBCF;
        /// <summary>Flat gain for every hit taken.</summary>
        [Export] public int HitGainBase = 100;
        /// <summary>Damage multiplier in the gain formula.</summary>
        [Export] public int HitGainDamageMultiplier = 10;

        [ExportGroup("Visuals")]
        /// <summary>VFX played when a Burst starts (index in Shared_VFXList.tres). -1 = none.</summary>
        [Export] public int BurstVFXIndex = 6;
        /// <summary>Height of the Burst VFX above the fighter's feet.</summary>
        [Export] public int BurstVFXHeight = 9000;

        private static BurstSettings _cached;

        public static BurstSettings Get()
        {
            if (_cached != null) return _cached;
            if (ResourceLoader.Exists(SettingsPath)) _cached = ResourceLoader.Load<BurstSettings>(SettingsPath);
            if (_cached == null)
            {
                GD.PushWarning("[Burst] " + SettingsPath + " not found, using default values.");
                _cached = new BurstSettings();
            }
            return _cached;
        }
    }
}
