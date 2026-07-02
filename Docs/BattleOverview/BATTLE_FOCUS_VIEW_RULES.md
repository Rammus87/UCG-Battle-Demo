# Battle Focus View Rules

## Battle Midline Rules In Focus View

Focus View must preserve the fixed Battle Layout. The view may zoom and pan the
content, but it must not rewrite the row relationship around the Playmat
midline.

Rules:

1. Opponent row and Player row must remain aligned around `playmatCenterY`.
2. Opponent and Player slots for the same Lane must keep the same X alignment.
3. Player row must not be pushed into the wood table area to make the hand easier to see.
4. Opponent row must not be clipped to an unusable sliver by Focus framing.
5. Hand Fan is a HUD/UI layer, not the safety boundary for Battle Layout.

## Scene Area Rules In Focus View

Scene Area is a shared center zone between both rows. Focus View must not treat
it as a lane-specific slot.

Rules:

1. Scene Area must remain between Opponent row and Player row.
2. Scene Area must stay near the Playmat centerline.
3. Scene Area must not drift to Lane 06, Lane 07, or any single Lane during Focus.
4. Scene Area must not be repositioned by right rail, Deck, or Trash layout.
5. Scene Area X must use the battle lane area / playmat inner battle area center.
6. Focus View must not reflow Scene Area; it can only move through `content.localScale` and `content.anchoredPosition`.

## Focus Lane Center Priority

Focus Lane View is lane-first. The current `focusLaneIndex` is always the primary
visual target.

Rules:

1. Focus View must use the current focus Lane as the main visual center.
2. Adjacent lanes may be mostly visible, but they must not pull the view away from the focus Lane.
3. Deck / Trash right rail must not control Focus `targetX`.
4. Deck / Trash may be clipped by the viewport during Focus View.
5. If Deck / Trash should be visible, it must already be inside `playmatInnerRect` from Overview layout.
6. Focus Lane 01 must prioritize Lane 01; Lane 02 and right rail are secondary context.
7. Focus Lane 08 must prioritize Lane 08; Lane 07 and left playmat extension are secondary context.

## Right Rail In Focus View

Right rail is a Battle Layout object, not a Focus camera target.

Rules:

1. Do not use Focus `targetX` or `targetY` to repair right rail layout.
2. If right rail appears outside the Playmat, fix its Overview layout first.
3. Right rail can be reported in debug bounds, but it must not rewrite Focus Lane centering.

## Focus Playmat Background Framing Rule

Overview View may show the whole playmat plus some wood table because it is a
global battlefield view.

Focus Lane View is a close battle view and must use playmat-only framing.

Rules:

1. Focus View background should primarily show the inside of the playmat.
2. Focus View must not show a large amount of wood table.
3. Focus View bottom should not reveal wood table in a way that makes Player row,
   Deck, or Trash look outside the playmat.
4. Focus View `viewportContentRect` should stay as much as possible inside the
   playmat visual safe area.
5. Focus View Y framing must be based on playmat center / playmat visual safe
   rect, not on hand position or the wood table background.
6. Hand Fan is a HUD/UI layer and must not be used as the safety boundary for
   Battle Layout or Playmat framing.
7. Fixing Focus background framing must not re-layout Lane, Slot, Scene, Deck, or Trash.
8. Fixing Focus background framing must not change Overview Layout.
9. Focus should use ViewTransformOnly / background framing so the view lands in
   the middle of the playmat.

## Focus Background Rule

If the playmat background and Battle content do not share the same transform
space, Focus View must handle background framing explicitly.

Rules:

1. Battle objects use `content.localScale` and `content.anchoredPosition`.
2. If the playmat background does not transform with `content`, it needs its own
   Focus background framing.
3. In Focus View, the playmat background must keep a consistent visual
   relationship with Battle content.
4. Battle content must not appear to be on the playmat while the background
   exposes wood table and creates a false overflow impression.
5. Overview may show wood table; Focus should enter a playmat-only crop or
   playmat-centered background framing.

## ViewTransformOnly Transition Rule

Overview / Focus switching must be smooth and must not jump between final
states.

Rules:

1. Overview -> Focus must tween smoothly.
2. Focus -> Overview must tween smoothly.
3. Tween may only affect `content.localScale`, `content.anchoredPosition`, and
   Focus / Overview background framing parameters.
4. Tween must not re-layout Lane, Slot, Scene, Deck, or Trash.
5. Tween must not change the final `targetX`, `targetY`, or `targetScale`.
6. Tween must not call legacy `SetContentView`, `SmoothFocusActiveLane`, or
   `FocusActiveLane`.
7. Tween start must use the current visible state to avoid first-frame jumps.
8. Tween end must land exactly on the existing ViewTransformOnly target.
9. Starting a new ViewTransformOnly transition must cancel the previous tween
   and continue from the current state.

Recommended timing:

- `0.25s` to `0.4s`.
- SmoothStep or ease-out.
- Fast enough to preserve battle operation rhythm.

## Scene Card Size Rule In Focus View

Scene Area is not an arbitrary UI block. Scene Area represents one real Scene
card on the battlefield, and Focus View must preserve the Overview Layout Scene
size.

Rules:

