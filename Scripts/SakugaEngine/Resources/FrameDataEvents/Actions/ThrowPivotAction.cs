using Godot;
using SakugaEngine.Game;
using SakugaEngine.Global;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class ThrowPivotAction : FrameDataAction
    {
        [Export] private Vector2I PivotPosition = Vector2I.Zero;
        [Export] private int ThrowReaction = -1;
        [Export] private bool EndThrow;

        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor.CurrentGrabbedNodeID < 0) return;

            SakugaActor grabbedActor = GameManager.Instance.GetActor(Actor.CurrentGrabbedNodeID) as SakugaActor;
             GD.Print(grabbedActor.Name);
            if (grabbedActor == null) return;
            if (grabbedActor.Body == null) return;
            if (grabbedActor.HitstunType != HitstunType.GRABBED) return;

            int grabState = ThrowReaction >= 0 ? grabbedActor.StanceManager.GetCurrentStance().HitReactions[ThrowReaction] : -1;
            grabbedActor.StateManager.PlayState(grabState, false);
            grabbedActor.Body.SetVelocity(Vector2I.Zero);
            Vector2I target = Actor.Body.FixedPosition + PivotPosition * new Vector2I(Actor.Body.PlayerSide, 1);
            // [FF PATCH: throw corner] If the victim would end up past the wall (a back throw with your back to the
            // corner, or any throw toward a wall), put the victim at the wall and move the thrower out by the same
            // amount, so the throw keeps its spacing and a back throw in the corner switches sides.
            int overflow = target.X - Mathf.Clamp(target.X, -GlobalVariables.WallLimit, GlobalVariables.WallLimit);
            if (overflow != 0)
            {
                Actor.Body.MoveTo(Actor.Body.FixedPosition - new Vector2I(overflow, 0));
                target.X -= overflow;
            }
            grabbedActor.Body.MoveTo(target);

            if (EndThrow) Actor.CurrentGrabbedNodeID = -1;
        }
    }
}
