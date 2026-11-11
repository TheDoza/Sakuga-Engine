using Godot;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Per move (MoveSettings.Fakeout): which Function Fakeout rules the move follows.</summary>
    public enum FakeoutMoveType
    {
        /// <summary>Worked out from the move: SuperGaugeRequired > 0 = Super, a motion input (236, 623, [4]6...) = Special,
        /// anything else = Normal. Risky has to be set by hand.</summary>
        Auto,
        /// <summary>Can't be Fakeout cancelled at all.</summary>
        None,
        Normal,
        Special,
        /// <summary>Invincible reversals, command grabs and other moves that should be expensive to make safe.</summary>
        Risky,
        Super,
    }

    /// <summary>Which Fakeout comes out (decides the cost, ring color, notification and slowdown).</summary>
    public enum FakeoutColor { None, Blue, Red, Purple, Gold }

    /// <summary>
    /// Function Fakeout: press A+B+C during a move to cancel it and return to neutral (Idle on the ground, Falling in the
    /// air, keeping the air momentum). A colored ring of light plays around the fighter and a notification shows on their
    /// side. A press that can't Fakeout shows a red X over the fighter instead. Universal: every fighter has it.
    ///
    /// - BLUE (25%): startup or recovery of a Special, opponent not in hitstun or blockstun.
    /// - RED (50%): a Normal, Special or Risky move while the opponent is in hitstun, blockstun or a knockdown.
    /// - PURPLE (75%): a Risky move that was blocked (opponent in blockstun) or whiffed (its recovery).
    /// - GOLD (50%): active or recovery frames of a Super. Supers cost 50%, so super + Gold = the whole gauge.
    ///
    /// No Fakeout: whiffed normals, the active frames of a whiffed special, the startup of a risky move or a super,
    /// throws that connected (opponent grabbed), any move set to None, and the frames / states a move blocks
    /// (MoveSettings.FakeoutBlockedFrames / FakeoutBlockedStates).
    ///
    /// The input buffer lives in CounterState (saved for rollback). Called from StanceManager.CheckMoves ([FF PATCH: fakeout]).
    /// </summary>
    public static class Fakeout
    {
        public static FakeoutSettings Settings => FakeoutSettings.Get();

        /// <summary>For tests / displays.</summary>
        public static int Count, FailCount;
        public static FakeoutColor LastColor = FakeoutColor.None;
        public static int LastPlayer = -1;

        const PlayerInputs Buttons = PlayerInputs.FACE_A | PlayerInputs.FACE_B | PlayerInputs.FACE_C;

        static bool IsFighter(SakugaActor a) =>
            a != null && a.Inputs != null && a.Parameters != null && a.Parameters.SuperGauge != null &&
            a.Body != null && a.StateManager != null && a.StanceManager != null;

        /// <summary>A+B+C all held, and the last of them went down this frame.</summary>
        static bool Pressed(SakugaActor a)
        {
            var inputs = a.Inputs;
            if (inputs.InputHistory == null) return false;
            int i = inputs.CurrentHistory;
            if ((inputs.InputHistory[i].rawInput & Buttons) != Buttons) return false;
            if (inputs.InputHistory[i].duration != 1) return false;
            int prev = (i - 1 + GlobalVariables.InputHistorySize) % GlobalVariables.InputHistorySize;
            return (inputs.InputHistory[prev].rawInput & Buttons) != Buttons;
        }

        /// <summary>
        /// Every frame from StanceManager.CheckMoves (also during hitstop, so it can be done on the hit). Returns true when
        /// a Fakeout came out; normal move input is then skipped for this frame.
        /// </summary>
        public static bool Try(SakugaActor a, StanceManager sm)
        {
            var s = Settings;
            if (!IsFighter(a) || sm == null || !s.Enabled) return false;
            ref CounterState c = ref a.Counter;

            // The press ran out without a Fakeout coming out: red X over the fighter.
            if (c.FakeoutBuffer > 0 && --c.FakeoutBuffer == 0) { Fail(a); }
            // Only a press made during a move counts: A+B+C in neutral starts a normal / throw, and that move must not
            // be Fakeout cancelled on its first frames by the same press.
            if (Pressed(a) && a.StateManager.CurrentStateType() == StateType.COMBAT)
                c.FakeoutBuffer = System.Math.Max(1, s.InputBufferFrames);
            if (c.FakeoutBuffer <= 0) return false;

            var color = Available(a);
            if (color == FakeoutColor.None) return false;
            int cost = s.Cost(color);
            if (a.Parameters.SuperGauge.CurrentValue < cost) return false;

            c.FakeoutBuffer = 0;
            Perform(a, sm, color, cost);
            return true;
        }

        /// <summary>The Fakeout this fighter could do right now (ignores meter), or None.</summary>
        public static FakeoutColor Available(SakugaActor a)
        {
            if (!IsFighter(a) || KO.IsKO(a)) return FakeoutColor.None;
            if (a.StateManager.CurrentStateType() != StateType.COMBAT) return FakeoutColor.None;
            var move = CurrentMove(a);
            if (move == null) return FakeoutColor.None;
            var type = TypeOf(move);
            if (type == FakeoutMoveType.None) return FakeoutColor.None;
            if (IsBlocked(a, move)) return FakeoutColor.None;

            var opp = a.GetOpponent(0);
            if (opp == null || opp.StateManager == null || opp.Body == null) return FakeoutColor.None;
            if (opp.HitstunType == HitstunType.GRABBED) return FakeoutColor.None;

            var oppKind = Training.FrameMeter.Classify(opp).Kind;
            bool oppHit = oppKind == Training.FrameKind.Hitstun || oppKind == Training.FrameKind.Knockdown;
            bool oppBlock = oppKind == Training.FrameKind.Blockstun;
            var phase = Training.FrameMeter.Classify(a).Kind;

            switch (type)
            {
                case FakeoutMoveType.Super:
                    return phase == Training.FrameKind.Active || phase == Training.FrameKind.Recovery ? FakeoutColor.Gold : FakeoutColor.None;
                case FakeoutMoveType.Risky:
                    if (oppHit) return FakeoutColor.Red;
                    bool wasBlocked = (a.CancelConditions & CancelCondition.BLOCK_CANCEL) != 0;
                    bool recoveryOk = phase == Training.FrameKind.Recovery && (move.FakeoutPurpleOnWhiff || wasBlocked);
                    if (oppBlock || recoveryOk) return FakeoutColor.Purple;
                    return FakeoutColor.None;
                case FakeoutMoveType.Special:
                    if (oppHit || oppBlock) return FakeoutColor.Red;
                    if (phase == Training.FrameKind.Startup || phase == Training.FrameKind.Recovery) return FakeoutColor.Blue;
                    return FakeoutColor.None;
                default: // Normal
                    return oppHit || oppBlock ? FakeoutColor.Red : FakeoutColor.None;
            }
        }

        /// <summary>The move blocks Fakeout right now: one of its FakeoutBlockedStates is playing, or its own state is on a
        /// frame inside one of its FakeoutBlockedFrames ranges (inclusive).</summary>
        static bool IsBlocked(SakugaActor a, MoveSettings move)
        {
            int state = a.StateManager.CurrentState;
            if (move.FakeoutBlockedStates != null)
                foreach (int st in move.FakeoutBlockedStates) if (st == state) return true;
            if (move.FakeoutBlockedFrames != null && state == move.MoveState)
            {
                int frame = a.StateManager.CurrentStateFrame;
                foreach (var r in move.FakeoutBlockedFrames) if (frame >= r.X && frame <= r.Y) return true;
            }
            return false;
        }

        /// <summary>Resolves Auto into Normal / Special / Super.</summary>
        public static FakeoutMoveType TypeOf(MoveSettings move)
        {
            if (move == null) return FakeoutMoveType.None;
            if (move.Fakeout != FakeoutMoveType.Auto) return move.Fakeout;
            if (move.SuperGaugeRequired > 0) return FakeoutMoveType.Super;
            if (Indicators.IsSpecialInput(move.Inputs)) return FakeoutMoveType.Special;
            return FakeoutMoveType.Normal;
        }

        /// <summary>The move the fighter is in: the stance manager's current move, or else the move that plays this state.</summary>
        static MoveSettings CurrentMove(SakugaActor a)
        {
            var sm = a.StanceManager;
            var stance = sm.GetCurrentStance();
            if (stance?.Moves == null) return null;
            if (sm.CurrentMove >= 0 && sm.CurrentMove < stance.Moves.Length && stance.Moves[sm.CurrentMove] != null)
                return stance.Moves[sm.CurrentMove];
            int state = a.StateManager.CurrentState;
            foreach (var m in stance.Moves) if (m != null && m.MoveState == state) return m;
            return null;
        }

        static void Perform(SakugaActor a, StanceManager sm, FakeoutColor color, int cost)
        {
            var s = Settings;
            a.Parameters.SuperGauge.SpendSuperGauge(cost);

            // Cancel the move and anything buffered.
            if (sm.CurrentMove >= 0) sm.EndMove();
            sm.BufferedMove = -1;
            sm.CancelBuffer = false;
            if (sm.MoveBuffer != null) sm.MoveBuffer.Stop();

            // Back to neutral. On the ground the fighter stops; in the air the momentum carries on.
            bool ground = a.Body.IsOnGround;
            a.StateManager.PlayState(ground ? s.GroundState : s.AirState, true);
            ref LocomotionState l = ref a.Locomotion;
            l.SpecialLanding = 0;
            l.SpecialLandLeft = 0;
            if (ground || !s.KeepAirMomentum)
            {
                a.Body.SetVelocity(Vector2I.Zero);
                l.DashVelocity = 0;
            }
            a.Counter.WasAttacking = false; // counts as a cancel (no Functional Counter auto-dash)

            Count++;
            LastColor = color;
            LastPlayer = (int)a.playerID;

            Notify(a, s.NotificationIndex(color));
            SpawnVFX(a, s.VFXIndex(color), s.VFXHeight);
        }

        /// <summary>A+B+C during a move that couldn't Fakeout (not allowed there, or not enough meter).</summary>
        static void Fail(SakugaActor a)
        {
            FailCount++;
            SpawnVFX(a, Settings.FailVFXIndex, Settings.FailVFXHeight);
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
            catch (System.ArgumentOutOfRangeException) { GD.PushWarning("[Fakeout] VFX index " + index + " is not in the fighter's VFX list."); }
        }
    }
}
