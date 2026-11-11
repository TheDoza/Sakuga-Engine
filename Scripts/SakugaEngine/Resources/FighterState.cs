using Godot;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class FighterState : Resource
    {
        [Export] public string StateName;
        [Export] public Global.MasterStance BaseStance;
        [Export] public Global.StateType Type;
        [Export] public AnimationData AnimationData;
        [Export] public FrameDataEvent[] TransitionEvents;
        [Export] public FrameDataEvent[] OnEnterEvents;
        [Export] public FrameDataEvent[] OnTickEvents;
        [Export] public FrameDataEvent[] OnExitEvents;
        [Export] public FrameDataEvent[] OnHitConfirmEvents;
        [Export] public FrameDataEvent[] OnHitReactionEvents;
        [ExportSubgroup("Anti-Air")]
        // Immune to hits from airborne attackers during the universal anti-air window
        // (GlobalVariables.AntiAirImmunityStartFrame to AntiAirImmunityEndFrame). Tick this on every 2C.
        [Export] public bool AntiAirImmunity;
        [ExportSubgroup("AI Flags")]
        [Export] public Global.AIFlags AIFlags;
    }
}