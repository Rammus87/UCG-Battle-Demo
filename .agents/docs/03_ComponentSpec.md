# 03_ComponentSpec.md — UCG-Battle-Demo UI Component 規格

## 1. 文件目的

這份文件定義 UCG-Battle-Demo 的 UI Component 規格。

所有 UI 元件修改都應優先遵循：

```txt
UIAGENTS.md
docs/01_ArtBible.md
docs/02_DesignToken.md
docs/03_ComponentSpec.md
```

本文件的目標是讓所有 UI 看起來像同一套產品，而不是不同階段臨時拼起來的 Unity Panel。

---

## 2. Component 設計原則

### 2.1 所有元件都要服務遊戲感

請不要只問：

> 這個 UI 漂亮嗎？

請問：

> 這個 UI 是否讓玩家更清楚理解戰場、卡牌、目前操作？

---

### 2.2 不要每個元件各自設計

所有元件都應使用同一套語言：

- 深色玻璃
- 細邊框
- 柔和陰影
- 粉色主操作
- 青藍焦點
- 弱白次要文字
- 細線與 corner detail

---

### 2.3 優先建立共用方法

若需要調整樣式，優先整理或擴充共用方法，例如：

```txt
ApplyGlassSurface
ApplyGameButtonStyle
ApplyDialogStyle
ApplyToastStyle
ApplyCounterCardStyle
ApplySlotStyle
ApplyCardPresentation
ApplyNavigationPillStyle
```

不要每個 UI 各寫一套顏色與陰影。

---

## 3. HUD Component

### 3.1 定位

HUD 是導航，不是畫面主角。

HUD 用來回答：

- 現在是什麼階段？
- 玩家現在該做什麼？
- 目前輪到誰？
- 目前有哪些關鍵狀態？

---

### 3.2 主提示 HUD

結構建議：

```txt
[小階段文字]
[主要任務]
[補充說明]
```

例：

```txt
角色設置階段
請選擇一張角色卡
可設置到目前可操作的 Lane
```

---

### 3.3 視覺規格

使用：

- Glass Level 2
- Border Brand
- Panel Shadow
- Top Highlight
- 適度 padding
- 細粉色 bottom accent line

文字：

```txt
小階段文字：Muted / Caption
主要任務：Hero / BrandPinkLight
補充說明：Body / Muted
```

---

### 3.4 禁止事項

不要：

- 做成亮紅色 Banner
- 做成 Debug log
- 使用純文字浮在背景上
- 使用大面積實心色塊
- 讓 HUD 搶過卡牌

---

## 4. Navigation Pill Component

### 4.1 定位

Navigation Pill 用於顯示：

- 當前回合
- 當前階段
- 勝利路數
- 小型狀態

---

### 4.2 視覺規格

使用：

- Glass Level 1
- Border Subtle
- Radius Small / Medium
- Caption typography
- 關鍵字可使用 BrandPinkLight
- 可加入左右細線裝飾

---

### 4.3 禁止事項

不要：

- 使用 Debug text
- 無底板純文字
- 過大
- 搶主提示 HUD

---

## 5. Button Component

### 5.1 定位

Button 是操作入口，不是裝飾品。

---

### 5.2 Button 類型

建議分為：

```txt
Primary Button
Secondary Button
Icon Button
Disabled Button
```

---

### 5.3 Primary Button

用途：

- 確認
- 下一步
- 主要操作

視覺：

- Glass Level 1 或 Level 2
- Border Brand
- BrandPinkLight 文字或白字
- hover 使用 Active Glow
- pressed 有輕微內凹感
- Radius Medium

---

### 5.4 Secondary Button

用途：

- 取消
- 返回
- 次要操作

視覺：

- Glass Level 1
- Border Subtle
- SoftWhite 文字
- hover 使用 FocusCyan 細框

---

### 5.5 Icon Button

用途：

- 右上功能按鈕
- 小型工具按鈕

視覺：

- 小尺寸
- Glass Level 1
- 細邊框
- icon / text 居中
- hover 不要過亮

---

### 5.6 Disabled Button

視覺：

- 透明度降低
- MutedWhite 文字
- Border Disabled
- 無 glow
- 仍保留可辨識形狀

---

### 5.7 禁止事項

不要：

- 使用 Unity 預設 Button 色塊
- 每個按鈕不同圓角
- 每個按鈕不同 hover 邏輯
- 使用大面積粉色實心底
- 讓按鈕比卡牌更搶眼

---

## 6. Toast Component

### 6.1 定位

Toast 是短暫提示，用於：

- 效果發動
- 操作結果
- 系統提示
- 教學提示

Toast 不是長期資訊欄。

---

### 6.2 結構

建議結構：

