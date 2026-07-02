Battle Overview 的核心規則：

1. 桌墊 Playmat 是唯一合法排版基準。
2. 木桌只是背景，不能承載任何戰鬥 UI。
3. 所有牌類物件都必須在桌墊內：
   - Opponent row
   - Player row
   - Scene Area
   - Deck / Trash
   - Lane number
   - 已放置卡牌

4. Battle Area 左展開是正確的。
5. Lane 01 在右側，Lane 02～08 往左延伸。
6. 不需要在手機豎屏畫面一次看完全部 Lane。
7. Lane 04 / Lane 05 / 更左側 Lane 被手機畫面邊緣切到，不是錯。
8. 手機畫面只是 viewport，不是 layout 邊界。
9. 不准為了手機畫面塞下全部 8 路而壓縮 Lane。
10. 不能把 Canvas / Screen / Camera View 當作合法排版範圍。

## Playmat Centerline 對稱規則

Battle Overview 的上下排版，必須以桌墊 Playmat 的上下邊界為基準。

請用 `playmatInnerRect` 的上下範圍計算一條水平中線：

```text
playmatCenterY = (playmatInnerRect.yMin + playmatInnerRect.yMax) / 2
```

Opponent row 與 Player row 必須以這條中線上下對稱。

也就是：

```text
Opponent row centerY - playmatCenterY
=
playmatCenterY - Player row centerY
```

規則：

1. Opponent row 位於 `playmatCenterY` 上方。
2. Player row 位於 `playmatCenterY` 下方。
3. 雙方 row 到 `playmatCenterY` 的距離必須一致。
4. Scene Area 應位於 `playmatCenterY` 附近。
5. Scene Area 是上下 row 之間的中層區域。
6. Scene Area 不得壓到 Opponent row。
7. Scene Area 不得壓到 Player row。
8. 不可以只把 Player row 拉回桌墊內就算完成。
9. 不可以只檢查上下 row 有沒有超出桌墊，還必須檢查上下是否以中線對稱。
10. 如果需要調整 `playmatInnerRect` 的 top / bottom 內距，必須同時維持正確中線，不可只單邊調 bottom ratio 造成整體失衡。

「不超出桌墊」只是最低要求。正確 Battle Overview 還必須符合上下 row 以 Playmat Centerline 對稱。
## Battle Midline And Row Alignment Rules

Battle Layout must use the horizontal middle line of `playmatInnerRect` as the
primary duel baseline.

```text
playmatCenterY = (playmatInnerRect.yMin + playmatInnerRect.yMax) / 2
```

Rules:

1. Opponent row and Player row must correspond vertically around `playmatCenterY`.
2. Opponent slot and Player slot for the same Lane must align on the X axis.
3. Opponent row must stay above `playmatCenterY`.
4. Player row must stay below `playmatCenterY`.
5. Neither row may leave the Playmat; Player row must not fall into the wood table area.
6. Opponent row must not be pushed outside the top edge of the Playmat.
7. Focus View or hand visibility must not break Battle row alignment.
8. Hand Fan is a HUD/UI layer, not a Battle Layout safety boundary.

## Scene Area Center Rules

Scene Area is the shared scene zone between both players. It is not a Slot for
any single Lane.

Rules:

1. Scene Area must sit between Opponent row and Player row.
2. Scene Area must stay close to `playmatCenterY`.
3. Scene Area must not drift to Lane 06, Lane 07, or any other single-lane position.
4. Scene Area must not be positioned by the right rail, Deck, or Trash.
5. Scene Area X must be based on the battle lane area / playmat inner battle area center.
6. Scene Area X must not be based on the full content width after the right rail is included.
7. Scene Area Y must be based on the middle region between Opponent row and Player row.
8. Scene Area must not overlap Character slots, Player row, or Opponent row.
9. Focus View must not reposition Scene Area; it may only move with the shared content transform.

## Right Rail Layout Rules

Deck / Trash right rail is part of Battle Layout. It must be correctly placed in
the Playmat during Overview layout before any Focus View is applied.

Rules:

1. Deck / Trash right rail must fit inside `playmatInnerRect` in Overview layout.
2. Focus View must not use `targetX` or `targetY` to repair a right rail layout error.
3. If right rail appears outside the Playmat in Focus View, first verify its Overview layout.
4. Right rail may be clipped by the phone viewport.
5. Right rail may not be outside `playmatInnerRect` in its own layout.

## Focus Lane Center Priority

Focus Lane View is lane-first. The current `focusLaneIndex` is always the primary
visual target.

Rules:

1. Focus View must center on the current focus Lane.
2. Adjacent lanes may be mostly visible, but they must not pull the camera away from the focus Lane.
3. Deck / Trash right rail must not drive Focus `targetX`.
4. Deck / Trash may be clipped by the viewport during Focus View.
5. Focus Lane 01 must prioritize Lane 01; Lane 02 and right rail are secondary visible context only.
6. Focus Lane 08 must prioritize Lane 08; Lane 07 and left playmat extension are secondary visible context only.

## Scene Card Size Rule / 場景卡大小規則

Scene Area is not an arbitrary UI block. Scene Area represents one real Scene
card on the battlefield.

Core rules:

1. Scene card size must match the normal battle card size.
2. Scene card is only rotated horizontally; it is not a smaller card variant.
3. If a normal battle card is `cardWidth x cardHeight`, Scene card size must be `cardHeight x cardWidth`.
4. Scene card must not use an independent custom size.
5. Scene card must not use thumbnail size.
6. Scene card must not use hand fan card size.
7. Scene card must not use placed animation size unless that size is proven to equal the actual battlefield card size.
8. Scene card must not use a size derived from `overviewBlend` or `content.localScale`.
9. Scene card must not be smaller than other battlefield cards in Overview.
10. Scene card must not recalculate size in Focus View.
11. Focus View may only scale Scene card through the shared `content.localScale`, together with the rest of Battle content.

Overview rules:

1. In Battle Overview, Scene card must use the same battlefield card size as character cards / opponent cards.
2. Scene card only rotates the normal battlefield card size.
3. Scene card must not be visually smaller than character cards.
4. Scene card must not use temporary dimensions such as `520 x 234` or `278.75 x 199.58`.
5. Scene Area layout size must be correct during Overview Layout.

Focus rules:

1. In Focus Lane View, Scene card must reuse the horizontal card size decided by Overview Layout.
2. Focus View must not assign a new Scene size.
3. Focus View must not switch Scene card to another size because of zoom or distance.
4. Scene card should scale with Lane / Slot / Deck / Trash through `content.localScale`.

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

Scene frame, Scene container, and SharedSceneSlot must use the same horizontal
card size.

## Scene Anchor Rule

Scene Area is anchored to the main right-side battle lanes, not the full eight-lane
range and not the right rail.

Rules:

1. Scene centerX must be `midpoint(Lane 01 centerX, Lane 02 centerX)`.
2. Scene centerY must be `playmatInnerRect.center.y`.
3. Scene Area must not use the full 8-lane `battleLaneBounds.center.x`.
4. Scene Area must not use full content width center.
5. Scene Area must not include Deck / Trash right rail in its center calculation.
6. Scene Area must not dynamically follow the current Focus Lane.
