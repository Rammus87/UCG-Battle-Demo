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

            SetTutorialText(isTutorialMode ? ComposeTutorialPrompt(message) : message, true);
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
                    return "可以設置場景時，把合適的場景卡放到中央場景區。";
                case UcgTutorialStep.WaitOpponentSetup:
                    return "對手正在行動，先觀察這一路的對戰狀況。";
                case UcgTutorialStep.Upgrade:
                    return GetUpgradeGoalText();
                case UcgTutorialStep.Open:
                    return "開放階段會翻開雙方卡牌，接著處理登場效果。";
                case UcgTutorialStep.Effect:
                    return "處理效果時，依照高亮提示選擇目標。";
                case UcgTutorialStep.BattleJudgement:
                    return "比較雙方 BP，較高的一方會贏下這一路。";
                case UcgTutorialStep.SetupLane2:
                    return "第 2 路開放了，選一張角色卡放上場吧！";
                case UcgTutorialStep.WinCondition:
                    return "繼續贏下三條不同路線，就能完成模擬對戰。";
                case UcgTutorialStep.Complete:
                    return "模擬對戰完成！";
                default:
                    return "先從第 1 路開始，選擇一張角色卡放上場吧！";
            }
        }

        string GetUpgradeGoalText()
        {
            if (currentMode == UcgTestMode.MonsterAlienTest)
            {
                return "現在可以把同名高等級角色疊上去，讓這一路變得更強。";
            }

            if (currentMode == UcgTestMode.TeamTest)
            {
                return "可以用高等級卡升級三人突擊隊，也可以直接結束升級。";
            }

            return "現在可以把角色升級，讓這一路變得更強！";
        }
    }
}
