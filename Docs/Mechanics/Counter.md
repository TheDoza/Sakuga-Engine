# Counter Hits

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** every fighter (engine logic; Big Counter is a hitbox checkbox) · **Rollback-safe:** Yes (the counter state is saved with each fighter, all integer math)

## What it does
Function Fighter has three counter hits.

| | When | What it does |
|---|---|---|
| **Counter Hit** | You hit someone during their attack's **startup or active** frames (not recovery) | +10% damage, +4 hitstun (+8 untechable time in the air), +8 hitstop for the opponent (more time to confirm). Red flash, "Counter" pops up. |
| **Counter Hit Carry** | The first hit of a multi-hit attack was a Counter Hit | The rest of **that attack's** hits count too: bonus damage and hitstun, but not the extra hitstop. Ends when the attack ends. |
| **Big Counter** | A Counter Hit with an attack whose hitbox has **Big Counter** ticked | Same as a Counter Hit, and **every later hit in that combo** gets +3 hitstun / untechable time. Doesn't stack: two Big Counters in one combo still add 3, not 6. "Big Counter" pops up. |
| **Functional Counter** *(name in progress)* | A **Big Counter** on an opponent who's in **Danger** (barrier gauge ran out) | For 5 seconds: every hit does **double hitstun**, launched opponents **fall slowly** (35% gravity), and every attack that ends without being cancelled is followed by an **auto-dash** toward the opponent. Gold burst, "Functional Counter" pops up. |

Functional Counter ending: when the 5 seconds run out, **the next hit still gets the long hitstun / slow fall**, shows a **green spark** and "Functional End", and everything after it is back to normal. The combo isn't over: you can keep juggling with normal rules. If the combo drops before that, it ends straight away. There's no gauge for it (unlike KOF XII): Danger + Big Counter is the only way in.

Functional Counter can be switched off: `FunctionalCounterEnabled` in `CounterSettings.tres`, or **Functional Counter: On / Off** in the Training Settings for testing.

Throw counters (BBCF) aren't in the game.

The defender's model also flashes **red** (Counter), **orange** (Big Counter) or **gold** (Functional Counter) for 24 frames: see Visuals.md.

**Punish** (a hit on an opponent in the recovery of a move) isn't a counter hit: no bonus, just the indicator. See Visuals.md → Punish and Reversal indicators.

**Counter Hit Recovery** (ready for future moves; no Dummy move uses it yet): tick **Counter Hit In Recovery** on a move's RECOVERY entry in its AnimationData → Hitboxes (the `HitboxState` with `animationStage` = RECOVERY). Getting hit there is then a normal **Counter** (red, with the full bonus; Big Counter / Functional Counter work too), and the **"Punish"** notification shows as well. The red effects win over the purple ones. Meant for big, risky moves.

