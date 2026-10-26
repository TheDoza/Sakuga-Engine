# Basic Bro — clip checklist

Copy of the checklist in the project's Basic Bro setup notes. Tick a clip once it's baked, exported and in BasicBro.glb, then point its state at it in `Fighters/BasicBro/BasicBro_Stamp.tres` → Animation Names and re-stamp.

**Movement**
- [x] Idle
- [x] Walk_Forward
- [x] Walk_Backward
- [x] Run
- [x] Run_Skid
- [ ] Step_Dash *(only for fighters with the step-dash move; stand-in: AirDash_Forward)*
- [x] Backdash

**Jumps and air**
- [x] Jump_Neutral
- [x] Jump_Forward
- [x] Jump_Backward
- [x] Falling
- [x] AirDash_Forward
- [x] AirDash_Backward
- [ ] Super_Jump_Neutral / Forward / Backward *(optional; stand-in: the matching Jump clip)*
- [ ] Double_Jump_Neutral / Forward / Backward *(optional; stand-in: the matching Jump clip)*

**Idle extras**
- [ ] Habit *(state 73: plays after 5 Idle loops, then back to Idle)*

**Crouch**
- [x] Crouch_Enter
- [x] Crouching
- [x] Crouch_Exit

**Recovery and wakeup**
- [x] Air_Recovery
- [x] Ground_Recovery
- [ ] Roll_Forward *(stand-in: Crouching)*
- [ ] Roll_Back *(stand-in: Crouching)*

**Hit reactions** *(stand-ins: Idle / Crouching / Falling)*
- [ ] Hit_Stand_Weak
- [ ] Hit_Stand_Strong
- [ ] Hit_Crouch
- [ ] Hit_Air_Weak
- [ ] Hit_Air
- [ ] Hit_GroundSlide
- [ ] Hit_Down
- [ ] Hit_GetUp
- [ ] Hit_WallBounce
- [ ] Hit_GroundBounce

**Blocking** *(stand-ins: Barrier_Stand / Barrier_Crouch / Barrier_Air)*
- [ ] Block_Stand_Enter
- [ ] Blocking_Stand
- [ ] Blockstun_Stand
- [ ] Block_Stand_Exit
- [ ] GuardBreak_Stand
- [ ] Block_Crouch_Enter
- [ ] Blocking_Crouch
- [ ] Blockstun_Crouch
- [ ] Block_Crouch_Exit
- [ ] GuardBreak_Crouch
- [ ] Block_Air_Enter
- [ ] Blocking_Air
- [ ] Blockstun_Air
- [ ] Block_Air_Exit
- [ ] GuardBreak_Air

**Barrier and Burst**
- [x] Barrier_Stand
- [x] Barrier_Crouch
- [x] Barrier_Air
- [ ] Burst *(stand-in: Barrier_Stand)*

**Throws** *(stand-ins: Idle / Falling / Crouch_Exit)*
- [ ] Throw_Forward
- [ ] Throw_Forward_Hit
- [ ] Throw_Back
- [ ] Throw_Back_Hit
- [ ] Throw_Air
- [ ] Throw_Air_Hit
- [ ] Throw_Air_Landing
- [ ] Hit_Thrown
- [ ] Hit_Thrown_Launch
- [ ] Throw_Reject_Ground
- [ ] Throw_Reject_Air
- [ ] Throw_Rejected_Ground
- [ ] Throw_Rejected_Air

**His own moves** *(states 100+, numpad names)*
- [ ] 5A
- [ ] 5B
- [ ] 5C
- [ ] 2A
- [ ] 2B
- [ ] 2C
- [ ] j.A *(name the clip j_5A)*
- [ ] j.B *(j_5B)*
- [ ] j.C *(j_5C)*
- [ ] Specials / super *(e.g. 623C — decide his move list first)*

**Presentation**
- [ ] Outro *(state 72: round-win pose and match-win outro; holds its last frame)*
- [ ] Intro *(not used by a state yet)*
- [ ] KO *(not used by a state yet)*