```txt
[Icon] [Title]
       [Body]
       [Muted detail optional]
```

左側可有 BrandPink accent bar。

---

### 6.3 視覺規格

使用：

- Glass Level 2
- Border Brand 或 Border Subtle
- 左側 BrandPink accent bar
- Panel Shadow
- Radius Medium
- Title 使用 BrandPinkLight
- Body 使用 BodyWhite
- Detail 使用 MutedWhite

---

### 6.4 動畫

Toast 動畫應：

- Fade in
- Slight slide up
- Hold
- Fade out

不要瞬間跳出。  
不要長時間遮住戰場。

---

### 6.5 位置規範

Toast 不應擋住：

- 主要卡牌
- 場景區
- active lane
- 手牌 Fan UI

通常應位於玩家 Lane 與手牌區之間，並避開場景卡安全區。

---

## 7. Dialog Component

### 7.1 定位

Dialog 是正式遊戲操作確認，不是 Unity Panel。

---

### 7.2 結構

標準結構：

```txt
[小標題]
[主問題]
[補充說明]
[Button Row]
```

例：

```txt
操作確認
確定設置到第 1 路嗎？
設置後將進入下一步操作。
[取消] [確定]
```

如果目前沒有取消流程，不要修改遊戲邏輯。  
可先只保留「確定」，但視覺結構仍要像正式 Dialog。

---

### 7.3 視覺規格

使用：

- Glass Level 3
- Border Brand
- Panel Shadow 強一點
- Radius Large
- 內部 padding 明顯
- 標題使用 BrandPinkLight
- 主問題使用 SoftWhite
- 補充說明使用 MutedWhite
- Button 使用 Button Component

---

### 7.4 禁止事項

不要：

- 半透明矩形 + 一行文字
- 無標題
- 無補充說明
- Unity 預設 Button
- Dialog 背景太亮
- Dialog 像 Debug Box

---

## 8. Counter Card Component

### 8.1 定位

Counter Card 用於顯示右側資訊，例如：

- 牌庫
- 棄牌
- 資源
- 其他數字

---

### 8.2 結構

```txt
[Title]
[Number]
[Optional small detail]
```

---

### 8.3 視覺規格

使用：

- Glass Level 2
- Border Subtle / Border Brand
- Panel Shadow
- Radius Medium
- Title 使用 MutedWhite
- Number 使用 Counter Number / BrandPinkLight
- 數字是主視覺
- 卡片之間間距一致

---

### 8.4 位置規範

Counter Card 應：

- 固定靠右
- 不壓到場上卡牌
- 不進入 Lane 主要區域
- 和最右側橫置卡保持安全距離

若空間不足，優先縮窄 Counter Card，不要壓縮卡牌安全區。

---

### 8.5 禁止事項

不要：

- 像 Debug 數字欄
- 標題與數字同大小
- 顏色過亮
- 壓住卡牌
- 和卡牌在同一視覺層級打架

---

## 9. Slot Component

### 9.1 定位

Slot 是玩家理解戰場的核心元件。

Slot 告訴玩家：

- 這裡可以放牌
- 這裡目前有卡
- 這裡可以操作
- 這裡是目標

---

### 9.2 Default Slot

視覺：

- Slot Glass
- Border Subtle
- 內陰影
- bottom shadow
- top highlight
- 可有非常淡的 edge trace
- 不要比卡牌更亮

---

### 9.3 Hover Slot

視覺：

- Border Focus
- Hover Glow
- Shadow 稍強
- 不要整格變青藍

---

### 9.4 Playable Slot

視覺：

- Border Brand
- Active Glow
- 可加入 very subtle pulse
- 不要整格變粉色

---

### 9.5 Target Slot

視覺：

- Border Target
- Target Glow
- 外圈柔和 focus
- 不遮住卡牌

---

### 9.6 Disabled Slot

視覺：

- 降低透明度
- Border Disabled
- 無 glow
- 仍可辨識

---

### 9.7 Occupied Slot

有卡牌時，Slot 應退為底座：

- 保留 ground shadow
- 保留非常淡底部暗面
- 不搶卡牌
- 不把卡牌洗白

---

### 9.8 禁止事項

不要：

- 只剩四個角標
- 大色塊
- 亮到比卡牌搶眼
- 使用大面積粉 / 青填色
- 覆蓋卡圖
- 影響 raycast / drag / drop

新增裝飾 Image 必須 `raycastTarget = false`。

---

## 10. Scene Area Component

### 10.1 定位

Scene Area 是中央場景卡區。

UCG 有場景卡，因此場景區是核心戰場元素，不是空白區。

---

### 10.2 視覺規格

可使用：

- corner marker
- thin border
- center light
- faint horizontal / vertical line
- subtle hologram detail
- ground shadow
- very faint scene label

