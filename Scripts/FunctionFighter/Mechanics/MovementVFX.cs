using Godot;
using SakugaEngine;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Dust / air effects for movement, spawned from the simulation through the fighter's VFX pool (so they're part of
    /// the match state and rollback-safe, like hit sparks). Each effect is a SakugaVFX scene in Fighters/Shared/VFX/Movement
    /// playing one of the movement sprite sheets, listed in the fighter's VFX list under the keys below. A fighter whose
    /// VFX list doesn't have a key simply gets no effect for it.
    ///
    /// | Effect          | Key               | When                                                        |
    /// |-----------------|-------------------|-------------------------------------------------------------|
    /// | Jump - Straight | MoveJumpStraight  | neutral jump / super jump leaves the ground                 |
    /// | Jump - Tilted   | MoveJumpTilted    | forward / back jump or super jump leaves the ground (mirrored for back) |
    /// | Landing         | MoveLanding       | any landing from the air that isn't a hit reaction          |
    /// | Walk_Run        | MoveRun           | run starts, then every RunDustInterval frames while running |
    /// | Break           | MoveBreak         | run skid starts                                             |
    /// | Dash            | MoveDash          | step dash starts; backdash starts (mirrored)                |
    /// | Puff            | MovePuff          | air dash and double jump                                    |
    ///
    /// Per fighter: LocomotionSettings → Movement effects (MovementEffects on / off, RunDustInterval). Size and position of
    /// each effect: its scene's Sprite node (pixel_size, position).
    /// </summary>
    public static class MovementVFX
    {
        public const string JumpStraight = "MoveJumpStraight";
        public const string JumpTilted = "MoveJumpTilted";
        public const string Landing = "MoveLanding";
        public const string Run = "MoveRun";
        public const string Break = "MoveBreak";
        public const string Dash = "MoveDash";
        public const string Puff = "MovePuff";

        /// <summary>Puff height above the feet for an air dash (roughly the middle of the body).</summary>
        public const int AirDashPuffHeight = 6000;
        /// <summary>How far behind the fighter the air dash puff appears.</summary>
        public const int AirDashPuffBehind = 2500;

        /// <summary>For tests: how many movement effects were spawned, and the last key.</summary>
        public static int SpawnCount;
        public static string LastKey = "";
        /// <summary>Test hook: called with the key of every spawned effect.</summary>
        public static System.Action<string> OnSpawn;

        /// <summary>Spawn <paramref name="key"/> at <paramref name="position"/> (world, not attached), mirrored by <paramref name="side"/>.</summary>
        public static void Spawn(SakugaActor a, string key, Vector2I position, int side)
        {
            if (a?.Pool == null || a.Body == null) return;
            if (!Locomotion.Settings(a).MovementEffects) return;
            VFXPoolReference r;
            try { r = a.Pool.GetVFXReference(key); }
            catch (System.Exception) { return; }
            if (r.Objects == null || r.Objects.Length == 0) return;
            a.Pool.SpawnVFX(key, position, side == 0 ? 1 : side, false);
            SpawnCount++;
            LastKey = key;
            OnSpawn?.Invoke(key);
        }

        /// <summary>At the fighter's feet (on the floor under it).</summary>
        public static void AtFeet(SakugaActor a, string key, int side) =>
            Spawn(a, key, new Vector2I(a.Body.FixedPosition.X, 0), side);

        /// <summary>A movement just started (Locomotion.Enter).</summary>
        public static void OnEnter(SakugaActor a, LocoMode mode, int dir)
        {
            if (a?.Body == null) return;
            int facing = a.Body.PlayerSide;
            switch (mode)
            {
                case LocoMode.Run: AtFeet(a, Run, facing); break;
                case LocoMode.Skid:
                {
                    int moving = System.Math.Sign(a.Locomotion.DashVelocity);
                    AtFeet(a, Break, moving != 0 ? moving : facing);
                    break;
                }
                case LocoMode.StepDash: AtFeet(a, Dash, facing); break;
                case LocoMode.Backdash: AtFeet(a, Dash, -facing); break; // mirrored: the trail streams the other way
                case LocoMode.DoubleJump: Spawn(a, Puff, a.Body.FixedPosition, facing); break;
                case LocoMode.AirDash:
                {
                    int way = (dir < 0 ? -1 : 1) * facing; // direction of travel in world space
                    Spawn(a, Puff, a.Body.FixedPosition + new Vector2I(-way * AirDashPuffBehind, AirDashPuffHeight), way);
                    break;
                }
            }
        }

        /// <summary>A jump / super jump left the ground. dir: -1 back, 0 neutral, 1 forward.</summary>
        public static void OnLaunch(SakugaActor a, int dir)
        {
            if (a?.Body == null) return;
            int facing = a.Body.PlayerSide;
            if (dir == 0) AtFeet(a, JumpStraight, facing);
            else AtFeet(a, JumpTilted, dir < 0 ? -facing : facing);
        }
    }
}
