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
