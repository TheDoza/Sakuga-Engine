# Universal State Layout + Stamp Tool

**Status:** ✅ Working (tested 2026-10-11) · **Template fighter:** Dummy · **Rollback-safe:** Yes (data only; the tool runs in the editor)

States **0–99 mean the same thing on every fighter** (0–73 used, 74–99 reserved for future universal mechanics). A fighter's own moves (normals, specials, supers) start at **100**.
The Dummy is the template: change a universal state on the Dummy, then re-stamp other fighters to copy the change.

## The layout
| # | State | # | State | # | State |
|---|---|---|---|---|---|
| 0 | Idle | 20 | Hit_Stand_Weak | 40 | Block_Stand_Enter |
| 1 | Walk_Forward | 21 | Hit_Stand_Strong | 41 | Blockstun_Stand |
| 2 | Walk_Backward | 22 | Hit_Crouch | 42 | Block_Stand_Exit |
| 3 | Jump_Neutral | 23 | Hit_Air_Weak | 43 | Blocking_Stand |
| 4 | Jump_Forward | 24 | Hit_Air | 44 | GuardBreak_Stand |
| 5 | Jump_Backward | 25 | Hit_GroundSlide | 45 | Block_Crouch_Enter |
| 6 | Falling | 26 | Hit_Down | 46 | Blockstun_Crouch |
| 7 | AirDash_Forward | 27 | Hit_GetUp | 47 | Block_Crouch_Exit |
| 8 | AirDash_Backward | 28 | Hit_WallBounce | 48 | Blocking_Crouch |
| 9 | *reserved* | 29 | Hit_GroundBounce | 49 | GuardBreak_Crouch |
| 10 | Crouch_Enter | 30 | Throw_Forward | 50 | Block_Air_Enter |
| 11 | Crouching | 31 | Throw_Forward_Hit | 51 | Blockstun_Air |
| 12 | Crouch_Exit | 32 | Throw_Back | 52 | Block_Air_Exit |
| 13 | Air_Recovery | 33 | Throw_Back_Hit | 53 | Blocking_Air |
| 14 | Ground_Recovery | 34 | Throw_Air | 54 | GuardBreak_Air |
| 15 | Barrier_Stand | 35 | Throw_Air_Hit | 55 | Throw_Reject_Ground |
| 16 | Barrier_Crouch | 36 | Throw_Air_Landing | 56 | Throw_Reject_Air |
| 17 | Barrier_Air | 37 | Hit_Thrown | 57 | Throw_Rejected_Ground |
| 18 | Burst | 38 | Hit_Thrown_Launch | 58 | Throw_Rejected_Air |
| 19 | *reserved* | | | | |
| | | 39 | *reserved (crumple, KO…)* | 59 | *reserved* |

| # | State | # | State |
|---|---|---|---|
| 60 | Run | 65 | Super_Jump_Forward |
| 61 | Run_Skid | 66 | Super_Jump_Backward |
| 62 | Step_Dash (used when the stance has `19_StepDash_Alt`) | 67 | Double_Jump_Neutral |
| 63 | Backdash | 68 | Double_Jump_Forward |
| 64 | Super_Jump_Neutral | 69 | Double_Jump_Backward |
| 70 | Roll_Forward | 71 | Roll_Back |
| 72 | Outro (round win + match win pose) | 73 | Habit (idle fidget) |
| 74–99 | *reserved* | | |

See [Locomotion.md](Locomotion.md) for how they behave.

Reserved slots point at `Fighters/Shared/Universal/States/Reserved.tres` (returns to Idle after 1 frame).

