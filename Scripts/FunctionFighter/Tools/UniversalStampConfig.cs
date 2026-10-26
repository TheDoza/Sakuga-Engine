using Godot;
using Godot.Collections;

namespace FunctionFighter.Tools
{
	/// <summary>
	/// Copies the universal state layout (states 0-99, universal moves, hit/block reaction tables)
	/// from a template fighter (the Dummy) onto another fighter, including throws and throw reject.
	///
	/// Usage: FileSystem dock > right-click > Create New > Resource > UniversalStampConfig,
	/// fill in the fields, then click "Stamp Universal States" in the Inspector.
	///
	/// The target's own states (100+) and own moves (anything not in Fighters/Shared/Universal/Moves)
	/// are kept. Re-stamping is safe: it only rewrites the universal part.
	///
	/// Loop lengths: when ModelScene is set, every universal state that loops itself (state ends -> go to frame 0,
	/// e.g. Idle, Walk, Crouching, the Barrier stances) gets its Duration set to its clip's length in frames, so the
	/// loop restarts exactly where the clip does and doesn't pop. Recalculated on every stamp, so it survives re-stamping.
	///
	/// Note: Sakuga's resource scripts are not [Tool] scripts, so in the editor they load as
	/// placeholders. This tool therefore reads and writes them through Get()/Set() by property
	/// name instead of casting to FighterState, DataContainer, etc.
	/// </summary>
	[GlobalClass, Tool]
	public partial class UniversalStampConfig : Resource
	{
		public const int UniversalCount = 100;
		/// <summary>Size of the old universal layout (before locomotion). Fighters stamped with it get their own states moved from 60+ to 100+.</summary>
		public const int LegacyUniversalCount = 60;
		public const string UniversalMovesFolder = "res://Fighters/Shared/Universal/Moves/";
		public const string SharedFolder = "res://Fighters/Shared/";

		[ExportGroup("Fighters")]
		/// <summary>The fighter whose states 0-99 are copied. Normally Fighters/Dummy/Dummy_Data.tres.</summary>
		[Export] public Resource TemplateData;
		/// <summary>The fighter to stamp (its DataContainer .tres).</summary>
		[Export] public Resource TargetData;
		/// <summary>Folder of the target fighter. Universal files go to TargetFolder/Universal/.</summary>
		[Export(PropertyHint.Dir)] public string TargetFolder = "res://Fighters/NewFighter";

		[ExportGroup("Template Hurtboxes (leave empty for the Dummy's)")]
		[Export] public Resource TemplateStandHurtbox;
		[Export] public Resource TemplateCrouchHurtbox;
		[Export] public Resource TemplateLyingHurtbox;

		[ExportGroup("Target Hurtboxes (empty = reuse the template's)")]
		[Export] public Resource StandHurtbox;
		[Export] public Resource CrouchHurtbox;
		[Export] public Resource LyingHurtbox;

		[ExportGroup("Template Throw Boxes (leave empty for the Dummy's)")]
		[Export] public Resource TemplateGroundThrowBox;
		[Export] public Resource TemplateAirThrowBox;

		[ExportGroup("Target Throw Boxes (empty = reuse the template's)")]
		/// <summary>HitboxSettings used on the active frames of the ground throws (hurtbox + throw box).</summary>
		[Export] public Resource GroundThrowBox;
		/// <summary>HitboxSettings used on the active frames of the air throw (hurtbox + throw box).</summary>
		[Export] public Resource AirThrowBox;

		[ExportGroup("Animations")]
		/// <summary>When on, each universal state plays the clip named in AnimationNames (keyed by state name).</summary>
		[Export] public bool UseAnimations = true;
		[Export] public Dictionary<string, string> AnimationNames = DefaultAnimationNames();

		[ExportGroup("Loop Lengths")]
		/// <summary>The fighter's model (its .glb, or a scene containing its AnimationPlayer). Used to read clip lengths.</summary>
		[Export] public PackedScene ModelScene;
		/// <summary>When on (and ModelScene is set), self-looping universal states get Duration = their clip's length in frames.</summary>
		[Export] public bool MatchLoopDurations = true;
		/// <summary>Other universal states whose Duration should always be their clip's length (one-shot clips that end into another state).</summary>
		[Export] public string[] ClipLengthStates = { "Habit", "Outro" };

