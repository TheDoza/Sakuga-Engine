using System.Collections.Generic;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Training
{
    public enum ReversalKind { None, Wakeup, Block, Hit, AirTech }

    /// <summary>
    /// Training dummy reversals and techs.
    ///
    /// When the dummy blocks or gets hit, a reversal is "armed". On the first frame the dummy could act
    /// (the same frame a perfectly timed button press would come out), it does the chosen action, or starts the
    /// recording. The situation decides whether it fires: Wakeup (after a knockdown or ground tech), Block,
    /// Hit (ground hitstun) or Air Tech. Delay adds frames after that first possible frame.
    ///
    /// The check runs inside StanceManager.CheckMoves ([FF PATCH: training reversal]), so timing matches a real input.
    /// Training is offline, so this state isn't part of rollback.
    /// </summary>
    public static class Reversal
    {
        public const int ExpireFrames = 20;
        public const int PopupFrames = 45;
        // Universal states
        const int AirRecoveryState = 13, GroundRecoveryState = 14, HitGroundSlide = 25, HitDown = 26, HitGetUp = 27;

        static bool _armed;
        static bool _lastWasBlock, _sawKnockdown, _sawAirTech, _sawGroundTech;
        static bool _delayStarted;
        static int _delayLeft;
        static int _freeFrames;
        static PlayerInputs _hold;
        static int _holdLeft;
        static SakugaActor _forcing;

        /// <summary>Text for the on-screen popup ("REVERSAL  5B Test Mid", "AIR TECH") and how long it shows.</summary>
        public static string PopupText = "";
        public static int PopupLeft;
        public static ReversalKind LastKind;

        /// <summary>True while a reversal / tech move is being checked or started: input conditions (66, 2~8, back + A+B)
        /// count as satisfied, everything else (meter, air actions left, surface) still applies.</summary>
        public static bool IsForcing(SakugaActor a) => a != null && _forcing == a;

        public static void Reset()
        {
            _armed = false;
            _delayStarted = false;
            _holdLeft = 0;
            _freeFrames = 0;
            _sawKnockdown = _sawAirTech = _sawGroundTech = _lastWasBlock = false;
        }

        static bool IsDummy(SakugaActor a) => TrainingController.Active && a != null && a.playerID == TrainingController.DummyIndex;

        /// <summary>The dummy is set to something the reversal options apply to (not played by a person or the CPU).</summary>
        static bool DummyAutomated =>
            TrainingSettings.Dummy == DummyMode.Stand || TrainingSettings.Dummy == DummyMode.Crouch || TrainingSettings.Dummy == DummyMode.Jump ||
            DummyMovement.IsMovementMode(TrainingSettings.Dummy);

        // ------------------------------------------------------------ events (SakugaActor hit code)

        public static void OnBlocked(SakugaActor dummy)
        {
            if (!IsDummy(dummy)) return;
            Arm(block: true);
        }

        public static void OnHit(SakugaActor dummy)
        {
            if (!IsDummy(dummy)) return;
            Arm(block: false);
        }

        static void Arm(bool block)
        {
            _armed = true;
            _delayStarted = false;
            _freeFrames = 0;
            _holdLeft = 0;
            _lastWasBlock = block;
            if (!block) { _sawKnockdown = _sawAirTech = _sawGroundTech = false; }
        }

        // ------------------------------------------------------------ per frame (TrainingController.AfterTick)

        public static void Tick(SakugaActor dummy)
        {
            if (PopupLeft > 0) PopupLeft--;
            if (dummy == null) return;
            if (dummy.StateManager.CurrentStateType() != StateType.HIT_REACTION) { _airPick = _groundPick = -1; _techDelayStarted = false; }
            int st = dummy.StateManager.CurrentState;
            bool knockedDown = dummy.HitstunType >= HitstunType.KNOCKDOWN || st == HitGroundSlide || st == HitDown || st == HitGetUp;
            // Knocked down without a normal hit event (e.g. thrown): still arm the wakeup reversal.
            if (!_armed && knockedDown && dummy.StateManager.CurrentStateType() == StateType.HIT_REACTION) Arm(block: false);
            if (!_armed) return;
            if (knockedDown) _sawKnockdown = true;
            if (st == AirRecoveryState) _sawAirTech = true;
            if (st == GroundRecoveryState || st == FunctionFighter.Mechanics.Locomotion.RollForward || st == FunctionFighter.Mechanics.Locomotion.RollBack) _sawGroundTech = true;

            // Never fired (the move isn't possible here, or this situation is switched off): give up after a while.
            if (Free(dummy))
            {
                _freeFrames++;
                if (_freeFrames > ExpireFrames + TrainingSettings.ReversalDelay) _armed = false;
            }
        }

        /// <summary>The dummy could start a move now: a MOVEMENT state (idle, walk, crouch...). The short NULL exit
        /// states after blockstun / getting up don't accept moves, so they don't count.</summary>
        static bool Free(SakugaActor a) =>
            a.StateManager.CurrentStateType() == StateType.MOVEMENT && !a.OnHitstun() && !a.OnHitstop() &&
            !a.ContainsFrameProperty(FrameProperties.LOCK_MOVE);

        public static ReversalKind CurrentKind()
        {
            if (_sawAirTech) return ReversalKind.AirTech;
            if (_sawKnockdown || _sawGroundTech) return ReversalKind.Wakeup;
            return _lastWasBlock ? ReversalKind.Block : ReversalKind.Hit;
        }

        static bool KindEnabled(ReversalKind k)
        {
            switch (k)
            {
                case ReversalKind.Wakeup: return TrainingSettings.ReversalOnWakeup;
                case ReversalKind.Block: return TrainingSettings.ReversalOnBlock;
                case ReversalKind.Hit: return TrainingSettings.ReversalOnHit;
                case ReversalKind.AirTech: return TrainingSettings.ReversalOnAirTech;
                default: return false;
            }
        }

        /// <summary>Counts the Delay setting down. False while still waiting.</summary>
        static bool DelayDone()
        {
            if (!_delayStarted) { _delayStarted = true; _delayLeft = TrainingSettings.ReversalDelay; }
            if (_delayLeft > 0) { _delayLeft--; return false; }
            return true;
        }

        // ------------------------------------------------------------ StanceManager.CheckMoves hook

        /// <summary>
        /// Called from StanceManager.CheckMoves before normal input matching. Returns true if it started a move
        /// (tech or reversal action) this frame; a Recording reversal starts playback here instead.
        /// </summary>
        public static bool Run(SakugaActor a, StanceManager sm)
        {
            if (!IsDummy(a) || sm == null) return false;
            if (TrainingController.Recording == RecordState.Recording || TrainingController.Recording == RecordState.Countdown) return false;
            if (!DummyAutomated && TrainingSettings.Dummy != DummyMode.Playback) return false;

            // Tech first (any automated dummy, including Playback).
            if (TryTech(a, sm)) return true;

            if (!DummyAutomated || !_armed || TrainingSettings.Reversal == ReversalMode.Off) return false;
            if (TrainingController.Recording == RecordState.Playing) return false;

            if (TrainingSettings.Reversal == ReversalMode.Recording)
            {
                if (!Free(a) || !TrainingController.HasRecording) return false;
                var kind = CurrentKind();
                if (!KindEnabled(kind)) { _armed = false; return false; }
                if (!DelayDone()) return false;
                _armed = false;
                LastKind = kind;
                Popup("REVERSAL  " + KindName(kind) + "  (recording)");
                // Playback starts this very frame: frame 0 of the recording goes into this frame's input.
                TrainingController.StartReversalPlayback(a);
                return false;
            }

            var action = SelectedAction(a);
            if (action == null) { _armed = false; return false; }
            int index = FindMove(sm, action.MoveName);
            if (index < 0) { _armed = false; return false; }
            if (!CanExecute(a, sm, index)) return false;
            var k = CurrentKind();
            if (!KindEnabled(k)) { _armed = false; return false; }
            if (!DelayDone()) return false;

            _armed = false;
            Execute(a, sm, index);
            StartHold(a, action);
            LastKind = k;
            Popup("REVERSAL  " + action.Label + "   (" + KindName(k) + ")");
            return true;
        }

        // Tech choice for the current knockdown / air hit (Random picks once), and its delay.
        static int _airPick = -1, _groundPick = -1;
        static bool _techDelayStarted, _techDelayAir;
        static int _techDelayLeft;
        static readonly System.Random _techRandom = new System.Random(4321);

        static bool TechDelayDone(bool air)
        {
            // Air and ground count separately (an air hit can end in a knockdown).
            if (!_techDelayStarted || _techDelayAir != air) { _techDelayStarted = true; _techDelayAir = air; _techDelayLeft = TrainingSettings.TechDelay; }
            if (_techDelayLeft > 0) { _techDelayLeft--; return false; }
            return true;
        }

        static bool TryTech(SakugaActor a, StanceManager sm)
        {
            if (a.StateManager.CurrentStateType() != StateType.HIT_REACTION) return false;
            bool air = !a.Body.IsOnGround;
            if (air)
            {
                var m = TrainingSettings.AirTech;
                if (m == AirTechMode.Off) return false;
                if (m == AirTechMode.Random) { if (_airPick < 0) _airPick = _techRandom.Next(3); m = (AirTechMode)(1 + _airPick); }
                int i = FindMove(sm, "Air Recovery");
                if (i < 0 || !CanExecute(a, sm, i) || !TechDelayDone(true)) return false;
                int dir = m == AirTechMode.Forward ? 1 : m == AirTechMode.Back ? -1 : 0;
                FunctionFighter.Mechanics.Locomotion.ForcedAirTechDir = dir;
                Execute(a, sm, i);
                Popup(dir > 0 ? "AIR TECH  (FORWARD)" : dir < 0 ? "AIR TECH  (BACK)" : "AIR TECH");
                return true;
            }

            if (a.StateManager.CurrentState != FunctionFighter.Mechanics.Locomotion.HitDown) return false;
            var g = TrainingSettings.GroundTech;
            if (g == GroundTechMode.Off) return false;
            if (g == GroundTechMode.Random) { if (_groundPick < 0) _groundPick = _techRandom.Next(4); g = (GroundTechMode)(1 + _groundPick); }
            if (g == GroundTechMode.Emergency)
            {
                if (!FunctionFighter.Mechanics.Locomotion.InEmergencyWindow(a) || !TechDelayDone(false)) return false;
                FunctionFighter.Mechanics.Locomotion.StartNeutralTech(a);
                Popup("EMERGENCY TECH");
                return true;
            }
            string name = g == GroundTechMode.ForwardRoll ? "Forward Roll" : g == GroundTechMode.BackRoll ? "Back Roll" : "Ground Recovery";
            int idx = FindMove(sm, name);
            if (idx < 0 || !CanExecute(a, sm, idx) || !TechDelayDone(false)) return false;
            Execute(a, sm, idx);
            Popup(g == GroundTechMode.ForwardRoll ? "FORWARD ROLL" : g == GroundTechMode.BackRoll ? "BACK ROLL" : "NEUTRAL TECH");
            return true;
        }

        static bool CanExecute(SakugaActor a, StanceManager sm, int index)
        {
            _forcing = a;
            try { return sm.CanExecuteNow(index); }
            finally { _forcing = null; }
        }

        static void Execute(SakugaActor a, StanceManager sm, int index)
        {
            _forcing = a;
            try { sm.ExecuteMove(index); }
            finally { _forcing = null; }
        }

        static void Popup(string text)
        {
            PopupText = text;
            PopupLeft = PopupFrames;
        }

        public static string KindName(ReversalKind k) =>
            k == ReversalKind.AirTech ? "AIR TECH" : k == ReversalKind.Wakeup ? "WAKEUP" : k == ReversalKind.Block ? "BLOCK" : "HIT";

        // ------------------------------------------------------------ hold input after the move starts

        static void StartHold(SakugaActor a, ReversalAction action)
        {
            _holdLeft = 0;
            if (string.IsNullOrEmpty(action.Hold) || action.HoldFrames <= 0) return;
            _hold = Notation.ToInputs(action.Hold, a.Body.PlayerSide);
            _holdLeft = action.HoldFrames;
            // This frame already read its input: hold it from now so the move's own checks (run, Barrier) see it.
            a.Inputs.InsertToHistory(_hold);
        }

        /// <summary>The dummy's input this frame while a reversal holds something (TrainingController.FilterInput).</summary>
        public static bool HoldInput(out PlayerInputs input)
        {
            input = 0;
            if (_holdLeft <= 0) return false;
            _holdLeft--;
            input = _hold;
            return true;
        }

        // ------------------------------------------------------------ the dummy's action list

        /// <summary>The reversal actions this fighter can do (its own moves first, then the universal ones it has).</summary>
        public static List<ReversalAction> Actions(SakugaActor a)
        {
            var result = new List<ReversalAction>();
            if (a == null || a.StanceManager == null) return result;
            var own = a.Data?.TrainingReversals;
            void AddFrom(ReversalList list)
            {
                if (list?.Actions == null) return;
                foreach (var act in list.Actions)
                    if (act != null && FindMove(a.StanceManager, act.MoveName) >= 0 && !result.Exists(x => x.MoveName == act.MoveName))
                        result.Add(act);
            }
            AddFrom(own);
            if (own == null || own.IncludeUniversal) AddFrom(ReversalList.Universal);
            return result;
        }

        public static ReversalAction SelectedAction(SakugaActor a)
        {
            var list = Actions(a);
            if (list.Count == 0) return null;
            var found = list.Find(x => x.MoveName == TrainingSettings.ReversalMove);
            return found ?? list[0];
        }

        /// <summary>Training menu: step through the dummy's actions.</summary>
        public static void CycleAction(SakugaActor a, int dir)
        {
            var list = Actions(a);
            if (list.Count == 0) return;
            int i = list.FindIndex(x => x.MoveName == TrainingSettings.ReversalMove);
            if (i < 0) i = 0;
            i = ((i + dir) % list.Count + list.Count) % list.Count;
            TrainingSettings.ReversalMove = list[i].MoveName;
        }

        public static int FindMove(StanceManager sm, string name)
        {
            if (sm == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < sm.GetMoveListLength(); i++)
                if (sm.GetMove(i) != null && sm.GetMove(i).MoveName == name) return i;
            return -1;
        }
    }

    /// <summary>Numpad notation → inputs, relative to facing (6 = toward the opponent). "4AB" = back + A + B.</summary>
    public static class Notation
    {
        public static PlayerInputs ToInputs(string text, int facing)
        {
            PlayerInputs i = 0;
            if (string.IsNullOrEmpty(text)) return i;
            PlayerInputs fwd = facing > 0 ? PlayerInputs.RIGHT : PlayerInputs.LEFT;
            PlayerInputs back = facing > 0 ? PlayerInputs.LEFT : PlayerInputs.RIGHT;
            foreach (char c in text.ToUpperInvariant())
            {
                switch (c)
                {
                    case '1': i |= PlayerInputs.DOWN | back; break;
                    case '2': i |= PlayerInputs.DOWN; break;
                    case '3': i |= PlayerInputs.DOWN | fwd; break;
                    case '4': i |= back; break;
                    case '6': i |= fwd; break;
                    case '7': i |= PlayerInputs.UP | back; break;
                    case '8': i |= PlayerInputs.UP; break;
                    case '9': i |= PlayerInputs.UP | fwd; break;
                    case 'A': i |= PlayerInputs.FACE_A; break;
                    case 'B': i |= PlayerInputs.FACE_B; break;
                    case 'C': i |= PlayerInputs.FACE_C; break;
                    case 'D': i |= PlayerInputs.FACE_D; break;
                }
            }
            return i;
        }
    }
}
