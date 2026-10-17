using System.Collections.Generic;
using Godot;
using SakugaEngine;
using SakugaEngine.Global;

namespace FunctionFighter.Mechanics
{
    /// <summary>
    /// Fantasy Strike-style state colors: a tint + outline on the fighter's model while something about them matters.
    /// Highest first:
    /// - Counter Hit (interrupted): red; Big Counter orange; Functional Counter gold (a short flash after the hit).
    ///   Punish (hit in recovery): purple. Reversal (move on the first possible frame): cyan, on the one who reversed.
    /// - Parry: green (FrameProperties.PARRY, for future parry states; Barrier is not a parry and doesn't flash).
    /// - Armor: blue (super armor up).
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
        /// <summary>[FF PATCH: indicators] Punish (on the punished fighter) and Reversal (on the fighter who reversed).</summary>
        public static Color PunishColor = new Color(0.85f, 0.2f, 1f);
        public static Color ReversalColor = new Color(0.15f, 0.9f, 1f);
        /// <summary>How long a counter flash lasts, in rendered frames.</summary>
        public static int CounterFrames = 24;
        /// <summary>Tint strength (0-1) and outline thickness (model units).</summary>
        public static float TintAlpha = 0.35f, OutlineGrow = 0.025f;

        class Look
        {
            public StandardMaterial3D Tint, Outline;
            public ulong CounterUntil;
            public Color CounterTint;
            public bool Applied;
        }

        static readonly Dictionary<ulong, Look> _looks = new();

        static Look Get(SakugaActor a)
        {
            ulong id = a.GetInstanceId();
            if (_looks.TryGetValue(id, out var l)) return l;
            l = new Look
            {
                Tint = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                },
                Outline = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    CullMode = BaseMaterial3D.CullModeEnum.Front,
                    Grow = true,
                    GrowAmount = OutlineGrow,
                },
            };
            l.Tint.NextPass = l.Outline;
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
                float pulse = 0.65f + 0.35f * Mathf.Sin(Now * 0.37f);
                l.Tint.AlbedoColor = new Color(c.R, c.G, c.B, TintAlpha * pulse);
                l.Outline.AlbedoColor = new Color(c.R, c.G, c.B);
                l.Outline.GrowAmount = OutlineGrow;
            }
            if (on == l.Applied) return;
            l.Applied = on;
            foreach (Node3D g in a.GraphicsNodes)
                if (g != null) SetOverlay(g, on ? l.Tint : null);
        }

        static void SetOverlay(Node n, Material m)
        {
            if (n is GeometryInstance3D gi && !n.Name.ToString().StartsWith("FF_")) gi.MaterialOverlay = m;
            foreach (Node c in n.GetChildren()) SetOverlay(c, m);
        }
    }
}
