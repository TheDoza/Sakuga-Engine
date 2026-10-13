using Godot;
using Godot.Collections;

namespace SakugaEngine.Utils
{
	public partial class InputRemapperWidget : Control
	{
		[Export] private InputRemapper Remapper;
		[Export] private string Prefix;
		[Export(PropertyHint.Enum, "Keyboard,Gamepad")] private int DeviceType = 0;
		[Export] private int deviceID;
		[Export] private InputRemapperButton[] Buttons;
		[Export] private Color DefaultColor;
		[Export] private Color SelectedColor;
		[Export] private Color RemappingColor;
		[Export] private Timer TimeFrame;
		[Export] private InputRemapperWidget WaitFor;

		private bool remapping = false;

		// [FF PATCH: disable buttons] While waiting for a new key, holding Right disables that button (no key at all).
		// A quick tap of Right still binds it like any other key.
		[Export] private float HoldToDisableSeconds = 0.6f;
		private bool holdingRight;
		private double holdTime;
		private InputEvent rightEvent;
		private Label titleLabel;
		private string titleText;

		private int Selected = 0;

		public bool IsRemapping => remapping;

		public override void _Ready()
		{
			titleLabel ??= GetNodeOrNull<Label>("VBoxContainer/Label");
			if (titleLabel != null && titleText == null) titleText = titleLabel.Text;
			for (int i = 0; i < Buttons.Length; i++)
			{
				UpdateText(i);
			}
			SetColors();
		}

        public override void _Process(double delta)
        {
			if (!Visible) return;
			
            base._Process(delta);
			if (remapping && holdingRight)
			{
				holdTime += delta;
				if (holdTime >= HoldToDisableSeconds) DisableSelected();
				return;
			}
			if (remapping && TimeFrame.TimeLeft <= 0)
			{
				AudioManager.Instance.PlayMenuClip(2);
				remapping = false;
				UpdateText(Selected);
				SetColors();
			}
        }

		public override void _UnhandledInput(InputEvent @event)
		{
			if (!Visible) return;
			if (WaitFor != null && WaitFor.IsRemapping) return;

			if (!remapping)
			{
				if (@event.IsActionPressed(Prefix + "up"))
				{
					Selected--;
					Selected = Mathf.Clamp(Selected, 0, Buttons.Length - 1);
					SetColors();
					AudioManager.Instance.PlayMenuClip(0);
				}
				if (@event.IsActionPressed(Prefix + "down"))
				{
					AudioManager.Instance.PlayMenuClip(0);
					Selected++;
					Selected = Mathf.Clamp(Selected, 0, Buttons.Length - 1);
					SetColors();
				}
				if (@event.IsActionPressed(Prefix + "accept") && Buttons[Selected].IsToggle)
				{
					// [FF PATCH: easy specials] option rows flip On/Off instead of waiting for a key
					AudioManager.Instance.PlayMenuClip(1);
					string key = Prefix + Buttons[Selected].ToggleKey;
					FunctionFighter.Input.PlayerOptions.SetBool(key, !FunctionFighter.Input.PlayerOptions.GetBool(key));
					UpdateText(Selected);
					return;
				}
				if (@event.IsActionPressed(Prefix + "accept"))
				{
					AudioManager.Instance.PlayMenuClip(1);
					//SetProcessUnhandledInput(true);
					Buttons[Selected].SetKey("Awaiting Input...");
					remapping = true;
					TimeFrame.Start();
					SetColors();
				}
				if (@event.IsActionPressed(Prefix + "return") || @event.IsActionPressed(Prefix + "options"))
				{
					AudioManager.Instance.PlayMenuClip(2);
					remapping = false;
					Visible = false;
				}
				if (@event.IsActionPressed(Prefix + "extra1") && !Buttons[Selected].IsToggle)
				{
					AudioManager.Instance.PlayMenuClip(2);
					ClearMapping();
				}
				if (@event.IsActionPressed(Prefix + "extra2"))
				{
					AudioManager.Instance.PlayMenuClip(2);
					Array<StringName> actions = new Array<StringName>();

					foreach(InputRemapperButton button in Buttons)
						if (!button.IsToggle) actions.Add(Prefix + button.GetAction());

					Remapper.ReturnToDefaultKeymaps(actions);

					for (int i = 0; i < Buttons.Length; i++)
					{
						UpdateText(i);
					}
				}
				return;
			}

			// [FF PATCH: disable buttons]
			if (CanDisable(Selected) && !holdingRight && @event.IsActionPressed(Prefix + "right") && !@event.IsEcho())
			{
				holdingRight = true;
				holdTime = 0;
				rightEvent = @event;
				Buttons[Selected].SetKey("Keep holding...");
				return;
			}
			if (holdingRight)
			{
				if (@event.IsActionReleased(Prefix + "right"))
				{
					// a quick tap: bind Right like any other key
					holdingRight = false;
					if (DeviceType == 0) RemapKeyboard(rightEvent); else RemapController(rightEvent, deviceID);
				}
				return;
			}

			switch (DeviceType)
			{
				case 0:
					RemapKeyboard(@event);
					break;
				case 1:
					RemapController(@event, deviceID);
					break;
			}
			
		}

