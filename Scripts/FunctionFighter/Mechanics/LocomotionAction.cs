using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Put in a universal movement state's OnEnter: tells Locomotion which movement this state is.
    /// Run 60, Run_Skid 61, Step_Dash 62, Backdash 63, jumps 3/4/5, super jumps 64-66, double jumps 67-69, air dashes 7/8.
    /// </summary>
    [GlobalClass]
    public partial class LocomotionAction : FrameDataAction
    {
        [Export] public LocoMode Mode;
        /// <summary>-1 back, 0 neutral/up, 1 forward.</summary>
        [Export(PropertyHint.Range, "-1,1")] public int Direction;

        public override void Execute(ref SakugaActor Actor) => Locomotion.Enter(Actor, Mode, Direction);
    }
}
