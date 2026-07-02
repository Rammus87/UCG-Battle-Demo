# Battle Playmat Coordinate System

## Purpose

Battle Layout and Focus View must not be judged by screenshot guessing alone.

The system must be able to report numeric layout state for:

- `playmatInnerRect`
- `playmatCenterY`
- `battleLaneBounds`
- `laneCenters[0..7]`
- `opponentRowBounds`
- `playerRowBounds`
- `sceneAreaBounds`
- `rightRailBounds`
- `deckBounds`
- `trashBounds`
- `viewportContentRect` under the current `targetX`, `targetY`, and `targetScale`

## Overview Layout Validity

Focus View must not repair broken layout.

If, in Overview layout:

- Deck / Trash is already outside `playmatInnerRect`
- Scene Area is not between both rows
- Opponent row and Player row are not aligned around `playmatCenterY`

then the Overview layout must be fixed. Do not use Focus transform to compensate.

## Battle Midline Validity

`playmatInnerRect.center.y` is the duel midline.

The diagnostic must be able to check:

- Whether Opponent row and Player row are aligned around `playmatCenterY`
- Whether Opponent and Player slots in the same Lane share the same `centerX`
- Whether Player row keeps safe distance from `playmatInnerRect.yMin`
- Whether Opponent row keeps safe distance from `playmatInnerRect.yMax`

## Scene Area Validity

Scene Area must satisfy:

- `centerX` uses `midpoint(Lane 01 centerX, Lane 02 centerX)`
- `centerY` stays near `playmatCenterY`
- It must not use full content width as its center
- It must not use a center that includes the right rail
- It must not use the full 8-lane `battleLaneBounds.center.x`
- It must not follow Lane 06 or Lane 07
- It must not overlap Player row or Opponent row

## Scene Card Size Validity

Scene Area represents one real horizontal Scene card, not an arbitrary UI block.
The diagnostic must be able to report:

- `normalBattleCardSize`
- `sceneHorizontalCardSize`
- `sceneCardSizeSource`
- `opponentSlotSize`
- `playerSlotSize`
- `sceneAreaBounds.size`
- `expectedSceneAreaBounds.size`
- `sceneDesignAnchor = Lane01Lane02Midpoint`
- `sceneCenterDeltaFromSceneDesignAnchor`

Rules:

- Scene card size must equal actual battle card size rotated 90 degrees.
- If normal battle card size is `cardWidth x cardHeight`, Scene card size is `cardHeight x cardWidth`.
- Scene card is only rotated; it is not a smaller card variant.
- Scene card must not use arbitrary temporary dimensions such as `520 x 234` or `278.75 x 199.58`.
- Scene card must not use thumbnail size, hand fan size, Scene Area current size, hard-coded magic numbers, or visual size after `content.localScale`.
- Scene card must not use animation `placedCardSize` unless it is proven to equal the actual battlefield card size.
- Scene frame, Scene container, and SharedSceneSlot must use the same horizontal card size.
- Focus View must not recalculate Scene Area size.

Validation:

- `sceneAreaBounds.size == expectedSceneAreaBounds.size`.
- `expectedSceneAreaBounds.size == normalBattleCardSize` rotated 90 degrees.
- Overview Scene card size must match other battlefield cards, only rotated.
- Focus Scene card size must match other battlefield cards and only scale through shared `content.localScale`.

## Right Rail Validity

Right rail / Deck / Trash must satisfy:

- `rightRailBounds` is inside `playmatInnerRect`
- Deck / Trash is inside `playmatInnerRect`
- Right rail may be clipped by the Focus viewport
- Right rail must not become the main anchor for Focus `targetX`
- Right rail must not be repaired by Focus `targetX`

## Focus View Validity

Current Focus `targetX` is close to correct. Diagnostics should validate it, not
modify it.

Focus View must report:

- `viewportContentRect`
- `focusLaneCenterX`
- `viewportCenterX`
- `focusLaneCenterDeltaFromViewportCenter`
- Whether `rightRailBounds` is outside `playmatInnerRect`
- Whether `sceneAreaBounds` stays near `playmatCenterY`
- Whether `playerRowBounds` and `opponentRowBounds` align around `playmatCenterY`
