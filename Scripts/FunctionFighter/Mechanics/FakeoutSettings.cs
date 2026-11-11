using Godot;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Tuning for Function Fakeout (the universal cancel to neutral, A+B+C during a move). One shared file:
    /// res://Fighters/Shared/Fakeout/FakeoutSettings.tres. Integers only, so the mechanic stays deterministic for rollback.
    /// Meter costs are in super gauge points (the gauge is 10000 = 100%).
    /// </summary>
    [GlobalClass]
    public partial class FakeoutSettings : Resource
    {
        public const string SettingsPath = "res://Fighters/Shared/Fakeout/FakeoutSettings.tres";

        [ExportGroup("General")]
        /// <summary>Turn the mechanic on or off.</summary>
        [Export] public bool Enabled = true;
        /// <summary>An A+B+C press is kept this many frames, so a press slightly before the window still comes out.</summary>
        [Export] public int InputBufferFrames = 3;
        /// <summary>State after a Fakeout on the ground / in the air (0 Idle, 6 Falling).</summary>
        [Export] public int GroundState = 0;
        [Export] public int AirState = 6;
        /// <summary>An air Fakeout keeps the fighter's momentum (off = stops in place, then falls). Ground Fakeouts always stop.</summary>
        [Export] public bool KeepAirMomentum = true;
        /// <summary>Height of the ring VFX above the fighter's position.</summary>
        [Export] public int VFXHeight = 9000;
        /// <summary>Red X over the fighter when A+B+C during a move can't Fakeout (index in Shared_VFXList.tres, -1 = none)
        /// and its height (18500 = same as the throw "!").</summary>
        [Export] public int FailVFXIndex = 23;
        [Export] public int FailVFXHeight = 18500;

        [ExportGroup("Blue: startup / recovery of specials, opponent not in blockstun or hitstun")]
        [Export] public int BlueCost = 2500;
        /// <summary>Notification (index in HitNotifs.tres) and ring VFX (index in Shared_VFXList.tres). -1 = none.</summary>
        [Export] public int BlueNotificationIndex = 14;
        [Export] public int BlueVFXIndex = 19;

        [ExportGroup("Red: normals or specials, opponent in hitstun or blockstun")]
        [Export] public int RedCost = 5000;
        [Export] public int RedNotificationIndex = 15;
        [Export] public int RedVFXIndex = 20;

        [ExportGroup("Purple: risky moves (invincible reversals, command grabs) when blocked or whiffed")]
        [Export] public int PurpleCost = 7500;
        [Export] public int PurpleNotificationIndex = 16;
        [Export] public int PurpleVFXIndex = 21;

        [ExportGroup("Gold: active or recovery frames of supers")]
        /// <summary>Supers cost 5000, so super + Gold uses the whole gauge.</summary>
        [Export] public int GoldCost = 5000;
        [Export] public int GoldNotificationIndex = 17;
        [Export] public int GoldVFXIndex = 22;

        public int Cost(FakeoutColor c) => c switch
        {
            FakeoutColor.Blue => BlueCost,
            FakeoutColor.Red => RedCost,
            FakeoutColor.Purple => PurpleCost,
            FakeoutColor.Gold => GoldCost,
            _ => 0,
        };

        public int NotificationIndex(FakeoutColor c) => c switch
        {
            FakeoutColor.Blue => BlueNotificationIndex,
            FakeoutColor.Red => RedNotificationIndex,
            FakeoutColor.Purple => PurpleNotificationIndex,
            FakeoutColor.Gold => GoldNotificationIndex,
            _ => -1,
        };

        public int VFXIndex(FakeoutColor c) => c switch
        {
            FakeoutColor.Blue => BlueVFXIndex,
            FakeoutColor.Red => RedVFXIndex,
            FakeoutColor.Purple => PurpleVFXIndex,
            FakeoutColor.Gold => GoldVFXIndex,
            _ => -1,
        };

        private static FakeoutSettings _cached;

        public static FakeoutSettings Get()
        {
            if (_cached != null) return _cached;
            if (ResourceLoader.Exists(SettingsPath)) _cached = ResourceLoader.Load<FakeoutSettings>(SettingsPath);
            if (_cached == null)
            {
                GD.PushWarning("[Fakeout] " + SettingsPath + " not found, using default values.");
                _cached = new FakeoutSettings();
            }
            return _cached;
        }
    }
}