## How it works (flow)
```
hit lands ─► defender in startup/active of an attack?  ─ no ─► same attack already countered? ─ yes ─► Carry (bonus, no extra hitstop)
               │ yes
               ▼
         Counter Hit (+10%, +4/+8 hitstun, +8 hitstop) ── hitbox BigCounter? ─ yes ─► Big Counter (combo gets +3 hitstun)
                                                                                     └ defender in Danger + FC on ─► Functional Counter (5 s)
Functional Counter: hitstun ×2, gravity ×0.35, auto-dash after attacks ─► time up ─► next hit: green spark, back to normal
combo drops (defender not in a hit reaction) ─► Big Counter combo and Functional Counter end
```
- "Startup or active" uses Sakuga's own counter check (`SakugaActor.IsCounter`): both fighters in a COMBAT state, defender's current hitbox stage before RECOVERY. Trades count as counter hits for both.
- The auto-dash starts when an attack (COMBAT state) ends into a free state without a cancel, on the ground. It moves 14,000 over 10 frames and stops 7,000 away from the opponent.
- The Functional Counter timer counts frames outside hitstop.

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/Counter/CounterSettings.tres` | **the numbers** (edit in the Inspector) |
| `Scripts/FunctionFighter/Mechanics/Counter.cs` | all the rules + the saved per-fighter state |
| `Scripts/FunctionFighter/Mechanics/CounterSettings.cs` | the settings resource (what each field means) |
| Any hitbox (`Hitboxes/*.tres`) → **Big Counter** checkbox | makes that attack a Big Counter attack. On the Dummy: **Test Wall Bounce (A)** |
| `Fighters/Shared/VFX/VFX_07.tscn` / `VFX_08` / `VFX_09` | red counter flash / gold Functional Counter start / green spark (entries 7–9 in `Shared_VFXList.tres`) |
| `HitNotifs.tres` | notifications 10 "Big Counter", 11 "Functional Counter", 12 "Functional End" (1 "Counter" already existed) |

## Engine patches (search for `[FF PATCH: counter]`)

`HitboxState.cs` → `CounterHitInRecovery` (new field, Counter Hit Recovery).

| File | What it changes |
|---|---|
| `SakugaActor.cs` → `HitDamage` | asks `Counter.OnHit` for this hit's bonus: hitstun, hitstop, damage, knockback gravity |
| `SakugaActor.cs` → hit code | the "Counter" popup now comes from `Counter.cs` (the "Punish" popup is unchanged) |
| `SakugaActor.cs` | stores the counter state; resets it each round; `Tick` runs carry, combo flags, the timer and the auto-dash |
| `GameState.cs` | saves/loads the counter state for rollback (key 18) |
| `HitboxElement.cs` | the **Big Counter** checkbox |

## Tuning: "I want to change…" (all in `CounterSettings.tres`)
| I want to… | Field | Current |
|---|---|---|
| Counter Hit damage | `DamagePercent` | 110 |
| Extra hitstun (ground / air) | `ExtraGroundHitstun` / `ExtraAirHitstun` | 4 / 8 |
| Extra hitstop | `ExtraHitstop` | 8 |
| Turn Counter Hit Carry off | `CounterHitCarry` | on |
| Big Counter combo bonus | `BigCounterComboHitstun` | 3 |
| Functional Counter on/off | `FunctionalCounterEnabled` | on |
| Its length | `Duration` | 300 (5 s) |
| Its hitstun / fall speed | `HitstunPercent` / `GravityPercent` | 200 / 35 |
| Auto-dash | `AutoDashDistance` / `AutoDashFrames` / `AutoDashMinGap` | 14,000 / 10 / 7,000 |
| Popups / effects | `*NotificationIndex`, `*VFXIndex`, `VFXHeight` | 1, 10, 11, 12 / 7, 8, 9 |
| Make a move's recovery counter-hittable | that move's `<n>_AnimationData.tres` → Hitboxes → the RECOVERY entry → `CounterHitInRecovery` | off |

## Adding it to another fighter
Nothing to do: every fighter has it. Tick **Big Counter** on the hitboxes of that fighter's Big Counter attacks (BBCF gives every character at least one).

## Known issues / gotchas
- The auto-dash follows any uncancelled attack, specials included. KOF XII only does it after normals; once specials exist, a "no auto-dash" flag on special moves would match it.
- The training data panel shows a hit's damage before the Danger ×1.5 multiplier.
- The extra hitstun values are a starting point, not BBCF's per-attack-level table.

## Test result
All checks pass (2026-10-12):
| Check | Result |
|---|---|
| Hit during the opponent's startup | Counter: 440 damage (400 + 10%), hitstun 22, hitstop 20 |
| Hit during recovery | no counter |
| Counter Hit Carry | second hit of the same attack: bonus, no extra hitstop; after the attack: nothing |
| Big Counter | later hits +3 hitstun, not stacking; cleared when the combo ends |
| Test Wall Bounce on a counter | Big Counter, 550 damage |
| Big Counter on Danger | Functional Counter: hitstun 40, 660 damage (Danger ×1.5) |
| Auto-dash after an attack | moves forward > 5,000 |
| Launched during it | stays in the air much longer than a normal air hit |
| Switched off / no Danger | no Functional Counter |
| Combo drops / time up | ends; the last hit keeps the long hitstun and ends it |
| Rollback save/load | restored exactly |

## Change log
| Date | Change |
|---|---|
| 2026-10-17 | Counter Hit Recovery flag (`HitboxState.CounterHitInRecovery`): Counter + Punish notification |
| 2026-10-12 | Created: Counter Hit + carry, Big Counter, Functional Counter |
