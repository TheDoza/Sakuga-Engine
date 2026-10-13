using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Move condition: 66 (run / step dash) or, with Back ticked, 44 (backdash).</summary>
    [GlobalClass]
    public partial class DashInputCondition : FrameDataCondition
    {
        [Export] public bool Back;
        public override bool Check(ref SakugaActor Actor) =>
            Training.Reversal.IsForcing(Actor) || Locomotion.DashInput(Actor, Back); // training reversal: the input is implied
    }
}
