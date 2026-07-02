using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    public enum UcgBattlefieldViewMode
    {
        FocusLane,
        OverviewAll
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UcgBattlefieldManager : MonoBehaviour
    {
        const float OverviewPlaymatInnerLeftRatio = 0.03f;
        const float OverviewPlaymatInnerRightRatio = 0.97f;
        const float OverviewPlaymatInnerTopRatio = 0.80f;
        const float OverviewPlaymatInnerBottomRatio = 0.36f;
        const float OverviewLaneAreaLeftRatio = 0.03f;
        const float OverviewLaneAreaRightRatio = 0.86f;
        const float OverviewRightRailLeftRatio = 0.88f;
        const float OverviewRightRailRightRatio = 0.97f;
        const float OverviewLaneLayoutCount = 8f;
        const float OverviewLaneRightRailGap = 170f;
        const float OverviewRotatedCardClearance = 72f;
        const float OverviewSceneRowGap = 36f;
        const float OverviewRowCenterlineTighten = 0.80f;
        const float OverviewLaneAreaLeftPadding = 24f;
        const float OverviewRightRailCardPadding = 48f;
        const float OverviewViewportRightPadding = 36f;
        const float OverviewVerticalEdgeMargin = 96f;
        const float OverviewPortraitCardAspect = 0.716f;
        const float OverviewPlaymatWorldAspect = 853f / 1280f; // Matches Docs/LayoutRefs/target_playmat_layout.png.
        const float FocusViewOpponentRowTopPadding = 72f;
        const float FocusViewPlayerRowBottomPadding = 118f;
        const float FocusViewCenterOffsetY = 0f;

        public int maxLaneCount = 8;
        public int initialLaneCount = 3;
        public int visibleLaneCount = 3;

        public ScrollRect scrollRect;
        public RectTransform viewport;
        public RectTransform content;
        public RectTransform lanesRoot;
        public UcgTutorialGuide tutorialGuide;
        public UcgTurnManager turnManager;
        public UcgPhaseManager phaseManager;
        public UcgCardInfoPanel cardInfoPanel;
        public Text resultText;
        public Font uiFont;
        public Sprite opponentCardSprite;
        public UcgOpponentScript opponentScript;
        public UcgTestMode opponentTestMode;
        public bool debugBattlefieldScroll;
        public bool forceOverviewOnly = true;

        public Vector2 placedCardSize = new Vector2(190f, 276f);
        public Vector2 opponentCardSize = new Vector2(172f, 250f);
        public Vector2 laneSize = new Vector2(300f, 660f);
        public Vector2 playerSlotSize = new Vector2(228f, 332f);
        public Vector2 opponentSlotSize = new Vector2(220f, 286f);
        public float laneSpacing = 30f;
        public float activeLaneScrollDuration = 0.32f;
        public float overviewScale = 0.4f;
        public float focusScale = 0.88f;
        public float overviewVisualCompensationScale = 1.65f;
        public float overviewLaneSpacingMultiplier = 1.3f;
        public float overviewContentLeftShift = 0f;
        public float overviewRowScreenOffset = 0f;
        public float overviewSafeLeftPadding = 70f;
        public float overviewSafeRightReservedWidth = 325f;
        public float overviewSafeRightPadding = 76f;
        public float overviewSafeTopPadding = 90f;
        public float overviewSafeBottomPadding = 150f;
        public float focusViewportPosition = 0.42f;
        public float combatAreaOffsetX;
        public float rightAuxiliaryColumnGutterWidth;
        public bool debugBattlefieldLayout;
        public bool hasInitializedBattlefieldView;
        [SerializeField] bool showViewTransformDebugButtons = true;
        [SerializeField] bool showPlaymatBoundsDebugOverlay = false;

        readonly List<UcgBattleLane> _lanes = new List<UcgBattleLane>();
        readonly List<RectTransform> _focusViewSafeRects = new List<RectTransform>();
        Coroutine _activeLaneScrollCoroutine;
        UcgBattlefieldViewMode _currentViewMode = UcgBattlefieldViewMode.OverviewAll;
        string _lastViewTransformSceneSyncInfo = "<none>";

        public List<UcgBattleLane> Lanes => _lanes;
        public bool IsViewTransformOnlyActive { get; private set; }
        public bool IsViewTransformOnlyTransitionRunning => _activeLaneScrollCoroutine != null;
        public bool IsViewTransformOnlyFocusFraming { get; private set; }
        public event System.Action BeforeViewTransformOnlyFocus;

        public void Configure(UcgTutorialGuide guide, UcgTurnManager turns, UcgPhaseManager phases, UcgCardInfoPanel infoPanel, Text sharedResultText, Vector2 cardSize, Sprite fixedOpponentSprite, Font font)
        {
            tutorialGuide = guide;
            turnManager = turns;
            phaseManager = phases;
            cardInfoPanel = infoPanel;
            resultText = sharedResultText;
            placedCardSize = cardSize;
            opponentCardSprite = fixedOpponentSprite;
            uiFont = font;
            EnsureLanesRoot();
            BuildLanes();
        }

        public void BuildLanes()
        {
            EnsureLanesRoot();
            ClearGeneratedLanes();

            int laneCount = Mathf.Max(1, maxLaneCount);
            float contentWidth = GetContentWidth(laneCount);
            content.sizeDelta = new Vector2(contentWidth, GetContentHeight(contentWidth));

            for (int i = 0; i < laneCount; i++)
            {
                var laneObject = new GameObject($"Lane {i + 1}", typeof(RectTransform), typeof(UcgBattleLane));
                laneObject.transform.SetParent(lanesRoot, false);

                var laneRect = laneObject.GetComponent<RectTransform>();
                ApplyLaneRect(laneRect, i);
                LogLaneRectState("BuildLanes:AfterInitialApplyLaneRect", i, laneRect);

                var lane = laneObject.GetComponent<UcgBattleLane>();
                lane.Initialize(i, uiFont, resultText, tutorialGuide, turnManager, phaseManager, playerSlotSize, opponentSlotSize, placedCardSize);
                ApplyLaneRect(laneRect, i);
                LogLaneRectState("BuildLanes:AfterInitializeApplyLaneRect", i, laneRect);
                lane.ConfigureOpponentScript(opponentScript, opponentTestMode);
                lane.ConfigureFixedOpponentCard(CreateFixedOpponentCardData(i), opponentCardSprite, cardInfoPanel, opponentCardSize);
                _lanes.Add(lane);
            }

            Canvas.ForceUpdateCanvases();
            RefreshOpenedLaneVisibility(turnManager != null ? turnManager.currentTurn : 1);
            SetContentToStart();
            LogAllLaneAnchoredPositions("BuildLanes:Completed");
            LogScrollDebugState("BuildLanes completed");
        }

        public void ResetBattlefield()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].ResetLane();
                    _lanes[i].ConfigureOpponentScript(opponentScript, opponentTestMode);
                    _lanes[i].ConfigureFixedOpponentCard(CreateFixedOpponentCardData(_lanes[i].laneIndex), opponentCardSprite, cardInfoPanel, opponentCardSize);
                }
            }

            RefreshOpenedLaneVisibility(turnManager != null ? turnManager.currentTurn : 1);
            SetContentToStart();
        }

        public List<UcgPlayArea> GetAllPlayerPlayAreas()
        {
            var playAreas = new List<UcgPlayArea>();
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] == null) continue;

                UcgPlayArea playArea = _lanes[i].GetPlayerPlayArea();
                if (playArea != null)
                {
                    playAreas.Add(playArea);
                }
            }

            return playAreas;
        }

        public UcgBattleLane GetLane(int index)
        {
            if (index < 0 || index >= _lanes.Count) return null;
            return _lanes[index];
        }

        public List<UcgBattleLane> GetAllVisibleLanes()
        {
            return new List<UcgBattleLane>(_lanes);
        }

        public List<UcgBattleLane> GetAllLanes()
        {
            return new List<UcgBattleLane>(_lanes);
        }

        public List<UcgBattleLane> GetOpenedLanes(int currentTurn)
        {
            var openedLanes = new List<UcgBattleLane>();
            int openedCount = GetOpenedLaneCount(currentTurn);
            for (int i = 0; i < openedCount; i++)
            {
                if (_lanes[i] != null)
                {
                    openedLanes.Add(_lanes[i]);
                }
            }

            return openedLanes;
        }

        public int GetOpenedLaneCount(int currentTurn)
        {
            return Mathf.Clamp(currentTurn, 1, _lanes.Count);
        }

        public void RefreshOpenedLaneVisibility(int currentTurn)
        {
            int openedCount = GetVisibleLaneCount(currentTurn);
            for (int i = 0; i < _lanes.Count; i++)
            {
                SetLaneOpened(i, i < openedCount);
            }
        }

        int GetVisibleLaneCount(int currentTurn)
        {
            return Mathf.Clamp(maxLaneCount, 1, _lanes.Count);
        }

        public void SetLaneOpened(int laneIndex, bool opened)
        {
            UcgBattleLane lane = GetLane(laneIndex);
            if (lane == null) return;
            lane.gameObject.SetActive(opened);
        }

        public void ConfigureOpponentScript(UcgOpponentScript script, UcgTestMode mode)
        {
            opponentScript = script;
            opponentTestMode = mode;

            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].ConfigureOpponentScript(script, mode);
                }
            }
        }

        public void ScrollToActiveLane()
        {
            if (turnManager == null) return;
            int laneIndex = turnManager.ActiveNewLaneIndex;
            if (laneIndex < 0) return;
            FocusActiveLane(laneIndex, "ScrollToActiveLane");
        }

        public void JumpToActiveLane(string source = "JumpToActiveLane")
        {
            if (turnManager == null)
            {
                FocusActiveLane(0, source);
                return;
            }

            int laneIndex = turnManager.ActiveNewLaneIndex;
            FocusActiveLane(laneIndex < 0 ? 0 : laneIndex, source);
        }

        public void ScrollToLane(int laneIndex)
        {
            FocusActiveLane(laneIndex);
        }

        [System.Obsolete("Legacy View API may trigger ApplyOverviewVisualCompensation / layout reflow. Use ViewTransformOnly APIs instead.", false)]
        public void FocusActiveLane(int laneIndex, string source = "FocusActiveLane")
        {
            if (forceOverviewOnly)
            {
                ShowOverviewInstant(source);
                return;
            }

            SetBattlefieldView(UcgBattlefieldViewMode.FocusLane, laneIndex, false, source);
        }

        [System.Obsolete("Legacy View API may trigger ApplyOverviewVisualCompensation / layout reflow. Use ViewTransformOnly APIs instead.", false)]
        public void SmoothFocusActiveLane(int laneIndex)
        {
            if (forceOverviewOnly)
            {
                ShowOverviewInstant("SmoothFocusActiveLane");
                return;
            }

            SetBattlefieldView(UcgBattlefieldViewMode.FocusLane, laneIndex, true, "SmoothFocusActiveLane");
        }

        public void FocusActiveLaneViewTransformOnly(int laneIndex, bool smooth, string source)
        {
            ApplyFocusLaneViewTransformOnly(laneIndex, smooth, source);
        }

        public void SetFocusViewSafeRects(params RectTransform[] rects)
        {
            _focusViewSafeRects.Clear();
            if (rects == null) return;

            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || _focusViewSafeRects.Contains(rect)) continue;

                _focusViewSafeRects.Add(rect);
            }
        }

        public void ReportViewTransformSceneSync(string info)
        {
            _lastViewTransformSceneSyncInfo = string.IsNullOrWhiteSpace(info) ? "<empty>" : info;
        }

        void ApplyFocusLaneViewTransformOnly(int laneIndex, bool smooth, string source)
        {
            LogViewTransformOnlyState($"{source}:ApplyFocusStart", laneIndex, 1f, 0f, 0f, -1f, smooth);
            LogLane1AndContentState($"{source}:BeforeFocusTarget");
            BeforeViewTransformOnlyFocus?.Invoke();
            if (!TryGetFocusLaneViewTransformOnlyTargets(laneIndex, out float targetX, out float targetY, out float targetScale))
            {
                Debug.Log($"[UCG ViewTransformOnly]\nsource={source}:ApplyFocusFailed\nlaneIndex={laneIndex}\nphase={GetViewTransformPhaseName()}");
                return;
            }

            int clampedLaneIndex = Mathf.Clamp(laneIndex, 0, _lanes.Count - 1);
            float viewportRatio = GetFocusLaneViewportRatio(clampedLaneIndex);
            LogViewTransformOnlyTarget(source, clampedLaneIndex, targetScale, targetX, targetY, viewportRatio, smooth);
            if (ShouldLogFocusSafeDiagnostic(source))
            {
                LogFocusSafeDiagnostic(source, clampedLaneIndex, targetScale, targetX, targetY);
            }
            StopActiveLaneScroll();
            IsViewTransformOnlyActive = true;
            IsViewTransformOnlyFocusFraming = true;
            if (smooth && Application.isPlaying && activeLaneScrollDuration > 0f)
            {
                _activeLaneScrollCoroutine = StartCoroutine(SmoothViewTransformOnly(targetX, targetY, targetScale, source));
                return;
            }

            SetViewTransformOnly(targetX, targetY, targetScale, source);
            LogLane1AndContentState($"{source}:AfterFocusInstant");
        }

        public void ShowOverviewViewTransformOnly(bool smooth, string source)
        {
            if (content == null || viewport == null || _lanes.Count == 0) return;

            LogViewTransformOnlyState($"{source}:ShowOverviewStart", -1, 1f, 0f, 0f, -1f, smooth);
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int overviewLaneCount = GetOverviewTargetLaneCount(currentTurn);
            float targetScale = GetOverviewScaleForLaneCount(overviewLaneCount);
            float targetX = GetOverviewTargetX(targetScale, overviewLaneCount);
            LogViewTransformOnlyTarget(source, Mathf.Max(0, overviewLaneCount - 1), targetScale, targetX, 0f, -1f, smooth);
            StopActiveLaneScroll();
            IsViewTransformOnlyActive = true;
            IsViewTransformOnlyFocusFraming = false;
            if (smooth && Application.isPlaying && activeLaneScrollDuration > 0f)
            {
                _activeLaneScrollCoroutine = StartCoroutine(SmoothViewTransformOnly(targetX, 0f, targetScale, source));
                return;
            }

            SetViewTransformOnly(targetX, 0f, targetScale, source);
        }

        public void ShowOverview()
        {
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int overviewLaneCount = GetOverviewTargetLaneCount(currentTurn);
            SetBattlefieldView(UcgBattlefieldViewMode.OverviewAll, overviewLaneCount - 1, !forceOverviewOnly, "ShowOverview");
        }

        public void ShowOverviewInstant(string source = "ShowOverviewInstant")
        {
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int overviewLaneCount = GetOverviewTargetLaneCount(currentTurn);
            SetBattlefieldView(UcgBattlefieldViewMode.OverviewAll, overviewLaneCount - 1, false, source);
        }

        void SetBattlefieldView(UcgBattlefieldViewMode viewMode, int laneIndex, bool smooth, string source)
        {
            if (content == null || viewport == null) return;
            if (_lanes.Count == 0) return;
            if (smooth && IsAnyCardDragging()) return;

            IsViewTransformOnlyActive = false;
            IsViewTransformOnlyFocusFraming = false;

            if (forceOverviewOnly)
            {
                StopActiveLaneScroll();
                viewMode = UcgBattlefieldViewMode.OverviewAll;
                int overviewLaneCount = GetOverviewTargetLaneCount(turnManager != null ? turnManager.currentTurn : 1);
                laneIndex = overviewLaneCount - 1;
                smooth = false;
            }

            if (viewMode == UcgBattlefieldViewMode.FocusLane)
            {
                Debug.LogWarning("Legacy View API may trigger ApplyOverviewVisualCompensation / layout reflow. Use ViewTransformOnly APIs instead.");
                return;
            }

            _currentViewMode = viewMode;
            UpdateViewportMaskForCurrentView();
            int clampedLaneIndex = Mathf.Clamp(laneIndex, 0, _lanes.Count - 1);
            float targetScale = viewMode == UcgBattlefieldViewMode.OverviewAll
                ? GetOverviewScaleForLaneCount(clampedLaneIndex + 1)
                : Mathf.Clamp(focusScale, 0.1f, 1f);
            float targetX = viewMode == UcgBattlefieldViewMode.OverviewAll
                ? GetOverviewTargetX(targetScale, clampedLaneIndex + 1)
                : GetFocusLaneTargetX(clampedLaneIndex);
            Vector2 beforePosition = content.anchoredPosition;
            Vector2 targetPosition = GetTargetContentPosition(targetX, targetScale);

            if (smooth)
            {
                SmoothSetContentView(targetX, targetScale);
            }
            else
            {
                SetContentView(targetX, targetScale);
            }

            LogBattlefieldLayoutAnimation(source, viewMode, smooth, beforePosition, targetPosition);
        }

        public void FlipAllFaceDownCards()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].FlipAllFaceDownCards();
                }
            }
        }

        public List<UcgEffectInstance> FlipAllFaceDownCardsAndCollectEffects()
        {
            var revealedEffects = new List<UcgEffectInstance>();
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null && _lanes[i].gameObject.activeSelf)
                {
                    _lanes[i].FlipAllFaceDownCards(revealedEffects);
                }
            }

            return revealedEffects;
        }

        public void ClearTemporaryBpModifiers()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].ClearTemporaryBpModifiers();
                }
            }
        }

        public void RestoreRestedCards()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].RestoreRestedCards();
                }
            }
        }

        public void ClearSceneBpModifiers()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].ClearSceneBpModifiers();
                }
            }
        }

        public void ClearConditionalBpModifiers()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].ClearConditionalBpModifiers();
                }
            }
        }

        public void ClearLaneHighlights()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                UcgPlayArea playArea = _lanes[i] != null ? _lanes[i].GetPlayerPlayArea() : null;
                if (playArea != null)
                {
                    playArea.SetHighlightState(UcgLaneHighlightState.Normal);
                }

                if (_lanes[i] != null)
                {
                    _lanes[i].SetActiveLaneFocus(false);
                    _lanes[i].ClearEffectTargetHighlight();
                }
            }
        }

        void EnsureLanesRoot()
        {
            RectTransform rootRect = transform as RectTransform;
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, 940f);
            float viewportHeight = Mathf.Clamp(laneSize.y > 0f ? laneSize.y : 820f, 820f, 1040f);
            rootRect.sizeDelta = new Vector2(1040f, viewportHeight);

            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null) scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 18f;

            ClearLegacyLaneChildren();
            viewport = EnsureViewport(rootRect);
            content = EnsureContent(viewport);
            lanesRoot = content;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
        }

        void ClearLegacyLaneChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Viewport") continue;
                if (child.GetComponent<UcgBattleLane>() == null) continue;

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

        void ClearGeneratedLanes()
        {
            _lanes.Clear();

            for (int i = lanesRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = lanesRoot.GetChild(i);
                if (child.GetComponent<UcgBattleLane>() == null) continue;

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

        RectTransform EnsureViewport(RectTransform rootRect)
        {
            const string viewportName = "Viewport";
            Transform existingViewport = transform.Find(viewportName);
            RectTransform viewportRect;
            Image viewportImage;

            if (existingViewport == null)
            {
                var viewportObject = new GameObject(viewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportObject.transform.SetParent(transform, false);
                viewportRect = viewportObject.GetComponent<RectTransform>();
                viewportImage = viewportObject.GetComponent<Image>();
            }
            else
            {
                viewportRect = existingViewport as RectTransform;
                viewportImage = existingViewport.GetComponent<Image>();
                if (viewportImage == null) viewportImage = existingViewport.gameObject.AddComponent<Image>();
                if (existingViewport.GetComponent<RectMask2D>() == null) existingViewport.gameObject.AddComponent<RectMask2D>();
            }

            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);

            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            RectMask2D viewportMask = viewportRect.GetComponent<RectMask2D>();
            if (viewportMask != null)
            {
                viewportMask.enabled = ShouldClipBattlefieldViewport();
            }

            return viewportRect;
        }

        bool ShouldClipBattlefieldViewport()
        {
            return !forceOverviewOnly && _currentViewMode != UcgBattlefieldViewMode.OverviewAll;
        }

        void UpdateViewportMaskForCurrentView()
        {
            RectMask2D viewportMask = viewport != null ? viewport.GetComponent<RectMask2D>() : null;
            if (viewportMask != null)
            {
                viewportMask.enabled = ShouldClipBattlefieldViewport();
            }
        }

        RectTransform EnsureContent(RectTransform viewportRect)
        {
            const string contentName = "Content";
            Transform existingContent = viewportRect.Find(contentName);
            RectTransform contentRect;

            if (existingContent == null)
            {
                var contentObject = new GameObject(contentName, typeof(RectTransform));
                contentObject.transform.SetParent(viewportRect, false);
                contentRect = contentObject.GetComponent<RectTransform>();
            }
            else
            {
                contentRect = existingContent as RectTransform;
            }

            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            float contentWidth = GetContentWidth(maxLaneCount);
            contentRect.sizeDelta = new Vector2(contentWidth, GetContentHeight(contentWidth));

            return contentRect;
        }

        Vector2 GetLanePosition(int index)
        {
            return new Vector2(GetLaneLeftX(index), 0f);
        }

        float GetLaneLeftX(int index)
        {
            return GetVisualOrderIndex(index) * GetLaneStep();
        }

        float GetLaneLeftXForScale(int index, float scale)
        {
            float t = GetOverviewBlend(scale);
            float extraSpacing = laneSpacing * (Mathf.Max(1f, overviewLaneSpacingMultiplier) - 1f) * t;
            return GetLaneLeftX(index) + GetVisualOrderIndex(index) * extraSpacing;
        }

        int GetVisualOrderIndex(int laneIndex)
        {
            int laneCount = Mathf.Max(1, maxLaneCount);
            return Mathf.Clamp(laneCount - 1 - laneIndex, 0, laneCount - 1);
        }

        float GetLaneStep()
        {
            return laneSize.x + laneSpacing;
        }

        float GetMaxScrollX()
        {
            float scale = content != null ? content.localScale.x : 1f;
            return GetMaxScrollX(scale);
        }

        float GetMaxScrollX(float scale)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float contentWidth = content != null && content.rect.width > 0f ? content.rect.width : GetContentWidth(maxLaneCount);
            float overviewLeftShiftAllowance = Mathf.Max(0f, overviewContentLeftShift) * GetOverviewBlend(scale);
            return Mathf.Max(0f, contentWidth * Mathf.Max(0.1f, scale) - viewportWidth) + overviewLeftShiftAllowance;
        }

        void SetContentToStart()
        {
            StopActiveLaneScroll();
            if (forceOverviewOnly)
            {
                ShowOverviewInstant("SetContentToStart");
                return;
            }

            _currentViewMode = UcgBattlefieldViewMode.FocusLane;
            SetContentView(GetFocusLaneTargetX(0), Mathf.Clamp(focusScale, 0.1f, 1f));
        }

        void SetContentAnchoredX(float targetX)
        {
            SetContentView(targetX, content != null ? content.localScale.x : 1f);
        }

        // Legacy View API may trigger ApplyOverviewVisualCompensation / layout reflow.
        // Use ViewTransformOnly APIs instead for Focus / camera-like view changes.
        void SetContentView(float targetX, float targetScale)
        {
            if (content == null) return;

            float clampedScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            content.localScale = new Vector3(clampedScale, clampedScale, 1f);
            content.anchoredPosition = GetTargetContentPosition(targetX, targetScale);
            ApplyOverviewVisualCompensation(clampedScale);

            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.velocity = Vector2.zero;
                scrollRect.horizontalNormalizedPosition = maxScrollX > 0f ? -clampedTargetX / maxScrollX : 0f;
            }
        }

        void SmoothSetContentView(float targetX, float targetScale)
        {
            if (!Application.isPlaying || activeLaneScrollDuration <= 0f)
            {
                SetContentView(targetX, targetScale);
                return;
            }

            StopActiveLaneScroll();
            _activeLaneScrollCoroutine = StartCoroutine(SmoothViewTo(targetX, targetScale));
        }

        // Legacy View API may trigger ApplyOverviewVisualCompensation / layout reflow.
        // Use ViewTransformOnly APIs instead for Focus / camera-like view changes.
        IEnumerator SmoothViewTo(float targetX, float targetScale)
        {
            float startX = content != null ? content.anchoredPosition.x - combatAreaOffsetX : 0f;
            float startScale = content != null ? content.localScale.x : 1f;
            float clampedTargetScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedTargetScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            float elapsed = 0f;

            while (elapsed < activeLaneScrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / activeLaneScrollDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetContentView(
                    Mathf.Lerp(startX, clampedTargetX, eased),
                    Mathf.Lerp(startScale, clampedTargetScale, eased));
                yield return null;
            }

            SetContentView(clampedTargetX, clampedTargetScale);
            _activeLaneScrollCoroutine = null;
        }

        public void PreviewFocusLaneViewTransformOnly(int laneIndex)
        {
            int internalLaneIndex = Mathf.Max(0, laneIndex - 1);
            ApplyFocusLaneViewTransformOnly(internalLaneIndex, true, $"DebugButton Focus {laneIndex:00}");
        }

        [ContextMenu("UCG/Test ViewTransform Focus Lane 01")]
        void TestViewTransformFocusLane01()
        {
            PreviewFocusLaneViewTransformOnly(1);
        }

        [ContextMenu("UCG/Test ViewTransform Focus Lane 02")]
        void TestViewTransformFocusLane02()
        {
            PreviewFocusLaneViewTransformOnly(2);
        }

        [ContextMenu("UCG/Test ViewTransform Focus Lane 04")]
        void TestViewTransformFocusLane04()
        {
            PreviewFocusLaneViewTransformOnly(4);
        }

        [ContextMenu("UCG/Test ViewTransform Overview")]
        void TestViewTransformOverview()
        {
            ShowOverviewInstant();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!showViewTransformDebugButtons) return;

            const float panelWidth = 132f;
            const float panelHeight = 138f;
            GUILayout.BeginArea(new Rect(12f, 12f, panelWidth, panelHeight), "View Transform", GUI.skin.window);
            if (GUILayout.Button("Focus 01")) PreviewFocusLaneViewTransformOnly(1);
            if (GUILayout.Button("Focus 02")) PreviewFocusLaneViewTransformOnly(2);
            if (GUILayout.Button("Focus 04")) PreviewFocusLaneViewTransformOnly(4);
            if (GUILayout.Button("Overview")) ShowOverviewViewTransformOnly(true, "DebugButton Overview");
            showPlaymatBoundsDebugOverlay = GUILayout.Toggle(showPlaymatBoundsDebugOverlay, "Bounds");
            GUILayout.EndArea();

            DrawPlaymatBoundsDebugOverlay();
        }
