using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Training
{
    /// <summary>
    /// "Full dummy movement": Dummy modes that move on their own with the real universal movement inputs, so you can
    /// practise hitting a fighter that walks, runs, backdashes, jumps, super jumps, double jumps or air dashes
    /// (anti-airs, whiff punishes, run-in checks). Each mode is a short input script relative to the dummy's facing,
    /// looped. "Random Movement" picks a new script every loop.
    ///
    /// - The script starts over whenever the dummy is hit or blocks, so it never resumes half a motion.
    /// - "Wait" steps hold nothing until the dummy is back on the ground and free to act.
    /// - Guard settings still apply (except while running / skidding, which can't guard, same as a player).
    /// - Reversal settings still apply (the reversal takes over while it plays).
    /// Training only (offline), so this static state isn't part of rollback.
    /// </summary>
    public static class DummyMovement
    {
        const int F = 1, B = 2, U = 4, D = 8;
        /// <summary>A step: hold <c>dir</c> (F/B/U/D bits) for <c>frames</c>; frames = -1 means wait until grounded + free.</summary>
        readonly struct Step { public readonly int Dir, Frames; public Step(int d, int f) { Dir = d; Frames = f; } }
        static Step S(int dir, int frames) => new Step(dir, frames);
        static readonly Step Wait = new Step(0, -1);

        /// <summary>Frames of idle between loops (gives you a window to act).</summary>
        public static int Gap = 14;

        static readonly Step[] WalkForward = { S(F, 1) };
        static readonly Step[] WalkBack = { S(B, 1) };
        static readonly Step[] Run = { S(F, 2), S(0, 2), S(F, 36), Wait };
        static readonly Step[] Backdash = { S(B, 2), S(0, 2), S(B, 2), Wait };
        static readonly Step[] JumpForward = { S(U | F, 3), Wait };
        static readonly Step[] JumpBack = { S(U | B, 3), Wait };
        static readonly Step[] SuperJump = { S(D, 2), S(U, 3), Wait };
        static readonly Step[] DoubleJump = { S(U, 2), S(0, 13), S(U, 2), Wait };
        static readonly Step[] AirDash = { S(U, 2), S(0, 11), S(F, 2), S(0, 2), S(F, 2), Wait };
        static readonly Step[][] RandomPool = { WalkForward, WalkBack, Run, Backdash, JumpForward, JumpBack, SuperJump, DoubleJump, AirDash };
        /// <summary>Random Movement walks for this many frames when it picks a walk.</summary>
        public static int RandomWalkFrames = 40;

        static Step[] _script;
        static int _step, _stepFrame, _gap, _randomWalk;
        static readonly System.Random _random = new System.Random(4321);

        public static bool IsMovementMode(DummyMode m) => m >= DummyMode.WalkForward && m <= DummyMode.RandomMovement;

        public static void Reset()
        {
            _script = null;
            _step = _stepFrame = _gap = _randomWalk = 0;
        }

        static Step[] ScriptFor(DummyMode m) => m switch
        {
            DummyMode.WalkForward => WalkForward,
            DummyMode.WalkBack => WalkBack,
            DummyMode.Run => Run,
            DummyMode.Backdash => Backdash,
            DummyMode.JumpForward => JumpForward,
            DummyMode.JumpBack => JumpBack,
            DummyMode.SuperJump => SuperJump,
            DummyMode.DoubleJump => DoubleJump,
            DummyMode.AirDash => AirDash,
            _ => null,
        };

        static bool Free(SakugaActor a) => a.Body.IsOnGround && a.StateManager.CurrentStateType() <= StateType.MOVEMENT;

        /// <summary>This frame's input for the dummy in a movement mode.</summary>
        public static PlayerInputs Input(SakugaActor dummy)
        {
            var mode = TrainingSettings.Dummy;
            var type = dummy.StateManager.CurrentStateType();
            if (type == StateType.HIT_REACTION || type == StateType.BLOCKING) { Reset(); _gap = Gap; return 0; }

            if (_gap > 0) { if (Free(dummy)) _gap--; return 0; }

            if (_script == null)
            {
                if (mode == DummyMode.RandomMovement)
                {
                    _script = RandomPool[_random.Next(RandomPool.Length)];
                    _randomWalk = _script.Length == 1 ? RandomWalkFrames : 0;
                }
                else _script = ScriptFor(mode);
                if (_script == null) return 0;
                _step = _stepFrame = 0;
            }

            // Walks loop forever (one step); Random Movement walks for a while, then picks again.
            if (_script.Length == 1)
            {
                int walkDir = _script[0].Dir;
                if (mode == DummyMode.RandomMovement && --_randomWalk <= 0) { _script = null; _gap = Gap / 2; }
                return ToInput(walkDir, dummy);
            }

            var st = _script[_step];
            int dir = 0;
            bool done;
            if (st.Frames < 0) done = _stepFrame > 2 && Free(dummy);
            else { dir = st.Dir; done = _stepFrame + 1 >= st.Frames; }
            _stepFrame++;
            if (done)
            {
                _step++;
                _stepFrame = 0;
                if (_step >= _script.Length) { _script = null; _gap = Gap; }
            }
            return ToInput(dir, dummy);
        }

        /// <summary>Forward = toward the opponent (the dummy's facing).</summary>
        static PlayerInputs ToInput(int dir, SakugaActor a)
        {
            PlayerInputs i = 0;
            bool right = a.Body.IsLeftSide;
            if ((dir & F) != 0) i |= right ? PlayerInputs.RIGHT : PlayerInputs.LEFT;
            if ((dir & B) != 0) i |= right ? PlayerInputs.LEFT : PlayerInputs.RIGHT;
            if ((dir & U) != 0) i |= PlayerInputs.UP;
            if ((dir & D) != 0) i |= PlayerInputs.DOWN;
            return i;
        }

        public static string Name(DummyMode m) => m switch
        {
            DummyMode.WalkForward => "Walk Forward",
            DummyMode.WalkBack => "Walk Back",
            DummyMode.Run => "Run",
            DummyMode.Backdash => "Backdash",
            DummyMode.JumpForward => "Jump Forward",
            DummyMode.JumpBack => "Jump Back",
            DummyMode.SuperJump => "Super Jump",
            DummyMode.DoubleJump => "Double Jump",
            DummyMode.AirDash => "Air Dash",
            DummyMode.RandomMovement => "Random Movement",
            _ => m.ToString(),
        };
    }
}