---

### 10.3 禁止事項

不要：

- 大 Panel
- 大玻璃板
- 場景區霧面遮罩
- 被 Toast 蓋住
- 被 Lane 擠掉
- 被右側資訊卡壓到

---

## 11. Card Presentation Component

### 11.1 定位

卡牌是主角。

Card Presentation 應使卡牌像實體卡牌，而不是 Sprite。

---

### 11.2 Front Card

場上正面卡應具備：

- bottom shadow
- edge highlight
- subtle rim
- hover lift
- selected rim
- target rim
- source rim

---

### 11.3 Back Card

背面卡必須使用正式 UCG 卡背：

```txt
Assets/Resources/UCG/CardBacks/ucg_card_back_standard.png
```

主 Image 在 Normal / Hover / Selected 狀態必須：

```txt
Color.white
alpha = 1
```

不得出現白膜、霧面或泛白。

---

### 11.4 Hover Card

Hover 規格：

- scale 只能小幅增加
- shadow 增強
- highlight 增強
- 不要大幅放大
- 不要擋住其他卡

建議 scale：

```txt
1.00 -> 1.02 ~ 1.03
```

---

### 11.5 Selected Card

Selected 規格：

- FocusCyan rim
- Source Glow
- shadow stronger
- 不洗白卡面

---

### 11.6 Target Card

Target 規格：

- BrandPinkLight rim
- Target Glow
- 微弱 pulse 可接受
- 不洗白卡面

---

### 11.7 Disabled Card

Disabled 規格：

- 降低飽和
- 可加灰階 wash
- 保留 shadow
- 保留卡牌輪廓
- 不要完全透明

---

### 11.8 禁止事項

不要：

- 全卡白色 overlay
- 大面積 gloss
- hover 放大過多
- selected 把整張卡變青
- target 把整張卡變粉
- disabled 變成完全不可見

---

## 12. Battlefield Detail Component

### 12.1 定位

Battlefield Detail 用來增加戰場 visual density。

它不是主要 UI，而是環境層。

---

### 12.2 可用元素

允許使用：

- thin line
- corner marker
- small dot
- faint grid
- faint circuit
- faint hex
- active lane line
- scene center light
- card ground light
- subtle energy beam

---

### 12.3 強度規範

一般 detail：

```txt
alpha 0.03~0.08
```

Active detail：

```txt
alpha 0.08~0.18
```

Focus detail：

```txt
alpha 0.12~0.24
```

---

### 12.4 禁止事項

不要：

- 大面積 Panel
- 大矩形
- 大霧面遮罩
- 大量高亮粒子
- 搶過卡牌
- 搶過 Slot

---

## 13. Tooltip / Guide Component

### 13.1 定位

Tooltip / Guide 是輔助說明，不是主 HUD。

---

### 13.2 視覺規格

使用：

- Glass Level 1 或 Level 2
- MutedWhite / BodyWhite
- 小型 accent
- 可有箭頭或指示線
- 不擋住主要操作區

---

### 13.3 禁止事項

不要：

- 大段文字覆蓋戰場
- 多個 Tooltip 同時出現
- 像 Debug log
- 蓋住場景區

---

## 14. Component 實作規則

### 14.1 裝飾 Image

所有純裝飾 Image 必須：

```csharp
raycastTarget = false;
```

包括：

- shadow
- glow
- corner marker
- line
- grid
- highlight
- accent

---

### 14.2 不改 hitbox

UI 視覺改動不應改變：

- drag
- drop
- click
- hover
- card placement
- lane selection

除非使用者明確要求。

---

### 14.3 不改 layout

若任務是 Art Pass，請不要順手調整 layout。

尤其不要改：

- CardFanUI layout
- Lane safety spacing
- Scene area safety
- Right counter safety
- WebGL bridge

---

### 14.4 共用樣式

若某樣式會用在兩個以上元件，請抽成共用方法或 token。

不要複製貼上多份相似程式。

---

## 15. Component 驗收標準

每次完成元件修改後，請檢查：

- 此元件是否符合 ArtBible？
- 是否使用 DesignToken？
- 是否和其他元件同一套語言？
- 是否避免 Unity 預設 UI 感？
- 是否避免大 Panel？
- 是否沒有搶過卡牌？
- 是否保留手機豎版可讀性？
- 是否沒有破壞 raycast / drag / drop？
- 是否沒有改規則或資料？

---

## 16. 一句話總結

ComponentSpec 的目的，是讓每個 UI 元件都有明確職責與一致外觀：

> HUD 負責導航，Toast 負責短提示，Dialog 負責確認，Counter 負責數字，Slot 負責戰場理解，Card 負責主視覺，Battlefield Detail 負責遊戲感。
