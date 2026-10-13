using Godot;
using System;

public partial class InputRemapperButton : HBoxContainer
{
	[Export] private string Title;
	[Export] private string Action;
	[Export] private Label TitleLabel;
	[Export] private ColorRect Field;
	[Export] private Label KeyLabel;
	// [FF PATCH: easy specials] An On/Off option row instead of a key binding (accept flips it).
	[Export] public bool IsToggle;
	/// <summary>Option name saved in PlayerOptions (prefixed with the widget's Prefix), e.g. "easy_specials".</summary>
	[Export] public string ToggleKey;

    public override void _Ready()
    {
        base._Ready();
		TitleLabel.Text = Title;
		// [FF PATCH: easy specials / save states] longer names (Easy Specials, Save State...) use a smaller font to keep the row width
		if (IsToggle || (Title != null && Title.Length > 8)) TitleLabel.AddThemeFontSizeOverride("font_size", 15);
    }


	// Called when the node enters the scene tree for the first time.
	public void SetColor(Color color)
	{
		Field.Color = color;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void SetKey(string text)
	{
		KeyLabel.Text = text;
	}

	public string GetAction() => Action;
}
