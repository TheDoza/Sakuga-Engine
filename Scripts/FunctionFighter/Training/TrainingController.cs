using Godot;
using SakugaEngine;
using SakugaEngine.Game;
using SakugaEngine.Global;
using FunctionFighter.Mechanics;

namespace FunctionFighter.Training
{
    /// <summary>Countdown = the 3-2-1 before recording starts (player 1 already controls the dummy, nothing is stored yet).</summary>
    public enum RecordState { Idle, Recording, Playing, Countdown }

    /// <summary>
    /// Applies the Training Settings while a Training match runs. Called from GameManager ([FF PATCH: training]):
    /// FilterInput for every player's input each frame, AfterTick after the simulation step, and the health refill.
    /// Player 2 is the dummy. Outside Training every method does nothing.
    ///
    /// Training is offline only, so the recording buffer and guard flags here aren't part of the rollback state.
    /// </summary>
    public static class TrainingController
    {
        public const int DummyIndex = 1;
        /// <summary>Longest recording, in frames (10 seconds).</summary>
        public const int MaxRecordFrames = 600;
        /// <summary>"After first hit" guard keeps blocking until the dummy has been free this many frames.</summary>
        public const int AfterHitGuardFrames = 20;
        /// <summary>Recording countdown length (3-2-1), in frames.</summary>
        public const int CountdownFrames = 90;
        /// <summary>"Play on Reset: After Round Call": READY for (RoundCallFrames - GoFrames) frames with nobody able to
        /// move, then GO! while the playback starts.</summary>
        public const int RoundCallFrames = 75, GoFrames = 30;

        public static RecordState Recording = RecordState.Idle;
        /// <summary>Recording slots (Recording → Slot picks which one records / plays; Playback: Random Slot picks among the
        /// filled ones each time).</summary>
        public const int Slots = 5;
        static readonly ushort[][] _buffers = MakeSlots();
        static readonly int[] _lengths = new int[Slots];
        static int _playSlot = -1;

        static ushort[][] MakeSlots()
        {
            var b = new ushort[Slots][];
            for (int i = 0; i < Slots; i++) b[i] = new ushort[MaxRecordFrames];
            return b;
        }
        static int _playIndex;

        static bool _afterHitGuard;
        static int _freeFrames;
        static bool _randomGuard;
        static bool _wasNeutral;
        /// <summary>Hits the dummy has blocked in the current string (reset when it gets hit or is left alone).</summary>
        static int _blockedInString;
        static int _countdown;
        static int _roundCall;
        /// <summary>Frames both fighters have been free (Refill After Combo).</summary>
        static int _bothFree;

        public static int CountdownLeft => Recording == RecordState.Countdown ? _countdown : 0;
        /// <summary>Frames left of the READY / GO! call after a reset (0 = none).</summary>
        public static int RoundCallLeft => _roundCall;
        public static int BlockedInString => _blockedInString;
        static readonly System.Random _random = new System.Random(1234);

        /// <summary>The slot selected in the menu (0-based).</summary>
        public static int RecordSlot => Mathf.Clamp(TrainingSettings.RecordSlot, 0, Slots - 1);
        /// <summary>Frames recorded in the selected slot.</summary>
        public static int RecordedFrames => _lengths[RecordSlot];
        public static int SlotLength(int slot) => slot >= 0 && slot < Slots ? _lengths[slot] : 0;
        /// <summary>The slot playing now (0-based), -1 = none.</summary>
        public static int PlayingSlot => Recording == RecordState.Playing || TrainingSettings.Dummy == DummyMode.Playback ? _playSlot : -1;
        /// <summary>Something can be played back: the selected slot, or (Playback: Random Slot) any slot.</summary>
        public static bool HasRecording
        {
            get
            {
                if (!TrainingSettings.PlaybackRandom) return _lengths[RecordSlot] > 0;
                foreach (int l in _lengths) if (l > 0) return true;
                return false;
            }
        }

