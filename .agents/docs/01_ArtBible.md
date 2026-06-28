# 01_ArtBible.md — UCG-Battle-Demo 美術總規範

## 1. 文件目的

這份文件是 UCG-Battle-Demo 的最高美術規範。所有 UI / Art Pass / 視覺修改，都必須優先遵循本文件。

本文件不是單次提示詞，也不是臨時修改說明。它是本專案的 Art Bible。

Codex 在進行任何 UI 或美術工作前，必須先理解：

> UCG-Battle-Demo 的目標不是做一個能跑的 Unity Demo，而是做出一個接近正式上市卡牌遊戲的教學對戰畫面。

---

## 2. 專案定位

UCG-Battle-Demo 是 UCG-tool 的附屬 Unity 教學對戰 Demo。

它的用途是：

- 承接 UCG-tool 網站的新手教學
- 讓玩家實際體驗 UCG 的對戰流程
- 作為未來接入 WebGL 的正式實戰教學模組
- 讓玩家感覺自己正在進入一款完整的卡牌遊戲，而不是測試工具

因此，本專案的視覺標準應高於一般 prototype。

---

## 3. 核心目標

本專案目前的核心目標：

> 把畫面從「Unity 測試 Demo」提升成「正式卡牌遊戲教學戰鬥畫面」。

所有美術修改都應服務這個目標。

請不要只是讓某個 Button 更漂亮。請思考它是否讓整個畫面更像正式遊戲。

請不要只是加更多 UI。請思考它是否提升了玩家的 game feel。

請不要只是調透明度。請思考它是否讓畫面更有層次、更有戰場感。

---

## 4. Target UI v1

目前本專案的主要目標圖稱為：

**Target UI v1**

Target UI v1 不是靈感圖。Target UI v1 是本專案目前的美術施工標準。

Codex 進行 UI / Art Pass 時，必須將 Target UI v1 視為 Art Bible 的視覺基準。

不要照抄以下內容：

- 卡牌內容
- 角色頭像
- 不符合 UCG 規則的 UI
- 不存在於本專案的功能元件

應該學習並套用以下設計語言：

- 深色科技感
- 玻璃 UI
- 粉色 / 青藍能量線
- 戰場細節
- 卡牌漂浮感
- HUD 層級
- 高級感陰影
- 細線與 corner marker
- 中央戰場存在感
- UI 元件一致性

---

## 5. 視覺優先順序

本專案畫面應該遵循以下視覺優先順序：

### 第一眼：戰場

玩家第一眼應該感覺到：

> 這是一個正式的對戰場地。

不是背景。不是 Debug UI。不是一堆按鈕。

戰場存在感來自：

- Slot
- 場景區
- 對戰線
- corner marker
- 科技細線
- 卡牌陰影
- 地面光
- 微弱 ambient light

不應該靠大面板或大遮罩建立。

### 第二眼：卡牌

卡牌是卡牌遊戲的主角。

卡牌必須比背景更重要。卡牌必須比 HUD 更重要。卡牌必須有重量、有深度、有落位感。

卡牌不應該像圖片貼在背景上。卡牌應該像真的放在戰場上。

### 第三眼：HUD / 資訊

HUD 的任務是輔助玩家理解目前狀態。

HUD 不應搶過卡牌。HUD 不應搶過戰場。HUD 不應像 Debug log。

HUD 應該清楚、穩定、精緻，但不是畫面主角。

---

## 6. 核心美術語言

本專案的視覺關鍵字：

- Dark Sci-Fi
- Glass UI
- Hologram
- Neon Accent
- Card Depth
- Battlefield Density
- Soft Shadow
- Thin Line
- Corner Marker
- Subtle Glow
- Layered UI
- Mobile-first Battle Screen

中文描述：

- 深色科技感
- 深色玻璃
- 粉色主視覺
- 青藍輔助提示
- 細線
- 微光
- 卡牌厚度
- 戰場細節
- 柔和陰影
- 不誇張的霓虹
- 手機豎版卡牌遊戲畫面

