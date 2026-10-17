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
        // [FF PATCH: combo display] combo damage under the counter (BBCF style) and, next to INVALID, the hit number where
        // the combo became invalid (circle-slash icon + number).
        Label _damage;
        InvalidMark _mark;
        bool _rightSide;
        public static Color DamageColor = new Color(1f, 0.86f, 0.35f);
        public static int DamageFontSize = 30;
        /// <summary>Gap between the screen edge and the hit number / damage (P1's ComboCount offset; P2 mirrors it).</summary>
        public static float P1Inset = 22f;

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

            // Combo damage, under the stun bar, lined up with the hit number (left for P1, mirrored for P2).
            _rightSide = StunBar.AnchorLeft >= 1f;
            float barTop = StunBar.OffsetTop, barH = (StunBar.OffsetBottom - StunBar.OffsetTop) * StunBar.Scale.Y;
            float barW = (StunBar.OffsetRight - StunBar.OffsetLeft) * StunBar.Scale.X;
            _damage = new Label { Text = "0", MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = _rightSide ? HorizontalAlignment.Right : HorizontalAlignment.Left };
            _damage.AddThemeFontSizeOverride("font_size", DamageFontSize);
            _damage.AddThemeColorOverride("font_color", DamageColor);
            _damage.AddThemeConstantOverride("outline_size", 8);
            _damage.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
            AddChild(_damage);
            _damage.LayoutMode = 1;
            _damage.AnchorLeft = _damage.AnchorRight = StunBar.AnchorLeft;
            _damage.AnchorTop = _damage.AnchorBottom = StunBar.AnchorTop;
            float inset = _rightSide ? 0f : ComboCount.OffsetLeft; // P1: under the number; P2: same gap from the right edge
            if (_rightSide) { _damage.OffsetLeft = StunBar.OffsetLeft; _damage.OffsetRight = StunBar.OffsetLeft + barW - P1Inset; }
            else { _damage.OffsetLeft = StunBar.OffsetLeft + inset; _damage.OffsetRight = StunBar.OffsetLeft + barW; }
            _damage.OffsetTop = barTop + barH + 2; _damage.OffsetBottom = barTop + barH + 2 + DamageFontSize + 8;

            _mark = new InvalidMark { Visible = false, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_mark);
            if (comboText != null)
            {
                _invalid.LayoutMode = comboText.LayoutMode;
                _invalid.AnchorLeft = comboText.AnchorLeft; _invalid.AnchorRight = comboText.AnchorRight;
                _invalid.AnchorTop = comboText.AnchorTop; _invalid.AnchorBottom = comboText.AnchorBottom;
                _invalid.OffsetLeft = comboText.OffsetLeft; _invalid.OffsetRight = comboText.OffsetRight;
                _invalid.OffsetTop = comboText.OffsetTop + 34; _invalid.OffsetBottom = comboText.OffsetBottom + 34;
            }
        }

        /// <summary>Damage row: "1110  INVALID ⊘2" for P1, "INVALID ⊘2  1110" for P2 (always toward the middle of the screen).</summary>
        void LayoutInvalidRow()
        {
            float dmgW = _damage.GetMinimumSize().X;
            float rowH = _damage.Size.Y;
            float invW = _invalid.GetMinimumSize().X * _invalid.Scale.X;
            float invH = _invalid.GetMinimumSize().Y * _invalid.Scale.Y;
            float markW = _mark.Visible ? _mark.Size.X + 6 : 0f;
            float y = _damage.Position.Y;
            float x = _rightSide
                ? _damage.Position.X + _damage.Size.X - dmgW - 14 - invW - markW
                : _damage.Position.X + dmgW + 14;
            _invalid.Position = new Vector2(x, y + (rowH - invH) / 2);
            _mark.Position = new Vector2(x + invW + 6, y + (rowH - _mark.Size.Y) / 2);
        }

        public void UpdateCounter(int stunValue, CombatTracker tracker)
        {
            StunBar.TintProgress = tracker.invalidHit ? InvalidHitColor : DefaultColor;
            StunBar.Value = stunValue;
            ComboCount.Text = tracker.HitCombo.ToString();
            if (_damage != null) _damage.Text = tracker.CurrentCombo.ToString();
            if (_invalid != null)
            {
                bool invalid = tracker.invalidHit && tracker.HitCombo > 1;
                _invalid.Visible = invalid;
                if (invalid) ComboCount.AddThemeColorOverride("font_color", InvalidTextColor);
                else ComboCount.AddThemeColorOverride("font_color", _countColor);
                if (_mark != null)
                {
                    _mark.Visible = invalid && tracker.InvalidAtHit > 0;
                    if (_mark.Visible) _mark.Number = tracker.InvalidAtHit;
                }
                if (invalid && _damage != null) LayoutInvalidRow();
            }
            if (Star != null)
            {
                if (tracker.invalidHit) Star.Rotation -= inverseRotation ? -0.01f : 0.01f;
                else if (stunValue > 0)
                    Star.Rotation += (inverseRotation ? -0.03f : 0.03f) * ((float)tracker.HitCombo * 0.5f);
            }
        }
    }

    /// <summary>[FF PATCH: combo display] Circle-slash icon followed by a number ("⊘ 3"): the hit where the combo became invalid.</summary>
    public partial class InvalidMark : Control
    {
        int _number;
        public int Number { get => _number; set { if (_number == value) return; _number = value; QueueRedraw(); } }
        public static int FontSize = 26;
        public static float Radius = 11f;

        public override void _Ready()
        {
            Size = new Vector2(Radius * 2 + 6 + FontSize * 1.4f, Mathf.Max(Radius * 2 + 4, FontSize + 6));
        }

        public override void _Draw()
        {
            Color c = ComboCounter.InvalidTextColor;
            Color outline = new Color(0, 0, 0, 0.9f);
            var center = new Vector2(Radius + 2, Size.Y / 2);
            var d = new Vector2(Radius * 0.7f, -Radius * 0.7f);
            // dark outline first, then the red icon on top
            DrawArc(center, Radius, 0, Mathf.Tau, 32, outline, 6f, true);
            DrawLine(center - d, center + d, outline, 6f, true);
            DrawArc(center, Radius, 0, Mathf.Tau, 32, c, 3f, true);
            DrawLine(center - d, center + d, c, 3f, true);
            var font = ThemeDB.FallbackFont;
            var pos = new Vector2(Radius * 2 + 8, Size.Y / 2 + FontSize * 0.36f);
            DrawStringOutline(font, pos, _number.ToString(), HorizontalAlignment.Left, -1, FontSize, 6, outline);
            DrawString(font, pos, _number.ToString(), HorizontalAlignment.Left, -1, FontSize, c);
        }
    }
}
