using System.Collections;
using System.Collections.Generic;
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

        public Vector2 placedCardSize = new Vector2(190f, 276f);
        public Vector2 opponentCardSize = new Vector2(172f, 250f);
        public Vector2 laneSize = new Vector2(300f, 660f);
        public Vector2 playerSlotSize = new Vector2(228f, 332f);
        public Vector2 opponentSlotSize = new Vector2(220f, 286f);
        public float laneSpacing = 30f;
        public float activeLaneScrollDuration = 0.32f;
        public float overviewScale = 0.4f;
        public float focusViewportPosition = 0.42f;
        public float combatAreaOffsetX;
        public float rightAuxiliaryColumnGutterWidth;
        public bool debugBattlefieldLayout;
        public bool hasInitializedBattlefieldView;

        readonly List<UcgBattleLane> _lanes = new List<UcgBattleLane>();
        Coroutine _activeLaneScrollCoroutine;
        UcgBattlefieldViewMode _currentViewMode = UcgBattlefieldViewMode.FocusLane;

        public List<UcgBattleLane> Lanes => _lanes;

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
            content.sizeDelta = new Vector2(contentWidth, laneSize.y);

            for (int i = 0; i < laneCount; i++)
            {
                var laneObject = new GameObject($"Lane {i + 1}", typeof(RectTransform), typeof(UcgBattleLane));
                laneObject.transform.SetParent(lanesRoot, false);

                var laneRect = laneObject.GetComponent<RectTransform>();
                ApplyLaneRect(laneRect, i);

                var lane = laneObject.GetComponent<UcgBattleLane>();
                lane.Initialize(i, uiFont, resultText, tutorialGuide, turnManager, phaseManager, playerSlotSize, opponentSlotSize, placedCardSize);
                ApplyLaneRect(laneRect, i);
                lane.ConfigureOpponentScript(opponentScript, opponentTestMode);
                lane.ConfigureFixedOpponentCard(CreateFixedOpponentCardData(i), opponentCardSprite, cardInfoPanel, opponentCardSize);
                _lanes.Add(lane);
            }

            Canvas.ForceUpdateCanvases();
            RefreshOpenedLaneVisibility(turnManager != null ? turnManager.currentTurn : 1);
            SetContentToStart();
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
            int minimumVisibleLanes = Mathf.Max(1, initialLaneCount);
            return Mathf.Clamp(Mathf.Max(currentTurn, minimumVisibleLanes), 1, _lanes.Count);
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

        public void FocusActiveLane(int laneIndex, string source = "FocusActiveLane")
        {
            SetBattlefieldView(UcgBattlefieldViewMode.FocusLane, laneIndex, false, source);
        }

        public void SmoothFocusActiveLane(int laneIndex)
        {
            SetBattlefieldView(UcgBattlefieldViewMode.FocusLane, laneIndex, true, "SmoothFocusActiveLane");
        }

        public void ShowOverview()
        {
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int overviewLaneCount = GetOverviewTargetLaneCount(currentTurn);
            SetBattlefieldView(UcgBattlefieldViewMode.OverviewAll, overviewLaneCount - 1, true, "ShowOverview");
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

            _currentViewMode = viewMode;
            int clampedLaneIndex = Mathf.Clamp(laneIndex, 0, _lanes.Count - 1);
            float targetScale = viewMode == UcgBattlefieldViewMode.OverviewAll
                ? GetOverviewScaleForLaneCount(clampedLaneIndex + 1)
                : 1f;
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

            return viewportRect;
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
            contentRect.sizeDelta = new Vector2(GetContentWidth(maxLaneCount), laneSize.y);

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
            return Mathf.Max(0f, contentWidth * Mathf.Max(0.1f, scale) - viewportWidth);
        }

        void SetContentToStart()
        {
            StopActiveLaneScroll();
            _currentViewMode = UcgBattlefieldViewMode.FocusLane;
            SetContentView(GetFocusLaneTargetX(0), 1f);
        }

        void SetContentAnchoredX(float targetX)
        {
            SetContentView(targetX, content != null ? content.localScale.x : 1f);
        }

        void SetContentView(float targetX, float targetScale)
        {
            if (content == null) return;

            float clampedScale = Mathf.Max(0.1f, targetScale);
            float maxScrollX = GetMaxScrollX(clampedScale);
            float clampedTargetX = Mathf.Clamp(targetX, -maxScrollX, 0f);
            content.localScale = new Vector3(clampedScale, clampedScale, 1f);
            content.anchoredPosition = GetTargetContentPosition(targetX, targetScale);

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
            return Mathf.Clamp(currentTurn + 2, Mathf.Max(1, visibleLaneCount), Mathf.Max(1, maxLaneCount));
        }

        float GetOverviewScaleForLaneCount(int laneCount)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            float targetWidth = GetContentWidth(Mathf.Clamp(laneCount, 1, Mathf.Max(1, maxLaneCount)));
            if (targetWidth <= 0f) return 1f;

            float fitScale = viewportWidth / targetWidth;
            return Mathf.Clamp(fitScale, overviewScale, 1f);
        }

        float GetOverviewTargetX(float scale, int laneCount)
        {
            float viewportWidth = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : 1040f;
            int clampedLaneCount = Mathf.Clamp(laneCount, 1, _lanes.Count > 0 ? _lanes.Count : maxLaneCount);
            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;

            for (int i = 0; i < clampedLaneCount; i++)
            {
                float left = GetLaneLeftX(i);
                float right = left + laneSize.x;
                groupLeft = Mathf.Min(groupLeft, left);
                groupRight = Mathf.Max(groupRight, right);
            }

            if (groupLeft == float.MaxValue)
            {
                groupLeft = 0f;
                groupRight = laneSize.x;
            }

            float groupCenter = (groupLeft + groupRight) * 0.5f * Mathf.Max(0.1f, scale);
            float targetX = viewportWidth * 0.5f - groupCenter;
            if (debugBattlefieldScroll)
            {
                Debug.Log($"ViewMode=OverviewAll, currentTurn={(turnManager != null ? turnManager.currentTurn : 1)}, activeLane={(turnManager != null ? turnManager.ActiveNewLaneIndex + 1 : 1)}, targetX={targetX}, openedLaneCount={GetOpenedLaneCount(turnManager != null ? turnManager.currentTurn : 1)}, overviewLaneCount={clampedLaneCount}");
            }
            return targetX;
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
            return laneSize.x * laneCount
                + laneSpacing * Mathf.Max(0, laneCount - 1)
                + Mathf.Max(0f, rightAuxiliaryColumnGutterWidth);
        }

        void ApplyLaneRect(RectTransform laneRect, int index)
        {
            if (laneRect == null) return;

            laneRect.anchorMin = new Vector2(0f, 0.5f);
            laneRect.anchorMax = new Vector2(0f, 0.5f);
            laneRect.pivot = new Vector2(0f, 0.5f);
            laneRect.sizeDelta = laneSize;
            laneRect.anchoredPosition = GetLanePosition(index);
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