#endif

        public float CalculateFocusLaneScale(
            float viewportWidth,
            float lanePitch,
            float footprintWidth,
            float sideVisibilityRatio)
        {
            float safeViewportWidth = Mathf.Max(1f, viewportWidth);
            float safeLanePitch = Mathf.Max(1f, lanePitch);
            float safeFootprintWidth = Mathf.Max(1f, footprintWidth);
            float sideRatio = Mathf.Clamp(sideVisibilityRatio, 0.5f, 0.95f);
            float visibleWorldWidth = 2f * (safeLanePitch + (sideRatio - 0.5f) * safeFootprintWidth);
            float rawScale = safeViewportWidth / Mathf.Max(1f, visibleWorldWidth);
            float minimumScale = Mathf.Clamp(overviewScale, 0.1f, 1f);
            return Mathf.Clamp(rawScale, minimumScale, 1f);
        }

        public float GetLaneCenterForView(int laneIndex)
        {
            if (content == null || laneIndex < 0 || laneIndex >= _lanes.Count)
            {
                return GetLaneLeftX(Mathf.Max(0, laneIndex)) + laneSize.x * 0.5f;
            }

            UcgBattleLane lane = _lanes[laneIndex];
            RectTransform slot = lane != null
                ? (lane.playerSlot != null ? lane.playerSlot : lane.opponentSlot)
                : null;
            if (slot != null)
            {
                Vector3 slotWorldCenter = slot.TransformPoint(slot.rect.center);
                return content.InverseTransformPoint(slotWorldCenter).x;
            }

            RectTransform laneRect = lane != null ? lane.transform as RectTransform : null;
            if (laneRect != null)
            {
                Vector3 laneWorldCenter = laneRect.TransformPoint(laneRect.rect.center);
                return content.InverseTransformPoint(laneWorldCenter).x;
            }

            return GetLaneLeftX(laneIndex) + laneSize.x * 0.5f;
        }

        public float GetContentTargetXForWorldPoint(
            float worldX,
            float viewportRatio,
            float scale)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float targetViewportX = viewportWidth * Mathf.Clamp01(viewportRatio);
            return targetViewportX - worldX * Mathf.Max(0.1f, scale) - combatAreaOffsetX;
        }

        public void SetViewTransformOnly(float targetX, float targetScale, string source = "SetViewTransformOnly")
        {
            SetViewTransformOnly(targetX, 0f, targetScale, source);
        }

        public void SetViewTransformOnly(float targetX, float targetY, float targetScale, string source = "SetViewTransformOnly")
        {
            if (content == null) return;

            IsViewTransformOnlyActive = true;
            ViewTransformOnlyLayoutSnapshot beforeSnapshot = CaptureViewTransformOnlyLayoutSnapshot();
            float clampedScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            content.localScale = new Vector3(clampedScale, clampedScale, 1f);
            content.anchoredPosition = new Vector2(clampedTargetX + combatAreaOffsetX, targetY);

            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.velocity = Vector2.zero;
                scrollRect.horizontalNormalizedPosition = maxScrollX > 0f ? -clampedTargetX / maxScrollX : 0f;
            }

            AssertViewTransformOnlyDidNotMutateBattleLayout(beforeSnapshot);
            LogViewTransformOnlyApplied($"{source}:SetApplied", targetScale, clampedTargetX, targetY);
            LogLane1AndContentState($"{source}:AfterSetViewTransformOnly");
        }

        public IEnumerator SmoothViewTransformOnly(float targetX, float targetScale)
        {
            yield return SmoothViewTransformOnly(targetX, 0f, targetScale, "SmoothViewTransformOnly");
        }

        public IEnumerator SmoothViewTransformOnly(float targetX, float targetY, float targetScale, string source = "SmoothViewTransformOnly")
        {
            if (!Application.isPlaying || activeLaneScrollDuration <= 0f)
            {
                SetViewTransformOnly(targetX, targetY, targetScale, source);
                yield break;
            }

            LogViewTransformOnlyState($"{source}:SmoothStart", -1, targetScale, targetX, targetY, -1f, true);
            IsViewTransformOnlyActive = true;
            float startX = content != null ? content.anchoredPosition.x - combatAreaOffsetX : 0f;
            float startY = content != null ? content.anchoredPosition.y : 0f;
            float startScale = content != null ? content.localScale.x : 1f;
            float clampedTargetScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedTargetScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            float elapsed = 0f;

            while (elapsed < activeLaneScrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / activeLaneScrollDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetViewTransformOnly(
                    Mathf.Lerp(startX, clampedTargetX, eased),
                    Mathf.Lerp(startY, targetY, eased),
                    Mathf.Lerp(startScale, clampedTargetScale, eased),
                    source);
                yield return null;
            }

            SetViewTransformOnly(clampedTargetX, targetY, clampedTargetScale, source);
            _activeLaneScrollCoroutine = null;
            LogViewTransformOnlyApplied($"{source}:SmoothComplete", clampedTargetScale, clampedTargetX, targetY);
            LogLane1AndContentState($"{source}:AfterSmoothComplete");
        }

        bool TryGetFocusLaneViewTransformOnlyTargets(int laneIndex, out float targetX, out float targetY, out float targetScale)
        {
            targetX = 0f;
            targetY = 0f;
            targetScale = 1f;
            if (content == null || viewport == null || _lanes.Count == 0) return false;

            int clampedLaneIndex = Mathf.Clamp(laneIndex, 0, _lanes.Count - 1);
            float viewportWidth = viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float lanePitch = GetLanePitchForView(clampedLaneIndex);
            float footprintWidth = GetViewTransformRotatedFootprintWidth(clampedLaneIndex);
            targetScale = CalculateFocusLaneScale(viewportWidth, lanePitch, footprintWidth, 0.88f);
            float laneCenterX = GetLaneCenterForView(clampedLaneIndex);
            float viewportRatio = GetFocusLaneViewportRatio(clampedLaneIndex);
            targetX = GetContentTargetXForWorldPoint(laneCenterX, viewportRatio, targetScale);
            targetY = GetContentTargetYForFocusLaneView(clampedLaneIndex, targetScale);
            return true;
        }

        void LogViewTransformOnlyTarget(
            string source,
            int laneIndex,
            float targetScale,
            float targetX,
            float targetY,
            float viewportRatio,
            bool smooth)
        {
            float contentScaleBefore = content != null ? content.localScale.x : 1f;
            Vector2 contentPosBefore = content != null ? content.anchoredPosition : Vector2.zero;
            float maxScrollX = GetMaxScrollX(targetScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            Vector2 contentPosAfterTarget = new Vector2(clampedTargetX + combatAreaOffsetX, targetY);
            Debug.Log(
                "[UCG ViewTransformOnly]\n"
                + $"source={source}\n"
                + $"laneIndex={laneIndex}\n"
                + $"targetScale={targetScale:0.###}\n"
                + $"targetX={targetX:0.#}\n"
                + $"targetY={targetY:0.#}\n"
                + GetFocusViewTargetDebugDetails(laneIndex, targetScale, targetX, targetX, targetY)
                + $"viewportRatio={(viewportRatio >= 0f ? viewportRatio.ToString("0.###") : "overview")}\n"
                + $"smooth={smooth}\n"
                + $"phase={GetViewTransformPhaseName()}\n"
                + $"coroutineRunning={IsViewTransformOnlyTransitionRunning}\n"
                + $"contentScaleBefore={contentScaleBefore:0.###}\n"
                + $"contentPosBefore={FormatVector2(contentPosBefore)}\n"
                + $"contentScaleAfterTarget={targetScale:0.###}\n"
                + $"contentPosAfterTarget={FormatVector2(contentPosAfterTarget)}");
        }

        void LogViewTransformOnlyState(
            string source,
            int laneIndex,
            float targetScale,
            float targetX,
            float targetY,
            float viewportRatio,
            bool smooth)
        {
            float contentScale = content != null ? content.localScale.x : 1f;
            Vector2 contentPosition = content != null ? content.anchoredPosition : Vector2.zero;
            Debug.Log(
                "[UCG ViewTransformOnly]\n"
                + $"source={source}\n"
                + $"laneIndex={laneIndex}\n"
                + $"targetScale={targetScale:0.###}\n"
                + $"targetX={targetX:0.#}\n"
                + $"targetY={targetY:0.#}\n"
                + $"viewportRatio={(viewportRatio >= 0f ? viewportRatio.ToString("0.###") : "overview")}\n"
                + $"smooth={smooth}\n"
                + $"phase={GetViewTransformPhaseName()}\n"
                + $"coroutineRunning={IsViewTransformOnlyTransitionRunning}\n"
                + $"contentScaleBefore={contentScale:0.###}\n"
                + $"contentPosBefore={FormatVector2(contentPosition)}\n"
                + $"contentScaleAfterTarget={targetScale:0.###}\n"
                + $"contentPosAfterTarget={FormatVector2(new Vector2(targetX + combatAreaOffsetX, targetY))}");
        }

        string GetFocusViewTargetDebugDetails(int laneIndex, float targetScale, float rawTargetX, float finalTargetX, float targetY)
        {
            if (laneIndex < 0 || laneIndex >= _lanes.Count) return "";

            float safeScale = Mathf.Max(0.1f, targetScale);
            float laneCenterX = GetLaneCenterForView(laneIndex);
            float viewportRatio = GetFocusLaneViewportRatio(laneIndex);
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float laneWindowCenterX = (viewportWidth * Mathf.Clamp01(viewportRatio) - finalTargetX - combatAreaOffsetX) / safeScale;
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect rightRailRect = GetOverviewRightRailRect(playmatInnerRect);
            Rect sceneAreaRect = GetOverviewSceneRect();
            TryGetFocusSafeBoundsInContent(laneIndex, out Rect focusSafeBounds);

            return
                $"focusLaneIndex={laneIndex}\n"
                + $"laneCenterX={laneCenterX:0.#}\n"
                + $"laneWindowCenterX={laneWindowCenterX:0.#}\n"
                + $"rawTargetX={rawTargetX:0.#}\n"
                + $"finalTargetX={finalTargetX:0.#}\n"
                + $"targetXChangedByRightRail={Mathf.Abs(rawTargetX - finalTargetX) > 0.1f}\n"
                + $"playmatInnerRect={FormatRect(playmatInnerRect)}\n"
                + $"rightRailRect={FormatRect(rightRailRect)}\n"
                + $"deckTrashFocusBounds={FormatRect(focusSafeBounds)}\n"
                + $"sceneAreaRect={FormatRect(sceneAreaRect)}\n";
        }

        bool ShouldLogFocusSafeDiagnostic(string source)
        {
            return !string.IsNullOrEmpty(source)
                && source.StartsWith("DebugButton Focus", System.StringComparison.Ordinal);
        }

        void LogFocusSafeDiagnostic(string source, int laneIndex, float targetScale, float targetX, float targetY)
        {
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            float playmatCenterY = playmatInnerRect.center.y;
            Rect viewportContentRect = GetViewportContentRect(targetX, targetY, targetScale);
            Rect battleLaneBounds = GetBattleLaneBounds();
            Rect opponentRowBounds = GetRowBounds(true);
            Rect playerRowBounds = GetRowBounds(false);
            Rect expectedSceneAreaBounds = GetOverviewSceneRect();
            Rect actualSceneAreaBounds = GetRegisteredSafeBounds(IsSceneSafeRect, expectedSceneAreaBounds);
            Vector2 sceneDesignAnchor = GetOverviewSceneCenter();
            Vector2 normalCardSize = GetOverviewNormalCardSize();
            Vector2 overviewStandardCardSize = GetOverviewStandardCardSize(1f);
            Vector2 actualBattleCardSize = GetOverviewActualBattleCardSize(out string actualBattleCardSizeSource);
            Vector2 deckZoneSingleCardSize = GetRegisteredSingleCardSize(IsDeckSafeRect, out string deckZoneSingleCardSizeSource);
            Vector2 trashZoneSingleCardSize = GetRegisteredSingleCardSize(IsTrashSafeRect, out string trashZoneSingleCardSizeSource);
            Vector2 playerRowSingleCardSize = GetRowSingleCardSize(false, out string playerRowSingleCardSizeSource);
            Vector2 opponentRowSingleCardSize = GetRowSingleCardSize(true, out string opponentRowSingleCardSizeSource);
            Vector2 expectedSceneHorizontalCardSize = GetOverviewSceneAreaSize();
            Rect rightRailLayoutBounds = GetOverviewRightRailRect(playmatInnerRect);
            Rect rightRailRegisteredBounds = GetRegisteredSafeBounds(IsRightRailSafeRect, rightRailLayoutBounds);
            Rect deckBounds = GetRegisteredSafeBounds(IsDeckSafeRect, new Rect());
            Rect trashBounds = GetRegisteredSafeBounds(IsTrashSafeRect, new Rect());
            float focusLaneCenterX = laneIndex >= 0 && laneIndex < _lanes.Count ? GetLaneCenterForView(laneIndex) : 0f;
            float viewportCenterX = viewportContentRect.center.x;
            float sceneDeltaX = actualSceneAreaBounds.width > 0f && battleLaneBounds.width > 0f
                ? actualSceneAreaBounds.center.x - battleLaneBounds.center.x
                : 0f;
            float sceneDeltaY = actualSceneAreaBounds.width > 0f
                ? actualSceneAreaBounds.center.y - playmatCenterY
                : 0f;
            float sceneDesignAnchorDeltaX = actualSceneAreaBounds.width > 0f
                ? actualSceneAreaBounds.center.x - sceneDesignAnchor.x
                : 0f;
            float playerBottomDistance = playerRowBounds.width > 0f
                ? playerRowBounds.yMin - playmatInnerRect.yMin
                : 0f;
            float opponentTopDistance = opponentRowBounds.width > 0f
                ? playmatInnerRect.yMax - opponentRowBounds.yMax
                : 0f;
            bool anyOutsidePlaymat =
                IsRectOutside(playmatInnerRect, opponentRowBounds)
                || IsRectOutside(playmatInnerRect, playerRowBounds)
                || IsRectOutside(playmatInnerRect, actualSceneAreaBounds)
                || IsRectOutside(playmatInnerRect, rightRailRegisteredBounds)
                || IsRectOutside(playmatInnerRect, deckBounds)
                || IsRectOutside(playmatInnerRect, trashBounds);

            var builder = new StringBuilder();
            builder.AppendLine("[UCG FocusSafeDiagnostic]");
            builder.AppendLine($"source={source}");
            builder.AppendLine($"focusLaneIndex={laneIndex}");
            builder.AppendLine($"targetScale={FormatFloat(targetScale)}");
            builder.AppendLine($"targetX={FormatFloat(targetX)}");
            builder.AppendLine($"targetY={FormatFloat(targetY)}");
            builder.AppendLine($"playmatInnerRect={FormatRect(playmatInnerRect)}");
            builder.AppendLine($"playmatCenterY={FormatFloat(playmatCenterY)}");
            builder.AppendLine($"viewportContentRect={FormatRect(viewportContentRect)}");
            builder.AppendLine($"battleLaneBounds={FormatRect(battleLaneBounds)}");
            builder.AppendLine($"laneCenters={FormatLaneCenters()}");
            builder.AppendLine($"focusLaneCenterX={FormatFloat(focusLaneCenterX)}");
            builder.AppendLine($"viewportCenterXInContent={FormatFloat(viewportCenterX)}");
            builder.AppendLine($"focusLaneCenterDeltaFromViewportCenter={FormatFloat(focusLaneCenterX - viewportCenterX)}");
            builder.AppendLine($"opponentRowBounds={FormatRect(opponentRowBounds)}");
            builder.AppendLine($"playerRowBounds={FormatRect(playerRowBounds)}");
            builder.AppendLine($"sceneAreaBounds={FormatRect(actualSceneAreaBounds)}");
            builder.AppendLine($"expectedSceneAreaBounds={FormatRect(expectedSceneAreaBounds)}");
            builder.AppendLine("sceneDesignAnchor=Lane01Lane02Midpoint");
            builder.AppendLine($"placedCardSize={FormatVector2(placedCardSize)}");
            builder.AppendLine($"overviewStandardCardSize={FormatVector2(overviewStandardCardSize)}");
            builder.AppendLine($"actualBattleCardSize={FormatVector2(actualBattleCardSize)}");
            builder.AppendLine($"actualBattleCardSizeSource={actualBattleCardSizeSource}");
            builder.AppendLine($"deckZoneSingleCardSize={FormatVector2(deckZoneSingleCardSize)} source={deckZoneSingleCardSizeSource}");
            builder.AppendLine($"trashZoneSingleCardSize={FormatVector2(trashZoneSingleCardSize)} source={trashZoneSingleCardSizeSource}");
            builder.AppendLine($"playerRowSingleCardSize={FormatVector2(playerRowSingleCardSize)} source={playerRowSingleCardSizeSource}");
            builder.AppendLine($"opponentRowSingleCardSize={FormatVector2(opponentRowSingleCardSize)} source={opponentRowSingleCardSizeSource}");
            builder.AppendLine($"normalCardSize={FormatVector2(normalCardSize)}");
            builder.AppendLine($"sceneCardSizeSource={GetOverviewSceneCardSizeSource()}");
            builder.AppendLine($"expectedSceneHorizontalCardSize={FormatVector2(expectedSceneHorizontalCardSize)}");
            builder.AppendLine($"rightRailBounds={FormatRect(rightRailRegisteredBounds)}");
            builder.AppendLine($"rightRailLayoutBounds={FormatRect(rightRailLayoutBounds)}");
            builder.AppendLine($"deckBounds={FormatRect(deckBounds)}");
            builder.AppendLine($"trashBounds={FormatRect(trashBounds)}");
            builder.AppendLine($"sceneCenterDeltaFromBattleLaneBoundsCenter={FormatFloat(sceneDeltaX)}");
            builder.AppendLine($"sceneCenterDeltaFromSceneDesignAnchor={FormatFloat(sceneDesignAnchorDeltaX)}");
            builder.AppendLine($"sceneCenterDeltaFromPlaymatCenterY={FormatFloat(sceneDeltaY)}");
            builder.AppendLine($"playerRowDistanceFromPlaymatBottom={FormatFloat(playerBottomDistance)}");
            builder.AppendLine($"opponentRowDistanceFromPlaymatTop={FormatFloat(opponentTopDistance)}");
            builder.AppendLine($"rightRailOverflowOutsidePlaymat={FormatOverflow(playmatInnerRect, rightRailRegisteredBounds)}");
            builder.AppendLine($"deckOverflowOutsidePlaymat={FormatOverflow(playmatInnerRect, deckBounds)}");
            builder.AppendLine($"trashOverflowOutsidePlaymat={FormatOverflow(playmatInnerRect, trashBounds)}");
            builder.AppendLine($"anyRegisteredBattleObjectOutsidePlaymat={anyOutsidePlaymat}");
            builder.AppendLine($"lastSceneSync={_lastViewTransformSceneSyncInfo}");
            builder.AppendLine($"registeredSceneInfo={FormatRegisteredSceneInfo()}");
            builder.AppendLine("registeredBattleRects:");
            AppendRegisteredSafeRectDiagnostics(builder, playmatInnerRect);
            Debug.Log(builder.ToString());
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void DrawPlaymatBoundsDebugOverlay()
        {
            if (!showPlaymatBoundsDebugOverlay || content == null || viewport == null) return;

            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect battleLaneBounds = GetBattleLaneBounds();
            Rect rightRailLayoutBounds = GetOverviewRightRailRect(playmatInnerRect);
            Rect deckBounds = GetRegisteredSafeBounds(IsDeckSafeRect, new Rect());
            Rect trashBounds = GetRegisteredSafeBounds(IsTrashSafeRect, new Rect());
            Rect sceneBounds = GetRegisteredSafeBounds(IsSceneSafeRect, GetOverviewSceneRect());
            Rect viewportContentRect = GetCurrentViewportContentRect();

            DrawContentRect(playmatInnerRect, new Color(0.15f, 0.95f, 1f, 0.95f), "playmatInnerRect");
            DrawContentHorizontalLine(playmatInnerRect.xMin, playmatInnerRect.xMax, playmatInnerRect.center.y, new Color(1f, 0.95f, 0.15f, 0.95f), "playmatCenterY");
            DrawContentRect(battleLaneBounds, new Color(0.25f, 0.95f, 0.25f, 0.95f), "battleLaneBounds");
            DrawContentRect(rightRailLayoutBounds, new Color(1f, 0.45f, 0.1f, 0.95f), "rightRailLayoutBounds");
            DrawContentRect(deckBounds, new Color(0.2f, 0.55f, 1f, 0.95f), "deckBounds");
            DrawContentRect(trashBounds, new Color(1f, 0.2f, 0.65f, 0.95f), "trashBounds");
            DrawContentRect(sceneBounds, new Color(0.85f, 0.25f, 1f, 0.95f), "sceneAreaBounds");
            DrawContentRect(viewportContentRect, new Color(1f, 1f, 1f, 0.9f), "viewportContentRect");
        }

        Rect GetCurrentViewportContentRect()
        {
            if (viewport == null || content == null) return new Rect();

            float safeScale = Mathf.Max(0.1f, content.localScale.x);
            Vector2 contentPosition = content.anchoredPosition;
            Rect viewportRect = viewport.rect;
            return Rect.MinMaxRect(
                (viewportRect.xMin - contentPosition.x) / safeScale,
                (viewportRect.yMin - contentPosition.y) / safeScale,
                (viewportRect.xMax - contentPosition.x) / safeScale,
                (viewportRect.yMax - contentPosition.y) / safeScale);
        }

        void DrawContentRect(Rect contentRect, Color color, string label)
        {
            if (contentRect.width <= 0f || contentRect.height <= 0f) return;

            Vector2 p1 = ContentPointToGuiPoint(new Vector2(contentRect.xMin, contentRect.yMin));
            Vector2 p2 = ContentPointToGuiPoint(new Vector2(contentRect.xMin, contentRect.yMax));
            Vector2 p3 = ContentPointToGuiPoint(new Vector2(contentRect.xMax, contentRect.yMax));
            Vector2 p4 = ContentPointToGuiPoint(new Vector2(contentRect.xMax, contentRect.yMin));
            DrawGuiLine(p1, p2, color, 2f);
            DrawGuiLine(p2, p3, color, 2f);
            DrawGuiLine(p3, p4, color, 2f);
            DrawGuiLine(p4, p1, color, 2f);

            if (!string.IsNullOrEmpty(label))
            {
                DrawOverlayLabel(p2 + new Vector2(4f, 4f), label, color);
            }
        }

        void DrawContentHorizontalLine(float xMin, float xMax, float y, Color color, string label)
        {
            Vector2 p1 = ContentPointToGuiPoint(new Vector2(xMin, y));
            Vector2 p2 = ContentPointToGuiPoint(new Vector2(xMax, y));
            DrawGuiLine(p1, p2, color, 2f);
            DrawOverlayLabel(p1 + new Vector2(4f, -18f), label, color);
        }

        Vector2 ContentPointToGuiPoint(Vector2 contentPoint)
        {
            Canvas canvas = content != null ? content.GetComponentInParent<Canvas>() : null;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector3 worldPoint = content != null
                ? content.TransformPoint(new Vector3(contentPoint.x, contentPoint.y, 0f))
                : new Vector3(contentPoint.x, contentPoint.y, 0f);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPoint);
            return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        }

        void DrawGuiLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            Vector2 delta = end - start;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        void DrawOverlayLabel(Vector2 position, string label, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(position.x, position.y, 240f, 22f), label);
            GUI.color = previousColor;
        }
