using Godot;
using MessagePack;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    public enum CounterKind : byte { None, Counter, BigCounter, Functional, Carry }

    /// <summary>Per-fighter counter-hit state. Saved with the fighter for rollback (SakugaActorState key 18).</summary>
    [MessagePackObject]
    public struct CounterState
    {
        // Attacker side
        /// <summary>Counter Hit Carry: the state whose hits keep counting as counter hits (-1 = none)...</summary>
        [Key(0)] public int CarryState;
        /// <summary>...and the frame of that state last seen (a restart of the same move ends the carry).</summary>
        [Key(1)] public int CarryFrame;
        /// <summary>Frames left in a Functional Counter this fighter started (0 = not in one).</summary>
        [Key(2)] public int FunctionalTimer;
        /// <summary>The Functional Counter ran out: the next hit still counts, then ends it with the green spark.</summary>
        [Key(3)] public bool FunctionalEnding;
        [Key(4)] public int DashFramesLeft;
        [Key(5)] public int DashStep;
        /// <summary>Was in an attack (COMBAT state) on the previous frame (auto-dash starts when an attack ends uncancelled).</summary>
        [Key(6)] public bool WasAttacking;

        // Defender side
        /// <summary>In a combo that started with a Big Counter: later hits get extra hitstun.</summary>
        [Key(7)] public bool BigCounterCombo;
        /// <summary>Caught in the opponent's Functional Counter.</summary>
        [Key(8)] public bool FunctionalVictim;
        /// <summary>What the last hit taken was (for the training display).</summary>
        [Key(9)] public CounterKind LastKind;
    }

    /// <summary>What a hit gets from counter rules. Applied in SakugaActor.HitDamage ([FF PATCH: counter]).</summary>
    public struct CounterResult
    {
        public CounterKind Kind;
        public int DamagePercent;
        public int ExtraHitstun;
        public int HitstunPercent;
        public int ExtraHitstop;
        public int GravityPercent;

        public static CounterResult Normal => new CounterResult { DamagePercent = 100, HitstunPercent = 100, GravityPercent = 100 };

        public int Hitstun(int baseHitstun) => baseHitstun * HitstunPercent / 100 + ExtraHitstun;
        public int Damage(int baseDamage) => (int)((long)baseDamage * DamagePercent / 100);
        public int Gravity(int baseGravity) => (int)((long)baseGravity * GravityPercent / 100);
    }

    /// <summary>
    /// Counter Hit, Big Counter and Functional Counter. See CounterSettings for the numbers.
    ///
    /// - Counter Hit: the defender was in the startup or active frames of an attack (Sakuga's IsCounter).
    ///   +10% damage, extra hitstun / untechable time and extra hitstop. Counter Hit Carry: the rest of that
    ///   attack's hits count too (no extra hitstop).
    /// - Big Counter: a Counter Hit with a BigCounter hitbox. Every later hit in that combo gets +3 hitstun (doesn't stack).
    /// - Functional Counter: a Big Counter on a defender in Danger (and the mechanic turned on). For 5 seconds the
    ///   attacker's hits do double hitstun, launched opponents fall slowly, and every attack that ends without a cancel
    ///   is followed by an auto-dash. The first hit after time runs out still counts and ends it with a green spark.
    ///   It also ends if the combo drops.
    /// </summary>
    public static class Counter
    {
        public static CounterSettings Settings => CounterSettings.Get();

        static bool IsFighter(SakugaActor a) => a != null && a.StateManager != null && a.Body != null && a.Parameters != null;

        public static void Reset(SakugaActor a)
        {
            a.Counter = new CounterState { CarryState = -1 };
        }

        public static bool InFunctionalCounter(SakugaActor a) => a != null && (a.Counter.FunctionalTimer > 0 || a.Counter.FunctionalEnding);

        /// <summary>Called by the defender's HitDamage before hitstun, damage and knockback are worked out.</summary>
        public static CounterResult OnHit(SakugaActor defender, SakugaActor attackerNode, HitboxElement box, bool groundHit)
        {
            var r = CounterResult.Normal;
            if (!IsFighter(defender) || attackerNode == null) return r;
            var s = Settings;
            SakugaActor attacker = attackerNode.GetMaster() ?? attackerNode;

            int forced = Training.TrainingController.ForcedCounter(defender);
            bool counter = forced == 0 ? attackerNode.IsCounter(defender) : forced > 0;
            bool big = counter && ((box != null && box.BigCounter) || forced == 2);
            bool carry = !counter && forced >= 0 && s.CounterHitCarry && IsCarrying(attackerNode);

            r.Kind = big ? CounterKind.BigCounter : counter ? CounterKind.Counter : carry ? CounterKind.Carry : CounterKind.None;

            // Later hits of a Big Counter combo (checked before this hit can start one).
            if (defender.Counter.BigCounterCombo) r.ExtraHitstun += s.BigCounterComboHitstun;

            if (counter || carry)
            {
                r.DamagePercent = s.DamagePercent;
                r.ExtraHitstun += groundHit ? s.ExtraGroundHitstun : s.ExtraAirHitstun;
            }
            if (counter)
            {
                r.ExtraHitstop = s.ExtraHitstop;
                if (s.CounterHitCarry && attackerNode.StateManager != null)
                {
                    attackerNode.Counter.CarryState = attackerNode.StateManager.CurrentState;
                    attackerNode.Counter.CarryFrame = attackerNode.StateManager.CurrentStateFrame;
                }
                Notify(attacker, big ? s.BigCounterNotificationIndex : s.CounterNotificationIndex);
                SpawnVFX(defender, s.CounterVFXIndex, s.VFXHeight);
            }
            if (big) defender.Counter.BigCounterCombo = true;

            // Functional Counter
            bool fcOn = Training.TrainingController.FunctionalCounterEnabled(s.FunctionalCounterEnabled);
            if (big && fcOn && defender.Barrier.Danger && !InFunctionalCounter(attacker))
            {
                attacker.Counter.FunctionalTimer = System.Math.Max(1, s.Duration);
                attacker.Counter.FunctionalEnding = false;
                attacker.Counter.WasAttacking = false;
                defender.Counter.FunctionalVictim = true;
                r.Kind = CounterKind.Functional;
                Notify(attacker, s.StartNotificationIndex);
                SpawnVFX(defender, s.StartVFXIndex, s.VFXHeight);
            }

            if (InFunctionalCounter(attacker) && defender.Counter.FunctionalVictim)
            {
                r.HitstunPercent = s.HitstunPercent;
                r.GravityPercent = s.GravityPercent;
                if (attacker.Counter.FunctionalEnding)
                {
                    // Last hit with the long hitstun / slow fall: green spark, then everything is back to normal.
                    SpawnVFX(defender, s.EndVFXIndex, s.VFXHeight);
                    Notify(attacker, s.EndNotificationIndex);
                    EndFunctional(attacker, defender);
                }
            }

            defender.Counter.LastKind = r.Kind;
            if (r.Kind != CounterKind.None) StateFlash.Counter(defender, r.Kind); // red / orange / gold flash on the interrupted fighter
            return r;
        }

        static bool IsCarrying(SakugaActor a)
        {
            if (a.StateManager == null || a.Counter.CarryState < 0) return false;
            return a.StateManager.CurrentState == a.Counter.CarryState && a.StateManager.CurrentStateFrame >= a.Counter.CarryFrame;
        }

        static void EndFunctional(SakugaActor attacker, SakugaActor defender)
        {
            attacker.Counter.FunctionalTimer = 0;
            attacker.Counter.FunctionalEnding = false;
            attacker.Counter.DashFramesLeft = 0;
            if (defender != null) defender.Counter.FunctionalVictim = false;
        }

        /// <summary>Once per frame after hitstop (from SakugaActor.Tick): carry, combo flags, Functional Counter timer and auto-dash.</summary>
        public static void Tick(SakugaActor a)
        {
            if (!IsFighter(a)) return;
            ref CounterState c = ref a.Counter;
            var s = Settings;
            StateType type = a.StateManager.CurrentStateType();

            // Counter Hit Carry ends when the attack ends or restarts.
            if (c.CarryState >= 0)
            {
                if (a.StateManager.CurrentState != c.CarryState || a.StateManager.CurrentStateFrame < c.CarryFrame) c.CarryState = -1;
                else c.CarryFrame = a.StateManager.CurrentStateFrame;
            }

            // Combo over: Big Counter bonus and Functional Counter end.
            if (type != StateType.HIT_REACTION)
            {
                c.BigCounterCombo = false;
                if (c.FunctionalVictim)
                {
                    c.FunctionalVictim = false;
                    var opp = a.GetOpponents();
                    if (opp != null) foreach (var o in opp) if (o != null && InFunctionalCounter(o)) EndFunctional(o, a);
                }
            }

            if (!InFunctionalCounter(a)) { c.WasAttacking = false; return; }

            if (c.FunctionalTimer > 0 && --c.FunctionalTimer == 0) c.FunctionalEnding = true;

            // Auto-dash: an attack that ends without being cancelled into another one.
            bool attacking = type == StateType.COMBAT;
            if (c.WasAttacking && !attacking && type <= StateType.MOVEMENT && a.Body.IsOnGround && c.DashFramesLeft == 0)
            {
                int frames = System.Math.Max(1, s.AutoDashFrames);
                c.DashFramesLeft = frames;
                c.DashStep = s.AutoDashDistance / frames;
            }
            c.WasAttacking = attacking;

            if (c.DashFramesLeft > 0)
            {
                c.DashFramesLeft--;
                SakugaActor target = a.GetOpponent(0);
                if (target != null && target.Body != null)
                {
                    int dx = target.Body.FixedPosition.X - a.Body.FixedPosition.X;
                    int dir = System.Math.Sign(dx);
                    if (dir != 0 && System.Math.Abs(dx) > s.AutoDashMinGap)
                    {
                        int step = System.Math.Min(c.DashStep, System.Math.Abs(dx) - s.AutoDashMinGap);
                        a.Body.FixedPosition.X = Mathf.Clamp(a.Body.FixedPosition.X + dir * step, -GlobalVariables.WallLimit, GlobalVariables.WallLimit);
                    }
                    else c.DashFramesLeft = 0;
                }
            }
        }

        static void Notify(SakugaActor a, int index)
        {
            var gm = GameManager.Instance;
            if (index < 0 || gm == null || gm.notifications == null || gm.notifications.Elements == null || index >= gm.notifications.Elements.Length) return;
            gm.PlayHitNotificationByIndex((int)a.playerID, index);
        }

        static void SpawnVFX(SakugaActor a, int index, int height)
        {
            if (index < 0 || a.Pool == null) return;
            try { a.Pool.SpawnVFX(index, a.Body.FixedPosition + new Vector2I(0, height), a.Body.PlayerSide, true); }
            catch (System.ArgumentOutOfRangeException) { GD.PushWarning("[Counter] VFX index " + index + " is not in the fighter's VFX list."); }
        }
    }
}
