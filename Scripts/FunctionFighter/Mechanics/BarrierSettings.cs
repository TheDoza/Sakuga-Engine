using Godot;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Tuning for the Barrier mechanic (BBCF-style barrier guard). One shared file:
    /// res://Fighters/Shared/Barrier/BarrierSettings.tres. All values are integers so the
    /// mechanic stays deterministic for rollback.
    /// </summary>
    [GlobalClass]
    public partial class BarrierSettings : Resource
    {
        public const string SettingsPath = "res://Fighters/Shared/Barrier/BarrierSettings.tres";

        [ExportGroup("Gauge")]
        /// <summary>Size of the gauge. Every fighter starts each round full.</summary>
        [Export] public int MaxValue = 10000;
        /// <summary>Taken instantly each time barrier starts (first frame of holding it).</summary>
        [Export] public int ActivationCost = 100;
        /// <summary>Taken every frame barrier is held after the first one. Not taken during hitstop.</summary>
        [Export] public int HoldDrainPerFrame = 10;
        /// <summary>A barrier-blocked hit costs this percent of the hit's raw damage...</summary>
        [Export] public int HitCostPercentOfDamage = 50;
        /// <summary>...but never more than this.</summary>
        [Export] public int HitCostCap = 500;

        [ExportGroup("Refill")]
        /// <summary>Frames after barrier is let go before the gauge starts refilling.</summary>
        [Export] public int RegenDelay = 124;
        /// <summary>Refill amount...</summary>
        [Export] public int RegenAmount = 16;
        /// <summary>...every this many frames (16 every 10 frames = 1.6 per frame). Keeps running during hitstop.</summary>
        [Export] public int RegenInterval = 10;

        [ExportGroup("Danger")]
        /// <summary>How long Danger lasts. The gauge refills from empty to full over this time, then barrier is back.</summary>
        [Export] public int DangerFrames = 900;
        /// <summary>Damage taken while in Danger, in percent (150 = 1.5x).</summary>
        [Export] public int DangerDamagePercent = 150;
        /// <summary>Hit notification shown when Danger starts (index in HitNotifs.tres). -1 = none.</summary>
        [Export] public int DangerNotificationIndex = 9;

        [ExportGroup("Pushback")]
        /// <summary>Extra pushback added by a barrier block, as a percent of that hit's normal block pushback (100 = doubled).</summary>
        [Export] public int ExtraPushbackPercent = 100;
        /// <summary>Frames over which the extra pushback is applied.</summary>
        [Export] public int PushFrames = 8;

        [ExportGroup("Visuals")]
        /// <summary>VFX played on the blocker for each barrier block (index in Shared_VFXList.tres). -1 = none.</summary>
        [Export] public int FlashVFXIndex = 5;
        /// <summary>Height of the flash above the blocker's feet.</summary>
        [Export] public int FlashHeight = 9000;
        /// <summary>Shield shown in front of a barriering fighter. Empty = no shield.</summary>
        [Export] public PackedScene ShieldScene;
        /// <summary>Shield position: distance in front of the fighter and height above the feet (game units).</summary>
        [Export] public Vector2I ShieldOffset = new Vector2I(3500, 9500);
        /// <summary>Shield position while the fighter is in a crouching state (crouch barrier, crouch block): distance in front and height above the feet (game units).</summary>
        [Export] public Vector2I CrouchShieldOffset = new Vector2I(3500, 6000);

        private static BarrierSettings _cached;

        public static BarrierSettings Get()
        {
            if (_cached != null) return _cached;
            if (ResourceLoader.Exists(SettingsPath)) _cached = ResourceLoader.Load<BarrierSettings>(SettingsPath);
            if (_cached == null)
            {
                GD.PushWarning("[Barrier] " + SettingsPath + " not found, using default values.");
                _cached = new BarrierSettings();
            }
            return _cached;
        }
    }
}
