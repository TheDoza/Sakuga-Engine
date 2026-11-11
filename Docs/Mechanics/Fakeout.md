# Function Fakeout

**Status:** 🧪 Testing (added 2026-11-05) · **Fighters:** all (engine-level, nothing per fighter) · **Rollback-safe:** Yes (state saved in CounterState)

## What it does
Press **A+B+C** (or the **G** macro) during a move to cancel it and go straight back to neutral: standing on the ground, falling in the air (keeping your air momentum). It costs super meter. A colored ring of light goes off around you and a notification shows on your side. If the Fakeout can't happen (not allowed at that point, or not enough meter), a **red X** pops up over your fighter instead, like the green "!" on a throw. Use it to extend combos, keep pressure going or make an unsafe move safe.

The color, and the cost, depend on what you cancel and what the opponent is doing:

| Color | Cost | When |
|---|---|---|
| 🔵 **Blue** | 25% | Startup or recovery of a **special**, opponent **not** in hitstun or blockstun (whiffed or not yet hit) |
| 🔴 **Red** | 50% | A **normal**, **special** or **risky** move while the opponent is in **hitstun, blockstun or a knockdown** |
| 🟣 **Purple** | 75% | A **risky** move (invincible reversal, command grab) that was **blocked**, or its **whiff recovery** |
| 🟡 **Gold** | 50% | **Active or recovery** frames of a **super**. Supers cost 50%, so super + Gold = the whole gauge |

No Fakeout: a whiffed normal, the active frames of a whiffed special, the startup of a risky move or a super, a throw that connected, any move set to **None** (all universal throws are), the parts of a move it blocks (`FakeoutBlockedFrames` / `FakeoutBlockedStates`, for command grabs and the like), and an A+B+C press made before the move started (that press only starts the move). A press during a move that can't Fakeout shows the red X.