#endif

        Rect GetViewportContentRect(float targetX, float targetY, float targetScale)
        {
            if (viewport == null) return new Rect();

            float safeScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(safeScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            Vector2 contentPosition = new Vector2(clampedTargetX + combatAreaOffsetX, targetY);
            Rect viewportRect = viewport.rect;
            return Rect.MinMaxRect(
                (viewportRect.xMin - contentPosition.x) / safeScale,
                (viewportRect.yMin - contentPosition.y) / safeScale,
                (viewportRect.xMax - contentPosition.x) / safeScale,
                (viewportRect.yMax - contentPosition.y) / safeScale);
        }

        Rect GetBattleLaneBounds()
        {
            Rect bounds = new Rect();
            bool hasBounds = false;
            for (int i = 0; i < _lanes.Count; i++)
            {
                UcgBattleLane lane = _lanes[i];
                if (lane == null) continue;

                EncapsulateRectTransformBoundsInContent(lane.opponentSlot, ref bounds, ref hasBounds);
                EncapsulateRectTransformBoundsInContent(lane.playerSlot, ref bounds, ref hasBounds);
            }

            return hasBounds ? bounds : new Rect();
        }

        Rect GetRowBounds(bool opponent)
        {
            Rect bounds = new Rect();
            bool hasBounds = false;
            for (int i = 0; i < _lanes.Count; i++)
            {
                UcgBattleLane lane = _lanes[i];
                RectTransform slot = lane != null
                    ? opponent ? lane.opponentSlot : lane.playerSlot
                    : null;
                EncapsulateRectTransformBoundsInContent(slot, ref bounds, ref hasBounds);
            }

            return hasBounds ? bounds : new Rect();
        }

        Rect GetRegisteredSafeBounds(System.Func<RectTransform, bool> predicate, Rect fallback)
        {
            Rect bounds = new Rect();
            bool hasBounds = false;
            for (int i = 0; i < _focusViewSafeRects.Count; i++)
            {
                RectTransform rect = _focusViewSafeRects[i];
                if (rect == null || predicate == null || !predicate(rect)) continue;

                EncapsulateRectTransformBoundsInContent(rect, ref bounds, ref hasBounds);
            }

            return hasBounds ? bounds : fallback;
        }

        bool IsSceneSafeRect(RectTransform rect)
        {
            return rect != null && rect.name.IndexOf("Scene", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool IsRightRailSafeRect(RectTransform rect)
        {
            return rect != null && !IsSceneSafeRect(rect);
        }

        bool IsDeckSafeRect(RectTransform rect)
        {
            return rect != null && rect.name.IndexOf("Deck", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool IsTrashSafeRect(RectTransform rect)
        {
            if (rect == null) return false;

            return rect.name.IndexOf("Discard", System.StringComparison.OrdinalIgnoreCase) >= 0
                || rect.name.IndexOf("Trash", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        string FormatLaneCenters()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append("Lane ").Append((i + 1).ToString("00")).Append('=').Append(FormatFloat(GetLaneCenterForView(i)));
            }

            return builder.ToString();
        }

        void AppendRegisteredSafeRectDiagnostics(StringBuilder builder, Rect playmatInnerRect)
        {
            for (int i = 0; i < _focusViewSafeRects.Count; i++)
            {
                RectTransform rect = _focusViewSafeRects[i];
                if (rect == null)
                {
                    builder.AppendLine($"  [{i}] <null>");
                    continue;
                }

                Rect bounds = new Rect();
                bool hasBounds = false;
                EncapsulateRectTransformBoundsInContent(rect, ref bounds, ref hasBounds);
                builder.AppendLine(
                    $"  [{i}] name={rect.name}, active={rect.gameObject.activeInHierarchy}, bounds={(hasBounds ? FormatRect(bounds) : "<none>")}, outsidePlaymat={(hasBounds && IsRectOutside(playmatInnerRect, bounds))}");
            }
        }

        string FormatRegisteredSceneInfo()
        {
            for (int i = 0; i < _focusViewSafeRects.Count; i++)
            {
                RectTransform rect = _focusViewSafeRects[i];
                if (!IsSceneSafeRect(rect)) continue;

                RectTransform root = rect.parent as RectTransform;
                return
                    $"index={i}, slotName={rect.name}, slotParent={(rect.parent != null ? rect.parent.name : "<none>")}, slotAnchored={FormatVector2(rect.anchoredPosition)}, rootName={(root != null ? root.name : "<none>")}, rootParent={(root != null && root.parent != null ? root.parent.name : "<none>")}, rootAnchored={(root != null ? FormatVector2(root.anchoredPosition) : "<none>")}, path={GetTransformPath(rect)}";
            }

            return "<none>";
        }

        string GetTransformPath(Transform transform)
        {
            if (transform == null) return "<none>";

            var stack = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack.ToArray());
        }

        bool IsRectOutside(Rect container, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f) return false;

            return rect.xMin < container.xMin
                || rect.xMax > container.xMax
                || rect.yMin < container.yMin
                || rect.yMax > container.yMax;
        }

        string FormatOverflow(Rect container, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f) return "<none>";

            float left = Mathf.Max(0f, container.xMin - rect.xMin);
            float right = Mathf.Max(0f, rect.xMax - container.xMax);
            float bottom = Mathf.Max(0f, container.yMin - rect.yMin);
            float top = Mathf.Max(0f, rect.yMax - container.yMax);
            float max = Mathf.Max(Mathf.Max(left, right), Mathf.Max(bottom, top));
            return $"left={FormatFloat(left)}, right={FormatFloat(right)}, bottom={FormatFloat(bottom)}, top={FormatFloat(top)}, max={FormatFloat(max)}";
        }

        void LogViewTransformOnlyApplied(string source, float targetScale, float targetX, float targetY)
        {
            float contentScaleAfter = content != null ? content.localScale.x : 1f;
            Vector2 contentPositionAfter = content != null ? content.anchoredPosition : Vector2.zero;
            Debug.Log(
                "[UCG ViewTransformOnly]\n"
                + $"source={source}\n"
                + $"laneIndex=-1\n"
                + $"targetScale={targetScale:0.###}\n"
                + $"targetX={targetX:0.#}\n"
                + $"targetY={targetY:0.#}\n"
                + $"viewportRatio=applied\n"
                + $"smooth={Application.isPlaying && activeLaneScrollDuration > 0f}\n"
                + $"phase={GetViewTransformPhaseName()}\n"
                + $"coroutineRunning={IsViewTransformOnlyTransitionRunning}\n"
                + $"contentScaleAfter={contentScaleAfter:0.###}\n"
                + $"contentPosAfter={FormatVector2(contentPositionAfter)}");
        }

        string GetViewTransformPhaseName()
        {
            return phaseManager != null ? phaseManager.CurrentPhase.ToString() : "<none>";
        }

        float GetContentTargetYForFocusLaneView(int laneIndex, float scale)
        {
            if (viewport == null || laneIndex < 0 || laneIndex >= _lanes.Count) return 0f;

            UcgBattleLane lane = _lanes[laneIndex];
            RectTransform opponentSlot = lane != null ? lane.opponentSlot : null;
            RectTransform playerSlot = lane != null ? lane.playerSlot : null;
            if (opponentSlot == null || playerSlot == null) return 0f;

            float viewportHeight = viewport.rect.height > 0f ? viewport.rect.height : 960f;
            float safeScale = Mathf.Max(0.1f, scale);
            float viewportTop = viewportHeight * 0.5f;
            float viewportBottom = -viewportTop;

            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            float idealTargetY = FocusViewCenterOffsetY - playmatInnerRect.center.y * safeScale;
            if (!TryGetFocusSafeBoundsInContent(laneIndex, out Rect focusBounds))
            {
                return idealTargetY;
            }

            float topLimit = viewportTop - FocusViewOpponentRowTopPadding - focusBounds.yMax * safeScale;
            float bottomLimit = viewportBottom + FocusViewPlayerRowBottomPadding - focusBounds.yMin * safeScale;
            if (bottomLimit <= topLimit)
            {
                return Mathf.Clamp(idealTargetY, bottomLimit, topLimit);
            }

            return (bottomLimit + topLimit) * 0.5f;
        }

        bool TryGetFocusSafeBoundsInContent(int laneIndex, out Rect bounds)
        {
            bounds = new Rect();
            bool hasBounds = false;

            if (laneIndex >= 0 && laneIndex < _lanes.Count)
            {
                UcgBattleLane lane = _lanes[laneIndex];
                if (lane != null)
                {
                    EncapsulateRectTransformBoundsInContent(lane.opponentSlot, ref bounds, ref hasBounds);
                    EncapsulateRectTransformBoundsInContent(lane.playerSlot, ref bounds, ref hasBounds);
                }
            }

            EncapsulateRect(GetOverviewSceneRect(), ref bounds, ref hasBounds);
            for (int i = 0; i < _focusViewSafeRects.Count; i++)
            {
                EncapsulateRectTransformBoundsInContent(_focusViewSafeRects[i], ref bounds, ref hasBounds);
            }

            if (!hasBounds)
            {
                bounds = GetOverviewPlaymatInnerRect();
                hasBounds = true;
            }

            return hasBounds;
        }

        Rect GetOverviewSceneRect()
        {
            Vector2 sceneCenter = GetOverviewSceneCenter();
            Vector2 sceneSize = GetOverviewSceneAreaSize();
            return Rect.MinMaxRect(
                sceneCenter.x - sceneSize.x * 0.5f,
                sceneCenter.y - sceneSize.y * 0.5f,
                sceneCenter.x + sceneSize.x * 0.5f,
                sceneCenter.y + sceneSize.y * 0.5f);
        }

        void EncapsulateRectTransformBoundsInContent(RectTransform rect, ref Rect bounds, ref bool hasBounds)
        {
            if (rect == null || content == null || !rect.gameObject.activeInHierarchy) return;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = content.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxX = Mathf.Max(maxX, local.x);
                maxY = Mathf.Max(maxY, local.y);
            }

            if (float.IsInfinity(minX) || float.IsInfinity(minY) || float.IsInfinity(maxX) || float.IsInfinity(maxY)) return;

            EncapsulateRect(Rect.MinMaxRect(minX, minY, maxX, maxY), ref bounds, ref hasBounds);
        }

        void EncapsulateRect(Rect rect, ref Rect bounds, ref bool hasBounds)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;

            if (!hasBounds)
            {
                bounds = rect;
                hasBounds = true;
                return;
            }

            bounds = Rect.MinMaxRect(
                Mathf.Min(bounds.xMin, rect.xMin),
                Mathf.Min(bounds.yMin, rect.yMin),
                Mathf.Max(bounds.xMax, rect.xMax),
                Mathf.Max(bounds.yMax, rect.yMax));
        }

        bool TryGetRectVerticalBoundsInContent(RectTransform rect, out float minY, out float maxY)
        {
            minY = 0f;
            maxY = 0f;
            if (rect == null || content == null) return false;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                float localY = content.InverseTransformPoint(corners[i]).y;
                minY = Mathf.Min(minY, localY);
                maxY = Mathf.Max(maxY, localY);
            }

            return !float.IsInfinity(minY) && !float.IsInfinity(maxY);
        }

        float GetLanePitchForView(int laneIndex)
        {
            if (_lanes.Count > 1)
            {
                int neighborIndex = laneIndex <= 0 ? 1 : laneIndex - 1;
                if (neighborIndex >= 0 && neighborIndex < _lanes.Count)
                {
                    float pitch = Mathf.Abs(GetLaneCenterForView(laneIndex) - GetLaneCenterForView(neighborIndex));
                    if (pitch > 1f) return pitch;
                }
            }

            return Mathf.Max(GetLaneStep(), GetOverviewLaneFootprintWidth() + Mathf.Max(0f, OverviewRotatedCardClearance));
        }

        float GetViewTransformRotatedFootprintWidth(int laneIndex)
        {
            UcgBattleLane lane = laneIndex >= 0 && laneIndex < _lanes.Count ? _lanes[laneIndex] : null;
            float slotHeight = 0f;
            if (lane != null)
            {
                if (lane.playerSlot != null) slotHeight = Mathf.Max(slotHeight, lane.playerSlot.sizeDelta.y);
                if (lane.opponentSlot != null) slotHeight = Mathf.Max(slotHeight, lane.opponentSlot.sizeDelta.y);
            }

            return Mathf.Max(slotHeight, GetOverviewLaneFootprintWidth());
        }

        float GetFocusLaneViewportRatio(int laneIndex)
        {
            if (laneIndex <= 0) return 0.45f;
            if (laneIndex >= _lanes.Count - 1) return 0.55f;
            return 0.5f;
        }

        struct ViewTransformOnlyLayoutSnapshot
        {
            public Vector2[] lanePositions;
            public Vector3[] laneScales;
            public Vector2[] opponentSlotPositions;
            public Vector2[] playerSlotPositions;
            public Vector2[] opponentSlotSizes;
            public Vector2[] playerSlotSizes;
        }

        ViewTransformOnlyLayoutSnapshot CaptureViewTransformOnlyLayoutSnapshot()
        {
            int count = _lanes.Count;
            var snapshot = new ViewTransformOnlyLayoutSnapshot
            {
                lanePositions = new Vector2[count],
                laneScales = new Vector3[count],
                opponentSlotPositions = new Vector2[count],
                playerSlotPositions = new Vector2[count],
                opponentSlotSizes = new Vector2[count],
                playerSlotSizes = new Vector2[count]
            };

            for (int i = 0; i < count; i++)
            {
                UcgBattleLane lane = _lanes[i];
                RectTransform laneRect = lane != null ? lane.transform as RectTransform : null;
                snapshot.lanePositions[i] = laneRect != null ? laneRect.anchoredPosition : Vector2.zero;
                snapshot.laneScales[i] = laneRect != null ? laneRect.localScale : Vector3.one;
                snapshot.opponentSlotPositions[i] = lane != null && lane.opponentSlot != null ? lane.opponentSlot.anchoredPosition : Vector2.zero;
                snapshot.playerSlotPositions[i] = lane != null && lane.playerSlot != null ? lane.playerSlot.anchoredPosition : Vector2.zero;
                snapshot.opponentSlotSizes[i] = lane != null && lane.opponentSlot != null ? lane.opponentSlot.sizeDelta : Vector2.zero;
                snapshot.playerSlotSizes[i] = lane != null && lane.playerSlot != null ? lane.playerSlot.sizeDelta : Vector2.zero;
            }

            return snapshot;
        }

        void AssertViewTransformOnlyDidNotMutateBattleLayout(ViewTransformOnlyLayoutSnapshot snapshot)
        {
            if (!debugBattlefieldLayout || snapshot.lanePositions == null) return;

            for (int i = 0; i < _lanes.Count && i < snapshot.lanePositions.Length; i++)
            {
                UcgBattleLane lane = _lanes[i];
                RectTransform laneRect = lane != null ? lane.transform as RectTransform : null;
                if (laneRect != null
                    && (Vector2.SqrMagnitude(laneRect.anchoredPosition - snapshot.lanePositions[i]) > 0.01f
                        || (laneRect.localScale - snapshot.laneScales[i]).sqrMagnitude > 0.0001f))
                {
                    Debug.LogWarning($"ViewTransformOnly mutated Lane {i + 1} layout.");
                }

                if (lane != null && lane.opponentSlot != null
                    && (Vector2.SqrMagnitude(lane.opponentSlot.anchoredPosition - snapshot.opponentSlotPositions[i]) > 0.01f
                        || Vector2.SqrMagnitude(lane.opponentSlot.sizeDelta - snapshot.opponentSlotSizes[i]) > 0.01f))
                {
                    Debug.LogWarning($"ViewTransformOnly mutated Lane {i + 1} opponent slot layout.");
                }

                if (lane != null && lane.playerSlot != null
                    && (Vector2.SqrMagnitude(lane.playerSlot.anchoredPosition - snapshot.playerSlotPositions[i]) > 0.01f
                        || Vector2.SqrMagnitude(lane.playerSlot.sizeDelta - snapshot.playerSlotSizes[i]) > 0.01f))
                {
                    Debug.LogWarning($"ViewTransformOnly mutated Lane {i + 1} player slot layout.");
                }
            }
        }

        Vector2 GetTargetContentPosition(float targetX, float targetScale)
        {
            float clampedScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            return new Vector2(clampedTargetX + combatAreaOffsetX, 0f);
        }

        void LogBattlefieldLayoutAnimation(string source, UcgBattlefieldViewMode viewMode, bool smooth, Vector2 beforePosition, Vector2 targetPosition)
        {
            if (!debugBattlefieldLayout && !debugBattlefieldScroll) return;

            bool positionChanged = Vector2.Distance(beforePosition, targetPosition) > 0.5f;
            if (!smooth && !positionChanged) return;

            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            int turn = turnManager != null ? turnManager.currentTurn : 0;
            bool laneRootActive = content != null && content.gameObject.activeInHierarchy;

            Debug.Log(
                "Battlefield layout animation:\n"
                + $"source={source}\n"
                + $"reason={(smooth ? "SmoothViewRequested" : "InstantLayoutRefresh")}\n"
                + $"viewMode={viewMode}\n"
                + $"phase={phaseText}\n"
                + $"turn={turn}\n"
                + $"isInitialSetup={!hasInitializedBattlefieldView}\n"
                + $"hasPlayedCombatIntro={hasInitializedBattlefieldView}\n"
                + $"combatRootPosBefore=({beforePosition.x:0.#},{beforePosition.y:0.#})\n"
                + $"combatRootPosAfter=({targetPosition.x:0.#},{targetPosition.y:0.#})\n"
                + $"laneRootActive={laneRootActive}");
        }

        void StopActiveLaneScroll()
        {
            if (_activeLaneScrollCoroutine == null) return;

            StopCoroutine(_activeLaneScrollCoroutine);
            _activeLaneScrollCoroutine = null;
        }

        float GetFocusLaneTargetX(int laneIndex)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float laneCenter = GetLaneLeftX(laneIndex) + laneSize.x * 0.5f;
            float rtlFocusViewportPosition = 1f - Mathf.Clamp01(focusViewportPosition);
            float targetX = viewportWidth * rtlFocusViewportPosition - laneCenter;
            if (debugBattlefieldScroll)
            {
                Debug.Log($"ViewMode=FocusLane, currentTurn={(turnManager != null ? turnManager.currentTurn : 1)}, activeLane={laneIndex + 1}, targetX={targetX}, openedLaneCount={(turnManager != null ? GetOpenedLaneCount(turnManager.currentTurn) : 1)}");
            }
            return targetX;
        }

        public int GetOverviewTargetLaneCount(int currentTurn)
        {
            return Mathf.Max(1, maxLaneCount);
        }

        float GetOverviewScaleForLaneCount(int laneCount)
        {
            return Mathf.Clamp(overviewScale, 0.1f, 1f);
        }

        float GetOverviewTargetX(float scale, int laneCount)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            int clampedLaneCount = Mathf.Clamp(laneCount, 1, _lanes.Count > 0 ? _lanes.Count : maxLaneCount);
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect laneAreaRect = GetOverviewLaneAreaRect(playmatInnerRect);
            float visualScale = GetOverviewLayoutVisualScale(scale, playmatInnerRect, laneAreaRect);
            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;

            for (int i = 0; i < clampedLaneCount; i++)
            {
                float centerX = GetOverviewLaneCenterX(i, playmatInnerRect, laneAreaRect, visualScale);
                float left = centerX - laneSize.x * visualScale * 0.5f;
                float right = left + laneSize.x * visualScale;
                groupLeft = Mathf.Min(groupLeft, left);
                groupRight = Mathf.Max(groupRight, right);
            }

            if (groupLeft == float.MaxValue)
            {
                groupLeft = 0f;
                groupRight = laneSize.x;
            }

            Rect rightRailRect = GetOverviewRightRailRect(playmatInnerRect);
            float overviewRightEdge = Mathf.Max(groupRight, rightRailRect.xMax);
            float targetX = viewportWidth - OverviewViewportRightPadding - overviewRightEdge * Mathf.Max(0.1f, scale);
            targetX -= Mathf.Max(0f, overviewContentLeftShift) * GetOverviewBlend(scale);
            if (debugBattlefieldScroll)
            {
                Debug.Log($"ViewMode=OverviewAll, currentTurn={(turnManager != null ? turnManager.currentTurn : 1)}, activeLane={(turnManager != null ? turnManager.ActiveNewLaneIndex + 1 : 1)}, targetX={targetX}, openedLaneCount={GetOpenedLaneCount(turnManager != null ? turnManager.currentTurn : 1)}, overviewLaneCount={clampedLaneCount}");
            }
            return targetX;
        }

        public float GetOverviewLayoutBlend(float scale)
        {
            if (_currentViewMode != UcgBattlefieldViewMode.OverviewAll) return 0f;

            float farScale = Mathf.Clamp(overviewScale, 0.1f, 1f);
            float denominator = Mathf.Max(0.001f, 1f - farScale);
            return Mathf.Clamp01((1f - Mathf.Clamp(scale, farScale, 1f)) / denominator);
        }

        float GetOverviewBlend(float scale)
        {
            return GetOverviewLayoutBlend(scale);
        }

        float GetOverviewVisualCompensation(float scale)
        {
            return Mathf.Lerp(1f, Mathf.Max(1f, overviewVisualCompensationScale), GetOverviewBlend(scale));
        }

        void ApplyOverviewVisualCompensation(float scale)
        {
            if (GetOverviewBlend(scale) > 0f)
            {
                ApplyOverviewLayout(scale);
                return;
            }

            float visualScale = GetOverviewVisualCompensation(scale);
            for (int i = 0; i < _lanes.Count; i++)
            {
                RectTransform laneRect = _lanes[i] != null ? _lanes[i].transform as RectTransform : null;
                if (laneRect == null) continue;

                laneRect.anchoredPosition = new Vector2(GetLaneLeftXForScale(i, scale), 0f);
                laneRect.localScale = new Vector3(visualScale, visualScale, 1f);
                _lanes[i].RestoreReferenceSlotLayout();
            }
        }

        void ApplyOverviewLayout(float scale)
        {
            if (content == null || viewport == null) return;

            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect laneAreaRect = GetOverviewLaneAreaRect(playmatInnerRect);
            float visualScale = GetOverviewLayoutVisualScale(scale, playmatInnerRect, laneAreaRect);
            for (int i = 0; i < _lanes.Count; i++)
            {
                UcgBattleLane lane = _lanes[i];
                RectTransform laneRect = lane != null ? lane.transform as RectTransform : null;
                if (laneRect == null) continue;

                laneRect.localScale = new Vector3(visualScale, visualScale, 1f);
                laneRect.anchoredPosition = GetOverviewLanePosition(i, laneAreaRect, scale, visualScale);
                lane.ApplyOverviewSlotLayout(
                    GetOverviewOpponentSlotPosition(playmatInnerRect, scale, visualScale),
                    GetOverviewPlayerSlotPosition(playmatInnerRect, scale, visualScale),
                    GetOverviewStandardCardSize(1f));
            }
            LogAllLaneAnchoredPositions("ApplyOverviewLayout:Completed");
        }

        public Rect GetOverviewPlaymatSafeArea()
        {
            return GetOverviewLaneAreaRect(GetOverviewPlaymatInnerRect());
        }

        public Rect GetOverviewPlaymatInnerRect()
        {
            float contentWidth = content != null && content.rect.width > 0f ? content.rect.width : GetContentWidth(maxLaneCount);
            float contentHeight = content != null && content.rect.height > 0f ? content.rect.height : laneSize.y;
            float outerLeft = Mathf.Clamp(overviewSafeLeftPadding, 0f, contentWidth * 0.45f);
            float outerRight = Mathf.Max(outerLeft + 1f, contentWidth - Mathf.Max(0f, overviewSafeRightPadding));
            float outerTop = contentHeight * 0.5f - Mathf.Max(0f, overviewSafeTopPadding);
            float outerBottom = -contentHeight * 0.5f + Mathf.Max(0f, overviewSafeBottomPadding);
            if (outerBottom > outerTop - 1f) outerBottom = outerTop - 1f;

            Rect outerPlaymat = Rect.MinMaxRect(outerLeft, outerBottom, outerRight, outerTop);
            float left = Mathf.Lerp(outerPlaymat.xMin, outerPlaymat.xMax, OverviewPlaymatInnerLeftRatio);
            float right = Mathf.Lerp(outerPlaymat.xMin, outerPlaymat.xMax, OverviewPlaymatInnerRightRatio);
            float bottom = Mathf.Lerp(outerPlaymat.yMin, outerPlaymat.yMax, OverviewPlaymatInnerBottomRatio);
            float top = Mathf.Lerp(outerPlaymat.yMin, outerPlaymat.yMax, OverviewPlaymatInnerTopRatio);
            if (bottom > top - 1f) bottom = top - 1f;
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        Rect GetOverviewLaneAreaRect(Rect playmatInnerRect)
        {
            float left = Mathf.Lerp(playmatInnerRect.xMin, playmatInnerRect.xMax, OverviewLaneAreaLeftRatio);
            float right = Mathf.Lerp(playmatInnerRect.xMin, playmatInnerRect.xMax, OverviewLaneAreaRightRatio);
            return Rect.MinMaxRect(left, playmatInnerRect.yMin, Mathf.Max(left + 1f, right), playmatInnerRect.yMax);
        }

        public Rect GetOverviewRightRailRect()
        {
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            return GetOverviewRightRailRect(playmatInnerRect);
        }

        public Vector2 GetOverviewCardSize()
        {
            return GetOverviewStandardCardSize(1f);
        }

        public Vector2 GetOverviewNormalCardSize()
        {
            return GetOverviewActualBattleCardSize(out _);
        }

        public Vector2 GetOverviewSceneAreaSize()
        {
            Vector2 cardSize = GetOverviewNormalCardSize();
            return new Vector2(cardSize.y, cardSize.x);
        }

        public Vector2 GetOverviewSceneCenter()
        {
            return GetOverviewSceneDesignAnchor();
        }

        public string GetOverviewSceneCardSizeSource()
        {
            GetOverviewActualBattleCardSize(out string source);
            return source + ", rotated 90 degrees";
        }

        Vector2 GetOverviewActualBattleCardSize(out string source)
        {
            Vector2 deckSize = GetRegisteredSingleCardSize(IsDeckSafeRect, out string deckSource);
            if (IsValidCardSize(deckSize))
            {
                source = deckSource;
                return deckSize;
            }

            Vector2 trashSize = GetRegisteredSingleCardSize(IsTrashSafeRect, out string trashSource);
            if (IsValidCardSize(trashSize))
            {
                source = trashSource;
                return trashSize;
            }

            Vector2 playerRowSize = GetRowSingleCardSize(false, out string playerRowSource);
            if (IsValidCardSize(playerRowSize))
            {
                source = playerRowSource;
                return playerRowSize;
            }

            Vector2 opponentRowSize = GetRowSingleCardSize(true, out string opponentRowSource);
            if (IsValidCardSize(opponentRowSize))
            {
                source = opponentRowSource;
                return opponentRowSize;
            }

            source = "fallback GetOverviewStandardCardSize(1f); no registered actual battle card bounds available";
            return GetOverviewStandardCardSize(1f);
        }

        Vector2 GetRegisteredSingleCardSize(System.Func<RectTransform, bool> predicate, out string source)
        {
            source = "<none>";
            for (int i = 0; i < _focusViewSafeRects.Count; i++)
            {
                RectTransform rect = _focusViewSafeRects[i];
                if (rect == null || predicate == null || !predicate(rect)) continue;

                Rect bounds = new Rect();
                bool hasBounds = false;
                EncapsulateRectTransformBoundsInContent(rect, ref bounds, ref hasBounds);
                if (!hasBounds || bounds.width <= 1f || bounds.height <= 1f) continue;

                source = $"{rect.name} registered bounds";
                return new Vector2(bounds.width, bounds.height);
            }

            return Vector2.zero;
        }

        Vector2 GetRowSingleCardSize(bool opponent, out string source)
        {
            source = "<none>";
            for (int i = 0; i < _lanes.Count; i++)
            {
                UcgBattleLane lane = _lanes[i];
                RectTransform slot = lane != null
                    ? opponent ? lane.opponentSlot : lane.playerSlot
                    : null;
                if (slot == null) continue;

                Rect bounds = new Rect();
                bool hasBounds = false;
                EncapsulateRectTransformBoundsInContent(slot, ref bounds, ref hasBounds);
                if (!hasBounds || bounds.height <= 1f) continue;

                float width = bounds.width > 1f ? bounds.width : bounds.height * OverviewPortraitCardAspect;
                source = $"{slot.name} actual row slot bounds";
                return new Vector2(width, bounds.height);
            }

            return Vector2.zero;
        }

        bool IsValidCardSize(Vector2 size)
        {
            return size.x > 1f && size.y > 1f;
        }

        Vector2 GetOverviewSceneDesignAnchor()
        {
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect laneAreaRect = GetOverviewLaneAreaRect(playmatInnerRect);
            float visualScale = GetOverviewLayoutVisualScale(overviewScale, playmatInnerRect, laneAreaRect);
            float lane01Center = GetOverviewLaneCenterX(0, playmatInnerRect, laneAreaRect, visualScale);
            float lane02Center = GetOverviewLaneCenterX(1, playmatInnerRect, laneAreaRect, visualScale);
            return new Vector2((lane01Center + lane02Center) * 0.5f, GetOverviewPlaymatCenterY(playmatInnerRect));
        }

        Rect GetOverviewRightRailRect(Rect playmatInnerRect)
        {
            float left = Mathf.Lerp(playmatInnerRect.xMin, playmatInnerRect.xMax, OverviewRightRailLeftRatio);
            float right = Mathf.Lerp(playmatInnerRect.xMin, playmatInnerRect.xMax, OverviewRightRailRightRatio);
            return Rect.MinMaxRect(left, playmatInnerRect.yMin, Mathf.Max(left + 1f, right), playmatInnerRect.yMax);
        }

        Vector2 GetOverviewLanePosition(int laneIndex, Rect laneAreaRect, float contentScale, float visualScale)
        {
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            float centerX = GetOverviewLaneCenterX(laneIndex, playmatInnerRect, laneAreaRect, visualScale);
            float laneLeft = centerX - laneSize.x * Mathf.Max(0.1f, visualScale) * 0.5f;
            return new Vector2(laneLeft, GetOverviewPlaymatCenterY(playmatInnerRect));
        }

        float GetOverviewLaneCenterX(int laneIndex, Rect playmatInnerRect, Rect laneAreaRect, float visualScale)
        {
            float t = Mathf.Clamp01(laneIndex / Mathf.Max(1f, OverviewLaneLayoutCount - 1f));
            GetOverviewLaneCenterRange(playmatInnerRect, laneAreaRect, visualScale, out float leftCenter, out float rightCenter);
            return Mathf.Lerp(rightCenter, leftCenter, t);
        }

        public float GetOverviewLayoutVisualScale(float scale)
        {
            Rect playmatInnerRect = GetOverviewPlaymatInnerRect();
            Rect laneAreaRect = GetOverviewLaneAreaRect(playmatInnerRect);
            return GetOverviewLayoutVisualScale(scale, playmatInnerRect, laneAreaRect);
        }

        float GetOverviewLayoutVisualScale(float scale, Rect playmatInnerRect, Rect laneAreaRect)
        {
            return GetOverviewVisualCompensation(scale);
        }

        void GetOverviewLaneCenterRange(Rect playmatInnerRect, Rect laneAreaRect, float visualScale, out float leftCenter, out float rightCenter)
        {
            Rect rightRailRect = GetOverviewRightRailRect(playmatInnerRect);
            float visualFootprint = GetOverviewLaneFootprintWidth() * Mathf.Max(0.1f, visualScale);
            float lanePitch = GetOverviewLanePitch(visualScale);
            float rightFootprintHalf = visualFootprint * 0.5f;
            float laneAreaMinCenter = Mathf.Max(playmatInnerRect.xMin, laneAreaRect.xMin) + rightFootprintHalf + OverviewLaneAreaLeftPadding;
            rightCenter = Mathf.Min(laneAreaRect.xMax, rightRailRect.xMin - OverviewLaneRightRailGap) - rightFootprintHalf;
            leftCenter = rightCenter - lanePitch * Mathf.Max(0f, OverviewLaneLayoutCount - 1f);
        }

        float GetOverviewLanePitch(float visualScale)
        {
            float rotatedFootprintWidth = GetOverviewLaneFootprintWidth() * Mathf.Max(0.1f, visualScale);
            return rotatedFootprintWidth + Mathf.Max(0f, OverviewRotatedCardClearance);
        }

        float GetOverviewLaneFootprintWidth()
        {
            return Mathf.Max(1f, GetOverviewStandardCardSize(1f).y);
        }

        Vector2 GetOverviewOpponentSlotPosition(Rect playmatInnerRect, float contentScale, float visualScale)
        {
            float slotY = GetOverviewSlotLocalRowHalfSpan(playmatInnerRect, visualScale);
            return new Vector2(laneSize.x * 0.5f, slotY);
        }

        Vector2 GetOverviewPlayerSlotPosition(Rect playmatInnerRect, float contentScale, float visualScale)
        {
            float slotY = -GetOverviewSlotLocalRowHalfSpan(playmatInnerRect, visualScale);
            return new Vector2(laneSize.x * 0.5f, slotY);
        }

        float GetOverviewSlotLocalRowHalfSpan(Rect playmatInnerRect, float visualScale)
        {
            float scale = Mathf.Max(0.1f, visualScale);
            float playmatCenterY = GetOverviewPlaymatCenterY(playmatInnerRect);
            float playerRowY = GetOverviewPlayerRowY(playmatInnerRect, visualScale);
            float currentHalfDistance = Mathf.Abs(playmatCenterY - playerRowY);
            float cardHalfHeight = GetOverviewStandardCardSize(visualScale).y * 0.5f;
            float sceneHalfHeight = GetOverviewSceneAreaSize().y * scale * 0.5f;
            float minHalfDistance = sceneHalfHeight + OverviewSceneRowGap + cardHalfHeight;
            float tightenedHalfDistance = currentHalfDistance * Mathf.Clamp01(OverviewRowCenterlineTighten);
            return Mathf.Max(minHalfDistance, tightenedHalfDistance) / scale;
        }

        float GetOverviewPlaymatCenterY(Rect playmatInnerRect)
        {
            return (playmatInnerRect.yMin + playmatInnerRect.yMax) * 0.5f;
        }

        float GetOverviewOpponentRowY(Rect playmatInnerRect, float visualScale)
        {
            float slotHalfHeight = GetOverviewStandardCardSize(visualScale).y * 0.5f;
            return playmatInnerRect.yMax - GetOverviewVerticalEdgeMargin(playmatInnerRect) - slotHalfHeight;
        }

        float GetOverviewPlayerRowY(Rect playmatInnerRect, float visualScale)
        {
            float slotHalfHeight = GetOverviewStandardCardSize(visualScale).y * 0.5f;
            return playmatInnerRect.yMin + GetOverviewVerticalEdgeMargin(playmatInnerRect) + slotHalfHeight;
        }

        float GetOverviewVerticalEdgeMargin(Rect playmatInnerRect)
        {
            return Mathf.Min(
                Mathf.Max(0f, OverviewVerticalEdgeMargin),
                Mathf.Max(0f, playmatInnerRect.height * 0.12f));
        }

        Vector2 GetOverviewStandardCardSize(float visualScale)
        {
            float scale = Mathf.Max(0.1f, visualScale);
            float height = Mathf.Max(playerSlotSize.y, opponentSlotSize.y, placedCardSize.y, opponentCardSize.y);
            float width = height * OverviewPortraitCardAspect;
            return new Vector2(width, height) * scale;
        }

        float GetOverviewRowY(Rect safeArea, float rowRatio)
        {
            return safeArea.yMax - safeArea.height * Mathf.Clamp01(rowRatio);
        }

        Vector2 GetOverviewBattleCardSize()
        {
            float scale = Mathf.Clamp(overviewScale, 0.1f, 1f) * GetOverviewLayoutVisualScale(overviewScale);
            return GetOverviewStandardCardSize(1f) * scale;
        }

        Vector2 GetOverviewSlotSize()
        {
            float scale = Mathf.Clamp(overviewScale, 0.1f, 1f) * GetOverviewLayoutVisualScale(overviewScale);
            return GetOverviewStandardCardSize(1f) * scale;
        }

        float ScreenXToContentLocal(float screenX, float contentScale)
        {
            float contentX = content != null ? content.anchoredPosition.x : 0f;
            return (screenX - contentX) / Mathf.Max(0.1f, contentScale);
        }

        bool IsLaneFullyVisible(int laneIndex, float scrollX)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float laneLeft = GetLaneLeftX(laneIndex);
            float laneRight = laneLeft + laneSize.x;
            float viewLeft = Mathf.Clamp(scrollX, 0f, GetMaxScrollX());
            float viewRight = viewLeft + viewportWidth;

            return laneLeft >= viewLeft && laneRight <= viewRight;
        }

        bool IsAnyCardDragging()
        {
            UcgCardView[] cardViews = FindObjectsByType<UcgCardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < cardViews.Length; i++)
            {
                if (cardViews[i] != null && cardViews[i].IsDragging)
                {
                    return true;
                }
            }

            return false;
        }

        float GetContentWidth(int laneCount)
        {
            if (laneCount <= 0) return 0f;
            float referenceWidth = laneSize.x * laneCount
                + laneSpacing * Mathf.Max(0, laneCount - 1)
                + Mathf.Max(0f, rightAuxiliaryColumnGutterWidth);
            return Mathf.Max(referenceWidth, GetOverviewRequiredContentWidth(laneCount));
        }

        float GetOverviewRequiredContentWidth(int laneCount)
        {
            int clampedLaneCount = Mathf.Max(1, laneCount);
            float overviewVisualScale = Mathf.Max(1f, overviewVisualCompensationScale);
            float visualFootprint = GetOverviewLaneFootprintWidth() * overviewVisualScale;
            float lanePitch = visualFootprint + Mathf.Max(0f, OverviewRotatedCardClearance);
            float laneSpan = visualFootprint + lanePitch * Mathf.Max(0, clampedLaneCount - 1);
            float rightRailCardWidth = GetOverviewStandardCardSize(overviewVisualScale).x + OverviewRightRailCardPadding * 2f;
            float laneAreaRatio = Mathf.Max(0.01f, OverviewLaneAreaRightRatio - OverviewLaneAreaLeftRatio);
            float requiredLaneAreaWidth = laneSpan + OverviewLaneAreaLeftPadding;
            float requiredRailClearanceWidth = OverviewLaneRightRailGap
                / Mathf.Max(0.01f, OverviewRightRailLeftRatio - OverviewLaneAreaRightRatio);
            float requiredRightRailWidth = rightRailCardWidth
                / Mathf.Max(0.01f, OverviewRightRailRightRatio - OverviewRightRailLeftRatio);
            float requiredInnerWidth = Mathf.Max(
                requiredLaneAreaWidth / laneAreaRatio,
                requiredRailClearanceWidth,
                requiredRightRailWidth);
            float innerRatio = Mathf.Max(0.01f, OverviewPlaymatInnerRightRatio - OverviewPlaymatInnerLeftRatio);
            return requiredInnerWidth / innerRatio
                + Mathf.Max(0f, overviewSafeLeftPadding)
                + Mathf.Max(0f, overviewSafeRightPadding);
        }

        float GetContentHeight(float contentWidth)
        {
            return Mathf.Max(laneSize.y, contentWidth * OverviewPlaymatWorldAspect);
        }

        void ApplyLaneRect(RectTransform laneRect, int index)
        {
            if (laneRect == null) return;

            laneRect.anchorMin = new Vector2(0f, 0.5f);
            laneRect.anchorMax = new Vector2(0f, 0.5f);
            laneRect.pivot = new Vector2(0f, 0.5f);
            laneRect.sizeDelta = laneSize;
            laneRect.anchoredPosition = GetLanePosition(index);
            LogLaneRectState("ApplyLaneRect", index, laneRect);
        }

        public void LogLane1AndContentState(string stage)
        {
            RectTransform laneRect = _lanes.Count > 0 && _lanes[0] != null
                ? _lanes[0].transform as RectTransform
                : null;
            LogLaneContentState(stage, 0, laneRect);
        }

        void LogLaneRectState(string stage, int laneIndex, RectTransform laneRect)
        {
            LogLaneContentState(stage, laneIndex, laneRect);
        }

        void LogAllLaneAnchoredPositions(string stage)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("[UCG LanePositionTrace]\n");
            builder.Append("stage=").Append(stage).Append('\n');
            builder.Append("contentPos=").Append(FormatVector2(content != null ? content.anchoredPosition : Vector2.zero)).Append('\n');
            builder.Append("contentScale=").Append(content != null ? content.localScale.ToString("F3") : "<none>").Append('\n');
            for (int i = 0; i < _lanes.Count; i++)
            {
                RectTransform laneRect = _lanes[i] != null ? _lanes[i].transform as RectTransform : null;
                builder.Append("laneIndex=").Append(i)
                    .Append(", name=").Append(laneRect != null ? laneRect.name : "<none>")
                    .Append(", anchored=").Append(FormatVector2(laneRect != null ? laneRect.anchoredPosition : Vector2.zero))
                    .Append(", parent=").Append(laneRect != null && laneRect.parent != null ? laneRect.parent.name : "<none>")
                    .Append('\n');
            }
            Debug.Log(builder.ToString());
        }

        void LogLaneContentState(string stage, int laneIndex, RectTransform laneRect)
        {
            RectTransform parentRect = laneRect != null ? laneRect.parent as RectTransform : null;
            Debug.Log(
                "[UCG LanePositionTrace]\n"
                + $"stage={stage}\n"
                + $"laneIndex={laneIndex}\n"
                + $"laneName={(laneRect != null ? laneRect.name : "<none>")}\n"
                + $"laneAnchored={(laneRect != null ? FormatVector2(laneRect.anchoredPosition) : "<none>")}\n"
                + $"laneSize={(laneRect != null ? FormatVector2(laneRect.sizeDelta) : "<none>")}\n"
                + $"laneScale={(laneRect != null ? laneRect.localScale.ToString("F3") : "<none>")}\n"
                + $"parentName={(parentRect != null ? parentRect.name : "<none>")}\n"
                + $"parentAnchored={(parentRect != null ? FormatVector2(parentRect.anchoredPosition) : "<none>")}\n"
                + $"parentScale={(parentRect != null ? parentRect.localScale.ToString("F3") : "<none>")}\n"
                + $"contentAnchored={(content != null ? FormatVector2(content.anchoredPosition) : "<none>")}\n"
                + $"contentScale={(content != null ? content.localScale.ToString("F3") : "<none>")}");
        }

        void LogScrollDebugState(string context)
        {
            if (!debugBattlefieldScroll) return;
            if (viewport == null || content == null) return;

            RectTransform lane1 = _lanes.Count > 0 ? _lanes[0].transform as RectTransform : null;
            RectTransform lane2 = _lanes.Count > 1 ? _lanes[1].transform as RectTransform : null;
            RectTransform lane3 = _lanes.Count > 2 ? _lanes[2].transform as RectTransform : null;
            float normalizedPosition = scrollRect != null ? scrollRect.horizontalNormalizedPosition : 0f;

            Debug.Log(
                "[UCG Battlefield Scroll Debug] " + context + "\n" +
                "Viewport " + FormatRectDebug(viewport) + "\n" +
                "Content " + FormatRectDebug(content) + "\n" +
                "Lane 1 " + FormatRectDebug(lane1) + "\n" +
                "Lane 2 anchoredPosition.x=" + FormatFloat(lane2 != null ? lane2.anchoredPosition.x : 0f) + "\n" +
                "Lane 3 anchoredPosition.x=" + FormatFloat(lane3 != null ? lane3.anchoredPosition.x : 0f) + "\n" +
                "ScrollRect.horizontalNormalizedPosition=" + FormatFloat(normalizedPosition) + "\n" +
                "combatAreaOffsetX=" + FormatFloat(combatAreaOffsetX) + "\n" +
                "rightAuxiliaryColumnGutterWidth=" + FormatFloat(rightAuxiliaryColumnGutterWidth) + "\n" +
                "Content.anchoredPosition.x sign=" + GetXSign(content.anchoredPosition.x));
        }

        string FormatRectDebug(RectTransform rect)
        {
            if (rect == null) return "(null)";
            return
                "anchorMin=" + FormatVector2(rect.anchorMin) +
                ", anchorMax=" + FormatVector2(rect.anchorMax) +
                ", pivot=" + FormatVector2(rect.pivot) +
                ", sizeDelta=" + FormatVector2(rect.sizeDelta) +
                ", anchoredPosition=" + FormatVector2(rect.anchoredPosition) +
                ", localPosition=" + FormatVector3(rect.localPosition) +
                ", rect.width=" + FormatFloat(rect.rect.width);
        }

        string FormatVector2(Vector2 value)
        {
            return "(" + FormatFloat(value.x) + ", " + FormatFloat(value.y) + ")";
        }

        string FormatVector3(Vector3 value)
        {
            return "(" + FormatFloat(value.x) + ", " + FormatFloat(value.y) + ", " + FormatFloat(value.z) + ")";
        }

        string FormatRect(Rect value)
        {
            return $"(xMin={FormatFloat(value.xMin)}, yMin={FormatFloat(value.yMin)}, xMax={FormatFloat(value.xMax)}, yMax={FormatFloat(value.yMax)}, w={FormatFloat(value.width)}, h={FormatFloat(value.height)})";
        }

        string FormatFloat(float value)
        {
            return value.ToString("0.##");
        }

        string GetXSign(float value)
        {
            if (value > 0.01f) return "positive";
            if (value < -0.01f) return "negative";
            return "zero";
        }

        UcgCardData CreateFixedOpponentCardData(int laneIndex)
        {
            switch (laneIndex)
            {
                case 1:
                    return CreateOpponentCard("opponent-lane-2", "對手測試 Lv.1 強", "對手角色B", 9000, 11000, 13000, 15000);
                case 2:
                    return CreateOpponentCard("opponent-lane-3", "對手測試 Lv.1 平局用", "對手角色C", 4000, 7000, 9000, 11000);
                default:
                    return CreateOpponentCard("opponent-lane-1", "對手測試 Lv.1 弱", "對手角色A", 3000, 6000, 9000, 11000);
            }
        }

        UcgCardData CreateOpponentCard(string id, string cardName, string characterName, int singleBp, int doubleBp, int tripleBp, int quadBp)
        {
            return new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = characterName,
                cardCategory = "超人力霸王",
                level = 1,
                teamTag = "",
                singleBp = singleBp,
                doubleBp = doubleBp,
                tripleBp = tripleBp,
                quadBp = quadBp,
            };
        }
    }
}
