using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class FighterStance : Resource
    {
        [ExportCategory("Settings")]
        [Export] public bool IsDamagePersistent;
        [Export] public bool IsRoundPersistent;
        [Export] public int DefaultState = 0;
        [Export] public MoveSettings[] Moves;
        [Export] public int[] HitReactions;
        [Export] public BlockSettings[] BlockReactions;
        [ExportCategory("Throw Escape")]
        [Export] public MotionInputs ThrowEscapeInput;
        [Export] public int GroundThrowEscapeState = -1;
        [Export] public int AirThrowEscapeState = -1;
        // [FF PATCH: throw reject] State the THROWER plays when its throw is rejected (BBCF style).
        // When either is set (>= 0), the defender plays the escape state above and stays put,
        // and all reject movement comes from these states' data instead of the engine's hardcoded push.
        // Leave both at -1 to keep upstream Sakuga behaviour (both players play the escape state and get pushed).
        [Export] public int GroundThrowRejectedState = -1;
        [Export] public int AirThrowRejectedState = -1;
    }
}
