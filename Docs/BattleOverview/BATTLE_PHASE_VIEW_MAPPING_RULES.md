# Battle Phase View Mapping Rules

## 核心原則

視角不應只由 Phase 決定。

如果階段涉及卡牌來源、效果來源、戰鬥對象，必須再判斷：

- 是否有明確 `focusLaneIndex`
- 是否有明確 source card
- source card 是我方還是敵方
- 目前是全場操作還是單一路線操作

Focus View 只應用於需要觀看某一路、某張敵方卡、某次翻開、某次戰鬥比較的情境。

Overview View 用於全場資訊、玩家選擇、設置階段、結果整理。

---

## Phase View Mapping v1

### 0. Pre-game / Start / First Player Decision

View Mode:

Overview 遠視角

Focus Target:

None

Decision Source:

Battle phase / game flow state.

Notes:

- 正式對戰開始前不可 Focus 任一路。
- `Start` / first-player decision / opening preparation 必須保持 Overview。
- 不得因 active lane、current lane、selected lane fallback 在「起始」階段跳入 Lane Focus。

---

### 1. 先攻方場景牌設置階段

視角：

Overview 遠視角

規則：

- 場景牌放置在 Scene Area。
- Scene Area 不屬於某一路 Lane。
- 不得為 SceneSetup 強制使用 Lane Focus。
- 不得用假的 `focusLaneIndex` 來看 Scene Area。

---

### 2. 角色牌設置階段

#### 2-1. 先攻方角色牌設置步驟

視角：

Overview 遠視角

#### 2-2. 後攻方角色牌設置步驟

視角：

Overview 遠視角

規則：

- 角色設置階段需要玩家看全場與可用戰鬥區。
- 不要自動切近視角。
- 不得因為 active lane 存在就強制 Focus。

---

### 3. 升級階段

#### 3-1. 先攻方升級步驟

視角：

Overview 遠視角

#### 3-2. 後攻方升級步驟

視角：

Overview 遠視角

規則：

- 升級前玩家需要看全場可升級角色。
- 不要自動切近視角。
- 只有未來若做單張升級動畫特寫，才另行定義 Focus，不在本輪處理。

---

### 4. 開放階段

#### 4-1. 覆蓋角色依序翻開

視角：

Focus Lane View 近視角

規則：

- 不再使用所有覆蓋角色一起翻開的視角設計。
- 後續要改為依序翻開。
- 翻開順序：從最新設置的一路往最舊一路。
- 目前暫定類似 Lane 08 -> Lane 07 -> Lane 06 -> ... -> Lane 01。
- 每翻開一路：
  - Focus 到該 Lane
  - 播放翻開動畫
  - 再切到下一路
- 如果某一路沒有需要翻開的角色，可跳過該路。
- 此階段可以使用 Focus Lane View。
- Focus 必須使用已完成的 ViewTransformOnly + Focus background framing。
- 不得重排 Lane / Slot。

---

### 5. 登場時效果階段 / Enter Effect Phase

#### 5-1. 先攻方登場時效果步驟

#### 5-2. 後攻方登場時效果步驟

視角規則：

- 我方效果來源：Overview 遠視角
- 敵方效果來源：Focus Lane View 近視角

重要規則：

- 不得單純用「先攻方 / 後攻方」判斷視角。
- 必須用效果來源卡的 owner 判斷。
- `sourceCard.owner == Player` 時使用 Overview。
- `sourceCard.owner == Opponent` 時使用 Focus Lane View。
- 如果沒有明確 source card 或 `focusLaneIndex`，應保持 Overview。
- EnterEffect preserve Focus 只應在敵方來源或明確需要 Focus 的來源時使用。

---

### 6. 效果發動階段 / Activated Effect Phase

#### 6-1. 先攻方效果發動步驟

#### 6-2. 後攻方效果發動步驟

視角規則：

- 我方效果來源：Overview 遠視角
- 敵方效果來源：Focus Lane View 近視角

重要規則：

