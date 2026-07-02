# Battle Focus View Workflow

## 每次修改 Focus View 前必讀

Codex 修改 Focus / ViewTransformOnly 前必須先讀：

- Docs/BattleOverview/BATTLE_OVERVIEW_LAYOUT_RULES.md
- Docs/BattleOverview/BATTLE_VIEW_TRANSFORM_RULES.md
- Docs/BattleOverview/BATTLE_FOCUS_VIEW_RULES.md
- Docs/BattleOverview/BATTLE_FOCUS_VIEW_WORKFLOW.md

## 修改前必須先判斷

請先判斷問題屬於哪一種：

1. Focus scale 問題
2. targetX 問題
3. targetY 問題
4. viewport clipping 問題
5. layout reflow 被誤觸發
6. 正式流程覆蓋 debug focus
7. confirmation modal / phase 切換造成回 Overview
8. Scene / Deck / Trash 沒跟 content transform 一起縮放

不可直接猜測修改。

## 調整順序

1. 先確認 Debug Focus 是否正確。
2. 再確認正式流程 Focus 是否和 Debug Focus 使用同一套 target。
3. 再確認 Focus 是否被 Overview 覆蓋。
4. 再調 targetY / targetX。
5. 最後才考慮 scale。

## 禁止優先改的項目

除非明確證明必要，否則不要改：

- Battle Layout
- Lane pitch
- overviewCardSize
- Scene size
- Deck / Trash size
- playmatInnerRect
- playmatCenterY
- right rail
- Camera / Hand Fan / Top Prompt
- 戰鬥邏輯

## 驗收方式

Focus View 調整後必須驗收：

1. Debug Focus 01 / 02 / 04
2. 正式流程 CharacterSetup
3. 正式流程 Upgrade
4. 回 Overview
5. 確認窗出現時是否保持 Focus
6. 取消 / 確認後是否合理回切

## 回報格式

每次修改後回報：

- 修改檔案
- 是否只改 ViewTransform
- 是否改了 Layout
- targetScale
- targetX
- targetY
- Debug Focus 是否正常
- 正式 Focus 是否正常
- Scene / Deck / Trash 是否跳位
- git diff --check 結果