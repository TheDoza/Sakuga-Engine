using Godot;
using SakugaEngine.Global;
using SakugaEngine.Resources;

namespace SakugaEngine
{
    [GlobalClass] [Tool]
    public partial class AnimationViewer : Node
    {
        [Export] private AnimationPlayer[] players;
        [Export] private string[] prefix;
        [Export] public Node3D OffsetNode;
        /// <summary>
        /// [FF PATCH: animation blending] Frames to crossfade from the previous clip into a new one when the clip changes.
        /// 0 = snap (the original behaviour). A state's AnimationSettings.BlendInFrames overrides this (-1 = use this value).
        /// Visual only: hitboxes, hurtboxes and frame data are never affected, so it's rollback-safe.
        /// </summary>
        [Export(PropertyHint.Range, "0,30")] public int BlendFrames = 0;

        private int targetFrame = 0;

        // [FF PATCH: animation blending] Per-player blend tracking. Visual only, never part of the game state.
        private const string TreeName = "SakugaBlendTree";
        private AnimationTree[] trees;
        private string[] currentClip;
        private float[] currentTime;
        private string[] previousClip;
        private float[] previousTime;
        private int[] blendStartFrame;
        private int[] blendLength;

        public void SetDepth(int LayerSorting)
        {
            // TODO
        }

        public void ViewAnimations(AnimationSettings anim, int Frame)
        {
            if (anim == null) return;
            if (string.IsNullOrEmpty(anim.SourceAnimation)) return;

            int modTarget = (Frame - anim.AtFrame) % anim.FrameStepping;

            if (anim.LimitRange)
            {
                targetFrame = (Frame - anim.AtFrame) + anim.AnimationRange.X;
                targetFrame = Mathf.Clamp(targetFrame, anim.AnimationRange.X, anim.AnimationRange.Y);
                if (anim.AnimationRange.Y > anim.AnimationRange.X)
                    targetFrame -= modTarget;
            }
            else
            {
                targetFrame = Frame - anim.AtFrame;
                targetFrame -= modTarget;
            }

            if (anim.Inverse)
                targetFrame = anim.AnimationRange.Y - targetFrame;

            float time = (targetFrame * anim.Speed) / (float)GlobalVariables.TicksPerSecond;
            int blend = anim.BlendInFrames >= 0 ? anim.BlendInFrames : BlendFrames;
            // Once a blend tree exists it handles every clip (a blend of 0 just snaps), so the AnimationPlayer
            // and the tree never fight over the skeleton. Never used in the editor.
            bool useTree = !Engine.IsEditorHint() && (blend > 0 || trees != null);

            for (int a = 0; a < players.Length; a++)
            {
                string prfx = "";
                if (prefix != null && prefix.Length > a) prfx = prefix[a];
                string clip = prfx + anim.SourceAnimation;

                if (useTree) ViewBlended(a, clip, time, blend, Frame);
                else
                {
                    players[a].Play(clip);
                    players[a].Seek(time, true);
                }
            }

            if (OffsetNode != null)
                OffsetNode.Position = anim.Offset;
        }

        // [FF PATCH: animation blending] Shows `clip` at `time`, crossfading from the previous clip over `blend` frames.
        // The previous clip is held on the frame it was showing when the switch happened.
        private void ViewBlended(int a, string clip, float time, int blend, int frame)
        {
            EnsureBlendState();
            AnimationPlayer player = players[a];
            if (player == null || !player.HasAnimation(clip)) return;

            if (clip != currentClip[a])
            {
                if (currentClip[a] != null && blend > 0)
                {
                    previousClip[a] = currentClip[a];
                    previousTime[a] = currentTime[a];
                    blendStartFrame[a] = frame;
                    blendLength[a] = blend;
                }
                else previousClip[a] = null;
                currentClip[a] = clip;
            }

            float weight = 1f;
            if (previousClip[a] != null)
            {
                int elapsed = frame - blendStartFrame[a];
                // Frame went backwards (state restarted / rolled back) or the blend is done: stop blending.
                if (elapsed < 0 || elapsed >= blendLength[a]) previousClip[a] = null;
                else weight = (elapsed + 1) / (float)(blendLength[a] + 1);
            }
            currentTime[a] = time;

            AnimationTree tree = trees[a] ?? CreateTree(a);
            var root = (AnimationNodeBlendTree)tree.TreeRoot;
            var from = (AnimationNodeAnimation)root.GetNode("From");
            var to = (AnimationNodeAnimation)root.GetNode("To");
            string fromClip = previousClip[a] ?? clip;
            if ((string)from.Animation != fromClip) from.Animation = fromClip;
            if ((string)to.Animation != clip) to.Animation = clip;

            if (!tree.Active)
            {
                player.Stop();
                tree.Active = true;
            }
            tree.Set("parameters/FromSeek/seek_request", previousClip[a] != null ? previousTime[a] : time);
            tree.Set("parameters/ToSeek/seek_request", time);
            tree.Set("parameters/Blend/blend_amount", weight);
            tree.Advance(0);
        }

        private void EnsureBlendState()
        {
            if (trees != null) return;
            int n = players.Length;
            trees = new AnimationTree[n];
            currentClip = new string[n];
            currentTime = new float[n];
            previousClip = new string[n];
            previousTime = new float[n];
            blendStartFrame = new int[n];
            blendLength = new int[n];
        }

        // Builds From -> FromSeek \
        //                          Blend -> output
        //        To   -> ToSeek   /
        // next to the AnimationPlayer, using the player's clips and root node. Driven manually (Advance(0)) each render.
        private AnimationTree CreateTree(int a)
        {
            AnimationPlayer player = players[a];
            var root = new AnimationNodeBlendTree();
            root.AddNode("From", new AnimationNodeAnimation(), new Vector2(0, 0));
            root.AddNode("To", new AnimationNodeAnimation(), new Vector2(0, 160));
            root.AddNode("FromSeek", new AnimationNodeTimeSeek(), new Vector2(200, 0));
            root.AddNode("ToSeek", new AnimationNodeTimeSeek(), new Vector2(200, 160));
            root.AddNode("Blend", new AnimationNodeBlend2(), new Vector2(400, 80));
            root.ConnectNode("FromSeek", 0, "From");
            root.ConnectNode("ToSeek", 0, "To");
            root.ConnectNode("Blend", 0, "FromSeek");
            root.ConnectNode("Blend", 1, "ToSeek");
            root.ConnectNode("output", 0, "Blend");

            var tree = new AnimationTree
            {
                Name = TreeName,
                TreeRoot = root,
                CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
                Active = false,
            };
            player.GetParent().AddChild(tree);
            tree.AnimPlayer = tree.GetPathTo(player);
            tree.RootNode = tree.GetPathTo(player.GetNode(player.RootNode));
            trees[a] = tree;
            return tree;
        }
    }
}
