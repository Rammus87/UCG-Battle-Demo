using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    public enum UcgTutorialStep
    {
        SetupLane1,
        SceneSetup,
        WaitOpponentSetup,
        Upgrade,
        Open,
        Effect,
        BattleJudgement,
        SetupLane2,
        WinCondition,
        Complete
    }

    [DisallowMultipleComponent]
    public class UcgTutorialGuide : MonoBehaviour
    {
        public Text tutorialText;
        public UcgTestMode currentMode;
        public UcgTutorialStep currentStep = UcgTutorialStep.SetupLane1;
        public bool isTutorialMode = true;
        public bool tutorialCompleted;
        Coroutine _promptFadeRoutine;
        string _lastPromptText;
        const float PromptFadeDuration = 0.18f;
        const string MissionGold = "#FFD66B";

        public void ResetForMode(UcgTestMode mode)
        {
            currentMode = mode;
            isTutorialMode = true;
            tutorialCompleted = false;
            currentStep = UcgTutorialStep.SetupLane1;
            _lastPromptText = "";
            ShowCurrentGoal();
        }

        public void SkipTutorial()
        {
            isTutorialMode = false;
            tutorialCompleted = true;
            if (tutorialText != null)
            {
                SetTutorialText("", false);
            }
        }

        public void ShowTutorialCompleteMessage()
        {
            if (tutorialText == null) return;

            currentStep = UcgTutorialStep.Complete;
            SetTutorialText(
                "模擬對戰完成！\n\n"
                + "你已經完成基礎實戰流程，接下來可以回到網站繼續探索卡牌與牌組。\n\n"
                + "點擊畫面任意處返回",
                false);
        }

        public void CompleteTutorial()
        {
            isTutorialMode = false;
            tutorialCompleted = true;
            if (tutorialText != null)
            {
                SetTutorialText("", false);
            }
        }

        public void SetStep(UcgTutorialStep step)
        {
            if (!isTutorialMode) return;
            currentStep = step;
        }

        public void NotifyCardPlayed(UcgCardData cardData, UcgPlayActionType actionType)
        {
            if (!isTutorialMode || cardData == null) return;

            if (actionType == UcgPlayActionType.PlayToEmptyArea)
            {
                currentStep = UcgTutorialStep.WaitOpponentSetup;
            }
            else if (actionType == UcgPlayActionType.Upgrade)
            {
                currentStep = UcgTutorialStep.Open;
            }
        }

        public void ShowPhasePrompt(string message)
        {
            if (tutorialText == null) return;
            if (tutorialCompleted)
            {
                SetTutorialText("", false);
                return;
            }

            string actionPrompt = ExtractActionPrompt(message);
            if (!string.IsNullOrWhiteSpace(actionPrompt))
            {
                SetTutorialText(FormatActionPrompt(actionPrompt), true);
                return;
            }

            SetTutorialText(isTutorialMode ? GetGoalText() : "", true);
        }

        public void ShowActionPrompt(string message)
        {
            if (tutorialText == null) return;
            if (tutorialCompleted)
            {
                SetTutorialText("", false);
                return;
            }

            string actionPrompt = ExtractActionPrompt(message);
            if (string.IsNullOrWhiteSpace(actionPrompt)) return;
            SetTutorialText(FormatActionPrompt(actionPrompt), true);
        }

        public void ShowCurrentGoal()
        {
            if (tutorialText == null) return;
            SetTutorialText(isTutorialMode ? GetGoalText() : "", false);
        }

        string ComposeTutorialPrompt(string message)
        {
            string goal = GetGoalText();
            string status = GetCompactStatusText(message);

            if (string.IsNullOrWhiteSpace(status) || status == goal || status.Contains(goal))
            {
                return goal;
            }

            return $"{goal}\n{status}";
        }

        string FormatActionPrompt(string message)
        {
            message = message != null ? message.Trim() : "";
            if (string.IsNullOrWhiteSpace(message)) return GetGoalText();
            if (message.Contains("<color=") || message.Contains("<size=")) return message;

            return $"<color={MissionGold}><size=26>{message}</size></color>";
        }

        string ExtractActionPrompt(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";

            string[] lines = message.Replace('\r', '\n').Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = CleanActionPromptLine(lines[i]);
                if (IsActionPromptLine(line)) return line;
            }

            return "";
        }

        string CleanActionPromptLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return "";

            line = line.Trim();
            int dividerIndex = line.LastIndexOf('｜');
            if (dividerIndex >= 0 && dividerIndex < line.Length - 1)
            {
                string tail = line.Substring(dividerIndex + 1).Trim();
                if (IsActionPromptLine(tail)) return tail;
            }

            int colonIndex = line.IndexOf('：');
            if (colonIndex >= 0 && colonIndex < line.Length - 1)
            {
                string tail = line.Substring(colonIndex + 1).Trim();
                if (IsActionPromptLine(tail)) return tail;
            }

            return line;
        }

        bool IsActionPromptLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (line.Contains("本回合先攻") || line.Contains("勝利路數") || line.Contains("遊戲結束")) return false;
            if (line.StartsWith("對手") && !line.Contains("請選擇對手")) return false;
            if (line.Contains("準備進入") || line.Contains("正在比較") || line.Contains("已完成")) return false;

            return line.Contains("請選擇")
                || line.Contains("請先選擇")
                || line.Contains("請依序選擇")
                || line.Contains("可以升級")
                || line.Contains("可以設置")
                || line.Contains("點擊完成")
                || line.Contains("放回牌庫")
                || line.Contains("放到底")
                || line.Contains("選擇目標");
        }

        string GetCompactStatusText(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";

            string[] lines = message.Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("先把") || line.StartsWith("現在可以") || line.StartsWith("下一回合"))
                {
                    continue;
                }
                return line;
            }

            return message.Trim();
        }

        void SetTutorialText(string value, bool animate)
        {
            if (tutorialText == null) return;
            value = value ?? "";
            if (_lastPromptText == value && tutorialText.text == value) return;

            _lastPromptText = value;
            if (_promptFadeRoutine != null)
            {
                StopCoroutine(_promptFadeRoutine);
                _promptFadeRoutine = null;
            }

            if (!animate || !isActiveAndEnabled)
            {
                tutorialText.text = value;
                SetTextAlpha(1f);
                return;
            }

            _promptFadeRoutine = StartCoroutine(FadePromptTextRoutine(value));
        }

        IEnumerator FadePromptTextRoutine(string value)
        {
            tutorialText.text = value;
            SetTextAlpha(0f);

            float elapsed = 0f;
            while (elapsed < PromptFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / PromptFadeDuration);
                SetTextAlpha(1f - Mathf.Pow(1f - t, 2f));
                yield return null;
            }

            SetTextAlpha(1f);
            _promptFadeRoutine = null;
        }

        void SetTextAlpha(float alpha)
        {
            if (tutorialText == null) return;
            Color color = tutorialText.color;
            color.a = alpha;
            tutorialText.color = color;
        }

        string GetGoalText()
        {
            switch (currentStep)
            {
                case UcgTutorialStep.SceneSetup:
                    return MissionPrompt("請選擇場景卡", "將其放到中央場景區");
                case UcgTutorialStep.WaitOpponentSetup:
                    return $"<color={MissionGold}><size=26>請觀察對手行動</size></color>";
                case UcgTutorialStep.Upgrade:
                    return GetUpgradeGoalText();
                case UcgTutorialStep.Open:
                    return $"<color={MissionGold}><size=26>請確認翻開的卡牌</size></color>";
                case UcgTutorialStep.Effect:
                    return $"<color={MissionGold}><size=26>請依照高亮提示選擇目標</size></color>";
                case UcgTutorialStep.BattleJudgement:
                    return $"<color={MissionGold}><size=26>請確認 BP 判定</size></color>";
                case UcgTutorialStep.SetupLane2:
                    return MissionPrompt("請選擇一張角色卡", "將其放上場地");
                case UcgTutorialStep.WinCondition:
                    return $"<color={MissionGold}><size=26>請繼續贏下三條路線</size></color>";
                case UcgTutorialStep.Complete:
                    return $"<color={MissionGold}>模擬對戰完成！</color>";
                default:
                    return MissionPrompt("請選擇一張角色卡", "將其放上場地");
            }
        }

        string MissionPrompt(string title, string subtitle)
        {
            return $"<color={MissionGold}><size=26>{title}</size></color>\n<size=18>{subtitle}</size>";
        }

        string GetUpgradeGoalText()
        {
            if (currentMode == UcgTestMode.MonsterAlienTest)
            {
                return MissionPrompt("請選擇升級卡", "疊到同名角色上");
            }

            if (currentMode == UcgTestMode.TeamTest)
            {
                return MissionPrompt("請選擇升級卡", "也可以直接結束升級");
            }

            return MissionPrompt("請選擇升級卡", "疊到場上角色上");
        }
    }
}
