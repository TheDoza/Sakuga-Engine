using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Put in the OnTick events of the neutral barrier states (15 Barrier_Stand, 16 Barrier_Crouch, 17 Barrier_Air).
    /// Tells the barrier meter that this fighter is barriering right now, so it drains like barrier-blocking does.
    /// (Those states can't be BLOCKING-type: Sakuga kicks a fighter out of BLOCKING states when no attack is near.)
    /// </summary>
    [GlobalClass]
    public partial class BarrierStanceAction : FrameDataAction
    {
        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor == null) return;
            Actor.Barrier.InStance = true;
        }
    }
}
