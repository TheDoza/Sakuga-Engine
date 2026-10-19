using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// What happens to a knocked-out fighter. The finishing hit always launches (even a standing hit like 5B): the fighter
    /// pops up in Hit_Air with a hard knockdown, lands in Hit_Down and stays down until the next round. A KO'd fighter
    /// ignores its inputs (no walking, attacks, techs, rolls, Burst), so a held stick does nothing.
    /// Launch strength is per fighter: LocomotionSettings → KO. Rollback-safe (only reads / sets saved fighter state).
    /// </summary>
    public static class KO
    {
        /// <summary>Universal Hit_Air (UniversalLayout): the airborne hit reaction that falls into Hit_Down.</summary>
        public const int HitAir = 24;

        public static bool IsKO(SakugaActor a) => a != null && a.IsKO();

        /// <summary>KO'd and lying still in Hit_Down (the round can end).</summary>
        public static bool IsDown(SakugaActor a)
        {
            if (!IsKO(a) || a.Body == null || !a.Body.IsOnGround || a.StateManager.CurrentState != Locomotion.HitDown) return false;
            var anim = a.StateManager.GetCurrentState().AnimationData;
            return anim == null || a.StateManager.CurrentStateFrame >= System.Math.Max(0, anim.Duration - 2);
        }

        /// <summary>Keeps a KO'd fighter lying in Hit_Down: its frame never reaches the state's end.</summary>
        public static void HoldDown(SakugaActor a)
        {
            var anim = a.StateManager.GetCurrentState().AnimationData;
            if (anim == null) return;
            int hold = System.Math.Max(0, anim.Duration - 2);
            if (a.StateManager.CurrentStateFrame > hold) a.StateManager.CurrentStateFrame = hold;
        }

        /// <summary>From SakugaActor.HitDamage, right after the damage ([FF PATCH: ko]). Launches the fighter if this hit KO'd it.</summary>
        public static void OnHit(SakugaActor defender, SakugaActor attacker)
        {
            if (!IsKO(defender) || defender.Body == null || defender.StateManager == null) return;
            var s = Locomotion.Settings(defender);
            defender.HitstunType = HitstunType.HARD_KNOCKDOWN;
            if (defender.StateManager.CurrentState != HitAir) defender.StateManager.PlayState(HitAir, true);
            int away = attacker?.Body != null ? attacker.Body.PlayerSide : -defender.Body.PlayerSide;
            if (defender.Body.IsOnGround) defender.Body.FixedPosition.Y = 1; // leave the floor so the launch isn't a landing
            defender.PushActor(s.KOLaunchTime, s.KOLaunchX * away, System.Math.Max(s.KOLaunchY, defender.Body.FixedVelocity.Y), s.KOLaunchGravity);
            defender.Body.UpdateColliders();
        }
    }
}