## How it works (flow)
```
A+B+C pressed during a move (kept 3 frames) ──► blocked frame / state? ──► which move? (MoveSettings.Fakeout)
    Super   ── active / recovery ─────────────────────────────► GOLD
    Risky   ── opponent in hitstun ───────────────────────────► RED
            ── opponent in blockstun, or own recovery ────────► PURPLE
    Special ── opponent in hitstun / blockstun ───────────────► RED
            ── own startup / recovery ────────────────────────► BLUE
    Normal  ── opponent in hitstun / blockstun ───────────────► RED
──► enough meter? ──► spend it · end the move · 0 Idle (ground, stops) / 6 Falling (air, momentum carries)
                     · ring VFX + notification
3 frames pass without a Fakeout ──► red X over the fighter
```
- **Which kind of move:** each move's `Fakeout` field (Move Flags, at the bottom of a MoveSettings). **Auto** works it out: costs meter (`SuperGaugeRequired` > 0) = Super; a motion input (236, 214, 623, [4]6...) = Special; anything else = Normal. **Risky** is never automatic: set it on invincible reversals and command grabs. **None** forbids it.
- **Can be done during hitstop**, so it can be input on the hit. The buffered press waits for the right moment for 3 frames.
- **Blocking parts of a move:** on the move's `.tres`, `FakeoutBlockedFrames` lists frame ranges of the move's own state (X to Y, inclusive) where Fakeout isn't allowed, and `FakeoutBlockedStates` lists follow-up states where it's never allowed (e.g. a command grab's throw animation). Use `Fakeout = None` to block the whole move.
- **No slowdown:** the opponent keeps moving at full speed.
- **Air momentum:** an air Fakeout keeps your current speed and direction and you fall from there (`KeepAirMomentum`). Ground Fakeouts stop you in place.
- **After a Fakeout** you're in neutral: walk, jump, block, attack. Air Fakeouts keep your remaining air actions. Special landing recovery from the cancelled move is cleared.
- **Meter** is checked against the cost. With not enough, the red X shows and the move carries on.

## Files
| File | What it is |
|---|---|
| `Scripts/FunctionFighter/Mechanics/Fakeout.cs` | The rules, the input check, the red X |
| `Scripts/FunctionFighter/Mechanics/FakeoutSettings.cs` | Tuning resource (class) |
| `Fighters/Shared/Fakeout/FakeoutSettings.tres` | Tuning values (costs, notifications, VFX, air momentum) |
| `Fighters/Shared/VFX/VFX_19.tscn` … `VFX_22.tscn` | Ring of light: blue, red, purple, gold (Shared_VFXList 19–22) |
| `Fighters/Shared/VFX/VFX_23.tscn` | Red X when a Fakeout fails (Shared_VFXList 23; same style as the throw "!", VFX_04) |
| `HitNotifs.tres` | Notifications 14–17: Blue / Red / Purple / Gold Fakeout |
| `Scripts/SakugaEngine/Resources/MoveSettings.cs` | `Fakeout`, `FakeoutBlockedFrames`, `FakeoutBlockedStates` per move |
| `Scripts/FunctionFighter/Mechanics/Counter.cs` | CounterState key 13: input buffer |
| `Fighters/Shared/Universal/Moves/11–13_Throw_*.tres` | `Fakeout = None`: throws can never be Fakeout cancelled |

### Engine patches (search for `[FF PATCH: fakeout]`)
| File | Change |
|---|---|
| `StanceManager.cs` → `CheckMoves` | `Fakeout.Try` runs first every frame; if it fires, normal input matching is skipped that frame |

### Dummy test moves
| Input | Move | Kind | Plays |
|---|---|---|---|
| **236A** | Test Fakeout Special | Special (Auto, motion input) | state 101 (Test Mid) |
| **623C** | Test Fakeout Risky | Risky (set by hand); frames 10–17 blocked (`FakeoutBlockedFrames`), so the first part of its recovery shows the red X and Purple works from frame 18 | state 101 (Test Mid) |
| **236B+C** | Test Fakeout Super | Super (Auto, costs 5000) | state 101 (Test Mid) |

Move files: `Fighters/Dummy/Special/Moves/24_TestFakeoutSpecial.tres`, `25_TestFakeoutRisky.tres`, `26_TestFakeoutSuper.tres` (appended to the stance's `Moves`). They reuse Test Mid's state and hitbox, so they look like 5B; only their Fakeout rules differ. The Dummy's other test moves are Normals.

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Change a cost | `FakeoutSettings.tres` | `BlueCost` / `RedCost` / `PurpleCost` / `GoldCost` (10000 = full gauge) | 2500 / 5000 / 7500 / 5000 |
| Risky move: no Purple on whiff (only after it was blocked) | the move's `.tres` | `FakeoutPurpleOnWhiff` off | on (Basic Bro 22C: off) |
| Make a move Risky (or forbid Fakeout) | the move's `.tres` | `Fakeout` = Risky (4) / None (1) | Auto |
| Make a super | the move's `.tres` | `SuperGaugeRequired` = 5000 (Auto then treats it as a Super) | — |
| Block part of a move | the move's `.tres` | `FakeoutBlockedFrames` (frame ranges of its state) / `FakeoutBlockedStates` (follow-up states) | — |
| How early the press can be | `FakeoutSettings.tres` | `InputBufferFrames` | 3 |
| Air Fakeout keeps momentum / stops | `FakeoutSettings.tres` | `KeepAirMomentum` | true |
| Where it puts you | `FakeoutSettings.tres` | `GroundState` / `AirState` (point at a dedicated Fakeout state if one is made) | 0 Idle / 6 Falling |
| Ring look | `VFX_19`–`22.tscn` | ring colors, sizes (`Ring` grows to 1.1, `Ring2` to 0.8, `Glow` to 0.7), `Duration` | 24 frames |
| Ring height | `FakeoutSettings.tres` | `VFXHeight` (6000 = middle of the body) | 6000 |
| Red X look / height | `VFX_23.tscn` / `FakeoutSettings.tres` | Label3D text and color / `FailVFXIndex`, `FailVFXHeight` (-1 index = no X) | red "X" / 23, 18500 |
| Notification text | `HitNotifs.tres` | elements 14–17 | Blue / Red / Purple / Gold Fakeout |
| Turn it off | `FakeoutSettings.tres` | `Enabled` | true |

## Adding it to another fighter
Nothing to do: it's engine-level and uses the shared VFX and notifications. Per fighter, only mark moves: `Fakeout = Risky` on invincible reversals and command grabs, `SuperGaugeRequired = 5000` on supers, `None` on anything that must never be cancelled, and `FakeoutBlockedFrames` / `FakeoutBlockedStates` on the parts of a move that must not be (command grab startup and throw animation, for example).

## Known issues / gotchas
- **No special / super moves on Basic Bro yet**, so Blue, Purple and Gold can only be tested on the Dummy's 236A / 623C / 236B+C for now.
- **A+B+C overlaps other inputs:** if a Fakeout isn't possible, the press still counts for any cancel the move allows (e.g. into a C normal). In neutral, A+B+C is just a normal press (no Fakeout, no X).
- **The red X shows 3 frames after the press** (when the input buffer runs out), not on the exact press frame.
- **Holding A+B (Barrier) and pressing C during a move** counts as A+B+C and can Fakeout.
- **No dedicated Fakeout animation:** you go straight to Idle / Falling. A short Fakeout state (pose + a few frames) can be added later by pointing `GroundState` / `AirState` at it.
- **No state flash on purpose:** blue is reserved for armor and purple for poison, so the color is shown by the ring only.

## Change log
| Date | Change |
|---|---|
| 2026-11-07 | `FakeoutPurpleOnWhiff` per move: off = a Risky move only gets Purple once it has been blocked (Basic Bro 22C) |
| 2026-11-05 | Rings made smaller (about the fighter's size), centered on the body, normal blending so each ring shows its Fakeout's color |
| 2026-11-05 | Slowdown removed; air Fakeouts keep momentum; red X on a failed Fakeout (VFX 23); FakeoutBlockedFrames / FakeoutBlockedStates; universal throws set to None; Test Fakeout Risky blocks frames 10–17 |
| 2026-11-05 | Created: Blue / Red / Purple / Gold Fakeouts, opponent slowdown, ring VFX 19–22, notifications 14–17, MoveSettings.Fakeout, Dummy test moves 236A / 623C / 236B+C |
