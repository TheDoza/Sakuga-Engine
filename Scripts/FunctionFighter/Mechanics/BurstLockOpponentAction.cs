using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>Put in the Burst state's OnHitConfirm: a fighter hit by a Burst can't Burst back until they recover.</summary>
    [GlobalClass]
    public partial class BurstLockOpponentAction : FrameDataAction
    {
        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor == null) return;
            var opponents = Actor.GetOpponents();
            if (opponents == null) return;
            foreach (var o in opponents)
                if (o != null && o.StateManager != null && o.StateManager.CurrentStateType() == SakugaEngine.Global.StateType.HIT_REACTION)
                    o.Burst.Locked = true;
        }
    }
}
