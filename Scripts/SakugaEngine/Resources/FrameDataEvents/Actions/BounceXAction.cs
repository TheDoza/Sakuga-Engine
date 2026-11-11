using Godot;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class BounceXAction : FrameDataAction
    {
        [Export] private bool CustomIntensity;
        [Export] private int Intensity;
        /// <summary>[FF PATCH: corner bounce] If the opponent is this close (world units) when the fighter bounces off the
        /// wall, it's a corner bounce: the fighter comes back off the wall at most CornerMaxSpeed, so it drops in front of
        /// the attacker instead of flying over them. 0 = off.</summary>
        [Export] private int CornerRange = 18000;
        [Export] private int CornerMaxSpeed = 6000;

        public override void Execute(ref SakugaActor Actor)
        {
            if (CustomIntensity) Actor.BounceXIntensity = Intensity;

            Actor.Body.BounceX(Actor.BounceXIntensity);

            // [FF PATCH: corner bounce]
            SakugaActor opponent = Actor.GetOpponent(0);
            if (CornerRange > 0 && opponent != null && opponent.Body != null &&
                System.Math.Abs(opponent.Body.FixedPosition.X - Actor.Body.FixedPosition.X) <= CornerRange)
                Actor.Body.FixedVelocity.X = System.Math.Clamp(Actor.Body.FixedVelocity.X, -CornerMaxSpeed, CornerMaxSpeed);
            Actor.BounceXIntensity = 0;
            Actor.Bounce.Stop();
        }
    }
}