---

## 7. 不應該出現的感覺

請避免以下畫面感：

- Unity 預設 UI
- Debug 工具畫面
- 半成品 prototype
- 大片透明矩形
- 大霧面遮罩
- 背景比卡牌更搶眼
- Button / HUD / Toast 各自不同風格
- 粗框
- 過亮 glow
- 過多 neon
- 卡牌像貼圖
- Slot 像定位角標
- 場景區沒有存在感
- 右側資訊像數字 debug 欄

---

## 8. 戰場哲學

戰場是本專案最重要的美術核心。

目前最大的視覺問題通常不是 HUD，而是：

> 戰場沒有形成。

正式卡牌遊戲的戰場不一定需要大桌面，但它必須讓玩家知道：

- 卡牌放在哪裡
- 哪一路正在處理
- 場景卡在哪裡
- 哪張牌是焦點
- 哪裡是可操作區域
- 哪裡是敵方
- 哪裡是我方

本專案不要使用大面積 Battle Board 來解決戰場問題。

應使用以下方式建立戰場：

- Slot glass surface
- corner marker
- thin battlefield line
- active lane connection
- scene area marker
- card ground shadow
- ambient light
- subtle grid / circuit / hex detail

戰場應該像科技投影場地。不是木桌。不是大面板。不是透明塑膠板。

---

## 9. 背景哲學

背景不是主角。

背景的任務是提供世界觀與氣氛。它不應該搶走卡牌與戰場的注意力。

如果背景太亮、太飽和、中央光太強，請讓背景退後。

可以使用：

- 降亮度
- 降飽和
- 降對比
- 中央柔化
- 四周暗角
- 戰場周圍 subtle dim

但不要粗暴加一大片黑色遮罩。不要把背景變髒。不要讓畫面失去科技感。

目標是：

> 背景仍然漂亮，但玩家第一眼看到的是戰場與卡牌。

---

## 10. 卡牌哲學

卡牌是整個畫面的主角。

所有卡牌都應具備基本材質感：

- bottom shadow
- edge highlight
- slight lift
- soft ground light
- selected rim
- target rim
- hover feedback

卡牌不應該只是 Sprite。

背面卡必須使用正式 UCG 卡背，不得回到紅色測試方塊。

卡背顯示要保持原圖飽和度。Normal 狀態不得出現白膜、霧面或過度 overlay。

Hover / Selected / Target 應該用邊框、陰影、微光處理，不要洗白整張卡。

---

## 11. Slot 哲學

Slot 的目的不是裝飾。Slot 的目的是告訴玩家：

> 這裡可以放牌。

Slot 必須像正式卡槽，而不是定位角標。

好的 Slot 應該做到：

- 空著時可見，但不搶畫面
- 可操作時清楚
- hover 時有回饋
- target 時有焦點
- 放上卡牌後退到背景，成為底座

Slot 不應該是一個大色塊。Slot 不應該只是四個角。Slot 不應該比卡牌更亮。

---

## 12. 場景區哲學

UCG 有場景卡，因此中央場景區必須被尊重。

場景區不能被以下元素擠壓：

- 敵方 Lane
- 我方 Lane
- Toast
- HUD
- 右側資訊卡
- 卡牌 hover 動畫

場景區應該有存在感，但不要成為大面板。

建議使用：

- corner marker
- thin border
- center light
- subtle horizontal / vertical line
- ground shadow
- faint hologram detail

場景區是戰場核心之一，不是空白區域，也不是隨便放卡的地方。

---

## 13. HUD 哲學

HUD 是導航，不是主角。

HUD 應該協助玩家理解：

- 現在是什麼階段
- 玩家該做什麼
- 目前輪到誰
- 目前有哪些資源 / 狀態

HUD 應該具備：

- 深色玻璃
- 清楚文字層級
- 小標題
- 主任務
- 補充說明
- 細線與 accent
- 適度留白

