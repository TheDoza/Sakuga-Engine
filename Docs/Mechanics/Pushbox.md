# Pushboxes

**Status:** ✅ Working (tested 2026-10-13) · **Fighters:** All fighters (engine-level + per-fighter default) · **Added:** 2026-10-13 · **Rollback-safe:** Yes (data only; positions are already saved)

## What it does
Fighters are solid: you can't walk through the opponent. Walking into them pushes them back, and in the corner you just stop against them. Jumping over them crosses up in open space, but **not in the corner**: you can't jump, super jump, double jump or air dash over a cornered opponent and land behind them. The cornered fighter can still jump out over you. The start of a wakeup roll passes through (see [TechRecovery.md](TechRecovery.md)).

## How it works (flow)
```
Every frame, for each pair of fighters:
  pushboxes overlap? --yes--> push both apart by half the overlap each
                              (a fighter against a wall doesn't move; the other one takes the whole push)
  either one has IgnorePushbox (wakeup roll's first frames) --> skip
  one is cornered (less room between it and the wall than the other's pushbox width)
      and it isn't jumping out (in the air above the other one)
      --> corner rule: its pushbox counts as reaching all the way up; the other fighter is
          always kept on the open side, however high it is
```
Which pushbox a fighter has this frame:
1. The current hitbox settings' **Pushbox** (`HitboxSettings.Pushbox`): normal case, can change per state and per frame.
2. If that's empty, or the state has no hitbox settings at all: the fighter's **DefaultPushbox** (`<Fighter>_Data.tres`).
3. If that's empty too: no pushbox (the engine's original behaviour, fighters overlap).

To make a state pass through on purpose (a teleport, a cross-under), give its hitbox settings the **Pushbox_None** preset (size 0 × 0). Don't leave the Pushbox empty, since that now means "use the default".

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/HitboxPresets/Pushbox_Idle.tres` | Standing / airborne pushbox: Center (0, 6000), Size (4000, 12000) |
| `Fighters/Shared/HitboxPresets/Pushbox_Crouch.tres` | Crouching / lying pushbox: Center (0, 3000), Size (4000, 6000) |
| `Fighters/Shared/HitboxPresets/Pushbox_None.tres` | Size (0, 0): passes through on purpose |
| `Fighters/<Fighter>/Hitboxes/*.tres` → `Pushbox` | Each hitbox settings' own pushbox |
| `Fighters/<Fighter>/<Fighter>_Data.tres` → `DefaultPushbox` | Fallback when a hitbox has none `[FF PATCH: pushbox]` |
| `Scripts/SakugaEngine/Collision/PhysicsBody.cs` → `UpdateColliders` | Picks the pushbox `[FF PATCH: pushbox]` |
| `Scripts/SakugaEngine/Collision/PhysicsWorld.cs` → `Simulate` | Pushes overlapping fighters apart (engine) |
| `Scripts/SakugaEngine/Collision/PhysicsWorld.cs` → `ResolveCorner`, `JumpingOut` | Corner rule: no jumping over a cornered fighter `[FF PATCH: pushbox]` |
| `Scripts/SakugaEngine/Resources/DataContainer.cs` → `DefaultPushbox` | The fallback field `[FF PATCH: pushbox]` |

Dummy hitbox settings and their pushbox:

| Hitbox | Pushbox |
|---|---|
| 0 Basic, 2 Slide Kick, 4 Test Mid, 5 Test Wall Bounce, 6 Test Ground Bounce, 7 Throw, 8 Air Throw, 9 Burst | Pushbox_Idle |
| 1 Lying, 3 Crouch, 10–13 (2A–2D) | Pushbox_Crouch |
| State 27 Hit_GetUp (no hitbox settings) | DefaultPushbox (Pushbox_Idle) |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Make every fighter wider / narrower | `Shared/HitboxPresets/Pushbox_Idle.tres` (and `_Crouch`) | `Size.X` | 4000 (fighters stop 4,000 apart) |
| Give one fighter its own body width | Make a copy of the preset in `Fighters/<Fighter>/`, set it on that fighter's hitboxes and `DefaultPushbox` | `Size` | — |
| Change the pushbox for one move (e.g. a lunge that pushes further) | That move's hitbox settings | `Pushbox` | Pushbox_Idle |
| Let a move pass through the opponent | That move's hitbox settings | `Pushbox` → `Pushbox_None.tres` | — |
| Change the fallback | `<Fighter>_Data.tres` | `DefaultPushbox` | Pushbox_Idle |
| How long a wakeup roll passes through | `<Fighter>_Locomotion.tres` | `RollPassThroughFrames` | 10 |

See the pushboxes in Training with **F10** (hitbox view): the pushbox is the box drawn around the feet-to-head area.

## Adding it to another fighter
1. Set `DefaultPushbox` in `<Fighter>_Data.tres` to `Pushbox_Idle.tres` (or the fighter's own box).
2. On each hitbox settings file, set `Pushbox`: Idle for standing / air states, Crouch for crouching and lying. Anything you forget falls back to `DefaultPushbox`, so the fighter is never walk-through by accident.

## Known issues / gotchas
- An empty `Pushbox` now means "use the default", not "no pushbox". Use `Pushbox_None` to pass through on purpose.
- Lying fighters keep a low pushbox (you can't walk through someone who's down). That's a design choice, and it matches the engine's sample fighter. To allow it, set the Dummy's `1_Hitbox_Lying.tres` Pushbox to `Pushbox_None`.
- Corner rule edge: the cornered fighter jumping straight up can still be walked under (it counts as jumping out). Most games allow this; say if you want it blocked too.
- The Dummy's hurtboxes sit 5,000 units in front of its centre (Center X 5000, Size X 12000), so they reach far forward and barely behind. That's separate from the pushbox, but it affects cross-ups. Worth tidying when hit states are reworked.

## Tests
FeatureTest `pushbox:` checks: walking in pushes and never passes through; a crouching dummy is still solid; a cornered dummy stays in the corner; jumping over crosses up midscreen; a forward roll passes through; an empty Pushbox falls back to `DefaultPushbox`; no jump, super jump, double jump or air dash over a cornered dummy (right and left wall); the cornered fighter can jump out over the opponent.

## Change log
| Date | Change |
|---|---|
| 2026-10-14 | Corner rule: can't jump over a cornered fighter (the cornered fighter can still jump out) |
| 2026-10-13 | Created. Dummy had no pushboxes (fighters walked through each other): added them, plus the `DefaultPushbox` fallback and `Pushbox_None` preset |
