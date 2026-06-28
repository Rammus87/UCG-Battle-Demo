# 06_Workflow.md — UCG-Battle-Demo Codex 工作流程

## 1. 目的

本文件規範 Codex 在本專案中的固定工作流程。

目標：

> 每一次修改都朝 Target UI v1 穩定前進，而不是每輪自由發揮。

---

## 2. 每次收到任務後

### Step 1：先閱讀文件

依序閱讀：

1. UIAGENTS.md
2. docs/01_ArtBible.md
3. docs/02_DesignToken.md
4. docs/03_ComponentSpec.md
5. docs/04_LayoutGuide.md
6. docs/05_AnimationGuide.md

不得跳過。

---

### Step 2：理解本輪目標

先確認本輪屬於哪一類：

- Art Pass
- Layout Pass
- Animation Pass
- Bug Fix
- Gameplay（只有使用者明確要求）

不要混合多種類型。

---

### Step 3：比對 Target UI

先比較：

- Current Screenshot
- Target UI v1

回答：

- 哪裡最不像？
- 哪裡可以保持？
- 哪裡應該優先改善？

不要直接開始修改。

---

### Step 4：最小修改原則

一次只改善一個主題。

例如：

- Battlefield
- Slot
- Card
- Dialog
- Toast
- Counter

不要一次重做全部 UI。

---

### Step 5：實作

遵守所有 Design 文件。

優先：

- 共用方法
- 共用 Token
- 共用 Component

不要重複寫樣式。

---

### Step 6：自我檢查

完成後確認：

□ 有沒有改到規則？
□ 有沒有改 AI？
□ 有沒有改 CardFanUI？
□ 有沒有新增大 Panel？
□ 有沒有新增 Hardcode Color？
□ 有沒有破壞 Layout Safe Area？
□ 有沒有偏離 ArtBible？

若有，先修正。

---

### Step 7：回報格式

只需要：

完成項目：
- ...

修改檔案：
- ...

未修改：
- 規則
- AI
- CardFanUI
- WebGL
- cards.json
- loader
- SFX
- Debug Tool

驗證：
- git diff --check
- Unity Play Mode 是否測試

不要輸出冗長報告。

---

## 3. 永遠不要做的事

不要：

- 為了填滿畫面新增大面板
- 任意更改 Layout
- 自行修改規則
- 大量 Hardcode 顏色
- 每個元件各做一套風格
- 因單一截圖就改整體配置

---

## 4. 永遠優先的事

優先：

1. 保持規則正確
2. 保持教學流程
3. 保持手機豎版
4. 保持卡牌可讀性
5. 提升 Game Feel
6. 接近 Target UI v1

---

## 5. 完成定義（Definition of Done）

一次 Art Pass 完成代表：

- 已符合 ArtBible
- 已使用 DesignToken
- 已符合 ComponentSpec
- 已遵守 LayoutGuide
- 已遵守 AnimationGuide
- 沒有破壞遊戲功能
- 畫面比上一版更接近 Target UI v1

不是：

「有改東西」

而是：

「整體品質提升且方向正確」。
