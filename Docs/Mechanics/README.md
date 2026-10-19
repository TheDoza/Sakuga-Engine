# Mechanics Reference

One page per gameplay mechanic: what it does, which files make it up, and which values to change.
Start here, then open the mechanic's page.

> The online copy is the "Sakuga Mechanics Reference" doc. This folder has a `.gdignore`, so Godot skips it. It's for you, not the game.

## Mechanics index

| Group | Mechanic | Status | Fighters | Page |
|---|---|---|---|---|
| Foundations | Universal layout + stamp tool | ✅ Working (tested 2026-10-11) | Dummy (template) | [UniversalLayout.md](UniversalLayout.md) |
| Foundations | Shared conventions | Reference | All | below |
| Foundations | Pushboxes (fighters are solid) | ✅ Working (tested 2026-10-13) | All fighters (presets + per-fighter default) | [Pushbox.md](Pushbox.md) |
| Controls & movement | Macro buttons, button config & Easy Specials | ✅ Working (tested 2026-10-12) | All fighters | [Controls.md](Controls.md) |
| Controls & movement | Locomotion (run, step dash, backdash, super / double jump) | ✅ Working (tested 2026-10-12) | All fighters (states 60–69, per-fighter numbers) | [Locomotion.md](Locomotion.md) |
| Defense | Blocking | ✅ Working (tested 2026-10-11) | Dummy | [Blocking.md](Blocking.md) |
| Defense | Crouching | ✅ Working (tested 2026-10-11) | Dummy | [Crouching.md](Crouching.md) |
| Defense | Barrier | ✅ Working (tested 2026-10-12) | All fighters (engine-level + states 15–17) | [Barrier.md](Barrier.md) |
| Defense | Burst | ✅ Working (tested 2026-10-12) | All fighters (engine-level + state 18) | [Burst.md](Burst.md) |
| Hits & counters | Basic hitstun | ✅ Working (tested 2026-10-11) | Dummy | [BasicHitstun.md](BasicHitstun.md) |
| Hits & counters | Clashes (hitbox meets hitbox) | ✅ Working (tested 2026-10-17) | All fighters (engine-level) | [Clash.md](Clash.md) |
| Hits & counters | Counter hits (Counter, Big, Functional) | ✅ Working (tested 2026-10-12) | All fighters (engine-level) | [Counter.md](Counter.md) |
| Knockdowns & recovery | Sliding knockdown | ✅ Working (tested 2026-10-05) | Dummy | [SlidingKnockdown.md](SlidingKnockdown.md) |
| Knockdowns & recovery | Wall & ground bounce | ✅ Working (tested 2026-10-11) | Dummy | [Bounce.md](Bounce.md) |
| Knockdowns & recovery | Tech & wakeup (Emergency / Neutral Tech, rolls, air recovery, INVALID combos) | ✅ Working (tested 2026-10-12) | All fighters (per-fighter numbers) | [TechRecovery.md](TechRecovery.md) |
| Throws | Throws & throw reject | ✅ Working (tested 2026-10-11) | Dummy (template) | [Throws.md](Throws.md) |
| Visual feedback | Visual indicators (state flashes, advantage hitsparks, punish / reversal, movement effects) | ✅ Working (tested 2026-10-17) | All fighters, all modes | [Visuals.md](Visuals.md) |
| Match flow | KO and Rematch screen (Rematch / Character Select / Leave Match) | ✅ Working (tested 2026-10-18) | All fighters | [MatchFlow.md](MatchFlow.md) |
| Training & testing | Training mode | ✅ Complete (tested 2026-10-12) | P2 is the dummy | [Training.md](Training.md) |
| Training & testing | How to test everything | Checklist | Dummy vs Dummy | [TestingGuide.md](TestingGuide.md) |

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
| 0 | Ground hit, weak | 17 | 20 Hit_Stand_Weak |
| 1 | Ground hit, strong | 18 | 21 Hit_Stand_Strong |
| 2 | Crouch hit | 19 | 22 Hit_Crouch |
| 3 | Air hit, weak (lands on feet) | 20 | 23 Hit_Air_Weak |
| 4 | Air hit, strong (knockdown) | 21 | 24 Hit_Air |
| 5 | Spin (horizontal) | 22 | 24 Hit_Air |
| 6 | Spin (vertical) | 23 | 24 Hit_Air |
| 7 | Falling | 24 | 24 Hit_Air |
| 8 | On the ground | 25 | 25 Hit_GroundSlide |
| 9 | Down | 26 | 26 Hit_Down |
| 10 | Get up | 27 | 27 Hit_GetUp |
| 11 | Sweep 1 | 28 | 24 Hit_Air |
| 12 | Sweep 2 | 29 | 24 Hit_Air |
| 13 | Faint | 30 | 21 Hit_Stand_Strong (placeholder) |
| 14 | Throw reaction (held, "!") | 61 | 37 Hit_Thrown |
| **15** | **Ground slide** *(new)* | ⚠️ **missing** | 25 Hit_GroundSlide |
| **16** | **Thrown launch** *(new: damage + hard knockdown)* | ⚠️ **missing** | 38 Hit_Thrown_Launch |

