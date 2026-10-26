using Godot;
using System.Collections.Generic;

namespace SakugaEngine.Global
{
    /// <summary>
    /// Player-facing settings: window resolution, fullscreen, and per-bus volume.
    /// Saved to user://settings.cfg. Lives outside the simulation, so it has no effect on rollback determinism.
    /// Register as an autoload named "GameSettings" (Project Settings > Globals > Autoload).
    /// </summary>
    public partial class GameSettings : Node
    {
        public const string SavePath = "user://settings.cfg";

        /// <summary>Resolutions offered in the options menu. The game's viewport stays 1280x720; the window scales it (canvas_items / expand).</summary>
        public static readonly Vector2I[] Resolutions =
        {
            new Vector2I(1280, 720),
            new Vector2I(1600, 900),
            new Vector2I(1920, 1080),
            new Vector2I(2560, 1440),
        };

        /// <summary>Audio buses that get a volume slider. Must match the bus names in Bus_Master.tres.</summary>
        public static readonly string[] Buses = { "Master", "BGM", "SFX", "Voice", "Announcer" };

        public static GameSettings Instance { get; private set; }

        public int ResolutionIndex { get; private set; } = 0;
        public bool Fullscreen { get; private set; } = false;

        private readonly Dictionary<string, float> busVolumes = new Dictionary<string, float>();

        public override void _Ready()
        {
            Instance = this;
            foreach (var bus in Buses) busVolumes[bus] = 1f;
            Load();
            ApplyAll();
        }

        public float GetBusVolume(string bus) => busVolumes.TryGetValue(bus, out float v) ? v : 1f;

        public void SetResolution(int index)
        {
            if (index < 0 || index >= Resolutions.Length) return;
            ResolutionIndex = index;
            ApplyWindow();
            Save();
        }

        public void SetFullscreen(bool on)
        {
            Fullscreen = on;
            ApplyWindow();
            Save();
        }

        /// <summary>linear: 0..1 slider value.</summary>
        public void SetBusVolume(string bus, float linear)
        {
            if (!busVolumes.ContainsKey(bus)) return;
            busVolumes[bus] = Mathf.Clamp(linear, 0f, 1f);
            ApplyBus(bus);
            Save();
        }

        public void ApplyAll()
        {
            ApplyWindow();
            foreach (var bus in Buses) ApplyBus(bus);
        }

        private void ApplyWindow()
        {
            // Fullscreen takes over the screen; windowed mode uses the chosen resolution and centers it.
            if (Fullscreen)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                return;
            }

            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            Vector2I size = Resolutions[ResolutionIndex];
            DisplayServer.WindowSetSize(size);

            Vector2I screen = DisplayServer.ScreenGetSize();
            DisplayServer.WindowSetPosition((screen - size) / 2);
        }

        private void ApplyBus(string bus)
        {
            int idx = AudioServer.GetBusIndex(bus);
            if (idx < 0) return;
            float linear = Mathf.Max(GetBusVolume(bus), 0.0001f); // avoid -inf dB; 0 slider is effectively silent
            AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(linear));
        }

        private void Load()
        {
            var cfg = new ConfigFile();
            if (cfg.Load(SavePath) != Error.Ok) return;

            ResolutionIndex = Mathf.Clamp((int)cfg.GetValue("video", "resolution", 0).AsInt32(), 0, Resolutions.Length - 1);
            Fullscreen = cfg.GetValue("video", "fullscreen", false).AsBool();

            foreach (var bus in Buses)
                busVolumes[bus] = Mathf.Clamp((float)cfg.GetValue("audio", bus, 1.0f).AsDouble(), 0f, 1f);
        }

        private void Save()
        {
            var cfg = new ConfigFile();
            cfg.SetValue("video", "resolution", ResolutionIndex);
            cfg.SetValue("video", "fullscreen", Fullscreen);
            foreach (var bus in Buses)
                cfg.SetValue("audio", bus, busVolumes[bus]);
            cfg.Save(SavePath);
        }
    }
}
