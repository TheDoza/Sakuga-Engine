using Godot;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Training frame advance: freeze the match, then step it one frame at a time (hold Step to play it slowly).
    /// Inputs held while you press Step go into that frame, so you can check exact timings (meaties, gaps, link windows,
    /// reversal frames) frame by frame.
    ///
    /// Frame Advance button toggles it, Step Frame steps (bindable in the button config; keyboard 1 defaults F2 / F3;
    /// pressing Step while running also freezes). Also on the RESET page of the Training menu.
    /// GameManager.GameLoop asks HoldFrame() before simulating ([FF PATCH: frame advance]); nothing else changes, so the
    /// simulation is exactly the same as running normally. Training only (offline).
    /// </summary>
    public static class FrameAdvance
    {
        /// <summary>Hold Step this many rendered frames before it repeats, then step every RepeatEvery frames.</summary>
        public static int RepeatDelay = 18, RepeatEvery = 4;

        public static bool Frozen { get; private set; }
        /// <summary>Frames stepped since freezing (shown on screen).</summary>
        public static int Stepped { get; private set; }

        static int _pending;
        static int _passedThisRender;
        static ulong _frozenRenders;

        /// <summary>
        /// A rendered-frame clock that stops while frozen (and moves one tick per step). Visual effects timed in rendered
        /// frames (state flashes) use it so they freeze with the game. Same as Engine.GetProcessFrames() when never frozen.
        /// </summary>
        public static ulong Clock => Engine.GetProcessFrames() - _frozenRenders;

        public static void Toggle()
        {
            if (!TrainingController.Active) return;
            Frozen = !Frozen;
            _pending = 0;
            Stepped = 0;
        }

        /// <summary>Step one frame (freezes first if the game is running).</summary>
        public static void Step()
        {
            if (!TrainingController.Active) return;
            if (!Frozen) { Toggle(); return; }
            _pending++;
        }

        public static void Reset()
        {
            Frozen = false;
            _pending = 0;
            Stepped = 0;
        }

        /// <summary>GameManager.GameLoop: true = skip this frame (frozen and no step waiting).</summary>
        public static bool HoldFrame()
        {
            if (!Frozen) return false;
            if (!TrainingController.Active) { Reset(); return false; }
            if (_pending > 0)
            {
                _pending--;
                Stepped++;
                _passedThisRender++;
                return false;
            }
            return true;
        }

        /// <summary>Once per rendered frame (TrainingMenu._Process): keeps Clock still while frozen.</summary>
        public static void RenderTick()
        {
            if (Frozen && _passedThisRender == 0) _frozenRenders++;
            _passedThisRender = 0;
        }
    }
}
