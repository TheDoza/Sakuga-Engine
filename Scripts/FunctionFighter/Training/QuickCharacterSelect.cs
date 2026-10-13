using Godot;
using SakugaEngine.Game;
using SakugaEngine.UI;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Quick Character Select: a row at the top of the Training pause menu, above the buttons.
    /// Press Up to reach it, Left/Right picks the character for P1 or the Dummy, Accept reloads Training with that pair.
    /// Training settings (and the recording) stay as they are. Press Down to go back to the buttons.
    /// Created by TrainingMenu when the pause menu is set up (Training only, never online).
    /// </summary>
    public partial class QuickCharacterSelect : Control
    {
        public static int FontSize = 16;
        public static Color PendingColor = new Color(0.9f, 0.58f, 0f);
        public static Color HintColor = new Color(1f, 1f, 1f, 0.6f);

        PauseMenu _pause;
        Selector _p1, _p2;
        Label _hint;

        /// <summary>The character each side will be after Accept (index in the roster).</summary>
        public int PendingP1 { get; private set; }
        public int PendingP2 { get; private set; }

        int RosterCount => GameManager.Instance?.fightersList?.elements?.Length ?? 0;

        public static QuickCharacterSelect Attach(PauseMenu pause)
        {
            var gm = GameManager.Instance;
            if (pause == null || gm == null || gm.fightersList == null || gm.Match == null) return null;
            var q = new QuickCharacterSelect { Name = "QuickCharacterSelect", _pause = pause };
            pause.AddChild(q);
            q.Build();
            return q;
        }

        void Build()
        {
            var m = GameManager.Instance.Match;
            PendingP1 = m.P1SelectedCharacter;
            PendingP2 = m.P2SelectedCharacter;

            // Full width of the pause panel, between the PAUSED title and the button row.
            AnchorLeft = 0; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 0;
            OffsetLeft = 0; OffsetRight = 0; OffsetTop = 46; OffsetBottom = 108;
            MouseFilter = MouseFilterEnum.Ignore;

            var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 24);
            row.SetAnchorsPreset(LayoutPreset.TopWide);
            row.OffsetBottom = 38;
            AddChild(row);

            _p1 = new Selector { Owner_ = this, Side = 0, CustomMinimumSize = new Vector2(280, 38) };
            _p2 = new Selector { Owner_ = this, Side = 1, CustomMinimumSize = new Vector2(280, 38) };
            foreach (var s in new[] { _p1, _p2 })
            {
                s.AddThemeFontSizeOverride("font_size", FontSize);
                s.Pressed += Apply;
                row.AddChild(s);
            }

            _hint = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            _hint.AddThemeFontSizeOverride("font_size", 13);
            _hint.SetAnchorsPreset(LayoutPreset.TopWide);
            _hint.OffsetTop = 40; _hint.OffsetBottom = 60;
            AddChild(_hint);

            Refresh();
        }

        public override void _Process(double delta)
        {
            if (_pause == null || !_pause.Visible) return;
            LinkFocus();
            bool focused = _p1.HasFocus() || _p2.HasFocus();
            _hint.Visible = focused || Changed;
        }

        bool Changed
        {
            get
            {
                var m = GameManager.Instance?.Match;
                return m != null && (PendingP1 != m.P1SelectedCharacter || PendingP2 != m.P2SelectedCharacter);
            }
        }

        /// <summary>Up from any pause button reaches the row (left half → P1, right half → Dummy); Down goes back.</summary>
        void LinkFocus()
        {
            var box = _pause.GetNodeOrNull<HBoxContainer>("HBoxContainer");
            if (box == null) return;
            Button first = null, last = null;
            int count = 0;
            foreach (var c in box.GetChildren()) if (c is Button) count++;
            int i = 0;
            foreach (var c in box.GetChildren())
            {
                if (c is not Button b) continue;
                first ??= b; last = b;
                var target = i < (count + 1) / 2 ? _p1 : _p2;
                b.FocusNeighborTop = b.GetPathTo(target);
                i++;
            }
            if (first == null) return;
            _p1.FocusNeighborBottom = _p1.GetPathTo(first);
            _p2.FocusNeighborBottom = _p2.GetPathTo(last);
            _p1.FocusNeighborTop = _p1.GetPathTo(_p1);
            _p2.FocusNeighborTop = _p2.GetPathTo(_p2);
        }

        /// <summary>Left/Right on a selector: step through the roster for that side.</summary>
        public void Step(int side, int dir)
        {
            int n = RosterCount;
            if (n <= 0) return;
            if (side == 0) PendingP1 = ((PendingP1 + dir) % n + n) % n;
            else PendingP2 = ((PendingP2 + dir) % n + n) % n;
            Refresh();
        }

        string NameOf(int index)
        {
            var list = GameManager.Instance?.fightersList?.elements;
            if (list == null || index < 0 || index >= list.Length) return "?";
            var p = list[index].Profile;
            return p == null ? $"#{index}" : (string.IsNullOrEmpty(p.FighterName) ? p.ShortName : p.FighterName);
        }

        void Refresh()
        {
            var m = GameManager.Instance.Match;
            bool many = RosterCount > 1;
            string arrowL = many ? "<   " : "", arrowR = many ? "   >" : "";
            _p1.Text = $"P1:   {arrowL}{NameOf(PendingP1)}{arrowR}";
            _p2.Text = $"Dummy:   {arrowL}{NameOf(PendingP2)}{arrowR}";
            Tint(_p1, PendingP1 != m.P1SelectedCharacter);
            Tint(_p2, PendingP2 != m.P2SelectedCharacter);
            _hint.Text = Changed ? "Accept: load Training with these characters" : "Left / Right: change character    Accept: reload";
            _hint.AddThemeColorOverride("font_color", Changed ? PendingColor : HintColor);
        }

        static void Tint(Button b, bool pending)
        {
            if (pending)
            {
                b.AddThemeColorOverride("font_color", PendingColor);
                b.AddThemeColorOverride("font_focus_color", PendingColor);
                b.AddThemeColorOverride("font_hover_color", PendingColor);
            }
            else
            {
                b.RemoveThemeColorOverride("font_color");
                b.RemoveThemeColorOverride("font_focus_color");
                b.RemoveThemeColorOverride("font_hover_color");
            }
        }

        /// <summary>Writes the picked pair into the match settings. Returns false if nothing is valid.</summary>
        public bool Commit()
        {
            var gm = GameManager.Instance;
            var list = gm?.fightersList?.elements;
            if (list == null || PendingP1 >= list.Length || PendingP2 >= list.Length) return false;
            var m = gm.Match;
            m.P1SelectedCharacter = PendingP1;
            m.P2SelectedCharacter = PendingP2;
            if (m.P1SelectedColor >= list[PendingP1].ColorPalettes.Length) m.P1SelectedColor = 0;
            if (m.P2SelectedColor >= list[PendingP2].ColorPalettes.Length) m.P2SelectedColor = 0;
            TrainingSettings.Save();
            return true;
        }

        /// <summary>Accept: reload Training with the picked pair (same scene, settings kept).</summary>
        void Apply()
        {
            if (_pause == null || !_pause.Visible) return;
            if (!Commit()) return;
            var gm = GameManager.Instance;
            var list = gm.fightersList.elements;
            string scene = GetTree().CurrentScene?.SceneFilePath;
            if (string.IsNullOrEmpty(scene)) scene = "res://Scenes/TestScene.tscn";
            FunctionFighter.UI.PauseOverlay.Blocking = false;
            _pause.Visible = false;
            LoadingScreenManager.Instance.LoadMatch(scene, list[PendingP1].Profile, list[PendingP2].Profile);
        }

        /// <summary>A pause-menu button that uses Left/Right to change its value instead of moving focus.</summary>
        partial class Selector : Button
        {
            public QuickCharacterSelect Owner_;
            public int Side;

            public override void _GuiInput(InputEvent e)
            {
                int dir = 0;
                if (e.IsActionPressed("ui_left") || (InputMap.HasAction("general_left") && e.IsActionPressed("general_left"))) dir = -1;
                else if (e.IsActionPressed("ui_right") || (InputMap.HasAction("general_right") && e.IsActionPressed("general_right"))) dir = 1;
                if (dir == 0) return;
                Owner_.Step(Side, dir);
                AcceptEvent();
            }
        }
    }
}
