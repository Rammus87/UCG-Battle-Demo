# 02_DesignToken.md — UCG-Battle-Demo Visual Token 規範

## 1. 文件目的

這份文件定義 UCG-Battle-Demo 的 Visual Token。

所有 UI / Art Pass / 美術修改都應優先使用本文件中的 Token。  
不要在各腳本中隨意 hardcode 新顏色、新陰影、新邊框或新 glow。

本文件的目的不是單純列出色碼，而是定義：

> 哪一種視覺效果，應該用在哪一種 UI 情境。

---

## 2. Token 使用原則

### 2.1 優先使用 UcgToolUiPalette

目前 Unity 專案中已有集中式 palette：

```txt
Assets/UCG/Scripts/UcgToolUiPalette.cs
```

Codex 若需要顏色或視覺 token，請優先使用或擴充 `UcgToolUiPalette`。

不要在多個腳本中重複寫：

```csharp
new Color(...)
Color32(...)
"#ff63aa"
```

若需要新增 token，請先放進 `UcgToolUiPalette`，再由其他 UI 腳本引用。

---

### 2.2 Token 是語義，不只是顏色

不要只思考：

```txt
粉色
青藍
白色
```

請思考：

```txt
目前操作
可放置
效果來源
效果目標
不可操作
主要任務
次要說明
卡牌陰影
面板陰影
```

同樣是粉色，不同情境的透明度、亮度、glow 強度都不同。

---

### 2.3 不要新增隨機顏色

本專案的主要色彩應保持克制。

允許：

- 深色
- 粉色
- 青藍
- 白色文字
- 弱白文字
- 少量警示金

避免：

- 綠色
- 紫色
- 紅色大面積
- 黃色大面積
- 高飽和亂色
- 每個元件都有不同顏色

---

## 3. Color Token

### 3.1 Brand Primary

```txt
Name: BrandPink
Hex: #e83f8c
```

用途：

- 主要品牌色
- 主要操作提示
- 可操作 Lane
- 當前任務提示
- Dialog 主標題
- Button active border
- Toast accent bar

不要用於：

- 大面積背景
- 整塊 Panel 填色
- 全畫面 glow
- 卡面 overlay

---

### 3.2 Brand Highlight

```txt
Name: BrandPinkLight
Hex: #ff63aa
```

用途：

- 重要數字
- 主要 CTA
- 主任務文字
- Hover / active 的高亮邊框
- Target rim
- 少量 glow

不要用於：

- 大片底色
- 長段文字
- 背景 wash
- 大面積 neon

---

### 3.3 Focus Cyan

```txt
Name: FocusCyan
Hex: #6de2ff
```

用途：

- 可放置目標
- hover 狀態
- 效果來源卡
- active lane 連線
- scene area 科技線
- selected card rim

不要用於：

- 主任務文字
- 大面積 Panel
- 過亮背景
- 與 BrandPink 同時大面積競爭

---

### 3.4 Deep Background

```txt
Name: DeepBackground
Suggested: rgba(3, 6, 12, 0.88~0.96)
```

用途：

- 最深層 UI 背景
- Dialog 背板
- 彈窗暗層
- 需要最高可讀性的文字底

不要用於：

- 所有 UI 都統一壓黑
- 遮住背景氣氛
- 戰場大面積遮罩

---

### 3.5 Deep Glass

```txt
Name: DeepGlass
Suggested: rgba(7, 12, 22, 0.72~0.86)
```

用途：

- HUD
- Toast
- Dialog
- Counter Card
- Button
- 小型資訊卡
- Navigation pill

這是最常用的玻璃底色。

注意：

- 透明度依層級調整
- 不要太亮
- 不要變成青綠霧板
- 不要整屏覆蓋

---

### 3.6 Slot Glass

```txt
Name: SlotGlass
Suggested: rgba(7, 12, 22, 0.20~0.38)
```

用途：

- 空 Slot
- Lane 卡槽
- 場景卡槽
- 可放置區底紋

Slot Glass 應該比 HUD 更淡。  
它的角色是底座，不是主 UI。

---

### 3.7 Soft White

```txt
Name: SoftWhite
Suggested: rgba(255, 255, 255, 0.88~0.94)
```

