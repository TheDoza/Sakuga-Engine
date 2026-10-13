using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Shows the barrier shield (BarrierSettings.ShieldScene) in front of a fighter while they're barriering.
    /// Visual only: it reads the barrier state every rendered frame and never changes the simulation,
    /// so it's rollback-safe. Called from SakugaActor.Render ([FF PATCH: barrier]).
    /// </summary>
    public static class BarrierVisual
    {
        const string NodeName = "BarrierShield";
        /// <summary>How far the shield is turned towards the camera-side (degrees). 0 = edge-on, 90 = flat to the camera.</summary>
        const float TurnDegrees = 35f;

        public static void Update(SakugaActor a)
        {
            if (a == null || a.Body == null || a.Inputs == null) return;
            var s = BarrierSettings.Get();
            var shield = a.GetNodeOrNull<Node3D>(NodeName);
            if (shield == null)
            {
                if (s.ShieldScene == null) return;
                shield = s.ShieldScene.Instantiate<Node3D>();
                shield.Name = NodeName;
                shield.Visible = false;
                a.AddChild(shield);
            }

            bool show = a.IsActive && a.Barrier.Active;
            shield.Visible = show;
            if (!show) return;

            int side = a.Body.PlayerSide;
            var pos = a.Body.FixedPosition + new Vector2I(s.ShieldOffset.X * side, s.ShieldOffset.Y);
            shield.GlobalPosition = GlobalFunctions.ToScaledVector3(pos);
            shield.GlobalRotation = new Vector3(0, Mathf.DegToRad(side > 0 ? TurnDegrees - 90f : 90f - TurnDegrees), 0);
        }
    }
}
