using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Put in the Burst state's OnEnter: empties the Burst gauge and plays the Burst VFX.</summary>
    [GlobalClass]
    public partial class BurstSpendAction : FrameDataAction
    {
        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor == null) return;
            Burst.Spend(Actor);
        }
    }
}
