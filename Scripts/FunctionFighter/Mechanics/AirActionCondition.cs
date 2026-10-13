using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Move condition: an air action is left (double jump when Jump is ticked, air dash otherwise).</summary>
    [GlobalClass]
    public partial class AirActionCondition : FrameDataCondition
    {
        [Export] public bool Jump;
        public override bool Check(ref SakugaActor Actor) => Locomotion.CanAirAction(Actor, Jump);
    }
}
