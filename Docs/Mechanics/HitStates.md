# Crumple, Trip, Spin, Launch and KnockedBack (hit states 74–79)

**Status:** 🧪 Testing (added 2026-11-01) · **Fighters:** all (universal states 74–79, delivered by the stamp) · **Rollback-safe:** Yes (data only)

## What it does
Five extra hit reactions a hitbox can put the opponent in. All five use the normal hitstun rules (hitstun from the hitbox, combo scaling, hitstun decay, counter-hit bonuses) and all five end lying down unless the victim air recovers: Crumple, Trip and Spin face down in **Hit_Down_FaceDown (78)**, Launch and KnockedBack face up in **Hit_Down (26)**, then get up through the usual tech / wakeup timeline (see [TechRecovery.md](TechRecovery.md)).

- **Hit_Crumple (74):** a very long standing stun. The fighter reels in place, then collapses face down into Hit_Down_FaceDown (78). Long enough to walk up and combo.
- **Hit_Trip (75):** feet swept out. A short hop, then they land **face down** in Hit_Down_FaceDown (78). A heavier sweep is just bigger launch / hitstun numbers on the hitbox, not another state.
- **Hit_Spin (76):** an airborne spin from launchers and specials. Juggleable; falls and lands face down in Hit_Down_FaceDown (78).
- **Hit_Launch (77):** knocked straight up by an anti-air launcher (a character's 2C). Juggleable, but the victim can **air recover once hitstun ends**, so the attacker has to keep the combo going; if they don't tech, they land in Hit_Down.
- **Hit_KnockedBack (79):** blown far backwards on a low arc by a heavy hit. Juggleable while it flies, can **wall bounce** if the hitbox arms one, and lands face up in Hit_Down.

## How it works (flow)
```
hit with reaction slot 13 ──► 74 Hit_Crumple ──(state ends = hitstun ends)──────────► 78 Hit_Down_FaceDown ──► tech / roll / 27 Hit_GetUp ──► 0 Idle
hit with reaction slot 11/12 ► 75 Hit_Trip ──(lands)───────────────────────────────► 78 Hit_Down_FaceDown ──► same tech / wakeup as 26
hit with reaction slot 5/6 ──► 76 Hit_Spin ──(lands)───────────────────────────────► 78 Hit_Down_FaceDown
hit with reaction slot 17 ───► 77 Hit_Launch ─(lands)───────────────────────────────► 26 Hit_Down (face up)
hit with reaction slot 18 ───► 79 Hit_KnockedBack ─(lands)──────────────────────────► 26 Hit_Down (face up)
                                   ├─(A/B/C after hitstun, BASIC hitstun only)──────► 13 Air_Recovery
                                   │  (75, 76, 77 and 79)
                                   ├─(touches a wall while a wall bounce is armed)──► 28 Hit_WallBounce
                                   └─(lands while a ground bounce is armed)─────────► 29 Hit_GroundBounce
```
- **Crumple** is built like Hit_Stand_Strong (21): a ground state (`BaseStance` 0) with `HitstunHold = 2`. It freezes on frame 2 while most of the hitstun runs, then plays its last 28 frames (the collapse) so the state ends exactly when hitstun ends, and goes to 26. More hits during it are ordinary ground hits.
- **Trip**, **Spin**, **Launch** and **KnockedBack** are copies of Hit_Air (24): `BaseStance` 2 (counts as airborne, so follow-up hits use the attacker's *air* settings), gravity every frame, land into 78 face down (Launch and KnockedBack: 26), and the same wall / ground bounce checks as Hit_Air.
- The height of the hop / launch, the gravity and the hitstun all come from the **attacker's hitbox**, not the state.

## How a hitbox picks one
A hitbox's `GroundHitReaction`, `CrouchHitReaction` and `AirHitReaction` are **slot numbers**. The stance's `HitReactions` table turns a slot into a state. These states use the slots the project's slot table already reserved for them (see README → *Hit reaction slots*):

| Slot | Meaning | State |
|---|---|---|
| 5 | Spin (horizontal) | 76 Hit_Spin |
| 6 | Spin (vertical) | 76 Hit_Spin |
| 11 | Sweep 1 | 75 Hit_Trip |
| 12 | Sweep 2 (heavier sweep) | 75 Hit_Trip |
| 13 | Faint | 74 Hit_Crumple |
| 17 | Launch (anti-air launcher) | 77 Hit_Launch |
| 18 | Knocked back (sent far backwards) | 79 Hit_KnockedBack |

So to make any move crumple, trip or spin, set the slot on its hitbox; no state or code changes. The table lives on the Dummy's stance and the stamp copies it to every fighter.

**Which knockdown:** the hitbox's `GroundHitstunType` / `AirHitstunType` decides how they lie down: 2 KNOCKDOWN = soft (Emergency Tech, Neutral Tech, rolls), 3 HARD_KNOCKDOWN = no tech, gets up after `HardKnockdownFrames`. With 1 BASIC, Trip, Spin and Launch can be **air-recovered** after the hitstun (like any air hit); use 2 or 3 to make them land every time. Launch is meant to be used with 1 BASIC.

## Files
| File | What it is |
|---|---|
| `Fighters/Dummy/Universal/States/74_Hit_Crumple.tres` | Crumple state (template) |
| `Fighters/Dummy/Universal/AnimationData/74_Hit_Crumple_AnimationData.tres` | Collapse length (`Duration` 30, `HitstunHold` 2) + standing hurtbox |
| `Fighters/Dummy/Universal/States/75_Hit_Trip.tres` | Trip state (template) |
| `Fighters/Dummy/Universal/AnimationData/75_Hit_Trip_AnimationData.tres` | Standing hurtbox; `Duration` 60 (it waits for the landing, not the duration) |
| `Fighters/Dummy/Universal/States/76_Hit_Spin.tres` | Spin state (template) |
| `Fighters/Dummy/Universal/AnimationData/76_Hit_Spin_AnimationData.tres` | Same as Trip |
| `Fighters/Dummy/Universal/States/77_Hit_Launch.tres` | Launch state (template) |
| `Fighters/Dummy/Universal/AnimationData/77_Hit_Launch_AnimationData.tres` | Same as Trip |
| `Fighters/Dummy/Universal/States/78_Hit_Down_FaceDown.tres` | Lying face down after a trip: a copy of 26 Hit_Down (same 64f, same lying hurtbox, ends into 14 Neutral Tech) |
| `Fighters/Dummy/Universal/AnimationData/78_Hit_Down_FaceDown_AnimationData.tres` | Same as 26's |
| `Fighters/Dummy/Universal/States/79_Hit_KnockedBack.tres` | KnockedBack state (template) |
| `Fighters/Dummy/Universal/AnimationData/79_Hit_KnockedBack_AnimationData.tres` | Same as Trip |
| `Fighters/Dummy/Dummy_Data.tres` | `States` positions 74–79 point at the files above (were Reserved) |
| `Fighters/Dummy/Stances/0_Dummy_Stance_Main.tres` | `HitReactions` slots 5, 6 → 76 · 11, 12 → 75 · 13 → 74 · 17 → 77 · 18 → 79 (new slots) |
| `Fighters/<Name>/Universal/States/74–79_*.tres` | Each fighter's copies, written by the stamp |
| `Fighters/<Name>/<Name>_Stamp.tres` | **Animation Names**: `Hit_Crumple`, `Hit_Trip`, `Hit_Spin`, `Hit_Launch`, `Hit_Down_FaceDown`, `Hit_KnockedBack` → clip |
| `Scripts/FunctionFighter/Tools/UniversalStampConfig.cs` | `DefaultAnimationNames()` lists these states (clip name = state name) for new fighters |

### Dummy test moves
| Input | Move | State | Hitbox | Sends the opponent to |
|---|---|---|---|---|
| **4C** | Test Crumple | 108 | `Hitboxes/14_Hitbox_TestCrumple_Active.tres` | Hit_Crumple (slot 13), 70f hitstun, mid |
| **3C** | Test Trip | 109 | `Hitboxes/15_Hitbox_TestTrip_Active.tres` | Hit_Trip (slot 11), low, hop of 40,000 |
| **6C** | Test Spin | 110 | `Hitboxes/16_Hitbox_TestSpin_Active.tres` | Hit_Spin (slot 5), mid launcher, 95,000 |
| **2C** | Test Launch | 111 | `Hitboxes/17_Hitbox_TestLaunch_Active.tres` | Hit_Launch (slot 17), mid anti-air (tall box above the Dummy), 100,000 up, 36f hitstun, BASIC (air-techable after) |
| **6B** | Test KnockedBack | 112 | `Hitboxes/18_Hitbox_TestKnockedBack_Active.tres` | Hit_KnockedBack (slot 18), mid, (-120,000, 35,000), 40f hitstun, soft knockdown |

Move files: `Special/Moves/19_TestCrumple.tres`, `20_TestTrip.tres`, `21_TestSpin.tres`, `22_TestLaunch.tres`, `23_TestKnockedBack.tres`. States: `Special/States/108_TestCrumple.tres`, `109_TestTrip.tres`, `110_TestSpin.tres`, `111_TestLaunch.tres`, `112_TestKnockedBack.tres` (+ `Special/AnimationData/` of the same numbers). Inputs: `Fighters/Shared/Inputs/35_Input_4C.tres`, `36_Input_3C.tres`, `37_Input_6C.tres`, `38_Input_1C.tres`, `39_Input_6B.tres`; Test Launch uses the existing `17_Input_2C.tres`.
Test Hard Sweep moved from 2C to **1C** to free 2C for Test Launch. The 2C input also matches 1+C and 3+C, so in the stance's `Moves` Test Launch sits **before** Test Hard Sweep and Test Trip: on a tie the move further down the list wins, so 1C stays Hard Sweep and 3C stays Trip.

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Make a move crumple / trip / spin | that move's attack hitbox | `GroundHitReaction`, `CrouchHitReaction`, `AirHitReaction` = 13 / 11 or 12 / 5 or 6 | — |
| Total crumple time | the attack hitbox (Dummy: `14_Hitbox_TestCrumple_Active`) | `GroundHitstun` | 70 |
| Length of the collapse at the end of a crumple | `74_Hit_Crumple_AnimationData` (Dummy), then re-stamp | `Duration` (collapse = `Duration` − `HitstunHold`) | 30 (28 frames) |
| What crumple goes into | `74_Hit_Crumple.tres` → TransitionEvents → PlayStateAction | `StateIndex` | 78 Hit_Down_FaceDown |
| Face up or face down after trip / spin / launch / knocked back | `75`–`77`, `79` `_*.tres` → the landing PlayStateAction (OnGround + falling) | `StateIndex` | 78 (75, 76) · 26 (77, 79) |
| Trip hop height / distance | the attack hitbox (Dummy: `15_Hitbox_TestTrip_Active`) | `GroundHitKnockback` (X, Y), `GroundHitKnockbackGravity` | (-15000, 40000), 230000 |
| Heavier sweep | a second hitbox with slot 12 | bigger `GroundHitKnockback` Y and `GroundHitstun`, or `GroundHitstunType` 3 for a hard knockdown | — |
| Spin launch height | the attack hitbox (Dummy: `16_Hitbox_TestSpin_Active`) | `GroundHitKnockback` Y / `AirHitKnockback` Y, gravity | 95000 / 80000, 230000 |
| Allow air recovery out of trip / spin | the attack hitbox | `GroundHitstunType` / `AirHitstunType` = 1 BASIC | 2 (lands every time) |
| Launch height | the attack hitbox (Dummy: `17_Hitbox_TestLaunch_Active`) | `GroundHitKnockback` Y / `AirHitKnockback` Y, gravity | 100000 / 90000, 230000 |
| How long the attacker has before the victim can air recover | the launch hitbox | `GroundHitstun` / `AirHitstun` | 36 |
| Make Launch always land (no air recovery) | the launch hitbox | `GroundHitstunType` / `AirHitstunType` = 2 | 1 |
| Anti-air reach | the launch hitbox | `Center` / `Size` | (7000, 9000) / (8000, 12000) |
| How far KnockedBack sends them | the attack hitbox (Dummy: `18_Hitbox_TestKnockedBack_Active`) | `GroundHitKnockback` / `AirHitKnockback` X (more negative = farther), Y = arc height | (-120000, 35000) |
| KnockedBack splats on the wall | the KnockedBack hitbox | `BounceTime` > 0, `BounceXIntensity` > 0 (see Bounce.md) | 0 (off) |
| Soft vs hard knockdown on landing | the attack hitbox | `GroundHitstunType` / `AirHitstunType` = 2 or 3 | 2 |
| Hurtbox while crumpling / tripping / spinning | `74–76_*_AnimationData` (Dummy), then re-stamp | `Hitboxes` → the hurtbox | standing hurtbox |
| Basic Bro's clips | `BasicBro_Stamp.tres` → Animation Names, then re-stamp | `Hit_Crumple`, `Hit_Trip`, `Hit_Spin`, `Hit_Launch` | Idle, Falling, Falling, Falling (stand-ins) |

## Adding it to another fighter
1. Nothing to build: re-stamp the fighter (open its `<Name>_Stamp.tres`, click **Stamp Universal States**). States 74–79 and the slot table come from the Dummy.
2. In the stamp's **Animation Names**, point `Hit_Crumple`, `Hit_Trip`, `Hit_Spin` and `Hit_Launch` at the fighter's clips (or stand-ins), then stamp again.
3. A fighter stamped before this change has its old Animation Names list, which doesn't have the new keys; add them by hand (new stamp configs get them from `DefaultAnimationNames()`).

## Face-down knockdown (78)
Hit_Crumple, Hit_Trip and Hit_Spin end in **78 Hit_Down_FaceDown** instead of 26 so they can show a face-down clip (Hit_Launch still lands face up in 26). It works exactly like Hit_Down: the code treats both as "lying down" (`Locomotion.IsLyingState` in `Scripts/FunctionFighter/Mechanics/Locomotion.cs`, also used by `KO.cs` and `Training/Reversal.cs`), so Emergency Tech, Neutral Tech, rolls, the forced tech, hard knockdowns, OTG hits and training wakeup reversals all behave the same. The wakeup moves (`10_Ground_Recovery`, `27_Forward_Roll`, `28_Back_Roll`) accept both states in `IsSequenceFromStates`, and Burst ignores both in `IgnoreStates`.
- To send another state face down: point its landing `PlayStateAction` → `StateIndex` at 78.
- Wall / ground bounces out of a trip or spin still land in 26 (face up), since they land through Hit_WallBounce / Hit_GroundBounce.
- Getting up uses the normal 27 Hit_GetUp; there's no face-down get-up yet.
- If you change 26 (duration, hurtbox), change 78 the same way.

## Known issues / gotchas
- **Re-stamp needed:** until a fighter is re-stamped, its 74–79 are still Reserved and its slot table is the old one (slots 5/6/11/12 → Hit_Air, 13 → Hit_Stand_Strong, no slot 17: a slot-17 hit is clamped to the last slot, Hit_Thrown_Launch).
- **Launch while lying after a BASIC hit:** if the victim doesn't tech, they land in Hit_Down with a BASIC hitstun type. Wakeup works as a soft knockdown, but Training's frame meter shows the lying frames as hitstun rather than knockdown.
- **Crumple and hitstun under 28 frames:** with very short hitstun (heavy combo decay) the hold never starts and the state runs its full 30 frames, so the collapse can outlast the hitstun by a few frames. Hits in those frames count as INVALID like any hit after hitstun.
- **A crumpling fighter can be thrown** (it's a ground state), same as Hit_Stand_Strong.
- **No clips yet:** Basic Bro shows Idle for Crumple (the same stand-in Hit_Stand_Strong uses) and Falling for Trip and Spin. The Dummy is a capsule and shows nothing; use **F10** (hitbox viewer).
- **Kaede** keeps her own slot table; slots 5/6/11/12/13 there still go to her own states, and she has no slot 17.

## Change log
| Date | Change |
|---|---|
| 2026-11-04 | Added 79 Hit_KnockedBack (slot 18) and Dummy Test KnockedBack on 6B |
| 2026-11-03 | Hit_Crumple and Hit_Spin also hand off to 78 instead of 26 |
| 2026-11-03 | Added 78 Hit_Down_FaceDown; Hit_Trip lands into it instead of 26 |
| 2026-11-02 | Added 77 Hit_Launch (slot 17) and Dummy Test Launch on 2C; Test Hard Sweep moved to 1C |
| 2026-11-01 | Created: states 74–76 on the Dummy (template), slots 5/6/11/12/13 remapped, Dummy test moves 4C / 3C / 6C (states 108–110) |
