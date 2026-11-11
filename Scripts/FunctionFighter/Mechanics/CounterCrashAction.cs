using Godot;
using SakugaEngine;
using SakugaEngine.Resources;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Counter Crash (guard cancel): put on the Counter Crash state's OnEnter. The move comes out of blockstun, so this ends
    /// the blockstun the fighter was in (the hitstun timer and the BlockStun flag), letting the attack run normally.
    /// The move itself (input 6+A+B in blockstun, 50% meter) is Fighters/Shared/Universal/Moves/29_Counter_Crash.tres.
    /// </summary>
    [GlobalClass]
    public partial class CounterCrashAction : FrameDataAction
    {
        public override void Execute(ref SakugaActor Actor)
        {
            if (Actor == null) return;
            if (Actor.CanHitstun()) Actor.Hitstun.Stop();
            Actor.BlockStun = false;
        }
    }
}