用途：

- 主要文字
- Button 文字
- Dialog 主內容
- Toast 內容
- HUD 關鍵狀態

不要使用純白 `#ffffff` 作為大量 UI 文字。  
純白太刺眼，會破壞深色科技感。

---

### 3.8 Body White

```txt
Name: BodyWhite
Suggested: rgba(255, 255, 255, 0.76~0.86)
```

用途：

- 一般說明文字
- 次級資訊
- Dialog 補充說明
- Toast body
- HUD body

---

### 3.9 Muted White

```txt
Name: MutedWhite
Suggested: rgba(255, 255, 255, 0.56~0.68)
```

用途：

- 次要資訊
- 小標籤
- 階段說明
- 弱化資訊
- Debug-like info 的視覺降權

不要用於主要任務或按鈕主文字。

---

### 3.10 Warning Gold

```txt
Name: WarningGold
Suggested: #f5c96b
```

用途：

- 少量警示
- 勝負提示
- guide ring 特殊提醒
- 不常見狀態

不要大面積使用金色。  
UCG Demo 的主視覺不是金色系。

---

## 4. Glass Token

### 4.1 Glass Level 1 — Thin Glass

用途：

- 小按鈕
- Navigation pill
- 小型狀態標籤
- 輕量資訊卡

建議：

```txt
Background alpha: 0.58~0.72
Border alpha: 0.22~0.34
Top highlight alpha: 0.05~0.09
Shadow alpha: 0.10~0.16
```

特徵：

- 輕
- 薄
- 不搶畫面
- 適合大量出現

---

### 4.2 Glass Level 2 — Standard Glass

用途：

- HUD
- Toast
- Counter Card
- 右側資訊卡
- 主要提示框

建議：

```txt
Background alpha: 0.72~0.84
Border alpha: 0.28~0.42
Top highlight alpha: 0.06~0.12
Inner shadow alpha: 0.04~0.08
Outer shadow alpha: 0.14~0.22
```

特徵：

- 清楚
- 正式
- 有厚度
- 適合主要 UI

---

### 4.3 Glass Level 3 — Heavy Glass

用途：

- Dialog
- 確認彈窗
- 重要 overlay panel
- 模態提示

建議：

```txt
Background alpha: 0.84~0.92
Border alpha: 0.36~0.52
Top highlight alpha: 0.08~0.14
Inner shadow alpha: 0.06~0.12
Outer shadow alpha: 0.22~0.34
```

特徵：

- 最穩
- 最有重量
- 只用在少數重要 UI

不要拿 Heavy Glass 做 HUD 或大戰場底板。

---

### 4.4 Slot Glass

用途：

- Lane Slot
- Scene Slot
- 空卡槽

建議：

```txt
Background alpha: 0.12~0.28
Border alpha: 0.18~0.34
Top highlight alpha: 0.04~0.08
Inner shadow alpha: 0.05~0.10
Ground shadow alpha: 0.12~0.22
```

特徵：

- 非常克制
- 保持可讀
- 不搶卡牌

---

## 5. Border Token

### 5.1 Border Subtle

用途：

- Default panel
- Slot default
- Counter card default
- 不需要強調的容器

建議：

```txt
Color: SoftWhite / FocusCyan / BrandPink
Alpha: 0.16~0.28
Width: 1px
```

---

### 5.2 Border Brand

用途：

- HUD
- Dialog
- Main button
- Toast
- Counter card

建議：

```txt
Color: BrandPink
Alpha: 0.28~0.48
Width: 1px ~ 2px
```

---

### 5.3 Border Focus

用途：

- Hover
- Selected
- 可放置目標
- active UI

建議：

```txt
Color: FocusCyan
Alpha: 0.45~0.70
Width: 1px ~ 2px
```

---

### 5.4 Border Target

用途：

- 效果目標
- 需要玩家注意的牌
- target lane

建議：

```txt
Color: BrandPinkLight
Alpha: 0.50~0.78
Width: 1px ~ 2px
```

---

### 5.5 Border Disabled

用途：

- 不可操作 UI
- disabled button
- disabled slot

建議：

```txt
Color: MutedWhite
Alpha: 0.10~0.20
Width: 1px
```

