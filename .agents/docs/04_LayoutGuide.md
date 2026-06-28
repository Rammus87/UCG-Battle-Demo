# 04_LayoutGuide.md — UCG-Battle-Demo Layout / Safe Area 規範

## 1. 文件目的

這份文件定義 UCG-Battle-Demo 的 Layout 與 Safe Area 規範。

本專案是手機豎版優先的 Unity 教學對戰 Demo。  
任何 UI / Art / Battlefield 修改，都必須尊重本文件中的安全區。

Layout 修改請優先遵循：

```txt
UIAGENTS.md
docs/01_ArtBible.md
docs/02_DesignToken.md
docs/03_ComponentSpec.md
docs/04_LayoutGuide.md
```

---

## 2. Layout 核心原則

### 2.1 不要只看單張截圖調整

遊戲畫面會受到以下因素影響：

- 相機拉近 / 拉遠
- WebGL 尺寸
- 手機瀏海與 safe area
- 卡牌橫置
- 場景卡
- Toast 出現
- Hover / selected / target 動畫
- 不同 Lane 狀態

因此，不要只因為某張截圖看起來「中間很空」就直接壓縮 layout。

---

### 2.2 安全比緊湊重要

戰場看起來集中很重要，但不能犧牲：

- 橫置卡牌安全距離
- 中央場景卡安全區
- CardFanUI 安全區
- 右側資訊卡與卡牌距離
- Toast 不遮擋主要資訊

若美術感與規則安全區衝突，優先保留規則安全區。

---

### 2.3 Art Pass 不等於 Layout Pass

如果任務是 Art Pass，原則上不要修改：

- anchors
- position
- size
- layout reference
- lane spacing
- CardFanUI layout

除非使用者明確要求或本輪任務就是 Layout Pass。

---

## 3. 畫面主要區域

手機豎版畫面可分為以下區域：

```txt
[上方 HUD / 回合導航]

[敵方 Lane 區]

[中央場景卡區]

[我方 Lane 區]

[Toast / 教學提示帶]

[底部 CardFanUI 手牌區]
```

右側另有：

```txt
[牌庫 / 棄牌 / 資源資訊卡]
```

---

## 4. 上方 HUD Safe Area

### 4.1 功能

上方 HUD 顯示：

- 階段
- 主任務
- 補充說明
- 回合導航
- 小型狀態
- 右上功能按鈕

---

### 4.2 規範

HUD 應保持在畫面上方 safe area 內。

不要讓敵方 Lane 太靠近 HUD。  
敵方卡牌 hover / lift / selected 時，仍需保留安全距離。

---

### 4.3 禁止事項

不要：

- 把敵方 Lane 往上推到 HUD 下緣
- 讓 hover 卡牌碰到 HUD
- 讓 Toast 出現在 HUD 區
- 因為想壓縮戰場而犧牲上方安全距離

---

## 5. Lane / Slot Layout

### 5.1 Lane 定位

Lane 是角色對戰區。  
每一路必須能容納：

- 直立卡
- 橫置卡
- hover / selected / target 效果
- 卡牌陰影
- Slot 邊框與 glow

---

### 5.2 直立卡安全尺寸

建議安全尺寸：

```txt
直立卡牌安全尺寸：約 180 x 244
```

此尺寸包含：

- 卡牌本體
- 邊框
- shadow
- hover 微幅放大
- selected / target rim

---

### 5.3 橫置卡安全尺寸

UCG 場上角色卡可能橫置。  
因此 Lane 不能只依直立卡排版。

建議安全尺寸：

```txt
橫置卡牌安全尺寸：至少 248 x 184
理想值：約 260 x 190
```

橫置後卡牌寬度變大，必須保留水平安全距離。

---

### 5.4 Lane 間距

建議：

```txt
Lane center spacing：至少 300
Lane visual gap：不要低於 36，建議 42+
```

不要為了畫面看起來集中，把 Lane 壓到橫置卡會碰撞。

---

### 5.5 Slot 視覺與 hitbox

Slot 視覺可以加：

- shadow
- edge trace
- top highlight
- corner marker
- glow

但不要改變：

- drop hitbox
- click hitbox
- drag 判定
- Lane selection 邏輯

新增裝飾 Image 必須：

```csharp
raycastTarget = false;
```

---

## 6. 中央場景卡區

### 6.1 場景卡的重要性

UCG 有場景卡，因此中央場景卡區是核心戰場區域。

場景區不是空白區。  
也不是可隨意侵入的美術空間。

---

### 6.2 場景卡安全區

建議：

```txt
場景卡保留區：至少 520 x 220
目前場景卡約：468 x 184
場景卡與上下 Lane 間距：建議 48 ~ 56
```

---

### 6.3 場景區不能被侵入

以下元素不得侵入場景卡安全區：

- 敵方 Lane
- 我方 Lane
- Toast
- 右側資訊卡
- HUD
- 卡牌 hover 動畫
- 大型 VFX

---

### 6.4 場景區視覺

可使用：

- corner marker
- thin border
- center light
- faint line
- small dot
- subtle hologram detail
- ground shadow

不要使用：

- 大面板
- 大玻璃板
- 場景區霧面遮罩
- 大色塊

---