		/// <summary>Buttons and macros can be disabled; directions and option rows can't.</summary>
		private bool CanDisable(int i)
		{
			if (Buttons[i].IsToggle) return false;
			string a = Buttons[i].GetAction();
			return a != "up" && a != "down" && a != "left" && a != "right";
		}

		private void DisableSelected()
		{
			string action = Prefix + Buttons[Selected].GetAction();
			InputMap.ActionEraseEvents(action);
			Remapper.Remap(action, new Array<InputEvent>());
			Remapper.SaveKeymap();
			AudioManager.Instance.PlayMenuClip(3);
			holdingRight = false;
			remapping = false;
			UpdateText(Selected);
			SetColors();
		}

		private void SetColors()
		{
			if (titleLabel != null && titleText != null)
				titleLabel.Text = remapping && CanDisable(Selected) ? "Hold Right: disable" : titleText;
			for (int i = 0; i < Buttons.Length; i++)
			{
				if (i == Selected)
					Buttons[i].SetColor(remapping ? RemappingColor : SelectedColor);
				else
					Buttons[i].SetColor(DefaultColor);
			}
		}

		private void RemapKeyboard(InputEvent @event)
		{
			if (@event is InputEventKey eventKey)
			{
				if (eventKey.Pressed)
				{
					InputMap.ActionEraseEvents(Prefix + Buttons[Selected].GetAction());
					InputMap.ActionAddEvent(Prefix + Buttons[Selected].GetAction(), eventKey);
					ReleaseFocus();
					UpdateText(Selected);
					Array<InputEvent> eventArray = new Array<InputEvent>();
					eventArray.Add(eventKey);
					Remapper.Remap(Prefix + Buttons[Selected].GetAction(), eventArray);
					Remapper.SaveKeymap();
					AudioManager.Instance.PlayMenuClip(3);
					remapping = false;
					SetColors();
				}
			}
		}

		private void RemapController(InputEvent @event, int deviceID)
		{
			if (@event.Device != deviceID) return;

			if (@event is InputEventJoypadButton eventPad)
			{
				if (eventPad.Pressed)
				{
					InputMap.ActionEraseEvents(Prefix + Buttons[Selected].GetAction());
					InputMap.ActionAddEvent(Prefix + Buttons[Selected].GetAction(), eventPad);
					ReleaseFocus();
					UpdateText(Selected);
					Array<InputEvent> eventArray = new Array<InputEvent>();
					eventArray.Add(eventPad);
					Remapper.Remap(Prefix + Buttons[Selected].GetAction(), eventArray);
					Remapper.SaveKeymap();
					AudioManager.Instance.PlayMenuClip(3);
					remapping = false;
					SetColors();
				}
			}
			else if (@event is InputEventJoypadMotion eventMotion)
			{
				if (Mathf.Abs(eventMotion.AxisValue) > 0.2f)
				{
					InputMap.ActionEraseEvents(Prefix + Buttons[Selected].GetAction());
					InputMap.ActionAddEvent(Prefix + Buttons[Selected].GetAction(), eventMotion);
					ReleaseFocus();
					UpdateText(Selected);
					Array<InputEvent> eventArray = new Array<InputEvent>();
					eventArray.Add(eventMotion);
					Remapper.Remap(Prefix + Buttons[Selected].GetAction(), eventArray);
					Remapper.SaveKeymap();
					AudioManager.Instance.PlayMenuClip(3);
					remapping = false;
					SetColors();
				}
			}
		}

