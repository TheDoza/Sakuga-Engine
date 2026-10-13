# Crouching

**Status:** ✅ Working (tested 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

Holding any down direction crouches; releasing stands up. Crouch hits and crouch blocks return to Crouching.

| State | What it is | Hurtbox |
|---|---|---|
| 10 Crouch_Enter | 4 frames → Crouching | Crouch |
| 11 Crouching | loops while down is held | Crouch |
| 12 Crouch_Exit | 4 frames → Idle | Basic |

| Move | Input | Notes |
|---|---|---|
| 7 Crouch | hold 1, 2 or 3 | ignored while crouching or crouch-blocking |
| 8 Uncrouch | release down | only from Crouch_Enter / Crouching |

47 Block_Crouch_Exit, 49 GuardBreak_Crouch and 22 Hit_Crouch go to 11 Crouching (Uncrouch fires next frame if down isn't held).

| Date | Change |
|---|---|
| 2026-10-11 | Created |
