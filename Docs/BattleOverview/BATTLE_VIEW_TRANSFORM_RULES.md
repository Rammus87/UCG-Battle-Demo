# Battle View Transform Rules

This document defines the separation between the fixed Battle Layout and the
camera-like View Transform used by Battle Overview, Focus Lane, and future
Detail views.

## Battle Layout And View Transform Separation

1. Battle Layout is the fixed playmat world.
2. View Transform is only the way the player looks at that world.
3. Overview / Focus / Close-up must not rearrange Lane / Slot / Scene / Deck / Trash.
4. View distance may change `content.localScale`.
5. View distance may change `content.anchoredPosition`.
6. View distance may change the viewport target offset.
7. View distance must not change Lane `anchoredPosition`.
8. View distance must not change Slot `sizeDelta`.
9. View distance must not change Scene Area position or size.
10. View distance must not change Deck / Trash position or size.
11. View distance must not change `playmatInnerRect`.
12. View distance must not change `playmatCenterY`.
13. View distance must not change right rail layout.
14. View distance must not use `Screen.width` or Canvas width to re-layout all 8 lanes.
15. A Lane clipped by the phone viewport is not a layout error.
16. A card-like battle object outside the Playmat is a layout error.

## View Modes

### Overview View

- Shows more of the playmat world.
- Does not need to show all 8 lanes at once.
- Used for board overview and phase prompts.
- May adjust `content.localScale`, `content.anchoredPosition`, and viewport target offset.
- Must not re-layout lanes, slots, Scene Area, Deck, or Trash.

### Focus Lane View

- Focuses the currently actionable lane.
- May make cards easier to read.
- May only adjust `content.localScale`, `content.anchoredPosition`, and viewport target offset.
- Must not re-layout the Battle Layout.

### Focus Playmat Background Framing

Overview View may show the whole playmat plus some wood table because it is a
global battlefield view. Focus Lane View is a close battle view and must use
playmat-only framing.

Rules:

- Focus View background should primarily show the inside of the playmat.
- Focus View must not show a large amount of wood table.
- Focus View bottom should not reveal wood table in a way that makes Player row,
  Deck, or Trash look outside the playmat.
- Focus View `viewportContentRect` should stay as much as possible inside the
  playmat visual safe area.
- Focus View Y framing must be based on playmat center / playmat visual safe
  rect, not on hand position or the wood table background.
- Hand Fan is a HUD/UI layer and must not be used as the safety boundary for
  Battle Layout or Playmat framing.
- Fixing Focus background framing must not re-layout Lane, Slot, Scene, Deck, or Trash.
- Fixing Focus background framing must not change Overview Layout.
- Focus should use ViewTransformOnly / background framing so the view lands in
  the middle of the playmat.

### Focus Background Transform

If the playmat background and Battle content do not share the same transform
space, Focus View must handle background framing explicitly.

Rules:

- Battle objects use `content.localScale` and `content.anchoredPosition`.
- If the playmat background does not transform with `content`, it needs its own
  Focus background framing.
- In Focus View, the playmat background must keep a consistent visual
  relationship with Battle content.
- Battle content must not appear to be on the playmat while the background
  exposes wood table and creates a false overflow impression.
- Overview may show wood table; Focus should enter a playmat-only crop or
  playmat-centered background framing.

### ViewTransformOnly Transition Rule

Switching between Overview and Focus must be smooth, not an instant visual jump.

Rules:

- Overview -> Focus must tween smoothly.
- Focus -> Overview must tween smoothly.
- The tween may only affect ViewTransformOnly values:
  - `content.localScale`
  - `content.anchoredPosition`
  - Focus / Overview background framing parameters
- The tween must not re-layout Lane, Slot, Scene, Deck, or Trash.
- The tween must not change the final `targetX`, `targetY`, or `targetScale`.
- The tween must not call legacy `SetContentView`, `SmoothFocusActiveLane`, or `FocusActiveLane`.
- The tween start must use the current visual state to avoid a first-frame jump.
- The tween end must land exactly on the existing target.
- If another view switch starts during a tween, cancel the previous tween and
  continue from the current state.

Recommended timing:

- Duration: `0.25s` to `0.4s`.
- Easing: ease-out or SmoothStep.
- Keep the transition quick enough for battle operations.

### Detail View

- Reserved for future card or effect detail presentation.
- Defined here as a future mode only.
- Must follow the same separation rule: the view may move or zoom, but must not rewrite the Battle Layout.

## `forceOverviewOnly` Rule

Until the new View Transform is implemented and verified, `forceOverviewOnly`
must remain `true`.

Do not directly re-enable the legacy Focus system. The legacy Focus system can
trigger layout reflow and may break the current Battle Overview layout.

## Legacy System Risk Notes

- Current Overview is not pure zoom; it is scale plus layout reflow.
- Legacy Focus uses `SmoothViewTo` / `GetFocusLaneTargetX` to change scale and X position.
- Re-enabling Focus may trigger reference layout or slot reflow.
- Scene / Deck / Trash are currently re-laid out by `UcgHandDemo` after reading Battlefield rects.
- `overviewVisualCompensationScale = 1.65f` may make cards and background scale inconsistently.
- Turning off `forceOverviewOnly` may break the current layout.

## Do Not

Do not modify these systems when updating this spec:

- `UcgBattlefieldManager.cs`
- `UcgHandDemo.cs`
- `UcgBattleLane.cs`
- `UcgPlayArea.cs`
- Unity layout scripts
- Camera
- Focus
- Zoom
- Pan
