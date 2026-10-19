using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// [FF PATCH: landing] Special landing recovery (the "+XL" in frame data, e.g. Jin's air Hishouken 49+8L).
    /// Put it in an air move's events, usually OnTick with an AtFrame condition for the frame the move starts applying it
    /// (e.g. Jin's j.236D applies it from frame 4). When the fighter lands, they can't act or guard for Frames frames,
    /// instead of the standard landing recovery. It still applies if the move ended or was cancelled before landing;
    /// getting hit clears it, and another move that sets it replaces it. Frames = 0 clears it (a move with its own
    /// landing state can use that).
    /// </summary>
    [GlobalClass]
    public partial class SetLandingRecoveryAction : FrameDataAction
    {
        [Export] public int Frames = 8;

        public override void Execute(ref SakugaActor Actor) => Locomotion.SetSpecialLanding(Actor, Frames);
    }
}
