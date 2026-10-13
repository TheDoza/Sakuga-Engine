# Controls: Macro Buttons & Easy Specials

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** every fighter · **Rollback-safe:** Yes (macros are expanded inside the simulation, the same on both sides)

## What it does
The four extra buttons are **macros**. The input mapping menu shows them by what they press:

| Button | Presses | Used for |
|---|---|---|
| E | A+B | Barrier (with back) |
| F | B+C | Throw |
| G | A+B+C | (free for now) |
| H | A+B+C+D | Burst |

Holding a macro is exactly the same as holding those buttons, so it works with every move. The fighter never sees E–H themselves.

**Save State / Load State / Frame Advance / Step Frame** (all mapping menus; on gamepads right above Easy Specials, on keyboards the last four rows): bind a key or button to Training's save / load state and frame advance. Keyboard 1 defaults to F5 / F6 / F2 / F3; keyboard 2 and gamepads start Disabled until you bind them. They only do something in Training (not paused); other modes ignore them. They aren't game inputs, so binding one to a button that's also an attack does both.

**Easy Specials** (gamepad mapping menus, last row): an On/Off accessibility option that will let controller players do some special moves with the right stick. It's a **placeholder**: it saves, but nothing uses it yet, because each fighter will pick its own easy specials once special moves exist (read it with `PlayerOptions.EasySpecials("j1_")`).

**Remapping from the pause menu:** pause → **Controls** opens the same mapping menus as character select, one per human player (P1 left, P2 right). Each player closes theirs with Back; when both are closed the pause menu comes back. Not shown in online matches.

**Disabling a button:** pick a button or macro, press Accept, then **hold Right** (about half a second) while it's waiting for a key. It shows **Disabled** and that button does nothing until it's mapped again. A quick tap of Right still binds Right like any other key. Directions can't be disabled.

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/Input/ButtonMacros.tres` | **what each macro presses** (tick buttons in the Inspector) and an on/off switch |
| `Scripts/FunctionFighter/Input/ButtonMacroSettings.cs` | the settings resource + `ButtonMacros.Expand` |
| `Scripts/FunctionFighter/Input/PlayerOptions.cs` | per-controller options (Easy Specials), saved to `user://player_options.cfg` |
| `Scenes/Instances/input_remapper.tscn` | the mapping menus: E–H labels, Save State / Load State rows, Easy Specials row |
| `project.godot` → Input Map | `k1_ / k2_ / j1_ / j2_` + `save_state` / `load_state` / `frame_advance` / `frame_step` actions |
| `Scripts/FunctionFighter/UI/PauseControls.cs` | the pause menu **Controls** page (node `PauseControls` in `Sakuga_UI.tscn`) and the shared pause-button helper |

## Engine patches
| Patch | File | What it changes |
|---|---|---|
| macros | `GameManager.cs` → `GameLoop` | every player's input goes through `ButtonMacros.Expand` |
| disable buttons | `InputRemapperWidget.cs`, `InputRemapper.cs` | hold Right to disable; disabled buttons save as "no keys" instead of crashing on load; shorter key names |
| pause controls / pause pages | `InputRemapper.cs`, `GameManager.cs` | the remap scene can be reused in the pause menu; the pause key is ignored while a pause page is open |
| easy specials | `InputRemapperButton.cs`, `InputRemapperWidget.cs` | a row can be an On/Off option instead of a key (`IsToggle`, `ToggleKey`); option rows are skipped by Clear/Defaults; long row names (over 8 letters) use a smaller font |

## Tuning
| I want to… | Where |
|---|---|
| Change what a macro presses | `ButtonMacros.tres` → `MacroE`…`MacroH` (and the `Title` of that row in `input_remapper.tscn`) |
| Back to plain E–H buttons | `ButtonMacros.tres` → `Enabled` off |

## Test result
E + back → barrier; F → throw; H in hitstun → Burst; G gives A+B+C and no E–H bit reaches the fighter (2026-10-12).

## Change log
| Date | Change |
|---|---|
| 2026-10-12 | Frame Advance / Step Frame rows |
| 2026-10-12 | Save State / Load State rows (bindable, used by Training) |
| 2026-10-12 | Controls page in the pause menu; hold Right to disable a button |
| 2026-10-12 | Created: E–H macros, Easy Specials placeholder |
