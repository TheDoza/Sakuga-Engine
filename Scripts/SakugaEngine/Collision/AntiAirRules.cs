using SakugaEngine.Global;
using SakugaEngine.Resources;

namespace SakugaEngine.Collision
{
    /// <summary>
    /// Universal anti-air rule. A target whose current state has AntiAirImmunity enabled
    /// ignores hits from airborne attackers while it is inside the global anti-air window.
    /// Everything read here (state, state frame, position) is already part of the saved
    /// game state, so the rule is deterministic and rollback-safe without extra save data.
    /// </summary>
    public static class AntiAirRules
    {
        public static bool IgnoresHit(SakugaActor attacker, SakugaActor target, HitboxElement attackBox)
        {
            if (attacker == null || target == null || attackBox == null) return false;

            // Only physical attacks count. Projectiles and throws are handled by their own immunity flags.
            if (attackBox.HitboxType != HitboxType.HITBOX) return false;

            // The attacker must be in the air (jump-ins, air dashes, air normals and air specials).
            if (attacker.Body == null || attacker.Body.IsOnGround) return false;

            if (target.StateManager == null || target.Data == null) return false;
            FighterState state = target.StateManager.GetCurrentState();
            if (state == null || !state.AntiAirImmunity) return false;

            // CurrentStateFrame is 0 on the first frame of the state, so add 1 to match frame data notation.
            int frame = target.StateManager.CurrentStateFrame + 1;
            return frame >= GlobalVariables.AntiAirImmunityStartFrame &&
                   frame <= GlobalVariables.AntiAirImmunityEndFrame;
        }
    }
}