**Universal moves** (shared files in `Fighters/Shared/Universal/Moves/`, always first in a stance's move list):
0 Walk_Forward · 1 Walk_Backward · 2 Jump_Neutral · 3 Jump_Forward · 4 Jump_Backward · 5 AirDash_Forward · 6 AirDash_Backward · 7 Crouch · 8 Uncrouch · 9 Air_Recovery · 10 Ground_Recovery · 11 Throw_Forward · 12 Throw_Back · 13 Throw_Air · 14 Barrier_Guard · 15 Barrier_Guard_Crouch · 16 Barrier_Guard_Air · 17 Barrier_Fizzle · 18 Burst · 19 Run (or `19_StepDash_Alt`) · 20 Backdash · 21–23 Super_Jump (neutral / forward / back) · 24–26 Double_Jump (neutral / forward / back) · 27 Forward_Roll · 28 Back_Roll.
A fighter's own moves come after them (Dummy: Slide Kick, Test Mid, Test Wall Bounce, Test Ground Bounce, then the 2A–2D knockdown tests).

## Outro and Habit (72–73)
- **72 Outro** plays the fighter's `Outro` clip and holds its last frame (state type Locked, no movement). It's used for both the round-win pose and the match-win outro: point the fighter's Data → `VictoryPoses` (round win) and `Outros` (match win, with its quote in `FinalMessage`) at state 72. The stamp doesn't fill those two lists, so set them once per fighter.
- **73 Habit** plays the `Habit` clip once, then goes back to Idle. It acts like Idle (you can walk, attack, block out of it). Idle goes into it after **5 full Idle loops** while the round is running; any other action resets the count.
  - Opt-in per fighter: the fighter scene needs a CustomVariable node named **IdleLoops** under `Parameters/Variables` (added to the Variables list, MaxValue 99, Reset on). Without it the fighter never plays a habit. The Dummy doesn't have one.
  - Number of loops: Dummy's `0_Idle.tres` → TransitionEvents → 2nd event → CheckCustomVariableCondition → `Value` (5), then re-stamp. With Basic Bro's 72-frame idle, 5 loops = 6 seconds.
  - Rollback-safe: the counter is a CustomVariable, which is saved with the fighter.
- With **Model Scene** set, the stamp sets both states' Duration to their clip length (`Clip Length States`), so Habit returns to Idle exactly when its clip ends.

## Folder structure (per fighter)
```
Fighters/<Name>/
  <Name>_Data.tres            fighter data (States list, Stances, health, AI)
  <Name>_Locomotion.tres      movement + tech numbers (per fighter, survives re-stamping)
  Stances/                    stance (moves list, hit + block reaction tables)
  Hitboxes/                   hurtboxes and attack hitboxes
  Universal/States/           0-99, written by the stamp tool — don't hand-edit (except on the Dummy)
  Universal/AnimationData/    0-99 timing + hurtbox + clip name, written by the stamp tool
  Special/States/             100+, the fighter's own moves
  Special/AnimationData/
  Special/Moves/
```

## Stamping a new fighter (e.g. Basic Bro)
1. Make the fighter's folder and an empty data file: FileSystem → right-click `Fighters/BasicBro` → **Create New → Resource → DataContainer** → save as `BasicBro_Data.tres`. Set its **Profile**.
2. Make its hurtboxes in `Fighters/BasicBro/Hitboxes/` (stand, crouch, lying) — copy the Dummy's three and resize them. Copy the Dummy's two throw boxes too (`7_Hitbox_Throw_Active`, `8_Hitbox_AirThrow_Active`) and resize their hurtbox.
3. Create the config: right-click the folder → **Create New → Resource → UniversalStampConfig** → save as `BasicBro_Stamp.tres`.
4. In the Inspector set:
   - **Template Data** = `Fighters/Dummy/Dummy_Data.tres`
   - **Target Data** = `BasicBro_Data.tres`, **Target Folder** = `res://Fighters/BasicBro`
   - **Stand / Crouch / Lying Hurtbox** = his three hurtboxes
   - **Ground / Air Throw Box** = his two throw boxes (leave empty to share the Dummy's)
   - **Use Animations** on; check **Animation Names** (state name → clip name; defaults follow the Kaede clip-name list)
   - **Model Scene** = the fighter's `.glb` and **Match Loop Durations** on: every state that loops itself (state ends → go to frame 0: Idle, Walk Forward/Backward, Crouching, Barrier Stand/Crouch/Air) gets its Duration set to its clip's length, so loops don't pop. Recalculated on every stamp.
5. Click **Stamp Universal States**. The Output panel reports what it wrote.
6. Make his scene: duplicate `Fighters/Dummy/Instances/Dummy_Fighter_Default.tscn`, set **Data** to `BasicBro_Data.tres`, swap the model, and add him to `Roster.tres`.

**Re-stamping** is safe: it rewrites states 0–99 and the universal moves, and keeps the fighter's own states (100+) and moves.
If the fighter had states before its first stamp, they're moved to 100+ and the tool warns you to update their numbers.
A fighter stamped under the old 0–59 layout has its own states moved from 60+ to 100+ automatically, and its own moves' **Move State** raised by 40. Any `PlayStateAction` in its own states that points at another own state still needs updating by hand (the Output panel reminds you).
The stamp also copies the Dummy's movement numbers to `<Folder>/<Name>_Locomotion.tres` (only if the fighter doesn't have one), so you can tune each fighter's run and jumps without re-stamping resetting them.
If the fighter has no AI data, it borrows the Dummy's until you give it its own.

Loop lengths: with **Model Scene** set, self-looping states get Duration = clip length in frames (Output says how many matched, and lists any clip it couldn't find). Without it, they keep the Dummy's Duration (Idle 60, Walk 60…), and a clip of a different length pops when the state restarts.

## What the tool does not copy
- Walk speed and air dash distance are copied exactly from the Dummy (re-stamping resets them). Run, jump, super jump, double jump, step dash and backdash numbers live in the fighter's own `<Name>_Locomotion.tres` and survive re-stamping.
- The fighter scene (`.tscn`) and roster entry — step 6 above.

## Test result
A fighter stamped from the Dummy (with its own copies of the hurtboxes) ran all 16 checks in `TestingGuide.md` frame-for-frame identically to the Dummy, both when stamped from a script and from inside the Godot editor.

## Old → new Dummy numbers (2026-10-11 migration)
| Old | New | Old | New | Old | New |
|---|---|---|---|---|---|
| 9 Hit_GroundSlide | 25 | 14–18 Stand block | 40–44 | 34–36 Crouch | 10–12 |
| 10 Hit_Air | 24 | 19–23 Crouch block | 45–49 | 37 Air_Recovery | 13 |
| 11 Hit_Down | 26 | 24–28 Air block | 50–54 | 38 Ground_Recovery | 14 |
| 12 Hit_GetUp | 27 | 30–33 Hitstun | 20–23 | 39–40 Bounces | 28–29 |
| 13 Slide Kick | 60 | 29 Test Mid | 61 | 41–42 Bounce tests | 62–63 |

## 0–59 → 0–99 (2026-10-12)
The Dummy's own states moved: 60 Slide Kick → **100**, 61 Test Mid → **101**, 62–63 bounce tests → **102–103**. Their file names still start with 60–63; only the position in the States list changed (the list position is what counts).

## Change log
| Date | Change |
|---|---|
| 2026-10-11 | Layout created; Dummy migrated; UniversalStampConfig tool added |
| 2026-10-12 | Wakeup rolls: states 70–71, moves 27–28; 14 Ground_Recovery is now Neutral Tech (32F); 26 Hit_Down lasts up to 64F (see TechRecovery.md) |
| 2026-10-12 | Universal block expanded to 0–99; locomotion states 60–69, moves 19–26; own states from 100; stamp copies `Locomotion` |
| 2026-10-12 | Burst added: state 18, move 18 |
| 2026-10-12 | Barrier added: states 15–17, moves 14–17 |
| 2026-10-20 | States 72 Outro and 73 Habit; Idle counts its loops into IdleLoops and plays Habit after 5; stamp `ClipLengthStates` |
| 2026-10-20 | Loop lengths: `ModelScene` + `MatchLoopDurations` set self-looping states' Duration from the clip length |
| 2026-10-11 | Throws added: states 30–38 and 55–58, moves 11–13; stamp tool copies throw boxes, rejected states and throw clip names; re-stamping no longer prints "another resource is loaded" errors |