		[ExportToolButton("Stamp Universal States")]
		public Callable StampButton => Callable.From(Stamp);

		[ExportToolButton("Reset Animation Names To Defaults")]
		public Callable ResetNamesButton => Callable.From(() => { AnimationNames = DefaultAnimationNames(); NotifyPropertyListChanged(); });

		public static Dictionary<string, string> DefaultAnimationNames() => new Dictionary<string, string>
		{
			// Clip names follow the Kaede reference list used for the Unity -> Blender -> glTF export.
			{ "Idle", "Idle" }, { "Walk_Forward", "Walk_Forward" }, { "Walk_Backward", "Walk_Backwards" },
			{ "Jump_Neutral", "Jump_Neutral" }, { "Jump_Forward", "Jump_Forward" }, { "Jump_Backward", "Jump_Backwards" },
			{ "Falling", "Falling" }, { "AirDash_Forward", "AirDash_Forward" }, { "AirDash_Backward", "AirDash_Backwards" },
			{ "Crouch_Enter", "Crouch_Enter" }, { "Crouching", "Crouching" }, { "Crouch_Exit", "Crouch_Exit" },
			{ "Air_Recovery", "Air_Recovery" }, { "Ground_Recovery", "Ground_Recovery" },
			{ "Hit_Stand_Weak", "Hit_Weak" }, { "Hit_Stand_Strong", "Hit_Strong" }, { "Hit_Crouch", "Hit_Crouch_Weak" },
			{ "Hit_Air_Weak", "Hit_Air_Weak" }, { "Hit_Air", "Hit_Air_Strong" }, { "Hit_GroundSlide", "Hit_OnTheGround_1" },
			{ "Hit_Down", "Hit_Down" }, { "Hit_GetUp", "Hit_GetUp" }, { "Hit_WallBounce", "Hit_WallBounce" }, { "Hit_GroundBounce", "Hit_GroundBounce" },
			{ "Block_Stand_Enter", "Block_Neutral_Enter" }, { "Blockstun_Stand", "Blockstun_Neutral" }, { "Block_Stand_Exit", "Block_Neutral_Exit" },
			{ "Blocking_Stand", "Blocking_Neutral" }, { "GuardBreak_Stand", "Guard_Break_Neutral" },
			{ "Block_Crouch_Enter", "Block_Crouch_Enter" }, { "Blockstun_Crouch", "Blockstun_Crouch" }, { "Block_Crouch_Exit", "Block_Crouch_Exit" },
			{ "Blocking_Crouch", "Blocking_Crouch" }, { "GuardBreak_Crouch", "Guard_Break_Crouch" },
			{ "Block_Air_Enter", "Block_Air_Enter" }, { "Blockstun_Air", "Blockstun_Air" }, { "Block_Air_Exit", "Block_Air_Exit" },
			{ "Blocking_Air", "Blocking_Air" }, { "GuardBreak_Air", "Guard_Break_Air" },
			{ "Throw_Forward", "Throw_Enter" }, { "Throw_Forward_Hit", "Throw_Sequence" },
			{ "Throw_Back", "Throw_Enter" }, { "Throw_Back_Hit", "Throw_Back_Sequence" },
			{ "Throw_Air", "Throw_Air_Enter" }, { "Throw_Air_Hit", "Throw_Air_Sequence" }, { "Throw_Air_Landing", "Landing" },
			{ "Hit_Thrown", "Throw_Reaction" }, { "Hit_Thrown_Launch", "Hit_Air_Strong" },
			{ "Throw_Reject_Ground", "Throw_Reject" }, { "Throw_Reject_Air", "Throw_Reject_Air" },
			{ "Throw_Rejected_Ground", "Throw_Rejected" }, { "Throw_Rejected_Air", "Throw_Rejected_Air" },
			{ "Barrier_Stand", "Barrier_Neutral" }, { "Barrier_Crouch", "Barrier_Crouch" }, { "Barrier_Air", "Barrier_Air" },
			{ "Burst", "Burst" },
			{ "Roll_Forward", "Roll_Forward" }, { "Roll_Back", "Roll_Back" },
			{ "Run", "Run" }, { "Run_Skid", "Run_Stop" }, { "Step_Dash", "Dash_Forward" }, { "Backdash", "Dash_Backwards" },
			{ "Super_Jump_Neutral", "Jump_Neutral" }, { "Super_Jump_Forward", "Jump_Forward" }, { "Super_Jump_Backward", "Jump_Backwards" },
			{ "Double_Jump_Neutral", "Jump_Neutral" }, { "Double_Jump_Forward", "Jump_Forward" }, { "Double_Jump_Backward", "Jump_Backwards" },
			{ "Outro", "Outro" }, { "Habit", "Habit" },
		};