1. Scene card size must match the normal battlefield card size.
2. Scene card is only rotated horizontally; it is not a smaller card variant.
3. If a normal battle card is `cardWidth x cardHeight`, Scene card size must be `cardHeight x cardWidth`.
4. Scene card must not use an independent custom size.
5. Scene card must not use thumbnail size.
6. Scene card must not use hand fan card size.
7. Scene card must not use placed animation size unless that size is proven to equal the actual battlefield card size.
8. Scene card must not use content scale, overview blend, or viewport state to recalculate Scene size.
9. Scene card must not be smaller than other battlefield cards in Overview.
10. Focus View must not assign a new Scene Area size.
11. Focus View must not switch Scene card to another size because of zoom or distance.
12. Focus View may only scale Scene card through the shared `content.localScale`, together with Lane / Slot / Deck / Trash.
13. Scene frame, Scene container, and SharedSceneSlot must remain the same horizontal card size.

Size source rules:

Allowed sources:

- Actual battle card size used by Opponent / Player battlefield cards.
- Lane card slot reference card size.
- Runtime RectTransform size used by normal upright battlefield cards.

Forbidden sources:

- Hand Fan card size.
- Animation `placedCardSize`, unless it is proven to equal the actual battlefield card size.
- Thumbnail size.
- Scene Area's current size.
- Hard-coded magic numbers.
- `278.75 x 199.58`.
- `520 x 234`.
- Visual size after `content.localScale`.

## Scene Anchor Rule In Focus View

Focus View must preserve the Scene Area anchor chosen by Overview Layout.

Rules:

1. Scene centerX is `midpoint(Lane 01 centerX, Lane 02 centerX)`.
2. Scene centerY is `playmatInnerRect.center.y`.
3. Focus View must not move Scene Area to the current Focus Lane.
4. Focus View must not anchor Scene Area to the full 8-lane range.
5. Focus View must not anchor Scene Area to Deck / Trash or right rail.

## 核心概念

Focus View 不是重新排版。
Focus View 只是一種觀看方式。
Focus View 是 ViewTransformOnly 狀態。
它不能建立第二套 Battle Layout，也不能把 Focus 當成新的 Layout 模式。
Debug Focus 與正式 Focus 必須使用同一套 Focus target 計算。
不允許 Debug Focus 一套 target，正式流程 Focus 另一套 target。

建議共用函式名稱方向：
- CalculateFocusViewTarget(focusLaneIndex)
- ApplyFocusLaneViewTransformOnly(focusLaneIndex)

Focus View 修正只能決定目前套用 Focus transform 或 Overview transform，
不得因此重新排版 Lane / Slot / Scene / Deck / Trash。

Focus View 只能改：
- content.localScale
- content.anchoredPosition
- viewport target offset

Focus View 不得改：
- Lane position
- Slot position
- Slot size
- Scene Area position / size
- Deck / Trash position / size
- playmatInnerRect
- playmatCenterY
- right rail layout
- card size

## Focus Lane View 可視範圍

Focus Lane View 目標：

- 目標 Lane 清楚可見
- 左右相鄰 Lane 大部分可見
- 約看到 3 路
- 左右相鄰 Lane 可以被手機 viewport 裁切一點
- 相鄰 Lane 約 80%～90% 可見
- 不需要看到更多 Lane
- 不把 8 路塞進手機畫面

例子：

Focus Lane 02：
主要看到 Lane 01 / Lane 02 / Lane 03

Focus Lane 04：
主要看到 Lane 03 / Lane 04 / Lane 05

Focus Lane 01：
主要看到 Lane 01 / Lane 02，以及右側 Deck / Trash rail 的一部分

Focus Lane 08：
主要看到 Lane 08 / Lane 07，以及左側桌墊延伸

## Focus View 的 Playmat Safe 規則

Focus View 拉近後仍必須遵守：

- 牌類物件不能看起來跑到桌墊外
- Player row 不能掉到木桌區
- Opponent row 不能被裁切到只剩一點
- Scene Area 不能和角色 Slot 重疊
- Deck / Trash 不能跑出桌墊
- Viewport 可以裁到部分 Lane，但不能讓合法牌物件被錯誤裁切成不可辨識

## X / Y framing

Focus View 不是只算 X。
Focus View 必須同時控制：

- targetX
- targetY
- targetScale

X framing：
- 目標 Lane 應靠近視覺中心
- Lane 01 可略偏左，保留右側 Deck / Trash 可見
- Lane 08 可略偏右，保留左側桌墊延伸

Y framing：
- 必須讓目標 Lane、Scene Area、Opponent row / Player row 的可見比例合理
- Opponent row 至少應看到約 60%
- Player row 不得掉到桌墊外或木桌區
- 不得為了看手牌而犧牲桌墊內 Battle Layout
- Hand Fan 是 UI 層，不能作為 Battle Layout 的安全基準

## 禁止事項

Focus View 不得：
- 呼叫 ApplyOverviewLayout()
- 呼叫 RestoreReferenceSlotLayout()
- 呼叫 Scene / Deck / Trash reflow
- 改 Lane / Slot layout
- 改 Scene / Deck / Trash layout
- 改卡槽尺寸
- 改 playmatInnerRect
- 改 right rail
- 關掉 forceOverviewOnly 來啟用舊 Focus 系統
