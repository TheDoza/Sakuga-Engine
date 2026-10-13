using Godot;
using SakugaEngine.Game;
using SakugaEngine.UI;
using SakugaEngine.Utils;

namespace FunctionFighter.UI
{
    /// <summary>
    /// Shared helpers for the extra pause-menu pages (Training, Controls).
    /// </summary>
    public static class PauseOverlay
    {
        /// <summary>True while a pause sub-page is open: GameManager then ignores the pause key ([FF PATCH: pause pages]),
        /// so Enter / Start can be used inside the page (or bound as a key) without unpausing.</summary>
        public static bool Blocking;

        /// <summary>Adds a button to the pause menu's row and re-spaces the row so every button fits.</summary>
        public static Button AddButton(PauseMenu pause, string text, int index)
        {
            var box = pause?.GetNodeOrNull<HBoxContainer>("HBoxContainer");
            if (box == null) return null;
            var button = new Button { Text = text };
            box.AddChild(button);
            box.MoveChild(button, Mathf.Clamp(index, 0, box.GetChildCount() - 1));
            int count = 0;
            foreach (var child in box.GetChildren())
                if (child is Button b) { b.CustomMinimumSize = new Vector2(124, 0); count++; }
            float half = count * 128 / 2f;
            box.OffsetLeft = -half; box.OffsetRight = half;
            pause.AnchorLeft = 0.5f - (half + 20) / 1280f;
            pause.AnchorRight = 0.5f + (half + 20) / 1280f;
            pause.OffsetLeft = 0; pause.OffsetRight = 0;
            return button;
        }
    }

    /// <summary>
    /// "Controls" button in the pause menu (every offline mode): opens the same input mapping menus as character
    /// select, one per human player, side by side. Each player closes theirs with Back / Return; when both are closed
    /// the pause menu comes back. Node "PauseControls" in Sakuga_UI.tscn.
    /// </summary>
    public partial class PauseControls : Control
    {
        [Export] public string RemapperScene = "res://Scenes/Instances/input_remapper.tscn";
        [Export] public Vector2 P1Position = new Vector2(40, 60);
        [Export] public Vector2 P2Position = new Vector2(974, 60);

        InputRemapper _remapper;
        PauseMenu _pause;
        Button _button;
        bool _added, _open;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            PauseOverlay.Blocking = false;
            var scene = GD.Load<PackedScene>(RemapperScene);
            if (scene == null) return;
            _remapper = scene.Instantiate<InputRemapper>();
            AddChild(_remapper);
            var p1 = new Control { Position = P1Position, MouseFilter = MouseFilterEnum.Ignore };
            var p2 = new Control { Position = P2Position, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(p1); AddChild(p2);
            _remapper.SetPivots(p1, p2);
        }

        public override void _Process(double delta)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.GetFighters() == null || _remapper == null) return;

            if (!_added)
            {
                _added = true;
                if (gm.Match.SelectedModeSettings.UseLAN) return; // not during online matches
                _pause = GetParent().GetNodeOrNull<PauseMenu>("PauseMenu");
                _button = PauseOverlay.AddButton(_pause, "Controls", 2);
                if (_button != null) _button.Pressed += Open;
            }

            if (!_open) return;
            if (!gm.IsPaused) { CloseAll(); return; }
            if (!_remapper.IsP1Remapping && !_remapper.IsP2Remapping)
            {
                _open = false;
                PauseOverlay.Blocking = false;
                _pause.CallPauseMenu(true);
                _button?.GrabFocus();
            }
        }

        void Open()
        {
            var gm = GameManager.Instance;
            if (_pause == null || !_pause.Visible || gm == null) return;
            int d1 = gm.Match.P1SelectedDevice, d2 = gm.Match.P2SelectedDevice;
            if (d1 < 0 && d2 < 0) return;
            _open = true;
            PauseOverlay.Blocking = true;
            _pause.Visible = false;
            GetViewport().GuiReleaseFocus();
            if (d1 >= 0) _remapper.ToggleInputMapperP1(d1);
            if (d2 >= 0 && d2 != d1) _remapper.ToggleInputMapperP2(d2);
        }

        void CloseAll()
        {
            _open = false;
            PauseOverlay.Blocking = false;
            _remapper.HideAll();
        }
    }
}