		private const string DummyStand = "res://Fighters/Dummy/Hitboxes/0_Hitbox_Basic.tres";
		private const string DummyCrouch = "res://Fighters/Dummy/Hitboxes/3_Hitbox_Crouch.tres";
		private const string DummyLying = "res://Fighters/Dummy/Hitboxes/1_Hitbox_Lying.tres";
		private const string DummyGroundThrow = "res://Fighters/Dummy/Hitboxes/7_Hitbox_Throw_Active.tres";
		private const string DummyAirThrow = "res://Fighters/Dummy/Hitboxes/8_Hitbox_AirThrow_Active.tres";

		/// <summary>Returns a short report. Errors are also pushed to the Output panel.</summary>
		public string Stamp()
		{
			if (TemplateData == null) return Fail("TemplateData is empty. Set it to Fighters/Dummy/Dummy_Data.tres.");
			if (TargetData == null) return Fail("TargetData is empty. Set it to the new fighter's _Data.tres.");
			if (TargetData == TemplateData || TargetData.ResourcePath == TemplateData.ResourcePath) return Fail("Target and template are the same fighter.");
			if (string.IsNullOrEmpty(TargetFolder) || !TargetFolder.StartsWith("res://")) return Fail("TargetFolder must be a res:// folder.");

			Array templateStates = TemplateData.Get("States").AsGodotArray();
			if (templateStates.Count < UniversalCount) return Fail($"Template has {templateStates.Count} states; it needs at least {UniversalCount}.");

			// Template hurtbox -> target hurtbox
			var hurtboxMap = new System.Collections.Generic.Dictionary<string, Resource>();
			AddHurtbox(hurtboxMap, TemplateStandHurtbox, DummyStand, StandHurtbox);
			AddHurtbox(hurtboxMap, TemplateCrouchHurtbox, DummyCrouch, CrouchHurtbox);
			AddHurtbox(hurtboxMap, TemplateLyingHurtbox, DummyLying, LyingHurtbox);
			AddHurtbox(hurtboxMap, TemplateGroundThrowBox, DummyGroundThrow, GroundThrowBox);
			AddHurtbox(hurtboxMap, TemplateAirThrowBox, DummyAirThrow, AirThrowBox);

			// Read the target's own states BEFORE writing anything: saving the new universal files
			// takes over the old files' paths, after which the old objects can't be recognised.
			Variant targetStatesV = TargetData.Get("States");
			Array oldStates = targetStatesV.VariantType == Variant.Type.Nil ? new Array() : targetStatesV.AsGodotArray();
			bool legacyStamped = oldStates.Count >= LegacyUniversalCount && IsUniversal(oldStates[0].As<Resource>()) &&
				(oldStates.Count <= LegacyUniversalCount || !IsUniversal(oldStates[LegacyUniversalCount].As<Resource>()));
			bool stampedBefore = !legacyStamped && oldStates.Count >= UniversalCount && IsUniversal(oldStates[0].As<Resource>());
			var ownStates = new Array();
			string note = "";
			if (stampedBefore)
				for (int i = UniversalCount; i < oldStates.Count; i++) ownStates.Add(oldStates[i]);
			else if (legacyStamped)
			{
				// Stamped with the old 0-59 layout: its own states move from 60+ to 100+ (own moves are renumbered below).
				for (int i = LegacyUniversalCount; i < oldStates.Count; i++) ownStates.Add(oldStates[i]);
				if (ownStates.Count > 0)
					note += $" Its own states moved from {LegacyUniversalCount}+ to {UniversalCount}+ (own moves' MoveState renumbered; check any other StateIndex numbers that point at them).";
			}
			else if (oldStates.Count > 0)
			{
				foreach (var s in oldStates) ownStates.Add(s);
				note = $" Its {oldStates.Count} existing states now start at {UniversalCount}: update any StateIndex/MoveState numbers that point at them.";
				GD.PushWarning("[UniversalStamp]" + note);
			}

			string folder = TargetFolder.TrimEnd('/');
			string statesDir = folder + "/Universal/States";
			string animDir = folder + "/Universal/AnimationData";
			DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(statesDir));
			DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(animDir));

