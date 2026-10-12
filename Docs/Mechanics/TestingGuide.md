# Testing in Training Mode

Pick **Dummy vs Dummy** in Training, press **F10** for hitboxes, and play both sides from one keyboard.
To see state names, open `GameModes/GM_Training.tres` and tick `AllowShowDebugElements`.

| | Move | A | B | C | D |
|---|---|---|---|---|---|
| Player 1 | W A S D | U | I | J | K |
| Player 2 | Arrow keys | Numpad 4 | Numpad 5 | Numpad 1 | Numpad 2 |

P1 test moves: **A** Test Wall Bounce · **B** Test Mid · **C** Slide Kick · **D** Test Ground Bounce. P2 faces left, so P2's "back" is the **Right arrow**.
No numpad? Remap `k2_face_a`…`k2_face_d` in Project → Project Settings → Input Map.

- [ ] **Slide:** P1 presses C → P2 slides, Hit_Down, gets up.
- [ ] **Slide re-hit:** P1 presses C, walks forward, presses C again during the slide → P2 launches (Hit_Air).
- [ ] **Stand block:** P2 holds Right; P1 presses B → Blockstun_Stand, chip damage, pushback.
- [ ] **Low beats stand block:** P2 holds Right; P1 presses C → P2 is hit and slides.
- [ ] **Crouch block:** P2 holds Down + Right; P1 presses C, then B → both blocked; P2 ends in Crouching.
- [ ] **Hitstun:** P1 presses B → Hit_Stand_Strong for 18 frames, then Neutral.
- [ ] **Crouching:** P2 holds Down → Crouching; releases → Crouch_Exit → Neutral.
- [ ] **Crouch hit:** P2 holds Down; P1 presses B → Hit_Crouch → Crouching.
- [ ] **Air tech:** P2 jumps in; P1 presses B; after the hit P2 taps Numpad 4 → Air_Recovery hop.
- [ ] **No air tech:** same, P2 presses nothing → lands on its feet.
- [ ] **Ground tech:** P1 presses C; while P2 is in Hit_Down, P2 taps Numpad 4 → Ground_Recovery.
- [ ] **Wall bounce:** both near the right wall, P1 on the left; P1 presses A → wall bounce → Hit_Down. Numpad 4 does nothing (hard knockdown).
- [ ] **Ground bounce:** P1 presses D → P2 pops up, bounces off the floor → Hit_Down (ground tech works).
- [ ] **Air spike:** P2 jumps in; P1 presses D → spiked down, bounces.
