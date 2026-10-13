using Godot;
using MessagePack;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>Per-fighter barrier state. Saved with the fighter for rollback (SakugaActorState key 16).</summary>
    [MessagePackObject]
    public struct BarrierState
    {
        [Key(0)] public int Value;
        [Key(1)] public bool Danger;
        /// <summary>True on frames where the fighter is barriering (neutral barrier or A+B held in a blocking state).</summary>
        [Key(2)] public bool Active;
        /// <summary>Frames since barrier was last used (drives the refill delay).</summary>
        [Key(3)] public int IdleFrames;
        [Key(4)] public int PushFramesLeft;
        [Key(5)] public int PushStep;
        [Key(6)] public int PushRemainder;
        /// <summary>Set each frame by BarrierStanceAction while in a neutral barrier state (15-17); read and cleared by the next Tick.</summary>
        [Key(7)] public bool InStance;
        /// <summary>Barrier was active on the previous simulated frame (so the activation cost is only paid once per use).</summary>
        [Key(8)] public bool WasActive;
        /// <summary>Frames spent in Danger so far.</summary>
        [Key(9)] public int DangerTimer;
        /// <summary>Barrier was put up and A+B hasn't been let go since: back is no longer needed to keep it (or to guard).</summary>
        [Key(10)] public bool Held;
    }

    /// <summary>
    /// Barrier (BBCF-style): hold back + A+B, in neutral or while blocking. A barrier block takes no chip damage and adds
    /// extra pushback (split between both fighters; if one is cornered, the other takes all of it). It runs on a gauge;
    /// when the gauge empties the fighter enters Danger (no barrier, more damage taken) until it refills completely.
    /// Called from SakugaActor ([FF PATCH: barrier]).
    /// </summary>
    public static class Barrier
    {
        public static BarrierSettings Settings => BarrierSettings.Get();

        static bool IsFighter(SakugaActor a) =>
            a != null && a.Inputs != null && a.Parameters != null && a.Parameters.Health != null && a.Body != null;

        static bool RoundRunning() =>
            GameManager.Instance == null || GameManager.Instance.Monitor == null ||
            GameManager.Instance.Monitor.MatchState == MatchState.ROUND_RUNNING;

        public static bool ButtonsHeld(SakugaActor a) =>
            a.Inputs.IsBeingPressed(a.Inputs.CurrentHistory, PlayerInputs.FACE_A) &&
            a.Inputs.IsBeingPressed(a.Inputs.CurrentHistory, PlayerInputs.FACE_B);

        /// <summary>Barrier meter available (not empty, not in Danger), buttons aside.</summary>
        public static bool HasMeter(SakugaActor a) => IsFighter(a) && !a.Barrier.Danger && a.Barrier.Value > 0;

        public static bool CanBarrier(SakugaActor a) =>
            IsFighter(a) && !a.Barrier.Danger && a.Barrier.Value > 0 && ButtonsHeld(a);

        /// <summary>Back (or down-back / up-back) is held, relative to the side the fighter is on (same rule as blocking).</summary>
        public static bool BackHeld(SakugaActor a) =>
            a.Inputs.CheckDirectionalInputs(a.Inputs.CurrentHistory, a.InputSide(InputSideCheck.SIDE_RELATIVE), DirectionalInputs.LEFT, ButtonMode.HOLD, false);

        public static bool DownHeld(SakugaActor a) =>
            a.Inputs.IsBeingPressed(a.Inputs.CurrentHistory, PlayerInputs.DOWN);

        /// <summary>
        /// Barrier is held: A+B held and barrier available, plus back held, or barrier was already up last frame.
        /// So starting a barrier needs back + A+B, but once it's up, letting go of back keeps it (A+B alone holds it).
        /// Used by the barrier moves (14-16) and to keep the neutral barrier states (15-17) going.
        /// </summary>
        public static bool HoldingBarrier(SakugaActor a) => CanBarrier(a) && (BackHeld(a) || a.Barrier.Held);

        /// <summary>
        /// Barrier is up but back isn't held: the fighter still guards (standing, crouching or air guard, by stance).
        /// Read by StanceManager.GetBlockState ([FF PATCH: barrier]).
        /// </summary>
        public static bool GuardsWithoutBack(SakugaActor a) => IsFighter(a) && a.Barrier.Held && CanBarrier(a);

        /// <summary>Full gauge, no Danger. Called at match start and every round.</summary>
        public static void Reset(SakugaActor a)
        {
            a.Barrier = new BarrierState { Value = Settings.MaxValue };
        }

        /// <summary>Every frame, before hitstop: refill and Danger timers (BBCF doesn't pause these during hitstop).</summary>
        public static void PreTick(SakugaActor a)
        {
            if (!IsFighter(a) || !RoundRunning()) return;
            var s = Settings;
            ref BarrierState b = ref a.Barrier;

            if (b.Danger)
            {
                b.DangerTimer++;
                int frames = System.Math.Max(1, s.DangerFrames);
                b.Value = (int)((long)s.MaxValue * b.DangerTimer / frames);
                if (b.DangerTimer >= frames)
                {
                    b.Danger = false;
                    b.DangerTimer = 0;
                    b.Value = s.MaxValue;
                    b.IdleFrames = 0;
                }
                return;
            }

            if (b.WasActive) return;
            b.IdleFrames++;
            int sinceDelay = b.IdleFrames - s.RegenDelay;
            if (sinceDelay > 0 && sinceDelay % System.Math.Max(1, s.RegenInterval) == 0)
                b.Value = System.Math.Min(s.MaxValue, b.Value + s.RegenAmount);
        }

        /// <summary>Once per frame, after hitstop: drains the gauge while barriering and applies queued pushback.</summary>
        public static void Tick(SakugaActor a)
        {
            if (!IsFighter(a)) return;
            var s = Settings;
            ref BarrierState b = ref a.Barrier;

            if (RoundRunning())
            {
                bool guarding = b.InStance || (a.StateManager != null && a.StateManager.CurrentStateType() == StateType.BLOCKING);
                b.InStance = false;
                b.Active = guarding && CanBarrier(a);

                if (b.Active)
                {
                    b.Value -= b.WasActive ? s.HoldDrainPerFrame : s.ActivationCost;
                    b.IdleFrames = 0;
                }
                b.WasActive = b.Active;
                // Once up, the barrier stays "held" until A+B is let go (or it can't be used): back isn't needed anymore,
                // including through blockstun and the block exit.
                if (b.Active) b.Held = true;
                else if (!CanBarrier(a)) b.Held = false;
                CheckDanger(a);
                b.Value = Mathf.Clamp(b.Value, 0, s.MaxValue);
            }

            // Extra pushback from a barrier block, spread over a few frames.
            if (b.PushFramesLeft > 0)
            {
                int step = b.PushStep + (b.PushFramesLeft == 1 ? b.PushRemainder : 0);
                a.Body.FixedPosition.X = Mathf.Clamp(a.Body.FixedPosition.X + step, -GlobalVariables.WallLimit, GlobalVariables.WallLimit);
                b.PushFramesLeft--;
            }
        }

        /// <summary>Training: put the fighter in Danger (empty gauge) if it isn't already.</summary>
        public static void ForceDanger(SakugaActor a)
        {
            if (!IsFighter(a) || a.Barrier.Danger) return;
            a.Barrier.Value = 0;
            CheckDanger(a);
        }

        static void CheckDanger(SakugaActor a)
        {
            ref BarrierState b = ref a.Barrier;
            if (b.Value > 0 || b.Danger) return;
            b.Value = 0;
            b.Danger = true;
            b.DangerTimer = 0;
            b.Active = false;
            b.WasActive = false;
            b.Held = false;
            int n = Settings.DangerNotificationIndex;
            var gm = GameManager.Instance;
            if (n >= 0 && gm != null && gm.notifications != null && gm.notifications.Elements != null && n < gm.notifications.Elements.Length)
                gm.PlayHitNotificationByIndex((int)a.playerID, n);
        }

        /// <summary>
        /// Called when <paramref name="defender"/> blocks a hit. Returns true if it was a barrier block
        /// (the caller then skips chip damage). Charges the gauge and queues the extra pushback for both fighters.
        /// </summary>
        public static bool OnBlock(SakugaActor defender, SakugaActor attacker, int knockbackX, int knockbackTime, int rawDamage)
        {
            if (!CanBarrier(defender) || !IsFighter(attacker)) return false;
            var s = Settings;
            ref BarrierState b = ref defender.Barrier;
            if (!b.WasActive) b.Value -= s.ActivationCost;
            b.Value -= System.Math.Min(System.Math.Max(0, rawDamage) * s.HitCostPercentOfDamage / 100, s.HitCostCap);
            b.IdleFrames = 0;
            b.Active = true;
            b.WasActive = true;
            CheckDanger(defender);
            b.Value = Mathf.Clamp(b.Value, 0, s.MaxValue);

            // Distance a normal block pushes the defender: knockback velocity for knockbackTime frames
            // (the blockstun states stop the fighter once the knockback ends).
            long v = System.Math.Abs((long)knockbackX);
            long normal = v * knockbackTime / GlobalVariables.TicksPerSecond;
            int extra = (int)(normal * s.ExtraPushbackPercent / 100);

            if (extra > 0)
            {
                int dx = defender.Body.FixedPosition.X - attacker.Body.FixedPosition.X;
                int dir = dx != 0 ? System.Math.Sign(dx) : -defender.Body.PlayerSide; // direction away from the attacker
                int roomDef = System.Math.Max(0, GlobalVariables.WallLimit - dir * defender.Body.FixedPosition.X - (int)normal);
                int roomAtt = System.Math.Max(0, GlobalVariables.WallLimit + dir * attacker.Body.FixedPosition.X);

                int defShare = System.Math.Min(extra / 2, roomDef);
                int attShare = System.Math.Min(extra - defShare, roomAtt);
                defShare = System.Math.Min(extra - attShare, roomDef); // a cornered attacker hands its share back

                Queue(defender, defShare * dir);
                Queue(attacker, -attShare * dir);
            }

            if (s.FlashVFXIndex >= 0 && defender.Pool != null)
            {
                try { defender.Pool.SpawnVFX(s.FlashVFXIndex, defender.Body.FixedPosition + new Vector2I(0, s.FlashHeight), defender.Body.PlayerSide, true); }
                catch (System.ArgumentOutOfRangeException) { GD.PushWarning("[Barrier] FlashVFXIndex is not in the fighter's VFX list."); }
            }
            return true;
        }

        static void Queue(SakugaActor a, int distance)
        {
            int frames = System.Math.Max(1, Settings.PushFrames);
            ref BarrierState b = ref a.Barrier;
            b.PushFramesLeft = frames;
            b.PushStep = distance / frames;
            b.PushRemainder = distance - b.PushStep * frames;
        }

        /// <summary>Damage after the Danger multiplier.</summary>
        public static int ApplyDanger(SakugaActor a, int damage)
        {
            if (a == null || !a.Barrier.Danger) return damage;
            return (int)((long)damage * Settings.DangerDamagePercent / 100);
        }
    }
}