			// Clip name -> length in frames, read from the fighter's model (null = don't touch loop durations).
			var clipFrames = (UseAnimations && MatchLoopDurations) ? ReadClipFrames() : null;
			int loopsMatched = 0;
			var loopsMissing = new System.Collections.Generic.List<string>();

			var universal = new Array();
			int written = 0, missingClips = 0;
			for (int i = 0; i < UniversalCount; i++)
			{
				var t = templateStates[i].As<Resource>();
				if (t == null) return Fail($"Template state {i} is empty.");

				// Shared placeholders (Reserved) are referenced, not copied.
				if (t.ResourcePath.StartsWith(SharedFolder)) { universal.Add(t); continue; }

				string stateName = (string)t.Get("StateName");
				string fileName = $"{i}_{stateName.Replace(" ", "")}";

				var tAnim = t.Get("AnimationData").As<Resource>();
				Resource anim = null;
				if (tAnim != null)
				{
					anim = tAnim.Duplicate(true);
					// Remap hurtboxes, using the template's (un-duplicated) list to read the original paths.
					Array tBoxes = tAnim.Get("Hitboxes").AsGodotArray();
					Array boxes = anim.Get("Hitboxes").AsGodotArray();
					for (int b = 0; b < boxes.Count && b < tBoxes.Count; b++)
					{
						var tHs = tBoxes[b].As<Resource>();
						var hs = boxes[b].As<Resource>();
						if (tHs == null || hs == null) continue;
						var src = tHs.Get("HitboxData").As<Resource>();
						if (src == null) { hs.Set("HitboxData", default(Variant)); continue; }
						hs.Set("HitboxData", hurtboxMap.TryGetValue(src.ResourcePath, out var mapped) ? mapped : src);
					}
					anim.Set("Hitboxes", boxes);

					if (UseAnimations)
					{
						var clips = new Array();
						string clip = null;
						if (AnimationNames != null && AnimationNames.TryGetValue(stateName, out string named) && !string.IsNullOrEmpty(named))
						{
							clip = named;
							clips.Add(new SakugaEngine.Resources.AnimationSettings { SourceAnimation = clip });
						}
						else missingClips++;
						anim.Set("Animations", clips);

						// Self-looping state: make its length match the clip so the loop doesn't pop.
						if (clipFrames != null && clip != null && (IsSelfLooping(t) || (ClipLengthStates != null && System.Array.IndexOf(ClipLengthStates, stateName) >= 0)))
						{
							if (clipFrames.TryGetValue(clip, out int frames) && frames > 0)
							{
								anim.Set("Duration", frames);
								loopsMatched++;
							}
							else loopsMissing.Add($"{stateName} ({clip})");
						}
					}

					string animPath = $"{animDir}/{fileName}_AnimationData.tres";
					// Take the path first so re-stamping doesn't trip Godot's "another resource is loaded from path" error.
					anim.TakeOverPath(animPath);
					Error e = ResourceSaver.Save(anim);
					if (e != Error.Ok) return Fail($"Could not save {animPath}: {e}");
				}

				var state = t.Duplicate(true);
				state.Set("AnimationData", anim);
				string statePath = $"{statesDir}/{fileName}.tres";
				state.TakeOverPath(statePath);
				Error se = ResourceSaver.Save(state);
				if (se != Error.Ok) return Fail($"Could not save {statePath}: {se}");
				universal.Add(state);
				written++;
			}

