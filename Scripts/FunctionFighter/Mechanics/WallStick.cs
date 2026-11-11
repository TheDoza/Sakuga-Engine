using Godot;
using MessagePack;
using SakugaEngine;
using SakugaEngine.Global;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Per-fighter wall stick state. Saved with the fighter for rollback (SakugaActorState key 20).</summary>
    [MessagePackObject]
    public struct WallStickState
    {
        /// <summary>Stick length armed by the last hit, waiting for the fighter to reach the wall.</summary>
        [Key(0)] public int Pending;
        /// <summary>Frames left stuck to the wall.</summary>
        [Key(1)] public int Left;
    }

    /// <summary>
    /// [FF PATCH: wall stick] Wall stick (e.g. Ragna's Straight Punch, Basic Bro's 22C~236C).
    ///
    /// A hitbox with WallStickFrames > 0 arms a wall stick on the fighter it hits. If that fighter touches the stage
    /// wall (the corner, not the camera edge) while still flying from the hit, they stop dead on the wall for
    /// WallStickFrames frames with no gravity, then fall with normal gravity. Hitstun is extended so the stick can't be
    /// teched out of. Landing first, leaving hitstun, or getting hit by something else clears it.
    ///
    /// Use it with a knockback that doesn't arm a wall bounce (BounceXIntensity = 0), or the wall bounce wins.
    /// </summary>
    public static class WallStick
    {
        /// <summary>Hitstun left after the stick ends, so the fall can't be teched.</summary>
        const int HitstunAfterStick = 30;

        static bool IsFighter(SakugaActor a) => a != null && a.StateManager != null && a.Body != null;

        public static void Reset(SakugaActor a)
        {
            if (a == null) return;
            a.WallStick = new WallStickState();
        }

        /// <summary>True while the fighter is held on the wall.</summary>
        public static bool Stuck(SakugaActor a) => a != null && a.WallStick.Left > 0;

        /// <summary>Called by the defender's HitDamage after the knockback is applied. Every hit replaces the previous one.</summary>
        public static void OnHit(SakugaActor defender, HitboxElement box)
        {
            if (!IsFighter(defender)) return;
            defender.WallStick.Pending = box != null ? System.Math.Max(0, box.WallStickFrames) : 0;
            defender.WallStick.Left = 0;
        }

        /// <summary>Once per frame from Tick (not during hitstop), before the state's own tick events.</summary>
        public static void Tick(SakugaActor a)
        {
            if (!IsFighter(a)) return;
            ref WallStickState w = ref a.WallStick;
            if (w.Pending <= 0 && w.Left <= 0) return;

            if (a.StateManager.CurrentStateType() != StateType.HIT_REACTION)
            {
                if (w.Left > 0) a.Body.CurrentGravity = GlobalVariables.DefaultGravity;
                w = new WallStickState();
                return;
            }

            if (w.Left > 0)
            {
                Hold(a);
                w.Left--;
                if (w.Left == 0) a.Body.CurrentGravity = GlobalVariables.DefaultGravity; // let go: fall
                return;
            }

            // Armed, waiting for the wall. Landing first cancels it.
            if (a.Body.IsOnGround && a.Body.FixedVelocity.Y <= 0)
            {
                w.Pending = 0;
                return;
            }
            if (!a.Body.IsOnWall) return;

            w.Left = w.Pending;
            w.Pending = 0;
            if (a.Hitstun != null)
            {
                uint need = (uint)(w.Left + HitstunAfterStick);
                if (a.Hitstun.TimeLeft < need) a.Hitstun.Start(need);
            }
            Hold(a);
            w.Left--;
        }

        static void Hold(SakugaActor a)
        {
            if (a.Knockback != null) a.Knockback.Stop();
            a.Body.FixedVelocity = Vector2I.Zero;
            a.Body.CurrentGravity = 0; // the hit state's AddGravity then adds nothing
        }
    }
}
