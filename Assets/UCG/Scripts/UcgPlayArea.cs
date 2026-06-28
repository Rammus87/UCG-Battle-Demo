using System.Collections;
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
        public UcgCardView topCard;
        public Vector2 placedCardSize = new Vector2(204f, 296f);
        public Vector2 upgradeStackOffset = new Vector2(14f, 18f);
        public Text resultText;
        public UcgTutorialGuide tutorialGuide;
        public UcgBattleLane ownerLane;
        public UcgTurnManager turnManager;
        public UcgPhaseManager phaseManager;

        [Header("Feedback")]
        public Image highlightImage;
        public Color defaultColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.070f);
        public Color hoverColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.060f);
        public Color occupiedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.040f);
        public Color activeSetupColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.052f);
        public Color upgradeAvailableColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.075f);
        public Color validDropColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.050f);
        public Color invalidDropColor = new Color(0.46f, 0.08f, 0.12f, 0.04f);
        public Color lockedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.018f);

        UcgLaneHighlightState _highlightState = UcgLaneHighlightState.Normal;
        bool _isPointerInside;
        UcgGuidancePulse _slotPulse;
        RectTransform _guideArrowRect;
        Text _guideArrowText;
        UcgGuidancePulse _guideArrowPulse;
        RectTransform _guideRingRect;
        Image _guideRingImage;
        Outline _guideRingOutline;
        UcgGuidancePulse _guideRingPulse;
        Coroutine _successFeedbackRoutine;

        void Awake()
        {
            if (cardSlot == null) cardSlot = transform as RectTransform;
            if (highlightImage == null) highlightImage = GetComponent<Image>();
            EnsureGuideArrow();
            RefreshHighlight();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;
            if (highlightImage == null) return;
            if (_highlightState != UcgLaneHighlightState.Normal)
            {
                RefreshSlotFrameStyle();
                return;
            }
            highlightImage.color = IsOccupied() ? occupiedColor : hoverColor;
            RefreshSlotFrameStyle();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            RefreshHighlight();
        }

        public void OnDrop(PointerEventData eventData)
        {
            RefreshTopCard();
            if (eventData.pointerDrag == null) return;

            var cardView = eventData.pointerDrag.GetComponent<UcgCardView>();
            var dragCard = eventData.pointerDrag.GetComponent<UIDragCard>();
            var cardRect = eventData.pointerDrag.transform as RectTransform;

            if (cardView == null || dragCard == null || cardRect == null) return;

            if (!TryDropCard(cardView, dragCard, cardRect, out string dropMessage))
            {
                ShowResult(dropMessage);
            }
        }

        public bool TryDropCard(UcgCardView cardView, UIDragCard dragCard, RectTransform cardRect, out string message)
        {
            RefreshTopCard();
            message = "";

            if (cardView == null || dragCard == null || cardRect == null)
            {
                message = "不是有效卡牌";
                return false;
            }

            UcgHandDemo demo = FindFirstObjectByType<UcgHandDemo>();
            if (demo == null)
            {
                message = "放置驗證器尚未建立";
                RefreshHighlight();
                return false;
            }

            if (!demo.ValidatePlayerCardDrop(
                    cardView,
                    ownerLane,
                    UcgPlayerCardDropTarget.Lane,
                    out message,
                    out UcgPlayActionType actionType,
                    true))
            {
                RefreshHighlight();
                return false;
            }

            dragCard.MarkDropped();
            cardView.SetSelected(false);

            int existingCardCount = CountPlacedCards();
            currentCard = currentCard == null ? cardView : currentCard;
            topCard = cardView;
            cardRect.SetParent(cardSlot, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = GetPlacedCardPosition(actionType, existingCardCount);
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

            cardView.SetDragging(false);
            cardView.ResetVisualState();
            cardView.SetFaceDown(true);
            ConfigureFieldCardClickTarget(cardView);
            RefreshHighlight();
            LockPlacedCards();
            ApplyPlacedCardSorting();
            demo.BeginPendingBattlefieldAction(cardView, dragCard, cardRect, ownerLane, actionType, message);
            return true;
        }

        public void RemovePendingCard(UcgCardView cardView)
        {
            if (cardView == null || cardSlot == null) return;

            if (currentCard == cardView) currentCard = null;
            if (topCard == cardView) topCard = null;
            RefreshHighlight();
            ApplyPlacedCardSorting();
        }

        public UcgCardView UpgradeFromEffect(UcgCardData cardData, UcgCardInfoPanel infoPanel, Sprite fallbackSprite, Font placeholderFont)
        {
            RefreshTopCard();
            if (cardData == null || cardSlot == null || topCard == null) return null;

            if (!cardData.IsExternalCard() && cardData.cardImage == null)
            {
                cardData.cardImage = fallbackSprite;
            }

            int existingCardCount = CountPlacedCards();
            var cardObject = new GameObject("Effect Upgrade Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(cardSlot, false);

            var cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = GetPlacedCardPosition(UcgPlayActionType.Upgrade, existingCardCount);
            cardRect.localEulerAngles = Vector3.zero;
            cardRect.localScale = Vector3.one;
            cardRect.sizeDelta = placedCardSize;
            cardRect.SetAsLastSibling();

            var image = cardObject.GetComponent<Image>();
            image.raycastTarget = true;

            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            Text label = CreateEffectPlaceholderText(cardRect, placeholderFont);
            var view = cardObject.AddComponent<UcgCardView>();
            view.cardImage = image;
            view.placeholderText = label;
            view.selectedSizeMultiplier = 1.12f;
            view.SetInfoPanel(infoPanel);
            view.Initialize(cardData);
            view.SetFaceDown(false);
            view.SetBattlefieldLocked(true);
            view.EnterEffectResolved = true;

            topCard = view;
            if (currentCard == null) currentCard = view;
            ConfigureFieldCardClickTarget(view);
            LockPlacedCards();
            ApplyPlacedCardSorting();
            RefreshHighlight();
            PlaySuccessfulActionFeedback(true);
            return view;
        }

        public bool RemoveTopCardFromEffect(UcgCardData expectedCard, out UcgCardData removedCard)
        {
            removedCard = null;
            RefreshTopCard();
            if (topCard == null || topCard.CardData == null) return false;
            if (expectedCard != null && !ReferenceEquals(topCard.CardData, expectedCard)) return false;

            UcgCardView removedView = topCard;
            removedCard = removedView.CardData;
            if (Application.isPlaying)
            {
                removedView.transform.SetParent(null, false);
                Destroy(removedView.gameObject);
            }
            else
            {
                DestroyImmediate(removedView.gameObject);
            }

            RefreshPlacedCardsAfterStackMove();
            return true;
        }

        public void ShowResult(string message)
        {
            if (resultText == null) return;
            resultText.text = message;
        }

        public int GetStackCount()
        {
            return CountPlacedCards();
        }

        public UcgCardView GetTopCard()
        {
            RefreshTopCard();
            return topCard;
        }

        public void RefreshPlacedCardsAfterStackMove()
        {
            RefreshTopCard();
            LockPlacedCards();

            if (cardSlot != null)
            {
                for (int i = 0; i < cardSlot.childCount; i++)
                {
                    var card = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                    if (card == null) continue;
                    ConfigureFieldCardClickTarget(card);
                }
            }

            ApplyPlacedCardSorting();
            RefreshHighlight();
        }

        public int GetCurrentBp()
        {
            UcgCardView card = GetTopCard();
            if (card == null || card.CardData == null) return 0;
            return card.CardData.GetBpByStackCount(GetStackCount());
        }

        public void ResetArea()
        {
            if (cardSlot != null)
            {
                for (int i = cardSlot.childCount - 1; i >= 0; i--)
                {
                    Transform child = cardSlot.GetChild(i);
                    if (child.GetComponent<UcgCardView>() == null) continue;

                    if (Application.isPlaying)
                    {
                        child.SetParent(null, false);
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            currentCard = null;
            topCard = null;
            SetHighlightState(UcgLaneHighlightState.Normal);
        }

        public void SetHighlightState(UcgLaneHighlightState state)
        {
            _highlightState = state;
            RefreshHighlight();
            RefreshGuideMotion();
        }

        public void PlaySuccessfulActionFeedback(bool isUpgrade)
        {
            if (!Application.isPlaying || highlightImage == null || !gameObject.activeInHierarchy) return;

            if (_successFeedbackRoutine != null)
            {
                StopCoroutine(_successFeedbackRoutine);
                _successFeedbackRoutine = null;
            }

            _successFeedbackRoutine = StartCoroutine(SuccessfulActionFeedbackRoutine(isUpgrade));
        }

        IEnumerator SuccessfulActionFeedbackRoutine(bool isUpgrade)
        {
            if (highlightImage == null) yield break;

            Color baseColor = highlightImage.color;
            Color flashColor = isUpgrade
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.12f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.11f);
            float duration = isUpgrade ? 0.22f : 0.18f;
            float elapsed = 0f;

            while (elapsed < duration && highlightImage != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                highlightImage.color = Color.Lerp(baseColor, flashColor, pulse);
                yield return null;
            }

            _successFeedbackRoutine = null;
            RefreshHighlight();
        }

        void LockPlacedCards()
        {
            if (cardSlot == null) return;

            for (int i = 0; i < cardSlot.childCount; i++)
            {
                var card = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null)
                {
                    card.SetBattlefieldLocked(true);
                }
            }
        }

        void ConfigureFieldCardClickTarget(UcgCardView cardView)
        {
            if (cardView == null || ownerLane == null) return;

            var clickTarget = cardView.GetComponent<UcgLaneClickTarget>();
            if (clickTarget == null) clickTarget = cardView.gameObject.AddComponent<UcgLaneClickTarget>();

            clickTarget.demo = FindFirstObjectByType<UcgHandDemo>();
            clickTarget.ownerLane = ownerLane;
            clickTarget.targetSide = UcgPlayerSide.Player;
        }

        Text CreateEffectPlaceholderText(RectTransform parent, Font placeholderFont)
        {
            if (parent == null) return null;

            var labelObject = new GameObject("Effect Placeholder", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 10f);
            labelRect.offsetMax = new Vector2(-10f, -10f);
            labelRect.localScale = Vector3.one;
            labelRect.localEulerAngles = Vector3.zero;

            Text label = labelObject.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 1f, 1f, 0.82f);
            label.fontSize = 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
            if (placeholderFont != null) label.font = placeholderFont;
            return label;
        }

        void ApplyPlacedCardSorting()
        {
            if (cardSlot == null) return;

            const int battlefieldCardSortingBase = 3000;
            for (int i = 0; i < cardSlot.childCount; i++)
            {
                var card = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card == null) continue;

                var cardCanvas = card.GetComponent<Canvas>();
                if (cardCanvas == null) cardCanvas = card.gameObject.AddComponent<Canvas>();

                if (card.GetComponent<GraphicRaycaster>() == null)
                {
                    card.gameObject.AddComponent<GraphicRaycaster>();
                }

                cardCanvas.overrideSorting = true;
                cardCanvas.sortingOrder = battlefieldCardSortingBase + i;
                card.selectedSortingOrder = battlefieldCardSortingBase + i;
            }
        }

        bool IsOccupied()
        {
            RefreshTopCard();
            return topCard != null;
        }

        void RefreshTopCard()
        {
            if (topCard != null && topCard.transform.parent == cardSlot) return;
            currentCard = null;
            topCard = null;

            if (cardSlot == null) return;

            for (int i = 0; i < cardSlot.childCount; i++)
            {
                var card = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null)
                {
                    if (currentCard == null) currentCard = card;
                    topCard = card;
                }
            }
        }

        int CountPlacedCards()
        {
            if (cardSlot == null) return 0;

            int count = 0;
            for (int i = 0; i < cardSlot.childCount; i++)
            {
                if (cardSlot.GetChild(i).GetComponent<UcgCardView>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        Vector2 GetPlacedCardPosition(UcgPlayActionType actionType, int existingCardCount)
        {
            if (actionType == UcgPlayActionType.PlayToEmptyArea) return Vector2.zero;
            int upgradeIndex = Mathf.Max(1, existingCardCount);
            return upgradeStackOffset * upgradeIndex;
        }

        void RefreshHighlight()
        {
            if (highlightImage == null) return;
            switch (_highlightState)
            {
                case UcgLaneHighlightState.ActiveSetupTarget:
                    highlightImage.color = activeSetupColor;
                    break;
                case UcgLaneHighlightState.UpgradeAvailable:
                    highlightImage.color = upgradeAvailableColor;
                    break;
                case UcgLaneHighlightState.ValidDropTarget:
                    highlightImage.color = validDropColor;
                    break;
                case UcgLaneHighlightState.InvalidDropTarget:
                    highlightImage.color = invalidDropColor;
                    break;
                case UcgLaneHighlightState.Locked:
                    highlightImage.color = lockedColor;
                    break;
                default:
                    highlightImage.color = IsOccupied() ? occupiedColor : defaultColor;
                    break;
            }

            Color slotFill = highlightImage.color;
            if (_highlightState == UcgLaneHighlightState.ActiveSetupTarget)
            {
                slotFill = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, Mathf.Min(slotFill.a, 0.052f));
            }
            else if (_highlightState == UcgLaneHighlightState.ValidDropTarget)
            {
                slotFill = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, Mathf.Min(slotFill.a, 0.050f));
            }
            else if (_highlightState == UcgLaneHighlightState.UpgradeAvailable)
            {
                slotFill.a = Mathf.Min(slotFill.a, 0.075f);
            }
            else if (_highlightState == UcgLaneHighlightState.InvalidDropTarget)
            {
                slotFill.a = Mathf.Min(slotFill.a, 0.052f);
            }
            else
            {
                slotFill.a = Mathf.Min(slotFill.a, IsOccupied() ? 0.040f : 0.070f);
            }
            highlightImage.color = slotFill;

            RefreshSlotFrameStyle();
            RefreshGuideMotion();
        }

        void RefreshSlotFrameStyle()
        {
            if (highlightImage == null) return;

            Color borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, IsOccupied() ? 0.28f : 0.24f);
            float borderDistance = IsOccupied() ? 1.2f : 1.05f;
            Color shadowColor = new Color(4f / 255f, 9f / 255f, 18f / 255f, IsOccupied() ? 0.24f : 0.17f);
            Vector2 shadowDistance = new Vector2(0f, IsOccupied() ? -4f : -3f);

            if (_highlightState == UcgLaneHighlightState.ActiveSetupTarget)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.66f);
                borderDistance = 1.6f;
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.10f);
                shadowDistance = new Vector2(0f, -4f);
            }
            else if (_highlightState == UcgLaneHighlightState.ValidDropTarget)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.54f);
                borderDistance = 1.7f;
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.12f);
                shadowDistance = new Vector2(0f, -4f);
            }
            else if (_highlightState == UcgLaneHighlightState.UpgradeAvailable)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.38f);
                borderDistance = 1.4f;
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.08f);
                shadowDistance = new Vector2(0f, -3.5f);
            }
            else if (_highlightState == UcgLaneHighlightState.InvalidDropTarget)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.28f);
                borderDistance = 1.1f;
                shadowColor = new Color(0.46f, 0.08f, 0.12f, 0.06f);
                shadowDistance = new Vector2(0f, -2.5f);
            }
            else if (_highlightState == UcgLaneHighlightState.Locked)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.09f);
                borderDistance = 0.8f;
                shadowColor = new Color(4f / 255f, 9f / 255f, 18f / 255f, 0.08f);
                shadowDistance = new Vector2(0f, -1.5f);
            }
            else if (_isPointerInside)
            {
                borderColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.46f);
                borderDistance = 1.5f;
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.075f);
                shadowDistance = new Vector2(0f, -3.5f);
            }

            Outline outline = highlightImage.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(borderDistance, -borderDistance);
                outline.useGraphicAlpha = true;
            }

            Shadow shadow = GetExactShadow();
            if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;
            shadow.useGraphicAlpha = true;

            RefreshCardGroundShadow(IsOccupied(), _highlightState, _isPointerInside);
        }

        void RefreshCardGroundShadow(bool occupied, UcgLaneHighlightState state, bool pointerInside)
        {
            RectTransform slotRect = cardSlot != null ? cardSlot : transform as RectTransform;
            if (slotRect == null) return;

            const string shadowName = "Slot Card Ground Shadow";
            Transform existingShadow = slotRect.Find(shadowName);
            RectTransform shadowRect;
            Image shadowImage;

            if (existingShadow == null)
            {
                var shadowObject = new GameObject(shadowName, typeof(RectTransform), typeof(Image));
                shadowObject.transform.SetParent(slotRect, false);
                shadowRect = shadowObject.GetComponent<RectTransform>();
                shadowImage = shadowObject.GetComponent<Image>();
            }
            else
            {
                shadowRect = existingShadow as RectTransform;
                shadowImage = existingShadow.GetComponent<Image>();
                if (shadowImage == null) shadowImage = existingShadow.gameObject.AddComponent<Image>();
            }

            ApplySlicedUiSprite(shadowImage);
            shadowImage.raycastTarget = false;
            shadowImage.enabled = true;

            float alpha = occupied ? 0.34f : 0.085f;
            Color shadowColor = new Color(2f / 255f, 6f / 255f, 14f / 255f, alpha);
            if (state == UcgLaneHighlightState.ActiveSetupTarget)
            {
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, occupied ? 0.20f : 0.075f);
            }
            else if (state == UcgLaneHighlightState.ValidDropTarget || pointerInside)
            {
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, occupied ? 0.20f : 0.095f);
            }
            else if (state == UcgLaneHighlightState.UpgradeAvailable)
            {
                shadowColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, occupied ? 0.16f : 0.07f);
            }

            shadowImage.color = shadowColor;
            shadowRect.anchorMin = new Vector2(occupied ? 0.10f : 0.18f, occupied ? 0.17f : 0.12f);
            shadowRect.anchorMax = new Vector2(occupied ? 0.90f : 0.82f, occupied ? 0.17f : 0.12f);
            shadowRect.offsetMin = new Vector2(0f, occupied ? -18f : -9f);
            shadowRect.offsetMax = new Vector2(0f, occupied ? 10f : 6f);
            shadowRect.localScale = Vector3.one;
            shadowRect.localEulerAngles = Vector3.zero;
            shadowRect.SetSiblingIndex(Mathf.Min(3, slotRect.childCount - 1));
        }

        Shadow GetExactShadow()
        {
            Shadow[] shadows = GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] != null && shadows[i].GetType() == typeof(Shadow))
                {
                    return shadows[i];
                }
            }

            return null;
        }

        static void ApplySlicedUiSprite(Image image)
        {
            if (image == null) return;

            Sprite roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (roundedSprite == null) return;

            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        void RefreshGuideMotion()
        {
            bool showDropGuide = (_highlightState == UcgLaneHighlightState.ActiveSetupTarget
                || _highlightState == UcgLaneHighlightState.ValidDropTarget)
                && !IsOccupied();
            bool pulseSlot = _highlightState == UcgLaneHighlightState.ActiveSetupTarget
                || _highlightState == UcgLaneHighlightState.ValidDropTarget
                || _highlightState == UcgLaneHighlightState.UpgradeAvailable;

            if (_slotPulse == null && highlightImage != null)
            {
                _slotPulse = gameObject.AddComponent<UcgGuidancePulse>();
                _slotPulse.targetImage = highlightImage;
                _slotPulse.targetRect = transform as RectTransform;
                _slotPulse.pulseScale = false;
                _slotPulse.pulseAlpha = true;
                _slotPulse.speed = 2.2f;
            }

            if (_slotPulse != null)
            {
                _slotPulse.alphaAmplitude = 0.018f;
                _slotPulse.CaptureBaseState();
                _slotPulse.enabled = pulseSlot;
            }

            EnsureGuideRing();
            ApplyGuideRingStyle();
            if (_guideRingRect != null)
            {
                _guideRingRect.gameObject.SetActive(showDropGuide || _highlightState == UcgLaneHighlightState.UpgradeAvailable);
                if (_guideRingRect.gameObject.activeSelf) _guideRingRect.SetAsLastSibling();
            }

            EnsureGuideArrow();
            ApplyGuideRingStyle();
            if (_guideArrowRect != null)
            {
                _guideArrowRect.gameObject.SetActive(showDropGuide);
                if (_guideArrowRect.gameObject.activeSelf) _guideArrowRect.SetAsLastSibling();
            }
        }

        void ApplyGuideRingStyle()
        {
            Color accent = UcgToolUiPalette.FocusCyan;
            if (_highlightState == UcgLaneHighlightState.ActiveSetupTarget)
            {
                accent = UcgToolUiPalette.BrandPinkLight;
            }
            else if (_highlightState == UcgLaneHighlightState.UpgradeAvailable)
            {
                accent = UcgToolUiPalette.WarningGold;
            }

            if (_guideRingImage != null)
            {
                _guideRingImage.color = UcgToolUiPalette.WithAlpha(accent, 0.028f);
            }
            if (_guideRingOutline != null)
            {
                _guideRingOutline.effectColor = UcgToolUiPalette.WithAlpha(accent, 0.32f);
            }
            if (_guideArrowText != null)
            {
                _guideArrowText.color = UcgToolUiPalette.WithAlpha(accent, 0.78f);
            }
        }

        void EnsureGuideRing()
        {
            if (_guideRingRect != null && _guideRingImage != null && _guideRingOutline != null) return;

            Transform existingRing = transform.Find("Drop Guide Ring");
            if (existingRing == null)
            {
                var ringObject = new GameObject("Drop Guide Ring", typeof(RectTransform), typeof(Image), typeof(Outline));
                ringObject.transform.SetParent(transform, false);
                existingRing = ringObject.transform;
            }

            _guideRingRect = existingRing as RectTransform;
            _guideRingImage = existingRing.GetComponent<Image>();
            if (_guideRingImage == null) _guideRingImage = existingRing.gameObject.AddComponent<Image>();
            _guideRingOutline = existingRing.GetComponent<Outline>();
            if (_guideRingOutline == null) _guideRingOutline = existingRing.gameObject.AddComponent<Outline>();

            RectTransform slotRect = transform as RectTransform;
            Vector2 slotSize = slotRect != null ? slotRect.rect.size : new Vector2(220f, 320f);
            if ((slotSize.x <= 0f || slotSize.y <= 0f) && slotRect != null) slotSize = slotRect.sizeDelta;

            _guideRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            _guideRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            _guideRingRect.pivot = new Vector2(0.5f, 0.5f);
            _guideRingRect.anchoredPosition = Vector2.zero;
            _guideRingRect.sizeDelta = slotSize + new Vector2(4f, 4f);
            _guideRingRect.localScale = Vector3.one;
            _guideRingRect.localEulerAngles = Vector3.zero;
            _guideRingRect.SetAsLastSibling();

            _guideRingImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.026f);
            _guideRingImage.raycastTarget = false;
            _guideRingOutline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.32f);
            _guideRingOutline.effectDistance = new Vector2(1.5f, -1.5f);
            _guideRingOutline.useGraphicAlpha = true;

            _guideRingPulse = _guideRingRect.GetComponent<UcgGuidancePulse>();
            if (_guideRingPulse == null) _guideRingPulse = _guideRingRect.gameObject.AddComponent<UcgGuidancePulse>();
            _guideRingPulse.targetImage = _guideRingImage;
            _guideRingPulse.targetRect = _guideRingRect;
            _guideRingPulse.pulseScale = true;
            _guideRingPulse.scaleAmplitude = 0.01f;
            _guideRingPulse.pulseAlpha = true;
            _guideRingPulse.alphaAmplitude = 0.035f;
            _guideRingPulse.speed = 1.9f;

            _guideRingRect.gameObject.SetActive(false);
        }

        void EnsureGuideArrow()
        {
            if (_guideArrowRect != null && _guideArrowText != null) return;

            Transform existingArrow = transform.Find("Drop Guide Arrow");
            if (existingArrow == null)
            {
                var arrowObject = new GameObject("Drop Guide Arrow", typeof(RectTransform), typeof(Text));
                arrowObject.transform.SetParent(transform, false);
                existingArrow = arrowObject.transform;
            }

            _guideArrowRect = existingArrow as RectTransform;
            _guideArrowText = existingArrow.GetComponent<Text>();
            if (_guideArrowText == null) _guideArrowText = existingArrow.gameObject.AddComponent<Text>();

            RectTransform slotRect = transform as RectTransform;
            float slotHeight = slotRect != null ? slotRect.rect.height : 260f;
            if (slotHeight <= 0f && slotRect != null) slotHeight = slotRect.sizeDelta.y;

            _guideArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _guideArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _guideArrowRect.pivot = new Vector2(0.5f, 0.5f);
            _guideArrowRect.anchoredPosition = new Vector2(0f, slotHeight * 0.5f + 12f);
            _guideArrowRect.sizeDelta = new Vector2(38f, 38f);
            _guideArrowRect.localScale = Vector3.one;
            _guideArrowRect.localEulerAngles = Vector3.zero;
            _guideArrowRect.SetAsLastSibling();

            _guideArrowText.text = "▼";
            _guideArrowText.alignment = TextAnchor.MiddleCenter;
            _guideArrowText.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.72f);
            _guideArrowText.fontSize = 28;
            _guideArrowText.resizeTextForBestFit = true;
            _guideArrowText.resizeTextMinSize = 22;
            _guideArrowText.resizeTextMaxSize = 28;
            _guideArrowText.raycastTarget = false;

            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null) _guideArrowText.font = font;
            }
            catch
            {
                // Keep Unity's default text font if the runtime font is unavailable.
            }

            if (_guideArrowPulse == null)
            {
                _guideArrowPulse = _guideArrowRect.gameObject.GetComponent<UcgGuidancePulse>();
                if (_guideArrowPulse == null) _guideArrowPulse = _guideArrowRect.gameObject.AddComponent<UcgGuidancePulse>();
                _guideArrowPulse.targetText = _guideArrowText;
                _guideArrowPulse.targetRect = _guideArrowRect;
                _guideArrowPulse.pulseScale = true;
                _guideArrowPulse.scaleAmplitude = 0.016f;
                _guideArrowPulse.alphaAmplitude = 0.07f;
                _guideArrowPulse.bobAmplitude = 4.5f;
                _guideArrowPulse.speed = 2.4f;
            }

            _guideArrowRect.gameObject.SetActive(false);
        }
    }
}
