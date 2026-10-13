using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Lets a fighter guard without holding back. Read by StanceManager.GetBlockState ([FF PATCH: barrier, training guard]).
    /// - A barrier that's already up (A+B still held after letting go of back) guards in the fighter's current stance.
    /// - The training dummy's Guard setting guards the right way for each hit (stand for overheads, crouch for lows),
    ///   or on purpose the wrong way after the first hit (Switch modes).
    /// </summary>
    public static class GuardOverride
    {
        public const StanceSelect None = (StanceSelect)(-1);

        public static StanceSelect GetGuardStance(SakugaActor a, HitType hit)
        {
            if (a == null || a.Body == null) return None;

            if (Barrier.GuardsWithoutBack(a))
            {
                if (!a.Body.IsOnGround) return StanceSelect.AIR;
                return a.IsCrouchState() ? StanceSelect.CROUCH : StanceSelect.GROUND;
            }

            // Training dummy Guard setting (right way, or the Switch modes for fuzzy tests).
            return Training.TrainingController.DummyGuardStance(a, hit);
        }
    }
}
