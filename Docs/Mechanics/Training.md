# Training Mode

**Status:** ✅ Complete (tested 2026-10-12) · **Fighters:** any (player 2 is the dummy) · **Rollback-safe:** n/a (Training is offline; the settings only change inputs and meters)

**Contents:** [Opening the menu](#opening-the-menu) · [Controls](#controls-quick-reference) · [The dummy](#the-dummy) · [Knockdowns, techs & wakeup](#knockdowns-techs--wakeup) · [Reversals](#reversals) · [Recording & playback](#recording--playback) · [Health & meters](#health--meters) · [Positions, resets & save states](#positions-resets--save-states) · [Frame data tools](#frame-data-tools) · [Other displays](#other-displays--indicators) · [Quick Character Select](#quick-character-select) · [Reference](#reference)

---

## Opening the menu
In Training, the pause menu has a **Training** button that opens **Training Settings**, split into 7 pages:

**Dummy Settings · Guard & Tech · Reversal Options · Health & Meters · Display · Recording · Reset**

- The page name is at the top: highlight it and press Left/Right, or switch pages from anywhere with **LB/RB** (gamepad) or **PageUp/PageDown**.
- Up/Down picks a row, Left/Right changes it, Accept runs an action, Back returns. Long pages scroll ("more above / below" shows when there is).
- To the right of the pause menu, the **Reset & Reposition** card shows every Back + direction reset on a little stage (red 1 = P1, blue 2 = P2).
- Overlays (frame meter, input display, status text, hitsparks) draw behind the pause menu and the settings pages.
- Settings are saved to `user://training_settings.cfg` and come back next time. **Restore Default Settings** (Reset page) puts them all back.

## Controls (quick reference)
| Action | Keyboard 1 default | Where to change / find it |
|---|---|---|
| Record dummy / stop | F8 | Recording page |
| Play recording | F9 | Recording page |
| Reset positions | F11, or **Back** + direction | Reset page; see [Back button reset](#back-button-reset) |
| Save state / Load state | F5 / F6 | Bindable: button config **Save State / Load State** rows; Reset page |
| Frame advance / Step frame | F2 / F3 (hold F3 = slow motion) | Bindable: button config **Frame Advance / Step Frame** rows; Reset page |
| Hitboxes on/off | F10 | Display page |

The bindable rows are in every button config page (gamepad: right above Easy Specials; keyboard: the last four rows). Keyboard 2 and gamepads start unbound. They only work in Training, and they aren't game inputs: binding one to an attack button does both.

---

## The dummy

### Dummy mode (Dummy Settings page)
| Mode | What the dummy does |
|---|---|
| **Controller** (default) | player 2 plays it |
| **Stand / Crouch / Jump** | holds that posture (Jump keeps jumping) |
| **CPU** | Sakuga's AI brain (only knows the Dummy's test moves for now) |
| **Playback** | loops the recording (see [Recording](#recording--playback)) |
| **Walk Forward / Walk Back** | keeps walking |
| **Run** | taps 66, runs in for about half a second, lets go (skids), waits, repeats |
| **Backdash** | 44, waits until it can act, repeats |
| **Jump Forward / Jump Back / Super Jump** (2 then 8) | jumps, lands, short pause, repeats |
| **Double Jump** | jumps, jumps again near the top |
| **Air Dash** | jumps, air dashes forward (66 in the air) |
| **Random Movement** | picks one of the movement modes every time (walks last ~40 frames) |

**Movement modes** use the same inputs a player would, so you can practise against movement: anti-airing jumps and super jumps, punishing a backdash, checking a run, catching an air dash. After every loop the dummy waits `DummyMovement.Gap` (14) free frames, giving you a window; if it gets hit or blocks, the pattern starts over once it's free. Guard and Reversal settings still apply (it can't guard while running / skidding, same as a player). Patterns are at the top of `DummyMovement.cs`, written as "hold forward / back / up / down for N frames" steps.

### Guard (Guard & Tech page)
| Guard | What it does |
|---|---|
| **None** | never blocks |
| **All** | always blocks the right way (stands for overheads, crouches for lows) |
| **After First Hit** | gets hit once, then blocks until it's been free for 20 frames |
| **Random** | randomly blocks or not, re-rolled each time it's free |
| **First Hit Only** | blocks the first hit of each string (also the first hit after waking up), then lets the rest hit |
| **Block X Hits** | blocks **Guard Hits** (1–10) hits, then lets the rest hit |
| **Switch: Stand > Crouch / Crouch > Stand** | fuzzy testing: blocks the first hit in the first stance if it can, then stays in the other stance for the rest of the string, right or wrong |

- Guard works in every dummy mode except Controller and CPU.
- A string ends when the dummy gets hit, or is left alone for 20 frames.
- Switch modes set the dummy's posture themselves (they ignore Stand / Crouch).

### Counters & Barrier (Dummy Settings page)
| Option | Choices |
|---|---|
| **Counter Hit** | Normal (the rules) · Always Counter · Always Big Counter · Never |
| **Functional Counter** | Default (what `CounterSettings.tres` says) · On · Off |
| **Dummy Barrier** | Normal · Infinite · Danger (keeps the dummy in Danger, for testing Functional Counters) |

---

## Knockdowns, techs & wakeup
The full knockdown timeline (Emergency Tech frames 1–8, no actions 9–23, wakeup options 24–63, forced tech at 64, rolls, hard knockdowns, INVALID combos) is in [TechRecovery.md](TechRecovery.md). These are the Training options for it.

### Dummy tech options (Guard & Tech page)
| Option | Choices |
|---|---|
| **Air Tech** | Off · Neutral · Forward · Back · Random: air recovery on the first possible frame |
| **Ground Tech** | Off · Neutral (Neutral Tech at frame 24) · Forward Roll · Back Roll · Emergency Tech (frame 1) · Random (picked once per knockdown) |
| **Tech Delay** | Frame 1 · +1 … +60 frames after the tech becomes possible (an Emergency Tech delayed past frame 8 doesn't happen) |

With Ground Tech Off the dummy lies until the forced tech at frame 64. Hard knockdowns (throws, wall bounces) can't be teched; the dummy just gets up.

### Wakeup reversals
Reversal → **On Wakeup** fires on the first frame after any ground tech, roll or getting up (also after a throw), and **On Air Tech** right after an air recovery. See [Reversals](#reversals).

### Knockdown test moves (Dummy)
Hold down + a button with P1 (keyboard: hold S):

| Move | What it tests |
|---|---|
| **2A** Test Launcher | air hit, air-techable after the untechable part |
| **2B** Test Air Knockdown | soft knockdown: Emergency Tech, rolls, Neutral Tech |
| **2C** Test Hard Sweep | low, hard knockdown: no tech, just gets up |
| **2D** Test Fast Knockdown | pops up and lands right away: wakeup / meaty practice |

Hitting a dummy that could have teched (lying after a soft knockdown, or in the air after the untechable part) turns the combo counter red with **INVALID**, in every mode.

### Practising tech yourself
Switch the dummy to **Controller** (or record it) and knock P1 down: hold A/B/C as you land for an Emergency Tech, or hold A/B/C + a direction from frame 9 on to tech or roll on frame 24 (hold-to-tech, see TechRecovery.md). Use **save states** to repeat the same knockdown, and **frame advance** to check the exact frames.

---

## Reversals
(Reversal Options page)

| Option | Choices |
|---|---|
| **Reversal** | Off · **Action** (a move from the dummy's list) · **Play Recording** (plays a recording, starting at its first input) |
| **Action** | the dummy's reversal list: its own moves (Dummy: 5A–5D, 2A–2D) then the universal ones it has: jumps, super jumps, backdash, run or step dash, Barrier, throws, Burst |
| **On Wakeup / On Block / On Hit / On Air Tech** | which situations trigger it (wakeup includes ground techs and getting up after a throw) |
| **Delay** | Frame 1 · +1 … +60 frames |

- **Frame-perfect:** the action starts on the exact frame a perfectly timed button press would (tested against a mashing player), and the Delay counts from there.
- Moves that need a motion (66, 2~8, back + A+B) come out directly; Run and Barrier keep holding their input for a moment (`Hold` / `HoldFrames` in the list).
- **Play Recording** uses the selected slot, or a random filled one with Playback: Random Slot.
- A blue popup shows what happened: "REVERSAL  5B Test Mid  (BLOCK)", "AIR TECH", "FORWARD ROLL".

### Adding reversal actions for a character
1. Right-click the fighter's folder → **Create New → Resource → ReversalList** → save as `<Name>_Reversals.tres`.
2. Add one **ReversalAction** per move: **Label** (what the menu shows, e.g. `236B  Fireball`), **Move Name** (exactly the move's MoveName in the stance), and optionally **Hold** (numpad notation facing the opponent, e.g. `4AB`) + **Hold Frames**.
3. Leave **Include Universal** on to also get jumps, backdash, run, Barrier, throws and Burst.
4. `<Name>_Data.tres` → **Training Reversals** = that file. A fighter without one gets the universal list. Moves the fighter doesn't have are hidden automatically.

---

## Recording & playback
(Recording page)

| Option | Choices |
|---|---|
| **Slot** | 1–5 (shows the length, or Empty): the slot that records and plays |
| **Playback** | This Slot · **Random Slot** (each playback, loop, reversal or Play on Reset picks a random filled slot: mixups) |
| **Record Dummy (F8)** | you control the dummy with player 1's controls until you press it again (max 10 s), into the selected slot. P1 stands still |
| **Play Recording (F9)** | the dummy plays it once. For a loop, set Dummy to **Playback** |
| **Clear Slot / Clear All Slots** | |
| **Countdown (3-2-1)** | On: pressing Record shows RECORDING IN 3 / 2 / 1 first; you can already move the dummy into place, nothing is stored until it ends |
| **Play on Reset** | Off · Immediately (any reset starts the recording) · After READY / GO! (nobody can move during READY; the recording starts on GO!) |

- Recordings are stored relative to the way the dummy faces, so they play back the same on either side.
- On screen: blinking red **REC SLOT n** with the time, green **PLAYBACK  SLOT n**.
- Recordings live in memory: they're kept through Quick Character Select, not after closing the game.
- A recording that ends holding forward, played back again within 10 frames, can turn into a 66 run (the normal input rule).

---

## Health & meters
(Health & Meters page)

| Option | Choices |
|---|---|
| **Health Refill** | On (both refill after each combo) · Off |
| **P1 Health / Dummy Health** | what the refill goes back to: 100 · 75 · 50 · 25 · 10 % |
| **Super Meter / Barrier / Burst** | Normal · Infinite · **Refill After Combo** |
| **Super Refill Amount** | 0–100 % in steps of 10: what Refill After Combo puts the super meter back to |

- **Infinite** keeps it full for both players.
- **Refill After Combo** refills once both fighters have been free for 30 frames (`TrainingSettings.RefillDelay`), i.e. after every combo or block string, never in the middle. Super goes back to the Super Refill Amount (practise combos with an exact amount of meter); Barrier and Burst go back to full.
- The data panel shows damage before the Danger ×1.5 multiplier.

---

## Positions, resets & save states

### Reset position (Reset page)
**Reset Position** (used by F11 and **Reset Now**): Center · Dummy Left Corner · Dummy Right Corner · P1 Left Corner · P1 Right Corner · Center, Sides Switched. A reset puts both fighters back with full health and states reset.

### Back button reset
Either player's **Back/Select** on a gamepad; **Tab** for keyboard 1, **Keypad +** for keyboard 2. The direction held decides where everyone goes (as seen on screen); the Reset & Reposition card next to the pause menu shows the same thing:

| Hold | Result |
|---|---|
| nothing (5) | midscreen |
| left (4) | left corner, P1 inside the corner |
| down-left (1) | left corner, P2 (dummy) inside the corner |
| down (2) | midscreen, sides switched (P1 on the right) |
| down-right (3) | right corner, P1 inside the corner |
| right (6) | right corner, P2 (dummy) inside the corner |

### Save states
- **One slot.** **Save State** (F5) / **Load State** (F6), bindable in the button config, also on the Reset page. A purple STATE SAVED / STATE LOADED notice shows under the status line; Load with nothing saved shows NO STATE SAVED.
- It's the same snapshot rollback uses, so everything comes back exactly: both fighters, positions, states and their frames, health, Barrier, Burst, super, counters, hitstun, the timer.
- Things that aren't part of the match (recordings, Training settings) stay as they are. Loading stops recording / playback and restarts the frame meter.
- The slot empties when Training starts again (e.g. after Quick Character Select).

---

## Frame data tools

### Frame meter (Display → Frame Meter)
Lower middle, above the meters: one row per player, a cell per frame (hitstop skipped, the hit frame counted).
- Colors: green startup · red active · blue recovery · yellow hitstun · orange blockstun · purple knockdown · teal "busy but not attacking" (prejump, backdash, rolls, techs, block / getup exits) · dark = free.
- White stripe on top = strike invincible; orange stripe underneath = throw invincible.
- Summary lines: Startup (the frame it hits on) / Active / Recovery of the first attack, and Advantage once both are free again. A new run starts when someone acts after both were free.

### Frame advance
- **Frame Advance** (F2) freezes the match; **Step Frame** (F3) moves it exactly one frame; hold Step to play slowly (after 18 rendered frames, one step every 4). Press Frame Advance again to resume. Pressing Step while the game runs also freezes it.
- Inputs you hold while pressing Step go into that frame, so you can test exact timings (meaties, links, reversal frames) frame by frame.
- Shows **FRAME ADVANCE +n** (frames stepped) at the top. State flashes and hitsparks freeze too; a few engine particle effects keep playing in real time.
- The simulation itself is untouched: `GameManager.GameLoop` just waits (`[FF PATCH: frame advance]`).

### Training data panels (Display → Training Data)
Bottom corners, one per player: damage, combo damage, max combo, hit type, damage scaling, frame data, advantage (green +, red −; the frame meter's number, which counts block / getup exit frames) and the counter type of the last hit. The combo counter is Sakuga's own, under the health bar.

### Advantage hitsparks (Display → Advantage Hitsparks)
The hitspark tells the **attacker** who recovers first: blue = plus, yellow = even, red = minus, and its **size** says by how much (±1 small → ±10 or more big). In every mode; the Training toggle Off brings back the old hit effects. How it works and where to tune it: [Visuals.md](Visuals.md).

---

## Other displays & indicators
(Display page)

| Display | What it shows |
|---|---|
| **Input History** | Sakuga's input list |
| **P1 / P2 Input Display** | a stick gate and A / B / C / D buttons per player above the data panels, showing what the fighter actually receives (recordings and reversals show too; macro buttons light the buttons they press) |
| **State Flashes** | white = invincible, blue = armor, red / orange / gold = counter hits, purple = punished, cyan = reversal (all modes; see Visuals.md) |
| **Punish Indicator** | "Punish" notification + purple flash when a hit lands on a recovering opponent (see Visuals.md) |
| **Reversal Indicator** | "Reversal" notification + cyan flash when a special comes out on the first possible frame (see Visuals.md) |
| **Hitboxes** (F10) | hitboxes and hurtboxes |

**Status line** (under the timer): RECORDING IN 3/2/1 · REC SLOT n · PLAYBACK SLOT n · READY / GO! · FRAME ADVANCE +n. Below it, short popups: blue for reversals and techs, purple for save states.

---

## Quick Character Select
The pause menu has a row above its buttons: **P1: < name >** and **Dummy: < name >**.
- Press **Up** from the buttons to reach it, **Left/Right** to pick a character (the name turns orange while it's different from what's loaded), **Accept** to reload Training with that pair. **Down** goes back to the buttons.
- Every Training setting and the recordings are kept (save states are cleared). Colors stay the same unless the new character doesn't have that palette.

---

## Reference

### Files
| Area | File | What it is |
|---|---|---|
| Core | `Scripts/FunctionFighter/Training/TrainingSettings.cs` | the options, their defaults and saving |
| | `Scripts/FunctionFighter/Training/TrainingController.cs` | applies them: dummy inputs, recording slots / playback, guard, meters, health, resets |
| | `Scripts/FunctionFighter/Training/TrainingMenu.cs` | the settings pages and the hotkeys (node `TrainingMenu` in `Sakuga_UI.tscn`) |
| | `GameModes/GM_Training.tres` → `IsTraining` | turns all of this on for that mode |
| Dummy | `Scripts/FunctionFighter/Training/DummyMovement.cs` | the movement modes (input scripts) |
| | `Scripts/FunctionFighter/Mechanics/GuardOverride.cs` | lets the dummy guard without input (and Barrier guard without back) |
| Reversals & techs | `Scripts/FunctionFighter/Training/Reversal.cs` | reversals and dummy techs (frame-perfect timing, triggers, delay, hold input) |
| | `Scripts/FunctionFighter/Training/ReversalAction.cs`, `ReversalList.cs` | the reversal list resources |
| | `Fighters/Shared/Training/UniversalReversals.tres` | universal reversal actions (every fighter) |
| | `Fighters/Dummy/Dummy_Reversals.tres` | the Dummy's own (5A–5D, 2A–2D) |
| Save states / frame advance | `Scripts/FunctionFighter/Training/SaveStates.cs`, `FrameAdvance.cs` | the save state slot; freeze / step |
| Displays | `Scripts/FunctionFighter/Training/FrameMeter.cs`, `FrameMeterHUD.cs` | frame meter data and drawing (colors and size at the top of FrameMeterHUD) |
| | `Scripts/FunctionFighter/Training/InputDisplayHUD.cs` | the input displays (positions and colors at the top) |
| | `Scripts/FunctionFighter/Training/TrainingStatusHUD.cs` | the status line and popups |
| | `Scripts/FunctionFighter/UI/TrainingInfoPanel.cs` | the training data panels (`GameHUD_Foreground/TrainingInfo/P1Info`, `P2Info`) |
| Menu extras | `Scripts/FunctionFighter/Training/QuickCharacterSelect.cs`, `ResetGuidePanel.cs` | the character row and the Reset & Reposition card (created by `TrainingMenu`) |
| Buttons | `Scenes/Instances/input_remapper.tscn`, `project.godot` Input Map | the bindable Save State / Load State / Frame Advance / Step Frame rows and actions (see [Controls.md](Controls.md)) |

### Engine patches (search for `[FF PATCH: training]`)
| File | What it changes |
|---|---|
| `GameManager.cs` → `GameLoop` | every input goes through `TrainingController.FilterInput`; after the step, `AfterTick` applies meters and guard; health refill follows the settings; `[FF PATCH: frame advance]` waits while frozen |
| `GameModeSettings.cs` | the `IsTraining` checkbox |
| `StanceManager.cs` → `GetBlockState` | dummy guard (shared with the barrier patch) |
| `StanceManager.cs` → `CheckMoves`, `CanExecuteNow` | `[FF PATCH: training reversal]`: runs the dummy's tech / reversal at the move-check point |
| `SakugaActor.cs` → hit / block | `[FF PATCH: training reversal]`: tells Training when the dummy blocks or gets hit |
| `MetersHUD.cs` | fills the training data panels |
| `DataContainer.cs` | the **Training Reversals** slot |

### Tuning
| I want to… | Where |
|---|---|
| Change a default | `TrainingSettings.Defaults()` and the field it sets |
| "After First Hit" guard window | `TrainingController.AfterHitGuardFrames` (20) |
| Pause between dummy movement loops | `DummyMovement.Gap` (14), `RandomWalkFrames` (40) |
| Longer recordings / more slots | `TrainingController.MaxRecordFrames` (600) / `Slots` (5) |
| Countdown / READY-GO length | `TrainingController.CountdownFrames` (90) / `RoundCallFrames` (75), `GoFrames` (30) |
| Refill After Combo delay | `TrainingSettings.RefillDelay` (30 frames both free) |
| Hold-to-step speed | `FrameAdvance.RepeatDelay` (18) / `RepeatEvery` (4) |
| Move / resize the data panels | `Sakuga_UI.tscn` → `TrainingInfo/P1Info`, `P2Info` (position); `TrainingInfoPanel` fields `FontSize`, `RowHeight`, `PanelWidth` |

### Known issues / gotchas
- The dummy is always player 2.
- If Godot re-saves `GM_Training.tres` before the C# project has been built, it drops the `IsTraining` box. Training is also detected by the mode name "Training", so the page still shows up. Build before saving resources after pulling C# changes.
- CPU uses Sakuga's AI brain, which only knows the Dummy's test moves.
- Recordings aren't saved to disk; the save state slot is cleared on a new match.
- During frame advance, a few engine particle effects keep animating in real time.

### Friends' suggestions
| Suggestion | Who | Status |
|---|---|---|
| Counter hit options | Ashley, Gucci | ✅ Counter Hit: Normal / Always Counter / Always Big Counter / Never |
| Gauge options ("the obvious stuff") | Gucci | ✅ Infinite / Refill After Combo for Super, Barrier, Burst; health refill and % |
| Meter regen options (refill after combo) | Steve | ✅ Refill After Combo + Super Refill Amount (gradual regen not added) |
| Player 2 control | Steve | ✅ Dummy: Controller |
| Record / playback | Gucci, Steve | ✅ 5 slots, Random Slot, loop with Dummy: Playback |
| Recording countdown (KOF), playback on position reset (with / without the round-start call) | Gucci | ✅ |
| Reset to corner / midscreen / sideswap | Steve | ✅ Back + direction, Reset Position |
| Save states | Steve | ✅ one slot, F5 / F6, bindable |
| Frame advance | — | ✅ F2 / F3, bindable |
| Frame data on screen; frame meter bar (Strive / GBVS) + input display | Steve, Himbo | ✅ data panels, frame meter, input displays, advantage hitsparks |
| Quick character select | Gucci | ✅ |
| Reversal on wakeup / block / hit / air tech, frame 1, with an X-frame delay | Himbo, Gucci, Steve | ✅ |
| Reversal **actions** without recording (e.g. jump or 5A on wakeup), per character | Glyn | ✅ reversal lists |
| Dummy techs (air / ground, rolls, Emergency) | Himbo | ✅ Air Tech / Ground Tech / Tech Delay |
| Guard: block first hit only (working on wakeup), block X hits, switch stand / crouch on the 2nd hit (fuzzies) | Gucci | ✅ |
| All movement options for the dummy (run, backdash, jumps, super / double jump) | Steve | ✅ dummy movement modes |
| Install options (strength / type / infinite duration) | Gucci, Steve | 📝 once Function Fighter has an install mechanic |
| Ranked search from Training | Himbo | 📝 later, with online |
| Frame data in the command list | Himbo | 📝 later, with the command list |
| KOF-style stand/crouch hurtbox switch in blockstun toggle | Gucci | ⛔ not doing |

### Test result
All checks pass (2026-10-12):
- **Dummy:** Crouch / Jump / Stand / CPU; every movement mode (walk both ways, run + skid, backdash, jump forward / back, super jump, double jump, air dash, random) and guarding while walking forward.
- **Guard:** All (stands for a mid, crouches for a low), None, After First Hit, First Hit Only (incl. after wakeup), Block 2 Hits, Switch Crouch > Stand fuzzy.
- **Counters & Barrier:** always / never counter; infinite Barrier and Burst.
- **Knockdowns & techs:** air tech; Emergency / Neutral +5 / back roll + wakeup reversal (see TechRecovery.md for the tech timeline tests).
- **Reversals:** 5B on block on the same frame as a perfect mash, +5 delay, trigger off, wakeup jump / Barrier / run, Burst on hit, wakeup after a throw, recording reversal on the same frame.
- **Recording:** P1 drives the dummy and playback repeats it; countdown; Play on Reset (immediate, READY / GO!); each slot plays its own recording, Random Slot plays both.
- **Health & meters:** Health Refill off; Dummy Health 50 %; Refill After Combo (no refill mid-combo, super back to 50 %, Barrier and Burst full).
- **Positions & save states:** corner reset; save state (load puts position / health / frame back, a hit sparks again after a load, load with nothing saved does nothing, bindable buttons).
- **Frame data tools:** frame meter; frame advance (frozen = no frames and no movement, a step = exactly one frame with the held input, resume; through the bindable buttons); advantage hitsparks exact on the first hit / block.

### Change log
| Date | Change |
|---|---|
| 2026-10-12 | Doc reorganised by topic (dummy, knockdowns & techs, reversals, recording, meters, resets & save states, frame data tools, displays) |
| 2026-10-12 | Frame advance (bindable Frame Advance / Step Frame); 5 recording slots + Random Slot playback; meters: Refill After Combo + Super Refill Amount |
| 2026-10-12 | Save / Load State are bindable buttons in the button config |
| 2026-10-17 | Display: Punish Indicator / Reversal Indicator toggles; Training Data → Counter shows Punish |
| 2026-10-12 | Dummy movement modes; save states (F5 / F6); Advantage Icons renamed Advantage Hitsparks |
| 2026-10-12 | Settings split into 7 pages (LB/RB, PgUp/PgDn); Reset & Reposition card; overlays behind the menus; Advantage Icons / State Flashes toggles |
| 2026-10-12 | Frame meter, P1 / P2 input displays, data-panel advantage from the meter |
| 2026-10-12 | Tech row split into Air Tech / Ground Tech / Tech Delay (rolls, Emergency Tech, Random) |
| 2026-10-12 | Guard: First Hit Only, Block X Hits, Switch (fuzzy); Tech; Reversal (action / recording, triggers, delay, per-character lists); recording countdown; Play on Reset with READY / GO!; on-screen indicators; scrolling settings page |
| 2026-10-12 | Quick Character Select row in the pause menu; friends' suggestions triaged |
| 2026-10-12 | Back button + direction resets; Training detected by mode name too; Training/Controls pages block the pause key |
| 2026-10-12 | Created: Training Settings page, recording, guard, counter settings, data panel layout fix |
