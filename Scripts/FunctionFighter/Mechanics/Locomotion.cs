using Godot;
using MessagePack;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    public enum LocoMode : byte { None, Run, Skid, StepDash, Backdash, Jump, SuperJump, DoubleJump, AirDash, NeutralTech, ForwardRoll, BackRoll, AirTech }

    /// <summary>Per-fighter movement state. Saved with the fighter for rollback (SakugaActorState key 19).</summary>
    [MessagePackObject]
    public struct LocomotionState
    {
        [Key(0)] public LocoMode Mode;
        /// <summary>The universal state that started Mode (when the fighter leaves it, Mode ends).</summary>
        [Key(1)] public int State;
        [Key(2)] public int Frame;
        /// <summary>-1 back, 0 neutral, 1 forward (relative to facing when the move started).</summary>
        [Key(3)] public int Dir;
        [Key(4)] public int PrejumpLeft;
        [Key(5)] public bool Launched;
        /// <summary>This airborne sequence started from a run / step dash (dash jump).</summary>
        [Key(6)] public bool DashJump;
        /// <summary>Horizontal momentum carried into a jump (world velocity).</summary>
        [Key(7)] public int Momentum;
        /// <summary>Current run / skid speed.</summary>
        [Key(8)] public int Speed;
        [Key(9)] public int AirActionsUsed;
        [Key(10)] public int AirJumpsUsed;
        [Key(11)] public int AirDashesUsed;
        /// <summary>Frames since leaving the ground.</summary>
        [Key(12)] public int AirFrames;
        /// <summary>Run momentum slide into an attack / Barrier: frames left and world velocity.</summary>
        [Key(13)] public int SlideLeft;
        [Key(14)] public int SlideVelocity;
        /// <summary>FrameProperties bits this system set (so only those get cleared).</summary>
        [Key(15)] public byte PropsSet;
        /// <summary>Latest run / step dash velocity (world), used for momentum when the run is cancelled.</summary>
        [Key(16)] public int DashVelocity;
        /// <summary>Super jumps and double jumps turn to face the opponent until landing (normal jumps don't).</summary>
        [Key(17)] public bool AutoFaceInAir;
    }

    /// <summary>
    /// Run, run skid, step dash, backdash, jumps (with prejump), super jumps, double jumps, air action limits and
    /// running momentum. The universal states call LocomotionAction on enter; everything else runs from
    /// SakugaActor.Tick ([FF PATCH: locomotion]). Numbers live in each fighter's LocomotionSettings.
    /// </summary>
    public static class Locomotion
    {
        // Universal state numbers
        public const int Idle = 0, JumpNeutral = 3, JumpForward = 4, JumpBack = 5, Falling = 6, AirDashForward = 7, AirDashBack = 8;
        public const int Run = 60, RunSkid = 61, StepDash = 62, Backdash = 63;
        public const int SuperJumpNeutral = 64, SuperJumpForward = 65, SuperJumpBack = 66;
        public const int DoubleJumpNeutral = 67, DoubleJumpForward = 68, DoubleJumpBack = 69;
        static readonly int[] BarrierStates = { 15, 16, 17 };
        // Tech & wakeup (universal states)
        public const int AirRecovery = 13, NeutralTechState = 14, HitDown = 26, HitGetUp = 27, RollForward = 70, RollBack = 71;
        /// <summary>Throw connected / rejected: the throw sets the positions, so a momentum slide stops here.</summary>
        static readonly int[] SlideStopStates = { 31, 33, 35, 36, 55, 56, 57, 58 };

        const FrameProperties Managed = FrameProperties.DAMAGE_IMUNITY | FrameProperties.THROW_IMUNITY |
                                        FrameProperties.BURST_IMUNITY | FrameProperties.NO_GUARD | FrameProperties.LOCK_MOVE;

        public static LocomotionSettings Settings(SakugaActor a) =>
            a?.Data?.Locomotion as LocomotionSettings ?? LocomotionSettings.Default;

        static bool IsFighter(SakugaActor a) => a != null && a.Body != null && a.StateManager != null && a.Inputs != null;

        public static void Reset(SakugaActor a) => a.Locomotion = new LocomotionState();

        /// <summary>Walk / crouch moves (priority 0) don't interrupt these.</summary>
        public static bool BlocksBasicMoves(SakugaActor a)
        {
            if (a == null) return false;
            var m = a.Locomotion.Mode;
            return (m == LocoMode.Run || m == LocoMode.Skid || m == LocoMode.StepDash || m == LocoMode.Backdash) &&
                   a.StateManager != null && a.StateManager.CurrentState == a.Locomotion.State;
        }

        /// <summary>A real move (jump, attack, barrier...) is waiting to come out.</summary>
        static bool MoveComing(SakugaActor a)
        {
            var sm = a.StanceManager;
            if (sm == null || sm.BufferedMove < 0) return false;
            var mv = sm.GetMove(sm.BufferedMove);
            return mv != null && mv.Priority > 0;
        }

        public static bool InPrejump(SakugaActor a) =>
            a != null && (a.Locomotion.Mode == LocoMode.Jump || a.Locomotion.Mode == LocoMode.SuperJump) && !a.Locomotion.Launched;

        // ------------------------------------------------------------ inputs

        public static PlayerInputs ForwardBit(SakugaActor a) => a.Body.PlayerSide > 0 ? PlayerInputs.RIGHT : PlayerInputs.LEFT;
        public static PlayerInputs BackBit(SakugaActor a) => a.Body.PlayerSide > 0 ? PlayerInputs.LEFT : PlayerInputs.RIGHT;

        static PlayerInputs Raw(SakugaActor a, int back)
        {
            int i = a.Inputs.CurrentHistory - back;
            while (i < 0) i += GlobalVariables.InputHistorySize;
            return a.Inputs.InputHistory[i].rawInput;
        }

        static int Duration(SakugaActor a, int back)
        {
            int i = a.Inputs.CurrentHistory - back;
            while (i < 0) i += GlobalVariables.InputHistorySize;
            return a.Inputs.InputHistory[i].duration;
        }

        static bool Has(PlayerInputs i, PlayerInputs bit) => (i & bit) != 0;

        /// <summary>
        /// 66 (run / step dash) or 44 (backdash): a fresh press of the direction, the input before it without that direction
        /// and without down, and the same direction (6 or 3) just before that, all inside the window.
        /// So 6356 runs but 6536 doesn't (BBCF), which keeps 236236 from running.
        /// </summary>
        public static bool DashInput(SakugaActor a, bool back)
        {
            if (!IsFighter(a)) return false;
            var s = Settings(a);
            PlayerInputs dir = back ? BackBit(a) : ForwardBit(a);
            PlayerInputs cur = Raw(a, 0);
            if (!Has(cur, dir) || Has(cur, PlayerInputs.DOWN) || Has(cur, PlayerInputs.UP)) return false;
            if (Duration(a, 0) > 2) return false;
            PlayerInputs mid = Raw(a, 1);
            if (Has(mid, dir) || Has(mid, PlayerInputs.DOWN)) return false;
            if (Duration(a, 1) > s.DashInputWindow) return false;
            PlayerInputs first = Raw(a, 2);
            return Has(first, dir) && !Has(first, PlayerInputs.UP);
        }

        /// <summary>Up just pressed with a down input inside the window before it (2 then 7/8/9).</summary>
        public static bool SuperJumpInput(SakugaActor a)
        {
            if (!IsFighter(a)) return false;
            if (!Has(Raw(a, 0), PlayerInputs.UP) || Duration(a, 0) > 3) return false;
            int window = Settings(a).SuperJumpInputWindow, gap = 0;
            for (int k = 1; k < 8; k++)
            {
                PlayerInputs r = Raw(a, k);
                if (Has(r, PlayerInputs.DOWN)) return true;
                gap += Duration(a, k);
                if (gap > window) return false;
            }
            return false;
        }

        /// <summary>A double jump or air dash is allowed right now (pool, caps, delay after leaving the ground).</summary>
        public static bool CanAirAction(SakugaActor a, bool jump)
        {
            if (!IsFighter(a) || a.Body.IsOnGround) return false;
            var s = Settings(a);
            ref LocomotionState l = ref a.Locomotion;
            if (l.AirFrames < s.AirActionDelay) return false;
            if (l.AirActionsUsed >= s.AirActions) return false;
            if (jump && s.MaxAirJumps >= 0 && l.AirJumpsUsed >= s.MaxAirJumps) return false;
            if (!jump && s.MaxAirDashes >= 0 && l.AirDashesUsed >= s.MaxAirDashes) return false;
            if (jump && !Training.Reversal.IsForcing(a))
            {
                // Up must be freshly pressed: holding up from the first jump doesn't double jump.
                if (!Has(Raw(a, 0), PlayerInputs.UP) || Has(Raw(a, 1), PlayerInputs.UP) || Duration(a, 0) > 2) return false;
            }
            return true;
        }

        // ------------------------------------------------------------ entering a movement state

        /// <summary>Called by LocomotionAction in a universal state's OnEnter.</summary>
        public static void Enter(SakugaActor a, LocoMode mode, int dir)
        {
            if (!IsFighter(a)) return;
            var s = Settings(a);
            ref LocomotionState l = ref a.Locomotion;
            // Run, step dash and skid all count as "dashing": their current speed carries into a jump.
            bool wasDashing = l.Mode == LocoMode.Run || l.Mode == LocoMode.StepDash || l.Mode == LocoMode.Skid;
            int carried = wasDashing ? l.DashVelocity : 0;
            int facing = a.Body.PlayerSide;

            switch (mode)
            {
                case LocoMode.Run:
                    l.Speed = s.RunStartSpeed;
                    l.DashVelocity = l.Speed * facing;
                    a.Body.SetLateralVelocity(l.DashVelocity);
                    break;
                case LocoMode.Skid:
                    l.Speed = System.Math.Abs(l.DashVelocity);
                    break;
                case LocoMode.Jump:
                case LocoMode.SuperJump:
                    l.PrejumpLeft = System.Math.Max(0, s.PrejumpFrames);
                    l.Launched = false;
                    l.DashJump = wasDashing;
                    l.Momentum = (int)((long)carried * s.JumpMomentumPercent / 100);
                    l.AirActionsUsed = l.AirJumpsUsed = l.AirDashesUsed = 0;
                    l.AutoFaceInAir = mode == LocoMode.SuperJump;
                    break;
                case LocoMode.DoubleJump:
                    l.AirActionsUsed++; l.AirJumpsUsed++;
                    l.AutoFaceInAir = true;
                    LaunchDoubleJump(a, s, dir);
                    break;
                case LocoMode.AirDash:
                    l.AirActionsUsed++; l.AirDashesUsed++;
                    break;
                case LocoMode.NeutralTech:
                    a.Body.SetLateralVelocity(0);
                    break;
                case LocoMode.ForwardRoll:
                case LocoMode.BackRoll:
                    // Remember the roll's direction in world space: rolling through the opponent flips the facing.
                    dir = (mode == LocoMode.ForwardRoll ? 1 : -1) * facing;
                    break;
                case LocoMode.AirTech:
                {
                    // Hold forward / back / nothing to recover that way (the training dummy passes its choice in).
                    int d = Training.Reversal.IsForcing(a) ? ForcedAirTechDir : HeldDirection(a);
                    int vx = d > 0 ? s.AirTechForwardX * facing : d < 0 ? -s.AirTechBackX * facing : 0;
                    a.Body.SetLateralVelocity(vx);
                    dir = d;
                    l.AutoFaceInAir = false;
                    break;
                }
            }

            l.Mode = mode;
            l.State = a.StateManager.CurrentState;
            l.Frame = 0;
            l.Dir = dir;
            if (mode == LocoMode.Jump || mode == LocoMode.SuperJump)
            {
                if (l.PrejumpLeft > 0) SetProps(a, FrameProperties.THROW_IMUNITY | FrameProperties.BURST_IMUNITY | FrameProperties.NO_GUARD);
                else Launch(a, s);
            }
        }

        static void LaunchDoubleJump(SakugaActor a, LocomotionSettings s, int dir)
        {
            ref LocomotionState l = ref a.Locomotion;
            int facing = a.Body.PlayerSide;
            int vx = dir > 0 ? s.DoubleJumpForwardX * facing : dir < 0 ? -s.DoubleJumpBackX * facing : 0;
            if (l.DashJump)
            {
                // Out of a dash jump the double jump keeps some of its momentum, whichever way it goes.
                int m = a.Body.FixedVelocity.X;
                int travel = System.Math.Sign(m);
                int inputWorld = dir * facing;
                int pct = dir == 0 ? s.DoubleJumpNeutralPercent :
                          System.Math.Sign(inputWorld) == travel ? s.DoubleJumpSameDirectionPercent : s.DoubleJumpReversePercent;
                vx += (int)((long)m * pct / 100);
            }
            a.Body.SetVelocity(new Vector2I(vx, s.DoubleJumpVelocityY));
            l.Launched = true;
        }

        static void Launch(SakugaActor a, LocomotionSettings s)
        {
            ref LocomotionState l = ref a.Locomotion;
            bool super = l.Mode == LocoMode.SuperJump;
            int facing = a.Body.PlayerSide;
            int fx = super ? s.SuperJumpForwardX : s.JumpForwardX;
            int bx = super ? s.SuperJumpBackX : s.JumpBackX;
            int vx = l.Dir > 0 ? fx * facing : l.Dir < 0 ? -bx * facing : 0;
            vx += l.Momentum;
            a.Body.SetVelocity(new Vector2I(vx, super ? s.SuperJumpVelocityY : s.JumpVelocityY));
            l.Launched = true;
            l.AirFrames = 0;
            SetProps(a, 0);
        }

        // ------------------------------------------------------------ every frame

        /// <summary>Once per frame after hitstop (SakugaActor.Tick).</summary>
        public static void Tick(SakugaActor a)
        {
            if (!IsFighter(a)) return;
            var s = Settings(a);
            ref LocomotionState l = ref a.Locomotion;
            int cur = a.StateManager.CurrentState;
            int facing = a.Body.PlayerSide;

            // Left the movement state (cancelled into something else, got hit, ...).
            if (l.Mode != LocoMode.None && cur != l.State) Leave(a, s, cur);
            a.Body.IgnorePushbox = false; // set again below while a wakeup roll passes through the opponent

            // Movement doesn't "hold" the move slot: once running / jumping / dashing, any real move may cancel it
            // (jump out of a run, air dash out of a super jump, attacks, Barrier). Walking is still blocked by
            // BlocksBasicMoves, backdash / early step dash by LOCK_MOVE, and the prejump keeps its hold.
            var sm = a.StanceManager;
            if (l.Mode != LocoMode.None && !InPrejump(a) && sm.CurrentMove >= 0 && sm.GetCurrentMove().MoveState == l.State)
                sm.CurrentMove = -1;

            if (a.Body.IsOnGround)
            {
                if (l.Mode != LocoMode.Jump && l.Mode != LocoMode.SuperJump)
                {
                    l.AirActionsUsed = l.AirJumpsUsed = l.AirDashesUsed = 0;
                    l.AirFrames = 0;
                    if (l.Mode != LocoMode.DoubleJump) { l.DashJump = false; l.AutoFaceInAir = false; }
                }
            }
            else
            {
                l.AirFrames++;
                if (l.AutoFaceInAir) AutoFace(a);
            }

            FrameProperties props = 0;
            switch (l.Mode)
            {
                case LocoMode.Run:
                {
                    PlayerInputs r = Raw(a, 0);
                    bool forward = Has(r, ForwardBit(a)) && !Has(r, PlayerInputs.UP) && !Has(r, BackBit(a));
                    // Let go of forward (or held back): skid, unless a move is about to cancel the run (e.g. Barrier Brake).
                    if (!forward) { if (!MoveComing(a)) a.StateManager.PlayState(RunSkid, true); break; }
                    l.Speed = System.Math.Min(s.RunMaxSpeed, l.Speed + s.RunAcceleration);
                    l.DashVelocity = l.Speed * facing;
                    a.Body.SetLateralVelocity(l.DashVelocity);
                    break;
                }
                case LocoMode.Skid:
                {
                    int frames = System.Math.Max(1, s.SkidFrames);
                    if (l.Frame >= frames) { l.DashVelocity = 0; a.Body.SetLateralVelocity(0); a.StateManager.PlayState(Idle, true); break; }
                    l.DashVelocity = l.Speed * (frames - l.Frame) / frames * facing;
                    a.Body.SetLateralVelocity(l.DashVelocity);
                    props |= FrameProperties.NO_GUARD;
                    break;
                }
                case LocoMode.StepDash:
                {
                    int frames = System.Math.Max(1, s.StepDashFrames);
                    if (l.Frame >= frames) { a.Body.SetLateralVelocity(0); a.StateManager.PlayState(Idle, true); break; }
                    int v0 = 120 * s.StepDashDistance / frames;
                    l.DashVelocity = v0 * (frames - l.Frame) / frames * facing;
                    a.Body.SetLateralVelocity(l.DashVelocity);
                    if (l.Frame < s.StepDashCancelFrame) props |= FrameProperties.LOCK_MOVE | FrameProperties.NO_GUARD;
                    break;
                }
                case LocoMode.Backdash:
                {
                    if (l.Frame >= s.BackdashFrames) { a.Body.SetLateralVelocity(0); a.StateManager.PlayState(Idle, true); break; }
                    int frames = System.Math.Max(1, s.BackdashMoveFrames);
                    int v0 = 120 * s.BackdashDistance / frames;
                    a.Body.SetLateralVelocity(l.Frame < frames ? -v0 * (frames - l.Frame) / frames * facing : 0);
                    props |= FrameProperties.LOCK_MOVE | FrameProperties.NO_GUARD;
                    if (l.Frame < s.BackdashInvulnFrames) props |= FrameProperties.DAMAGE_IMUNITY;
                    if (l.Frame < s.BackdashThrowInvulnFrames) props |= FrameProperties.THROW_IMUNITY;
                    break;
                }
                case LocoMode.Jump:
                case LocoMode.SuperJump:
                    if (!l.Launched)
                    {
                        // Prejump: a dash jump keeps sliding, otherwise stand still.
                        a.Body.SetVelocity(new Vector2I(l.Momentum, 0));
                        if (--l.PrejumpLeft <= 0) Launch(a, s);
                        else props |= FrameProperties.THROW_IMUNITY | FrameProperties.BURST_IMUNITY | FrameProperties.NO_GUARD;
                        break;
                    }
                    Gravity(a);
                    break;
                case LocoMode.DoubleJump:
                    Gravity(a);
                    break;
                case LocoMode.NeutralTech:
                {
                    if (l.Frame >= s.NeutralTechFrames) { a.StateManager.PlayState(Idle, true); break; }
                    props |= FrameProperties.LOCK_MOVE | FrameProperties.NO_GUARD;
                    if (l.Frame < s.NeutralTechInvulnFrames) props |= FrameProperties.DAMAGE_IMUNITY | FrameProperties.THROW_IMUNITY | FrameProperties.BURST_IMUNITY;
                    break;
                }
                case LocoMode.ForwardRoll:
                case LocoMode.BackRoll:
                {
                    bool fwd = l.Mode == LocoMode.ForwardRoll;
                    int frames = System.Math.Max(1, s.RollFrames);
                    if (l.Frame >= frames) { a.Body.SetLateralVelocity(0); a.StateManager.PlayState(Idle, true); break; }
                    int move = System.Math.Max(1, s.RollMoveFrames);
                    int dist = fwd ? s.ForwardRollDistance : s.BackRollDistance;
                    int v0 = 120 * dist / move;
                    a.Body.SetLateralVelocity(l.Frame < move ? v0 * (move - l.Frame) / move * System.Math.Sign(l.Dir) : 0);
                    int f1 = l.Frame + 1; // BBCF-style frame numbers start at 1
                    props |= FrameProperties.LOCK_MOVE;
                    if (f1 <= (fwd ? s.ForwardRollInvulnFrames : s.BackRollInvulnFrames)) props |= FrameProperties.DAMAGE_IMUNITY | FrameProperties.BURST_IMUNITY;
                    if (f1 <= s.RollThrowInvulnFrames) props |= FrameProperties.THROW_IMUNITY;
                    if (f1 < s.RollGuardFrom) props |= FrameProperties.NO_GUARD;
                    if (f1 <= s.RollPassThroughFrames) a.Body.IgnorePushbox = true;
                    break;
                }
                case LocoMode.AirTech:
                {
                    if (l.Frame >= s.AirTechFrames) { a.StateManager.PlayState(a.Body.IsOnGround ? Idle : Falling, true); break; }
                    props |= FrameProperties.LOCK_MOVE;
                    if (l.Frame < s.AirTechInvulnFrames) props |= FrameProperties.DAMAGE_IMUNITY | FrameProperties.THROW_IMUNITY | FrameProperties.BURST_IMUNITY;
                    break;
                }
            }

            // Lying down after a knockdown (BBCF timeline): Emergency Tech window, then nothing, then wakeup options,
            // then a forced Neutral Tech. Hard knockdowns (throws) can't tech: they get up after a fixed time.
            if (l.Mode == LocoMode.None && cur == HitDown) KnockdownTick(a, s);

            // Running momentum carried into an attack, throw or Barrier. Getting hit, blocking, or a throw connecting ends it.
            if (l.SlideLeft > 0 && StopsSlide(a, cur)) l.SlideLeft = 0;
            if (l.SlideLeft > 0 && s.SlideFrames > 0)
            {
                int step = l.SlideVelocity / GlobalVariables.TicksPerSecond * l.SlideLeft / s.SlideFrames;
                a.Body.FixedPosition.X = Mathf.Clamp(a.Body.FixedPosition.X + step, -GlobalVariables.WallLimit, GlobalVariables.WallLimit);
                l.SlideLeft--;
            }

            SetProps(a, props);
            l.Frame++;
        }

        // ------------------------------------------------------------ tech & wakeup

        /// <summary>Set by the training dummy before it air techs: -1 back, 0 neutral, 1 forward.</summary>
        public static int ForcedAirTechDir;

        /// <summary>1-based frame since landing in Hit_Down (frame 1 = the landing frame).</summary>
        public static int KnockdownFrame(SakugaActor a) => a.StateManager.CurrentStateFrame + 1;

        public static bool IsHardKnockdown(SakugaActor a) => a.HitstunType >= HitstunType.HARD_KNOCKDOWN;

        /// <summary>Lying in Hit_Down from a soft knockdown, inside the Emergency Tech window.</summary>
        public static bool InEmergencyWindow(SakugaActor a) =>
            IsFighter(a) && a.StateManager.CurrentState == HitDown && !IsHardKnockdown(a) && KnockdownFrame(a) <= Settings(a).EmergencyTechWindow;

        /// <summary>Lying in Hit_Down from a soft knockdown, inside the wakeup window (Neutral Tech / rolls allowed).</summary>
        public static bool InWakeupWindow(SakugaActor a)
        {
            if (!IsFighter(a) || a.StateManager.CurrentState != HitDown || IsHardKnockdown(a)) return false;
            var s = Settings(a);
            int f = KnockdownFrame(a);
            return f > s.WakeupLockUntil && f < s.ForcedTechFrame;
        }

        /// <summary>-1 back, 0 neutral, 1 forward (held right now, relative to facing).</summary>
        public static int HeldDirection(SakugaActor a)
        {
            PlayerInputs r = Raw(a, 0);
            bool f = Has(r, ForwardBit(a)), b = Has(r, BackBit(a));
            return f == b ? 0 : f ? 1 : -1;
        }

        /// <summary>A, B or C was pressed (not just held) within the last <paramref name="frames"/> frames.</summary>
        public static bool TechButtonPressedWithin(SakugaActor a, int frames)
        {
            const PlayerInputs buttons = PlayerInputs.FACE_A | PlayerInputs.FACE_B | PlayerInputs.FACE_C;
            int age = 0;
            for (int k = 0; k < GlobalVariables.InputHistorySize - 1; k++)
            {
                PlayerInputs now = Raw(a, k), before = Raw(a, k + 1);
                if (((now & ~before) & buttons) != 0) return true; // this entry started with a new button
                age += Duration(a, k);
                if (age > frames) return false;
            }
            return false;
        }

        public static void StartNeutralTech(SakugaActor a) => a.StateManager.PlayState(NeutralTechState, true);

        /// <summary>A, B or C is held right now.</summary>
        public static bool TechButtonHeld(SakugaActor a) =>
            (Raw(a, 0) & (PlayerInputs.FACE_A | PlayerInputs.FACE_B | PlayerInputs.FACE_C)) != 0;

        static readonly string[] AirTechMoves = { "Air Recovery" };
        static readonly string[] GroundTechMoves = { "Ground Recovery", "Forward Roll", "Back Roll" };

        /// <summary>
        /// Holding A/B/C (plus a direction) techs on the first frame it's allowed, without having to time a press
        /// (BBCF: "hold a button and a direction to recover as soon as possible"). Air Recovery in the air; Neutral Tech or
        /// a roll (by direction) once the wakeup window opens. Called from StanceManager.CheckMoves ([FF PATCH: tech]).
        /// </summary>
        public static bool TryHeldTech(SakugaActor a, StanceManager sm)
        {
            if (!IsFighter(a) || sm == null || a.StateManager.CurrentStateType() != StateType.HIT_REACTION) return false;
            if (!TechButtonHeld(a)) return false;
            string[] moves = !a.Body.IsOnGround ? AirTechMoves : a.StateManager.CurrentState == HitDown ? GroundTechMoves : null;
            if (moves == null) return false;
            foreach (var name in moves)
            {
                int i = Training.Reversal.FindMove(sm, name);
                if (i >= 0 && sm.CanExecuteNow(i)) { sm.ExecuteMove(i); return true; }
            }
            return false;
        }

        /// <summary>Last ground tech this fighter did (for the Training popup).</summary>
        public static string LastTechName(SakugaActor a) => a.Locomotion.Mode switch
        {
            LocoMode.ForwardRoll => "FORWARD ROLL",
            LocoMode.BackRoll => "BACK ROLL",
            LocoMode.AirTech => "AIR TECH",
            _ => "NEUTRAL TECH",
        };

        static void KnockdownTick(SakugaActor a, LocomotionSettings s)
        {
            int f = KnockdownFrame(a);
            if (IsHardKnockdown(a))
            {
                if (f >= s.HardKnockdownFrames) a.StateManager.PlayState(HitGetUp, true);
                return;
            }
            // Emergency Tech = Neutral Tech right on landing. A press in the last few frames before landing counts too.
            if (f <= s.EmergencyTechWindow && (TechButtonHeld(a) || TechButtonPressedWithin(a, f - 1 + s.EmergencyTechBuffer))) { StartNeutralTech(a); return; }
            if (f >= s.ForcedTechFrame) StartNeutralTech(a);
        }

        /// <summary>A hit right now counts as an air hit (early part of a wakeup roll). Read by SakugaActor.HitDamage.</summary>
        public static bool HitAsAirborne(SakugaActor a)
        {
            if (a == null) return false;
            var l = a.Locomotion;
            bool roll = (l.Mode == LocoMode.ForwardRoll || l.Mode == LocoMode.BackRoll) && a.StateManager.CurrentState == l.State;
            return roll && l.Frame < Settings(a).RollGuardFrom;
        }

        /// <summary>A hit right now counts as a crouching hit (late part of a wakeup roll).</summary>
        public static bool HitAsCrouching(SakugaActor a)
        {
            if (a == null) return false;
            var l = a.Locomotion;
            bool roll = (l.Mode == LocoMode.ForwardRoll || l.Mode == LocoMode.BackRoll) && a.StateManager.CurrentState == l.State;
            return roll && l.Frame >= Settings(a).RollGuardFrom;
        }

        static void Leave(SakugaActor a, LocomotionSettings s, int newState)
        {
            ref LocomotionState l = ref a.Locomotion;
            bool dashing = l.Mode == LocoMode.Run || l.Mode == LocoMode.StepDash || l.Mode == LocoMode.Skid;
            if (dashing && newState != RunSkid && l.DashVelocity != 0)
            {
                bool attack = a.StateManager.CurrentStateType() == StateType.COMBAT;
                bool barrier = System.Array.Exists(BarrierStates, x => x == newState);
                if (attack || barrier)
                {
                    l.SlideLeft = s.SlideFrames;
                    l.SlideVelocity = (int)((long)l.DashVelocity * s.SlidePercent / 100);
                }
            }
            l.Mode = LocoMode.None;
            l.State = -1;
            SetProps(a, 0);
        }

        static bool StopsSlide(SakugaActor a, int state)
        {
            var type = a.StateManager.CurrentStateType();
            return type == StateType.HIT_REACTION || type == StateType.BLOCKING || type == StateType.LOCKED ||
                   System.Array.Exists(SlideStopStates, x => x == state);
        }

        static void Gravity(SakugaActor a)
        {
            a.Body.CurrentGravity = GlobalVariables.DefaultGravity;
            a.Body.AddGravity();
        }

        /// <summary>Double jumps and super jumps turn to face the opponent after crossing over them. Normal jumps don't.</summary>
        static void AutoFace(SakugaActor a)
        {
            int side = a.Body.IsLeftSide ? 1 : -1;
            if (a.Body.PlayerSide != side) a.Body.FlipSide();
        }

        static void SetProps(SakugaActor a, FrameProperties wanted)
        {
            ref LocomotionState l = ref a.Locomotion;
            a.FrameProperties &= ~(FrameProperties)l.PropsSet;
            a.FrameProperties |= wanted;
            l.PropsSet = (byte)(wanted & Managed);
        }
    }
}
