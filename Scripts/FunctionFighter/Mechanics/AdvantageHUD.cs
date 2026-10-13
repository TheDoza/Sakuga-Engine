using System.Collections.Generic;
using Godot;
using SakugaEngine;

namespace FunctionFighter.Mechanics
{
	/// <summary>
	/// Draws the advantage hitsparks (Advantage gives the predicted advantage). Each spark is a hot white core, tapered
	/// spikes thrown in the direction of the hit and a few embers. Color + shape say who's ahead, size says by how much
	/// (±1 small ... ±BigFrames or more big; even stays the base size):
	///   plus        blue burst + shockwave ring + cross (dark blue + double ring from +4)
	///   even        yellow-orange burst with a sideways slash
	///   minus       red burst with an X (dark red from −4)
	/// Colors / sizes / timing are the statics below. Visual only; created on first use, behind the pause menu.
	/// </summary>
	public partial class AdvantageHUD : Control
	{
		public static Color LotPlus = new Color(0.3f, 0.62f, 1f), LittlePlus = new Color(0.55f, 0.88f, 1f), Even = new Color(1f, 0.78f, 0.2f),
			LittleMinus = new Color(1f, 0.38f, 0.3f), LotMinus = new Color(0.95f, 0.12f, 0.1f);
		/// <summary>Seconds a spark lasts.</summary>
		public static float Lifetime = 0.34f;
		/// <summary>Base size in HUD pixels (1280x720): the size of an even (0) spark.</summary>
		public static float Size_ = 62f;
		/// <summary>The size says how much: ±1 frame = SmallScale × base, ±BigFrames or more = BigScale × base, in between
		/// grows evenly. Even (0) stays at the base size.</summary>
		public static float SmallScale = 0.5f, BigScale = 1.65f;
		public static int BigFrames = 10;

		struct SparkFx
		{
			public Vector3 World;
			public AdvantageBand Band;
			public int Adv;
			public float Age;
			public int Seed;
			public int Dir; // attacker facing: streaks fly this way
			public bool Blocked;
		}

		readonly List<SparkFx> _sparks = new();
		static AdvantageHUD _instance;

		public static void Spark(Vector2I contact, int advantage, SakugaActor attacker, bool blocked)
		{
			var hud = Ensure();
			if (hud == null) return;
			var at = contact == Vector2I.Zero ? attacker.Body.FixedPosition + new Vector2I(0, 8000) : contact;
			hud._sparks.Add(new SparkFx
			{
				World = SakugaEngine.Global.GlobalFunctions.ToScaledVector3(at),
				Band = Advantage.Band(advantage),
				Adv = advantage,
				Seed = (at.X * 73856093) ^ (at.Y * 19349663) ^ (int)Time.GetTicksMsec(),
				Dir = attacker.Body.PlayerSide,
				Blocked = blocked,
			});
		}

		static AdvantageHUD Ensure()
		{
			if (_instance != null && IsInstanceValid(_instance)) return _instance;
			var tree = Engine.GetMainLoop() as SceneTree;
			var pause = tree?.Root.FindChild("PauseMenu", true, false);
			if (pause == null) return null;
			var ui = pause.GetParent();
			_instance = new AdvantageHUD { Name = "AdvantageHUD", MouseFilter = MouseFilterEnum.Ignore };
			ui.AddChild(_instance);
			_instance.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			ui.MoveChild(_instance, pause.GetIndex());
			return _instance;
		}

		ulong _lastClock;

		public override void _Process(double delta)
		{
			// Frozen by Training frame advance: only age by the frames stepped.
			ulong clock = Training.FrameAdvance.Clock;
			float dt = Training.FrameAdvance.Frozen ? (clock - _lastClock) / 60f : (float)delta;
			_lastClock = clock;
			for (int i = _sparks.Count - 1; i >= 0; i--)
			{
				var s = _sparks[i];
				s.Age += dt;
				if (s.Age > Lifetime) _sparks.RemoveAt(i); else _sparks[i] = s;
			}
			QueueRedraw();
		}

		/// <summary>Spark size multiplier for this much advantage (bigger = more plus / more minus).</summary>
		public static float SizeScale(int adv)
		{
			int a = Mathf.Abs(adv);
			if (a == 0) return 1f;
			float t = BigFrames <= 1 ? 1f : Mathf.Clamp((a - 1f) / (BigFrames - 1f), 0f, 1f);
			return Mathf.Lerp(SmallScale, BigScale, t);
		}

		public static Color BandColor(AdvantageBand b) => b switch
		{
			AdvantageBand.LotPlus => LotPlus,
			AdvantageBand.LittlePlus => LittlePlus,
			AdvantageBand.Even => Even,
			AdvantageBand.LittleMinus => LittleMinus,
			_ => LotMinus,
		};

		public override void _Draw()
		{
			var cam = GetViewport()?.GetCamera3D();
			if (cam == null) return;
			float scale = GetViewportRect().Size.X / 1280f;
			foreach (var s in _sparks)
			{
				if (cam.IsPositionBehind(s.World)) continue;
				DrawSpark(cam.UnprojectPosition(s.World), s, scale);
			}
		}

