using Godot;

namespace FunctionFighter.Training
{
    /// <summary>
    /// One entry in a fighter's reversal list: a move the training dummy can do on the first possible frame
    /// (after wakeup, blockstun, hitstun or an air tech) without a recording.
    /// </summary>
    [GlobalClass]
    public partial class ReversalAction : Resource
    {
        /// <summary>What the Training menu shows, e.g. "5B  Test Mid".</summary>
        [Export] public string Label = "";
        /// <summary>The move's MoveName in the fighter's stance. If the fighter doesn't have it, the entry is hidden.</summary>
        [Export] public string MoveName = "";
        /// <summary>Input to keep holding after the move starts, in numpad notation facing the opponent (6 = forward):
        /// e.g. "6" keeps a run going, "4AB" holds a Barrier. Empty = let go.</summary>
        [Export] public string Hold = "";
        /// <summary>How many frames to hold it.</summary>
        [Export] public int HoldFrames = 0;
    }
}
