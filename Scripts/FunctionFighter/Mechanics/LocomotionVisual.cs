using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Movement indicators, drawn every rendered frame (visual only, never part of the simulation):
    /// - super jump: faint afterimages trailing the fighter (BBCF-style);
    /// - air dash / air backdash: speed lines streaming past the fighter.
    /// Called from SakugaActor.Render ([FF PATCH: locomotion]).
    /// </summary>
    public static class LocomotionVisual
    {
        /// <summary>Spawn an afterimage every this many rendered frames.</summary>
        public static int AfterimageInterval = 3;
        /// <summary>How long an afterimage takes to fade, in seconds.</summary>
        public static float AfterimageFade = 0.22f;
        public static Color AfterimageColor = new Color(0.65f, 0.85f, 1f, 0.4f);
        public static Color SpeedLineColor = new Color(1f, 1f, 1f, 0.6f);

        const string LinesName = "FF_SpeedLines";
        static readonly System.Collections.Generic.Dictionary<ulong, int> _ghostTimers = new();
        static int _renderFrame;

        public static void Update(SakugaActor a)
        {
            if (a == null || a.Body == null || a.StateManager == null || a.GraphicsNodes == null || a.GraphicsNodes.Length == 0) return;
            _renderFrame++;

            bool superJump = a.IsActive && a.Locomotion.Mode == LocoMode.SuperJump && a.Locomotion.Launched;
            if (superJump)
            {
                ulong id = a.GetInstanceId();
                _ghostTimers.TryGetValue(id, out int t);
                if (t <= 0) { SpawnAfterimage(a); t = AfterimageInterval; }
                _ghostTimers[id] = t - 1;
            }

            int st = a.StateManager.CurrentState;
            bool airDash = a.IsActive && (st == Locomotion.AirDashForward || st == Locomotion.AirDashBack);
            UpdateSpeedLines(a, airDash, st == Locomotion.AirDashForward ? 1 : -1);
            StateFlash.Update(a);
        }

        static void SpawnAfterimage(SakugaActor a)
        {
            Node root = a.GetTree()?.CurrentScene;
            if (root == null) return;
            foreach (Node3D g in a.GraphicsNodes)
                if (g != null && g.Visible) CopyMeshes(g, root);
        }

        static void CopyMeshes(Node n, Node root)
        {
            if (n is MeshInstance3D mi && mi.Visible && mi.Mesh != null)
            {
                Mesh mesh = mi.Mesh;
                if (mi.Skin != null)
                {
                    try { var baked = mi.BakeMeshFromCurrentSkeletonPose(); if (baked != null) mesh = baked; }
                    catch (System.Exception) { }
                }
                var mat = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = AfterimageColor,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    NoDepthTest = false,
                };
                var ghost = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
                root.AddChild(ghost);
                ghost.GlobalTransform = mi.GlobalTransform;
                var tw = ghost.CreateTween();
                tw.TweenProperty(mat, "albedo_color:a", 0f, AfterimageFade);
                tw.TweenCallback(Callable.From(ghost.QueueFree));
            }
            foreach (Node c in n.GetChildren()) CopyMeshes(c, root);
        }

        static void UpdateSpeedLines(SakugaActor a, bool on, int dashDir)
        {
            var lines = a.GetNodeOrNull<Node3D>(LinesName);
            if (lines == null)
            {
                if (!on) return;
                lines = BuildLines();
                lines.Name = LinesName;
                a.AddChild(lines);
            }
            lines.Visible = on;
            if (!on) return;

            // Lines stream behind the direction of travel: a forward dash leaves them behind, a back dash in front.
            int facing = a.Body.PlayerSide;
            Vector3 pos = GlobalFunctions.ToScaledVector3(a.Body.FixedPosition);
            lines.GlobalPosition = pos;
            lines.GlobalRotation = Vector3.Zero;
            int i = 0;
            foreach (Node c in lines.GetChildren())
            {
                if (c is not MeshInstance3D m) continue;
                float jitter = ((_renderFrame * 7 + i * 13) % 10) / 10f;
                float behind = 0.45f + 0.5f * jitter;
                m.Position = new Vector3(-dashDir * facing * behind, 0.15f + i * 0.22f, 0.25f);
                i++;
            }
        }

        static Node3D BuildLines()
        {
            var root = new Node3D();
            var mat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = SpeedLineColor,
            };
            for (int i = 0; i < 6; i++)
            {
                var box = new BoxMesh { Size = new Vector3(0.55f + 0.2f * (i % 3), 0.018f, 0.018f), Material = mat };
                root.AddChild(new MeshInstance3D { Mesh = box, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            }
            return root;
        }
    }
}