---

## 6. Shadow Token

### 6.1 Panel Shadow

用途：

- HUD
- Toast
- Dialog
- Counter Card
- Button

建議：

```txt
Color: rgba(0,0,0,0.25~0.38)
Offset: y -4 ~ -8
Blur feel: soft
```

注意：

- 不要硬黑影
- 不要像物件掉下去
- 只要微微浮起

---

### 6.2 Ground Shadow

用途：

- 場上卡牌
- 背面卡
- Slot 底座
- 場景卡

建議：

```txt
Color: rgba(0,0,0,0.28~0.42)
Shape: soft oval / soft rectangle
Offset: y -6 ~ -12
Alpha depends on card state
```

Ground Shadow 是卡牌「有重量」的關鍵。

---

### 6.3 Slot Shadow

用途：

- 空 Slot
- active Slot
- target Slot

建議：

```txt
Default alpha: 0.08~0.16
Hover alpha: 0.14~0.24
Target alpha: 0.18~0.30
```

Slot Shadow 不應比卡牌 shadow 更強。

---

### 6.4 Card Shadow

用途：

- 場上卡牌
- 手牌 hover
- selected card

建議：

```txt
Normal: 0.22~0.34
Hover: 0.34~0.48
Selected: 0.38~0.54
Disabled: 0.14~0.24
```

Card Shadow 必須保留，即使卡牌 disabled。  
不要讓 disabled card 完全扁平。

---

## 7. Glow Token

Glow 只用於互動或焦點。  
不要把 glow 當背景裝飾大量使用。

### 7.1 Hover Glow

用途：

- hover slot
- hover button
- hover card

建議：

```txt
Color: FocusCyan
Alpha: 0.12~0.24
```

---

### 7.2 Active Glow

用途：

- active lane
- currently actionable UI
- current phase focus

建議：

```txt
Color: BrandPink / BrandPinkLight
Alpha: 0.16~0.30
```

---

### 7.3 Source Glow

用途：

- 效果來源卡
- source scene card

建議：

```txt
Color: FocusCyan
Alpha: 0.18~0.32
```

---

### 7.4 Target Glow

用途：

- 效果目標卡
- target lane

建議：

```txt
Color: BrandPinkLight
Alpha: 0.20~0.36
```

---

### 7.5 Ambient Glow

用途：

- 戰場科技細節
- 場景區中心光
- 對戰線

建議：

```txt
Alpha: 0.03~0.10
```

Ambient Glow 必須非常淡。  
它是氣氛，不是焦點。

---

## 8. Radius Token

Unity UI 若無法真正圓角，可使用 9-sliced sprite 或現有 rounded image 解決。  
不要因為方便就讓每個元件角度不同。

### 8.1 Radius Small

用途：

- Badge
- 小型 pill
- 小型 button

視覺感：

```txt
小圓角，偏科技 UI
```

---

### 8.2 Radius Medium

用途：

- HUD
- Toast
- Counter Card
- Slot
- Button

視覺感：

```txt
明顯圓角，但不要可愛化
```

---

### 8.3 Radius Large

用途：

- Dialog
- Modal
- 大提示框

視覺感：

```txt
正式彈窗，厚重且柔和
```

---

## 9. Typography Token

本專案文字應建立層級，而不是全部同一大小與顏色。

### 9.1 Hero / Main Task

用途：

- 玩家當前要做的事
- 主提示核心文字
- 重要階段提示

建議：

```txt
Color: BrandPinkLight
Weight: Bold
Size: Largest in current component
```

---

### 9.2 Title

用途：

- Dialog 標題
- Toast 標題
- HUD 主標題
- Counter label group title

建議：

```txt
Color: SoftWhite or BrandPinkLight
Weight: SemiBold / Bold
```

---

### 9.3 Subtitle

用途：

- 階段文字
- 補充狀態
- 小標籤

建議：

```txt
Color: BodyWhite
Weight: Medium
```

---

### 9.4 Body

用途：

- 一般說明
- Toast 內容
- Dialog 補充文字

建議：

```txt
Color: BodyWhite
Weight: Regular
```

---

### 9.5 Muted

用途：

