# Mechanics Reference

One page per gameplay mechanic: what it does, which files make it up, and which values to change.
Start here, then open the mechanic's page.

> The online copy is the Claude doc "Sakuga Mechanics Reference". This folder has a `.gdignore`, so Godot skips it. It's for you, not the game.

## Mechanics index

| Mechanic | Status | Fighters | Page |
|---|---|---|---|
| Wall & ground bounce | ✅ Working (tested 2026-10-11) | Dummy | [Bounce.md](Bounce.md) |
| Tech recovery | ✅ Working (tested 2026-10-11) | Dummy | [TechRecovery.md](TechRecovery.md) |
| Crouching | ✅ Working (tested 2026-10-11) | Dummy | [Crouching.md](Crouching.md) |
| Basic hitstun | ✅ Working (tested 2026-10-11) | Dummy | [BasicHitstun.md](BasicHitstun.md) |
| Blocking | ✅ Working (tested 2026-10-11) | Dummy | [Blocking.md](Blocking.md) |
| Sliding knockdown | ✅ Working (tested 2026-10-05) | Dummy | [SlidingKnockdown.md](SlidingKnockdown.md) |
| Barrier | 📝 Planned | — | *(not started)* |

Status key: ✅ Working · 🧪 Testing · 🚧 In progress · 📝 Planned · ⛔ Shelved

**How to test everything in Training Mode:** [TestingGuide.md](TestingGuide.md)

To add a mechanic, copy [`_TEMPLATE.md`](_TEMPLATE.md), rename it, fill it in, and add a row above.

---

## Shared conventions (read before editing any mechanic)

### 1. Hit reaction slots

When a hitbox says `GroundHitReaction = 15`, that's a **slot number**, not a state number.
Each fighter's stance (`Stances/..._Stance_Main.tres` → `HitReactions`) maps slots to that fighter's own states.
**Every fighter must fill every slot**, or the engine clamps to the last slot and plays the wrong reaction.

| Slot | Meaning | Kaede state | Dummy state |
|---|---|---|---|
| 0 | Ground hit, weak | 17 | 30 Hit_Stand_Weak |
| 1 | Ground hit, strong | 18 | 31 Hit_Stand_Strong |
| 2 | Crouch hit | 19 | 32 Hit_Crouch |
| 3 | Air hit, weak (lands on feet) | 20 | 33 Hit_Air_Weak |
| 4 | Air hit, strong (knockdown) | 21 | 10 Hit_Air |
| 5 | Spin (horizontal) | 22 | 10 Hit_Air |
| 6 | Spin (vertical) | 23 | 10 Hit_Air |
| 7 | Falling | 24 | 10 Hit_Air |
| 8 | On the ground | 25 | 9 Hit_GroundSlide |
| 9 | Down | 26 | 11 Hit_Down |
| 10 | Get up | 27 | 12 Hit_GetUp |
| 11 | Sweep 1 | 28 | 10 Hit_Air |
| 12 | Sweep 2 | 29 | 10 Hit_Air |
| 13 | Faint | 30 | 31 Hit_Stand_Strong (placeholder) |
| 14 | Throw reaction | 61 | 0 (none yet) |
| **15** | **Ground slide** *(new)* | ⚠️ **missing** | 9 Hit_GroundSlide |

### 2. State numbers = position in the list

`StateIndex` in a `PlayStateAction`, and `MoveState` in a move, mean "position in the fighter's
`States` list in `<Fighter>_Data.tres`", counting from 0. Name new files with that number.
⚠️ The Dummy's existing files `12_Dummy_AirDash_Forward` and `13_Dummy_AirDash_Backwards` are
really at positions **7** and **8**. Ignore their prefixes.

### 3. Enum numbers you'll see in .tres files

| Field | Values |
|---|---|
| `Type` (state) | 0 NULL · 1 MOVEMENT · 2 COMBAT · 3 BLOCKING · 4 HIT_REACTION · 5 LOCKED |
| `BaseStance` | 0 NEUTRAL · 1 CROUCH · 2 ON_THE_GROUND *(counts as airborne!)* |
| `HitboxType` | 0 HURTBOX · 1 HITBOX · 2 PROXIMITY_BLOCK · 3 PROJECTILE · 4 THROW · 5 COUNTER · 6 DEFLECT |
| `HitType` | 0 HIGH · 1 MID · 2 LOW · 3 UNBLOCKABLE |
| `*HitstunType` | 0 NONE · 1 BASIC · 2 KNOCKDOWN · 3 HARD_KNOCKDOWN · 4 DIZZY · 5 STAGGERED |
| `animationStage` | 0 STARTUP · 1 ACTIVE · 2 RECOVERY |
| `CompareMode` | 0 = · 1 ≠ · 2 > · 3 ≥ · 4 < · 5 ≤ |

### 4. Units

- **Time** is in frames (60 per second).
- **Distance:** the Dummy is 10,000 units tall. The stage runs from -75,000 to +75,000.
- **Velocity:** a velocity of 60,000 moves 1,000 units per frame. Negative X in a hitbox knockback = pushed **away** from the attacker.
- **Ground slowdown:** about 4,166 velocity per frame (engine default) once knockback time ends.

### 5. Rollback rule of thumb

If a mechanic is built only from `.tres` data (states, hitboxes, existing actions/conditions), it's
automatically rollback-safe. If it adds a new C# variable that changes during a match, that variable
must also be saved/restored in `SakugaActor.SetStateData` / `GetStateData`. Note which kind each mechanic is.
