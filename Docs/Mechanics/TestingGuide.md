# Testing in Training Mode

Pick **Dummy vs Dummy** in Training, press **F10** for hitboxes, and play both sides from one keyboard.
To see state names, open `GameModes/GM_Training.tres` and tick `AllowShowDebugElements`.

| | Move | A | B | C | D |
|---|---|---|---|---|---|
| Player 1 | W A S D | U | I | J | K |
| Player 2 | Arrow keys | Numpad 4 | Numpad 5 | Numpad 1 | Numpad 2 |

P1 test moves: **A** Test Wall Bounce · **B** Test Mid · **C** Slide Kick · **D** Test Ground Bounce · knockdown tests (hold S = down): **2A** Test Launcher (air hit, air-techable) · **2B** Test Air Knockdown (soft knockdown: Emergency Tech / rolls) · **2C** Test Hard Sweep (low, hard knockdown: no tech) · **2D** Test Fast Knockdown (pops and lands right away: wakeup / meaty practice) · **B+C** throw (**4+B+C** back throw, **B+C** in the air = air throw). P2 faces left, so P2's "back" is the **Right arrow**.
No numpad? Remap `k2_face_a`…`k2_face_d` in Project → Project Settings → Input Map.
Macro buttons: **E** = A+B, **F** = B+C, **G** = A+B+C, **H** = A+B+C+D (P1 keyboard: O / P / L / ; · P2 keyboard: Numpad 6 / 3 / 0 / . by default).
Training Settings: pause → **Training**. The steps below assume **Dummy: Controller** (the default), so player 2 plays the dummy.

The checks are grouped in the same order as the mechanics reference.

## Controls & Movement

- [ ] **Macros:** P2 holds Right + Numpad 6 (E = A+B) → barrier. P1 next to P2 presses P (F = B+C) → throw. P2 presses Numpad . (H = A+B+C+D) while getting hit → Burst.
- [ ] **Easy Specials:** in the character select input menu, a gamepad's list ends with **Easy Specials Off/On** (accept flips it; does nothing yet).
- [ ] **Controls in the pause menu:** pause → Controls → both players' mapping menus open; Back closes each; the pause menu returns.
- [ ] **Disable a button:** in Controls, pick A+B, press Accept, hold Right → "Disabled"; in game that button does nothing. Map it again to bring it back.
- [ ] **Run:** P1 taps D, D and keeps holding → runs and speeds up; let go → Run_Skid slides to a stop. D, S+D, D (6356) also runs; D, A, D (6536) doesn't.
- [ ] **Skid can't guard:** while P1 skids, P2 presses B → P1 is hit.
- [ ] **Barrier Brake:** while running, P1 holds A + U + I (back + A+B) → straight into barrier and slides a long way. Also: let go and tap A (skid), then U + I → barrier keeps the skid's speed.
- [ ] **Running attack / throw:** run, press I (B) → the attack slides forward with the run's speed. Same with I + J (throw).
- [ ] **Backdash:** P1 taps A, A → hops back; P2's B passes through the start of it, but hits the end.
- [ ] **Prejump:** P2 throws (B+C) just as P1 presses W → the throw whiffs (4 frames on the ground, throw invincible).
- [ ] **Super jump:** S, then W → noticeably higher, with faint blue afterimages.
- [ ] **Double jump:** jump, then press W again in the air → second jump. Then air dash (D, D) → nothing (one air action). Air dash first → no double jump after.
- [ ] **Cross-up:** jump over P2 → P1 keeps facing the same way; double jump on the other side → P1 turns around.
- [ ] **Dash jump:** run, then W+D → goes much farther than a normal jump. Double jump forward keeps the speed, up drifts forward, back still goes back but only a little.
- [ ] **Air dash lines:** air dash (D, D in the air) → white speed lines behind P1; air backdash (A, A) → lines in front.
- [ ] **Pushbox:** P1 walks into P2 → P1 stops and slowly pushes P2 back; never walks through. Same with P2 crouching. In the corner, P2 doesn't move and P1 stops against it. F10 shows the pushbox.
- [ ] **Pushbox cross-up:** P1 jumps over P2 from close → lands on the other side. Knock P2 down with C and have it forward roll → it rolls through P1.
- [ ] **Step Dash:** swap move 19 in the Dummy's stance to `19_StepDash_Alt.tres` → D, D now does a short hop instead of a run (swap back afterwards).

