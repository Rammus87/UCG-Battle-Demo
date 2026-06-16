using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UcgPlayArea : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Drop State")]
        public RectTransform cardSlot;
        public UcgCardView currentCard;
        public Vector2 placedCardSize = new Vector2(204f, 296f);

        [Header("Feedback")]
        public Image highlightImage;
        public Color defaultColor = new Color(0.08f, 0.16f, 0.22f, 0.35f);
        public Color hoverColor = new Color(0.2f, 0.55f, 0.72f, 0.45f);
        public Color occupiedColor = new Color(0.14f, 0.12f, 0.18f, 0.42f);

        void Awake()
        {
            if (cardSlot == null) cardSlot = transform as RectTransform;
            if (highlightImage == null) highlightImage = GetComponent<Image>();
            RefreshHighlight();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlightImage == null) return;
            highlightImage.color = IsOccupied() ? occupiedColor : hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshHighlight();
        }

        public void OnDrop(PointerEventData eventData)
        {
            RefreshCurrentCard();
            if (IsOccupied()) return;
            if (eventData.pointerDrag == null) return;

            var cardView = eventData.pointerDrag.GetComponent<UcgCardView>();
            var dragCard = eventData.pointerDrag.GetComponent<UIDragCard>();
            var cardRect = eventData.pointerDrag.transform as RectTransform;

            if (cardView == null || dragCard == null || cardRect == null) return;

            dragCard.MarkDropped();
            cardView.SetSelected(false);

            currentCard = cardView;
            cardRect.SetParent(cardSlot, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localEulerAngles = Vector3.zero;
            cardRect.localScale = Vector3.one;
            cardRect.sizeDelta = placedCardSize;
            cardRect.SetAsLastSibling();

            var canvasGroup = cardRect.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            RefreshHighlight();
        }

        bool IsOccupied()
        {
            RefreshCurrentCard();
            return currentCard != null;
        }

        void RefreshCurrentCard()
        {
            if (currentCard != null && currentCard.transform.parent == cardSlot) return;
            currentCard = null;

            if (cardSlot == null) return;

            for (int i = 0; i < cardSlot.childCount; i++)
            {
                var card = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null)
                {
                    currentCard = card;
                    return;
                }
            }
        }

        void RefreshHighlight()
        {
            if (highlightImage == null) return;
            highlightImage.color = IsOccupied() ? occupiedColor : defaultColor;
        }
    }
}
