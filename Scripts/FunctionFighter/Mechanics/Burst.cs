using Godot;
using MessagePack;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>Per-fighter Burst state. Saved with the fighter for rollback (SakugaActorState key 17).</summary>
    [MessagePackObject]
    public struct BurstState
    {
        [Key(0)] public int Value;
        /// <summary>Burst is disabled until this fighter recovers (was thrown, or was hit by a Burst).</summary>
        [Key(1)] public bool Locked;
        /// <summary>Passive refill left over after PassiveSpeedPercent scaling, in hundredths of a point.</summary>
        [Key(2)] public int PassiveRemainder;
    }

    /// <summary>
    /// Burst (BBCF-style): A+B+C+D while getting hit, costs a full Burst gauge. The gauge is full at match start,
    /// carries over between rounds, refills slowly (faster at low health) and from hits taken.
    /// Called from SakugaActor / SakugaParameters ([FF PATCH: burst]).
    /// </summary>
    public static class Burst
    {
        public static BurstSettings Settings => BurstSettings.Get();

        static bool IsFighter(SakugaActor a) =>
            a != null && a.Inputs != null && a.Parameters != null && a.Parameters.Health != null && a.Body != null;

        static bool RoundRunning() =>
            GameManager.Instance == null || GameManager.Instance.Monitor == null ||
            GameManager.Instance.Monitor.MatchState == MatchState.ROUND_RUNNING;

        /// <summary>Full gauge. Called once when the fighter is created (not between rounds).</summary>
        public static void Init(SakugaActor a)
        {
            a.Burst = new BurstState { Value = Settings.MaxValue };
        }

        public static bool IsFull(SakugaActor a) => a.Burst.Value >= Settings.MaxValue;

        /// <summary>True when Burst can't be used right now even with a full gauge (shown crossed out on the HUD).</summary>
        public static bool IsDisabled(SakugaActor a) => a.Burst.Locked || a.SuperFlashing;

        /// <summary>Everything that has to be true for the Burst move to come out.</summary>
        public static bool CanBurst(SakugaActor a) =>
            IsFighter(a) && IsFull(a) && !IsDisabled(a) && !KO.IsKO(a) &&
            a.StateManager != null && a.StateManager.CurrentStateType() == StateType.HIT_REACTION;

        /// <summary>Every frame, before hitstop: passive refill and unlocking.</summary>
        public static void PreTick(SakugaActor a)
        {
            if (!IsFighter(a)) return;
            ref BurstState b = ref a.Burst;
            var s = Settings;

            // Thrown fighters can't Burst until they've recovered.
            if (a.HitstunType == HitstunType.GRABBED) b.Locked = true;
            // Recovered = no longer in a hit reaction.
            if (b.Locked && a.StateManager != null && a.StateManager.CurrentStateType() != StateType.HIT_REACTION && a.HitstunType != HitstunType.GRABBED)
                b.Locked = false;

            if (!RoundRunning() || b.Value >= s.MaxValue) return;
            int max = System.Math.Max(1, a.Data.MaxHealth);
            int healthPercent = a.Parameters.Health.CurrentValue * 100 / max;
            int gain = healthPercent <= s.LowHealthPercent ? s.LowHealthGain :
                       healthPercent < s.HighHealthPercent ? s.MidHealthGain : s.HighHealthGain;
            // Scaled by PassiveSpeedPercent; the fraction left over is kept (in hundredths) so nothing is lost to rounding.
            b.PassiveRemainder += gain * System.Math.Max(0, s.PassiveSpeedPercent);
            b.Value = System.Math.Min(s.MaxValue, b.Value + b.PassiveRemainder / 100);
            b.PassiveRemainder %= 100;
        }

        /// <summary>Gauge gained from taking a hit (not chip). <paramref name="damage"/> is the damage actually taken.</summary>
        public static void OnDamaged(SakugaActor a, int damage)
        {
            if (!IsFighter(a) || damage <= 0) return;
            var s = Settings;
            int gain;
            switch (s.GainMode)
            {
                case BurstGainMode.Simple:
                    gain = s.HitGainBase + damage * s.HitGainDamageMultiplier;
                    break;
                case BurstGainMode.PassiveOnly:
                    return;
                default:
                    int scaling = a.Parameters.Prorations != null ? a.Parameters.Prorations.CurrentDamageScaling : 100;
                    int lost = System.Math.Max(0, 100 - scaling); // how much the combo has been scaled down, in percent
                    gain = s.HitGainBase + damage * s.HitGainDamageMultiplier * lost / 100;
                    break;
            }
            a.Burst.Value = System.Math.Min(s.MaxValue, a.Burst.Value + gain);
        }

        /// <summary>Burst used: empty the gauge and play the Burst VFX.</summary>
        public static void Spend(SakugaActor a)
        {
            a.Burst.Value = 0;
            var s = Settings;
            if (s.BurstVFXIndex >= 0 && a.Pool != null)
            {
                try { a.Pool.SpawnVFX(s.BurstVFXIndex, a.Body.FixedPosition + new Vector2I(0, s.BurstVFXHeight), a.Body.PlayerSide, true); }
                catch (System.ArgumentOutOfRangeException) { GD.PushWarning("[Burst] BurstVFXIndex is not in the fighter's VFX list."); }
            }
        }
    }
}
