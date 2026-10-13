using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Move condition: down then up inside the super jump window (2 then 7/8/9).</summary>
    [GlobalClass]
    public partial class SuperJumpInputCondition : FrameDataCondition
    {
        public override bool Check(ref SakugaActor Actor) =>
            Training.Reversal.IsForcing(Actor) || Locomotion.SuperJumpInput(Actor); // training reversal: the input is implied
    }
}
