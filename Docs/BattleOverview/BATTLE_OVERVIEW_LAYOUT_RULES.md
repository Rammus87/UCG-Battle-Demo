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
