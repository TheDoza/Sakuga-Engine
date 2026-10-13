using Godot;

namespace FunctionFighter.Input
{
    /// <summary>
    /// Per-controller options set in the input mapping menu, saved to user://player_options.cfg.
    /// Keys are the remap widget's prefix + option name, e.g. "j1_easy_specials".
    /// </summary>
    public static class PlayerOptions
    {
        const string FilePath = "user://player_options.cfg";
        const string Section = "options";
        static ConfigFile _cfg;

        static ConfigFile Cfg
        {
            get
            {
                if (_cfg != null) return _cfg;
                _cfg = new ConfigFile();
                _cfg.Load(FilePath); // missing file = all defaults
                return _cfg;
            }
        }

        public static bool GetBool(string key, bool fallback = false) => (bool)Cfg.GetValue(Section, key, fallback);

        public static void SetBool(string key, bool value)
        {
            Cfg.SetValue(Section, key, value);
            Cfg.Save(FilePath);
        }

        /// <summary>
        /// Easy Specials (accessibility, placeholder): lets a controller player do certain special moves with the right stick.
        /// Nothing reads this yet: each fighter will pick which specials go on the stick once special moves exist.
        /// </summary>
        public static bool EasySpecials(string prefix) => GetBool(prefix + "easy_specials");
    }
}
