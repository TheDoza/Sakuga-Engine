using SakugaEngine;
using SakugaEngine.Global;
using FunctionFighter.Mechanics;

namespace FunctionFighter.Training
{
    /// <summary>What a fighter was doing on one frame (one cell of the frame meter).</summary>
    public enum FrameKind : byte { Free, Movement, Startup, Active, Recovery, Hitstun, Blockstun, Knockdown }

    public struct FrameCell
    {
        public FrameKind Kind;
        /// <summary>Strike invincible this frame (white stripe on top).</summary>
        public bool Invuln;
        /// <summary>Throw invincible this frame (orange stripe at the bottom).</summary>
        public bool ThrowInvuln;
    }

    /// <summary>
    /// Strive / GBVS-style frame meter data. Records one cell per fighter per frame (hitstop frames are skipped),
    /// starting a new run when someone acts after both were free, and stopping a few frames after both are free again.
    /// Read by FrameMeterHUD. Training only, so it isn't part of rollback.
    /// </summary>
    public static class FrameMeter
    {
        public const int Columns = 78;
        /// <summary>Both free this many frames in a row ends the run.</summary>
        public const int EndGap = 3;

        public static readonly FrameCell[][] Cells = { new FrameCell[Columns], new FrameCell[Columns] };
        public static int Count;
        static bool _running;
        static int _gap;
        // Whole-run counters (not limited to the visible columns).
        static readonly int[] _startup = new int[2], _active = new int[2], _recovery = new int[2], _end = new int[2];
        static readonly bool[] _attackDone = new bool[2], _busy = new bool[2];
        static int _length;
        static bool _prevHitstop;

        public struct Summary
        {
            public bool HasAttack;
            public int Startup, Active, Recovery;
            public bool HasAdvantage;
            public int Advantage;
        }

        public static Summary GetSummary(int player)
        {
            var s = new Summary();
            if (_startup[player] + _active[player] + _recovery[player] > 0)
            {
                s.HasAttack = true;
                s.Startup = _startup[player] + 1; // the move hits on its first active frame
                s.Active = _active[player];
                s.Recovery = _recovery[player];
            }
            if (!_running && _busy[0] && _busy[1])
            {
                s.HasAdvantage = true;
                int other = 1 - player;
                s.Advantage = _end[other] - _end[player];
            }
            return s;
        }

        public static void Clear()
        {
            Count = 0; _running = false; _gap = 0; _length = 0;
            for (int p = 0; p < 2; p++) { _startup[p] = _active[p] = _recovery[p] = _end[p] = 0; _attackDone[p] = _busy[p] = false; }
        }

        public static FrameCell Classify(SakugaActor a)
        {
            var c = new FrameCell
            {
                Invuln = a.ContainsFrameProperty(FrameProperties.DAMAGE_IMUNITY),
                ThrowInvuln = a.ContainsFrameProperty(FrameProperties.THROW_IMUNITY),
            };
            var type = a.StateManager.CurrentStateType();
            switch (type)
            {
                case StateType.COMBAT:
                    var stage = a.Body?.CurrentHitbox?.animationStage ?? AnimationStage.STARTUP;
                    c.Kind = stage == AnimationStage.ACTIVE ? FrameKind.Active : stage == AnimationStage.RECOVERY ? FrameKind.Recovery : FrameKind.Startup;
                    break;
                case StateType.HIT_REACTION:
                    bool lying = a.Body.IsOnGround && a.StateManager.GetCurrentState().BaseStance == MasterStance.ON_THE_GROUND;
                    c.Kind = lying && a.HitstunType >= HitstunType.KNOCKDOWN ? FrameKind.Knockdown : FrameKind.Hitstun;
                    break;
                case StateType.BLOCKING:
                    c.Kind = a.BlockStun ? FrameKind.Blockstun : FrameKind.Free;
                    break;
                case StateType.MOVEMENT:
                    bool locked = a.ContainsFrameProperty(FrameProperties.LOCK_MOVE) || Locomotion.InPrejump(a) || a.Locomotion.Mode == LocoMode.Skid;
                    c.Kind = locked ? FrameKind.Movement : FrameKind.Free;
                    break;
                default: // NULL exit states, LOCKED (techs, throws being done to you...)
                    c.Kind = FrameKind.Movement;
                    break;
            }
            return c;
        }

        /// <summary>Once per simulation frame (TrainingController.AfterTick).</summary>
        public static void Record(SakugaActor[] fighters)
        {
            if (fighters == null || fighters.Length < 2 || fighters[0] == null || fighters[1] == null) return;
            // Hitstop doesn't count as frames, except the frame the hit lands (it starts this frame).
            bool stop = fighters[0].OnHitstop() || fighters[1].OnHitstop();
            bool wasStop = _prevHitstop;
            _prevHitstop = stop;
            if (stop && wasStop) return;

            var c0 = Classify(fighters[0]);
            var c1 = Classify(fighters[1]);
            bool busy = c0.Kind != FrameKind.Free || c1.Kind != FrameKind.Free;

            if (!_running)
            {
                if (!busy) return;
                Clear();
                _running = true;
            }

            if (busy) _gap = 0;
            else if (++_gap >= EndGap) { _running = false; }

            Add(0, c0);
            Add(1, c1);
            _length++;
            if (Count < Columns) Count++;
            else
            {
                // Scroll: drop the oldest column.
                for (int p = 0; p < 2; p++)
                    System.Array.Copy(Cells[p], 1, Cells[p], 0, Columns - 1);
            }
            Cells[0][Count - 1] = c0;
            Cells[1][Count - 1] = c1;
        }

        static void Add(int p, FrameCell c)
        {
            if (c.Kind != FrameKind.Free) { _busy[p] = true; _end[p] = _length + 1; }
            // Counts only the first attack of the run (startup / active / recovery in order).
            if (_attackDone[p]) return;
            switch (c.Kind)
            {
                case FrameKind.Startup: if (_active[p] == 0 && _recovery[p] == 0) _startup[p]++; break;
                case FrameKind.Active: if (_recovery[p] == 0) _active[p]++; break;
                case FrameKind.Recovery: _recovery[p]++; break;
                default:
                    if (_startup[p] + _active[p] + _recovery[p] > 0) _attackDone[p] = true;
                    break;
            }
        }
    }
}