		// Spike from radius r0 to r1 along dir: wide at the base, sharp at the tip.
		void Spike(Vector2 p, Vector2 dir, float r0, float r1, float width, Color c)
		{
			if (r1 <= r0) return;
			var side = new Vector2(-dir.Y, dir.X) * width * 0.5f;
			var b = p + dir * r0;
			DrawColoredPolygon(new[] { b + side, p + dir * r1, b - side, p + dir * Mathf.Max(0, r0 - width * 0.6f) }, c);
		}

		void DrawSpark(Vector2 p, SparkFx s, float scale)
		{
			var rng = new RandomNumberGenerator { Seed = (ulong)(uint)s.Seed };
			float t = Mathf.Clamp(s.Age / Lifetime, 0, 1);
			// Pops to full size almost instantly, then the spikes keep flying out while the middle hollows.
			float pop = t < 0.1f ? 0.7f + 3.5f * t : 1.05f + 0.25f * (t - 0.1f);
			float hollow = t < 0.15f ? 0f : (t - 0.15f) / 0.85f;
			float fade = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
			bool lot = s.Band == AdvantageBand.LotPlus || s.Band == AdvantageBand.LotMinus;
			bool plus = s.Band == AdvantageBand.LotPlus || s.Band == AdvantageBand.LittlePlus;
			bool minus = s.Band == AdvantageBand.LotMinus || s.Band == AdvantageBand.LittleMinus;
			float size = Size_ * scale * SizeScale(s.Adv) * (s.Blocked ? 0.8f : 1f) * pop;
			Color c = BandColor(s.Band);
			Color hot = c.Lerp(Colors.White, 0.75f);
			Color deep = c.Darkened(0.25f);
			c.A *= fade; hot.A *= fade; deep.A *= fade * 0.9f;
			var fwd = new Vector2(s.Dir, 0);

			// flash + glow (biggest on the first frames)
			float flash = Mathf.Max(0, 1f - t * 4f);
			DrawCircle(p, size * 1.25f, new Color(c.R, c.G, c.B, 0.18f * fade));
			DrawCircle(p, size * (0.9f + 0.3f * flash), new Color(hot.R, hot.G, hot.B, 0.35f * flash));

			// starburst: radial spikes, longer toward the way the hit was going
			int n = lot ? 18 : 14;
			float off = rng.RandfRange(0, Mathf.Tau);
			for (int i = 0; i < n; i++)
			{
				float ang = off + Mathf.Tau * i / n + rng.RandfRange(-0.12f, 0.12f);
				var d = Vector2.Right.Rotated(ang);
				float bias = 0.75f + 0.45f * Mathf.Max(0, d.Dot(fwd));
				float len = size * (i % 2 == 0 ? rng.RandfRange(1.1f, 1.5f) : rng.RandfRange(0.6f, 0.9f)) * bias;
				float r0 = len * 0.7f * hollow;
				float w = size * (i % 2 == 0 ? 0.26f : 0.18f) * (1f - 0.5f * hollow);
				Spike(p, d, r0, len, w, deep);
				Spike(p, d, r0, len * 0.62f, w * 0.55f, hot);
			}

			// band shape: the readable part
			if (plus)
			{
				// shockwave ring(s): you're plus
				DrawArc(p, size * (0.5f + 1.1f * t), 0, Mathf.Tau, 48, new Color(hot.R, hot.G, hot.B, fade), (lot ? 5f : 3f) * scale * (1 - t) + 1f);
				if (lot) DrawArc(p, size * (0.3f + 0.75f * t), 0, Mathf.Tau, 40, c, 2.5f * scale * (1 - t) + 1f);
				foreach (var d in new[] { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right })
					Spike(p, d, size * 0.3f * hollow, size * (lot ? 2.1f : 1.6f), size * 0.16f, hot);
			}
			else if (minus)
			{
				// a broken X: you're minus
				foreach (var d0 in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
				{
					var d = d0.Normalized();
					Spike(p, d, size * 0.35f * hollow, size * (lot ? 2f : 1.55f), size * (lot ? 0.3f : 0.22f), deep);
					Spike(p, d, size * 0.35f * hollow, size * (lot ? 1.3f : 1f), size * 0.12f, hot);
				}
			}
			else
			{
				// even: one long horizontal slash through the hit
				Spike(p, fwd, 0, size * 2.2f, size * 0.24f, hot);
				Spike(p, -fwd, 0, size * 1.4f, size * 0.18f, hot);
			}

			// sparks thrown forward that fall a little
			int embers = lot ? 10 : 7;
			for (int i = 0; i < embers; i++)
			{
				var d = fwd.Rotated(rng.RandfRange(-1.0f, 1.0f));
				float dist = size * rng.RandfRange(0.9f, 2.0f) * (0.4f + 0.6f * t);
				var q = p + d * dist + new Vector2(0, 40 * scale * t * t);
				DrawLine(q, q - d * size * 0.25f, hot, Mathf.Max(1f, 3f * scale * (1 - t)));
			}

			// white-hot core
			DrawCircle(p, size * 0.38f * (1f - 0.8f * hollow), new Color(1, 1, 1, fade));
		}
	}
}
