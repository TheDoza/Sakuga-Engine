using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>True when the fighter can Burst: full gauge, getting hit, not thrown / hit by a Burst / frozen by a super flash.</summary>
    [GlobalClass]
    public partial class BurstReadyCondition : FrameDataCondition
    {
        [Export] private bool Not;
        public override bool Check(ref SakugaActor Actor)
        {
            bool ready = Burst.CanBurst(Actor);
            return Not ? !ready : ready;
        }
    }
}
