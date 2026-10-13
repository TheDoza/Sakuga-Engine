using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Move condition for the wakeup options (Neutral Tech, Forward Roll, Back Roll): lying after a soft knockdown,
    /// inside the wakeup window (after the no-action frames, before the forced Neutral Tech), holding the right direction.
    /// Direction: 0 = no direction (Neutral Tech), 1 = forward (Forward Roll), -1 = back (Back Roll).
    /// </summary>
    [GlobalClass]
    public partial class GroundTechCondition : FrameDataCondition
    {
        [Export] public int Direction;

        public override bool Check(ref SakugaActor Actor)
        {
            if (!Locomotion.InWakeupWindow(Actor)) return false;
            if (Training.Reversal.IsForcing(Actor)) return true; // the training dummy picked this option itself
            return Locomotion.HeldDirection(Actor) == Direction;
        }
    }
}
