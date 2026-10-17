using Godot;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Punish and Reversal indicators, in the same style as the counter indicators: a hit notification on the player's
    /// side ("Punish" / "Reversal"), a flash VFX and a short color flash on the fighter.
    ///
    /// - PUNISH: a hit that lands while the opponent is in the recovery frames of a move (a blocked or whiffed attack,
    ///   a whiffed throw...). Visual only: damage and hitstun don't change. A counter hit (startup / active) stays a
    ///   Counter. Detected in Counter.OnHit, which calls OnPunish here.
    /// - REVERSAL: a move started on the very first frame a fighter can act after blockstun, hitstun, a knockdown /
    ///   getup, a tech or a roll (attacks, throws, jumps, super / double jumps, air dashes, run, step dash, backdash,
    ///   barrier). Checked every frame after the simulation (GameManager, [FF PATCH: indicators]).
    ///
    /// The "was in a reaction" flag lives in CounterState (saved for rollback), so a re-simulated frame gives the same
    /// answer. Both show in every mode; in Training, Display → Punish Indicator / Reversal Indicator turn them off.
    /// </summary>
    public static class Indicators
    {
        /// <summary>Universal states (UniversalLayout) that count as starting an action when they come straight out of a reaction.</summary>
        static readonly int[] ActionStates =
        {
            Locomotion.JumpNeutral, Locomotion.JumpForward, Locomotion.JumpBack,
            Locomotion.AirDashForward, Locomotion.AirDashBack,
            15, 16, 17, // Barrier Stand / Crouch / Air
            Locomotion.Run, Locomotion.StepDash, Locomotion.Backdash,
            Locomotion.SuperJumpNeutral, Locomotion.SuperJumpForward, Locomotion.SuperJumpBack,
            Locomotion.DoubleJumpNeutral, Locomotion.DoubleJumpForward, Locomotion.DoubleJumpBack,
        };
        /// <summary>
        /// States that Locomotion ends from code (Neutral Tech, air recovery, rolls): it switches them to Idle / Falling
        /// after moves were checked that frame, so the first frame a move can come out is the frame after. That one free
        /// frame still counts as "first possible".
        /// </summary>
        static readonly int[] CodeEndedStates = { Locomotion.NeutralTechState, Locomotion.AirRecovery, Locomotion.RollForward, Locomotion.RollBack };
        /// <summary>Burst (state 18) is done during hitstun, so what follows it isn't a reversal.</summary>
        const int BurstState = 18;

        public static bool PunishOn => !Training.TrainingController.Active || Training.TrainingSettings.ShowPunish;
        public static bool ReversalOn => !Training.TrainingController.Active || Training.TrainingSettings.ShowReversal;

        /// <summary>For tests / displays.</summary>
        public static int PunishCount, ReversalCount;
        public static int LastReversalPlayer = -1;
        public static string LastReversalState = "";

        /// <summary>A Punish landed on <paramref name="defender"/> (from Counter.OnHit).</summary>
        /// <param name="effects">false on a counter hit in recovery: the red counter bubble and flash win, only the
        /// "Punish" notification is added.</param>
        public static void OnPunish(SakugaActor attacker, SakugaActor defender, bool effects = true)
        {
            PunishCount++;
            if (!PunishOn) return;
            var s = Counter.Settings;
            Notify(attacker, s.PunishNotificationIndex);
            if (!effects) return;
            SpawnVFX(defender, s.PunishVFXIndex, s.VFXHeight);
            StateFlash.Flash(defender, StateFlash.PunishColor);
        }

        /// <summary>Every frame, after both fighters have moved (GameManager, next to Advantage.AfterTick).</summary>
        public static void AfterTick(SakugaActor[] fighters)
        {
            if (fighters == null) return;
            foreach (var f in fighters) if (f != null) Tick(f);
        }

        static void Tick(SakugaActor a)
        {
            if (a.StateManager == null || a.Body == null || a.OnHitstop()) return;
            ref CounterState c = ref a.Counter;

            var kind = Training.FrameMeter.Classify(a).Kind;
            bool stunned = kind == Training.FrameKind.Hitstun || kind == Training.FrameKind.Blockstun || kind == Training.FrameKind.Knockdown;
            int st = a.StateManager.CurrentState;
            if (stunned) { c.InReaction = true; c.ReactionState = st; c.ReactionGrace = false; return; }
            if (!c.InReaction) return;

            if (st == BurstState) { c.InReaction = false; return; }
            if (IsAction(a, st))
            {
                c.InReaction = false;
                OnReversal(a);
                return;
            }
            // Exit / getup / tech / roll frames keep waiting.
            if (kind != Training.FrameKind.Free) { c.ReactionState = st; return; }
            // Free to act. After a code-ended state the first possible frame is the next one; otherwise the chance is gone.
            if (!c.ReactionGrace && System.Array.IndexOf(CodeEndedStates, c.ReactionState) >= 0) { c.ReactionGrace = true; return; }
            c.InReaction = false;
        }

        static bool IsAction(SakugaActor a, int state)
        {
            if (a.StateManager.CurrentStateType() == StateType.COMBAT) return true;
            foreach (int s in ActionStates) if (s == state) return true;
            return false;
        }

        static void OnReversal(SakugaActor a)
        {
            ReversalCount++;
            LastReversalPlayer = (int)a.playerID;
            LastReversalState = a.StateManager.GetCurrentState()?.StateName ?? "";
            if (!ReversalOn) return;
            var s = Counter.Settings;
            Notify(a, s.ReversalNotificationIndex);
            SpawnVFX(a, s.ReversalVFXIndex, s.VFXHeight);
            StateFlash.Flash(a, StateFlash.ReversalColor);
        }

        static void Notify(SakugaActor a, int index)
        {
            var gm = GameManager.Instance;
            if (index < 0 || gm == null || gm.notifications == null || gm.notifications.Elements == null || index >= gm.notifications.Elements.Length) return;
            gm.PlayHitNotificationByIndex((int)a.playerID, index);
        }

        static void SpawnVFX(SakugaActor a, int index, int height)
        {
            if (index < 0 || a?.Pool == null) return;
            try { a.Pool.SpawnVFX(index, a.Body.FixedPosition + new Vector2I(0, height), a.Body.PlayerSide, true); }
            catch (System.ArgumentOutOfRangeException) { GD.PushWarning("[Indicators] VFX index " + index + " is not in the fighter's VFX list."); }
        }
    }
}
