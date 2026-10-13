using Godot;
using SakugaEngine;

namespace SakugaEngine.UI
{
    public partial class ComboCounter : Control
    {
        [Export] private Color InvalidHitColor = new Color(0.85f, 0.15f, 0.15f); // [FF PATCH: invalid combo] was black
        Color DefaultColor;
        public TextureProgressBar StunBar;
        public Label ComboCount;
        [Export] private TextureRect Star;
        [Export] private bool inverseRotation;
        // [FF PATCH: invalid combo] "INVALID" under the hit counter (BBCF style) when the combo could have been teched out of
        Label _invalid;
        Color _countColor = Colors.White;
        public static Color InvalidTextColor = new Color(1f, 0.25f, 0.25f);

        public override void _Ready()
        {
            StunBar = GetNode<TextureProgressBar>("StunBar");
            ComboCount = GetNode<Label>("ComboCount");
            DefaultColor = StunBar.TintProgress;
            _countColor = ComboCount.GetThemeColor("font_color");
            var comboText = GetNodeOrNull<Label>("ComboText");
            _invalid = new Label { Text = "INVALID", Visible = false, MouseFilter = MouseFilterEnum.Ignore };
            _invalid.AddThemeColorOverride("font_color", InvalidTextColor);
            _invalid.AddThemeConstantOverride("outline_size", 6);
            _invalid.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
            _invalid.Scale = new Vector2(1.5f, 1.5f);
            AddChild(_invalid);
            if (comboText != null)
            {
                _invalid.LayoutMode = comboText.LayoutMode;
                _invalid.AnchorLeft = comboText.AnchorLeft; _invalid.AnchorRight = comboText.AnchorRight;
                _invalid.AnchorTop = comboText.AnchorTop; _invalid.AnchorBottom = comboText.AnchorBottom;
                _invalid.OffsetLeft = comboText.OffsetLeft; _invalid.OffsetRight = comboText.OffsetRight;
                _invalid.OffsetTop = comboText.OffsetTop + 34; _invalid.OffsetBottom = comboText.OffsetBottom + 34;
            }
        }

        public void UpdateCounter(int stunValue, CombatTracker tracker)
        {
            StunBar.TintProgress = tracker.invalidHit ? InvalidHitColor : DefaultColor;
            StunBar.Value = stunValue;
            ComboCount.Text = tracker.HitCombo.ToString();
            if (_invalid != null)
            {
                bool invalid = tracker.invalidHit && tracker.HitCombo > 1;
                _invalid.Visible = invalid;
                if (invalid) ComboCount.AddThemeColorOverride("font_color", InvalidTextColor);
                else ComboCount.AddThemeColorOverride("font_color", _countColor);
            }
            if (Star != null)
            {
                if (tracker.invalidHit) Star.Rotation -= inverseRotation ? -0.01f : 0.01f;
                else if (stunValue > 0)
                    Star.Rotation += (inverseRotation ? -0.03f : 0.03f) * ((float)tracker.HitCombo * 0.5f);
            }
        }
    }
}