## 7. 我方 Lane 與 CardFanUI

### 7.1 CardFanUI 是底部主要手牌區

CardFanUI 是目前底部手牌扇形 UI。  
除非使用者明確要求，請不要修改 CardFanUI layout。

不要修改：

- 手牌位置
- 手牌排列
- 手牌扇形角度
- 手牌縮放
- 手牌拖曳邏輯
- CardFanUI layout 參數

---

### 7.2 我方 Lane 安全距離

我方 Lane 不應太靠近 CardFanUI。

原因：

- 手牌 hover 需要空間
- 拖曳卡牌需要空間
- Toast 可能出現在玩家 Lane 與手牌之間
- 手機底部 safe area 需要保留

---

### 7.3 禁止事項

不要：

- 為了壓縮戰場把我方 Lane 往下推
- 讓 Toast 蓋住手牌
- 讓 Lane 視覺與手牌扇形混在一起
- 修改 CardFanUI 來解決戰場空間問題

---

## 8. Toast / 教學提示 Layout

### 8.1 Toast 建議位置

Toast 通常應位於：

```txt
玩家 Lane 下方
CardFanUI 上方
中央場景區之外
```

---

### 8.2 Toast 安全規範

Toast 不應遮住：

- 場景卡
- active lane 主要卡牌
- 右側資訊卡
- 手牌 Fan UI
- HUD

---

### 8.3 Toast 尺寸

Toast 寬度不應過大。

建議：

```txt
寬度不超過 620 ~ 680
```

依照實際 Canvas 尺寸可調整，但原則是：

- 內容清楚
- 不佔滿整屏
- 不遮擋場景區

---

## 9. 右側資訊卡 Layout

### 9.1 右側資訊卡定位

右側資訊卡顯示：

- 牌庫
- 棄牌
- 資源
- 其他數值

它們是 HUD，不是戰場核心。

---

### 9.2 位置規範

右側資訊卡應：

- 固定靠右
- 垂直排列
- 尺寸一致
- 間距一致
- 不進入 Lane 主戰場區

---

### 9.3 與橫置卡安全距離

右側資訊卡必須考慮最右側 Lane 的橫置卡狀態。

建議：

```txt
右側資訊卡與最右側橫置卡牌距離：至少 40 ~ 48
```

若空間不足：

優先順序：

1. 縮窄右側資訊卡
2. 讓資訊卡更靠邊
3. 降低資訊卡視覺權重
4. 最後才考慮微調 Lane

不要為了資訊卡壓縮卡牌安全區。

---

## 10. 背景 Layout / Treatment

### 10.1 背景不是主角

背景應填滿畫面，但視覺上必須退後。

Background Treatment 可以做：

- 降亮度
- 降飽和
- 降中央光
- 加 subtle vignette
- 戰場周圍微弱暗化

---

### 10.2 禁止事項

不要：

- 用大黑板遮住背景
- 用大霧面遮罩
- 讓背景亮度高於卡牌
- 讓背景中央光圈搶過戰場

---

## 11. Zoom / Camera 注意事項

畫面可能有拉近 / 拉遠。

因此 layout 判斷不應只依單一狀態。

檢查 layout 時至少注意：

- 正常視角
- 操作提示出現時
- 場景卡存在時
- 橫置卡存在時
- Toast 出現時
- 右側資訊卡存在時
- 手牌 hover / drag 時

---

## 12. WebGL / 手機 Safe Area

未來 Demo 會接入 UCG-tool 網站，因此必須保持手機豎版安全。

注意：

- 上方瀏海區
- 底部操作區
- 觸控手勢區
- 不同手機高度
- WebGL Canvas 尺寸變化

不要使用只在目前 Editor 截圖中看起來剛好的絕對配置。

---

## 13. Layout Pass 工作規則

如果任務是 Layout Pass，請遵守：

1. 先分析重疊風險
2. 確認橫置卡安全區
3. 確認場景卡安全區
4. 確認 CardFanUI 不受影響
5. 確認右側資訊卡不壓卡牌
6. 最小修改
7. 不順手改美術風格

---

## 14. Art Pass 工作規則

如果任務是 Art Pass，請遵守：

1. 不修改 layout
2. 不修改 anchors
3. 不修改 position
4. 不修改 size
5. 不修改 hitbox
6. 不修改 CardFanUI layout
7. 只改材質、顏色、陰影、線條、細節

除非使用者明確要求，Art Pass 不應順手做 Layout Pass。

---

## 15. Layout 驗收標準

每次修改 layout 後，請檢查：

- 橫置卡是否會碰到隔壁 Lane？
- 橫置卡是否會碰到右側資訊卡？
- 場景卡區是否仍有 520 x 220 左右安全空間？
- 場景卡與上下 Lane 是否有足夠距離？
- Toast 是否避開場景卡？
- Toast 是否避開手牌？
- HUD 是否不壓敵方 Lane？
- 我方 Lane 是否不壓 CardFanUI？
- 手機豎版是否仍可讀？
- raycast / drag / drop 是否不受影響？

---

## 16. 一句話總結

LayoutGuide 的核心原則是：

> 可以讓畫面更集中，但不能犧牲 UCG 的橫置卡、場景卡、手牌 Fan UI 與手機豎版安全區。
