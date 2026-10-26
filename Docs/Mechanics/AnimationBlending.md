# Animation Blending

**Status:** 🧪 Testing · **Fighters:** Basic Bro (4 frames); Dummy and Kaede off · **Added:** 2026-10-20 · **Rollback-safe:** Yes (visual only)

## What it does
When a fighter's animation changes (Idle → Walk, Walk → Crouch, landing, and so on), the old pose fades into the new one over a few frames instead of snapping. It smooths transitions between clips that came from different sources. It's the same idea as Unity's Animator transition blend.
Only the model blends. Hitboxes, hurtboxes, frame data and timing are exactly the same as without it.

## How it works (flow)
```
state changes clip --> AnimationViewer remembers the old clip + the frame it was on
                   --> for BlendFrames frames: old pose (held) fades into new clip (playing)
                   --> blend done: new clip only
```
The fade is driven by the state frame, so it pauses during hitstop. If the same clip continues into the next state there's nothing to blend. Built on a small AnimationTree (two clips + a Blend2) that the AnimationViewer creates next to the model's AnimationPlayer the first time it's needed.

## Files
| File | What it is |
|---|---|
| `Scripts/SakugaEngine/Components/AnimationViewer.cs` | `BlendFrames` setting + the crossfade (`[FF PATCH: animation blending]`) |
| `Scripts/SakugaEngine/Resources/AnimationSettings.cs` | `BlendInFrames` per-clip override (`[FF PATCH: animation blending]`) |
| `Fighters/<Fighter>/Instances/<Fighter>_Fighter_Default.tscn` → `Graphics/Mesh` | where `BlendFrames` is set per fighter |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Blend length for a whole fighter | fighter scene → `Graphics/Mesh` (AnimationViewer) | `Blend Frames` | Basic Bro 4, others 0 (off) |
| Blend length for one state | that state's AnimationData → `Animations` → the clip | `Blend In Frames` | -1 (use the fighter's) |
| An attack to snap in with no blend | the attack's AnimationData → clip | `Blend In Frames` | set to 0 |

Suggested values: 3–5 frames for movement. Attacks usually look sharper with 0–2, so the startup pose reads clearly.

## Adding it to another fighter
1. Open the fighter scene, select `Graphics/Mesh`, set **Blend Frames** (4 is a good start).
2. Make sure the model's AnimationPlayer has Speed Scale 0 (same as without blending).

## Known issues / gotchas
- Universal states are rewritten by the stamp tool, so a `Blend In Frames` set on a universal state's AnimationData is lost on re-stamp. Use the fighter-wide `Blend Frames`; per-clip overrides are for the fighter's own moves (100+).
- The old clip is held on the frame it was showing (it doesn't keep playing during the fade). With 3–5 frames this isn't visible.
- Sprite fighters (Kaede) should keep Blend Frames at 0.
- Looping clips: blending doesn't hide a loop pop. That's fixed by the stamp tool's Match Loop Durations (see UniversalLayout.md).

## Change log
| Date | Change |
|---|---|
| 2026-10-20 | Created |