- 次要資訊
- 弱提示
- 說明標籤
- Debug-like info 的降權文字

建議：

```txt
Color: MutedWhite
Weight: Regular
```

---

### 9.6 Counter Number

用途：

- 牌庫數字
- 棄牌數字
- 資源數字

建議：

```txt
Color: BrandPinkLight
Weight: Bold
Size: Larger than title
```

Counter Number 是資訊卡主視覺。

---

### 9.7 Caption

用途：

- 小型 badge
- 小提示
- navigation pill

建議：

```txt
Color: MutedWhite / SoftWhite
Weight: Medium
```

---

## 10. State Token

### 10.1 Normal

用途：

- 預設狀態

建議：

```txt
Glass: default
Border: subtle
Glow: none
Shadow: default
```

---

### 10.2 Hover

用途：

- 滑鼠或觸控 hover / focus

建議：

```txt
Border: FocusCyan
Glow: HoverGlow
Shadow: slightly stronger
Scale: subtle only
```

---

### 10.3 Active

用途：

- 目前可操作
- 當前 Lane
- 當前階段

建議：

```txt
Border: BrandPink / BrandPinkLight
Glow: ActiveGlow
Text: BrandPinkLight if needed
```

---

### 10.4 Selected

用途：

- 玩家選中的卡
- 選中的目標

建議：

```txt
Border: FocusCyan
Glow: SourceGlow
Shadow: stronger
```

---

### 10.5 Target

用途：

- 效果目標
- 攻擊目標
- 將被處理的卡

建議：

```txt
Border: BrandPinkLight
Glow: TargetGlow
```

---

### 10.6 Disabled

用途：

- 不可操作
- 不符合條件
- 暫時鎖定

建議：

```txt
Alpha: lower but readable
Text: MutedWhite
Border: Disabled
Glow: none
Shadow: keep subtle
```

Disabled 不代表消失。  
玩家仍需理解它存在，只是不能操作。

---

## 11. Background Treatment Token

背景應支援戰場，不是搶走焦點。

可用處理：

```txt
Brightness: -10% ~ -25%
Saturation: -5% ~ -15%
Contrast: -5% ~ -12%
Center glow: -10% ~ -20%
Vignette: subtle
```

禁止：

- 純黑遮罩蓋滿
- 大霧面板
- 讓背景失去科技感
- 背景比卡牌更亮

---

## 12. Battlefield Detail Token

戰場細節應該非常淡，堆出質感。

允許：

- Thin line
- Corner marker
- Small dot
- Faint grid
- Faint circuit
- Faint hex
- Scene center light
- Active lane line
- Card ground light

建議 alpha：

```txt
Normal detail: 0.03~0.08
Active detail: 0.08~0.18
Focus detail: 0.12~0.24
```

不要：

- 全部細節同時發亮
- 大面積科技圖案
- 讓 battlefield detail 搶過卡牌

---

## 13. Hardcode 規則

請避免在功能腳本中直接 hardcode 視覺參數。

不建議：

```csharp
image.color = new Color(1f, 0f, 0f, 0.5f);
text.color = Color.white;
```

建議：

```csharp
image.color = UcgToolUiPalette.BrandPink;
text.color = UcgToolUiPalette.BodyWhite;
```

若缺少 token：

1. 先在 `UcgToolUiPalette.cs` 新增語義 token
2. 再在元件中使用
3. 不要在多個元件內各自定義相似顏色

---

## 14. 驗收標準

每次 UI / Art Pass 後，請檢查：

- 是否使用既有 token？
- 是否新增了不必要的顏色？
- 是否把 BrandPink 用成大面積底色？
- 是否讓 FocusCyan 和 BrandPink 同時搶主視覺？
- 是否保持文字層級？
- 是否避免純白大量文字？
- 是否避免過亮 glow？
- 是否避免大 Panel？
- 是否保留卡牌與戰場為主角？
- 是否能被整理回 UcgToolUiPalette？

---

## 15. 一句話總結

Design Token 的目的不是限制美術，而是讓所有 UI 都像同一套產品：

> 深色玻璃為底，粉色負責主操作，青藍負責焦點與來源，白色負責資訊，陰影負責重量，細線與微光負責科技感。
