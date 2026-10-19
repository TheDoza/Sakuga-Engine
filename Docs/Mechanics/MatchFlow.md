# Match End: KO and Rematch screen

**Status:** ✅ Working (tested 2026-10-18) · **Fighters:** All fighters, every mode that can KO · **Added:** 2026-10-18 · **Rollback-safe:** KO: yes (saved fighter state only). Rematch screen: menu only, after the match

## What it does
- **KO:** the finishing hit always launches the fighter (even a standing 5B). They fly up, land in Hit_Down and **stay down** until the next round. A KO'd fighter ignores its inputs: no walking, attacks, techs, rolls or Burst, so a held stick does nothing.
- **Rematch screen:** after the winner's outro, each player gets their own menu: **Rematch**, **Character Select**, **Leave Match**. The winner's victory render and quote are shown, with both portraits, WON / LOST and rounds won in the middle strip.
  - Rematch only when **both** players pick it. A CPU side always picks Rematch.
  - **Character Select** from either player takes both there.
  - **Leave Match** from either player leaves (main menu; later, online returns to its lobby).
  - Up / down to move, Accept or A to pick, Return or B to take a pick back. Input is ignored for the first half second so the button that skipped the outro doesn't pick anything.

## How it works (flow)
```
finishing hit --> KO.OnHit: Hit_Air, hard knockdown, launch (KOLaunchX / Y) --> lands --> Hit_Down (held, no tech / getup)
round ends (KO'd fighter lying still) --> winner's outro --> RESULTS --> RematchScreen
  both Rematch --> reload the match scene | any Character Select --> SelectScreen | any Leave Match --> LeaveMatchScene
```

## Files
| File | What it is |
|---|---|
| `Scripts/FunctionFighter/Mechanics/KO.cs` | KO launch, keeping the KO'd fighter down |
| `Scripts/FunctionFighter/UI/RematchScreen.cs` | the results / rematch screen (built in code, no scene) |
| `Scripts/SakugaEngine/Components/SakugaActor.cs` → `HitDamage` | `[FF PATCH: ko]` calls KO.OnHit after the damage |
| `Scripts/SakugaEngine/Components/StanceManager.cs` → `CheckMoves` | `[FF PATCH: ko]` a KO'd fighter's inputs do nothing |
| `Scripts/SakugaEngine/Components/GameMonitor.cs` → `MatchEndCondition`, `ShowResultScreen` | `[FF PATCH: ko]` the round ends once the KO'd fighter lies still; `[FF PATCH: rematch]` opens the rematch screen (the old results panel is the fallback) |
| `Scripts/SakugaEngine/Game/GameManager.cs` | `[FF PATCH: rematch]` the old "any button returns" on the results is off while the rematch screen is open |
| `Scripts/SakugaEngine/Resources/GameModeSettings.cs` | `[FF PATCH: rematch]` `LeaveMatchScene` |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| How far / high a KO launches | `<Name>_Locomotion.tres` → KO | `KOLaunchX`, `KOLaunchY`, `KOLaunchGravity`, `KOLaunchTime` | 25000, 60000, 230000, 8 |
| Where Leave Match goes for a mode | `GameModes/GM_<Mode>.tres` | `LeaveMatchScene` | `res://Scenes/MainMenu.tscn` |
| Colours of the rematch screen | `RematchScreen.cs` | `AccentColor`, `StripColor`, `GoldColor`, `WonColor`, `LostColor` | orange-red, navy, gold |
| Input delay before picks count | `RematchScreen.cs` | `InputDelayFrames` | 30 |
| Winner render / quote | `<Name>_Profile.tres` → `VictoryRender`; the outro's message | | |

## Known issues / gotchas
- Online: Leave Match should go back to the lobby. Set the online mode's `LeaveMatchScene` to the lobby scene when online play is in.
- Rematch reloads the current match scene with the same characters and stage.
- Training can't KO (its mode has `CanKO` off), so the screen never shows there.

## Tests
SUITE=ko (KOTest): a KO'd fighter holding forward never walks and stays in Hit_Down; the match reaches the results; the rematch screen opens; one Rematch waits for the other player; Rematch + Character Select goes to character select; Leave Match goes to the main menu; both Rematch restarts; the red lost-health bar uses the fighter's MaxHealth.

## Change log
| Date | Change |
|---|---|
| 2026-10-18 | Created: KO launch and stay-down, KO'd fighters ignore inputs, rematch screen |
