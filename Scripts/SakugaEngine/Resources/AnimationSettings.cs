using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class AnimationSettings : Resource
    {
        [Export] public int AtFrame;
        [Export] public string SourceAnimation;
        [Export] public float Speed = 1f;
        [Export] public bool Inverse = false;
        [Export] public int FrameStepping = 1;
        [Export] public bool LimitRange;
        [Export] public Vector2I AnimationRange;
        [Export] public Vector3 Offset = Vector3.Zero;
        /// <summary>[FF PATCH: animation blending] Frames to crossfade into this clip from the previous one.
        /// -1 = use the fighter's AnimationViewer.BlendFrames, 0 = snap (no blend). Visual only.</summary>
        [Export] public int BlendInFrames = -1;
    }
}
