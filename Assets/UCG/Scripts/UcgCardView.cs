using System;
using System.Collections;
using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class UcgCardView : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
    {
        public event Action<UcgCardView> OnCardSelected;

        [Header("Data")]
        public UcgCardData cardData;

        [Header("Display")]
        public Image cardImage;
        public Text placeholderText;
        public Color placeholderColor = new Color(0.18f, 0.22f, 0.3f, 1f);
        public Color faceDownColor = new Color(0.68f, 0.02f, 0.1f, 1f);
        public Color imageCardColor = Color.white;
        public UcgCardInfoPanel infoPanel;
        public bool debugCardViewBinding;

        [Header("Selection")]
        public float selectedSizeMultiplier = 1.35f;
        public int selectedSortingOrder = 1200;
        public bool isLockedInBattlefield;
        public bool isFaceDown;

        RectTransform _rectTransform;
        Canvas _selectionCanvas;
        Vector2 _baseSize;
        bool _isSelected;
        bool _isDragging;
        bool _previousOverrideSorting;
        int _previousSortingOrder;
        Outline _playableHighlight;
        UcgGuidancePulse _playableHighlightPulse;
        Coroutine _operationFeedbackRoutine;
        RectTransform _operationFeedbackRect;
        Image _operationFeedbackImage;
        Outline _operationFeedbackOutline;
        string _requestedImageLocal;
        string _boundCardId;
        string _boundImageLocal;
        Image _cardArtImage;
        RectTransform _cardArtRect;

        public bool IsSelected => _isSelected;
        public UcgCardData CardData => cardData;
        public bool IsLockedInBattlefield => isLockedInBattlefield;
        public bool IsFaceDown => isFaceDown;
        public bool IsDragging => _isDragging;
        public bool EnterEffectResolved { get; set; }

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (cardImage == null) cardImage = GetComponent<Image>();
            _baseSize = _rectTransform.sizeDelta;
        }

        void Start()
        {
            Refresh();
        }

        void LateUpdate()
        {
            if (!_isSelected || _selectionCanvas == null) return;

            _selectionCanvas.overrideSorting = true;
            _selectionCanvas.sortingOrder = selectedSortingOrder;
        }

        public void Initialize(UcgCardData data)
        {
            cardData = data;
            EnterEffectResolved = false;
            _requestedImageLocal = null;
            _boundCardId = data != null ? data.id : "";
            _boundImageLocal = data != null ? data.imageLocal : "";
            Refresh();
        }

        public void SetInfoPanel(UcgCardInfoPanel panel)
        {
            infoPanel = panel;
        }

        public void SetBattlefieldLocked(bool locked)
        {
            isLockedInBattlefield = locked;

            var dragCard = GetComponent<UIDragCard>();
            if (dragCard != null)
            {
                dragCard.enabled = !locked;
            }

            var hover = GetComponent<UIHandCardHover>();
            if (hover != null)
            {
                hover.enabled = !locked;
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (cardImage == null) cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.raycastTarget = true;
            }
        }

        public void SetFaceDown(bool faceDown)
        {
            isFaceDown = faceDown;
            Refresh();
        }

        public void FlipFaceUp()
        {
            SetFaceDown(false);
        }

        public void SetDragging(bool dragging)
        {
            _isDragging = dragging;
            if (!dragging && !_isSelected && !isLockedInBattlefield)
            {
                ResetVisualState();
                NotifyHandLayoutRefresh();
            }
        }

        public void SetBaseSize(Vector2 baseSize)
        {
            _baseSize = baseSize;
            if (!_isSelected)
            {
                _rectTransform.sizeDelta = _baseSize;
            }
        }

        public void ResetVisualState()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (!_isSelected)
            {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.sizeDelta = _baseSize;
            }
        }

        public void SetPlayableHighlight(bool active)
        {
            if (!active && _playableHighlight == null) return;

            if (_playableHighlight == null)
            {
                _playableHighlight = GetComponent<Outline>();
                if (_playableHighlight == null) _playableHighlight = gameObject.AddComponent<Outline>();
            }

            _playableHighlight.effectColor = new Color(0.22f, 0.9f, 0.48f, 0.95f);
            _playableHighlight.effectDistance = new Vector2(5f, -5f);
            _playableHighlight.enabled = active;
            if (_playableHighlightPulse == null)
            {
                _playableHighlightPulse = GetComponent<UcgGuidancePulse>();
                if (_playableHighlightPulse == null) _playableHighlightPulse = gameObject.AddComponent<UcgGuidancePulse>();
                _playableHighlightPulse.targetOutline = _playableHighlight;
                _playableHighlightPulse.targetRect = transform as RectTransform;
                _playableHighlightPulse.pulseScale = true;
                _playableHighlightPulse.pulseAlpha = true;
                _playableHighlightPulse.scaleAmplitude = 0.028f;
                _playableHighlightPulse.alphaAmplitude = 0.18f;
                _playableHighlightPulse.bobAmplitude = isLockedInBattlefield ? 0f : 8f;
                _playableHighlightPulse.speed = 2.8f;
            }

            _playableHighlightPulse.bobAmplitude = isLockedInBattlefield ? 0f : 8f;
            _playableHighlightPulse.CaptureBaseState();
            _playableHighlightPulse.enabled = active;
        }

        public void PlayTapFeedback()
        {
            StartOperationFeedback(
                0.12f,
                1.025f,
                new Color(0.72f, 0.96f, 1f, 0.18f),
                new Color(0.5f, 0.95f, 1f, 0.34f),
                true);
        }

        public void PlayBoardActionFeedback(bool isUpgrade)
        {
            StartOperationFeedback(
                isUpgrade ? 0.2f : 0.16f,
                isUpgrade ? 1.07f : 1.045f,
                isUpgrade ? new Color(1f, 0.82f, 0.32f, 0.2f) : new Color(0.42f, 0.95f, 1f, 0.18f),
                isUpgrade ? new Color(1f, 0.78f, 0.22f, 0.42f) : new Color(0.46f, 0.96f, 1f, 0.34f),
                true);
        }

        public void PlayJudgementFeedback(bool isWinner)
        {
            StartOperationFeedback(
                0.22f,
                1f,
                isWinner ? new Color(0.25f, 1f, 0.48f, 0.16f) : new Color(1f, 0.35f, 0.28f, 0.13f),
                isWinner ? new Color(0.32f, 1f, 0.58f, 0.36f) : new Color(1f, 0.38f, 0.32f, 0.28f),
                false);
        }

        void StartOperationFeedback(float duration, float peakScale, Color fillColor, Color outlineColor, bool pulseScale)
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;

            if (_operationFeedbackRoutine != null)
            {
                StopCoroutine(_operationFeedbackRoutine);
                _operationFeedbackRoutine = null;
            }

            _operationFeedbackRoutine = StartCoroutine(OperationFeedbackRoutine(
                Mathf.Max(0.05f, duration),
                Mathf.Max(1f, peakScale),
                fillColor,
                outlineColor,
                pulseScale));
        }

        IEnumerator OperationFeedbackRoutine(float duration, float peakScale, Color fillColor, Color outlineColor, bool pulseScale)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null) yield break;

            EnsureOperationFeedbackOverlay();

            Vector3 baseScale = rect.localScale;
            if (_operationFeedbackRect != null)
            {
                _operationFeedbackRect.SetAsLastSibling();
                _operationFeedbackRect.gameObject.SetActive(true);
            }

            float elapsed = 0f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);

                if (pulseScale)
                {
                    rect.localScale = baseScale * Mathf.Lerp(1f, peakScale, pulse);
                }

                if (_operationFeedbackImage != null)
                {
                    Color color = fillColor;
                    color.a = fillColor.a * pulse;
                    _operationFeedbackImage.color = color;
                }

                if (_operationFeedbackOutline != null)
                {
                    Color color = outlineColor;
                    color.a = outlineColor.a * pulse;
                    _operationFeedbackOutline.effectColor = color;
                    _operationFeedbackOutline.enabled = color.a > 0.001f;
                }

                yield return null;
            }

            if (rect != null && pulseScale)
            {
                rect.localScale = baseScale;
            }

            if (_operationFeedbackImage != null)
            {
                Color color = _operationFeedbackImage.color;
                color.a = 0f;
                _operationFeedbackImage.color = color;
            }

            if (_operationFeedbackOutline != null)
            {
                _operationFeedbackOutline.enabled = false;
            }

            _operationFeedbackRoutine = null;
        }

        void EnsureOperationFeedbackOverlay()
        {
            const string overlayName = "Operation Feedback Overlay";
            if (_operationFeedbackRect != null && _operationFeedbackImage != null && _operationFeedbackOutline != null) return;

            Transform existingOverlay = transform.Find(overlayName);
            if (existingOverlay == null)
            {
                var overlayObject = new GameObject(overlayName, typeof(RectTransform), typeof(Image), typeof(Outline));
                overlayObject.transform.SetParent(transform, false);
                existingOverlay = overlayObject.transform;
            }

            _operationFeedbackRect = existingOverlay as RectTransform;
            _operationFeedbackImage = existingOverlay.GetComponent<Image>();
            if (_operationFeedbackImage == null) _operationFeedbackImage = existingOverlay.gameObject.AddComponent<Image>();
            _operationFeedbackOutline = existingOverlay.GetComponent<Outline>();
            if (_operationFeedbackOutline == null) _operationFeedbackOutline = existingOverlay.gameObject.AddComponent<Outline>();

            _operationFeedbackRect.anchorMin = Vector2.zero;
            _operationFeedbackRect.anchorMax = Vector2.one;
            _operationFeedbackRect.pivot = new Vector2(0.5f, 0.5f);
            _operationFeedbackRect.offsetMin = Vector2.zero;
            _operationFeedbackRect.offsetMax = Vector2.zero;
            _operationFeedbackRect.localScale = Vector3.one;
            _operationFeedbackRect.localEulerAngles = Vector3.zero;
            _operationFeedbackRect.SetAsLastSibling();

            if (_operationFeedbackImage.sprite == null)
            {
                try
                {
                    _operationFeedbackImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                }
                catch
                {
                    // Keep the simple image fallback if the built-in UI sprite is unavailable.
                }
            }

            _operationFeedbackImage.color = Color.clear;
            _operationFeedbackImage.raycastTarget = false;
            _operationFeedbackOutline.enabled = false;
            _operationFeedbackOutline.effectDistance = new Vector2(4f, -4f);
            _operationFeedbackOutline.useGraphicAlpha = true;
        }

        public void Refresh()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (cardImage == null) cardImage = GetComponent<Image>();
            if (_baseSize == Vector2.zero) _baseSize = _rectTransform.sizeDelta;
            EnsureCardArtImage();
            _boundCardId = cardData != null ? cardData.id : "";
            _boundImageLocal = cardData != null ? cardData.imageLocal : "";

            bool shouldUseExternalImage = !isFaceDown && cardData != null && cardData.IsExternalCard();
            bool externalImageRequested = shouldUseExternalImage && _requestedImageLocal == cardData.imageLocal;
            Sprite sprite = !isFaceDown && cardData != null && (!shouldUseExternalImage || externalImageRequested)
                ? cardData.cardImage
                : null;
            string displayName = cardData != null && !string.IsNullOrWhiteSpace(cardData.cardName)
                ? cardData.cardName
                : "UCG Card";

            cardImage.sprite = null;
            cardImage.color = isFaceDown ? faceDownColor : placeholderColor;
            cardImage.preserveAspect = false;

            bool isSceneCard = !isFaceDown && cardData != null && cardData.IsSceneCard();
            if (_cardArtImage != null)
            {
                UcgCardImageApplier.ApplySprite(_cardArtImage, sprite);
                _cardArtImage.color = imageCardColor;
                if (sprite != null)
                {
                    string visibilityReason;
                    UcgCardImageApplier.ValidateVisibility(cardData, gameObject, _cardArtImage, placeholderText, isFaceDown, out visibilityReason);
                }
            }

            ApplyCardImageLayout(isSceneCard);

            if (placeholderText != null)
            {
                placeholderText.text = isFaceDown ? "背面" : displayName;
                placeholderText.enabled = isFaceDown || sprite == null;
            }

            TryLoadExternalImage(sprite);
        }

        void AssignLoadedCardSprite(UcgCardData loadedCard, Sprite sprite, string requestImageLocal)
        {
            if (loadedCard == null || sprite == null) return;

            string loadedId = loadedCard.id;
            if (loadedId != _boundCardId || requestImageLocal != _boundImageLocal)
            {
                Debug.LogWarning($"Skip stale card image assignment. loaded={loadedId}, bound={_boundCardId}, loadedImageLocal={requestImageLocal}, boundImageLocal={_boundImageLocal}");
                return;
            }

            if (cardData != null)
            {
                cardData.cardImage = sprite;
            }

            if (cardImage == null) cardImage = GetComponent<Image>();
            EnsureCardArtImage();

            bool isSceneCard = !isFaceDown && cardData != null && cardData.IsSceneCard();
            ApplyCardImageLayout(isSceneCard);

            if (_cardArtImage != null)
            {
                UcgCardImageApplier.ApplySprite(_cardArtImage, isFaceDown ? null : sprite);
                _cardArtImage.color = imageCardColor;
                _cardArtImage.preserveAspect = false;
                _cardArtImage.raycastTarget = false;
            }

            if (placeholderText != null)
            {
                placeholderText.enabled = isFaceDown;
            }

            if (cardImage != null)
            {
                cardImage.sprite = null;
                cardImage.color = isFaceDown ? faceDownColor : placeholderColor;
                cardImage.preserveAspect = false;
            }

            string visibilityReason;
            bool isVisible = UcgCardImageApplier.ValidateVisibility(loadedCard, gameObject, _cardArtImage, placeholderText, isFaceDown, out visibilityReason);
            Texture2D texture = sprite.texture;
            if (debugCardViewBinding)
            {
                Debug.Log(
                    $"Assign card image success: card={loadedCard.id} {loadedCard.cardName}, " +
                    $"view={gameObject.name}, boundCardId={_boundCardId}, " +
                    $"target={(_cardArtImage != null ? _cardArtImage.name : "null")}, " +
                    $"spriteNull={sprite == null}, texture={texture.width}x{texture.height}, " +
                    $"imageEnabled={(_cardArtImage != null && _cardArtImage.enabled)}, " +
                    $"alpha={(_cardArtImage != null ? _cardArtImage.color.a : 0f)}, " +
                    $"placeholderActive={(placeholderText != null && placeholderText.enabled)}, " +
                    $"faceDown={isFaceDown}, finalVisible={isVisible}, visibilityReason={visibilityReason}");
            }
        }

        void ApplyCardImageLayout(bool isSceneCard)
        {
            if (_cardArtRect == null) return;

            Vector2 cardSize = _rectTransform.rect.size;
            if (cardSize == Vector2.zero) cardSize = _rectTransform.sizeDelta;

            _cardArtRect.anchorMin = new Vector2(0.5f, 0.5f);
            _cardArtRect.anchorMax = new Vector2(0.5f, 0.5f);
            _cardArtRect.pivot = new Vector2(0.5f, 0.5f);
            _cardArtRect.anchoredPosition = Vector2.zero;
            _cardArtRect.localScale = Vector3.one;
            _cardArtRect.localRotation = isSceneCard
                ? Quaternion.Euler(0f, 0f, -90f)
                : Quaternion.identity;

            float fillBleed = isSceneCard ? 1.08f : 1.02f;
            _cardArtRect.sizeDelta = isSceneCard
                ? new Vector2(cardSize.y * fillBleed, cardSize.x * fillBleed)
                : new Vector2(cardSize.x * fillBleed, cardSize.y * fillBleed);
        }

        void EnsureCardArtImage()
        {
            const string artName = "Card Art Image";
            if (_cardArtImage != null && _cardArtRect != null) return;

            Transform existingArt = transform.Find(artName);
            if (existingArt == null)
            {
                var artObject = new GameObject(artName, typeof(RectTransform), typeof(Image));
                artObject.transform.SetParent(transform, false);
                existingArt = artObject.transform;
            }

            _cardArtRect = existingArt as RectTransform;
            _cardArtImage = existingArt.GetComponent<Image>();
            if (_cardArtImage == null) _cardArtImage = existingArt.gameObject.AddComponent<Image>();

            _cardArtRect.anchorMin = new Vector2(0.5f, 0.5f);
            _cardArtRect.anchorMax = new Vector2(0.5f, 0.5f);
            _cardArtRect.pivot = new Vector2(0.5f, 0.5f);
            _cardArtRect.anchoredPosition = Vector2.zero;
            _cardArtRect.localScale = Vector3.one;
            _cardArtImage.raycastTarget = false;
            _cardArtImage.enabled = false;
            _cardArtRect.SetAsLastSibling();
        }

        void TryLoadExternalImage(Sprite currentSprite)
        {
            if (debugCardViewBinding && cardData != null && IsDigaTutorialTargetScene(cardData))
            {
                Debug.Log(
                    "UcgCardView SetCard:\n" +
                    $"card = {cardData.cardName}\n" +
                    $"hasLocalSprite = {cardData.cardImage != null}\n" +
                    $"imageLocal = {cardData.imageLocal}\n" +
                    $"willLoadExternalImage = {!isFaceDown && cardData.IsExternalCard() && currentSprite == null}");
            }

            if (isFaceDown || currentSprite != null || cardData == null) return;
            if (!cardData.IsExternalCard()) return;
            if (_requestedImageLocal == cardData.imageLocal) return;

            string requestImageLocal = cardData.imageLocal;
            _requestedImageLocal = requestImageLocal;
            UcgCardData requestCardData = cardData;

            UcgCardImageLoader.GetOrCreate().LoadCardImage(requestCardData, loadedSprite =>
            {
                if (this == null) return;
                if (loadedSprite == null)
                {
                    Debug.LogWarning($"Card image fallback to placeholder after load pipeline: {requestCardData.id} {requestCardData.cardName}");
                    return;
                }

                AssignLoadedCardSprite(requestCardData, loadedSprite, requestImageLocal);
            });
        }

        static bool IsDigaTutorialTargetScene(UcgCardData data)
        {
            return data != null && data.cardName == UcgDigaTutorialDeckFactory.TargetTutorialSceneName;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetSelected(!_isSelected);
            if (infoPanel != null)
            {
                if (isFaceDown)
                {
                    infoPanel.ShowMessage("面朝下角色\n此卡尚未開放");
                }
                else
                {
                    infoPanel.ShowCard(cardData);
                }
            }
            OnCardSelected?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging || isLockedInBattlefield) return;

            var hover = GetComponent<UIHandCardHover>();
            if (hover != null && hover.enabled)
            {
                hover.OnPointerExit(eventData);
            }

            if (!_isSelected)
            {
                ResetVisualState();
            }

            NotifyHandLayoutRefresh();
        }

        void NotifyHandLayoutRefresh()
        {
            if (transform.parent == null) return;

            var layout = transform.parent.GetComponent<UIHandLayout>();
            if (layout != null)
            {
                layout.NotifyLayoutChanged(true);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;

            if (selected)
            {
                _selectionCanvas = GetComponent<Canvas>();
                if (_selectionCanvas == null) _selectionCanvas = gameObject.AddComponent<Canvas>();

                _previousOverrideSorting = _selectionCanvas.overrideSorting;
                _previousSortingOrder = _selectionCanvas.sortingOrder;
                _selectionCanvas.overrideSorting = true;
                _selectionCanvas.sortingOrder = selectedSortingOrder;

                if (GetComponent<GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<GraphicRaycaster>();
                }

                _rectTransform.sizeDelta = _baseSize * selectedSizeMultiplier;
            }
            else
            {
                _rectTransform.sizeDelta = _baseSize;

                if (_selectionCanvas != null)
                {
                    _selectionCanvas.overrideSorting = _previousOverrideSorting;
                    _selectionCanvas.sortingOrder = _previousSortingOrder;
                }
            }
        }
    }
}
