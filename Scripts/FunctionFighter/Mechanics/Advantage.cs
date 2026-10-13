using System.Collections.Generic;
using Godot;
using SakugaEngine;

namespace FunctionFighter.Mechanics
{
    public enum AdvantageBand { LotMinus, LittleMinus, Even, LittlePlus, LotPlus }

    /// <summary>
    /// Advantage hitsparks (Fantasy Strike style): the hitspark itself tells the ATTACKER whether they recover first after
    /// a hit or block, and by how much (color/shape = plus, even or minus; size = how much).
    ///
    /// The spark spawns on the impact frame, so the advantage is predicted at that moment:
    ///   predicted = (defender's stun + defender's exit frames) − attacker's frames until it can act
    /// - Attacker frames: the frames left in the move's state; once measured, the real number for that move at that frame
    ///   (covers moves whose recovery spans several states).
    /// - Exit frames: frames after the stun before the defender can act in that reaction (Block_Stand_Exit = 4, a normal
    ///   hit = 0, a knockdown = the time lying). Block exits are seeded from the universal exit states; all are measured.
    /// The two are learned separately, so one move can't spoil another move's spark, and the stun is always this hit's real
    /// stun (counter hits, combo decay). Learning only uses CLEAN exchanges: if another hit lands, or the attacker cancels
    /// into another move before recovering, that measurement is thrown away. What was learned is saved to
    /// user://advantage_learned.cfg (delete it, or call ResetLearning, after big frame data changes).
    /// Visual only: nothing here changes the simulation (and a hit re-simulated by rollback doesn't spark twice).
    /// </summary>
    public static class Advantage
    {
        /// <summary>"By a lot" from this many frames on.</summary>
        public static int ALot = 4;
        /// <summary>Give up measuring after this many frames (e.g. a long knockdown).</summary>
        public static int MaxWait = 150;

        /// <summary>Universal blockstun states → their exit state (UniversalLayout: 41/42 stand, 46/47 crouch, 51/52 air).</summary>
        static readonly int[,] BlockExits = { { 41, 42 }, { 46, 47 }, { 51, 52 } };

        const string FilePath = "user://advantage_learned.cfg";

        /// <summary>New sparks on (all modes). In Training, Display → Advantage Hitsparks Off brings back the old hit effects.</summary>
        public static bool SparksOn => !Training.TrainingController.Active || Training.TrainingSettings.ShowAdvantage;

        class Pending
        {
            public SakugaActor Target;
            public string AttackerKey, ExitKey;
            public int Stun;
            public int AttackerState, AttackerStateFrame;
            public int Frames, AttackerFree = -1, DefenderFree = -1;
        }

        static readonly Dictionary<ulong, Pending> _pending = new();
        /// <summary>Frames until the attacker can act, per (character, move state, frame of it, move length).</summary>
        static readonly Dictionary<string, int> _attacker = new();
        /// <summary>Defender exit frames per (character, reaction state).</summary>
        static readonly Dictionary<string, int> _exit = new();
        static readonly HashSet<long> _sparked = new();
        static bool _loaded, _dirty;

        /// <summary>For tests / displays.</summary>
        public static int LastPredicted, LastMeasured, SparkCount, MeasureCount, DiscardCount;
        public static AdvantageBand LastBand;
        public static string LastDebug = "";

        static int GameFrame => SakugaEngine.Game.GameManager.Instance?.Frame ?? 0;

        public static AdvantageBand Band(int adv) =>
            adv >= ALot ? AdvantageBand.LotPlus : adv > 0 ? AdvantageBand.LittlePlus : adv == 0 ? AdvantageBand.Even :
            adv > -ALot ? AdvantageBand.LittleMinus : AdvantageBand.LotMinus;

        static int Remaining(SakugaActor a)
        {
            var st = a.StateManager.GetCurrentState();
            if (st?.AnimationData == null || a.StateManager.CurrentStateType() != SakugaEngine.Global.StateType.COMBAT) return 0;
            return Mathf.Max(0, st.AnimationData.Duration - a.StateManager.CurrentStateFrame);
        }