        /// <summary>Choose the slot to play: the selected one, or a random filled one. False = nothing to play.</summary>
        static bool PickPlaySlot()
        {
            _playSlot = -1;
            if (!TrainingSettings.PlaybackRandom)
            {
                if (_lengths[RecordSlot] > 0) _playSlot = RecordSlot;
                return _playSlot >= 0;
            }
            int filled = 0;
            foreach (int l in _lengths) if (l > 0) filled++;
            if (filled == 0) return false;
            int pick = _random.Next(filled);
            for (int i = 0; i < Slots; i++)
                if (_lengths[i] > 0 && pick-- == 0) { _playSlot = i; break; }
            return true;
        }
        public static int PlaybackFrame => _playIndex;

        public static bool Active
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || gm.Match == null || gm.Match.SelectedModeSettings == null) return false;
                var mode = gm.Match.SelectedModeSettings;
                // The mode name is a fallback: the IsTraining box can get dropped if Godot re-saves GM_Training.tres
                // before the C# project is rebuilt.
                return mode.IsTraining || mode.ModeName == "Training";
            }
        }

        /// <summary>Called when a Training match starts.</summary>
        public static void OnMatchStart()
        {
            TrainingSettings.Load();
            Recording = RecordState.Idle;
            _playIndex = 0;
            _afterHitGuard = false;
            _freeFrames = 0;
            _blockedInString = 0;
            _roundCall = 0;
            Reversal.Reset();
            DummyMovement.Reset();
            SaveStates.Clear();
            FrameAdvance.Reset();
        }

        /// <summary>A save state was loaded: stop recording / playback and start the guard bookkeeping fresh.</summary>
        public static void OnStateLoaded()
        {
            if (Recording != RecordState.Idle) Recording = RecordState.Idle;
            _playIndex = 0;
            _roundCall = 0;
            _afterHitGuard = false;
            _freeFrames = 0;
            _blockedInString = 0;
        }

        // ---------------------------------------------------------------- inputs

        public static PlayerInputs FilterInput(int player, PlayerInputs input, SakugaActor[] fighters, byte[] raw, int inputSize)
        {
            if (!Active || fighters == null || fighters.Length <= DummyIndex) return input;
            SakugaActor dummy = fighters[DummyIndex];

            // READY... GO! after a reset: nobody moves until GO!, then the playback starts.
            if (_roundCall > 0)
            {
                if (player == DummyIndex)
                {
                    _roundCall--;
                    if (_roundCall == GoFrames && PickPlaySlot()) { _playIndex = 0; Recording = RecordState.Playing; }
                }
                if (_roundCall >= GoFrames) return 0;
            }

            if (Recording == RecordState.Countdown)
            {
                // 3-2-1: player 1 already moves the dummy into place; nothing is stored yet.
                if (player != DummyIndex) return 0;
                if (--_countdown <= 0) { Recording = RecordState.Recording; _lengths[RecordSlot] = 0; }
                PlayerInputs p = (PlayerInputs)raw[0] | (PlayerInputs)(raw[1] << 8);
                return FunctionFighter.Input.ButtonMacros.Expand(p);
            }

            if (Recording == RecordState.Recording)
            {
                // Player 1's controls drive the dummy; player 1 stands still.
                if (player != DummyIndex) return 0;
                PlayerInputs p1 = 0;
                p1 |= (PlayerInputs)raw[0];
                p1 |= (PlayerInputs)(raw[1] << 8);
                p1 = FunctionFighter.Input.ButtonMacros.Expand(p1);
                int rs = RecordSlot;
                if (_lengths[rs] < MaxRecordFrames)
                    _buffers[rs][_lengths[rs]++] = (ushort)ToFacingRight(p1, dummy);
                else
                    StopRecording();
                return p1;
            }

            if (player != DummyIndex) return input;

            // A reversal action keeps holding its input (a run, a Barrier) for a few frames.
            if (Reversal.HoldInput(out PlayerInputs hold)) return hold;

            bool loop = TrainingSettings.Dummy == DummyMode.Playback;
            bool slotOk = _playSlot >= 0 && _lengths[_playSlot] > 0;
            if (loop && !slotOk && Recording != RecordState.Playing) { slotOk = PickPlaySlot(); _playIndex = 0; }
            if ((Recording == RecordState.Playing || loop) && slotOk)
            {
                if (_playIndex >= _lengths[_playSlot])
                {
                    _playIndex = 0;
                    if (!loop) { Recording = RecordState.Idle; return DummyIdleInput(input, dummy); }
                    // Looping: Random Slot picks again every time round.
                    if (!PickPlaySlot()) return DummyIdleInput(input, dummy);
                }
                return FromFacingRight((PlayerInputs)_buffers[_playSlot][_playIndex++], dummy);
            }

            return DummyIdleInput(input, dummy);
        }

        static PlayerInputs DummyIdleInput(PlayerInputs input, SakugaActor dummy)
        {
            // Switch guard modes set the posture: the first stance until a hit is blocked, then the other one.
            bool switchMode = TrainingSettings.Guard == GuardMode.SwitchStandToCrouch || TrainingSettings.Guard == GuardMode.SwitchCrouchToStand;
            bool automated = TrainingSettings.Dummy == DummyMode.Stand || TrainingSettings.Dummy == DummyMode.Crouch;
            if (switchMode && automated)
            {
                bool crouchFirst = TrainingSettings.Guard == GuardMode.SwitchCrouchToStand;
                bool crouch = _blockedInString == 0 ? crouchFirst : !crouchFirst;
                return crouch ? PlayerInputs.DOWN : 0;
            }
            if (DummyMovement.IsMovementMode(TrainingSettings.Dummy)) return DummyMovement.Input(dummy);
            switch (TrainingSettings.Dummy)
            {
                case DummyMode.Stand: return 0;
                case DummyMode.Crouch: return PlayerInputs.DOWN;
                case DummyMode.Jump: return PlayerInputs.UP;
                case DummyMode.Playback: return 0; // nothing recorded yet
                default: return input; // Controller, CPU
            }
        }

        /// <summary>Recordings are stored as if the dummy faced right, so they play back the same way on either side.</summary>
        static PlayerInputs ToFacingRight(PlayerInputs i, SakugaActor a) => a.Body.IsLeftSide ? i : SwapLeftRight(i);
        static PlayerInputs FromFacingRight(PlayerInputs i, SakugaActor a) => a.Body.IsLeftSide ? i : SwapLeftRight(i);

        static PlayerInputs SwapLeftRight(PlayerInputs i)
        {
            bool l = (i & PlayerInputs.LEFT) != 0, r = (i & PlayerInputs.RIGHT) != 0;
            i &= ~(PlayerInputs.LEFT | PlayerInputs.RIGHT);
            if (l) i |= PlayerInputs.RIGHT;
            if (r) i |= PlayerInputs.LEFT;
            return i;
        }

        // ---------------------------------------------------------------- recording

        /// <summary>Start (or stop) recording: player 1 controls the dummy until recording stops.</summary>
        public static void ToggleRecording()
        {
            if (Recording == RecordState.Recording || Recording == RecordState.Countdown) { StopRecording(); return; }
            _playIndex = 0;
            _roundCall = 0;
            if (TrainingSettings.RecordCountdown)
            {
                Recording = RecordState.Countdown;
                _countdown = CountdownFrames;
                return;
            }
            Recording = RecordState.Recording;
            _lengths[RecordSlot] = 0;
        }

        static void StopRecording()
        {
            Recording = RecordState.Idle;
            _playIndex = 0;
        }

        /// <summary>Play the recording once (or stop playback).</summary>
        public static void TogglePlayback()
        {
            if (Recording == RecordState.Recording || Recording == RecordState.Countdown) StopRecording();
            if (Recording == RecordState.Playing) { Recording = RecordState.Idle; return; }
            if (!PickPlaySlot()) return;
            _playIndex = 0;
            Recording = RecordState.Playing;
        }

        /// <summary>Reversal (Recording): play the recording from this frame on, starting at its first input (idle frames
        /// before it are skipped). That input goes into this frame's input, which the move check is about to read, so it
        /// lands on the first possible frame.</summary>
        public static void StartReversalPlayback(SakugaActor dummy)
        {
            if (!PickPlaySlot()) return;
            var buffer = _buffers[_playSlot];
            int length = _lengths[_playSlot];
            // Skip the idle frames before the first input, so the reversal isn't delayed by them.
            int start = 0;
            while (start < length - 1 && buffer[start] == 0) start++;
            Recording = RecordState.Playing;
            dummy.Inputs.InsertToHistory(FromFacingRight((PlayerInputs)buffer[start], dummy));
            _playIndex = start + 1;
        }

        /// <summary>Empty the selected slot.</summary>
        public static void ClearRecording()
        {
            _lengths[RecordSlot] = 0;
            _playIndex = 0;
            _playSlot = -1;
            Recording = RecordState.Idle;
        }

        /// <summary>Empty every slot.</summary>
        public static void ClearAllRecordings()
        {
            for (int i = 0; i < Slots; i++) _lengths[i] = 0;
            _playIndex = 0;
            _playSlot = -1;
            Recording = RecordState.Idle;
        }

        // ---------------------------------------------------------------- per frame

        public static void AfterTick(SakugaActor[] fighters)
        {
            if (!Active || fighters == null) return;

            // Refill After Combo: once both fighters have been free for RefillDelay frames (the exchange is over).
            bool allFree = true;
            foreach (var f in fighters)
                if (f == null || f.StateManager.CurrentStateType() > StateType.MOVEMENT) allFree = false;
            _bothFree = allFree ? _bothFree + 1 : 0;
            bool refillNow = _bothFree == TrainingSettings.RefillDelay;

            for (int i = 0; i < fighters.Length; i++)
            {
                var f = fighters[i];
                if (f == null || f.Parameters == null) continue;
                bool free = f.StateManager.CurrentStateType() <= StateType.MOVEMENT;

                if (refillNow && TrainingSettings.SuperMode == MeterMode.RefillAfterCombo && f.Parameters.SuperGauge != null)
                    f.Parameters.SuperGauge.CurrentValue = (int)((long)f.Data.MaxSuperGauge * TrainingSettings.SuperRefillPercent / 100);
                bool dangerDummy = i == DummyIndex && TrainingSettings.DummyBarrier == DummyBarrierMode.Danger;
                if (refillNow && TrainingSettings.BarrierMode == MeterMode.RefillAfterCombo && !dangerDummy)
                {
                    f.Barrier.Danger = false;
                    f.Barrier.DangerTimer = 0;
                    f.Barrier.Value = Barrier.Settings.MaxValue;
                }
                if (refillNow && TrainingSettings.BurstMode == MeterMode.RefillAfterCombo)
                    f.Burst.Value = BurstSettings.Get().MaxValue;

                if (TrainingSettings.InfiniteSuper && free && f.Parameters.SuperGauge != null)
                    f.Parameters.SuperGauge.CurrentValue = f.Data.MaxSuperGauge;

                var bar = Barrier.Settings;
                if (i == DummyIndex && TrainingSettings.DummyBarrier == DummyBarrierMode.Danger)
                    Barrier.ForceDanger(f);
                else if (TrainingSettings.InfiniteBarrier || (i == DummyIndex && TrainingSettings.DummyBarrier == DummyBarrierMode.Infinite))
                {
                    f.Barrier.Danger = false;
                    f.Barrier.DangerTimer = 0;
                    f.Barrier.Value = bar.MaxValue;
                }

                if (TrainingSettings.InfiniteBurst)
                    f.Burst.Value = BurstSettings.Get().MaxValue;
            }

            var dummy = fighters[DummyIndex];
            bool useCpu = TrainingSettings.Dummy == DummyMode.CPU && dummy.Brain != null && Recording != RecordState.Recording && Recording != RecordState.Countdown;
            if (dummy.UseAI != useCpu) dummy.UseAI = useCpu;

            UpdateGuard(dummy);
            Reversal.Tick(dummy);
            FrameMeter.Record(fighters);
        }

        // ---------------------------------------------------------------- hit events (SakugaActor, [FF PATCH: training reversal])

        public static void OnDummyBlocked(SakugaActor target)
        {
            if (!Active || target == null || target.playerID != DummyIndex) return;
            _blockedInString++;
            _freeFrames = 0;
            Reversal.OnBlocked(target);
        }

        public static void OnDummyHit(SakugaActor target)
        {
            if (!Active || target == null || target.playerID != DummyIndex) return;
            _blockedInString = 0; // got hit: the next time it's free it starts a fresh string
            Reversal.OnHit(target);
        }

        static void UpdateGuard(SakugaActor dummy)
        {
            var type = dummy.StateManager.CurrentStateType();
            bool neutral = type <= StateType.MOVEMENT;
            if (type == StateType.HIT_REACTION)
            {
                _afterHitGuard = true;
                _freeFrames = 0;
            }
            else if (neutral)
            {
                _freeFrames++;
                if (_freeFrames > AfterHitGuardFrames) { _afterHitGuard = false; _blockedInString = 0; }
            }
            if (neutral && !_wasNeutral) _randomGuard = _random.Next(2) == 0;
            _wasNeutral = neutral;
        }

        /// <summary>The dummy guards on its own right now (Guard setting). Read by GuardOverride.</summary>
        public static bool DummyGuards(SakugaActor a)
        {
            if (!Active || a == null || a.playerID != DummyIndex) return false;
            if (Recording == RecordState.Recording || Recording == RecordState.Countdown) return false;
            if (TrainingSettings.Dummy == DummyMode.Controller || TrainingSettings.Dummy == DummyMode.CPU) return false;
            switch (TrainingSettings.Guard)
            {
                case GuardMode.All: return true;
                case GuardMode.AfterFirstHit: return _afterHitGuard;
                case GuardMode.Random: return _randomGuard;
                case GuardMode.FirstHitOnly: return _blockedInString == 0;
                case GuardMode.BlockXHits: return _blockedInString < Mathf.Max(1, TrainingSettings.GuardHits);
                case GuardMode.SwitchStandToCrouch:
                case GuardMode.SwitchCrouchToStand: return true;
                default: return false;
            }
        }

        /// <summary>
        /// Which way the dummy guards this hit, or GuardOverride.None to not guard. Normally the right way (stand for
        /// overheads, crouch for lows). The Switch modes block the first hit of a string in their first stance (if that
        /// stance can block it) and every later hit in the other stance, right or wrong: that's what tests fuzzies.
        /// </summary>
        public static StanceSelect DummyGuardStance(SakugaActor a, HitType hit)
        {
            if (!DummyGuards(a)) return GuardOverride.None;
            if (!a.Body.IsOnGround) return StanceSelect.AIR;
            var g = TrainingSettings.Guard;
            if (g == GuardMode.SwitchStandToCrouch || g == GuardMode.SwitchCrouchToStand)
            {
                bool crouchFirst = g == GuardMode.SwitchCrouchToStand;
                if (_blockedInString == 0)
                {
                    if (hit == HitType.HIGH) return StanceSelect.GROUND;
                    if (hit == HitType.LOW) return StanceSelect.CROUCH;
                    return crouchFirst ? StanceSelect.CROUCH : StanceSelect.GROUND;
                }
                return crouchFirst ? StanceSelect.GROUND : StanceSelect.CROUCH;
            }
            switch (hit)
            {
                case HitType.HIGH: return StanceSelect.GROUND;
                case HitType.LOW: return StanceSelect.CROUCH;
                default: return a.IsCrouchState() ? StanceSelect.CROUCH : StanceSelect.GROUND;
            }
        }

        /// <summary>0 = rules as normal, 1 = every hit on the dummy is a Counter Hit, 2 = a Big Counter, -1 = never a counter.</summary>
        public static int ForcedCounter(SakugaActor defender)
        {
            if (!Active || defender == null || defender.playerID != DummyIndex) return 0;
            switch (TrainingSettings.Counter)
            {
                case CounterMode.Counter: return 1;
                case CounterMode.BigCounter: return 2;
                case CounterMode.Off: return -1;
                default: return 0;
            }
        }

        /// <summary>Functional Counter on/off: the Training setting, or CounterSettings.FunctionalCounterEnabled.</summary>
        public static bool FunctionalCounterEnabled(bool settingDefault)
        {
            if (!Active) return settingDefault;
            switch (TrainingSettings.FunctionalCounter)
            {
                case FunctionalCounterMode.On: return true;
                case FunctionalCounterMode.Off: return false;
                default: return settingDefault;
            }
        }

        // ---------------------------------------------------------------- health

        public static bool HealthRefillOn(int player) => !Active || TrainingSettings.HealthRefill;

        public static int HealthRefillValue(int player, SakugaActor f)
        {
            int max = f.Data.MaxHealth;
            if (!Active) return max;
            int pct = player == DummyIndex ? TrainingSettings.DummyHealthPercent : TrainingSettings.P1HealthPercent;
            return Mathf.Max(1, max * pct / 100);
        }

        // ---------------------------------------------------------------- reset

        /// <summary>Put both fighters back at the chosen position, full reset of states and timers.</summary>
        public static void ResetPositions(SakugaActor[] fighters) => ResetPositions(fighters, TrainingSettings.Position);

        /// <summary>
        /// Back button + direction (absolute, as seen on screen). Numpad notation:
        /// 5 midscreen · 4 left corner, P1 inside · 1 left corner, P2 inside · 2 midscreen, sides switched ·
        /// 3 right corner, P1 inside · 6 right corner, P2 inside. Up directions count as their neutral/left/right row.
        /// </summary>
        public static ResetPosition PositionForDirection(bool left, bool right, bool down)
        {
            if (down)
            {
                if (left) return ResetPosition.LeftCorner;      // 1
                if (right) return ResetPosition.P1RightCorner;  // 3
                return ResetPosition.CenterSwapped;             // 2
            }
            if (left) return ResetPosition.P1LeftCorner;        // 4
            if (right) return ResetPosition.RightCorner;        // 6
            return ResetPosition.Center;                        // 5
        }

        public static void ResetPositions(SakugaActor[] fighters, ResetPosition position)
        {
            if (fighters == null || fighters.Length < 2) return;
            foreach (var f in fighters) f.Reset();
            if (Recording == RecordState.Playing) Recording = RecordState.Idle;
            _blockedInString = 0;
            _afterHitGuard = false;
            Reversal.Reset();
            DummyMovement.Reset();

            int wall = GlobalVariables.WallLimit - 4000;
            int gap = GlobalVariables.StartingPosition * 2;
            int p1, p2;
            switch (position)
            {
                case ResetPosition.CenterSwapped: p1 = GlobalVariables.StartingPosition; p2 = -GlobalVariables.StartingPosition; break;
                case ResetPosition.LeftCorner: p2 = -wall; p1 = -wall + gap; break;
                case ResetPosition.RightCorner: p2 = wall; p1 = wall - gap; break;
                case ResetPosition.P1LeftCorner: p1 = -wall; p2 = -wall + gap; break;
                case ResetPosition.P1RightCorner: p1 = wall; p2 = wall - gap; break;
                default: p1 = -GlobalVariables.StartingPosition; p2 = GlobalVariables.StartingPosition; break;
            }
            Place(fighters[0], p1, p1 < p2);
            Place(fighters[1], p2, p2 < p1);

            for (int i = 0; i < fighters.Length; i++)
            {
                var f = fighters[i];
                f.Parameters.Health.CurrentValue = HealthRefillValue(i, f);
                f.Body.UpdateColliders();
            }

            // Play on Reset: start the recording now, or after a READY... GO! call.
            _roundCall = 0;
            bool recordingBusy = Recording == RecordState.Recording || Recording == RecordState.Countdown;
            if (!recordingBusy && HasRecording)
            {
                if (TrainingSettings.PlayOnReset == PlayOnResetMode.Immediately) { if (PickPlaySlot()) { _playIndex = 0; Recording = RecordState.Playing; } }
                else if (TrainingSettings.PlayOnReset == PlayOnResetMode.AfterRoundCall) { Recording = RecordState.Idle; _roundCall = RoundCallFrames; }
            }
        }

        static void Place(SakugaActor f, int x, bool leftSide)
        {
            f.Body.FixedPosition.X = x;
            f.Body.FixedPosition.Y = 0;
            f.Body.UpdateSide(leftSide);
            f.Body.FlipSide();
        }
    }
}
