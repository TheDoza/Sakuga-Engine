using Godot;
using SakugaEngine;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class DataContainer : Resource
    {
        [Export] public FighterProfile Profile;
        [Export] public FighterState[] States;
        [Export] public FighterStance[] Stances;

        [Export] public int MaxHealth = 10000;
        [Export] public int MaxSuperGauge = 10000;
        [Export] public ushort BaseAttack = 100;
        [Export] public ushort BaseDefense = 100;
        /// <summary>[FF PATCH: locomotion] Run, dash, jump and air action numbers (empty = defaults).</summary>
        [Export] public FunctionFighter.Mechanics.LocomotionSettings Locomotion;
        /// <summary>[FF PATCH: training reversal] This fighter's reversal actions in Training (empty = universal ones only).</summary>
        [Export] public FunctionFighter.Training.ReversalList TrainingReversals;
        /// <summary>[FF PATCH: pushbox] Pushbox used when a state's hitbox settings have none (or the state has no hitbox
        /// settings at all), so a forgotten pushbox never lets fighters walk through each other. Per-hitbox pushboxes
        /// (HitboxSettings.Pushbox) always win. For a state that should pass through on purpose, give its hitbox settings
        /// a pushbox with Size (0, 0) (Shared/HitboxPresets/Pushbox_None.tres). Empty = no fallback (old behaviour).</summary>
        [Export] public BoxShape DefaultPushbox;
        /// <summary>[FF PATCH: state flash] Draw StateFlash's outline pass on this fighter (counter / armor / invincible
        /// flashes). Turn it off for models that carry their own inverted-hull outline (Basic Bro and the rest of the
        /// Unity fighter pack): the outline pass culls front faces, which on an inverted hull draws the hull's near side,
        /// so the whole body turns the flash color. The tint still shows either way.</summary>
        [Export] public bool StateFlashOutline = true;

        [Export] public AIData _aiData;

        [Export] public FighterIntro[] Intros;
        [Export] public FighterOutro[] Outros;
        [Export] public FighterIntro[] VictoryPoses;
        [Export] public int DefeatPose;
    }
}
