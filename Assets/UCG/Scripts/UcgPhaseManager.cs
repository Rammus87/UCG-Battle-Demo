using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    public enum UcgGamePhase
    {
        Start,
        Draw,
        SceneSetup,
        CharacterSetup,
        Upgrade,
        Open,
        EnterEffect,
        BattleEffect,
        BattleJudgement,
        End
    }

    [DisallowMultipleComponent]
    public class UcgPhaseManager : MonoBehaviour
    {
        public UcgGamePhase CurrentPhase = UcgGamePhase.CharacterSetup;
        public UcgTurnManager turnManager;
        public Text phaseInfoText;

        public void ResetPhase()
        {
            CurrentPhase = UcgGamePhase.SceneSetup;
            UpdatePhaseInfoText();
        }

        public void SetPhase(UcgGamePhase phase)
        {
            CurrentPhase = phase;
            UpdatePhaseInfoText();
        }

        public void NextPhase()
        {
            switch (CurrentPhase)
            {
                case UcgGamePhase.Start:
                    CurrentPhase = UcgGamePhase.Draw;
                    break;
                case UcgGamePhase.Draw:
                    CurrentPhase = UcgGamePhase.SceneSetup;
                    break;
                case UcgGamePhase.SceneSetup:
                    CurrentPhase = UcgGamePhase.CharacterSetup;
                    break;
                case UcgGamePhase.CharacterSetup:
                    CurrentPhase = UcgGamePhase.Upgrade;
                    break;
                case UcgGamePhase.Upgrade:
                    CurrentPhase = UcgGamePhase.Open;
                    break;
                case UcgGamePhase.Open:
                    CurrentPhase = UcgGamePhase.EnterEffect;
                    break;
                case UcgGamePhase.EnterEffect:
                    CurrentPhase = UcgGamePhase.BattleEffect;
                    break;
                case UcgGamePhase.BattleEffect:
                    CurrentPhase = UcgGamePhase.BattleJudgement;
                    break;
                case UcgGamePhase.BattleJudgement:
                    CurrentPhase = UcgGamePhase.End;
                    break;
                case UcgGamePhase.End:
                    CurrentPhase = UcgGamePhase.Start;
                    break;
                default:
                    CurrentPhase = UcgGamePhase.CharacterSetup;
                    break;
            }

            UpdatePhaseInfoText();
        }

        public bool CanPlaceCharacter(out string message)
        {
            if (CurrentPhase == UcgGamePhase.CharacterSetup)
            {
                message = "";
                return true;
            }

            message = CurrentPhase == UcgGamePhase.Upgrade
                ? "目前是升級階段，不能設置新角色"
                : "目前階段不能設置角色";
            return false;
        }

        public bool CanUpgrade(out string message)
        {
            if (CurrentPhase == UcgGamePhase.Upgrade)
            {
                message = "";
                return true;
            }

            message = CurrentPhase == UcgGamePhase.CharacterSetup
                ? "目前是角色設置階段，不能升級"
                : "目前階段不能升級";
            return false;
        }

        public string GetPhaseDisplayName()
        {
            switch (CurrentPhase)
            {
                case UcgGamePhase.Start:
                    return "起始階段";
                case UcgGamePhase.Draw:
                    return "抽牌階段";
                case UcgGamePhase.SceneSetup:
                    return "場景牌設置階段";
                case UcgGamePhase.CharacterSetup:
                    return "角色牌設置階段";
                case UcgGamePhase.Upgrade:
                    return "升級階段";
                case UcgGamePhase.Open:
                    return "開放階段";
                case UcgGamePhase.EnterEffect:
                    return "登場效果階段";
                case UcgGamePhase.BattleEffect:
                    return "戰鬥效果階段";
                case UcgGamePhase.BattleJudgement:
                    return "勝負判定階段";
                case UcgGamePhase.End:
                    return "結束階段";
                default:
                    return "未知階段";
            }
        }

        public string GetCurrentPhaseInstruction()
        {
            int laneNumber = turnManager != null && turnManager.ActiveNewLaneIndex >= 0
                ? turnManager.ActiveNewLaneIndex + 1
                : 0;

            switch (CurrentPhase)
            {
                case UcgGamePhase.CharacterSetup:
                    return laneNumber > 0
                        ? $"選擇一張角色卡，放到第 {laneNumber} 路。"
                        : "本回合不再開放新的戰鬥區。";
                case UcgGamePhase.Start:
                    return "新的回合開始，準備進入抽牌。";
                case UcgGamePhase.Draw:
                    return turnManager != null && turnManager.currentTurn <= 1
                        ? "第 1 回合不抽牌，準備展開場地。"
                        : "抽 1 張牌，補充手牌。";
                case UcgGamePhase.SceneSetup:
                    return "可以設置一張合法場景卡；沒有合適場景時會略過。";
                case UcgGamePhase.Upgrade:
                    return "可以選擇已登場角色升級一次，也可以結束升級。";
                case UcgGamePhase.Open:
                    return "雙方角色翻開，接著確認登場效果。";
                case UcgGamePhase.EnterEffect:
                    return "依先攻順序處理登場效果。";
                case UcgGamePhase.BattleEffect:
                    return "依先攻順序處理戰鬥效果；沒有效果就進入判定。";
                case UcgGamePhase.BattleJudgement:
                    return "正在比較雙方 BP，判定這一路的勝負。";
                case UcgGamePhase.End:
                    return "判定完成，確認後進入下一回合。";
                default:
                    return "此階段暫不開放拖牌操作。";
            }
        }

        public string GetPhaseInfoText()
        {
            int turn = turnManager != null ? turnManager.currentTurn : 1;
            return $"第 {turn} 回合｜{GetPhaseDisplayName()}\n{GetCurrentPhaseInstruction()}";
        }

        public void UpdatePhaseInfoText()
        {
            if (phaseInfoText == null) return;
            phaseInfoText.text = GetPhaseInfoText();
        }
    }
}