        /// <summary>The character, as a config-file-safe word (letters, digits, _).</summary>
        static string Who(SakugaActor a)
        {
            string raw = (a.Data?.ResourcePath ?? a.Name.ToString()).Replace("res://", "");
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        static int ExitFrames(SakugaActor target, int reactionState, string exitKey)
        {
            if (_exit.TryGetValue(exitKey, out int learned)) return learned;
            for (int i = 0; i < BlockExits.GetLength(0); i++)
            {
                if (BlockExits[i, 0] != reactionState) continue;
                try
                {
                    var exit = target.StateManager.GetState(BlockExits[i, 1]);
                    if (exit?.AnimationData != null) return exit.AnimationData.Duration;
                }
                catch (System.Exception) { }
            }
            return 0;
        }

        /// <summary>A hit or block just landed (SakugaActor hit code, [FF PATCH: advantage]). Spawns the spark.</summary>
        public static void OnContact(SakugaActor attacker, SakugaActor target, Vector2I contact, bool blocked)
        {
            if (attacker == null || target == null || attacker.StateManager == null || target.StateManager == null) return;
            Load();

            // Rollback can re-simulate the same hit: handle it once.
            long sparkId = ((long)GameFrame << 8) ^ (long)attacker.playerID;
            if (!_sparked.Add(sparkId)) return;
            if (_sparked.Count > 256) _sparked.Clear();

            // Another hit before an exchange finished: that exchange isn't a clean sample any more.
            if (_pending.Count > 0) { DiscardCount += _pending.Count; _pending.Clear(); }

            int stun = (int)(target.Hitstun?.TimeLeft ?? 0);
            int tState = target.StateManager.CurrentState;
            int remaining = Remaining(attacker);
            // The move's length is in the key, so changing its frame data doesn't reuse an old value.
            int length = attacker.StateManager.GetCurrentState()?.AnimationData?.Duration ?? 0;
            string attackerKey = $"{Who(attacker)}__s{attacker.StateManager.CurrentState}_f{attacker.StateManager.CurrentStateFrame}_l{length}";
            string exitKey = $"{Who(target)}__r{tState}";
            int exitFrames = ExitFrames(target, tState, exitKey);
            bool known = _attacker.TryGetValue(attackerKey, out int attackerFrames);
            if (!known) attackerFrames = remaining;
            int predicted = stun + exitFrames - attackerFrames;

            _pending[attacker.GetInstanceId()] = new Pending
            {
                Target = target, AttackerKey = attackerKey, ExitKey = exitKey, Stun = stun,
                AttackerState = attacker.StateManager.CurrentState, AttackerStateFrame = attacker.StateManager.CurrentStateFrame,
            };

            LastDebug = $"stun {stun} exit {exitFrames} attacker {attackerFrames} (measured before: {known})";
            LastPredicted = predicted;
            LastBand = Band(predicted);
            SparkCount++;
            if (SparksOn) AdvantageHUD.Spark(contact, predicted, attacker, blocked);
        }

        static bool Free(SakugaActor a) => Training.FrameMeter.Classify(a).Kind == Training.FrameKind.Free;

        /// <summary>Every frame after both fighters have moved (GameManager, next to TrainingController.AfterTick).</summary>
        public static void AfterTick(SakugaActor[] fighters)
        {
            if (fighters == null) return;
            foreach (var f in fighters) if (f != null) Tick(f);
            if (_dirty) Save();
        }

        static void Tick(SakugaActor attacker)
        {
            ulong id = attacker.GetInstanceId();
            if (!_pending.TryGetValue(id, out var p)) return;
            if (attacker.OnHitstop() || p.Target.OnHitstop()) return;

            // The attacker got hit, or cancelled into another move before its state finished: not a clean sample.
            // (A state that ended and chained into a recovery state is fine.)
            if (p.AttackerFree < 0)
            {
                var type = attacker.StateManager.CurrentStateType();
                bool hit = type == SakugaEngine.Global.StateType.HIT_REACTION || type == SakugaEngine.Global.StateType.BLOCKING;
                bool changed = attacker.StateManager.CurrentState != p.AttackerState;
                bool cancelled = false;
                if (changed && !Free(attacker))
                {
                    var prev = attacker.StateManager.GetState(p.AttackerState);
                    int prevLength = prev?.AnimationData?.Duration ?? 0;
                    cancelled = p.AttackerStateFrame < prevLength - 1;
                }
                if (hit || cancelled)
                {
                    _pending.Remove(id);
                    DiscardCount++;
                    return;
                }
                p.AttackerState = attacker.StateManager.CurrentState;
                p.AttackerStateFrame = attacker.StateManager.CurrentStateFrame;
            }

            p.Frames++;
            if (p.AttackerFree < 0 && Free(attacker)) p.AttackerFree = p.Frames;
            if (p.DefenderFree < 0 && Free(p.Target)) p.DefenderFree = p.Frames;
            if (p.AttackerFree >= 0 && p.DefenderFree >= 0)
            {
                _pending.Remove(id);
                int measured = p.DefenderFree - p.AttackerFree;
                Learn(_attacker, p.AttackerKey, p.AttackerFree);
                Learn(_exit, p.ExitKey, p.DefenderFree - p.Stun);
                LastMeasured = measured;
                MeasureCount++;
            }
            else if (p.Frames > MaxWait) _pending.Remove(id);
        }

        static void Learn(Dictionary<string, int> table, string key, int value)
        {
            if (table.TryGetValue(key, out int old) && old == value) return;
            table[key] = value;
            _dirty = true;
        }

        /// <summary>Forget pending measurements (save-state load, reset). Learned values are kept.</summary>
        public static void ClearPending() => _pending.Clear();

        /// <summary>A save state was loaded: the game frame went back, so hits on those frames may spark again.</summary>
        public static void OnStateLoaded() { _pending.Clear(); _sparked.Clear(); }

        /// <summary>Forget everything learned (tests; or after big frame data changes). Also deletes the saved file.</summary>
        public static void ResetLearning()
        {
            _attacker.Clear(); _exit.Clear(); _pending.Clear(); _sparked.Clear();
            _loaded = true; _dirty = false;
            if (FileAccess.FileExists(FilePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(FilePath));
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var cfg = new ConfigFile();
            if (cfg.Load(FilePath) != Error.Ok) return;
            foreach (string section in new[] { "attacker", "exit" })
            {
                if (!cfg.HasSection(section)) continue;
                var table = section == "attacker" ? _attacker : _exit;
                foreach (string k in cfg.GetSectionKeys(section)) table[k] = (int)cfg.GetValue(section, k);
            }
        }

        static void Save()
        {
            _dirty = false;
            var cfg = new ConfigFile();
            foreach (var kv in _attacker) cfg.SetValue("attacker", kv.Key, kv.Value);
            foreach (var kv in _exit) cfg.SetValue("exit", kv.Key, kv.Value);
            cfg.Save(FilePath);
        }
    }
}
