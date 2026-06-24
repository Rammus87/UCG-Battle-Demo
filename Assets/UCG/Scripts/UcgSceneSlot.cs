using System.Collections;
using System.Collections.Generic;
using System.Text;
using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    public enum UcgSceneSlotVisualState
    {
        EmptyIdle,
        CanPlaceScene,
        ScenePlaced,
        SceneJustPlaced
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class UcgSceneSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        const string DropReceiverName = "Scene Drop Receiver";
        const string HighlightFrameName = "Scene Drop Highlight Frame";
        const string GuideArrowName = "Scene Guide Arrow";
        const string ScenePlacedToastName = "Scene Placed Toast";

        public UcgSceneCardView currentSceneView;
        public UcgPlayerSide sceneOwner = UcgPlayerSide.Player;
        public Text labelText;
        public Image backgroundImage;
        public UcgHandDemo demo;
        public UcgCardInfoPanel infoPanel;
        public Font uiFont;
        public Vector2 sceneCardSize = new Vector2(520f, 204f);
        public bool debugSceneDiagnostics;
        public bool debugSceneSlotVerbose;

        public Color normalColor = new Color(0.045f, 0.075f, 0.1f, 0.46f);
        public Color hoverColor = new Color(0.08f, 0.14f, 0.18f, 0.56f);
        public Color validColor = new Color(0.22f, 0.78f, 1f, 0.52f);
        public Color invalidColor = new Color(1f, 0.78f, 0.32f, 0.42f);

        bool _highlightValid;
        bool _highlightInvalid;
        bool _dropRaycastEnabled;
        bool _sceneJustPlacedActive;
        Image _dropReceiverImage;
        RectTransform _dropReceiverRect;
        Image _highlightFrameImage;
        Outline _highlightFrameOutline;
        UcgGuidancePulse _highlightFramePulse;
        RectTransform _guideArrowRect;
        Text _guideArrowText;
        UcgGuidancePulse _guideArrowPulse;
        RectTransform _scenePlacedToastRect;
        Text _scenePlacedToastText;
        UcgGuidancePulse _scenePlacedToastPulse;
        Coroutine _sceneJustPlacedRoutine;
        Coroutine _orientationAfterRebuildRoutine;
        UcgSceneSlotVisualState _visualState = UcgSceneSlotVisualState.EmptyIdle;

        readonly List<string> _lastCleanupDisabledObjects = new List<string>();
        readonly List<string> _lastActiveBackgroundObjects = new List<string>();
        readonly List<string> _lastActiveRedOrPlaceholderObjects = new List<string>();
        bool _lastCleanupRedBackFound;

        UcgCardData _currentSceneData;

        public bool HasSceneCard => _currentSceneData != null;
        public UcgCardData SceneCardData => _currentSceneData;
        public UcgPlayerSide SceneOwner => sceneOwner;
        public UcgSceneSlotVisualState CurrentVisualState => _visualState;

        public void Initialize(UcgHandDemo ownerDemo, UcgCardInfoPanel panel, Font font)
        {
            demo = ownerDemo;
            infoPanel = panel;
            uiFont = font;
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();

            EnsureDropReceiver();
            EnsureLabel();
            EnsureHighlightFrame();
            EnsureGuideArrow();
            EnsureScenePlacedToast();

            _highlightValid = false;
            _highlightInvalid = false;
            _dropRaycastEnabled = false;
            _sceneJustPlacedActive = false;
            RefreshSceneSlotVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            RefreshSceneSlotVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshSceneSlotVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentSceneView != null)
            {
                demo?.HandleSceneCardClickedForEffect(currentSceneView);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            var cardView = eventData.pointerDrag.GetComponent<UcgCardView>();
            var dragCard = eventData.pointerDrag.GetComponent<UIDragCard>();
            var cardRect = eventData.pointerDrag.transform as RectTransform;
            if (cardView == null || dragCard == null || cardRect == null) return;

            if (!TryDropCard(cardView, dragCard, cardRect, out string message))
            {
                demo?.ShowSceneDropMessage(message);
                SetHighlight(false, true);
                return;
            }

            demo?.ShowSceneDropMessage(message);
        }

        public bool TryDropCard(UcgCardView cardView, UIDragCard dragCard, RectTransform cardRect, out string message)
        {
            message = "";

            if (cardView == null || dragCard == null || cardRect == null)
            {
                message = "不是有效場景卡";
                return false;
            }

            if (demo == null)
            {
                message = "場景區尚未建立";
                SetHighlight(false, true);
                return false;
            }

            if (!demo.TryPlaceSceneCardFromHand(cardView, dragCard, cardRect, out message))
            {
                SetHighlight(false, true);
                return false;
            }

            return true;
        }

        public void SetSceneCardFromHand(UcgCardView cardView, RectTransform cardRect, UcgPlayerSide owner)
        {
            if (cardView == null || cardRect == null) return;

            UcgCardData sceneData = cardView.CardData;
            ClearSceneCard();
            ConsumeDraggedHandCard(cardView, cardRect);

            CreateSceneCardVisual(sceneData, owner, true);
        }

        public void PreviewSceneCard(UcgCardData cardData, UcgPlayerSide owner)
        {
            if (cardData == null) return;
            ClearSceneCard();
            CreateSceneCardVisual(cardData, owner, false);
        }

        public void PlaceSceneCardFromScript(UcgCardData cardData, UcgPlayerSide owner)
        {
            PlaceSceneCardFromScript(cardData, owner, true);
        }

        public void PlaceSceneCardFromScript(UcgCardData cardData, UcgPlayerSide owner, bool playJustPlacedFeedback)
        {
            if (cardData == null) return;

            ClearSceneCard();
            CreateSceneCardVisual(cardData, owner, playJustPlacedFeedback);
        }

        void CreateSceneCardVisual(UcgCardData cardData, UcgPlayerSide owner, bool playJustPlacedFeedback)
        {
            if (cardData == null) return;

            StopSceneJustPlacedFeedback();
            sceneOwner = owner;

            RemoveAllSceneVisualChildren();

            var cardObject = new GameObject("Current Scene Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UcgSceneCardView));
            cardObject.transform.SetParent(transform, false);

            var rect = cardObject.GetComponent<RectTransform>();
            ConfigureSceneCardRect(rect);

            var image = cardObject.GetComponent<Image>();
            image.enabled = false;
            image.color = Color.clear;
            image.raycastTarget = false;

            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            _currentSceneData = cardData;
            currentSceneView = cardObject.GetComponent<UcgSceneCardView>();
            currentSceneView.Initialize(cardData, owner, infoPanel, uiFont);
            currentSceneView.demo = demo;
            currentSceneView.ApplySceneSlotStateVisual(true, playJustPlacedFeedback);
            ForceSceneCardBoardOrientation();
            DisableSceneVisualRaycasts(currentSceneView.transform);
            ApplySceneCardSorting(currentSceneView);
            ScheduleForceSceneCardBoardOrientationAfterRebuild();

            if (playJustPlacedFeedback)
            {
                BeginSceneJustPlacedFeedback();
            }
            else
            {
                _sceneJustPlacedActive = false;
                RefreshSceneSlotVisual();
            }
        }

        public void ClearSceneCard()
        {
            StopSceneJustPlacedFeedback();
            RemoveAllSceneVisualChildren();
            currentSceneView = null;
            _currentSceneData = null;
            _sceneJustPlacedActive = false;
            SetHighlight(false, false);
            RefreshSceneSlotVisual();
        }

        void RemoveAllSceneVisualChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!IsSceneVisualChild(child)) continue;

                DestroyImmediate(child.gameObject);
            }
        }

        bool IsSceneVisualChild(Transform child)
        {
            if (child == null) return false;
            return child.GetComponent<UcgCardView>() != null
                || child.GetComponent<UcgSceneCardView>() != null
                || child.name == "Current Scene Card"
                || child.name == "Shared Scene Card";
        }

        int CountSceneVisualChildren()
        {
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (IsSceneVisualChild(transform.GetChild(i)))
                {
                    count++;
                }
            }

            return count;
        }

        void DebugSceneVisualCounts()
        {
            int cardViewCount = 0;
            int sceneCardViewCount = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!IsSceneVisualChild(child)) continue;
                if (child.GetComponent<UcgCardView>() != null) cardViewCount++;
                if (child.GetComponent<UcgSceneCardView>() != null) sceneCardViewCount++;
            }

            Debug.Log($"SharedSceneSlot child count={CountSceneVisualChildren()}, UcgCardView={cardViewCount}, UcgSceneCardView={sceneCardViewCount}");
        }

        void DisableSceneVisualRaycasts(Transform root)
        {
            if (root == null) return;

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        void DebugRaycastTargets()
        {
            bool backgroundRaycast = backgroundImage != null && backgroundImage.raycastTarget;
            bool receiverRaycast = _dropReceiverImage != null && _dropReceiverImage.raycastTarget;
            bool highlightRaycast = _highlightFrameImage != null && _highlightFrameImage.raycastTarget;
            bool sceneVisualRaycast = false;
            if (currentSceneView != null)
            {
                var graphics = currentSceneView.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    sceneVisualRaycast |= graphics[i].raycastTarget;
                }
            }

            var rect = transform as RectTransform;
            Debug.Log($"SharedSceneSlot raycast: background={backgroundRaycast}, receiver={receiverRaycast}, highlight={highlightRaycast}, sceneVisualAny={sceneVisualRaycast}, anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}, rect={rect.rect}");
        }

        public void SetHighlight(bool valid, bool invalid)
        {
            _highlightValid = valid;
            _highlightInvalid = invalid;
            RefreshSceneSlotVisual();
        }

        public void SetDropRaycastEnabled(bool enabled)
        {
            _dropRaycastEnabled = enabled;
            if (!enabled)
            {
                _highlightValid = false;
                _highlightInvalid = false;
            }

            RefreshSceneSlotVisual();
        }

        public void RefreshSceneSlotVisual()
        {
            ApplySceneSlotVisualState(GetDesiredVisualState());
        }

        public void ApplySceneSlotVisualState(UcgSceneSlotVisualState state)
        {
            _visualState = state;
            EnsureDropReceiver();
            EnsureLabel();
            EnsureHighlightFrame();
            EnsureGuideArrow();
            EnsureScenePlacedToast();

            ConfigureRootSlotSurface();
            ConfigureDropReceiverForState(state);
            ConfigureLabelForState(state);
            ConfigureHighlightForState(state);
            ConfigureGuideArrowForState(state);
            ConfigureScenePlacedToastForState(state);
            ConfigureCurrentSceneCardForState(state);
            CleanupNonEssentialSceneSlotVisuals(state);

            if (IsSceneSlotDebugVerbose())
            {
                DebugSceneSlotVisualState();
                DebugSceneCardFinalOrientation();
                DebugPrintSceneSlotHierarchy();
            }
            else if (debugSceneDiagnostics)
            {
                DebugSceneVisualCounts();
                DebugRaycastTargets();
            }
        }

        UcgSceneSlotVisualState GetDesiredVisualState()
        {
            if (_currentSceneData != null)
            {
                return _sceneJustPlacedActive
                    ? UcgSceneSlotVisualState.SceneJustPlaced
                    : UcgSceneSlotVisualState.ScenePlaced;
            }

            if (_dropRaycastEnabled || _highlightValid || _highlightInvalid)
            {
                return UcgSceneSlotVisualState.CanPlaceScene;
            }

            return UcgSceneSlotVisualState.EmptyIdle;
        }

        void ConfigureRootSlotSurface()
        {
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
                backgroundImage.color = Color.clear;
                backgroundImage.raycastTarget = false;
            }

            var outline = GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        void ConfigureDropReceiverForState(UcgSceneSlotVisualState state)
        {
            bool active = _dropRaycastEnabled
                && (state == UcgSceneSlotVisualState.CanPlaceScene
                    || state == UcgSceneSlotVisualState.ScenePlaced
                    || state == UcgSceneSlotVisualState.SceneJustPlaced);

            if (_dropReceiverRect != null)
            {
                _dropReceiverRect.anchorMin = Vector2.zero;
                _dropReceiverRect.anchorMax = Vector2.one;
                _dropReceiverRect.offsetMin = Vector2.zero;
                _dropReceiverRect.offsetMax = Vector2.zero;
                _dropReceiverRect.localScale = Vector3.one;
                _dropReceiverRect.localEulerAngles = Vector3.zero;
                _dropReceiverRect.SetAsFirstSibling();
                _dropReceiverRect.gameObject.SetActive(active);
            }

            if (_dropReceiverImage != null)
            {
                _dropReceiverImage.enabled = active;
                _dropReceiverImage.color = Color.clear;
                _dropReceiverImage.raycastTarget = active;
            }
        }

        void ConfigureLabelForState(UcgSceneSlotVisualState state)
        {
            if (labelText == null) return;

            bool show = state == UcgSceneSlotVisualState.CanPlaceScene && _currentSceneData == null;
            labelText.gameObject.SetActive(show);
            labelText.enabled = show;
            labelText.raycastTarget = false;
            if (!show) return;

            labelText.text = _highlightInvalid ? "不能設置" : "場景區";
            labelText.color = _highlightInvalid
                ? new Color(1f, 0.86f, 0.46f, 0.48f)
                : new Color(0.72f, 0.96f, 1f, 0.42f);
        }

        void ConfigureHighlightForState(UcgSceneSlotVisualState state)
        {
            bool show = state == UcgSceneSlotVisualState.CanPlaceScene
                && (_dropRaycastEnabled || _highlightValid || _highlightInvalid);

            RectTransform frameRect = _highlightFrameImage != null
                ? _highlightFrameImage.rectTransform
                : null;
            if (frameRect != null)
            {
                frameRect.gameObject.SetActive(show);
                frameRect.anchorMin = new Vector2(0.5f, 0.5f);
                frameRect.anchorMax = new Vector2(0.5f, 0.5f);
                frameRect.pivot = new Vector2(0.5f, 0.5f);
                frameRect.anchoredPosition = Vector2.zero;
                frameRect.localScale = Vector3.one;
                frameRect.localEulerAngles = Vector3.zero;
                frameRect.sizeDelta = sceneCardSize + new Vector2(28f, 18f);
                if (show) frameRect.SetAsFirstSibling();
            }

            Color guideColor = _highlightInvalid
                ? new Color(1f, 0.78f, 0.32f, 1f)
                : new Color(0.35f, 0.9f, 1f, 1f);

            if (_highlightFrameImage != null)
            {
                _highlightFrameImage.enabled = show;
                _highlightFrameImage.color = show
                    ? new Color(guideColor.r, guideColor.g, guideColor.b, _highlightValid ? 0.055f : 0.035f)
                    : Color.clear;
                _highlightFrameImage.raycastTarget = false;
            }

            if (_highlightFrameOutline != null)
            {
                _highlightFrameOutline.enabled = show;
                _highlightFrameOutline.effectColor = show
                    ? new Color(guideColor.r, guideColor.g, guideColor.b, _highlightValid ? 0.42f : 0.24f)
                    : Color.clear;
                _highlightFrameOutline.effectDistance = _highlightValid
                    ? new Vector2(3f, -3f)
                    : new Vector2(2f, -2f);
                _highlightFrameOutline.useGraphicAlpha = true;
            }

            if (_highlightFramePulse != null)
            {
                _highlightFramePulse.enabled = show;
                _highlightFramePulse.alphaAmplitude = _highlightValid ? 0.026f : 0.012f;
                _highlightFramePulse.scaleAmplitude = _highlightValid ? 0.01f : 0.004f;
                if (show) _highlightFramePulse.CaptureBaseState();
            }
        }

        void ConfigureGuideArrowForState(UcgSceneSlotVisualState state)
        {
            bool show = state == UcgSceneSlotVisualState.CanPlaceScene && _highlightValid;
            if (_guideArrowRect != null)
            {
                _guideArrowRect.gameObject.SetActive(show);
                if (show)
                {
                    float slotHeight = GetSlotHeight();
                    _guideArrowRect.anchoredPosition = new Vector2(0f, slotHeight * 0.5f + 14f);
                    _guideArrowRect.SetAsLastSibling();
                }
            }

            if (_guideArrowText != null)
            {
                _guideArrowText.enabled = show;
                _guideArrowText.raycastTarget = false;
                _guideArrowText.color = new Color(0.62f, 0.98f, 1f, 0.58f);
            }

            if (_guideArrowPulse != null)
            {
                _guideArrowPulse.enabled = show;
                if (show) _guideArrowPulse.CaptureBaseState();
            }
        }

        void ConfigureScenePlacedToastForState(UcgSceneSlotVisualState state)
        {
            bool show = state == UcgSceneSlotVisualState.SceneJustPlaced;
            if (_scenePlacedToastRect != null)
            {
                _scenePlacedToastRect.gameObject.SetActive(show);
                if (show)
                {
                    _scenePlacedToastRect.anchoredPosition = new Vector2(0f, GetSlotHeight() * 0.5f + 16f);
                    _scenePlacedToastRect.SetAsLastSibling();
                }
            }

            if (_scenePlacedToastText != null)
            {
                _scenePlacedToastText.enabled = show;
                _scenePlacedToastText.raycastTarget = false;
                _scenePlacedToastText.text = "場景卡已設置";
                _scenePlacedToastText.color = new Color(0.78f, 0.98f, 1f, 0.82f);
            }

            if (_scenePlacedToastPulse != null)
            {
                _scenePlacedToastPulse.enabled = show;
                if (show) _scenePlacedToastPulse.CaptureBaseState();
            }
        }

        void ConfigureCurrentSceneCardForState(UcgSceneSlotVisualState state)
        {
            if (currentSceneView == null) return;

            bool placed = state == UcgSceneSlotVisualState.ScenePlaced
                || state == UcgSceneSlotVisualState.SceneJustPlaced;
            bool boosted = state == UcgSceneSlotVisualState.SceneJustPlaced;

            currentSceneView.gameObject.SetActive(placed);
            if (!placed) return;

            var rect = currentSceneView.transform as RectTransform;
            ConfigureSceneCardRect(rect);
            currentSceneView.ApplySceneSlotStateVisual(true, boosted);
            ForceSceneCardBoardOrientation();
            DisableSceneVisualRaycasts(currentSceneView.transform);
            ApplySceneCardSorting(currentSceneView);
        }

        public void ForceSceneCardBoardOrientation()
        {
            if (currentSceneView == null) return;

            RectTransform rect = currentSceneView.transform as RectTransform;
            ConfigureSceneCardRect(rect);
            currentSceneView.ForceSceneCardBoardOrientation();
        }

        void ScheduleForceSceneCardBoardOrientationAfterRebuild()
        {
            if (!Application.isPlaying) return;
            if (_orientationAfterRebuildRoutine != null)
            {
                StopCoroutine(_orientationAfterRebuildRoutine);
            }

            _orientationAfterRebuildRoutine = StartCoroutine(ForceSceneCardBoardOrientationAfterRebuild());
        }

        IEnumerator ForceSceneCardBoardOrientationAfterRebuild()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ForceSceneCardBoardOrientation();
            _orientationAfterRebuildRoutine = null;
            if (IsSceneSlotDebugVerbose())
            {
                DebugSceneCardFinalOrientation();
            }
        }

        void CleanupNonEssentialSceneSlotVisuals(UcgSceneSlotVisualState state)
        {
            _lastCleanupDisabledObjects.Clear();
            _lastActiveBackgroundObjects.Clear();
            _lastActiveRedOrPlaceholderObjects.Clear();
            _lastCleanupRedBackFound = false;

            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;

                bool redBack = IsPotentialRedBackImage(image);
                if (redBack) _lastCleanupRedBackFound = true;

                if (IsAllowedSceneSlotImage(image, state))
                {
                    image.raycastTarget = image == _dropReceiverImage && _dropReceiverImage.enabled;
                    continue;
                }

                bool wasVisibleOrInteractive = image.enabled
                    || image.raycastTarget
                    || (image.gameObject.activeSelf && image.color.a > 0.01f);

                image.enabled = false;
                image.raycastTarget = false;
                if (image == backgroundImage)
                {
                    image.color = Color.clear;
                }

                if (image.transform != transform
                    && !IsCurrentSceneRoot(image.transform)
                    && image != _dropReceiverImage)
                {
                    image.gameObject.SetActive(false);
                }

                if (wasVisibleOrInteractive)
                {
                    _lastCleanupDisabledObjects.Add(GetHierarchyPath(image.transform));
                }
            }

            CleanupNonEssentialSceneSlotTexts(state);
            CollectActiveSceneSlotIssues();
            if (IsSceneSlotDebugVerbose())
            {
                Debug.Log(
                    "SceneSlot visual cleanup:\n" +
                    $"redBackObjectFound={_lastCleanupRedBackFound}\n" +
                    $"disabledObjects=[{string.Join(", ", _lastCleanupDisabledObjects.ToArray())}]\n" +
                    $"activeBackgroundObjects=[{string.Join(", ", _lastActiveBackgroundObjects.ToArray())}]\n" +
                    $"activeRedOrPlaceholderObjects=[{string.Join(", ", _lastActiveRedOrPlaceholderObjects.ToArray())}]");
            }
        }

        void CleanupNonEssentialSceneSlotTexts(UcgSceneSlotVisualState state)
        {
            var texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null) continue;
                if (IsAllowedSceneSlotText(text, state))
                {
                    text.raycastTarget = false;
                    continue;
                }

                text.enabled = false;
                text.raycastTarget = false;
                if (text != labelText
                    && text != _guideArrowText
                    && text != _scenePlacedToastText
                    && !IsCurrentSceneChild(text.transform))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        bool IsAllowedSceneSlotImage(Image image, UcgSceneSlotVisualState state)
        {
            if (image == null) return false;

            if (image == _dropReceiverImage)
            {
                return _dropReceiverImage.gameObject.activeSelf
                    && _dropReceiverImage.enabled
                    && _dropReceiverImage.raycastTarget;
            }

            if (state == UcgSceneSlotVisualState.CanPlaceScene && image == _highlightFrameImage)
            {
                return _highlightFrameImage.gameObject.activeSelf && _highlightFrameImage.enabled;
            }

            bool placed = state == UcgSceneSlotVisualState.ScenePlaced
                || state == UcgSceneSlotVisualState.SceneJustPlaced;
            if (!placed || currentSceneView == null) return false;

            return image == currentSceneView.CardArtImage
                || image == currentSceneView.GlowImage;
        }

        bool IsAllowedSceneSlotText(Text text, UcgSceneSlotVisualState state)
        {
            if (text == null) return false;
            if (state == UcgSceneSlotVisualState.CanPlaceScene && text == labelText && labelText.enabled) return true;
            if (state == UcgSceneSlotVisualState.CanPlaceScene && text == _guideArrowText && _guideArrowText.enabled) return true;
            if (state == UcgSceneSlotVisualState.SceneJustPlaced && text == _scenePlacedToastText && _scenePlacedToastText.enabled) return true;

            bool placed = state == UcgSceneSlotVisualState.ScenePlaced
                || state == UcgSceneSlotVisualState.SceneJustPlaced;
            return placed
                && currentSceneView != null
                && text == currentSceneView.FallbackText
                && currentSceneView.FallbackText != null
                && currentSceneView.FallbackText.enabled;
        }

        void CollectActiveSceneSlotIssues()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || !image.enabled) continue;

                if (IsSemiTransparentBackgroundImage(image))
                {
                    _lastActiveBackgroundObjects.Add(GetHierarchyPath(image.transform));
                }

                if (IsPotentialRedBackImage(image) || IsPlaceholderOrBackName(image.name))
                {
                    _lastActiveRedOrPlaceholderObjects.Add(GetHierarchyPath(image.transform));
                }
            }

            var texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy || !text.enabled) continue;
                if (IsPlaceholderOrBackName(text.name) || IsPlaceholderOrBackName(text.text))
                {
                    _lastActiveRedOrPlaceholderObjects.Add(GetHierarchyPath(text.transform));
                }
            }
        }

        void ConfigureSceneCardRect(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            rect.sizeDelta = sceneCardSize;
        }

        void ApplySceneCardSorting(UcgSceneCardView view)
        {
            if (view == null) return;

            var cardCanvas = view.GetComponent<Canvas>();
            if (cardCanvas == null) cardCanvas = view.gameObject.AddComponent<Canvas>();

            if (view.GetComponent<GraphicRaycaster>() == null)
            {
                view.gameObject.AddComponent<GraphicRaycaster>();
            }

            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = 3450;
            view.transform.SetAsLastSibling();
        }

        void EnsureDropReceiver()
        {
            Transform existing = transform.Find(DropReceiverName);
            if (existing == null)
            {
                var receiverObject = new GameObject(DropReceiverName, typeof(RectTransform), typeof(Image));
                receiverObject.transform.SetParent(transform, false);
                existing = receiverObject.transform;
            }

            _dropReceiverRect = existing as RectTransform;
            _dropReceiverImage = existing.GetComponent<Image>();
            if (_dropReceiverImage == null) _dropReceiverImage = existing.gameObject.AddComponent<Image>();

            _dropReceiverRect.anchorMin = Vector2.zero;
            _dropReceiverRect.anchorMax = Vector2.one;
            _dropReceiverRect.offsetMin = Vector2.zero;
            _dropReceiverRect.offsetMax = Vector2.zero;
            _dropReceiverRect.localScale = Vector3.one;
            _dropReceiverRect.localEulerAngles = Vector3.zero;
            _dropReceiverImage.sprite = null;
            _dropReceiverImage.color = Color.clear;
            _dropReceiverImage.enabled = false;
            _dropReceiverImage.raycastTarget = false;
            _dropReceiverRect.gameObject.SetActive(false);
        }

        void EnsureLabel()
        {
            const string labelName = "Scene Slot Label";
            Transform existing = transform.Find(labelName);
            RectTransform labelRect;
            if (existing == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(transform, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                labelText = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existing as RectTransform;
                labelText = existing.GetComponent<Text>();
                if (labelText == null) labelText = existing.gameObject.AddComponent<Text>();
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);

            labelText.alignment = TextAnchor.MiddleCenter;
            if (uiFont != null) labelText.font = uiFont;
            labelText.fontSize = 16;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 11;
            labelText.resizeTextMaxSize = 16;
            labelText.raycastTarget = false;
            labelText.enabled = false;
            labelText.gameObject.SetActive(false);
        }

        void ConsumeDraggedHandCard(UcgCardView cardView, RectTransform cardRect)
        {
            cardView.SetSelected(false);
            cardView.SetDragging(false);
            cardView.SetPlayableHighlight(false);

            var dragCard = cardRect.GetComponent<UIDragCard>();
            if (dragCard != null) dragCard.enabled = false;

            var dragLayerCard = cardRect.GetComponent<UcgDragLayerCard>();
            if (dragLayerCard != null) dragLayerCard.enabled = false;

            var hover = cardRect.GetComponent<UIHandCardHover>();
            if (hover != null) hover.enabled = false;

            var canvasGroup = cardRect.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            var graphics = cardRect.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].enabled = false;
                graphics[i].raycastTarget = false;
            }

            cardRect.SetParent(null, false);
            cardRect.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(cardRect.gameObject);
            }
            else
            {
                DestroyImmediate(cardRect.gameObject);
            }
        }

        void RefreshColor()
        {
            RefreshSceneSlotVisual();
        }

        void EnsureHighlightFrame()
        {
            Transform existingFrame = transform.Find(HighlightFrameName);
            RectTransform frameRect;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(HighlightFrameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(transform, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                _highlightFrameImage = frameObject.GetComponent<Image>();
                _highlightFrameOutline = frameObject.GetComponent<Outline>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                _highlightFrameImage = existingFrame.GetComponent<Image>();
                if (_highlightFrameImage == null) _highlightFrameImage = existingFrame.gameObject.AddComponent<Image>();
                _highlightFrameOutline = existingFrame.GetComponent<Outline>();
                if (_highlightFrameOutline == null) _highlightFrameOutline = existingFrame.gameObject.AddComponent<Outline>();
            }

            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = sceneCardSize + new Vector2(28f, 18f);
            _highlightFrameImage.raycastTarget = false;
            _highlightFrameImage.enabled = false;
            _highlightFrameOutline.effectDistance = new Vector2(2f, -2f);
            _highlightFrameOutline.enabled = false;
            frameRect.gameObject.SetActive(false);
            frameRect.SetAsFirstSibling();

            _highlightFramePulse = frameRect.GetComponent<UcgGuidancePulse>();
            if (_highlightFramePulse == null) _highlightFramePulse = frameRect.gameObject.AddComponent<UcgGuidancePulse>();
            _highlightFramePulse.targetImage = _highlightFrameImage;
            _highlightFramePulse.targetRect = frameRect;
            _highlightFramePulse.targetOutline = _highlightFrameOutline;
            _highlightFramePulse.pulseScale = true;
            _highlightFramePulse.scaleAmplitude = 0.01f;
            _highlightFramePulse.pulseAlpha = true;
            _highlightFramePulse.alphaAmplitude = 0.026f;
            _highlightFramePulse.speed = 1.8f;
            _highlightFramePulse.enabled = false;
        }

        void EnsureGuideArrow()
        {
            if (_guideArrowRect != null && _guideArrowText != null) return;

            Transform existingArrow = transform.Find(GuideArrowName);
            if (existingArrow == null)
            {
                var arrowObject = new GameObject(GuideArrowName, typeof(RectTransform), typeof(Text));
                arrowObject.transform.SetParent(transform, false);
                existingArrow = arrowObject.transform;
            }

            _guideArrowRect = existingArrow as RectTransform;
            _guideArrowText = existingArrow.GetComponent<Text>();
            if (_guideArrowText == null) _guideArrowText = existingArrow.gameObject.AddComponent<Text>();

            _guideArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _guideArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _guideArrowRect.pivot = new Vector2(0.5f, 0.5f);
            _guideArrowRect.anchoredPosition = new Vector2(0f, GetSlotHeight() * 0.5f + 14f);
            _guideArrowRect.sizeDelta = new Vector2(34f, 34f);
            _guideArrowRect.localScale = Vector3.one;
            _guideArrowRect.localEulerAngles = Vector3.zero;
            _guideArrowRect.SetAsLastSibling();

            _guideArrowText.text = "▼";
            _guideArrowText.alignment = TextAnchor.MiddleCenter;
            _guideArrowText.color = new Color(0.62f, 0.98f, 1f, 0.58f);
            if (uiFont != null) _guideArrowText.font = uiFont;
            _guideArrowText.fontSize = 26;
            _guideArrowText.resizeTextForBestFit = true;
            _guideArrowText.resizeTextMinSize = 22;
            _guideArrowText.resizeTextMaxSize = 26;
            _guideArrowText.raycastTarget = false;

            _guideArrowPulse = _guideArrowRect.GetComponent<UcgGuidancePulse>();
            if (_guideArrowPulse == null) _guideArrowPulse = _guideArrowRect.gameObject.AddComponent<UcgGuidancePulse>();
            _guideArrowPulse.targetText = _guideArrowText;
            _guideArrowPulse.targetRect = _guideArrowRect;
            _guideArrowPulse.pulseScale = true;
            _guideArrowPulse.scaleAmplitude = 0.016f;
            _guideArrowPulse.pulseAlpha = true;
            _guideArrowPulse.alphaAmplitude = 0.06f;
            _guideArrowPulse.bobAmplitude = 4f;
            _guideArrowPulse.speed = 2.2f;

            _guideArrowText.enabled = false;
            _guideArrowRect.gameObject.SetActive(false);
        }

        void EnsureScenePlacedToast()
        {
            if (_scenePlacedToastRect != null && _scenePlacedToastText != null) return;

            Transform existingToast = transform.Find(ScenePlacedToastName);
            if (existingToast == null)
            {
                var toastObject = new GameObject(ScenePlacedToastName, typeof(RectTransform), typeof(Text));
                toastObject.transform.SetParent(transform, false);
                existingToast = toastObject.transform;
            }

            _scenePlacedToastRect = existingToast as RectTransform;
            _scenePlacedToastText = existingToast.GetComponent<Text>();
            if (_scenePlacedToastText == null) _scenePlacedToastText = existingToast.gameObject.AddComponent<Text>();
            var toastCanvas = existingToast.GetComponent<Canvas>();
            if (toastCanvas == null) toastCanvas = existingToast.gameObject.AddComponent<Canvas>();
            toastCanvas.overrideSorting = true;
            toastCanvas.sortingOrder = 3460;

            _scenePlacedToastRect.anchorMin = new Vector2(0.5f, 0.5f);
            _scenePlacedToastRect.anchorMax = new Vector2(0.5f, 0.5f);
            _scenePlacedToastRect.pivot = new Vector2(0.5f, 0.5f);
            _scenePlacedToastRect.anchoredPosition = new Vector2(0f, GetSlotHeight() * 0.5f + 16f);
            _scenePlacedToastRect.sizeDelta = new Vector2(260f, 34f);
            _scenePlacedToastRect.localScale = Vector3.one;
            _scenePlacedToastRect.localEulerAngles = Vector3.zero;

            _scenePlacedToastText.text = "場景卡已設置";
            _scenePlacedToastText.alignment = TextAnchor.MiddleCenter;
            _scenePlacedToastText.color = new Color(0.78f, 0.98f, 1f, 0.82f);
            if (uiFont != null) _scenePlacedToastText.font = uiFont;
            _scenePlacedToastText.fontSize = 20;
            _scenePlacedToastText.resizeTextForBestFit = true;
            _scenePlacedToastText.resizeTextMinSize = 14;
            _scenePlacedToastText.resizeTextMaxSize = 20;
            _scenePlacedToastText.raycastTarget = false;

            _scenePlacedToastPulse = _scenePlacedToastRect.GetComponent<UcgGuidancePulse>();
            if (_scenePlacedToastPulse == null) _scenePlacedToastPulse = _scenePlacedToastRect.gameObject.AddComponent<UcgGuidancePulse>();
            _scenePlacedToastPulse.targetText = _scenePlacedToastText;
            _scenePlacedToastPulse.targetRect = _scenePlacedToastRect;
            _scenePlacedToastPulse.pulseAlpha = true;
            _scenePlacedToastPulse.alphaAmplitude = 0.12f;
            _scenePlacedToastPulse.pulseScale = true;
            _scenePlacedToastPulse.scaleAmplitude = 0.02f;
            _scenePlacedToastPulse.speed = 2.6f;

            _scenePlacedToastText.enabled = false;
            _scenePlacedToastRect.gameObject.SetActive(false);
        }

        void BeginSceneJustPlacedFeedback()
        {
            StopSceneJustPlacedFeedback();
            if (!Application.isPlaying)
            {
                _sceneJustPlacedActive = false;
                RefreshSceneSlotVisual();
                return;
            }

            _sceneJustPlacedActive = true;
            ApplySceneSlotVisualState(UcgSceneSlotVisualState.SceneJustPlaced);
            _sceneJustPlacedRoutine = StartCoroutine(SceneJustPlacedFeedbackRoutine());
        }

        IEnumerator SceneJustPlacedFeedbackRoutine()
        {
            RectTransform cardRect = currentSceneView != null
                ? currentSceneView.transform as RectTransform
                : null;

            float duration = 0.72f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (cardRect != null)
                {
                    float scale = Mathf.Lerp(1.075f, 1f, eased);
                    cardRect.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            if (cardRect != null) cardRect.localScale = Vector3.one;
            _sceneJustPlacedActive = false;
            _sceneJustPlacedRoutine = null;
            RefreshSceneSlotVisual();
        }

        void StopSceneJustPlacedFeedback()
        {
            if (_sceneJustPlacedRoutine != null)
            {
                StopCoroutine(_sceneJustPlacedRoutine);
                _sceneJustPlacedRoutine = null;
            }

            if (_orientationAfterRebuildRoutine != null)
            {
                StopCoroutine(_orientationAfterRebuildRoutine);
                _orientationAfterRebuildRoutine = null;
            }

            if (currentSceneView != null)
            {
                RectTransform cardRect = currentSceneView.transform as RectTransform;
                if (cardRect != null) cardRect.localScale = Vector3.one;
            }
        }

        float GetSlotHeight()
        {
            RectTransform slotRect = transform as RectTransform;
            if (slotRect == null) return sceneCardSize.y;
            float slotHeight = slotRect.rect.height;
            if (slotHeight <= 0f) slotHeight = slotRect.sizeDelta.y;
            if (slotHeight <= 0f) slotHeight = sceneCardSize.y;
            return slotHeight;
        }

        bool IsCurrentSceneRoot(Transform target)
        {
            return currentSceneView != null && target == currentSceneView.transform;
        }

        bool IsCurrentSceneChild(Transform target)
        {
            return currentSceneView != null
                && target != null
                && target.IsChildOf(currentSceneView.transform);
        }

        bool IsSceneSlotDebugVerbose()
        {
            return debugSceneSlotVerbose;
        }

        void DebugSceneSlotVisualState()
        {
            RectTransform cardRect = currentSceneView != null
                ? currentSceneView.transform as RectTransform
                : null;
            Image cardImage = currentSceneView != null ? currentSceneView.CardArtImage : null;
            string cardId = _currentSceneData != null ? _currentSceneData.id : "none";
            string cardName = _currentSceneData != null ? _currentSceneData.cardName : "none";
            Vector2 size = cardRect != null ? cardRect.sizeDelta : Vector2.zero;
            Vector3 rotation = cardRect != null ? cardRect.localEulerAngles : Vector3.zero;

            Debug.Log(
                "SceneSlot visual state:\n" +
                $"state={_visualState}\n" +
                $"sceneCard={cardId} / {cardName}\n" +
                $"sceneCardRectSize={size}\n" +
                $"sceneCardRotation={rotation}\n" +
                $"sceneCardImageEnabled={(cardImage != null && cardImage.enabled)}\n" +
                $"activeBackgroundObjects=[{string.Join(", ", _lastActiveBackgroundObjects.ToArray())}]\n" +
                $"activeRedPlaceholderCardBackObjects=[{string.Join(", ", _lastActiveRedOrPlaceholderObjects.ToArray())}]\n" +
                $"glowObjectActive={(currentSceneView != null && currentSceneView.GlowImage != null && currentSceneView.GlowImage.gameObject.activeInHierarchy && currentSceneView.GlowImage.enabled)}\n" +
                $"dropReceiverActive={(_dropReceiverImage != null && _dropReceiverImage.gameObject.activeInHierarchy && _dropReceiverImage.enabled && _dropReceiverImage.raycastTarget)}");
        }

        void DebugSceneCardFinalOrientation()
        {
            RectTransform viewRoot = currentSceneView != null
                ? currentSceneView.transform as RectTransform
                : null;
            RectTransform cardArt = currentSceneView != null ? currentSceneView.CardArtRect : null;
            string sceneCardId = _currentSceneData != null ? _currentSceneData.id : "none";
            Vector2 rectSize = viewRoot != null ? viewRoot.sizeDelta : Vector2.zero;
            float viewRootRotation = viewRoot != null ? NormalizeEulerZ(viewRoot.localEulerAngles.z) : 0f;
            float cardArtRotation = cardArt != null ? NormalizeEulerZ(cardArt.localEulerAngles.z) : 0f;
            float finalDisplayedRotation = currentSceneView != null
                ? currentSceneView.FinalDisplayedRotation
                : 0f;

            Debug.Log(
                "Scene card final orientation:\n" +
                $"sceneCardId={sceneCardId}\n" +
                $"viewRootRotation={viewRootRotation}\n" +
                $"cardArtRotation={cardArtRotation}\n" +
                $"finalDisplayedRotation={finalDisplayedRotation}\n" +
                $"rectSize={rectSize}\n" +
                "isSceneOnBoard=true\n" +
                $"displayedImage={(cardArt != null ? GetHierarchyPath(cardArt) : "none")}");
        }

        public void DebugPrintSceneSlotHierarchy()
        {
            var builder = new StringBuilder();
            string sceneCardId = _currentSceneData != null ? _currentSceneData.id : "none";
            string sceneCardName = _currentSceneData != null ? _currentSceneData.cardName : "none";
            builder.AppendLine("SharedSceneSlot hierarchy:");
            builder.AppendLine($"state={_visualState}");
            builder.AppendLine("sceneCard=" + sceneCardId + " / " + sceneCardName);
            for (int i = 0; i < transform.childCount; i++)
            {
                AppendHierarchy(transform.GetChild(i), 0, builder);
            }

            Debug.Log(builder.ToString());
        }

        void AppendHierarchy(Transform node, int depth, StringBuilder builder)
        {
            if (node == null) return;

            string indent = new string(' ', depth * 2);
            var image = node.GetComponent<Image>();
            var graphic = node.GetComponent<Graphic>();
            var rect = node as RectTransform;
            var outline = node.GetComponent<Outline>();
            var canvasGroup = node.GetComponent<CanvasGroup>();
            var mask = node.GetComponent<Mask>();
            var rectMask = node.GetComponent<RectMask2D>();
            bool redOrMaroon = image != null && IsRedOrMaroonColor(image.color);
            bool transparentBackground = image != null && IsSemiTransparentBackgroundImage(image);
            bool placeholderOrBack = IsPlaceholderOrBackName(node.name)
                || (image != null && image.sprite != null && IsPlaceholderOrBackName(image.sprite.name));
            var cardView = node.GetComponentInParent<UcgCardView>();
            bool faceDown = cardView != null && cardView.IsFaceDown;
            string canvasGroupInfo = canvasGroup != null
                ? "alpha=" + canvasGroup.alpha.ToString("0.###") + ",blocks=" + canvasGroup.blocksRaycasts + ",interactable=" + canvasGroup.interactable
                : "none";

            builder.Append(indent);
            builder.Append("- ");
            builder.Append(GetHierarchyPath(node));
            builder.Append($" activeSelf={node.gameObject.activeSelf}");
            builder.Append($" activeInHierarchy={node.gameObject.activeInHierarchy}");
            builder.Append($" imageExists={image != null}");
            if (image != null)
            {
                builder.Append($" imageEnabled={image.enabled}");
                builder.Append($" imageColor={FormatColor(image.color)}");
                builder.Append($" imageAlpha={image.color.a:0.###}");
                builder.Append($" sprite={(image.sprite != null ? image.sprite.name : "null")}");
            }

            builder.Append($" rectSize={(rect != null ? rect.rect.size.ToString() : "n/a")}");
            builder.Append($" raycastTarget={(graphic != null && graphic.raycastTarget)}");
            builder.Append($" outline={(outline != null && outline.enabled)}");
            builder.Append($" canvasGroup={canvasGroupInfo}");
            builder.Append($" mask={(mask != null && mask.enabled)}");
            builder.Append($" rectMask={(rectMask != null && rectMask.enabled)}");
            builder.Append($" redOrMaroon={redOrMaroon}");
            builder.Append($" transparentBackground={transparentBackground}");
            builder.Append($" placeholderOrCardBack={placeholderOrBack || faceDown}");
            builder.AppendLine();

            for (int i = 0; i < node.childCount; i++)
            {
                AppendHierarchy(node.GetChild(i), depth + 1, builder);
            }
        }

        static float NormalizeEulerZ(float z)
        {
            z %= 360f;
            if (z > 180f) z -= 360f;
            if (z <= -180f) z += 360f;
            return z;
        }

        string GetHierarchyPath(Transform target)
        {
            if (target == null) return "null";
            var names = new List<string>();
            Transform current = target;
            while (current != null)
            {
                names.Add(current.name);
                if (current == transform) break;
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        string FormatColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        bool IsPotentialRedBackImage(Image image)
        {
            if (image == null) return false;
            var cardView = image.GetComponentInParent<UcgCardView>();
            return IsRedOrMaroonColor(image.color)
                || IsPlaceholderOrBackName(image.name)
                || (image.sprite != null && IsPlaceholderOrBackName(image.sprite.name))
                || (cardView != null && cardView.IsFaceDown);
        }

        bool IsSemiTransparentBackgroundImage(Image image)
        {
            if (image == null || image.color.a <= 0.025f || image.color.a >= 0.92f) return false;
            string name = image.name.ToLowerInvariant();
            bool backgroundName = name.Contains("background")
                || name.Contains("backing")
                || name.Contains("drop")
                || name.Contains("slot")
                || name.Contains("frame")
                || name.Contains("placeholder")
                || name.Contains("debug");
            RectTransform rect = image.rectTransform;
            Vector2 size = rect != null ? rect.rect.size : Vector2.zero;
            if (size == Vector2.zero && rect != null) size = rect.sizeDelta;
            bool large = Mathf.Abs(size.x) >= sceneCardSize.x * 0.82f
                && Mathf.Abs(size.y) >= sceneCardSize.y * 0.72f;
            return backgroundName || large;
        }

        bool IsRedOrMaroonColor(Color color)
        {
            if (color.a <= 0.025f) return false;
            return color.r > 0.32f
                && color.r > color.g * 1.55f
                && color.r > color.b * 1.15f
                && color.g < 0.34f;
        }

        bool IsPlaceholderOrBackName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string lower = value.ToLowerInvariant();
            return lower.Contains("back")
                || lower.Contains("背面")
                || lower.Contains("placeholder")
                || lower.Contains("cardback")
                || lower.Contains("card back")
                || lower.Contains("maroon")
                || lower.Contains("red");
        }
    }
}
