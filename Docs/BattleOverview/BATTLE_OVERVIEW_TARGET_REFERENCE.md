target_playmat_layout.png 是 Battle Overview 排版最高視覺參考。

目標圖規則：

1. 右側是 Deck / Trash rail。
2. Battle Area 在右側 rail 左邊向左展開。
3. Lane 01 在最右，Lane 08 在最左。
4. 畫面由左到右顯示：
   08 07 06 05 04 03 02 01
5. Opponent row 在上方。
6. Player row 在下方。
7. Scene Area 在上下 row 中間。
8. Right rail 四個區塊必須完整在桌墊內：
   - 對手牌庫
   - 對手棄牌
   - 玩家牌庫
   - 玩家棄牌
9. Deck / Trash right rail 不能跑到木桌上。
10. Battle Area 和 right rail 都必須在桌墊內。

## Playmat Centerline 對稱規則

target_playmat_layout.png 的上下 row 不是只要在桌墊內即可，還必須以 Playmat 的水平中線作為對稱基準。

請用 `playmatInnerRect` 的上下範圍計算：

```text
playmatCenterY = (playmatInnerRect.yMin + playmatInnerRect.yMax) / 2
```

Opponent row 與 Player row 必須上下對稱：

```text
Opponent row centerY - playmatCenterY
=
playmatCenterY - Player row centerY
```

目標圖判斷規則：

1. Opponent row 位於 `playmatCenterY` 上方。
2. Player row 位於 `playmatCenterY` 下方。
3. Opponent row 與 Player row 到 `playmatCenterY` 的距離一致。
4. Scene Area 位於 `playmatCenterY` 附近，是上下 row 之間的中層區域。
5. Scene Area 不得壓到 Opponent row。
6. Scene Area 不得壓到 Player row。
7. 不可以只把 Player row 拉回桌墊內就算符合目標圖。
8. 不可以只檢查上下 row 是否超出桌墊，還必須檢查上下是否以 Playmat Centerline 對稱。
9. 如果調整 `playmatInnerRect` 的 top / bottom 內距，必須同時維持正確中線，不可只單邊調 bottom ratio 造成整體失衡。

「不超出桌墊」只是最低要求。符合 target_playmat_layout.png 的 Battle Overview，還必須符合 Playmat Centerline 上下對稱。
