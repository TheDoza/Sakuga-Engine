using Godot;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class HitboxState : Resource
    {
        [Export] public int AtFrame;
        [Export] public Global.AnimationStage animationStage;
        [Export] public HitboxSettings HitboxData;
        /// <summary>[FF PATCH: counter] Counter Hit Recovery: getting hit during these frames counts as a counter hit even
        /// though they're recovery (tick it on a move's RECOVERY entry for big, risky moves). Shows "Counter" (red) and
        /// still shows "Punish".</summary>
        [Export] public bool CounterHitInRecovery;
    }
}
