using Godot;

namespace SakugaEngine.Resources
{
    /// <summary>
    /// Passes when one of the actor's pooled spawnables is currently active.
    /// Looks up the pool entry by Index, or by Key when Index is below zero.
    /// Use Not = true on a move to block it while its projectile is still on screen.
    /// </summary>
    [GlobalClass]
    public partial class CheckSpawnActiveCondition : FrameDataCondition
    {
        [Export] private int Index = 0;
        [Export] private string Key = "";
        [Export] private bool Not;

        public override bool Check(ref SakugaActor Actor)
        {
            if (Actor == null || Actor.Pool == null) return Not;

            PoolReference reference;
            if (Index >= 0)
            {
                if (Actor.SpawnablesList == null || Index >= Actor.SpawnablesList.SpawnObjects.Length) return Not;
                reference = Actor.Pool.GetSpawnReference(Index);
            }
            else
            {
                if (string.IsNullOrEmpty(Key)) return Not;
                reference = Actor.Pool.GetSpawnReference(Key);
            }

            if (reference.Objects == null) return Not;

            bool active = Actor.Pool.GetActiveSpawn(reference) != null;
            return Not ? !active : active;
        }
    }
}