		private void ClearMapping()
		{
			InputMap.ActionEraseEvents(Prefix + Buttons[Selected].GetAction());
			ReleaseFocus();
			Buttons[Selected].SetKey(" ");
			Remapper.Remap(Prefix + Buttons[Selected].GetAction(), new Array<InputEvent>()); // [FF PATCH: disable buttons] empty, not null
			Remapper.SaveKeymap();
		}

		private void UpdateText(int selected)
		{
			if (Buttons[selected].IsToggle)
			{
				bool on = FunctionFighter.Input.PlayerOptions.GetBool(Prefix + Buttons[selected].ToggleKey);
				Buttons[selected].SetKey(on ? "On" : "Off");
				return;
			}
			var events = InputMap.ActionGetEvents(Prefix + Buttons[selected].GetAction());
			if (events.Count == 0) { Buttons[selected].SetKey("Disabled"); return; }
			string actionStr = events[0].AsText();
			if (DeviceType > 0) actionStr = GetSimplifiedGamepadButton(actionStr);
			else actionStr = actionStr.Replace(" - Physical", ""); // [FF PATCH: disable buttons] shorter key names so they fit

			Buttons[selected].SetKey(actionStr);
		}

		private string GetSimplifiedGamepadButton(string action)
		{
			if (action.Contains("Joypad Button 0")) return "Button A";
			else if (action.Contains("Joypad Button 1 ")) return "Button B";
			else if (action.Contains("Joypad Button 2 ")) return "Button X";
			else if (action.Contains("Joypad Button 3 ")) return "Button Y";
			else if (action.Contains("Joypad Button 4 ")) return "Button Back";
			else if (action.Contains("Joypad Button 5 ")) return "Button Home";
			else if (action.Contains("Joypad Button 6 ")) return "Button Start";
			else if (action.Contains("Joypad Button 7 ")) return "L.TB. Button";
			else if (action.Contains("Joypad Button 8 ")) return "R.TB. Button";
			else if (action.Contains("Joypad Button 9 ")) return "L. Shoulder";
			else if (action.Contains("Joypad Button 10")) return "R. Shoulder";
			else if (action.Contains("Joypad Button 11")) return "D-Pad Up";
			else if (action.Contains("Joypad Button 12")) return "D-Pad Down";
			else if (action.Contains("Joypad Button 13")) return "D-Pad Left";
			else if (action.Contains("Joypad Button 14")) return "D-Pad Right";
			else if (action.Contains("Joypad Motion on Axis 1") && action.Contains("with Value -0")) return "L.TB. Up";
			else if (action.Contains("Joypad Motion on Axis 1") && action.Contains("with Value 0")) return "L.TB. Down";
			else if (action.Contains("Joypad Motion on Axis 0") && action.Contains("with Value -0")) return "L.TB. Left";
			else if (action.Contains("Joypad Motion on Axis 0") && action.Contains("with Value 0")) return "L.TB. Right";
			else if (action.Contains("Joypad Motion on Axis 3") && action.Contains("with Value -0")) return "R.TB. Up";
			else if (action.Contains("Joypad Motion on Axis 3") && action.Contains("with Value 0")) return "R.TB. Down";
			else if (action.Contains("Joypad Motion on Axis 2") && action.Contains("with Value -0")) return "R.TB. Left";
			else if (action.Contains("Joypad Motion on Axis 2") && action.Contains("with Value 0")) return "R.TB. Right";
			else if (action.Contains("Joypad Motion on Axis 4")) return "Left Trigger";
			else if (action.Contains("Joypad Motion on Axis 5")) return "Right Trigger";

			return "";
		}
	}
}
