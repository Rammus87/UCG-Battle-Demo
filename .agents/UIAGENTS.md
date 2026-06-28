# AGENTS.md — UCG-Battle-Demo Codex 工作規範

## 專案定位

UCG-Battle-Demo 是 UCG-tool 的附屬 Unity 教學對戰 Demo，未來會接入 UCG-tool 網站。

目前主要目標不是新增功能，而是把 Demo 從「Unity 測試畫面」提升成「接近正式卡牌遊戲的教學戰鬥畫面」。

本專案的核心方向：

- 手機豎版優先
- 作為 UCG-tool 的正式實戰教學 Demo
- 視覺需靠近 UCG-tool 訓練模組與 Target UI v1
- 保留現有規則與教學流程
- 優先做美術、UI、視覺層級、遊戲感 polish

---

## 目前專案位置

Windows：

```txt
C:\Users\86186\CodexProjects\UCG-Battle-Demo
C:\Users\86186\CodexProjects\UCG-tool
```

---

## 絕對禁止事項

除非使用者明確要求，請不要修改以下內容：

- 遊戲規則流程
- AI 行為
- 卡片效果
- 抽牌流程
- 判定流程
- 效果發動流程
- WebGL bridge
- SFX / 音效事件
- Debug Tool
- CardFanUI layout
- cards.json
- 卡圖 loader
- 牌組資料
- UCG-tool 網頁端功能

如果任務是 UI / Art Pass，請只改視覺層，不要碰規則層。

---

## 目前美術目標

目前目標圖稱為：

**Target UI v1**

之後所有 UI / Art Pass 都應以 Target UI v1 為主要施工標準。

不要把 Target UI v1 當「靈感參考」，而是當「美術規格書 / Art Bible」。

核心目標：

> 第一眼看到戰場，第二眼看到卡牌，第三眼看到 HUD。  
> 不要第一眼只看到背景。

---

## 視覺風格關鍵字

整體風格：

- 深色科技感
- 玻璃 UI
- 粉色主視覺
- 青藍輔助提示
- 細線
- 光點
- corner marker
- hologram
- soft shadow
- card depth
- battlefield density

不要使用：

- Unity 預設 Panel 感
- 大片半透明矩形
- 大 Battle Board 霧面遮罩
- 粗框
- 大色塊
- 過亮 neon
- Debug UI 感

---

## Design Token

目前請優先使用 `UcgToolUiPalette` 內的 token。

主要色彩：

```txt
Brand Pink: #e83f8c
Brand Pink Light: #ff63aa
Focus Cyan: #6de2ff
Deep Glass: rgba(7,12,22,0.78)
Soft White: rgba(255,255,255,0.92)
Muted White: rgba(255,255,255,0.68)
```

若需要新增顏色，請先確認是否真的必要。  
優先擴充 `UcgToolUiPalette`，不要在各腳本中散落硬編碼顏色。

---

## 美術施工原則

### 1. 不要用更多 Panel 解決質感

Codex 很容易遇到畫面太空時加 Panel，但本專案不要這樣做。

請用以下方式增加高級感：

- 細線
- corner marker
- 玻璃邊框
- 內陰影
- top highlight
- bottom shadow
- ambient light
- 科技細節
- grid / circuit / hex / particle
- 卡牌底部 shadow

不要用：

- 大面積透明面板
- 霧面遮罩
- 大色塊
- 整屏 glow

---

### 2. 背景要退後

目前背景很漂亮，但太搶戲。

Art Pass 中應注意：

- 背景亮度不要壓過卡牌
- 中央光圈不要搶過戰場
- 背景只是環境，不是主角
- 可用 vignette / dim overlay / 降飽和 / 降對比，讓卡牌與戰場站出來

---

### 3. 戰場要成形

不要做一大片 Battle Board。

Target UI v1 的戰場感來自：

- 中央光線
- 場景區 corner marker
- 敵我 Lane 對戰連線
- slot 玻璃卡槽
- 卡牌 shadow
- 微弱科技線條
- 區域標籤

請使用這些細節堆出 battlefield，而不是新增大面板。

---

### 4. Slot 必須像正式卡槽

空 Slot 不應只是定位角標。

Slot 應具備：

- 深色玻璃底
- 細邊框
- 圓角
- 內陰影
- bottom shadow
- top highlight
- hover / active / target 狀態

狀態建議：

```txt
Default: 很淡的深色玻璃 + 細框
Hover: 青藍邊框 + 微弱 cyan glow
Playable: 粉色邊框 + 微弱 pink glow
Target: 粉色亮框 + soft outer glow
Disabled: 降低透明度，不要完全消失
```

卡牌放上去後，Slot 只作為底座，不要搶過卡牌。

---

### 5. 卡牌要有重量

所有場上卡牌應避免像貼圖。

卡牌應具備：

- bottom shadow
- soft ground light
- edge highlight
- hover lift
- selected rim
- target rim
- 呼吸感要非常輕微

避免：

- 全卡白膜
- 大面積 overlay
- 過度放大
- 過亮 glow

---

## 正式卡背

目前正式卡背已導入：

```txt
Assets/Resources/UCG/CardBacks/ucg_card_back_standard.png
```

背面卡應使用正式 UCG 卡背，不要回到紅色方塊。

卡背顯示規則：

