using System.Collections.Generic;
using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Fantasy Strike-style state colors: a strong color wash, a bright rim glow on the silhouette and (on fighters
    /// without their own outline) a colored outline, while something about the fighter matters.
    /// Highest first:
    /// - Counter Hit (interrupted): red; Big Counter orange; Functional Counter gold (a short flash after the hit).
    ///   Punish and Reversal do not flash (notification only). Purple is reserved for poison (PoisonColor, unused yet).
    /// - Parry: green (FrameProperties.PARRY, for future parry states; Barrier is not a parry and doesn't flash).
    /// - Armor: blue (super armor up). Blue is reserved for armor.
    /// - Invincible: white (strike invincible frames, or an attack / locked state with no hurtbox, e.g. Burst).
    /// Visual only (drawn from LocomotionVisual.Update every rendered frame); in Training, Display → State Flashes.
    /// </summary>
    public static class StateFlash
    {
        public static Color CounterColor = new Color(1f, 0.15f, 0.1f);
        public static Color BigCounterColor = new Color(1f, 0.55f, 0.05f);
        public static Color FunctionalColor = new Color(1f, 0.85f, 0.15f);
        public static Color ParryColor = new Color(0.2f, 1f, 0.35f);
        public static Color ArmorColor = new Color(0.25f, 0.55f, 1f);
        public static Color InvulnColor = new Color(1f, 1f, 1f);
        /// <summary>Reserved for a future poison state (Fantasy Strike purple). Nothing flashes purple yet.</summary>
        public static Color PoisonColor = new Color(0.85f, 0.2f, 1f);
        public static Color ReversalColor = new Color(0.15f, 0.9f, 1f);
        /// <summary>How long a counter flash lasts, in rendered frames.</summary>
        public static int CounterFrames = 24;
        /// <summary>How much of the model is covered by the flash color (0-1). White uses less, so an invincible
        /// fighter reads as glowing rather than turning into a white silhouette.</summary>
        public static float TintStrength = 0.5f, InvulnTintStrength = 0.3f;
        /// <summary>Rim glow: brightness (0-1+), how tight it hugs the silhouette (higher = thinner), and how far
        /// toward white its center goes (gives the hot edge Fantasy Strike has).</summary>
        public static float RimStrength = 1.0f, RimPower = 2.2f, RimWhiten = 0.35f;
        /// <summary>Outline thickness in world units (meters), the same on every fighter whatever its import scale.</summary>
        public static float OutlineGrow = 0.03f;
        /// <summary>Pulse depth (0 = steady, 0.3 = dips to 70%) and speed.</summary>
        public static float PulseDepth = 0.2f, PulseSpeed = 0.37f;

        /// <summary>Overlay shader: mixes the flash color over the model, plus a rim glow from how edge-on each pixel
        /// is. abs() on the facing term so inverted-hull outlines (normals pointing in) still light up at the edge.
        /// Works in the Compatibility renderer.</summary>
        const string OverlayCode = @"
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_back, shadows_disabled, fog_disabled;
uniform vec4 flash_color : source_color = vec4(1.0);
uniform float tint = 0.5;
uniform float rim = 1.0;
uniform float rim_power = 2.2;
uniform float rim_whiten = 0.35;
uniform float pulse = 1.0;
void fragment() {
	float facing = clamp(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.0, 1.0);
	float r = pow(1.0 - facing, rim_power) * rim;
	ALBEDO = mix(flash_color.rgb, vec3(1.0), clamp(r * rim_whiten, 0.0, 1.0));
	ALPHA = clamp((tint + r) * pulse, 0.0, 1.0);
}
";
        static Shader _overlayShader;
        static Shader OverlayShader => _overlayShader ??= new Shader { Code = OverlayCode };

        class Look
        {
            public ShaderMaterial Tint;
            public StandardMaterial3D Outline;
            public ulong CounterUntil;
            public Color CounterTint;
            public bool Applied;
            /// <summary>World scale of the fighter's mesh (0 = not measured yet). Grow is applied in the mesh's own
            /// units, so a model imported at 0.01 scale (Basic Bro) needs 100x the grow of a 1.0 model (the Dummy).</summary>
            public float MeshScale;
        }

        static readonly Dictionary<ulong, Look> _looks = new();

        static Look Get(SakugaActor a)
        {
            ulong id = a.GetInstanceId();
            if (_looks.TryGetValue(id, out var l)) return l;
            l = new Look
            {
                Tint = new ShaderMaterial { Shader = OverlayShader },
                Outline = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    CullMode = BaseMaterial3D.CullModeEnum.Front,
                    Grow = true,
                    GrowAmount = OutlineGrow,
                },
            };
            // Models with their own inverted-hull outline turn this off (DataContainer.StateFlashOutline).
            l.Tint.NextPass = (a.Data?.StateFlashOutline ?? true) ? l.Outline : null;
            _looks[id] = l;
            return l;
        }

        // Stops while Training frame advance is frozen, so flashes freeze with the game.
        static ulong Now => Training.FrameAdvance.Clock;

        /// <summary>Called when a Counter / Big Counter / Functional Counter lands on this fighter.</summary>
        public static void Counter(SakugaActor defender, CounterKind kind)
        {
            if (defender == null) return;
            var l = Get(defender);
            l.CounterUntil = Now + (ulong)CounterFrames;
            l.CounterTint = kind == CounterKind.BigCounter ? BigCounterColor : kind == CounterKind.Functional ? FunctionalColor : CounterColor;
        }

        /// <summary>A short flash in any color (Punish, Reversal), same length as a counter flash.</summary>
        public static void Flash(SakugaActor a, Color color)
        {
            if (a == null) return;
            var l = Get(a);
            l.CounterUntil = Now + (ulong)CounterFrames;
            l.CounterTint = color;
        }

        public static bool Enabled => !Training.TrainingController.Active || Training.TrainingSettings.ShowStateFlash;

        /// <summary>The color this fighter should flash right now (alpha 0 = none).</summary>
        public static Color Current(SakugaActor a)
        {
            var l = Get(a);
            if (Now < l.CounterUntil) return l.CounterTint;
            if (a.ContainsFrameProperty(FrameProperties.PARRY)) return ParryColor;
            if (a.Parameters?.SuperArmor != null && a.Parameters.SuperArmor.CurrentValue > 0) return ArmorColor;
            if (IsInvincible(a)) return InvulnColor;
            return new Color(0, 0, 0, 0);
        }

        public static bool IsInvincible(SakugaActor a)
        {
            if (a.ContainsFrameProperty(FrameProperties.DAMAGE_IMUNITY)) return true;
            var type = a.StateManager.CurrentStateType();
            if (type != StateType.COMBAT && type != StateType.LOCKED) return false;
            var data = a.Body?.CurrentHitbox?.HitboxData;
            if (data?.Hitboxes == null) return true;
            foreach (var h in data.Hitboxes)
                if (h != null && h.HitboxType == HitboxType.HURTBOX && h.Size != Vector2I.Zero) return false;
            return true;
        }

        public static void Update(SakugaActor a)
        {
            if (a?.GraphicsNodes == null || a.StateManager == null) return;
            var l = Get(a);
            Color c = Enabled && a.IsActive ? Current(a) : new Color(0, 0, 0, 0);
            bool on = c.A > 0;
            if (on)
            {
                // Pulse a little so it reads as a flash, not a recolor.
                float pulse = 1f - PulseDepth * 0.5f * (1f + Mathf.Sin(Now * PulseSpeed));
                bool white = c.R > 0.95f && c.G > 0.95f && c.B > 0.95f;
                l.Tint.SetShaderParameter("flash_color", new Color(c.R, c.G, c.B, 1f));
                l.Tint.SetShaderParameter("tint", white ? InvulnTintStrength : TintStrength);
                l.Tint.SetShaderParameter("rim", RimStrength);
                l.Tint.SetShaderParameter("rim_power", RimPower);
                l.Tint.SetShaderParameter("rim_whiten", white ? 0f : RimWhiten);
                l.Tint.SetShaderParameter("pulse", pulse);
                l.Outline.AlbedoColor = new Color(c.R, c.G, c.B);
                if (l.MeshScale <= 0f) l.MeshScale = MeasureMeshScale(a);
                l.Outline.GrowAmount = OutlineGrow / l.MeshScale;
            }
            if (on == l.Applied) return;
            l.Applied = on;
            foreach (Node3D g in a.GraphicsNodes)
                if (g != null) SetOverlay(g, on ? l.Tint : null);
        }

        /// <summary>World scale of the first mesh under the fighter's graphics (1 if none found). Sign is ignored, so a
        /// fighter mirrored to face left measures the same.</summary>
        static float MeasureMeshScale(SakugaActor a)
        {
            foreach (Node3D g in a.GraphicsNodes)
            {
                var gi = g == null ? null : FindGeometry(g);
                if (gi == null) continue;
                Vector3 s = gi.GlobalTransform.Basis.Scale.Abs();
                float avg = (s.X + s.Y + s.Z) / 3f;
                if (avg > 0.000001f) return avg;
            }
            return 1f;
        }

        static GeometryInstance3D FindGeometry(Node n)
        {
            if (n is GeometryInstance3D gi && !n.Name.ToString().StartsWith("FF_")) return gi;
            foreach (Node c in n.GetChildren())
            {
                var found = FindGeometry(c);
                if (found != null) return found;
            }
            return null;
        }

        static void SetOverlay(Node n, Material m)
        {
            if (n is GeometryInstance3D gi && !n.Name.ToString().StartsWith("FF_")) gi.MaterialOverlay = m;
            foreach (Node c in n.GetChildren()) SetOverlay(c, m);
        }
    }
}
