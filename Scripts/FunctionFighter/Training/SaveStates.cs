using Godot;
using SakugaEngine.Game;
using FunctionFighter.Mechanics;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Training save state: one slot holding the whole match (both fighters, positions, states, meters, timers),
    /// using the same snapshot rollback uses (GameManager.SaveState / LoadState), so anything rollback restores is
    /// restored here too. Save a situation (a knockdown, a corner, a meaty) and load it to try it again.
    ///
    /// Save State / Load State buttons (bindable in the button config; keyboard 1 defaults F5 / F6), or the RESET page
    /// of the Training menu.
    /// Called between frames (from TrainingMenu input), never during the simulation step.
    /// The slot is emptied when a Training match starts (a new character select = a new match).
    /// </summary>
    public static class SaveStates
    {
        public static Color NoticeColor = new Color(0.75f, 0.6f, 1f);
        /// <summary>How long the "STATE SAVED / LOADED" notice shows, in milliseconds (real time, so it shows while paused).</summary>
        public static int NoticeMs = 900;

        static byte[] _slot;
        public static bool HasState => _slot != null;
        public static string Notice = "";
        static ulong _noticeUntil;
        public static float NoticeAlpha
        {
            get
            {
                ulong now = Time.GetTicksMsec();
                if (now >= _noticeUntil) return 0f;
                return Mathf.Clamp((_noticeUntil - now) / 250f, 0f, 1f);
            }
        }

        static void Show(string text)
        {
            Notice = text;
            _noticeUntil = Time.GetTicksMsec() + (ulong)NoticeMs;
        }

        public static void Clear() { _slot = null; _noticeUntil = 0; }

        public static bool Save()
        {
            var gm = GameManager.Instance;
            if (gm == null || !TrainingController.Active) return false;
            var data = gm.SaveState();
            _slot = (byte[])data.Clone();
            Show("STATE SAVED");
            return true;
        }

        public static bool Load()
        {
            var gm = GameManager.Instance;
            if (gm == null || !TrainingController.Active) return false;
            if (_slot == null) { Show("NO STATE SAVED"); return false; }
            gm.LoadState((byte[])_slot.Clone());
            // Training-side helpers that aren't in the match snapshot start fresh.
            TrainingController.OnStateLoaded();
            Reversal.Reset();
            DummyMovement.Reset();
            FrameMeter.Clear();
            Advantage.OnStateLoaded();
            Show("STATE LOADED");
            return true;
        }
    }
}
