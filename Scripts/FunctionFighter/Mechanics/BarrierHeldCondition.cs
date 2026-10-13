using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// True while the fighter holds back + A+B and can barrier (meter left, not in Danger).
    /// Used by the neutral barrier states (15 Barrier_Stand, 16 Barrier_Crouch, 17 Barrier_Air) to know when to stop.
    /// </summary>
    [GlobalClass]
    public partial class BarrierHeldCondition : FrameDataCondition
    {
        /// <summary>Flip the result (true when the barrier is NOT held).</summary>
        [Export] private bool Not;
        /// <summary>Also require down to be held / not held. 0 = don't care, 1 = down held, 2 = down not held.</summary>
        [Export] private int Down;

        public override bool Check(ref SakugaActor Actor)
        {
            if (Actor == null || Actor.Inputs == null) return Not;
            bool held = Barrier.HoldingBarrier(Actor) || (Training.Reversal.IsForcing(Actor) && Barrier.HasMeter(Actor));
            if (held && Down == 1) held = Barrier.DownHeld(Actor);
            if (held && Down == 2) held = !Barrier.DownHeld(Actor);
            return Not ? !held : held;
        }
    }
}