### 2. State numbers = position in the list (fixed universal layout)

`StateIndex` in a `PlayStateAction`, and `MoveState` in a move, mean "position in the fighter's
`States` list in `<Fighter>_Data.tres`", counting from 0. **States 0–99 are the universal layout and mean
the same thing on every fighter; a fighter's own moves start at 100.** Full table and the stamp tool:
[UniversalLayout.md](UniversalLayout.md).

### 3. Enum numbers you'll see in .tres files

| Field | Values |
|---|---|
| `Type` (state) | 0 NULL · 1 MOVEMENT · 2 COMBAT · 3 BLOCKING · 4 HIT_REACTION · 5 LOCKED |
| `BaseStance` | 0 NEUTRAL · 1 CROUCH · 2 ON_THE_GROUND *(counts as airborne!)* |
| `HitboxType` | 0 HURTBOX · 1 HITBOX · 2 PROXIMITY_BLOCK · 3 PROJECTILE · 4 THROW · 5 COUNTER · 6 DEFLECT |
| `HitType` | 0 HIGH · 1 MID · 2 LOW · 3 UNBLOCKABLE |
| `*HitstunType` | 0 NONE · 1 BASIC · 2 KNOCKDOWN · 3 HARD_KNOCKDOWN · 4 DIZZY · 5 STAGGERED · 6 GRABBED |
| `animationStage` | 0 STARTUP · 1 ACTIVE · 2 RECOVERY |
| `CompareMode` | 0 = · 1 ≠ · 2 > · 3 ≥ · 4 < · 5 ≤ |

### 4. Units

- **Time** is in frames (60 per second).
- **Distance:** the Dummy is 10,000 units tall. The stage runs from -75,000 to +75,000.
- **Velocity:** a velocity of 60,000 moves 1,000 units per frame. Negative X in a hitbox knockback = pushed **away** from the attacker.
- **Ground slowdown:** about 4,166 velocity per frame (engine default) once knockback time ends.

### 5. Move priority

When two moves' inputs match on the same frame, the one with the higher `Priority` comes out (walk 0, jump 1, air dash / super jump / double jump 2, run / step dash / backdash 3, normals 5, throws 6–7, barrier 7–9). Movement states don't hold the move slot, so any move can cancel a run or a jump once it's started.
On a tie, the move lower in the stance's `Moves` list wins. This is an `[FF PATCH]` in `StanceManager.cs`; see [Throws.md](Throws.md).

### 6. Engine patches

Every change to Sakuga's own C# is marked with a `[FF PATCH: ...]` comment. Search for `[FF PATCH` to find them all.
Current patches: blockstun, crouch block, block knockback Y (see [Blocking.md](Blocking.md)); no throw combos, throw reject, move priority, throw corner (see [Throws.md](Throws.md)); barrier, move conditions (see [Barrier.md](Barrier.md)); burst (see [Burst.md](Burst.md)); counter (see [Counter.md](Counter.md)); training, training reversal, frame advance (see [Training.md](Training.md)); macros, easy specials, disable buttons, pause controls, pause pages (see [Controls.md](Controls.md)); locomotion, landing (see [Locomotion.md](Locomotion.md)); tech, invalid combo (see [TechRecovery.md](TechRecovery.md)); advantage, state flash, indicators, combo display (see [Visuals.md](Visuals.md)); pushbox (see [Pushbox.md](Pushbox.md)).

### 7. Rollback rule of thumb

If a mechanic is built only from `.tres` data (states, hitboxes, existing actions/conditions), it's
automatically rollback-safe. If it adds a new C# variable that changes during a match, that variable
must also be saved/restored in `SakugaActor.SetStateData` / `GetStateData`. Note which kind each mechanic is.

**Rollback sync test (2026-10-13):** both fighters played 18,000 frames of random inputs (6 runs × 3,000), and every frame the game rolled back 8 frames and re-simulated them. The state came out byte-identical every time: 0 desyncs. The runs covered runs, dashes, all jumps, barrier, Burst, throws and throw rejects, blocks, counters, knockdowns, techs, rolls, and wall / ground bounces. So everything up to now is rollback-safe. Training-only helpers (dummy, recordings, save states, frame advance) are offline-only and aren't part of rollback. Re-run this check whenever a mechanic adds new C# state.
