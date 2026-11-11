using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Hitspark for a throw landing (put on the throw's hit state, on the frame the throw deals its damage). Uses the same
    /// advantage hitspark as normal hits (Advantage.OnContact), or the old hit effect when those sparks are turned off.
    /// </summary>
    [GlobalClass]
    public partial class ThrowSparkAction : FrameDataAction
    {
        /// <summary>Where on the thrown fighter the spark appears, from their feet.</summary>
        [Export] public Vector2I Offset = new Vector2I(0, 9000);
        /// <summary>VFX list index used when the advantage hitsparks are off (0 = Hit).</summary>
        [Export] public int FallbackVFXIndex = 0;

        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor == null || Actor.Body == null) return;
            SakugaActor target = Actor.GetOpponent(0);
            if (target == null || target.Body == null) return;
            Vector2I contact = target.Body.FixedPosition + Offset;
            if (Advantage.SparksOn) Advantage.OnContact(Actor, target, contact, false);
            else if (Actor.Pool != null && FallbackVFXIndex >= 0) Actor.Pool.SpawnVFX(FallbackVFXIndex, contact, Actor.Body.PlayerSide, false);
        }
    }
}