- Normal / Hover / Selected 狀態下主圖必須保持 `Color.white`、alpha = 1
- 不可出現泛白 / 霧面 / 白膜
- Disabled 才能灰階或降飽和
- Selected 使用細青藍邊框，不要洗白整張卡
- Target 使用粉色細框，不要洗白整張卡

---

## UI Component Language

請逐步整理成可複用元件，不要每個地方各寫一套樣式。

建議共用方法：

```txt
ApplyGlassSurface
ApplyGameButtonStyle
ApplyDialogStyle
ApplyToastStyle
ApplyCounterCardStyle
ApplySlotStyle
ApplyCardPresentation
```

所有 UI 應像同一家公司做的：

- HUD
- Toast
- Dialog
- Button
- Counter Card
- Slot
- Navigation
- Tooltip

---

## HUD 規範

HUD 目前已接近可用，不要過度重做。

原則：

- 深色玻璃底
- 細粉色邊框
- 上下細線裝飾
- 主任務亮粉
- 階段小字弱白
- 補充說明弱白
- 不要亮紅色實心 Banner
- 不要 Debug 感

---

## Toast / 效果提示規範

Toast 應接近 Target UI v1：

- 深色玻璃底
- 左側粉色 accent bar
- icon / 驚嘆號
- 標題亮粉
- 內容白色
- 次要文字弱白
- 圓角
- 柔和陰影
- 淡入 + 上滑 + 淡出

不要做成單行黑條。  
不要做成 Debug 訊息。

---

## Dialog / 確認彈窗規範

Dialog 應是正式遊戲彈窗，不是 Unity Panel。

建議結構：

```txt
小標題：操作確認
主文字：確定設置到第 1 路嗎？
次要說明：設置後將進入下一步操作
按鈕：取消 / 確定
```

若目前沒有取消流程，不要修改遊戲邏輯。  
可先只保留「確定」按鈕，但視覺結構要正式。

---

## 右側資訊卡規範

牌庫 / 棄牌 / 資源等資訊卡：

- 深色玻璃卡片
- 圓角
- 細粉色或青藍邊框
- 柔和陰影
- 標題小、弱白
- 數字大、亮粉
- 間距一致
- 靠右排列
- 不要和場上卡牌重疊

卡牌永遠比資訊卡重要。  
資訊卡不能遮住卡牌。

---

## Layout 安全區規則

不要只看目前截圖調整 layout，因為遊戲中會有：

- 卡牌橫置
- 場景卡
- 相機拉近 / 拉遠
- WebGL 不同比例
- 手機豎版安全區

已知安全區建議：

```txt
直立卡牌安全尺寸：約 180 x 244
橫置卡牌安全尺寸：至少 248 x 184，理想 260 x 190
Lane center spacing：至少 300
Lane visual gap：不要低於 36，建議 42+
場景卡保留區：至少 520 x 220
場景卡與上下 Lane 間距：建議 48 ~ 56
右側資訊卡與最右側橫置卡牌距離：至少 40 ~ 48
```

不要為了畫面集中犧牲橫置安全區與場景卡安全區。

---

## CardFanUI 注意事項

CardFanUI 是底部手牌扇形 UI。

除非使用者明確要求，請不要修改：

- CardFanUI layout
- 手牌位置
- 手牌排列邏輯
- 手牌縮放規則
- 手牌拖曳邏輯

可以修改卡牌材質表現，但不能破壞 Fan layout。

---

## Art Pass 工作方式

後續建議以 Art Pass 方式施工。

每輪只做一件主要事情。

建議階段：

```txt
Environment Pass：背景退後、環境光、戰場科技細節
Battlefield Pass：場景區、Lane、Slot、戰場連線
Card Presentation Pass：卡牌材質、卡背、shadow、hover、selected
Component Pass：Dialog、Toast、Button、Counter Card
Motion Pass：hover、flip、place、upgrade、effect animation
Polish Pass：整體收斂
```

每輪都要避免大改功能。

---

## 目前推薦下一步

目前建議下一步是：

**Environment Detail Pass**

目標：

- 增加戰場 visual density
- 讓背景退後
- 讓戰場站出來
- 用 line / corner / grid / light / shadow 補細節
- 不新增大 Panel

請記住：

> 不要試圖用「更多 Panel」增加質感。  
> 要用「更多細節：Lines、Corner、Glass、Shadow、Light、Hierarchy」增加質感。

---

## 驗收總標準

每次完成 Art Pass 後，請自我檢查：

- 第一眼是否看到戰場，而不是背景？
- 卡牌是否比背景更重要？
- Slot 是否像正式卡槽，而不是定位線？
- UI 是否像同一套設計系統？
- 是否誤改了規則、AI、卡片效果、CardFanUI？
- 是否新增了不必要的大面板？
- 所有新增裝飾 Image 是否 `raycastTarget = false`？
- 是否保持手機豎版可用？
- 是否保留橫置卡與場景卡安全區？

---

## 回報格式

完成後請簡短回報：

```txt
完成項目：
- ...

修改檔案：
- ...

未修改：
- 規則 / AI / CardFanUI / cards.json / loader / WebGL / SFX / Debug Tool

驗證：
- git diff --check
- Unity Play Mode 是否有跑
- 若未跑，請明確說明
```

不要寫很長的報告。  
優先讓使用者看截圖與效果。