- 不得單純用 phase 判斷視角。
- 不得單純用先攻 / 後攻判斷視角。
- 必須根據效果來源卡 owner 判斷。
- 玩家選擇要發動哪張卡前，應保持 Overview。
- 敵方效果來源已確定時，才切 Focus。
- 我方效果來源已確定時，仍保持 Overview。

---

### 7. 勝負判定階段

#### 7-1. 各戰鬥區依序比較 BP

視角：

Focus Lane View 近視角

規則：

- 每次比較一個戰鬥區。
- Focus 到正在比較 BP 的 Lane。
- 顯示該 Lane 的雙方角色。
- 播放 BP 比較 / 勝負動畫。
- 再切到下一個 Lane。
- 不得一次用 Overview 同時處理所有 Lane 的 BP 比較。

#### 7-2. 處理勝負結果

視角：

Overview 遠視角

規則：

- 所有 Lane 比較完成後，回到 Overview。
- 勝負結果、整體狀態、回合整理應使用 Overview。
- 不要停留在最後一個 Focus Lane。

---

## View Mode Decision Summary

以下情況使用 Overview：

- SceneSetup
- CharacterSetup
- Upgrade
- 玩家需要全場選擇
- 我方效果發動 / 我方登場時效果
- 沒有明確 `focusLaneIndex`
- 勝負結果整理

以下情況使用 Focus Lane View：

- 開放階段依序翻開某一路
- 敵方登場時效果來源 Lane
- 敵方發動效果來源 Lane
- 勝負判定中正在比較 BP 的 Lane

---

## 禁止事項

- 不得把所有 phase 都強制 Focus。
- 不得因為 activeLaneIndex 存在就自動 Focus。
- 不得讓 SceneSetup 使用假的 Lane Focus。
- 不得在 CharacterSetup / Upgrade 自動切近視角。
- 不得用先攻 / 後攻直接判斷我方 / 敵方。
- 不得為了切視角重排 Lane / Slot / Scene / Deck / Trash。
- 不得修改 Focus `targetX` / `targetY` / `targetScale`。
- 不得修改 Scene position / size。
- 不得修改 Deck / Trash / right rail layout。
- 不得接回 legacy View API。

---

## Patch 01 Status

已完成：

- `SceneSetup` phase entry 改為 Overview ViewTransformOnly。
- `CharacterSetup` phase entry 改為 Overview ViewTransformOnly。
- `Upgrade` phase entry 改為 Overview ViewTransformOnly。
- `EnterEffect` phase entry 不再依 `_preserveFocusThroughEnterEffect` 強制維持 Focus。

保留的 Implementation Gap：

- Open phase 逐路翻牌 Focus sequence 尚未實作。
- EnterEffect 依 source card owner 決定 Overview / Focus 尚未實作。
- BattleEffect / ActivatedEffect 依 source card owner 決定 Overview / Focus 尚未實作。
- BattleJudgement 逐 Lane Focus 比較尚未實作。

## Patch 01b Status

已完成：

- `Start` / first-player decision / opening preparation 改為 Overview ViewTransformOnly。
- Initial battlefield view 不再自動 Focus active lane。
- Opening first-player decision sequence 不再自動 Focus active lane。

保留的 Implementation Gap：

- Debug Focus 按鈕可保留作為手動驗證工具。
- 後續正式 Focus 仍只應由明確允許的 phase / sequence 白名單觸發。

## Patch 02 Status

已完成：

- Open phase 改為 sequential lane reveal。
- View Mode：Focus Lane View。
- Order：Lane 08 -> Lane 01。
- 每一路翻開前會先套用 ViewTransformOnly Focus Lane View。
- Open sequence complete 後回 Overview，再交給 EnterEffect phase view decision。

保留的 Implementation Gap：

- EnterEffect 依 source card owner 決定 Overview / Focus 尚未實作。
- BattleEffect / ActivatedEffect 依 source card owner 決定 Overview / Focus 尚未實作。
- BattleJudgement 逐 Lane Focus 比較尚未實作。
