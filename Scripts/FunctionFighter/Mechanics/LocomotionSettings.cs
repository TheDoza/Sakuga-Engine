using Godot;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Per-fighter movement numbers (run, skid, step dash, backdash, jumps, air actions, momentum).
    /// Each fighter's DataContainer points at one (Locomotion field), e.g. Fighters/Dummy/Dummy_Locomotion.tres.
    /// Velocities use Sakuga's units: 60,000 = 1,000 units per frame. Integers only (rollback-safe).
    /// </summary>
    [GlobalClass]
    public partial class LocomotionSettings : Resource
    {
        [ExportGroup("Run (66, hold 6 or 3)")]
        /// <summary>Speed on the first frame of the run.</summary>
        [Export] public int RunStartSpeed = 30000;
        /// <summary>Speed gained every frame until RunMaxSpeed.</summary>
        [Export] public int RunAcceleration = 600;
        [Export] public int RunMaxSpeed = 42000;
        /// <summary>Frames between the two forward taps for a run / step dash / backdash.</summary>
        [Export] public int DashInputWindow = 10;

        [ExportGroup("Run Skid (run released or back held)")]
        /// <summary>Length of the skid. The fighter can't guard during it (Barrier cancels it: Barrier Brake).</summary>
        [Export] public int SkidFrames = 14;
        /// <summary>Allow 66 / 44 (run, step dash, backdash) to cancel the skid. Off: the skid has to finish first, so runs
        /// can't be chained by mashing 6. Attacks, throws, jumps and Barrier (Barrier Brake) still cancel it.</summary>
        [Export] public bool SkidDashCancel = false;

        [ExportGroup("Step Dash (alternative to Run)")]
        [Export] public int StepDashFrames = 18;
        [Export] public int StepDashDistance = 9000;
        /// <summary>Moves (jump, attacks, barrier) can only cancel the step dash from this frame on.</summary>
        [Export] public int StepDashCancelFrame = 10;

        [ExportGroup("Backdash (44)")]
        [Export] public int BackdashFrames = 22;
        [Export] public int BackdashDistance = 7500;
        /// <summary>Frames with the backdash moving (the rest is a vulnerable recovery that can't be cancelled).</summary>
        [Export] public int BackdashMoveFrames = 14;
        /// <summary>Fully invincible on frames 1..this.</summary>
        [Export] public int BackdashInvulnFrames = 7;
        /// <summary>Throw invincible on frames 1..this.</summary>
        [Export] public int BackdashThrowInvulnFrames = 12;

        [ExportGroup("Landing recovery")]
        /// <summary>Standard landing recovery (BBCF): for this many frames after landing, walks, jumps, super jumps and
        /// backdash are locked. Attacks, guarding, run and step dash work at once, and doing one unlocks everything.
        /// Special landing recovery (a move's +XL) is set per move with SetLandingRecoveryAction.</summary>
        [Export] public int LandingRecoveryFrames = 5;
        /// <summary>BBCF: the frame before a jumping fighter touches down counts as grounded. A hit on that frame gives a
        /// standing ground hit reaction (and puts the fighter on the floor), and ground throws can grab them.</summary>
        [Export] public bool PreLandingFrameGrounded = true;

        [ExportGroup("KO")]
        /// <summary>The finishing hit launches the fighter: speed away from the attacker, up, and the fall's gravity.</summary>
        [Export] public int KOLaunchX = 25000;
        [Export] public int KOLaunchY = 60000;
        [Export] public int KOLaunchGravity = 230000;
        /// <summary>Frames the launch speed is held before gravity takes over.</summary>
        [Export] public int KOLaunchTime = 8;

        [ExportGroup("Movement effects")]
        /// <summary>Dust / air puffs on jumps, landings, run, skid, step dash, air dash and double jump (MovementVFX).</summary>
        [Export] public bool MovementEffects = true;
        /// <summary>While running, a dust puff every this many frames (0 = only when the run starts).</summary>
        [Export] public int RunDustInterval = 12;

        [ExportGroup("Jump")]
        /// <summary>Prejump: on the ground, can't guard, throw and Burst invincible. Also used by super jumps.</summary>
        [Export] public int PrejumpFrames = 4;
        [Export] public int JumpVelocityY = 80000;
        [Export] public int JumpForwardX = 18000;
        [Export] public int JumpBackX = 12000;

        [ExportGroup("Super Jump (2 then 7/8/9)")]
        [Export] public int SuperJumpVelocityY = 100000;
        [Export] public int SuperJumpForwardX = 20000;
        [Export] public int SuperJumpBackX = 14000;
        /// <summary>Frames the down input stays valid for a super jump.</summary>
        [Export] public int SuperJumpInputWindow = 10;

        [ExportGroup("Double Jump")]
        [Export] public int DoubleJumpVelocityY = 68000;
        [Export] public int DoubleJumpForwardX = 16000;
        [Export] public int DoubleJumpBackX = 14000;
        /// <summary>Frames after a jump leaves the ground before a double jump or air dash is allowed.</summary>
        [Export] public int AirActionDelay = 4;

        [ExportGroup("Air actions")]
        /// <summary>Shared pool: double jumps and air dashes both use it (BBCF). 1 = one double jump OR one air dash.</summary>
        [Export] public int AirActions = 1;
        /// <summary>Most double jumps per jump (-1 = only limited by the pool).</summary>
        [Export] public int MaxAirJumps = -1;
        /// <summary>Most air dashes per jump (-1 = only limited by the pool). E.g. pool 2, jumps 1, dashes 1 = air dash > double jump.</summary>
        [Export] public int MaxAirDashes = -1;
        /// <summary>Air dash 66 / 44: most frames the first tap, the gap between taps and the second tap may each last.
        /// Bigger = easier air dashes.</summary>
        [Export] public int AirDashInputWindow = 6;
        /// <summary>Lowest height (world units, SimulationScale 10000 = 1m, measured from the ground) an air dash can start from. 0 = no minimum.</summary>
        [Export] public int AirDashMinHeight = 10000;

        [ExportGroup("Running momentum (run, skid and step dash)")]
        /// <summary>Percent of the run speed carried into a jump (dash jump).</summary>
        [Export] public int JumpMomentumPercent = 50;
        /// <summary>Back jump (7) out of a run / skid / step dash: percent of that carried momentum it keeps. 0 = a normal
        /// back jump (the run's forward speed is dropped); 100 = full forward carry, which can cancel the back jump out.</summary>
        [Export] public int BackJumpMomentumPercent = 0;
        /// <summary>Super jump (2 then 7/8/9): percent of the running momentum carried into the jump. Separate from JumpMomentumPercent so
        /// normal jumps are unaffected when tuning super jumps.</summary>
        [Export] public int SuperJumpMomentumPercent = 25;
        /// <summary>Double jump after a dash jump, in the same direction it's travelling: percent of the momentum kept.</summary>
        [Export] public int DoubleJumpSameDirectionPercent = 100;
        /// <summary>...straight up (8).</summary>
        [Export] public int DoubleJumpNeutralPercent = 60;
        /// <summary>...against the momentum (e.g. back): it still goes back, just less.</summary>
        [Export] public int DoubleJumpReversePercent = 25;
        /// <summary>Attacks, throws and Barrier out of a run / skid / step dash keep sliding: percent of the current speed,
        /// fading to 0 over SlideFrames (Barrier Brake). Getting hit or blocking ends the slide.</summary>
        [Export] public int SlidePercent = 100;
        [Export] public int SlideFrames = 20;

        [ExportGroup("Knockdown & wakeup (BBCF timeline, frame 1 = landing)")]
        /// <summary>Emergency Tech: A/B/C on frames 1..this after landing = Neutral Tech right away.</summary>
        [Export] public int EmergencyTechWindow = 8;
        /// <summary>...and a press this many frames BEFORE landing counts too (so you don't have to guess the landing).</summary>
        [Export] public int EmergencyTechBuffer = 6;
        /// <summary>No actions up to this frame (9..23 in BBCF); Neutral Tech / rolls from the next frame.</summary>
        [Export] public int WakeupLockUntil = 23;
        /// <summary>Lying this long = automatic Neutral Tech.</summary>
        [Export] public int ForcedTechFrame = 64;
        /// <summary>Hard knockdowns (throws) can't tech; they get up (Hit_GetUp) after this many frames.</summary>
        [Export] public int HardKnockdownFrames = 30;

        [ExportGroup("Neutral Tech (A/B/C, no direction)")]
        [Export] public int NeutralTechFrames = 32;
        [Export] public int NeutralTechInvulnFrames = 32;

        [ExportGroup("Wakeup rolls (6 or 4 + A/B/C)")]
        [Export] public int RollFrames = 30;
        /// <summary>Frames the roll moves (it stops for the rest).</summary>
        [Export] public int RollMoveFrames = 20;
        [Export] public int ForwardRollDistance = 22000;
        [Export] public int BackRollDistance = 20000;
        /// <summary>Fully invincible on frames 1..this.</summary>
        [Export] public int ForwardRollInvulnFrames = 3;
        [Export] public int BackRollInvulnFrames = 10;
        /// <summary>Throw invincible on frames 1..this.</summary>
        [Export] public int RollThrowInvulnFrames = 17;
        /// <summary>From this frame the roll can guard (and a hit is a crouching hit). Before it, a hit is an air hit.</summary>
        [Export] public int RollGuardFrom = 18;
        /// <summary>Passes through the opponent on frames 1..this.</summary>
        [Export] public int RollPassThroughFrames = 10;

        [ExportGroup("Air Recovery (4 / 5 / 6 + A/B/C)")]
        [Export] public int AirTechFrames = 15;
        [Export] public int AirTechInvulnFrames = 15;
        [Export] public int AirTechForwardX = 15000;
        [Export] public int AirTechBackX = 15000;

        private static LocomotionSettings _default;
        public static LocomotionSettings Default => _default ??= new LocomotionSettings();
    }
}