			// Target states = universal 0-99 + the target's own states (100+), collected before stamping.
			var states = new Array(universal);
			foreach (var s in ownStates) states.Add(s);
			TargetData.Set("States", states);

			// Sakuga's AI brain crashes on a fighter without AI data, so borrow the template's until the fighter has its own.
			if (TargetData.Get("_aiData").VariantType == Variant.Type.Nil && TemplateData.Get("_aiData").VariantType != Variant.Type.Nil)
			{
				TargetData.Set("_aiData", TemplateData.Get("_aiData"));
				note += " AI data was empty, so it now uses the template's (replace it later).";
			}

			// Stance: universal moves first (shared files), then the target's own moves; copy reaction tables.
			Array templateStances = TemplateData.Get("Stances").AsGodotArray();
			var tStance = templateStances.Count > 0 ? templateStances[0].As<Resource>() : null;
			if (tStance == null) return Fail("Template has no stance.");
			Variant targetStancesV = TargetData.Get("Stances");
			Array targetStances = targetStancesV.VariantType == Variant.Type.Nil ? new Array() : targetStancesV.AsGodotArray();
			Resource stance = targetStances.Count > 0 ? targetStances[0].As<Resource>() : null;
			if (stance == null)
			{
				stance = tStance.Duplicate(false);
				stance.Set("Moves", new Array());
				string stancePath = folder + "/Stances/0_Stance_Main.tres";
				DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(folder + "/Stances"));
				ResourceSaver.Save(stance, stancePath, ResourceSaver.SaverFlags.ChangePath);
				stance.TakeOverPath(stancePath);
				targetStances = new Array { stance };
				TargetData.Set("Stances", targetStances);
			}
			var moves = new Array();
			foreach (var m in tStance.Get("Moves").AsGodotArray())
				if (m.As<Resource>()?.ResourcePath.StartsWith(UniversalMovesFolder) == true) moves.Add(m);
			int universalMoves = moves.Count;
			Variant ownMovesV = stance.Get("Moves");
			if (ownMovesV.VariantType != Variant.Type.Nil)
				foreach (var m in ownMovesV.AsGodotArray())
					if (m.As<Resource>() != null && !m.As<Resource>().ResourcePath.StartsWith(UniversalMovesFolder)) moves.Add(m);
			stance.Set("Moves", moves);
			if (legacyStamped)
				foreach (var m in moves)
				{
					var mv = m.As<Resource>();
					if (mv == null || mv.ResourcePath.StartsWith(UniversalMovesFolder)) continue;
					int ms = (int)mv.Get("MoveState");
					if (ms >= LegacyUniversalCount) { mv.Set("MoveState", ms + (UniversalCount - LegacyUniversalCount)); ResourceSaver.Save(mv); }
				}
			stance.Set("HitReactions", tStance.Get("HitReactions"));
			var blocks = new Array();
			foreach (var b in tStance.Get("BlockReactions").AsGodotArray()) blocks.Add(b.As<Resource>().Duplicate(true));
			stance.Set("BlockReactions", blocks);
			foreach (string p in new[] { "DefaultState", "ThrowEscapeInput", "GroundThrowEscapeState", "AirThrowEscapeState",
										 "GroundThrowRejectedState", "AirThrowRejectedState" })
				stance.Set(p, tStance.Get(p));
			ResourceSaver.Save(stance);

			// Movement numbers (run, dashes, jumps): the fighter gets its own copy of the template's to tune.
			if (TargetData.Get("Locomotion").VariantType == Variant.Type.Nil && TemplateData.Get("Locomotion").VariantType != Variant.Type.Nil)
			{
				var loco = TemplateData.Get("Locomotion").As<Resource>().Duplicate(true);
				string locoPath = $"{folder}/{folder.GetFile()}_Locomotion.tres";
				loco.TakeOverPath(locoPath);
				ResourceSaver.Save(loco);
				TargetData.Set("Locomotion", loco);
				note += $" Movement numbers copied to {locoPath}.";
			}
			ResourceSaver.Save(TargetData);

			string report = $"[UniversalStamp] Stamped {written} universal states and {universalMoves} universal moves onto {TargetData.ResourcePath}." +
				(UseAnimations && missingClips > 0 ? $" {missingClips} states have no clip name (they'll show no animation)." : "") +
				(clipFrames != null ? $" {loopsMatched} looping / clip-length states matched to their clip length." : "") +
				(loopsMissing.Count > 0 ? $" Clip not found in ModelScene for: {string.Join(", ", loopsMissing)}." : "") +
				(UseAnimations && MatchLoopDurations && ModelScene == null ? " ModelScene is empty, so loop lengths were left as the template's." : "") + note;
			GD.Print(report);
