using Godot;
using SakugaEngine.Global;
using System.Collections.Generic;

namespace SakugaEngine.UI
{
    /// <summary>
    /// Options screen built in code: resolution, fullscreen, and a volume slider per audio bus.
    /// Place this Control as a SIBLING of MainMenu (not a child), so hiding MainMenu doesn't hide it.
    /// </summary>
    public partial class OptionsMenu : Control
    {
        /// <summary>Screen to bring back when this closes (e.g. the MainMenu node).</summary>
        [Export] public Control ReturnTo;

        private OptionButton resolutionDropdown;
        private CheckBox fullscreenCheck;
        private Button backButton;
        private readonly Dictionary<string, HSlider> sliders = new Dictionary<string, HSlider>();
        private readonly Dictionary<string, Label> sliderLabels = new Dictionary<string, Label>();
        private bool refreshing;

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            BuildUI();
            Visible = false;
        }

        public override void _UnhandledInput(InputEvent e)
        {
            if (Visible && e.IsActionPressed("ui_cancel"))
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
        }

        public void GoTo()
        {
            Refresh();
            Visible = true;
            backButton.GrabFocus();
        }

        private void Close()
        {
            Visible = false;
            if (ReturnTo != null)
            {
                if (ReturnTo is MainMenu menu) menu.GoTo();
                else ReturnTo.Visible = true;
            }
        }

        private void BuildUI()
        {
            var background = new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f) };
            background.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(background);

            var center = new CenterContainer();
            center.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(center);

            var panel = new PanelContainer();
            center.AddChild(panel);

            var root = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
            root.AddThemeConstantOverride("separation", 12);
            panel.AddChild(root);

            root.AddChild(new Label { Text = "OPTIONS", HorizontalAlignment = HorizontalAlignment.Center });

            // Resolution
            var resRow = new HBoxContainer();
            resRow.AddChild(new Label { Text = "Resolution", CustomMinimumSize = new Vector2(200, 0) });
            resolutionDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            foreach (var r in GameSettings.Resolutions)
                resolutionDropdown.AddItem($"{r.X} x {r.Y}");
            resolutionDropdown.ItemSelected += index =>
            {
                if (refreshing) return;
                GameSettings.Instance.SetResolution((int)index);
            };
            resRow.AddChild(resolutionDropdown);
            root.AddChild(resRow);

            // Fullscreen
            fullscreenCheck = new CheckBox { Text = "Fullscreen" };
            fullscreenCheck.Toggled += on =>
            {
                if (refreshing) return;
                GameSettings.Instance.SetFullscreen(on);
            };
            root.AddChild(fullscreenCheck);

            // Volumes
            root.AddChild(new HSeparator());
            foreach (var bus in GameSettings.Buses)
                AddVolumeRow(root, bus);

            root.AddChild(new HSeparator());

            backButton = new Button { Text = "Back", CustomMinimumSize = new Vector2(0, 40) };
            backButton.Pressed += Close;
            root.AddChild(backButton);
        }

        private void AddVolumeRow(VBoxContainer root, string bus)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = bus + " Volume", CustomMinimumSize = new Vector2(200, 0) });

            var slider = new HSlider
            {
                MinValue = 0,
                MaxValue = 1,
                Step = 0.01,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(220, 0),
            };
            var label = new Label { CustomMinimumSize = new Vector2(56, 0), HorizontalAlignment = HorizontalAlignment.Right };

            slider.ValueChanged += value =>
            {
                label.Text = $"{Mathf.RoundToInt((float)value * 100f)}%";
                if (refreshing) return;
                GameSettings.Instance.SetBusVolume(bus, (float)value);
            };

            row.AddChild(slider);
            row.AddChild(label);
            root.AddChild(row);

            sliders[bus] = slider;
            sliderLabels[bus] = label;
        }

        /// <summary>Pull current values from GameSettings into the controls without writing them back.</summary>
        private void Refresh()
        {
            refreshing = true;
            var gs = GameSettings.Instance;
            if (gs != null)
            {
                resolutionDropdown.Selected = gs.ResolutionIndex;
                fullscreenCheck.ButtonPressed = gs.Fullscreen;
                foreach (var bus in GameSettings.Buses)
                {
                    float v = gs.GetBusVolume(bus);
                    sliders[bus].Value = v;
                    sliderLabels[bus].Text = $"{Mathf.RoundToInt(v * 100f)}%";
                }
            }
            refreshing = false;
        }
    }
}
