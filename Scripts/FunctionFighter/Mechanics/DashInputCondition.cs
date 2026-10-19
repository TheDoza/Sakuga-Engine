using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Move condition: 66 (run / step dash) or, with Back ticked, 44 (backdash). Not during a run skid unless the
    /// fighter's LocomotionSettings.SkidDashCancel is on. With Air ticked it's the air dash 66 / 44 instead, using the
    /// fighter's AirDashInputWindow.</summary>
    [GlobalClass]
    public partial class DashInputCondition : FrameDataCondition
    {
        [Export] public bool Back;
        [Export] public bool Air;
        public override bool Check(ref SakugaActor Actor)
        {
            if (Air) return Training.Reversal.IsForcing(Actor) || Locomotion.AirDashInput(Actor, Back);
            if (Actor.Locomotion.Mode == LocoMode.Skid && !Locomotion.Settings(Actor).SkidDashCancel) return false;
            return Training.Reversal.IsForcing(Actor) || Locomotion.DashInput(Actor, Back); // training reversal: the input is implied
        }
    }
}