#if TOOLS
			if (Engine.IsEditorHint()) EditorInterface.Singleton.GetResourceFilesystem().Scan();
#endif
			return report;
		}

		/// <summary>
		/// Reads every clip in ModelScene's AnimationPlayer and returns name -> length in game frames.
		/// Clips in a named library are listed as "library/clip" (the default library has no prefix).
		/// </summary>
		private System.Collections.Generic.Dictionary<string, int> ReadClipFrames()
		{
			if (ModelScene == null) return null;
			var result = new System.Collections.Generic.Dictionary<string, int>();
			Node root = ModelScene.Instantiate();
			try
			{
				AnimationPlayer player = root as AnimationPlayer;
				if (player == null)
					foreach (Node n in root.FindChildren("*", "AnimationPlayer", true, false)) { player = n as AnimationPlayer; if (player != null) break; }
				if (player == null)
				{
					GD.PushWarning("[UniversalStamp] ModelScene has no AnimationPlayer; loop lengths left as the template's.");
					return null;
				}
				foreach (StringName lib in player.GetAnimationLibraryList())
				{
					AnimationLibrary library = player.GetAnimationLibrary(lib);
					foreach (StringName name in library.GetAnimationList())
					{
						string key = string.IsNullOrEmpty(lib) ? (string)name : $"{lib}/{name}";
						int frames = Mathf.RoundToInt(library.GetAnimation(name).Length * SakugaEngine.Global.GlobalVariables.TicksPerSecond);
						result[key] = frames;
					}
				}
			}
			finally
			{
				root.Free();
			}
			return result;
		}

		/// <summary>True if the state restarts itself when it ends (state ended -> go to frame 0), like Idle or Walk.</summary>
		private static bool IsSelfLooping(Resource state)
		{
			Variant eventsV = state.Get("TransitionEvents");
			if (eventsV.VariantType == Variant.Type.Nil) return false;
			foreach (var ev in eventsV.AsGodotArray())
			{
				var e = ev.As<Resource>();
				if (e == null) continue;
				Variant condsV = e.Get("Conditions"), actsV = e.Get("Actions");
				if (condsV.VariantType == Variant.Type.Nil || actsV.VariantType == Variant.Type.Nil) continue;
				var conds = condsV.AsGodotArray();
				var acts = actsV.AsGodotArray();
				if (conds.Count != 1 || acts.Count != 1) continue;
				if (!ScriptIs(conds[0].As<Resource>(), "StateEndedCondition.cs")) continue;
				var act = acts[0].As<Resource>();
				if (!ScriptIs(act, "GoToFrameActions.cs")) continue;
				Variant frame = act.Get("Frame");
				if (frame.VariantType == Variant.Type.Nil || (int)frame == 0) return true;
			}
			return false;
		}

		private static bool ScriptIs(Resource r, string fileName)
		{
			if (r == null) return false;
			var script = r.GetScript().As<Script>();
			return script != null && script.ResourcePath.EndsWith(fileName);
		}

		private static bool IsUniversal(Resource r) =>
			r != null && (r.ResourcePath.Contains("/Universal/States/") || r.ResourcePath.StartsWith(SharedFolder));

		private static void AddHurtbox(System.Collections.Generic.Dictionary<string, Resource> map, Resource template, string fallbackPath, Resource target)
		{
			var t = template ?? (ResourceLoader.Exists(fallbackPath) ? ResourceLoader.Load(fallbackPath) : null);
			if (t != null && target != null) map[t.ResourcePath] = target;
		}

		private static string Fail(string msg)
		{
			GD.PushError("[UniversalStamp] " + msg);
			return "ERROR: " + msg;
		}
	}
}
