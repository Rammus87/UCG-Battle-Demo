Viewport 規則：

1. 手機豎屏 Game View 不是 Battle Layout 的完整範圍。
2. Battle Board Content 可以比手機畫面寬。
3. 左側 Lane 被 viewport 裁到是正常現象。
4. 不能因為 Lane 04 / Lane 05 被畫面左側切到，就把 Battle Area 拉回右邊。
5. 不能因為手機畫面看不到 8 路，就壓縮 Lane pitch。
6. 不能用 Screen.width / Canvas width / Viewport width 決定 8 路總寬。
7. Camera / viewport 只負責看見一部分桌墊世界。
8. Battle Layout 要以 playmatInnerRect / battle board content 為基準。
9. Viewport clipping 可以存在，但不能裁掉桌墊內合法牌物件的錯誤部分。
10. 如果是 Mask / RectMask2D 裁掉合法物件，要修 mask / parent bounds / coordinate conversion，而不是重排整個 Battle Area。