HUD 不應該像 Debug log、純文字、Unity Banner 或臨時訊息框。

---

## 14. Toast / 效果提示哲學

Toast 是瞬間引導，不是長期資訊欄。

Toast 應該：

- 出現時清楚
- 不擋住主要卡牌
- 不擋住場景區
- 有標題與內容層級
- 使用深色玻璃
- 使用左側 accent bar
- 可有 icon
- 有淡入 / 上滑 / 淡出

Toast 不應該像 Debug log，不應該只是一行黑底字，不應該持續佔用戰場中心。

---

## 15. Dialog 哲學

Dialog 是正式遊戲操作確認。

Dialog 不應該像 Unity Panel。

Dialog 必須有資訊結構：

- 小標題
- 主問題
- 補充說明
- 按鈕

即使目前只有「確定」按鈕，也應保持正式 Dialog 結構。

Dialog 應該：

- 深色玻璃
- 細邊框
- 柔和陰影
- 清楚字級
- 內部留白
- 按鈕一致

不要只是半透明矩形加一行文字。

---

## 16. 右側資訊哲學

右側資訊卡是 HUD，不是戰場主體。

它的任務是顯示：

- 牌庫
- 棄牌
- 資源
- 其他數值

它應該：

- 靠右
- 有一致尺寸
- 有一致間距
- 標題小
- 數字大
- 不壓到卡牌
- 不搶主戰場

卡牌永遠比資訊卡重要。若空間不足，應優先保護卡牌。

---

## 17. 動畫哲學

動畫的目的不是炫技。動畫的目的是增加 game feel。

所有動畫應該：

- 短
- 輕
- 有回饋
- 不干擾操作
- 不破壞可讀性

Hover 動畫要像卡牌被拿起。Selected 動畫要像卡牌被聚焦。Target 動畫要像被指定。Flip 動畫要清楚但不要過度花俏。

避免：

- 大幅縮放
- 過度晃動
- 過度 glow
- 長時間阻擋操作

---

## 18. Do

請優先做：

- 讓背景退後
- 讓戰場出現
- 讓卡牌更有重量
- 讓 Slot 更像正式卡槽
- 讓 UI 使用同一套設計語言
- 用細節提升質感
- 用陰影建立層次
- 用 corner / line / light 建立科技感
- 保留手機豎版可讀性
- 保留橫置卡安全區
- 保留場景卡安全區

---

## 19. Don't

請避免：

- 增加大面板
- 增加大玻璃板
- 增加大片半透明矩形
- 用更多 Panel 解決畫面太空
- 讓背景搶過卡牌
- 讓 HUD 搶過卡牌
- 讓 Slot 搶過卡牌
- 讓卡牌泛白
- 讓卡牌 hover 過度放大
- 改動規則流程
- 改動 AI
- 改動 CardFanUI layout
- 改動 cards.json
- 改動 loader
- 改動 WebGL bridge
- 改動 SFX
- 改動 Debug Tool

---

## 20. 最終畫面判斷標準

每次完成 UI / Art Pass 後，請用以下問題檢查：

1. 第一眼是否看到戰場，而不是背景？
2. 卡牌是否比背景更重要？
3. Slot 是否像正式卡槽？
4. 場景區是否有穩定存在感？
5. HUD 是否清楚但不搶戲？
6. Toast 是否像正式遊戲提示？
7. Dialog 是否像正式遊戲彈窗？
8. 右側資訊是否清楚但不干擾卡牌？
9. 所有 UI 是否像同一套產品？
10. 是否不小心改了規則或資料？
11. 是否新增了不必要的大面板？
12. 是否保留橫置卡與場景卡安全區？

如果答案是否定，這輪 Art Pass 還沒有完成。

---

## 21. 一句話總結

UCG-Battle-Demo 的美術目標是：

> 用深色科技玻璃、細線、陰影、卡牌厚度與戰場細節，把 Unity Demo 變成一個具有正式卡牌遊戲質感的手機豎版教學對戰畫面。
