using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UcgCardInfoPanel : MonoBehaviour
    {
        public Text infoText;

        public void ShowCard(UcgCardData cardData)
        {
            if (cardData == null)
            {
                ShowMessage("請選擇一張卡牌");
                return;
            }

            if (cardData.IsSceneCard())
            {
                string sceneDescription = string.IsNullOrWhiteSpace(cardData.sceneDescription)
                    ? "無"
                    : cardData.sceneDescription;
                ShowMessage(
                    $"卡名：{cardData.cardName}\n" +
                    "類型：場景卡\n" +
                    $"回合 / 光點：{cardData.sceneTurnCost}\n" +
                    $"場景效果：{sceneDescription}");
                return;
            }

            string teamTag = string.IsNullOrWhiteSpace(cardData.teamTag) ? "無" : cardData.teamTag;
            string effectText = string.IsNullOrWhiteSpace(cardData.effectDescription)
                ? "無"
                : cardData.effectDescription;
            ShowMessage(
                $"卡名：{cardData.cardName}\n" +
                $"角色：{cardData.characterName}\n" +
                $"類型：{cardData.cardCategory}\n" +
                $"等級：Lv.{cardData.level}\n" +
                $"隊伍：{teamTag}\n" +
                $"效果：{effectText}");
        }

        public void ShowMessage(string message)
        {
            gameObject.SetActive(true);
            if (infoText == null) return;
            infoText.text = message;
        }

        public void Clear()
        {
            if (infoText != null) infoText.text = "";
            gameObject.SetActive(false);
        }
    }
}