## Defense

- [ ] **Stand block:** P2 holds Right; P1 presses B → Blockstun_Stand, chip damage, pushback.
- [ ] **Low beats stand block:** P2 holds Right; P1 presses C → P2 is hit and slides.
- [ ] **Crouch block:** P2 holds Down + Right; P1 presses C, then B → both blocked; P2 ends in Crouching.
- [ ] **Crouching:** P2 holds Down → Crouching; releases → Crouch_Exit → Idle.
- [ ] **Barrier gauge:** a blue bar labelled BARRIER sits under each health bar; it turns yellow below half.
- [ ] **Barrier block:** P2 holds Right + Numpad 4 + Numpad 5 (back + A+B); P1 presses B → cyan shield flash, no chip damage, both fighters slide apart (P1 slides back too), P2's bar drops.
- [ ] **Neutral barrier:** P2 holds Right + Numpad 4 + Numpad 5 with nothing coming → P2 stands still in barrier, the bar drains, no A/B attack comes out. Let go of Numpad 4/5 → walks back again.
- [ ] **Crouch / air barrier:** same while holding Down + Right → crouching barrier; jump and hold it → air barrier, and P2 lands still barriering.
- [ ] **Corner:** put P2 against the right wall and repeat → P2 stays, P1 slides back the full distance.
- [ ] **Danger:** hold the neutral barrier for about 16 seconds until the bar empties → "Danger" pops up, the bar turns dark red with DANGER under it, chip is back and hits do 1.5x damage. After 15 seconds the bar is full again and barrier works.
- [ ] **Barrier shield:** while P2 holds Right + Numpad 4 + Numpad 5, a green magic-circle shield shows in front of P2.
- [ ] **Barrier without back:** P2 holds Right + Numpad 4 + 5, then lets go of Right (keeps 4 + 5) → still barriering; P1 presses B → blocked, no chip, back to barrier afterwards.
- [ ] **Burst gauge:** an orange round "BURST" icon sits under each portrait (full at match start).
- [ ] **Burst:** P1 hits P2 with B; while P2 is reeling, P2 presses Numpad 4 + 5 + 1 + 2 (A+B+C+D) → explosion, P1 flies away and lands in a knockdown, P2's icon shows 0 and starts refilling; P1's icon is crossed out until P1 gets up.
- [ ] **Burst needs the full gauge:** right after, repeat → nothing (the icon isn't orange yet).
- [ ] **Block the Burst:** the attacker has to be out of recovery and holding back (or barrier) when the Burst comes out, e.g. they stop their combo early. The Dummy's test moves leave the attacker in recovery, so this is covered by the automated checks rather than by hand for now.
- [ ] **Burst carries over:** in a versus match (Training has no rounds), Burst in round 1, then start round 2 → the icon is still low.
- [ ] **Burst refill:** after a Burst, the icon takes about 33 seconds to fill at full health (half the old speed).

## Hits & Counters

- [ ] **Hitstun:** P1 presses B → Hit_Stand_Strong for 18 frames, then Idle.
- [ ] **Crouch hit:** P2 holds Down; P1 presses B → Hit_Crouch → Crouching.
- [ ] **Counter Hit:** P2 presses B, P1 presses B just before → "Counter", red flash, 440 damage.
- [ ] **Big Counter:** P2 presses B, P1 presses A just before → "Big Counter".
- [ ] **Functional Counter:** Training Settings → Dummy Barrier: **Danger**, Counter Hit: **Always Big Counter**. P1 presses B → "Functional Counter", gold burst; P1 auto-dashes forward after the attack; hits stun much longer; after 5 s the next hit shows a green spark and "Functional End". Functional Counter: **Off** → it never starts.

## Knockdowns & Recovery

- [ ] **Slide:** P1 presses C → P2 slides into Hit_Down, then techs up on its own at frame 64.
- [ ] **Slide re-hit:** P1 presses C, walks forward, presses C again during the slide → P2 launches (Hit_Air).
- [ ] **Air tech:** P2 jumps in; P1 presses B; after the hit P2 taps Numpad 4 → Air_Recovery hop.
- [ ] **No air tech:** same, P2 presses nothing → lands on its feet.
- [ ] **Ground tech:** P1 presses C; P2 waits about half a second after landing, then taps Numpad 4 → Neutral Tech. Tapping it during the first moment lying down (frames 9–23) does nothing.
- [ ] **Wall bounce:** both near the right wall, P1 on the left; P1 presses A → wall bounce → Hit_Down. Numpad 4 does nothing (hard knockdown).
- [ ] **Ground bounce:** P1 presses D → P2 pops up, bounces off the floor → Hit_Down (ground tech works).
- [ ] **Air spike:** P2 jumps in; P1 presses D → spiked down, bounces.
- [ ] **Emergency Tech:** P1 presses C; P2 presses Numpad 4 just before or as it lands → it gets straight up (no lying down).
- [ ] **Forced tech:** P1 presses C, P2 does nothing → it lies about a second, then techs on its own.
- [ ] **Rolls:** after a knockdown, P2 holds Left (toward P1) + Numpad 4 → Forward Roll, rolls through P1 to the other side. Right + Numpad 4 → Back Roll away.
- [ ] **Knockdown moves:** 2A → P2 can air tech. 2B → P2 lands lying, Emergency Tech / rolls work. 2C → crouch-block it; unblocked P2 can't tech and just gets up. 2D → P2 is down almost instantly.
- [ ] **INVALID combo:** 2B, then hit P2 while it lies on the ground (C) → the combo counter turns red and says INVALID (any mode).
- [ ] **Hold to tech:** P2 holds Numpad 4 before getting hit by 2A → air techs the instant it can. Holding it through a 2B landing → Emergency Tech.

## Throws

- [ ] **Throw:** walk P1 right next to P2 and press B+C → green **!** over P2, then P2 flies away, hard knockdown, 1,500 damage. The B/C normals don't come out.
- [ ] **Back throw:** hold A (back) and press B+C → P2 lands behind P1.
- [ ] **Throw reject:** P1 throws; while the **!** is showing, P2 presses Numpad 5 + Numpad 1 (B+C) → "Escaped", P1 slides back about half a screen, P2 stays put, both can act on the same frame.
- [ ] **Too late:** press B+C after the **!** disappears → still thrown.
- [ ] **No throw combos:** P1 presses C (slide), walks up and presses B+C while P2 is still sliding → the throw whiffs (P2 is in hitstun).
- [ ] **Throw beats blocking:** P2 holds Right (back); P1 throws → P2 is thrown.
- [ ] **Air throw:** both jump; P1 presses B+C near P2 at the same height → P2 is thrown, hard knockdown. Whiff it and P1 has a short landing recovery.
- [ ] **Air reject:** same, P2 presses B+C during the **!** → both pop up a little and fall; P1 is pushed back.
- [ ] **Knockdown can't be teched:** after any throw, P2 mashing A does nothing until it stands up.
- [ ] **No tech after throws:** throw P2, mash Numpad 4 → it just gets up.

## Visual Feedback

- [ ] **State flashes:** Counter Hit setting Always Counter → P2 flashes red when hit. Backdash / rolls / techs flash white.
- [ ] **Advantage hitsparks:** P1 presses B on a blocking P2 → the block effect plus a red X-shaped hitspark (minus) on the impact frame. On hit: the hitspark replaces the old hit effect. Training Display → Advantage Hitsparks Off → the old hit effect comes back. Any mode (Versus) shows the hitsparks too.
- [ ] **Hitspark size:** a hit that's −1 makes a small red spark; a very minus block (e.g. a big move blocked) makes a big dark-red one; even (0) is the normal yellow.

## Training Mode

- [ ] **Training data:** after a hit, the bottom-left panel lines up (names left, numbers right) and shows Advantage in green/red and the Counter type.
- [ ] **Dummy:** Dummy: Crouch / Jump / Stand → the dummy does it and ignores P2's keys. CPU → it moves and attacks on its own.
- [ ] **Guard:** Dummy: Stand, Guard: All → blocks P1's B standing and C crouching. After First Hit → the first B hits, the next one is blocked.
- [ ] **Recording:** press F8, walk with P1's keys (the dummy moves, P1 stays), press F8 again, then F9 → the dummy repeats it. Dummy: Playback → loops it.
- [ ] **Health / meters:** Health Refill Off → damage stays. Dummy Health 50% → refills to half. Barrier / Burst: Infinite → gauges stay full.
- [ ] **Reset:** Reset Position: Dummy Left Corner, then F11 → dummy against the left wall, P1 next to it.
- [ ] **Back button reset:** in Training, hold a direction and press Tab (P1 keyboard) or Back/Select (gamepad): 5 mid, 4 left corner P1 inside, 1 left corner dummy inside, 2 mid with sides switched, 3 right corner P1 inside, 6 right corner dummy inside.
- [ ] **Quick Character Select:** pause → press Up → P1 / Dummy row. Left/Right changes the name (orange), Accept reloads Training with that pair; your Training settings are unchanged.
- [ ] **Guard First Hit Only:** Dummy Stand, Guard First Hit Only. P1 presses B twice quickly → the first is blocked, the second hits. Knock P2 down with C and press B as it stands up → blocked again.
- [ ] **Block X Hits:** Guard Hits 2, Guard Block 2 Hits → B, B, B in a row: block, block, hit.
- [ ] **Fuzzy (Switch: Crouch > Stand):** the dummy crouches; P1 presses B (blocked crouching), then C right after → the dummy has stood up and the low hits.
- [ ] **Air Tech:** Air Tech Forward; P2 jumps (set Dummy Jump), P1 hits it with B → AIR TECH (FORWARD) popup, the dummy flips toward P1.
- [ ] **Reversal:** Reversal Action, Action 5B, On Block. Guard All. P1 presses B → after blocking, the dummy hits back instantly; blue REVERSAL popup. Delay +10 → noticeably later.
- [ ] **Wakeup reversal:** Action 8 Neutral Jump (or 4+AB Barrier) → knock the dummy down with C, it jumps (or barriers) as it gets up.
- [ ] **Burst reversal:** Action ABCD Burst, On Hit, Burst Infinite → the dummy bursts on the first hit.
- [ ] **Recording reversal:** record the dummy pressing B (F8, B, F8), set Reversal: Play Recording → after blocking, the dummy plays it at once.
- [ ] **Countdown:** F8 → RECORDING IN 3 / 2 / 1 (you can walk the dummy), then red REC.
- [ ] **Play on Reset:** record a walk, Play on Reset: After READY / GO! → F11: READY (nobody moves), GO!, the dummy walks.
- [ ] **Training Ground Tech:** set Ground Tech Emergency / Back Roll / Random → the dummy does it after every knockdown (blue popup).
- [ ] **Frame meter:** P1 presses B on a blocking P2 → bar fills: green startup, red active, blue recovery for P1; orange blockstun for P2; summary "Startup 8 Active 3 Recovery 16 Advantage …". Backdash / rolls show a white stripe on invincible frames.
- [ ] **Input display:** hold a direction and buttons → the ball moves and the buttons light up. Display → P1 / P2 Input Display turn each off.
- [ ] **Settings pages:** pause → Training: page name at the top; LB/RB or PgUp/PgDn switches pages.
- [ ] **Reset card:** the pause menu shows Reset & Reposition on the right; the frame meter and input display stay behind the menu.
- [ ] **Dummy movement:** Dummy → Run / Backdash / Jump Forward / Super Jump / Double Jump / Air Dash / Random Movement → the dummy does it on loop with a short pause. With Guard All it still blocks while walking.
- [ ] **Save states:** F5 → STATE SAVED. Move around, hit the dummy, F6 → everything snaps back. F6 with nothing saved → NO STATE SAVED. Bind a gamepad button to Save State / Load State in the button config (rows above Easy Specials) and use it in Training.
- [ ] **Frame advance:** in Training press F2 → FRAME ADVANCE at the top, everything stops. Hold right and tap F3 → P1 moves one frame per tap. Hold F3 → slow motion. F2 → back to normal.
- [ ] **Recording slots:** Recording → Slot 1, record a jump; Slot 2, record a low. Playback: Random Slot, Dummy: Playback → it mixes the two.
- [ ] **Refill After Combo:** Super Meter → Refill After Combo, Super Refill Amount 50 % → do a combo with a super; once things settle the meter is back at half.

