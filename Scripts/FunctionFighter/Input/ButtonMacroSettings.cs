using Godot;
using SakugaEngine.Global;

namespace FunctionFighter.Input
{
    /// <summary>
    /// What the four extra buttons (E, F, G, H) press. Each one is a macro: holding it is the same as holding
    /// every button ticked here. One shared file: res://Fighters/Shared/Input/ButtonMacros.tres.
    /// The remap menus show these as "A+B", "B+C", ... (see InputRemapperButton titles in input_remapper.tscn).
    /// </summary>
    [GlobalClass]
    public partial class ButtonMacroSettings : Resource
    {
        public const string SettingsPath = "res://Fighters/Shared/Input/ButtonMacros.tres";

        /// <summary>Turn the macros off to get Sakuga's plain E-H buttons back.</summary>
        [Export] public bool Enabled = true;
        /// <summary>Button E presses these. Default A+B (barrier).</summary>
        [Export] public ButtonInputs MacroE = ButtonInputs.FACE_A | ButtonInputs.FACE_B;
        /// <summary>Button F presses these. Default B+C (throw).</summary>
        [Export] public ButtonInputs MacroF = ButtonInputs.FACE_B | ButtonInputs.FACE_C;
        /// <summary>Button G presses these. Default A+B+C.</summary>
        [Export] public ButtonInputs MacroG = ButtonInputs.FACE_A | ButtonInputs.FACE_B | ButtonInputs.FACE_C;
        /// <summary>Button H presses these. Default A+B+C+D (Burst).</summary>
        [Export] public ButtonInputs MacroH = ButtonInputs.FACE_A | ButtonInputs.FACE_B | ButtonInputs.FACE_C | ButtonInputs.FACE_D;

        private static ButtonMacroSettings _cached;

        public static ButtonMacroSettings Get()
        {
            if (_cached != null) return _cached;
            if (ResourceLoader.Exists(SettingsPath)) _cached = ResourceLoader.Load<ButtonMacroSettings>(SettingsPath);
            if (_cached == null)
            {
                GD.PushWarning("[Macros] " + SettingsPath + " not found, using default values.");
                _cached = new ButtonMacroSettings();
            }
            return _cached;
        }
    }

    /// <summary>
    /// Turns the E-H macro buttons into the buttons they stand for. Runs on every player's input each simulated
    /// frame ([FF PATCH: macros] in GameManager.GameLoop), so it's the same on both sides of an online match.
    /// </summary>
    public static class ButtonMacros
    {
        public static PlayerInputs Expand(PlayerInputs input)
        {
            var s = ButtonMacroSettings.Get();
            if (!s.Enabled) return input;

            ButtonInputs extra = 0;
            if ((input & PlayerInputs.FACE_E) != 0) extra |= s.MacroE;
            if ((input & PlayerInputs.FACE_F) != 0) extra |= s.MacroF;
            if ((input & PlayerInputs.FACE_G) != 0) extra |= s.MacroG;
            if ((input & PlayerInputs.FACE_H) != 0) extra |= s.MacroH;

            input &= ~(PlayerInputs.FACE_E | PlayerInputs.FACE_F | PlayerInputs.FACE_G | PlayerInputs.FACE_H);
            return input | ToPlayerInputs(extra);
        }

        /// <summary>ButtonInputs (A=1, B=2, ...) to PlayerInputs (A=16, B=32, ...).</summary>
        public static PlayerInputs ToPlayerInputs(ButtonInputs b) => (PlayerInputs)((int)b << 4);
    }
}
