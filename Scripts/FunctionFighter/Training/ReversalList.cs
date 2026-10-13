using Godot;

namespace FunctionFighter.Training
{
    /// <summary>
    /// A fighter's reversal actions for Training (DataContainer → Training Reversals). The fighter's own moves come
    /// first, then the universal list (jumps, backdash, run, Barrier, throws, Burst) unless Include Universal is off.
    /// Fighters without a list get the universal one only.
    /// </summary>
    [GlobalClass]
    public partial class ReversalList : Resource
    {
        [Export] public ReversalAction[] Actions = new ReversalAction[0];
        [Export] public bool IncludeUniversal = true;

        public const string UniversalPath = "res://Fighters/Shared/Training/UniversalReversals.tres";
        static ReversalList _universal;
        public static ReversalList Universal => _universal ??= ResourceLoader.Exists(UniversalPath) ? GD.Load<ReversalList>(UniversalPath) : new ReversalList();
    }
}
