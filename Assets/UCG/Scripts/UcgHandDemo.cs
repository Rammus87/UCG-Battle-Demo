using System.Collections;
using System.Collections.Generic;
using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UCG
{
    public enum UcgTestMode
    {
        UltramanTest,
        MonsterAlienTest,
        TeamTest
    }

    public enum UcgPlayerCardDropTarget
    {
        Lane,
        SceneSlot
    }

    [DisallowMultipleComponent]
    public class UcgHandDemo : MonoBehaviour
    {
        const int DemoCardCount = 6;
        const string BattleBackgroundAssetPath = "Assets/UCG/Art/Backgrounds/battle_bg_mobile_symmetry.png";
        const string BattleBackgroundResourcePath = "UCG/Backgrounds/battle_bg_mobile_symmetry";
        static readonly string[] EffectTestCardIds =
        {
            "BP05-002",
            "BP05-008",
            "BP01-043",
            "BP01-105",
            "BP02-012",
            "BP05-044"
        };

        [Header("Optional Scene References")]
        public Canvas canvas;
        public RectTransform cardHolder;
        public Sprite battleBackgroundSprite;
        public RectTransform dragLayer;
        public RectTransform playerPlayArea;
        public Text playResultText;
        public Text effectFeedbackText;
        public Text turnInfoText;
        public Text phaseInfoText;
        public Text deckCountText;
        public Text opponentZoneText;
        public RectTransform discardPilePanel;
        public Text discardPilePanelText;
        public Button playerDiscardButton;
        public Button opponentDiscardButton;
        public Button closeDiscardPanelButton;
        public Text gameResultText;
        public Button restartButton;
        public Button switchTestButton;
        public Button skipTutorialButton;
        public Button nextTurnButton;
        public Button nextPhaseButton;
        public UcgCardInfoPanel cardInfoPanel;
        public UcgTutorialGuide tutorialGuide;
        public UcgBattlefieldManager battlefieldManager;
        public UcgTurnManager turnManager;
        public UcgPhaseManager phaseManager;
        public UcgDeckManager deckManager;
        public UcgExternalCardDatabase externalCardDatabase;
        public UcgCardImageLoader cardImageLoader;
        public UcgEffectManager effectManager;
        public UcgOpponentScript opponentScript;
        public UcgTurnOrderManager turnOrderManager;
        public UcgSfxController sfxController;
        public UcgSceneSlot sharedSceneSlot;
        public RectTransform playerDeckAnchor;
        public RectTransform playerDiscardAnchor;
        public RectTransform opponentDeckAnchor;
        public RectTransform opponentDiscardAnchor;
        public RectTransform playerSidePileGroup;
        public RectTransform opponentSidePileGroup;
        public RectTransform combatBoardRegionRoot;
        public RectTransform pileSideRegionRoot;
        public RectTransform sceneZoneAnchor;
        public Text playerDeckZoneText;
        public Text playerDiscardZoneText;
        public Text opponentDeckZoneText;
        public Text opponentDiscardZoneText;

        [Header("Test Mode")]
        public UcgTestMode currentTestMode = UcgTestMode.UltramanTest;

        [Header("Optional Demo Sprites")]
        public Sprite[] testCardSprites = new Sprite[DemoCardCount];

        [Header("Debug")]
        public bool debugLayoutDiagnostics;
        public bool debugOpponentRuntime;
        public bool debugScenePlacement;
        public bool debugSceneSlotVerbose;
        public bool debugBpBreakdown;
        public bool debugEffectResolution;
        public bool debugDeckOperation;
        public bool debugAdvanceButton;
        public bool debugAdvancePrompt;
        public bool debugDropValidation;
        public bool debugInteractionLock;
        public bool debugBattlefieldLayout;
        public bool debugForceSidePileExtremeOffset;

        [Header("Debug / Board Zones")]
        [SerializeField] public bool debugBoardZones = false;

        [Header("Debug / Effect Test Tools")]
        [SerializeField] bool debugEffectTestTools = false;

        [Header("Layout")]
        public Vector2 cardSize = new Vector2(190f, 276f);
        public float holderHeight = 430f;
        public float bottomSafePadding = 64f;
        public float horizontalSafePadding = 48f;
        public float minimumHolderWidth = 984f;
        public float autoPhaseDelaySeconds = 1f;
        public float opponentActionDelaySeconds = 0.75f;
        public float noValidSelectionAutoCloseDelay = 1.75f;
        public float openingOverviewHoldSeconds = 0.75f;
        public float openingFocusTransitionSeconds = 0.42f;
        public float advanceButtonCountdownSeconds = 30f;
        public float upgradeAdvanceCountdownSeconds = 30f;
        public float characterPlayAnimationSeconds = 0.3f;
        public float characterUpgradeAnimationSeconds = 0.36f;
        public float judgementResultAnimationSeconds = 0.85f;
        public float combatAreaOffsetX = 0f;
        public float combatFocusViewportPosition = 0.44f;
        public float debugCombatViewportOffset = 0f;
        public float rightAuxiliaryColumnGutterWidth = 450f;
        public float sceneAreaOffsetRatio = 0.55f;
        public float rightSidePileColumnDownShift = 0f;
        public float boardCardSlotWidth = 162f;
        public float boardCardSlotHeight = 224f;
        public float portraitSlotWidth = 162f;
        public float portraitSlotHeight = 224f;
        public float battleSlotWidth = 162f;
        public float battleSlotHeight = 224f;
        public float pileSlotWidth = 162f;
        public float pileSlotHeight = 224f;
        public float horizontalCardSafePadding = 34f;
        public float laneGapForHorizontalCard = 36f;
        public float minLaneGap = 28f;
        public float combatToPileGap = 96f;
        public float boardZoneSectionGap = 42f;
        public float opponentRowY = 240f;
        public float playerRowY = -240f;
        public float sceneAreaY = 0f;
        public float sceneAreaWidth = 560f;
        public float sceneAreaHeight = 230f;
        public float sceneToOpponentLaneGap = 34f;
        public float sceneToPlayerLaneGap = 34f;
        public float fieldColumnX = 0f;
        public float pileColumnRightInset = 96f;
        public bool useFixedReferenceBoardLayout = true;
        public Vector2 combatRegionPos = new Vector2(-78f, 0f);
        public Vector2 combatRegionSize = new Vector2(700f, 920f);
        public Vector2 pileRegionPos = new Vector2(334f, 0f);
        public Vector2 pileRegionSize = new Vector2(192f, 920f);
        public Vector2 referenceSceneAreaPos = new Vector2(0f, 0f);
        public Vector2 referenceOpponentBattleSlotPos = new Vector2(0f, 240f);
        public Vector2 referencePlayerBattleSlotPos = new Vector2(0f, -240f);
        public Vector2 referenceOpponentPileGroupPos = new Vector2(0f, 226f);
        public Vector2 referencePlayerPileGroupPos = new Vector2(0f, -226f);
        public float referenceDeckLocalY = 112f;
        public float referenceDiscardLocalY = -112f;
        public float sidePileScale = 0.78f;
        public float sidePileFocusedScale = 0.78f;
        public float sidePileGap = 22f;
        public float pileGroupVerticalSeparation = 16f;
        public float sidePileColumnMargin = 32f;
        public bool sidePileFollowFocus = false;
        public float sidePileFocusCompensationFactor = 0.18f;
        public float sidePileToLaneGap = 32f;
        public float sidePileTooFarGap = 160f;
        public float sidePileMinGapFromLane = 12f;
        public float combatToPileGapX = 42f;
        public float sidePileColumnNudgeX = 40f;
        public float sidePanelWidth = 260f;
        public float sidePanelRightMargin = 96f;
        public float deckDiscardGroupGap = 16f;
        public float debugSidePileExtremeOffsetX = -300f;
        public float sidePileRightMargin = 4f;
        public float sidePileBackgroundAlpha = 0.32f;
        public float sidePileOutlineAlpha = 0.46f;
        public float sceneAreaScale = 0.9f;
        public float sceneAreaAlpha = 0.07f;
        public float sceneAreaOutlineAlpha = 0.3f;

        int _createdHandCardSerial;
        Coroutine _autoPhaseRoutine;
        Coroutine _opponentActionRoutine;
        Coroutine _sceneSetupSkipRoutine;
        Coroutine _tutorialCompletionRoutine;
        Coroutine _effectAutoAdvanceRoutine;
        Coroutine _effectFeedbackRoutine;
        Coroutine _playStatusRoutine;
        Coroutine _openingFirstPlayerRoutine;
        Coroutine _deckOperationNoValidAutoCloseRoutine;
        Coroutine _advanceCountdownRoutine;
        Coroutine _battlefieldCommitAnimationRoutine;
        Coroutine _judgementVisualRoutine;
        bool _isTutorialFinishWaitingForClick;
        bool _tutorialFinishedNotified;
        bool _isAutoPhaseRunning;
        bool _isOpponentActionRunning;
        bool _isPlayerDraggingHandCard;
        bool _isAdvanceCountdownActive;
        bool _isAdvanceCountdownPaused;
        bool _advancePromptHandled;
        bool _isSelectingEffectTarget;
        bool _isSelectingDeckOperationCard;
        bool _isEffectAutoAdvancing;
        bool _isOpeningFirstPlayerSequence;
        bool _isOpeningCameraIntro;
        bool _openingCameraOverrodeScrollDuration;
        bool _isResolvingVisualAnimation;
        bool _boardZoneDebugPrinted;
        bool _debugBoardZonesStateLogged;
        bool _layoutDebugBoundsLogged;
        bool _hasInitializedBattlefieldView;
        float _initialBattlefieldContentOffsetX;
        bool _hasInitialBattlefieldContentOffset;
        float _lastAppliedCombatFocusViewportPosition = float.MinValue;
        float _lastSeenDebugCombatViewportOffset = float.MinValue;
        float _lastBattlefieldFocusOffsetX;
        float _lastBaseSidePileColumnX;
        float _lastFinalSidePileColumnX;
        float _lastSidePileScale;
        float _lastSidePileMinGapFromLane;
        float _lastPlayerLaneRightEdge;
        float _lastOpponentLaneRightEdge;
        float _lastSidePileToLaneGap;
        float _lastNearestLaneRightEdgeWorld;
        float _lastComputedSidePileColumnX;
        float _lastNudgedSidePileColumnX;
        float _lastBeforeClampSidePileColumnX;
        float _lastClampMinX;
        float _lastClampMaxX;
        float _lastClampedSidePileColumnX;
        float _openingCameraPreviousScrollDuration;
        float _lastSidePileColumnBeforeX;
        float _lastSidePileColumnAfterX;
        float _lastBoardZoneRootAnchoredX;
        float _lastBattlefieldContentAnchoredX;
        bool _lastSidePileClamped;
        bool _lastSidePileUsedGapInFormula;
        bool _lastSidePileOverwrittenByLayout;
        bool _lastSidePileExtremeOffsetApplied;
        bool _lastSidePileOverlapWithLane;
        bool _lastSidePileOverlapWithRevealArea;
        bool _lastSidePileTooFar;
        string _lastSidePileClampReason = "None";
        string _lastPileRegionNudgeMethod = "NotApplied";
        float _lastPileRegionXBeforeMethod = float.MinValue;
        float _lastPileRegionXBeforeNudge = float.MinValue;
        float _lastPileRegionXNudgeValue = float.MinValue;
        float _lastPileRegionXAfterNudge = float.MinValue;
        float _lastPileRegionXMaxSafeClamp = float.MinValue;
        float _lastPileRegionXAfterClamp = float.MinValue;
        float _lastPileRegionXAfterApply = float.MinValue;
        float _lastPileRegionVisibleRight = float.MinValue;
        float _lastPileRegionViewportRight = float.MinValue;
        bool _lastPileRegionClampApplied;
        int _lastPileRegionLayoutFrame = -1;
        bool _advanceToUpgradeAfterOpponentSetup;
        bool _showRestoredCardsMessageOnStart;
        bool _enterEffectPhaseHadPendingEffects;
        bool _battleEffectPhaseHadPendingEffects;
        string _openingFirstPlayerMessage = "";
        string _topPromptActionText = "";
        RectTransform _topPromptProgressTrackRect;
        RectTransform _topPromptProgressFillRect;
        RectTransform _effectFeedbackToastPanel;
        Image _effectFeedbackToastImage;
        RectTransform _effectFeedbackToastAccent;
        Image _effectFeedbackToastAccentImage;
        RectTransform _turnStartBannerRoot;
        CanvasGroup _turnStartBannerCanvasGroup;
        Text _turnStartBannerTurnText;
        Text _turnStartBannerInitiativeText;
        Coroutine _turnStartBannerRoutine;
        UcgGamePhase _lastTopPromptPhase = UcgGamePhase.Start;
        int _lastTurnStartBannerShownTurn;
        UcgEffectInstance _activeEffectSourceHighlight;
        int _opponentUpgradeExecutedTurn = -1;
        int _lastPlayerWinCount;
        int _lastOpponentWinCount;
        int _sceneCardPlacedTurn = -1;
        int _activatedEffectsPreparedTurn = -1;
        UcgEffectInstance _pendingTargetEffect;
        UcgEffectTargetType _pendingTargetType = UcgEffectTargetType.None;
        UcgBattleLane _pendingSwapSourceLane;
        UcgBattleLane _pendingBp05005StepDownLane;
        UcgBattleLane _pendingBp05008DiscardLane;
        UcgCardData _pendingBp05008ReturnedTopCard;
        int _pendingBp05008ReturnedLevel;
        UcgEffectInstance _pendingBp01105Effect;
        UcgCardData _pendingBp01105SelectedCard;
        readonly List<UcgCardData> _pendingBp01105RevealedCards = new List<UcgCardData>();
        UcgEffectInstance _pendingBp01043ReorderEffect;
        string _pendingBp01043ReorderMessage = "";
        readonly List<UcgCardData> _pendingBp01043RevealedCards = new List<UcgCardData>();
        UcgPendingAction _pendingAction;
        RectTransform _pendingConfirmRoot;
        Text _pendingConfirmText;
        Button _pendingConfirmButton;
        RectTransform _gameOverModalRoot;
        Text _gameOverModalText;
        Button _gameOverRestartButton;
        RectTransform _tutorialFinishClickLayer;
        Text _advancePromptMainText;
        Text _advancePromptCountdownText;
        RectTransform _advancePromptProgressTrackRect;
        RectTransform _advancePromptProgressFillRect;
        UcgGuidancePulse _advancePromptPulse;
        UnityEngine.Events.UnityAction _advancePromptConfirmAction;
        string _currentAdvancePromptLabel = "";
        float _advanceCountdownTotalSeconds;
        float _advanceCountdownRemainingSeconds;
        bool _advancePromptAutoAdvanceEnabled = true;
        int _advancePromptResetVersion;
        int _shownAdvancePromptResetVersion = -1;
        RectTransform _deckOperationSelectionRoot;
        RectTransform _deckOperationCardsRoot;
        Text _deckOperationSelectionTitle;
        Button _deckOperationNoSelectionButton;
        Button _boardDebugToggleButton;
        RectTransform _debugBoardZonesActivePanel;
        Text _debugBoardZonesActiveText;
        RectTransform _effectTestToolPanel;
        Text _effectTestSelectedCardText;
        int _effectTestSelectedCardIndex;
        UcgCardSelectionContext _pendingDeckSelection;
        int _opponentDeckCount = UcgDeckManager.DemoTemplateCount * UcgDeckManager.DemoRepeatCount - DemoCardCount;
        int _opponentHandCount;
        readonly List<UcgCardData> _playerDiscardPile = new List<UcgCardData>();
        readonly List<UcgCardData> _opponentDiscardPile = new List<UcgCardData>();
        readonly Dictionary<UcgCardData, List<string>> _temporaryTypeGrants = new Dictionary<UcgCardData, List<string>>();
        readonly List<UcgTemporarySceneSummon> _temporarySceneSummons = new List<UcgTemporarySceneSummon>();
        readonly Queue<string> _effectFeedbackQueue = new Queue<string>();
        readonly HashSet<string> _queuedEffectFeedbackMessages = new HashSet<string>();
        readonly HashSet<string> _bp01043RevealReorderHandledEffectKeys = new HashSet<string>();
        readonly HashSet<int> _playerWonTutorialLaneIndexes = new HashSet<int>();

        public bool IsGameOver { get; private set; }
        public UcgGameResultType CurrentGameResult { get; private set; } = UcgGameResultType.None;

        sealed class UcgTemporarySceneSummon
        {
            public UcgBattleLane lane;
            public UcgPlayerSide ownerSide;
            public UcgCardData cardData;
            public UcgCardData sourceSceneCard;
            public int turnNumber;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapBattleDemo()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "BattleDemo") return;
            if (FindFirstObjectByType<UcgHandDemo>() != null) return;

            var demoObject = new GameObject("UCGHandDemo");
            demoObject.AddComponent<UcgHandDemo>();
        }

        void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureDebugBoardZonesActivePanel();
            EnsureCardInfoPanel();
            EnsureTutorialGuide();
            EnsurePlayResultText();
            EnsureEffectFeedbackText();
            EnsureTurnStartBanner();
            EnsureTurnManager();
            EnsurePhaseManager();
            EnsureDeckManager();
            EnsureExternalCardServices();
            EnsureSfxController();
            EnsureEffectManager();
            EnsureOpponentScript();
            EnsureTurnOrderManager();
            EnsureDeckCountText();
            EnsureZoneInfoUI();
            EnsureDiscardPilePanel();
            EnsureGameResultText();
            EnsureBattlefieldManager();
            EnsureSceneSlots();
            EnsurePendingConfirmDialog();
            EnsureGameOverModal();
            EnsureTutorialFinishClickLayer();
            EnsureDeckOperationSelectionUI();
            EnsureCardHolder();
            EnsureRestartButton();
            EnsureSwitchTestButton();
            EnsureSkipTutorialButton();
            EnsureBoardDebugToggleButton();
            EnsureEffectTestToolPanel();
            EnsureNextPhaseButton();
            EnsureNextTurnButton();
            HideBattleJudgeButton();
            EnsureDragLayer();
            tutorialGuide.ResetForMode(currentTestMode);
            ResetDeckAndBuildStartingHand();
            ShowCurrentTestMode();
            turnManager.ResetTurns();
            turnOrderManager.ResetTurnOrder();
            _openingFirstPlayerMessage = turnOrderManager.GetOpeningFirstPlayerText();
            BeginOpeningFirstPlayerSequence();
            UpdateDebugBoardZonesActivePanel();
            LogDebugBoardZonesState("Start", true);
            LogUcgHandDemoInstances("Start");
        }

        void Update()
        {
            UpdateDebugBoardZonesActivePanel();
            UpdateLayoutDebugBounds();
            UpdateDebugCombatViewportOffset();
            UpdateEffectTestToolPanelVisibility();
            UpdateTopPhaseHud();
            EnsureTutorialPanelTopLayer();
        }

        void EnsureDebugBoardZonesActivePanel()
        {
            if (canvas == null) return;

            const string panelName = "Debug Board Zones Active Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            Image panelImage;
            Outline panelOutline;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(Outline));
                panelObject.transform.SetParent(canvas.transform, false);
                _debugBoardZonesActivePanel = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
                panelOutline = panelObject.GetComponent<Outline>();
            }
            else
            {
                _debugBoardZonesActivePanel = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
                panelOutline = existingPanel.GetComponent<Outline>();
                if (panelOutline == null) panelOutline = existingPanel.gameObject.AddComponent<Outline>();
            }

            _debugBoardZonesActivePanel.anchorMin = new Vector2(0.5f, 0.5f);
            _debugBoardZonesActivePanel.anchorMax = new Vector2(0.5f, 0.5f);
            _debugBoardZonesActivePanel.pivot = new Vector2(0.5f, 0.5f);
            _debugBoardZonesActivePanel.anchoredPosition = Vector2.zero;
            _debugBoardZonesActivePanel.sizeDelta = new Vector2(460f, 190f);
            _debugBoardZonesActivePanel.localScale = Vector3.one;
            _debugBoardZonesActivePanel.localEulerAngles = Vector3.zero;

            panelImage.enabled = true;
            panelImage.color = new Color(0.05f, 0.95f, 0.18f, 0.74f);
            panelImage.raycastTarget = false;

            panelOutline.enabled = true;
            panelOutline.effectColor = new Color(1f, 0.12f, 0.08f, 1f);
            panelOutline.effectDistance = new Vector2(4f, -4f);

            _debugBoardZonesActiveText = EnsureZoneText(
                _debugBoardZonesActivePanel,
                "Debug Board Zones Active Text",
                new Vector2(0.04f, 0.1f),
                new Vector2(0.96f, 0.9f),
                LoadPlaceholderFont(),
                28,
                Color.white);
            _debugBoardZonesActiveText.text = "DEBUG BOARD ZONES ACTIVE";
            _debugBoardZonesActiveText.alignment = TextAnchor.MiddleCenter;
            _debugBoardZonesActiveText.raycastTarget = false;

            UpdateDebugBoardZonesActivePanel();
        }

        void UpdateDebugBoardZonesActivePanel()
        {
            if (_debugBoardZonesActivePanel == null)
            {
                if (canvas != null)
                {
                    EnsureDebugBoardZonesActivePanel();
                }
                return;
            }

            bool visible = debugBoardZones;
            if (_debugBoardZonesActivePanel.gameObject.activeSelf != visible)
            {
                _debugBoardZonesActivePanel.gameObject.SetActive(visible);
            }

            if (visible)
            {
                _debugBoardZonesActivePanel.SetAsLastSibling();
                RefreshPileSideRegionDebugVisibility();
            }
        }

        void RefreshPileSideRegionDebugVisibility()
        {
            if (!debugBoardZones) return;

            ForceDebugRegionVisible(pileSideRegionRoot, "Pile Side Region");
            ForceDebugPileGroupVisible(opponentSidePileGroup);
            ForceDebugPileGroupVisible(playerSidePileGroup);
            ForceDebugPileZoneVisible(opponentDiscardAnchor, "OP DISCARD");
            ForceDebugPileZoneVisible(opponentDeckAnchor, "OP DECK");
            ForceDebugPileZoneVisible(playerDeckAnchor, "PLAYER DECK");
            ForceDebugPileZoneVisible(playerDiscardAnchor, "PLAYER DISCARD");
        }

        void ForceDebugRegionVisible(RectTransform region, string regionName)
        {
            if (region == null) return;

            region.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(region, 1f);
            region.SetAsLastSibling();
            EnsureBoardRegionVisual(region, regionName);
        }

        void ForceDebugPileGroupVisible(RectTransform group)
        {
            if (group == null) return;

            group.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(group, 1f);
            group.SetAsLastSibling();
        }

        void ForceDebugPileZoneVisible(RectTransform zone, string labelText)
        {
            if (zone == null) return;

            zone.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(zone, 1f);
            zone.SetAsLastSibling();

            Image image = zone.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = true;
                image.color = new Color(0.18f, 0.06f, 0.30f, 0.86f);
                image.raycastTarget = false;
            }

            Outline outline = zone.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = new Color(1f, 0.92f, 0.12f, 1f);
                outline.effectDistance = new Vector2(3.2f, -3.2f);
            }

            Text label = zone.Find("Zone Label") != null ? zone.Find("Zone Label").GetComponent<Text>() : null;
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = labelText;
                label.fontSize = 16;
                label.color = new Color(1f, 0.96f, 0.18f, 1f);
            }

            UpdateBoardZoneDebugText(zone);
        }

        void EnsureCanvas()
        {
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            ConfigureCanvasScaler(canvas.gameObject);
            EnsureHudBackground();
        }

        void ConfigureCanvasScaler(GameObject canvasObject)
        {
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        void EnsureHudBackground()
        {
            if (canvas == null) return;

            const string backgroundName = "UCG HUD Background";
            Transform existingBackground = canvas.transform.Find(backgroundName);
            RectTransform backgroundRect;
            Image backgroundImage;

            if (existingBackground == null)
            {
                var backgroundObject = new GameObject(backgroundName, typeof(RectTransform), typeof(Image));
                backgroundObject.transform.SetParent(canvas.transform, false);
                backgroundRect = backgroundObject.GetComponent<RectTransform>();
                backgroundImage = backgroundObject.GetComponent<Image>();
            }
            else
            {
                backgroundRect = existingBackground as RectTransform;
                backgroundImage = existingBackground.GetComponent<Image>();
                if (backgroundImage == null) backgroundImage = existingBackground.gameObject.AddComponent<Image>();
            }

            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.localScale = Vector3.one;
            backgroundRect.localEulerAngles = Vector3.zero;
            backgroundRect.SetAsFirstSibling();
            Sprite battleBackground = LoadBattleBackgroundSprite();
            bool hasBattleBackground = battleBackground != null;
            backgroundImage.sprite = battleBackground;
            backgroundImage.color = hasBattleBackground
                ? Color.white
                : new Color(0.015f, 0.03f, 0.055f, 1f);
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;

            EnsureHudDecorBar(backgroundRect, "HUD Energy Band Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(1180f, 46f), -8f, hasBattleBackground ? Color.clear : new Color(0.18f, 0.62f, 0.95f, 0.045f));
            EnsureHudDecorBar(backgroundRect, "HUD Energy Band Field", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 930f), new Vector2(1080f, 430f), 0f, hasBattleBackground ? Color.clear : new Color(0.12f, 0.24f, 0.34f, 0.08f));
            EnsureHudDecorBar(backgroundRect, "HUD Energy Band Hand", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1180f, 220f), 5f, hasBattleBackground ? Color.clear : new Color(0.02f, 0.08f, 0.13f, 0.28f));
            EnsureHudDecorBar(backgroundRect, "HUD Diagonal Trace A", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, 100f), new Vector2(760f, 5f), -24f, hasBattleBackground ? Color.clear : new Color(0.42f, 0.92f, 1f, 0.035f));
            EnsureHudDecorBar(backgroundRect, "HUD Diagonal Trace B", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(330f, -40f), new Vector2(720f, 4f), -24f, hasBattleBackground ? Color.clear : new Color(0.85f, 0.92f, 1f, 0.025f));
        }

        Sprite LoadBattleBackgroundSprite()
        {
            if (battleBackgroundSprite != null) return battleBackgroundSprite;

#if UNITY_EDITOR
            Sprite editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattleBackgroundAssetPath);
            if (editorSprite != null) return editorSprite;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BattleBackgroundAssetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif

            Sprite resourceSprite = Resources.Load<Sprite>(BattleBackgroundResourcePath);
            if (resourceSprite != null) return resourceSprite;

            Sprite[] resourceSprites = Resources.LoadAll<Sprite>(BattleBackgroundResourcePath);
            if (resourceSprites != null && resourceSprites.Length > 0) return resourceSprites[0];

            Debug.LogWarning($"UCG battle background missing: assign battleBackgroundSprite or add Resources/{BattleBackgroundResourcePath}");
            return null;
        }

        void EnsureHudDecorBar(RectTransform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, float rotationZ, Color color)
        {
            Transform existingBar = parent.Find(objectName);
            RectTransform barRect;
            Image barImage;

            if (existingBar == null)
            {
                var barObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                barObject.transform.SetParent(parent, false);
                barRect = barObject.GetComponent<RectTransform>();
                barImage = barObject.GetComponent<Image>();
            }
            else
            {
                barRect = existingBar as RectTransform;
                barImage = existingBar.GetComponent<Image>();
                if (barImage == null) barImage = existingBar.gameObject.AddComponent<Image>();
            }

            barRect.anchorMin = anchorMin;
            barRect.anchorMax = anchorMax;
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = anchoredPosition;
            barRect.sizeDelta = size;
            barRect.localScale = Vector3.one;
            barRect.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            barImage.color = color;
            barImage.raycastTarget = false;
        }

        Image EnsureHudBackplate(string objectName, RectTransform targetRect, Color fillColor, Color outlineColor, Vector2 padding)
        {
            if (targetRect == null || targetRect.parent == null) return null;

            Transform existingPlate = targetRect.parent.Find(objectName);
            RectTransform plateRect;
            Image plateImage;

            if (existingPlate == null)
            {
                var plateObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline));
                plateObject.transform.SetParent(targetRect.parent, false);
                plateRect = plateObject.GetComponent<RectTransform>();
                plateImage = plateObject.GetComponent<Image>();
            }
            else
            {
                plateRect = existingPlate as RectTransform;
                plateImage = existingPlate.GetComponent<Image>();
                if (plateImage == null) plateImage = existingPlate.gameObject.AddComponent<Image>();
                if (existingPlate.GetComponent<Outline>() == null) existingPlate.gameObject.AddComponent<Outline>();
            }

            plateRect.anchorMin = targetRect.anchorMin;
            plateRect.anchorMax = targetRect.anchorMax;
            plateRect.pivot = targetRect.pivot;
            plateRect.anchoredPosition = targetRect.anchoredPosition;
            plateRect.sizeDelta = targetRect.sizeDelta + padding;
            plateRect.localScale = Vector3.one;
            plateRect.localEulerAngles = Vector3.zero;
            plateImage.color = fillColor;
            plateImage.raycastTarget = false;
            ApplyRoundedPanelImage(plateImage);

            var outline = plateRect.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(plateRect.gameObject);
            shadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;

            plateRect.SetSiblingIndex(Mathf.Max(0, targetRect.GetSiblingIndex()));
            return plateImage;
        }

        void ApplyHudButtonStyle(Button button, Color normalColor, Color highlightedColor)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = normalColor;
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.58f, 0.9f, 1f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = new Color(highlightedColor.r * 0.82f, highlightedColor.g * 0.82f, highlightedColor.b * 0.82f, highlightedColor.a);
            colors.selectedColor = highlightedColor;
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.42f);
            button.colors = colors;
        }

        void ApplyRoundedPanelImage(Image image)
        {
            if (image == null) return;

            Sprite roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (roundedSprite == null) return;

            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        Shadow EnsureUiShadow(GameObject target)
        {
            if (target == null) return null;

            Shadow[] shadows = target.GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] != null && shadows[i].GetType() == typeof(Shadow))
                {
                    return shadows[i];
                }
            }

            return target.AddComponent<Shadow>();
        }

        Text EnsurePlayResultText()
        {
            const string resultName = "Play Result Text";
            Transform existingResult = canvas.transform.Find(resultName);
            RectTransform resultRect;

            if (existingResult == null)
            {
                var resultObject = new GameObject(resultName, typeof(RectTransform), typeof(Text));
                resultObject.transform.SetParent(canvas.transform, false);
                resultRect = resultObject.GetComponent<RectTransform>();
                playResultText = resultObject.GetComponent<Text>();
            }
            else
            {
                resultRect = existingResult as RectTransform;
                playResultText = existingResult.GetComponent<Text>();
                if (playResultText == null) playResultText = existingResult.gameObject.AddComponent<Text>();
            }

            resultRect.anchorMin = new Vector2(0.5f, 1f);
            resultRect.anchorMax = new Vector2(0.5f, 1f);
            resultRect.pivot = new Vector2(0.5f, 1f);
            resultRect.anchoredPosition = new Vector2(0f, -56f);
            resultRect.sizeDelta = new Vector2(680f, 94f);

            playResultText.text = "";
            playResultText.alignment = TextAnchor.MiddleCenter;
            playResultText.color = UcgToolUiPalette.SoftWhite;
            playResultText.supportRichText = true;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                playResultText.font = placeholderFont;
            }
            playResultText.fontSize = 18;
            playResultText.resizeTextForBestFit = true;
            playResultText.resizeTextMinSize = 11;
            playResultText.resizeTextMaxSize = 23;
            playResultText.lineSpacing = 0.86f;
            playResultText.raycastTarget = false;
            Image panelImage = EnsureHudBackplate(
                "Play Result HUD Panel",
                resultRect,
                UcgToolUiPalette.DeepGlass,
                UcgToolUiPalette.GlassBorder,
                new Vector2(24f, 14f));
            RectTransform panelRect = panelImage != null ? panelImage.transform as RectTransform : null;
            EnsureTopPromptProgress(panelRect);
            ApplyTopPromptLayer(resultRect, panelRect);
            UpdateTopPhaseHud();

            return playResultText;
        }

        void EnsureTopPromptProgress(RectTransform panelRect)
        {
            if (panelRect == null) return;

            const string trackName = "Top Prompt Countdown Track";
            Transform existingTrack = panelRect.Find(trackName);
            Image trackImage;
            if (existingTrack == null)
            {
                var trackObject = new GameObject(trackName, typeof(RectTransform), typeof(Image));
                trackObject.transform.SetParent(panelRect, false);
                _topPromptProgressTrackRect = trackObject.GetComponent<RectTransform>();
                trackImage = trackObject.GetComponent<Image>();
            }
            else
            {
                _topPromptProgressTrackRect = existingTrack as RectTransform;
                trackImage = existingTrack.GetComponent<Image>();
                if (trackImage == null) trackImage = existingTrack.gameObject.AddComponent<Image>();
            }

            _topPromptProgressTrackRect.anchorMin = new Vector2(0.1f, 0f);
            _topPromptProgressTrackRect.anchorMax = new Vector2(0.9f, 0f);
            _topPromptProgressTrackRect.pivot = new Vector2(0.5f, 0f);
            _topPromptProgressTrackRect.anchoredPosition = new Vector2(0f, 10f);
            _topPromptProgressTrackRect.sizeDelta = new Vector2(0f, 4f);
            _topPromptProgressTrackRect.localScale = Vector3.one;
            _topPromptProgressTrackRect.localEulerAngles = Vector3.zero;
            trackImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.16f);
            trackImage.raycastTarget = false;

            const string fillName = "Top Prompt Countdown Fill";
            Transform existingFill = _topPromptProgressTrackRect.Find(fillName);
            Image fillImage;
            if (existingFill == null)
            {
                var fillObject = new GameObject(fillName, typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(_topPromptProgressTrackRect, false);
                _topPromptProgressFillRect = fillObject.GetComponent<RectTransform>();
                fillImage = fillObject.GetComponent<Image>();
            }
            else
            {
                _topPromptProgressFillRect = existingFill as RectTransform;
                fillImage = existingFill.GetComponent<Image>();
                if (fillImage == null) fillImage = existingFill.gameObject.AddComponent<Image>();
            }

            _topPromptProgressFillRect.anchorMin = Vector2.zero;
            _topPromptProgressFillRect.anchorMax = Vector2.one;
            _topPromptProgressFillRect.offsetMin = Vector2.zero;
            _topPromptProgressFillRect.offsetMax = Vector2.zero;
            _topPromptProgressFillRect.localScale = Vector3.one;
            _topPromptProgressFillRect.localEulerAngles = Vector3.zero;
            fillImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.78f);
            fillImage.raycastTarget = false;

            Shadow fillGlow = EnsureUiShadow(_topPromptProgressFillRect.gameObject);
            fillGlow.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.24f);
            fillGlow.effectDistance = new Vector2(0f, -1f);
            fillGlow.useGraphicAlpha = true;

            SetTopPromptProgress(0f, false);
        }

        void ApplyTopPromptLayer(RectTransform textRect, RectTransform panelRect)
        {
            ApplyHudCanvasLayer(panelRect, 27990);
            ApplyHudCanvasLayer(textRect, 28000);
            if (panelRect != null) panelRect.SetAsLastSibling();
            if (textRect != null) textRect.SetAsLastSibling();
        }

        void ApplyHudCanvasLayer(RectTransform rect, int sortingOrder)
        {
            if (rect == null) return;

            Canvas layerCanvas = rect.GetComponent<Canvas>();
            if (layerCanvas == null) layerCanvas = rect.gameObject.AddComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = sortingOrder;
        }

        void EnsureTurnStartBanner()
        {
            if (canvas == null) return;

            const string bannerName = "Turn Start Battle Prompt";
            Transform existingBanner = canvas.transform.Find(bannerName);
            Image panelImage;

            if (existingBanner == null)
            {
                var bannerObject = new GameObject(bannerName, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
                bannerObject.transform.SetParent(canvas.transform, false);
                _turnStartBannerRoot = bannerObject.GetComponent<RectTransform>();
                panelImage = bannerObject.GetComponent<Image>();
                _turnStartBannerCanvasGroup = bannerObject.GetComponent<CanvasGroup>();
            }
            else
            {
                _turnStartBannerRoot = existingBanner as RectTransform;
                panelImage = existingBanner.GetComponent<Image>();
                if (panelImage == null) panelImage = existingBanner.gameObject.AddComponent<Image>();
                if (existingBanner.GetComponent<Outline>() == null) existingBanner.gameObject.AddComponent<Outline>();
                _turnStartBannerCanvasGroup = existingBanner.GetComponent<CanvasGroup>();
                if (_turnStartBannerCanvasGroup == null) _turnStartBannerCanvasGroup = existingBanner.gameObject.AddComponent<CanvasGroup>();
            }

            _turnStartBannerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _turnStartBannerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _turnStartBannerRoot.pivot = new Vector2(0.5f, 0.5f);
            _turnStartBannerRoot.anchoredPosition = new Vector2(0f, 36f);
            _turnStartBannerRoot.sizeDelta = new Vector2(430f, 128f);
            _turnStartBannerRoot.localScale = Vector3.one;
            _turnStartBannerRoot.localEulerAngles = Vector3.zero;

            panelImage.color = UcgToolUiPalette.DeepGlass;
            panelImage.raycastTarget = false;
            ApplyRoundedPanelImage(panelImage);

            var outline = _turnStartBannerRoot.GetComponent<Outline>();
            outline.effectColor = UcgToolUiPalette.GlassBorder;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(_turnStartBannerRoot.gameObject);
            shadow.effectColor = new Color(0f, 0.06f, 0.16f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;

            _turnStartBannerCanvasGroup.alpha = 0f;
            _turnStartBannerCanvasGroup.interactable = false;
            _turnStartBannerCanvasGroup.blocksRaycasts = false;

            _turnStartBannerTurnText = EnsureTurnStartBannerText(
                "Turn Text",
                new Vector2(0.06f, 0.58f),
                new Vector2(0.94f, 0.86f),
                18,
                UcgToolUiPalette.SoftWhite);
            _turnStartBannerInitiativeText = EnsureTurnStartBannerText(
                "Initiative Text",
                new Vector2(0.06f, 0.15f),
                new Vector2(0.94f, 0.62f),
                36,
                UcgToolUiPalette.WarningGold);

            ApplyHudCanvasLayer(_turnStartBannerRoot, 27985);
            _turnStartBannerRoot.gameObject.SetActive(false);
        }

        Text EnsureTurnStartBannerText(string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            if (_turnStartBannerRoot == null) return null;

            Transform existingText = _turnStartBannerRoot.Find(objectName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
                textObject.transform.SetParent(_turnStartBannerRoot, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
                if (existingText.GetComponent<Outline>() == null) existingText.gameObject.AddComponent<Outline>();
            }

            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
            textRect.localEulerAngles = Vector3.zero;

            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null) text.font = placeholderFont;
            text.text = "";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(11, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            var outline = text.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0.04f, 0.09f, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            return text;
        }

        bool ShouldPlayTurnStartBannerForCurrentTurn()
        {
            int turnNumber = GetCurrentTurnStartBannerNumber();
            return turnNumber > 0 && _lastTurnStartBannerShownTurn != turnNumber;
        }

        int GetCurrentTurnStartBannerNumber()
        {
            return Mathf.Max(1, turnManager != null ? turnManager.currentTurn : 1);
        }

        void BeginTurnStartBannerThenAutoPhase()
        {
            if (_turnStartBannerRoutine != null) return;
            _turnStartBannerRoutine = StartCoroutine(TurnStartBannerThenAutoPhaseRoutine());
        }

        IEnumerator TurnStartBannerThenAutoPhaseRoutine()
        {
            HideAdvanceButton();
            yield return PlayTurnStartBannerForCurrentTurn();
            _turnStartBannerRoutine = null;

            if (IsGameOver || phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.Start) yield break;
            BeginAutoPhaseRoutine(phaseManager.CurrentPhase);
        }

        IEnumerator PlayTurnStartBannerForCurrentTurn()
        {
            int turnNumber = GetCurrentTurnStartBannerNumber();
            if (turnNumber <= 0 || _lastTurnStartBannerShownTurn == turnNumber) yield break;

            _lastTurnStartBannerShownTurn = turnNumber;
            yield return PlayTurnStartBannerAnimation(turnNumber);
        }

        IEnumerator PlayTurnStartBannerAnimation(int turnNumber)
        {
            EnsureTurnStartBanner();
            if (_turnStartBannerRoot == null || _turnStartBannerCanvasGroup == null) yield break;

            bool playerFirst = turnOrderManager == null || turnOrderManager.GetCurrentFirstPlayer() == UcgPlayerSide.Player;
            if (_turnStartBannerTurnText != null)
            {
                _turnStartBannerTurnText.text = $"第 {turnNumber} 回合";
            }
            if (_turnStartBannerInitiativeText != null)
            {
                _turnStartBannerInitiativeText.text = playerFirst ? "你是先攻" : "你是後攻";
                _turnStartBannerInitiativeText.color = playerFirst
                    ? UcgToolUiPalette.WarningGold
                    : UcgToolUiPalette.SoftWhite;
            }

            _turnStartBannerRoot.gameObject.SetActive(true);
            _turnStartBannerRoot.SetAsLastSibling();
            ApplyHudCanvasLayer(_turnStartBannerRoot, 27985);
            yield return AnimateTurnStartBanner(0f, 1f, 0.2f, 0.96f, 1.03f);
            _turnStartBannerCanvasGroup.alpha = 1f;
            _turnStartBannerRoot.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.75f);
            yield return AnimateTurnStartBanner(1f, 0f, 0.28f, 1f, 0.98f);
            HideTurnStartBannerImmediate();
        }

        IEnumerator AnimateTurnStartBanner(float fromAlpha, float toAlpha, float durationSeconds, float fromScale, float toScale)
        {
            float duration = Mathf.Max(0.01f, durationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                if (_turnStartBannerCanvasGroup != null)
                {
                    _turnStartBannerCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                }
                if (_turnStartBannerRoot != null)
                {
                    _turnStartBannerRoot.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
                }

                yield return null;
            }
        }

        void StopTurnStartBannerRoutine()
        {
            if (_turnStartBannerRoutine != null)
            {
                StopCoroutine(_turnStartBannerRoutine);
                _turnStartBannerRoutine = null;
            }

            HideTurnStartBannerImmediate();
        }

        void HideTurnStartBannerImmediate()
        {
            if (_turnStartBannerCanvasGroup != null)
            {
                _turnStartBannerCanvasGroup.alpha = 0f;
                _turnStartBannerCanvasGroup.interactable = false;
                _turnStartBannerCanvasGroup.blocksRaycasts = false;
            }
            if (_turnStartBannerRoot != null)
            {
                _turnStartBannerRoot.localScale = Vector3.one;
                _turnStartBannerRoot.gameObject.SetActive(false);
            }
        }

        Text EnsureEffectFeedbackText()
        {
            const string feedbackName = "Effect Feedback Text";
            Transform existingFeedback = canvas.transform.Find(feedbackName);
            RectTransform feedbackRect;

            if (existingFeedback == null)
            {
                var feedbackObject = new GameObject(feedbackName, typeof(RectTransform), typeof(Text), typeof(Outline));
                feedbackObject.transform.SetParent(canvas.transform, false);
                feedbackRect = feedbackObject.GetComponent<RectTransform>();
                effectFeedbackText = feedbackObject.GetComponent<Text>();
            }
            else
            {
                feedbackRect = existingFeedback as RectTransform;
                effectFeedbackText = existingFeedback.GetComponent<Text>();
                if (effectFeedbackText == null) effectFeedbackText = existingFeedback.gameObject.AddComponent<Text>();
                if (existingFeedback.GetComponent<Outline>() == null) existingFeedback.gameObject.AddComponent<Outline>();
            }

            feedbackRect.anchorMin = new Vector2(0.5f, 0f);
            feedbackRect.anchorMax = new Vector2(0.5f, 0f);
            feedbackRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackRect.anchoredPosition = new Vector2(0f, 520f);
            feedbackRect.sizeDelta = new Vector2(680f, 38f);
            feedbackRect.localScale = Vector3.one;

            effectFeedbackText.text = "";
            effectFeedbackText.alignment = TextAnchor.MiddleCenter;
            effectFeedbackText.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.SoftWhite, 0f);
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                effectFeedbackText.font = placeholderFont;
            }
            effectFeedbackText.fontSize = 16;
            effectFeedbackText.resizeTextForBestFit = true;
            effectFeedbackText.resizeTextMinSize = 12;
            effectFeedbackText.resizeTextMaxSize = 16;
            effectFeedbackText.raycastTarget = false;

            var outline = effectFeedbackText.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.58f);
            outline.effectDistance = new Vector2(1f, -1f);
            EnsureEffectFeedbackToastVisual(feedbackRect);
            return effectFeedbackText;
        }

        void EnsureEffectFeedbackToastVisual(RectTransform textRect)
        {
            if (canvas == null || textRect == null) return;

            const string panelName = "Effect Feedback Toast Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(canvas.transform, false);
                _effectFeedbackToastPanel = panelObject.GetComponent<RectTransform>();
                _effectFeedbackToastImage = panelObject.GetComponent<Image>();
            }
            else
            {
                _effectFeedbackToastPanel = existingPanel as RectTransform;
                _effectFeedbackToastImage = existingPanel.GetComponent<Image>();
                if (_effectFeedbackToastImage == null) _effectFeedbackToastImage = existingPanel.gameObject.AddComponent<Image>();
            }

            _effectFeedbackToastPanel.anchorMin = textRect.anchorMin;
            _effectFeedbackToastPanel.anchorMax = textRect.anchorMax;
            _effectFeedbackToastPanel.pivot = textRect.pivot;
            _effectFeedbackToastPanel.anchoredPosition = textRect.anchoredPosition;
            _effectFeedbackToastPanel.sizeDelta = textRect.sizeDelta + new Vector2(22f, 12f);
            _effectFeedbackToastPanel.localScale = Vector3.one;
            _effectFeedbackToastPanel.localEulerAngles = Vector3.zero;
            _effectFeedbackToastPanel.SetSiblingIndex(Mathf.Max(0, textRect.GetSiblingIndex()));

            ApplyRoundedPanelImage(_effectFeedbackToastImage);
            _effectFeedbackToastImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.ToastGlass, 0f);
            _effectFeedbackToastImage.raycastTarget = false;

            const string accentName = "Effect Feedback Toast Accent";
            Transform existingAccent = _effectFeedbackToastPanel.Find(accentName);
            if (existingAccent == null)
            {
                var accentObject = new GameObject(accentName, typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(_effectFeedbackToastPanel, false);
                _effectFeedbackToastAccent = accentObject.GetComponent<RectTransform>();
                _effectFeedbackToastAccentImage = accentObject.GetComponent<Image>();
            }
            else
            {
                _effectFeedbackToastAccent = existingAccent as RectTransform;
                _effectFeedbackToastAccentImage = existingAccent.GetComponent<Image>();
                if (_effectFeedbackToastAccentImage == null) _effectFeedbackToastAccentImage = existingAccent.gameObject.AddComponent<Image>();
            }

            _effectFeedbackToastAccent.anchorMin = new Vector2(0f, 0f);
            _effectFeedbackToastAccent.anchorMax = new Vector2(0f, 1f);
            _effectFeedbackToastAccent.pivot = new Vector2(0f, 0.5f);
            _effectFeedbackToastAccent.anchoredPosition = Vector2.zero;
            _effectFeedbackToastAccent.sizeDelta = new Vector2(5f, 0f);
            _effectFeedbackToastAccent.localScale = Vector3.one;
            _effectFeedbackToastAccent.localEulerAngles = Vector3.zero;
            _effectFeedbackToastAccentImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0f);
            _effectFeedbackToastAccentImage.raycastTarget = false;
            SetEffectFeedbackToastAlpha(0f);
        }

        void SetEffectFeedbackToastAlpha(float alpha)
        {
            if (_effectFeedbackToastImage != null)
            {
                _effectFeedbackToastImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.78f * alpha);
            }
            if (_effectFeedbackToastAccentImage != null)
            {
                _effectFeedbackToastAccentImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.92f * alpha);
            }
        }

        void EnsureBattlefieldManager()
        {
            const string battlefieldName = "Battlefield";
            Transform existingBattlefield = canvas.transform.Find(battlefieldName);
            RectTransform battlefieldRect;

            if (existingBattlefield == null)
            {
                var battlefieldObject = new GameObject(battlefieldName, typeof(RectTransform), typeof(UcgBattlefieldManager));
                battlefieldObject.transform.SetParent(canvas.transform, false);
                battlefieldRect = battlefieldObject.GetComponent<RectTransform>();
                battlefieldManager = battlefieldObject.GetComponent<UcgBattlefieldManager>();
            }
            else
            {
                battlefieldRect = existingBattlefield as RectTransform;
                battlefieldManager = existingBattlefield.GetComponent<UcgBattlefieldManager>();
                if (battlefieldManager == null) battlefieldManager = existingBattlefield.gameObject.AddComponent<UcgBattlefieldManager>();
            }

            battlefieldRect.anchorMin = new Vector2(0.5f, 0f);
            battlefieldRect.anchorMax = new Vector2(0.5f, 0f);
            battlefieldRect.pivot = new Vector2(0.5f, 0.5f);
            battlefieldRect.anchoredPosition = new Vector2(0f, 940f);
            battlefieldRect.sizeDelta = new Vector2(1040f, 960f);

            battlefieldManager.maxLaneCount = 8;
            battlefieldManager.initialLaneCount = 3;
            battlefieldManager.visibleLaneCount = 3;
            battlefieldManager.lanesRoot = battlefieldRect;
            battlefieldManager.playerSlotSize = GetBattleSlotSize();
            battlefieldManager.opponentSlotSize = GetOpponentBattleSlotSize();
            battlefieldManager.laneSize = new Vector2(GetBattleLaneWidth(), 960f);
            battlefieldManager.laneSpacing = GetBattleLaneSpacing();
            battlefieldManager.opponentCardSize = GetOpponentCardBoardSize();
            battlefieldManager.combatAreaOffsetX = GetCombatAreaOffsetX();
            ApplyCombatFocusViewportPosition("EnsureBattlefieldManager");
            battlefieldManager.rightAuxiliaryColumnGutterWidth = rightAuxiliaryColumnGutterWidth;
            battlefieldManager.debugBattlefieldLayout = debugBattlefieldLayout || debugLayoutDiagnostics;
            battlefieldManager.hasInitializedBattlefieldView = _hasInitializedBattlefieldView;
            battlefieldManager.Configure(tutorialGuide, turnManager, phaseManager, cardInfoPanel, playResultText, GetPlacedBattleCardSize(), GetTestCardSprite(0), LoadPlaceholderFont());
            battlefieldManager.ConfigureOpponentScript(opponentScript, currentTestMode);
            ApplyReferenceBattleSlotLayout();
            ConfigureLaneClickTargets();
            ApplyInitialBattlefieldView();
            EnsureBattlefieldZoneAnchors();
            RefreshZoneInfoUI();
            SetZoneInfoUIVisible(true);
        }

        void ApplyInitialBattlefieldView()
        {
            if (battlefieldManager == null) return;

            battlefieldManager.debugBattlefieldLayout = debugBattlefieldLayout || debugLayoutDiagnostics;
            battlefieldManager.hasInitializedBattlefieldView = _hasInitializedBattlefieldView;
            ApplyCombatFocusViewportPosition("ApplyInitialBattlefieldView");
            battlefieldManager.RefreshOpenedLaneVisibility(turnManager != null ? turnManager.currentTurn : 1);
            battlefieldManager.JumpToActiveLane("ApplyInitialBattlefieldView");
            LogCombatViewportDiagnostic("ApplyInitialBattlefieldView", "FocusLane", GetCurrentActiveLaneIndex());
            CaptureInitialBattlefieldContentOffset();
            _hasInitializedBattlefieldView = true;
            battlefieldManager.hasInitializedBattlefieldView = true;
            RefreshBoardZoneLayout();
        }

        void CaptureInitialBattlefieldContentOffset()
        {
            if (battlefieldManager == null || battlefieldManager.content == null) return;

            _initialBattlefieldContentOffsetX = battlefieldManager.content.anchoredPosition.x;
            _hasInitialBattlefieldContentOffset = true;
        }

        void ConfigureLaneClickTargets()
        {
            if (battlefieldManager == null) return;

            var lanes = battlefieldManager.GetAllLanes();
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                ConfigureLaneClickTarget(lane.playerSlot, lane, UcgPlayerSide.Player);
                ConfigureLaneClickTarget(lane.opponentSlot, lane, UcgPlayerSide.Opponent);
            }
        }

        void ConfigureLaneClickTarget(RectTransform slot, UcgBattleLane lane, UcgPlayerSide targetSide)
        {
            if (slot == null || lane == null) return;

            var clickTarget = slot.GetComponent<UcgLaneClickTarget>();
            if (clickTarget == null) clickTarget = slot.gameObject.AddComponent<UcgLaneClickTarget>();

            clickTarget.demo = this;
            clickTarget.ownerLane = lane;
            clickTarget.targetSide = targetSide;
        }

        void EnsureSceneSlots()
        {
            DisableLegacySceneSlot("Player Scene Slot");
            DisableLegacySceneSlot("Opponent Scene Slot");

            sharedSceneSlot = EnsureSharedSceneSlot(
                "SharedSceneSlot",
                GetReferenceSceneAreaPosition(),
                new Color(0.035f, 0.065f, 0.085f, 0.12f));

            ApplyPortraitBattlefieldLayout();
        }

        float GetCombatAreaOffsetX()
        {
            return combatAreaOffsetX;
        }

        Vector2 GetBattleSlotSize()
        {
            return GetPortraitCardSlotSize();
        }

        Vector2 GetOpponentBattleSlotSize()
        {
            return GetPortraitCardSlotSize();
        }

        Vector2 GetPlacedBattleCardSize()
        {
            return GetPortraitCardSlotSize();
        }

        Vector2 GetOpponentCardBoardSize()
        {
            return GetPlacedBattleCardSize();
        }

        float GetBattleLaneWidth()
        {
            Vector2 portraitSize = GetPortraitCardSlotSize();
            float horizontalCardWidth = portraitSize.y;
            float safeWidth = horizontalCardWidth + Mathf.Max(0f, horizontalCardSafePadding);
            return Mathf.Clamp(Mathf.Max(portraitSize.x, safeWidth), 190f, 340f);
        }

        float GetBattleLaneSpacing()
        {
            return Mathf.Clamp(Mathf.Max(minLaneGap, laneGapForHorizontalCard, boardZoneSectionGap), 24f, 72f);
        }

        Vector2 GetPortraitCardSlotSize()
        {
            float requestedWidth = boardCardSlotWidth > 0f ? boardCardSlotWidth : portraitSlotWidth;
            float requestedHeight = boardCardSlotHeight > 0f ? boardCardSlotHeight : portraitSlotHeight;
            float width = Mathf.Clamp(requestedWidth, 140f, 210f);
            float height = Mathf.Clamp(requestedHeight, 190f, 286f);
            return new Vector2(width, height);
        }

        float GetSceneAreaOffsetX()
        {
            return fieldColumnX + GetCombatAreaOffsetX() * Mathf.Clamp(sceneAreaOffsetRatio, 0f, 1f);
        }

        Vector2 GetReferenceSceneAreaPosition()
        {
            return useFixedReferenceBoardLayout
                ? new Vector2(referenceSceneAreaPos.x, sceneAreaY)
                : new Vector2(GetSceneAreaOffsetX(), sceneAreaY);
        }

        Vector2 GetReferenceOpponentBattleSlotPosition()
        {
            float sceneCenterY = GetReferenceSceneAreaPosition().y;
            float y = sceneCenterY
                + GetSceneAreaSize().y * 0.5f
                + Mathf.Max(0f, sceneToOpponentLaneGap)
                + GetBattleSlotSize().y * 0.5f;
            return new Vector2(referenceOpponentBattleSlotPos.x, y);
        }

        Vector2 GetReferencePlayerBattleSlotPosition()
        {
            float sceneCenterY = GetReferenceSceneAreaPosition().y;
            float y = sceneCenterY
                - GetSceneAreaSize().y * 0.5f
                - Mathf.Max(0f, sceneToPlayerLaneGap)
                - GetBattleSlotSize().y * 0.5f;
            return new Vector2(referencePlayerBattleSlotPos.x, y);
        }

        void ApplyReferenceBattleSlotLayout()
        {
            if (!useFixedReferenceBoardLayout || battlefieldManager == null) return;

            Vector2 opponentSlotPosition = GetReferenceOpponentBattleSlotPosition();
            Vector2 playerSlotPosition = GetReferencePlayerBattleSlotPosition();

            var lanes = battlefieldManager.GetAllLanes();
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                lane.ApplyReferenceSlotLayout(opponentSlotPosition, playerSlotPosition);
            }
        }

        float GetRevealSelectionOffsetX()
        {
            return Mathf.Clamp(GetSceneAreaOffsetX(), -110f, 0f);
        }

        void ApplyPortraitBattlefieldLayout()
        {
            if (battlefieldManager != null)
            {
                battlefieldManager.combatAreaOffsetX = GetCombatAreaOffsetX();
                ApplyCombatFocusViewportPosition("ApplyPortraitBattlefieldLayout");
                battlefieldManager.rightAuxiliaryColumnGutterWidth = rightAuxiliaryColumnGutterWidth;
                battlefieldManager.debugBattlefieldLayout = debugBattlefieldLayout || debugLayoutDiagnostics;
                battlefieldManager.hasInitializedBattlefieldView = _hasInitializedBattlefieldView;
                ApplyReferenceBattleSlotLayout();
            }

            if (sceneZoneAnchor != null)
            {
                sceneZoneAnchor.anchoredPosition = GetReferenceSceneAreaPosition();
                sceneZoneAnchor.sizeDelta = GetSceneAreaSize();
                if (sharedSceneSlot != null)
                {
                    sharedSceneSlot.sceneCardSize = GetSceneCardBoardSize();
                }
                EnsureSceneZoneMatFrame(sceneZoneAnchor.parent as RectTransform, sceneZoneAnchor.anchoredPosition, sceneZoneAnchor.sizeDelta);
            }

            RefreshBoardZoneLayout();
        }

        float GetEffectiveCombatFocusViewportPosition()
        {
            float formalFocus = Mathf.Clamp01(combatFocusViewportPosition);
            return debugCombatViewportOffset > 0f
                ? Mathf.Clamp01(debugCombatViewportOffset)
                : formalFocus;
        }

        void ApplyCombatFocusViewportPosition(string source)
        {
            if (battlefieldManager == null) return;

            float effectiveFocus = GetEffectiveCombatFocusViewportPosition();
            battlefieldManager.focusViewportPosition = effectiveFocus;
            _lastAppliedCombatFocusViewportPosition = effectiveFocus;
            _lastSeenDebugCombatViewportOffset = debugCombatViewportOffset;
        }

        void UpdateDebugCombatViewportOffset()
        {
            if (battlefieldManager == null) return;

            float effectiveFocus = GetEffectiveCombatFocusViewportPosition();
            bool offsetChanged = Mathf.Abs(debugCombatViewportOffset - _lastSeenDebugCombatViewportOffset) > 0.0001f;
            bool focusMismatch = Mathf.Abs(battlefieldManager.focusViewportPosition - effectiveFocus) > 0.0001f
                || Mathf.Abs(_lastAppliedCombatFocusViewportPosition - effectiveFocus) > 0.0001f;

            if (!offsetChanged && !focusMismatch) return;

            ApplyCombatFocusViewportPosition("UpdateDebugCombatViewportOffset");

            int activeLaneIndex = GetCurrentActiveLaneIndex();
            if (IsCombatFocusPhase())
            {
                battlefieldManager.SmoothFocusActiveLane(activeLaneIndex);
                LogCombatViewportDiagnostic("UpdateDebugCombatViewportOffset", "FocusLane", activeLaneIndex, true);
            }
            else
            {
                LogCombatViewportDiagnostic("UpdateDebugCombatViewportOffset", "OverviewAll", activeLaneIndex, true);
            }
        }

        bool IsCombatFocusPhase()
        {
            if (phaseManager == null) return false;

            return phaseManager.CurrentPhase == UcgGamePhase.SceneSetup
                || phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                || phaseManager.CurrentPhase == UcgGamePhase.Upgrade;
        }

        int GetCurrentActiveLaneIndex()
        {
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : 0;
            return Mathf.Max(0, activeLaneIndex);
        }

        bool ShouldLogCombatViewportDiagnostic()
        {
            return debugBoardZones
                || debugBattlefieldLayout
                || debugCombatViewportOffset > 0f;
        }

        void LogCombatViewportDiagnostic(string source, string focusMode, int laneIndex, bool force = false)
        {
            if (!force && !ShouldLogCombatViewportDiagnostic()) return;
            if (battlefieldManager == null) return;

            RectTransform viewportRect = battlefieldManager.viewport;
            RectTransform contentRect = battlefieldManager.content;
            float formalFocus = Mathf.Clamp01(combatFocusViewportPosition);
            float focusCenterFinal = GetEffectiveCombatFocusViewportPosition();
            float managerFocus = battlefieldManager.focusViewportPosition;
            float viewportWidth = viewportRect != null && viewportRect.rect.width > 0f ? viewportRect.rect.width : 1040f;
            float contentScale = contentRect != null ? contentRect.localScale.x : 1f;
            float contentX = contentRect != null ? contentRect.anchoredPosition.x : float.MinValue;
            int clampedLaneIndex = Mathf.Clamp(laneIndex, 0, Mathf.Max(0, battlefieldManager.maxLaneCount - 1));
            float focusTargetBeforeClamp = GetDiagnosticFocusTargetX(clampedLaneIndex, focusCenterFinal);
            float focusTargetAfterClamp = GetDiagnosticClampedContentTargetX(focusTargetBeforeClamp, 1f);
            float focusTargetContentX = focusTargetAfterClamp + battlefieldManager.combatAreaOffsetX;
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int overviewLaneCount = battlefieldManager.GetOverviewTargetLaneCount(currentTurn);
            float overviewScale = GetDiagnosticOverviewScale(overviewLaneCount);
            float overviewTargetBeforeClamp = GetDiagnosticOverviewTargetX(overviewScale, overviewLaneCount);
            float overviewTargetAfterClamp = GetDiagnosticClampedContentTargetX(overviewTargetBeforeClamp, overviewScale);
            float overviewTargetContentX = overviewTargetAfterClamp + battlefieldManager.combatAreaOffsetX;

            Debug.Log(
                "[UCG Camera] Combat viewport diagnostic\n"
                + $"source={source}\n"
                + $"focusMode={focusMode}\n"
                + $"phase={(phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None")}\n"
                + $"turn={currentTurn}\n"
                + $"activeLaneIndex={clampedLaneIndex}\n"
                + $"formalCombatFocusViewportPosition={formalFocus:0.00}\n"
                + $"debugCombatViewportOffset={debugCombatViewportOffset:0.00}\n"
                + $"focusCenterFinal={focusCenterFinal:0.00}\n"
                + $"battlefieldManager.focusViewportPosition={managerFocus:0.00}\n"
                + $"showOverviewUsesFocusCenter=False\n"
                + $"viewportWidth={viewportWidth:0.#}\n"
                + $"viewportX={FormatRectXDiagnostic(viewportRect)}\n"
                + $"contentX={FormatDebugFloat(contentX)}\n"
                + $"contentScale={contentScale:0.00}\n"
                + $"contentWorldX={FormatWorldRect(contentRect)}\n"
                + $"combatAreaOffsetX={battlefieldManager.combatAreaOffsetX:0.#}\n"
                + $"focusTargetBeforeClamp={focusTargetBeforeClamp:0.#}\n"
                + $"focusTargetAfterClamp={focusTargetAfterClamp:0.#}\n"
                + $"focusTargetContentX={focusTargetContentX:0.#}\n"
                + $"overviewLaneCount={overviewLaneCount}\n"
                + $"overviewScale={overviewScale:0.00}\n"
                + $"overviewTargetBeforeClamp={overviewTargetBeforeClamp:0.#}\n"
                + $"overviewTargetAfterClamp={overviewTargetAfterClamp:0.#}\n"
                + $"overviewTargetContentX={overviewTargetContentX:0.#}");
        }

        float GetDiagnosticFocusTargetX(int laneIndex, float focusViewportPosition)
        {
            if (battlefieldManager == null) return float.MinValue;

            float viewportWidth = battlefieldManager.viewport != null && battlefieldManager.viewport.rect.width > 0f
                ? battlefieldManager.viewport.rect.width
                : 1040f;
            float laneCenter = GetDiagnosticLaneLeftX(laneIndex) + battlefieldManager.laneSize.x * 0.5f;
            return viewportWidth * (1f - Mathf.Clamp01(focusViewportPosition)) - laneCenter;
        }

        float GetDiagnosticOverviewScale(int laneCount)
        {
            if (battlefieldManager == null) return 1f;

            float viewportWidth = battlefieldManager.viewport != null && battlefieldManager.viewport.rect.width > 0f
                ? battlefieldManager.viewport.rect.width
                : 1040f;
            float targetWidth = GetDiagnosticContentWidth(Mathf.Clamp(laneCount, 1, Mathf.Max(1, battlefieldManager.maxLaneCount)));
            if (targetWidth <= 0f) return 1f;

            float fitScale = viewportWidth / targetWidth;
            return Mathf.Clamp(fitScale, battlefieldManager.overviewScale, 1f);
        }

        float GetDiagnosticOverviewTargetX(float scale, int laneCount)
        {
            if (battlefieldManager == null) return float.MinValue;

            float viewportWidth = battlefieldManager.viewport != null && battlefieldManager.viewport.rect.width > 0f
                ? battlefieldManager.viewport.rect.width
                : 1040f;
            int clampedLaneCount = Mathf.Clamp(laneCount, 1, Mathf.Max(1, battlefieldManager.maxLaneCount));
            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;

            for (int i = 0; i < clampedLaneCount; i++)
            {
                float left = GetDiagnosticLaneLeftX(i);
                float right = left + battlefieldManager.laneSize.x;
                groupLeft = Mathf.Min(groupLeft, left);
                groupRight = Mathf.Max(groupRight, right);
            }

            if (groupLeft == float.MaxValue)
            {
                groupLeft = 0f;
                groupRight = battlefieldManager.laneSize.x;
            }

            float groupCenter = (groupLeft + groupRight) * 0.5f * Mathf.Max(0.1f, scale);
            return viewportWidth * 0.5f - groupCenter;
        }

        float GetDiagnosticLaneLeftX(int laneIndex)
        {
            if (battlefieldManager == null) return 0f;

            int laneCount = Mathf.Max(1, battlefieldManager.maxLaneCount);
            int visualOrder = Mathf.Clamp(laneCount - 1 - laneIndex, 0, laneCount - 1);
            float laneStep = battlefieldManager.laneSize.x + battlefieldManager.laneSpacing;
            return visualOrder * laneStep;
        }

        float GetDiagnosticClampedContentTargetX(float targetX, float scale)
        {
            if (battlefieldManager == null) return targetX;

            float maxScrollX = GetDiagnosticMaxScrollX(scale);
            return Mathf.Clamp(targetX, -maxScrollX, 0f);
        }

        float GetDiagnosticMaxScrollX(float scale)
        {
            if (battlefieldManager == null) return 0f;

            float viewportWidth = battlefieldManager.viewport != null && battlefieldManager.viewport.rect.width > 0f
                ? battlefieldManager.viewport.rect.width
                : 1040f;
            float contentWidth = battlefieldManager.content != null && battlefieldManager.content.rect.width > 0f
                ? battlefieldManager.content.rect.width
                : GetDiagnosticContentWidth(Mathf.Max(1, battlefieldManager.maxLaneCount));
            return Mathf.Max(0f, contentWidth * Mathf.Max(0.1f, scale) - viewportWidth);
        }

        float GetDiagnosticContentWidth(int laneCount)
        {
            if (battlefieldManager == null || laneCount <= 0) return 0f;

            return battlefieldManager.laneSize.x * laneCount
                + battlefieldManager.laneSpacing * Mathf.Max(0, laneCount - 1)
                + Mathf.Max(0f, battlefieldManager.rightAuxiliaryColumnGutterWidth);
        }

        string FormatRectXDiagnostic(RectTransform rect)
        {
            if (rect == null) return "missing";

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = corners[0].x;
            float maxX = corners[0].x;
            for (int i = 1; i < corners.Length; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                maxX = Mathf.Max(maxX, corners[i].x);
            }

            return $"anchoredX={rect.anchoredPosition.x:0.#}, worldX=({minX:0.#},{maxX:0.#})";
        }

        void DisableLegacySceneSlot(string slotName)
        {
            Transform legacySlot = canvas.transform.Find(slotName);
            if (legacySlot != null)
            {
                legacySlot.gameObject.SetActive(false);
            }
        }

        void EnsurePendingConfirmDialog()
        {
            const string rootName = "ConfirmModalLayer";
            Transform existingRoot = canvas.transform.Find(rootName);
            if (existingRoot == null)
            {
                existingRoot = canvas.transform.Find("Pending Action Confirm");
                if (existingRoot != null)
                {
                    existingRoot.name = rootName;
                }
            }

            if (existingRoot == null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                rootObject.transform.SetParent(canvas.transform, false);
                _pendingConfirmRoot = rootObject.GetComponent<RectTransform>();
            }
            else
            {
                _pendingConfirmRoot = existingRoot as RectTransform;
                if (existingRoot.GetComponent<Canvas>() == null) existingRoot.gameObject.AddComponent<Canvas>();
                if (existingRoot.GetComponent<GraphicRaycaster>() == null) existingRoot.gameObject.AddComponent<GraphicRaycaster>();
            }

            _pendingConfirmRoot.SetParent(canvas.transform, false);
            _pendingConfirmRoot.anchorMin = Vector2.zero;
            _pendingConfirmRoot.anchorMax = Vector2.one;
            _pendingConfirmRoot.pivot = new Vector2(0.5f, 0.5f);
            _pendingConfirmRoot.offsetMin = Vector2.zero;
            _pendingConfirmRoot.offsetMax = Vector2.zero;
            _pendingConfirmRoot.localScale = Vector3.one;
            _pendingConfirmRoot.localEulerAngles = Vector3.zero;
            _pendingConfirmRoot.SetAsLastSibling();

            var rootImage = _pendingConfirmRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Color.clear;
                rootImage.raycastTarget = false;
            }

            var modalCanvas = _pendingConfirmRoot.GetComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 20000;

            Font font = LoadPlaceholderFont();
            RectTransform dimRect = EnsureModalDimBackground(_pendingConfirmRoot);
            var backdrop = dimRect.GetComponent<UcgPendingConfirmBackdrop>();
            backdrop.demo = this;

            RectTransform panelRect = EnsureModalConfirmPanel(_pendingConfirmRoot, font);
            var panelBlocker = panelRect.GetComponent<UcgPendingConfirmBackdrop>();
            if (panelBlocker == null) panelBlocker = panelRect.gameObject.AddComponent<UcgPendingConfirmBackdrop>();
            panelBlocker.demo = null;

            Transform textTransform = panelRect.Find("Confirm Text");
            _pendingConfirmText = textTransform != null ? textTransform.GetComponent<Text>() : null;

            Transform buttonTransform = panelRect.Find("Confirm Button");
            _pendingConfirmButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;

            if (_pendingConfirmButton != null)
            {
                _pendingConfirmButton.interactable = true;
                _pendingConfirmButton.onClick.RemoveListener(ConfirmPendingAction);
                _pendingConfirmButton.onClick.AddListener(ConfirmPendingAction);
            }

            _pendingConfirmRoot.gameObject.SetActive(false);
        }

        void EnsureGameOverModal()
        {
            const string rootName = "GameOverModalLayer";
            Transform existingRoot = canvas.transform.Find(rootName);
            if (existingRoot == null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                rootObject.transform.SetParent(canvas.transform, false);
                _gameOverModalRoot = rootObject.GetComponent<RectTransform>();
            }
            else
            {
                _gameOverModalRoot = existingRoot as RectTransform;
                if (existingRoot.GetComponent<Canvas>() == null) existingRoot.gameObject.AddComponent<Canvas>();
                if (existingRoot.GetComponent<GraphicRaycaster>() == null) existingRoot.gameObject.AddComponent<GraphicRaycaster>();
            }

            _gameOverModalRoot.SetParent(canvas.transform, false);
            _gameOverModalRoot.anchorMin = Vector2.zero;
            _gameOverModalRoot.anchorMax = Vector2.one;
            _gameOverModalRoot.pivot = new Vector2(0.5f, 0.5f);
            _gameOverModalRoot.offsetMin = Vector2.zero;
            _gameOverModalRoot.offsetMax = Vector2.zero;
            _gameOverModalRoot.localScale = Vector3.one;
            _gameOverModalRoot.localEulerAngles = Vector3.zero;
            _gameOverModalRoot.SetAsLastSibling();

            var rootCanvas = _gameOverModalRoot.GetComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = 21000;

            Font font = LoadPlaceholderFont();
            EnsureGameOverDimBackground(_gameOverModalRoot);
            RectTransform panelRect = EnsureGameOverPanel(_gameOverModalRoot, font);

            Transform textTransform = panelRect.Find("Game Over Text");
            _gameOverModalText = textTransform != null ? textTransform.GetComponent<Text>() : null;

            Transform buttonTransform = panelRect.Find("Game Over Restart Button");
            _gameOverRestartButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            if (_gameOverRestartButton != null)
            {
                _gameOverRestartButton.onClick.RemoveListener(RestartDemo);
                _gameOverRestartButton.onClick.AddListener(RestartDemo);
            }

            _gameOverModalRoot.gameObject.SetActive(false);
        }

        void EnsureGameOverDimBackground(RectTransform root)
        {
            const string dimName = "Game Over Dim Background";
            Transform existingDim = root.Find(dimName);
            RectTransform dimRect;
            Image dimImage;

            if (existingDim == null)
            {
                var dimObject = new GameObject(dimName, typeof(RectTransform), typeof(Image));
                dimObject.transform.SetParent(root, false);
                dimRect = dimObject.GetComponent<RectTransform>();
                dimImage = dimObject.GetComponent<Image>();
            }
            else
            {
                dimRect = existingDim as RectTransform;
                dimImage = existingDim.GetComponent<Image>();
                if (dimImage == null) dimImage = existingDim.gameObject.AddComponent<Image>();
            }

            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimRect.SetAsFirstSibling();
            dimImage.color = new Color(0f, 0f, 0f, 0.58f);
            dimImage.raycastTarget = true;
        }

        RectTransform EnsureGameOverPanel(RectTransform root, Font font)
        {
            const string panelName = "Game Over Panel";
            Transform existingPanel = root.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(root, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(640f, 430f);
            panelRect.SetAsLastSibling();
            panelImage.color = new Color(0.05f, 0.07f, 0.1f, 0.98f);
            panelImage.raycastTarget = true;

            EnsureGameOverText(panelRect, font);
            EnsureGameOverRestartButton(panelRect, font);
            return panelRect;
        }

        void EnsureGameOverText(RectTransform panelRect, Font font)
        {
            const string textName = "Game Over Text";
            Transform existingText = panelRect.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(panelRect, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = new Vector2(0.08f, 0.32f);
            textRect.anchorMax = new Vector2(0.92f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 34;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 34;
            text.raycastTarget = false;
            if (font != null) text.font = font;
        }

        void EnsureGameOverRestartButton(RectTransform panelRect, Font font)
        {
            const string buttonName = "Game Over Restart Button";
            Transform existingButton = panelRect.Find(buttonName);
            RectTransform buttonRect;
            Button button;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panelRect, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                button = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                button = existingButton.GetComponent<Button>();
                if (button == null) button = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(0.5f, 0.12f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.12f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(240f, 72f);

            var image = button.GetComponent<Image>();
            image.color = new Color(0.18f, 0.42f, 0.7f, 0.96f);
            image.raycastTarget = true;
            button.targetGraphic = image;
            EnsureButtonLabel(buttonRect, "重新開始");
        }

        RectTransform EnsureModalDimBackground(RectTransform root)
        {
            const string dimName = "DimBackground";
            Transform existingDim = root.Find(dimName);
            RectTransform dimRect;
            Image dimImage;

            if (existingDim == null)
            {
                var dimObject = new GameObject(dimName, typeof(RectTransform), typeof(Image), typeof(UcgPendingConfirmBackdrop));
                dimObject.transform.SetParent(root, false);
                dimRect = dimObject.GetComponent<RectTransform>();
                dimImage = dimObject.GetComponent<Image>();
            }
            else
            {
                dimRect = existingDim as RectTransform;
                dimImage = existingDim.GetComponent<Image>();
                if (dimImage == null) dimImage = existingDim.gameObject.AddComponent<Image>();
                if (existingDim.GetComponent<UcgPendingConfirmBackdrop>() == null) existingDim.gameObject.AddComponent<UcgPendingConfirmBackdrop>();
            }

            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimRect.SetAsFirstSibling();
            dimImage.color = new Color(0f, 0f, 0f, 0.55f);
            dimImage.raycastTarget = true;
            return dimRect;
        }

        RectTransform EnsureModalConfirmPanel(RectTransform root, Font font)
        {
            const string panelName = "Confirm Panel";
            Transform existingPanel = root.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(UcgPendingConfirmBackdrop));
                panelObject.transform.SetParent(root, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(560f, 270f);
            panelRect.SetAsLastSibling();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            panelImage.raycastTarget = true;

            EnsureModalConfirmText(panelRect, font);
            EnsureModalConfirmButton(panelRect, font);
            return panelRect;
        }

        void EnsureModalConfirmText(RectTransform panelRect, Font font)
        {
            const string textName = "Confirm Text";
            Transform existingText = panelRect.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(panelRect, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = new Vector2(0.08f, 0.48f);
            textRect.anchorMax = new Vector2(0.92f, 0.88f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 30;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 30;
            text.raycastTarget = false;
            if (font != null) text.font = font;
        }

        void EnsureModalConfirmButton(RectTransform panelRect, Font font)
        {
            const string buttonName = "Confirm Button";
            Transform existingButton = panelRect.Find(buttonName);
            RectTransform buttonRect;
            Button button;
            Image buttonImage;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panelRect, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                button = buttonObject.GetComponent<Button>();
                buttonImage = buttonObject.GetComponent<Image>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                button = existingButton.GetComponent<Button>();
                if (button == null) button = existingButton.gameObject.AddComponent<Button>();
                buttonImage = existingButton.GetComponent<Image>();
                if (buttonImage == null) buttonImage = existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(0.32f, 0.12f);
            buttonRect.anchorMax = new Vector2(0.68f, 0.36f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            buttonImage.color = new Color(0.18f, 0.55f, 0.9f, 1f);
            buttonImage.raycastTarget = true;
            button.targetGraphic = buttonImage;
            button.interactable = true;

            Transform existingText = buttonRect.Find("Text");
            RectTransform buttonTextRect;
            Text buttonText;
            if (existingText == null)
            {
                var buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                buttonTextObject.transform.SetParent(buttonRect, false);
                buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
                buttonText = buttonTextObject.GetComponent<Text>();
            }
            else
            {
                buttonTextRect = existingText as RectTransform;
                buttonText = existingText.GetComponent<Text>();
                if (buttonText == null) buttonText = existingText.gameObject.AddComponent<Text>();
            }

            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            buttonText.text = "確定";
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.fontSize = 28;
            buttonText.raycastTarget = false;
            if (font != null) buttonText.font = font;
        }

        UcgSceneSlot EnsureSharedSceneSlot(string slotName, Vector2 anchoredPosition, Color color)
        {
            RectTransform sceneAreaRoot = EnsureSharedSceneAreaRoot();
            Transform existingSlot = sceneAreaRoot.Find(slotName);
            if (existingSlot == null)
            {
                existingSlot = canvas.transform.Find(slotName);
            }
            if (existingSlot == null && battlefieldManager != null)
            {
                existingSlot = battlefieldManager.transform.Find(slotName);
            }

            RectTransform slotRect;
            Image slotImage;
            UcgSceneSlot sceneSlot;

            if (existingSlot == null)
            {
                var slotObject = new GameObject(slotName, typeof(RectTransform), typeof(Image), typeof(UcgSceneSlot));
                slotObject.transform.SetParent(sceneAreaRoot, false);
                slotRect = slotObject.GetComponent<RectTransform>();
                slotImage = slotObject.GetComponent<Image>();
                sceneSlot = slotObject.GetComponent<UcgSceneSlot>();
            }
            else
            {
                slotRect = existingSlot as RectTransform;
                slotRect.SetParent(sceneAreaRoot, false);
                slotImage = existingSlot.GetComponent<Image>();
                if (slotImage == null) slotImage = existingSlot.gameObject.AddComponent<Image>();
                sceneSlot = existingSlot.GetComponent<UcgSceneSlot>();
                if (sceneSlot == null) sceneSlot = existingSlot.gameObject.AddComponent<UcgSceneSlot>();
            }

            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = anchoredPosition;
            slotRect.sizeDelta = GetSceneAreaSize();
            slotRect.gameObject.SetActive(true);
            sceneZoneAnchor = slotRect;
            EnsureSceneZoneMatFrame(sceneAreaRoot, anchoredPosition, slotRect.sizeDelta);

            slotImage.color = Color.clear;
            slotImage.enabled = false;
            slotImage.raycastTarget = false;
            var slotOutline = slotRect.GetComponent<Outline>();
            if (slotOutline == null) slotOutline = slotRect.gameObject.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.48f, 0.88f, 1f, 0.03f);
            slotOutline.effectDistance = new Vector2(1.5f, -1.5f);
            slotOutline.enabled = false;

            sceneSlot.backgroundImage = slotImage;
            sceneSlot.normalColor = color;
            sceneSlot.hoverColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.16f);
            sceneSlot.validColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.52f);
            sceneSlot.invalidColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.42f);
            sceneSlot.sceneCardSize = GetSceneCardBoardSize();
            sceneSlot.debugSceneSlotVerbose = debugSceneSlotVerbose;
            sceneSlot.debugSceneDiagnostics = debugSceneSlotVerbose || debugLayoutDiagnostics;
            sceneSlot.Initialize(this, cardInfoPanel, LoadPlaceholderFont());
            sceneSlot.SetDropRaycastEnabled(false);
            sceneSlot.SetHighlight(false, false);
            LogBoardZoneDebug("EnsureSharedSceneSlot", true);
            if (debugLayoutDiagnostics)
            {
                Debug.Log($"SharedSceneSlot DropArea rect: anchoredPosition={slotRect.anchoredPosition}, sizeDelta={slotRect.sizeDelta}, rect={slotRect.rect}");
                LogLane3PlayerSlotRect();
            }
            return sceneSlot;
        }

        Vector2 GetSceneAreaSize()
        {
            float scale = Mathf.Clamp(sceneAreaScale, 0.75f, 1f);
            float width = Mathf.Clamp(sceneAreaWidth, 420f, 640f);
            float height = Mathf.Clamp(sceneAreaHeight, 170f, 260f);
            return new Vector2(width * scale, height * scale);
        }

        Vector2 GetSceneCardBoardSize()
        {
            float scale = Mathf.Clamp(sceneAreaScale, 0.75f, 1f);
            float width = Mathf.Clamp(sceneAreaWidth - 40f, 360f, 600f);
            float height = Mathf.Clamp(sceneAreaHeight - 26f, 144f, 238f);
            return new Vector2(width * scale, height * scale);
        }

        void EnsureSceneZoneMatFrame(RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            if (parent == null) return;

            const string frameName = "Scene Area Mat Frame";
            Transform existingFrame = parent.Find(frameName);
            RectTransform frameRect;
            Image frameImage;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(parent, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                if (existingFrame.GetComponent<Outline>() == null) existingFrame.gameObject.AddComponent<Outline>();
            }

            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = anchoredPosition;
            frameRect.sizeDelta = size;
            frameRect.localScale = Vector3.one;
            frameRect.localEulerAngles = Vector3.zero;
            frameRect.SetAsFirstSibling();

            frameImage.color = debugBoardZones
                ? new Color(0.025f, 0.16f, 0.22f, 0.24f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, sceneAreaAlpha);
            frameImage.raycastTarget = false;

            Outline outline = frameRect.GetComponent<Outline>();
            outline.effectColor = debugBoardZones
                ? new Color(0.72f, 1f, 1f, 0.9f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, sceneAreaOutlineAlpha);
            outline.effectDistance = new Vector2(1.3f, -1.3f);

            EnsureSceneZoneInnerFrame(frameRect);
            Text label = EnsureSceneZoneLabel(frameRect);
            label.text = "SCENE";
        }

        void EnsureSceneZoneInnerFrame(RectTransform parent)
        {
            const string frameName = "Scene Card Inner Frame";
            Transform existingFrame = parent.Find(frameName);
            RectTransform frameRect;
            Image frameImage;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(parent, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                if (existingFrame.GetComponent<Outline>() == null) existingFrame.gameObject.AddComponent<Outline>();
            }

            frameRect.anchorMin = new Vector2(0.045f, 0.14f);
            frameRect.anchorMax = new Vector2(0.955f, 0.86f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frameRect.SetAsFirstSibling();
            frameImage.color = debugBoardZones
                ? new Color(0.04f, 0.24f, 0.28f, 0.24f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.055f);
            frameImage.raycastTarget = false;

            Outline outline = frameRect.GetComponent<Outline>();
            outline.effectColor = debugBoardZones
                ? new Color(0.78f, 1f, 1f, 0.82f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.22f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
        }

        Text EnsureSceneZoneLabel(RectTransform parent)
        {
            const string labelName = "Scene Area Label";
            Transform existingLabel = parent.Find(labelName);
            RectTransform labelRect;
            Text label;

            if (existingLabel == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(parent, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                label = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existingLabel as RectTransform;
                label = existingLabel.GetComponent<Text>();
                if (label == null) label = existingLabel.gameObject.AddComponent<Text>();
            }

            labelRect.anchorMin = new Vector2(0.08f, 0.39f);
            labelRect.anchorMax = new Vector2(0.92f, 0.61f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = debugBoardZones
                ? new Color(0.9f, 1f, 1f, 0.95f)
                : new Color(0.78f, 0.98f, 1f, 0.38f);
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 15;
            label.raycastTarget = false;
            Font font = LoadPlaceholderFont();
            if (font != null) label.font = font;
            return label;
        }

        void LogLane3PlayerSlotRect()
        {
            if (battlefieldManager == null) return;

            UcgBattleLane lane3 = battlefieldManager.GetLane(2);
            UcgPlayArea playArea = lane3 != null ? lane3.GetPlayerPlayArea() : null;
            RectTransform playRect = playArea != null ? playArea.transform as RectTransform : null;
            if (playRect == null) return;

            Debug.Log($"Lane 3 PlayerSlot rect: anchoredPosition={playRect.anchoredPosition}, sizeDelta={playRect.sizeDelta}, rect={playRect.rect}");
        }

        RectTransform EnsureSharedSceneAreaRoot()
        {
            const string areaName = "SharedSceneAreaRoot";
            RectTransform boardRoot = EnsureBattlefieldZoneRoot();
            RectTransform parentRoot = EnsureCombatBoardRegionRoot(boardRoot);
            if (parentRoot == null && canvas != null)
            {
                parentRoot = canvas.transform as RectTransform;
            }

            Transform existingArea = parentRoot != null ? parentRoot.Find(areaName) : null;
            if (existingArea == null && boardRoot != null)
            {
                existingArea = boardRoot.Find(areaName);
            }
            if (existingArea == null && battlefieldManager != null && battlefieldManager.content != null)
            {
                existingArea = battlefieldManager.content.Find(areaName);
            }
            if (existingArea == null && battlefieldManager != null && battlefieldManager.transform != null)
            {
                existingArea = battlefieldManager.transform.Find(areaName);
            }
            if (existingArea == null && canvas != null)
            {
                existingArea = canvas.transform.Find(areaName);
            }
            RectTransform areaRect;

            if (existingArea == null)
            {
                var areaObject = new GameObject(areaName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                areaObject.transform.SetParent(parentRoot != null ? parentRoot : canvas.transform, false);
                areaRect = areaObject.GetComponent<RectTransform>();
            }
            else
            {
                areaRect = existingArea as RectTransform;
                if (parentRoot != null && areaRect.parent != parentRoot)
                {
                    areaRect.SetParent(parentRoot, false);
                }
                if (existingArea.GetComponent<Canvas>() == null) existingArea.gameObject.AddComponent<Canvas>();
                if (existingArea.GetComponent<GraphicRaycaster>() == null) existingArea.gameObject.AddComponent<GraphicRaycaster>();
            }

            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.pivot = new Vector2(0.5f, 0.5f);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;
            areaRect.localScale = Vector3.one;
            areaRect.localEulerAngles = Vector3.zero;

            var areaCanvas = areaRect.GetComponent<Canvas>();
            areaCanvas.overrideSorting = false;
            areaCanvas.sortingOrder = 0;

            var areaGraphics = areaRect.GetComponents<Graphic>();
            for (int i = 0; i < areaGraphics.Length; i++)
            {
                areaGraphics[i].raycastTarget = false;
            }

            areaRect.SetAsLastSibling();
            if (debugLayoutDiagnostics) Debug.Log($"SharedSceneBand rect: anchoredPosition={areaRect.anchoredPosition}, sizeDelta={areaRect.sizeDelta}, rect={areaRect.rect}");
            return areaRect;
        }

        void EnsureTurnManager()
        {
            if (turnManager == null)
            {
                turnManager = GetComponent<UcgTurnManager>();
            }

            if (turnManager == null)
            {
                turnManager = gameObject.AddComponent<UcgTurnManager>();
            }

            turnManager.maxLaneCount = 8;
            turnManager.turnInfoText = EnsureTurnInfoText();
            turnManager.ResetTurns();
        }

        void EnsurePhaseManager()
        {
            if (phaseManager == null)
            {
                phaseManager = GetComponent<UcgPhaseManager>();
            }

            if (phaseManager == null)
            {
                phaseManager = gameObject.AddComponent<UcgPhaseManager>();
            }

            phaseManager.turnManager = turnManager;
            phaseManager.phaseInfoText = EnsurePhaseInfoText();
            phaseManager.ResetPhase();
        }

        void EnsureDeckManager()
        {
            if (deckManager == null)
            {
                deckManager = GetComponent<UcgDeckManager>();
            }

            if (deckManager == null)
            {
                deckManager = gameObject.AddComponent<UcgDeckManager>();
            }

            deckManager.debugDeckOperation = debugDeckOperation || debugEffectResolution;
            UcgEffectParser.debugDeckOperation = deckManager.debugDeckOperation;
        }

        void EnsureExternalCardServices()
        {
            if (externalCardDatabase == null)
            {
                externalCardDatabase = GetComponent<UcgExternalCardDatabase>();
            }

            if (externalCardDatabase == null)
            {
                externalCardDatabase = gameObject.AddComponent<UcgExternalCardDatabase>();
            }

            if (cardImageLoader == null)
            {
                cardImageLoader = GetComponent<UcgCardImageLoader>();
            }

            if (cardImageLoader == null)
            {
                cardImageLoader = gameObject.AddComponent<UcgCardImageLoader>();
            }

            cardImageLoader.database = externalCardDatabase;
        }

        void EnsureSfxController()
        {
            if (sfxController == null)
            {
                sfxController = FindFirstObjectByType<UcgSfxController>();
            }

            if (sfxController == null)
            {
                var sfxObject = new GameObject("UCG Sfx Controller", typeof(UcgSfxController));
                sfxObject.transform.SetParent(canvas != null ? canvas.transform : transform, false);
                sfxController = sfxObject.GetComponent<UcgSfxController>();
            }
        }

        void EnsureEffectManager()
        {
            if (effectManager == null)
            {
                effectManager = GetComponent<UcgEffectManager>();
            }

            if (effectManager == null)
            {
                effectManager = gameObject.AddComponent<UcgEffectManager>();
            }
        }

        void EnsureOpponentScript()
        {
            if (opponentScript == null)
            {
                opponentScript = GetComponent<UcgOpponentScript>();
            }

            if (opponentScript == null)
            {
                opponentScript = gameObject.AddComponent<UcgOpponentScript>();
            }
        }

        void EnsureTurnOrderManager()
        {
            if (turnOrderManager == null)
            {
                turnOrderManager = GetComponent<UcgTurnOrderManager>();
            }

            if (turnOrderManager == null)
            {
                turnOrderManager = gameObject.AddComponent<UcgTurnOrderManager>();
            }
        }

        Text EnsureDeckCountText()
        {
            EnsureBattlefieldZoneAnchors();
            UpdateDeckCountText();
            return deckCountText;
        }

        void EnsureZoneInfoUI()
        {
            EnsureBattlefieldZoneAnchors();
            RefreshZoneInfoUI();
            SetZoneInfoUIVisible(true);
        }

        void SetZoneInfoUIVisible(bool visible)
        {
            SetZoneAnchorVisible(combatBoardRegionRoot, visible);
            SetZoneAnchorVisible(pileSideRegionRoot, visible);
            SetZoneAnchorVisible(playerSidePileGroup, visible);
            SetZoneAnchorVisible(opponentSidePileGroup, visible);
            SetZoneAnchorVisible(playerDeckAnchor, visible);
            SetZoneAnchorVisible(playerDiscardAnchor, visible);
            SetZoneAnchorVisible(opponentDeckAnchor, visible);
            SetZoneAnchorVisible(opponentDiscardAnchor, visible);
        }

        void SetZoneAnchorVisible(RectTransform anchor, bool visible)
        {
            if (anchor != null)
            {
                anchor.gameObject.SetActive(visible);
            }
        }

        void EnsureBattlefieldZoneAnchors()
        {
            if (canvas == null) return;

            DisableLegacyZoneHud("Deck Count Text");
            DisableLegacyZoneHud("Player Zone Info Button");
            DisableLegacyZoneHud("Opponent Zone Info Button");

            RectTransform root = EnsureBattlefieldZoneRoot();
            RectTransform pileRegion = EnsurePileSideRegionRoot(root);
            Font font = LoadPlaceholderFont();
            Vector2 zoneSize = GetBoardZoneCardSize();
            playerSidePileGroup = EnsureBoardPileGroup(pileRegion, "PlayerSidePileGroup");
            opponentSidePileGroup = EnsureBoardPileGroup(pileRegion, "OpponentSidePileGroup");

            playerDeckAnchor = EnsureBattlefieldZoneFrame(
                root,
                playerSidePileGroup,
                "Player Deck Zone",
                zoneSize,
                "牌庫",
                font,
                out playerDeckZoneText);
            playerDiscardAnchor = EnsureBattlefieldZoneFrame(
                root,
                playerSidePileGroup,
                "Player Discard Zone",
                zoneSize,
                "棄牌區",
                font,
                out playerDiscardZoneText);
            opponentDeckAnchor = EnsureBattlefieldZoneFrame(
                root,
                opponentSidePileGroup,
                "Opponent Deck Zone",
                zoneSize,
                "牌庫",
                font,
                out opponentDeckZoneText);
            opponentDiscardAnchor = EnsureBattlefieldZoneFrame(
                root,
                opponentSidePileGroup,
                "Opponent Discard Zone",
                zoneSize,
                "棄牌區",
                font,
                out opponentDiscardZoneText);
            ApplyBoardZoneRootLayout(root);
            ApplyBoardZoneLayoutForPortrait(root, zoneSize);
            HideUnusedFixedBoardHudRoot();
            LogBoardZoneDebug("EnsureBattlefieldZoneAnchors", true);

            deckCountText = playerDeckZoneText;
            opponentZoneText = opponentDeckZoneText;
            playerDiscardButton = DisableZoneButton(playerDiscardAnchor);
            opponentDiscardButton = DisableZoneButton(opponentDiscardAnchor);
        }

        Vector2 GetBoardZoneCardSize()
        {
            Vector2 portraitSize = GetPortraitCardSlotSize();
            float width = pileSlotWidth > 0f ? pileSlotWidth : portraitSize.x;
            float height = pileSlotHeight > 0f ? pileSlotHeight : portraitSize.y;
            return new Vector2(
                Mathf.Clamp(width, 150f, 210f),
                Mathf.Clamp(height, 206f, 286f));
        }

        float GetEffectiveSidePileScale()
        {
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : 0;
            float configuredScale = activeLaneIndex > 0
                ? Mathf.Min(sidePileScale, sidePileFocusedScale)
                : sidePileScale;
            return Mathf.Clamp(configuredScale, 0.5f, 0.8f);
        }

        float GetBoardZoneVerticalGap(Vector2 zoneSize)
        {
            return Mathf.Clamp(deckDiscardGroupGap, 16f, 34f);
        }

        void ApplyReferenceBoardLayout(RectTransform root, Vector2 zoneSize)
        {
            if (root == null) return;

            float beforeX = playerSidePileGroup != null ? playerSidePileGroup.anchoredPosition.x : float.MinValue;

            ApplyPileSideRegionSafeVisibilityLayout(root, zoneSize);
            float groupNudgeX = ApplyPileSideInternalLayout(zoneSize, "ReferenceBoardLayout");
            ApplyBoardZoneDebugVisualState();
            ApplyDebugSidePileExtremeOffsetToVisibleGroups(debugForceSidePileExtremeOffset);

            float afterX = playerSidePileGroup != null ? playerSidePileGroup.anchoredPosition.x : float.MinValue;
            float finalDebugGroupX = groupNudgeX
                + (debugForceSidePileExtremeOffset ? debugSidePileExtremeOffsetX : 0f);
            UpdateSidePileLayoutDebugValues(
                root,
                zoneSize,
                0f,
                groupNudgeX,
                -groupNudgeX,
                groupNudgeX,
                groupNudgeX,
                groupNudgeX,
                finalDebugGroupX,
                beforeX,
                afterX,
                false,
                false,
                debugForceSidePileExtremeOffset,
                "FixedReference");
        }

        float ApplyPileSideInternalLayout(Vector2 requestedZoneSize, string source)
        {
            if (pileSideRegionRoot == null) return 0f;

            float regionWidth = pileSideRegionRoot.rect.width > 0f ? pileSideRegionRoot.rect.width : pileSideRegionRoot.sizeDelta.x;
            float regionHeight = pileSideRegionRoot.rect.height > 0f ? pileSideRegionRoot.rect.height : pileSideRegionRoot.sizeDelta.y;
            if (regionWidth <= 0f) regionWidth = Mathf.Max(1f, requestedZoneSize.x + 24f);
            if (regionHeight <= 0f) regionHeight = Mathf.Max(1f, requestedZoneSize.y * 4f + deckDiscardGroupGap * 3f + 24f);

            float horizontalPadding = 10f;
            float desiredGap = Mathf.Clamp(deckDiscardGroupGap, 8f, 22f);
            float minVerticalPadding = 4f;
            float maxWidth = Mathf.Max(1f, regionWidth - horizontalPadding * 2f);
            float maxHeight = Mathf.Max(1f, (regionHeight - minVerticalPadding * 2f - desiredGap * 3f) / 4f);
            float scale = Mathf.Min(1f, maxWidth / Mathf.Max(1f, requestedZoneSize.x), maxHeight / Mathf.Max(1f, requestedZoneSize.y));
            Vector2 zoneSize = new Vector2(
                Mathf.Max(1f, requestedZoneSize.x * scale),
                Mathf.Max(1f, requestedZoneSize.y * scale));
            float groupNudgeX = 0f;

            float freeHeight = Mathf.Max(0f, regionHeight - zoneSize.y * 4f - desiredGap * 3f);
            float topPadding = Mathf.Max(minVerticalPadding, freeHeight * 0.5f);
            float bottomPadding = topPadding;
            float firstY = regionHeight * 0.5f - topPadding - zoneSize.y * 0.5f;
            float stepY = zoneSize.y + desiredGap;
            float opDiscardY = firstY;
            float opDeckY = firstY - stepY;
            float playerDeckY = firstY - stepY * 2f;
            float playerDiscardY = firstY - stepY * 3f;

            LayoutPileSideGroupWithTwoZones(
                opponentSidePileGroup,
                opponentDiscardAnchor,
                opDiscardY,
                opponentDeckAnchor,
                opDeckY,
                zoneSize,
                groupNudgeX,
                $"{source}.OpponentPileGroup");
            LayoutPileSideGroupWithTwoZones(
                playerSidePileGroup,
                playerDeckAnchor,
                playerDeckY,
                playerDiscardAnchor,
                playerDiscardY,
                zoneSize,
                groupNudgeX,
                $"{source}.PlayerPileGroup");

            LogPileSideInternalLayout(zoneSize, desiredGap, topPadding, bottomPadding, source);
            return groupNudgeX;
        }

        void LayoutPileSideGroupWithTwoZones(
            RectTransform group,
            RectTransform firstZone,
            float firstWorldLocalY,
            RectTransform secondZone,
            float secondWorldLocalY,
            Vector2 zoneSize,
            float groupX,
            string source)
        {
            if (group == null) return;

            float groupCenterY = (firstWorldLocalY + secondWorldLocalY) * 0.5f;
            float groupHeight = Mathf.Abs(firstWorldLocalY - secondWorldLocalY) + zoneSize.y;
            LayoutBoardPileGroup(group, new Vector2(groupX, groupCenterY), new Vector2(zoneSize.x, groupHeight), source);
            LayoutBoardZone(firstZone, new Vector2(0f, firstWorldLocalY - groupCenterY), zoneSize);
            LayoutBoardZone(secondZone, new Vector2(0f, secondWorldLocalY - groupCenterY), zoneSize);
        }

        void ApplyPileSideRegionSafeVisibilityLayout(RectTransform root, Vector2 zoneSize)
        {
            if (root == null || pileSideRegionRoot == null) return;

            float rootHeight = root.rect.height > 0f ? root.rect.height : 920f;
            Vector2 requestedSize = pileRegionSize;
            float desiredGap = Mathf.Clamp(deckDiscardGroupGap, 12f, 28f);
            float verticalPadding = 6f;
            requestedSize.x = Mathf.Max(requestedSize.x, zoneSize.x + 24f);
            requestedSize.y = Mathf.Min(
                Mathf.Max(requestedSize.y, zoneSize.y * 4f + desiredGap * 3f + verticalPadding * 2f),
                rootHeight);

            float visibleRight = GetSceneLayerPileRightReference(root, requestedSize.x, zoneSize);
            float viewportRight = GetVisibleBattlefieldRightInReference(
                root,
                root.rect.width > 0f ? root.rect.width : 1040f);
            float rightSafeMargin = 20f;
            float baseX = visibleRight == float.MinValue
                ? pileRegionPos.x
                : visibleRight - requestedSize.x * 0.5f - rightSafeMargin;
            float maxSafeX = viewportRight == float.MinValue
                ? baseX + Mathf.Max(0f, sidePileColumnNudgeX)
                : viewportRight - requestedSize.x * 0.5f - rightSafeMargin;
            float afterNudgeX = baseX + sidePileColumnNudgeX;
            float safeX = Mathf.Min(afterNudgeX, maxSafeX);

            Vector2 beforePosition = pileSideRegionRoot.anchoredPosition;
            _lastPileRegionNudgeMethod = "ApplyPileSideRegionSafeVisibilityLayout";
            _lastPileRegionXBeforeMethod = beforePosition.x;
            _lastPileRegionXBeforeNudge = baseX;
            _lastPileRegionXNudgeValue = sidePileColumnNudgeX;
            _lastPileRegionXAfterNudge = afterNudgeX;
            _lastPileRegionXMaxSafeClamp = maxSafeX;
            _lastPileRegionXAfterClamp = safeX;
            _lastPileRegionVisibleRight = visibleRight;
            _lastPileRegionViewportRight = viewportRight;
            _lastPileRegionClampApplied = Mathf.Abs(safeX - afterNudgeX) > 0.1f;
            _lastPileRegionLayoutFrame = Time.frameCount;
            pileSideRegionRoot.anchorMin = new Vector2(0.5f, 0.5f);
            pileSideRegionRoot.anchorMax = new Vector2(0.5f, 0.5f);
            pileSideRegionRoot.pivot = new Vector2(0.5f, 0.5f);
            pileSideRegionRoot.sizeDelta = requestedSize;
            pileSideRegionRoot.anchoredPosition = new Vector2(safeX, pileRegionPos.y);
            pileSideRegionRoot.localScale = Vector3.one;
            pileSideRegionRoot.localEulerAngles = Vector3.zero;
            pileSideRegionRoot.gameObject.SetActive(true);
            pileSideRegionRoot.SetAsLastSibling();
            EnsureVisibleCanvasGroup(pileSideRegionRoot, 1f);
            EnsureBoardRegionVisual(pileSideRegionRoot, "Pile Side Region");
            _lastPileRegionXAfterApply = pileSideRegionRoot.anchoredPosition.x;
        }

        void ApplyBoardZoneLayoutForPortrait(RectTransform root, Vector2 zoneSize)
        {
            if (root == null) return;

            if (useFixedReferenceBoardLayout)
            {
                ApplyReferenceBoardLayout(root, zoneSize);
                return;
            }

            float boardWidth = root.rect.width > 0f ? root.rect.width : 1040f;
            float boardHeight = root.rect.height > 0f ? root.rect.height : 820f;
            float zoneVerticalGap = GetBoardZoneVerticalGap(zoneSize);
            float groupHeight = zoneSize.y * 2f + zoneVerticalGap;
            float baseGroupX = GetCardMatPileColumnX(root, boardWidth, zoneSize);
            float minSectionX = GetSidePileMinXForVisibleLaneGap(root, zoneSize);
            float computedGroupX = minSectionX == float.MinValue
                ? baseGroupX
                : Mathf.Max(baseGroupX, minSectionX);
            float clampMinX = -boardWidth * 0.5f + zoneSize.x * 0.5f + Mathf.Max(0f, boardZoneSectionGap);
            if (minSectionX != float.MinValue)
            {
                clampMinX = Mathf.Max(clampMinX, minSectionX);
            }
            float clampMaxX = GetCardMatPileColumnMaxX(root, boardWidth, zoneSize);
            float nudgedGroupX = computedGroupX + sidePileColumnNudgeX;
            bool extremeOffsetApplied = debugForceSidePileExtremeOffset;
            float beforeClampGroupX = nudgedGroupX;
            float groupX = Mathf.Clamp(beforeClampGroupX, clampMinX, clampMaxX);
            string clampReason = "None";
            if (groupX <= clampMinX + 0.1f && beforeClampGroupX < clampMinX - 0.1f)
            {
                clampReason = "HitMin";
            }
            else if (groupX >= clampMaxX - 0.1f && beforeClampGroupX > clampMaxX + 0.1f)
            {
                clampReason = "HitMax";
            }
            bool computedClamped = groupX < computedGroupX - 0.1f || groupX > computedGroupX + 0.1f;
            bool clamped = computedClamped
                || (groupX < beforeClampGroupX - 0.1f || groupX > beforeClampGroupX + 0.1f);
            float groupYLimit = Mathf.Max(0f, boardHeight * 0.5f - groupHeight * 0.5f - 8f);
            float opponentGroupY = Mathf.Clamp(opponentRowY, 0f, groupYLimit);
            float playerGroupY = Mathf.Clamp(playerRowY, -groupYLimit, 0f);
            float minimumGroupSeparation = groupHeight + Mathf.Max(0f, pileGroupVerticalSeparation);
            if (opponentGroupY - playerGroupY < minimumGroupSeparation)
            {
                float centerY = (opponentGroupY + playerGroupY) * 0.5f;
                opponentGroupY = Mathf.Clamp(centerY + minimumGroupSeparation * 0.5f, 0f, groupYLimit);
                playerGroupY = Mathf.Clamp(centerY - minimumGroupSeparation * 0.5f, -groupYLimit, 0f);
            }
            float stackOffsetY = zoneSize.y * 0.5f + zoneVerticalGap * 0.5f;
            float columnDownShift = GetAppliedRightSidePileColumnDownShift(boardHeight, Mathf.Max(opponentGroupY, -playerGroupY), stackOffsetY, zoneSize);

            Vector2 playerGroupPosition = new Vector2(groupX, playerGroupY - columnDownShift);
            Vector2 opponentGroupPosition = new Vector2(groupX, opponentGroupY - columnDownShift);
            float beforeX = playerSidePileGroup != null ? playerSidePileGroup.anchoredPosition.x : float.MinValue;
            LayoutBoardPileGroup(playerSidePileGroup, playerGroupPosition, new Vector2(zoneSize.x, groupHeight), "LayoutBoardZones.PlayerPileGroup");
            LayoutBoardPileGroup(opponentSidePileGroup, opponentGroupPosition, new Vector2(zoneSize.x, groupHeight), "LayoutBoardZones.OpponentPileGroup");
            LayoutBoardZone(playerDeckAnchor, new Vector2(0f, stackOffsetY), zoneSize);
            LayoutBoardZone(playerDiscardAnchor, new Vector2(0f, -stackOffsetY), zoneSize);
            LayoutBoardZone(opponentDeckAnchor, new Vector2(0f, stackOffsetY), zoneSize);
            LayoutBoardZone(opponentDiscardAnchor, new Vector2(0f, -stackOffsetY), zoneSize);
            ApplyBoardZoneDebugVisualState();
            ApplyDebugSidePileExtremeOffsetToVisibleGroups(extremeOffsetApplied);
            float afterX = playerSidePileGroup != null ? playerSidePileGroup.anchoredPosition.x : float.MinValue;
            float finalDebugGroupX = extremeOffsetApplied ? groupX + debugSidePileExtremeOffsetX : groupX;
            UpdateSidePileLayoutDebugValues(
                root,
                zoneSize,
                baseGroupX,
                computedGroupX,
                clampMinX,
                clampMaxX,
                nudgedGroupX,
                beforeClampGroupX,
                finalDebugGroupX,
                beforeX,
                afterX,
                clamped,
                minSectionX != float.MinValue,
                extremeOffsetApplied,
                clampReason);
        }

        float ResolveSidePileColumnX(RectTransform root, float boardWidth, Vector2 zoneSize, float baseGroupX, out bool clamped, out bool usedGapInFormula)
        {
            usedGapInFormula = false;
            float focusOffsetX = GetCurrentBattlefieldFocusOffsetX();
            float compensatedX = baseGroupX;
            if (sidePileFollowFocus && !IsBoardZoneRootUnderBattlefieldContent(root))
            {
                compensatedX += Mathf.Max(0f, focusOffsetX) * Mathf.Max(0f, sidePileFocusCompensationFactor);
            }

            float rightMargin = GetSidePanelRightMargin();
            float maxX = GetSidePileMaxColumnX(root, boardWidth, zoneSize, rightMargin);
            if (IsBoardZoneRootUnderBattlefieldContent(root))
            {
                float sidePanelColumnX = GetSidePanelColumnX(root, boardWidth, zoneSize);
                float sidePanelMinXForLaneGap = GetSidePileMinXForVisibleLaneGap(root, zoneSize);
                float sidePanelRequestedX = sidePanelMinXForLaneGap == float.MinValue
                    ? sidePanelColumnX
                    : Mathf.Max(sidePanelColumnX, sidePanelMinXForLaneGap);
                clamped = sidePanelRequestedX > maxX + 0.1f;
                return Mathf.Min(sidePanelRequestedX, maxX);
            }

            float minXForLaneGap = GetSidePileMinXForVisibleLaneGap(root, zoneSize);
            float requestedX = Mathf.Max(compensatedX, minXForLaneGap);
            float finalX = Mathf.Min(requestedX, maxX);
            clamped = requestedX > maxX + 0.1f;
            return finalX;
        }

        float GetCardMatPileColumnX(RectTransform root, float boardWidth, Vector2 zoneSize)
        {
            if (IsBoardZoneRootUnderBattlefieldContent(root))
            {
                float sceneColumnX = GetSceneLayerPileColumnX(root, zoneSize);
                if (sceneColumnX != float.MinValue) return sceneColumnX;
            }

            float rightEdge = GetVisibleBattlefieldRightInReference(root, boardWidth);
            float rightInset = Mathf.Clamp(pileColumnRightInset, 24f, 160f);
            return rightEdge - rightInset - zoneSize.x * 0.5f;
        }

        float GetCardMatPileColumnMaxX(RectTransform root, float boardWidth, Vector2 zoneSize)
        {
            if (IsBoardZoneRootUnderBattlefieldContent(root))
            {
                return root != null && root.rect.width > 0f
                    ? root.rect.xMax - zoneSize.x * 0.5f - Mathf.Clamp(sidePileRightMargin, 0f, 40f)
                    : GetCardMatPileColumnX(root, boardWidth, zoneSize);
            }

            float rightEdge = GetVisibleBattlefieldRightInReference(root, boardWidth);
            float safeMargin = Mathf.Clamp(sidePileRightMargin, 0f, 40f);
            return rightEdge - safeMargin - zoneSize.x * 0.5f;
        }

        float GetSceneLayerPileRightReference(RectTransform root, float regionWidth, Vector2 zoneSize)
        {
            if (!IsBoardZoneRootUnderBattlefieldContent(root))
            {
                return GetVisibleBattlefieldRightInReference(root, root != null && root.rect.width > 0f ? root.rect.width : 1040f);
            }

            float columnX = GetSceneLayerPileColumnX(root, zoneSize);
            return columnX == float.MinValue ? float.MinValue : columnX + regionWidth * 0.5f + 20f;
        }

        float GetSceneLayerPileColumnX(RectTransform root, Vector2 zoneSize)
        {
            if (root == null) return float.MinValue;

            float laneRight = GetRightmostLaneRightInReference(root);
            if (laneRight == float.MinValue) return float.MinValue;

            float gap = Mathf.Max(0f, Mathf.Max(Mathf.Max(sidePileMinGapFromLane, combatToPileGapX), sidePileToLaneGap));
            float desiredX = laneRight + gap + zoneSize.x * 0.5f;
            if (root.rect.width <= 0f) return desiredX;

            float maxX = root.rect.xMax - zoneSize.x * 0.5f - Mathf.Clamp(sidePileRightMargin, 0f, 40f);
            return Mathf.Min(desiredX, maxX);
        }

        float GetSidePileMaxColumnX(RectTransform root, float boardWidth, Vector2 zoneSize, float rightMargin)
        {
            if (IsBoardZoneRootUnderBattlefieldContent(root))
            {
                return root != null && root.rect.width > 0f
                    ? root.rect.xMax - zoneSize.x * 0.5f - rightMargin
                    : GetSceneLayerPileColumnX(root, zoneSize);
            }

            float maxBoardX = boardWidth * 0.5f - zoneSize.x * 0.5f - rightMargin;

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            RectTransform boardRect = battlefieldManager != null ? battlefieldManager.transform as RectTransform : root;
            if (canvasRect == null || boardRect == null) return maxBoardX;

            float canvasRightInBoard = boardRect.InverseTransformPoint(canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMax, 0f, 0f))).x;
            return Mathf.Min(maxBoardX + 28f, canvasRightInBoard - zoneSize.x * 0.5f - rightMargin);
        }

        float GetSidePanelRightMargin()
        {
            return Mathf.Clamp(sidePanelRightMargin, 16f, 48f);
        }

        float GetEffectiveSidePanelWidth(Vector2 zoneSize)
        {
            float minimumWidth = zoneSize.x + GetSidePanelRightMargin();
            float maximumWidth = Mathf.Max(minimumWidth, Mathf.Max(0f, rightAuxiliaryColumnGutterWidth));
            return Mathf.Clamp(sidePanelWidth, minimumWidth, maximumWidth);
        }

        float GetSidePanelRightX(RectTransform root, float boardWidth)
        {
            float rightEdge = GetVisibleBattlefieldRightInReference(root, boardWidth);
            return rightEdge - GetSidePanelRightMargin();
        }

        float GetSidePanelLeftX(RectTransform root, float boardWidth, Vector2 zoneSize)
        {
            return GetSidePanelRightX(root, boardWidth) - GetEffectiveSidePanelWidth(zoneSize);
        }

        float GetSidePanelColumnX(RectTransform root, float boardWidth, Vector2 zoneSize)
        {
            return GetSidePanelRightX(root, boardWidth) - zoneSize.x * 0.5f;
        }

        float GetVisibleBattlefieldRightInReference(RectTransform root, float boardWidth)
        {
            if (root != null && battlefieldManager != null && battlefieldManager.viewport != null)
            {
                RectTransform viewportRect = battlefieldManager.viewport;
                Vector3 viewportRightWorld = viewportRect.TransformPoint(new Vector3(viewportRect.rect.xMax, 0f, 0f));
                return root.InverseTransformPoint(viewportRightWorld).x;
            }

            return root != null ? root.rect.xMax : boardWidth * 0.5f;
        }

        float GetSidePileMinXForVisibleLaneGap(RectTransform root, Vector2 zoneSize)
        {
            if (root == null || battlefieldManager == null) return float.MinValue;

            float maxLaneRight = GetRightmostLaneRightInReference(root);

            if (maxLaneRight == float.MinValue) return float.MinValue;
            float sectionGap = Mathf.Max(0f, Mathf.Max(Mathf.Max(sidePileMinGapFromLane, combatToPileGapX), combatToPileGap));
            return maxLaneRight + sectionGap + zoneSize.x * 0.5f;
        }

        float GetRightmostLaneRightInReference(RectTransform root)
        {
            GetLaneRightEdgesInReference(root, out float playerRightEdge, out float opponentRightEdge);
            return Mathf.Max(playerRightEdge, opponentRightEdge);
        }

        void GetLaneRightEdgesInReference(RectTransform root, out float playerRightEdge, out float opponentRightEdge)
        {
            playerRightEdge = float.MinValue;
            opponentRightEdge = float.MinValue;
            if (root == null || battlefieldManager == null || turnManager == null) return;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            if (lanes == null || lanes.Count == 0)
            {
                int laneIndex = Mathf.Clamp(turnManager.ActiveNewLaneIndex, 0, Mathf.Max(0, battlefieldManager.maxLaneCount - 1));
                UcgBattleLane lane = battlefieldManager.GetLane(laneIndex);
                if (lane != null) lanes = new List<UcgBattleLane> { lane };
            }

            if (lanes == null) return;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null || !lane.gameObject.activeInHierarchy) continue;

                playerRightEdge = Mathf.Max(playerRightEdge, GetRectRightInReference(root, lane.playerSlot));
                opponentRightEdge = Mathf.Max(opponentRightEdge, GetRectRightInReference(root, lane.opponentSlot));
            }
        }

        float GetRectRightInReference(RectTransform reference, RectTransform rectTransform)
        {
            if (reference == null || rectTransform == null) return float.MinValue;

            Rect rect = GetRectInReferenceSpace(reference, rectTransform);
            return rect.xMax;
        }

        float GetCurrentBattlefieldFocusOffsetX()
        {
            if (battlefieldManager == null || battlefieldManager.content == null) return 0f;
            if (!_hasInitialBattlefieldContentOffset)
            {
                CaptureInitialBattlefieldContentOffset();
            }

            return battlefieldManager.content.anchoredPosition.x - _initialBattlefieldContentOffsetX;
        }

        void UpdateSidePileLayoutDebugValues(
            RectTransform root,
            Vector2 zoneSize,
            float baseGroupX,
            float computedGroupX,
            float clampMinX,
            float clampMaxX,
            float nudgedGroupX,
            float beforeClampGroupX,
            float finalGroupX,
            float beforeX,
            float afterX,
            bool clamped,
            bool usedGapInFormula,
            bool extremeOffsetApplied,
            string clampReason)
        {
            _lastBattlefieldFocusOffsetX = GetCurrentBattlefieldFocusOffsetX();
            _lastBaseSidePileColumnX = baseGroupX;
            _lastComputedSidePileColumnX = computedGroupX;
            _lastNudgedSidePileColumnX = nudgedGroupX;
            _lastBeforeClampSidePileColumnX = beforeClampGroupX;
            _lastClampMinX = clampMinX;
            _lastClampMaxX = clampMaxX;
            _lastClampedSidePileColumnX = finalGroupX;
            _lastFinalSidePileColumnX = finalGroupX;
            _lastSidePileColumnBeforeX = beforeX;
            _lastSidePileColumnAfterX = afterX;
            _lastSidePileScale = GetEffectiveSidePileScale();
            _lastSidePileMinGapFromLane = sidePileMinGapFromLane;
            _lastSidePileClamped = clamped;
            _lastSidePileUsedGapInFormula = usedGapInFormula;
            _lastSidePileExtremeOffsetApplied = extremeOffsetApplied;
            _lastSidePileClampReason = clampReason;
            _lastSidePileOverwrittenByLayout = afterX != float.MinValue && Mathf.Abs(afterX - finalGroupX) > 0.1f;
            _lastBoardZoneRootAnchoredX = root != null ? root.anchoredPosition.x : float.MinValue;
            _lastBattlefieldContentAnchoredX = battlefieldManager != null && battlefieldManager.content != null
                ? battlefieldManager.content.anchoredPosition.x
                : float.MinValue;
            GetLaneRightEdgesInReference(root, out _lastPlayerLaneRightEdge, out _lastOpponentLaneRightEdge);
            float laneRight = Mathf.Max(_lastPlayerLaneRightEdge, _lastOpponentLaneRightEdge);
            _lastNearestLaneRightEdgeWorld = GetActiveLaneRightWorldX();
            _lastSidePileToLaneGap = laneRight == float.MinValue
                ? float.MinValue
                : finalGroupX - zoneSize.x * 0.5f - laneRight;
            _lastSidePileTooFar = _lastSidePileToLaneGap != float.MinValue
                && _lastSidePileToLaneGap > Mathf.Max(sidePileTooFarGap, sidePileToLaneGap);
            _lastSidePileOverlapWithLane =
                DoesBoardZoneOverlapAnyLane(playerDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(playerDiscardAnchor)
                || DoesBoardZoneOverlapAnyLane(opponentDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(opponentDiscardAnchor);
            _lastSidePileOverlapWithRevealArea =
                DoesBoardZoneOverlapRevealArea(playerDeckAnchor)
                || DoesBoardZoneOverlapRevealArea(playerDiscardAnchor)
                || DoesBoardZoneOverlapRevealArea(opponentDeckAnchor)
                || DoesBoardZoneOverlapRevealArea(opponentDiscardAnchor);
        }

        float GetActiveLaneRightWorldX()
        {
            if (battlefieldManager == null || turnManager == null) return float.MinValue;

            int laneIndex = Mathf.Clamp(turnManager.ActiveNewLaneIndex, 0, Mathf.Max(0, battlefieldManager.maxLaneCount - 1));
            UcgBattleLane lane = battlefieldManager.GetLane(laneIndex);
            if (lane == null || !lane.gameObject.activeInHierarchy) return float.MinValue;

            float right = float.MinValue;
            right = Mathf.Max(right, GetRectRightInWorld(lane.playerSlot));
            right = Mathf.Max(right, GetRectRightInWorld(lane.opponentSlot));
            return right;
        }

        float GetRectRightInWorld(RectTransform rectTransform)
        {
            if (rectTransform == null) return float.MinValue;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            float maxX = corners[0].x;
            for (int i = 1; i < corners.Length; i++)
            {
                maxX = Mathf.Max(maxX, corners[i].x);
            }

            return maxX;
        }

        float GetAppliedRightSidePileColumnDownShift(float boardHeight, float groupY, float stackOffsetY, Vector2 zoneSize)
        {
            float bottomSafeMargin = 6f;
            float maxColumnDownShift = Mathf.Max(
                0f,
                boardHeight * 0.5f - bottomSafeMargin - (groupY + stackOffsetY + zoneSize.y * 0.5f));
            return Mathf.Clamp(rightSidePileColumnDownShift, 0f, maxColumnDownShift);
        }

        void LayoutBoardPileGroup(RectTransform group, Vector2 anchoredPosition, Vector2 size, string source)
        {
            if (group == null) return;

            Vector2 beforePosition = group.anchoredPosition;
            group.anchorMin = new Vector2(0.5f, 0.5f);
            group.anchorMax = new Vector2(0.5f, 0.5f);
            group.pivot = new Vector2(0.5f, 0.5f);
            group.anchoredPosition = anchoredPosition;
            group.sizeDelta = size;
            group.localScale = Vector3.one;
            group.localEulerAngles = Vector3.zero;
            LogSetSidePilePosition(source, beforePosition, group.anchoredPosition);
        }

        void ApplyDebugSidePileExtremeOffsetToVisibleGroups(bool shouldApply)
        {
            if (!shouldApply) return;
            if (Mathf.Abs(debugSidePileExtremeOffsetX) < 0.01f) return;

            ApplyDebugSidePileExtremeOffsetToGroup(playerSidePileGroup, "DebugExtremeOffset.PlayerSidePileGroup");
            ApplyDebugSidePileExtremeOffsetToGroup(opponentSidePileGroup, "DebugExtremeOffset.OpponentSidePileGroup");
        }

        void ApplyDebugSidePileExtremeOffsetToGroup(RectTransform group, string source)
        {
            if (group == null) return;

            Vector2 beforePosition = group.anchoredPosition;
            group.anchoredPosition = beforePosition + new Vector2(debugSidePileExtremeOffsetX, 0f);
            LogSetSidePilePosition(source, beforePosition, group.anchoredPosition);
        }

        void LogSetSidePilePosition(string source, Vector2 beforePosition, Vector2 afterPosition)
        {
            if (!debugBoardZones && !debugBattlefieldLayout && !debugForceSidePileExtremeOffset) return;

            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            Debug.Log(
                "SetSidePilePosition:\n"
                + $"frame={Time.frameCount}\n"
                + $"source={source}\n"
                + $"target={(source != null && source.Contains("Opponent") ? "opponentSidePileGroup" : "playerSidePileGroup")}\n"
                + $"xBefore={FormatDebugFloat(beforePosition.x)}\n"
                + $"xAfter={FormatDebugFloat(afterPosition.x)}\n"
                + $"worldXAfter={FormatDebugFloat(GetSidePileGroupWorldX(source))}\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"yBefore={FormatDebugFloat(beforePosition.y)}\n"
                + $"yAfter={FormatDebugFloat(afterPosition.y)}");
        }

        float GetSidePileGroupWorldX(string source)
        {
            RectTransform target = source != null && source.Contains("Opponent")
                ? opponentSidePileGroup
                : playerSidePileGroup;
            if (target == null) return float.MinValue;

            return target.TransformPoint(target.rect.center).x;
        }

        void LayoutBoardZone(RectTransform zone, Vector2 anchoredPosition, Vector2 size)
        {
            if (zone == null) return;

            zone.anchorMin = new Vector2(0.5f, 0.5f);
            zone.anchorMax = new Vector2(0.5f, 0.5f);
            zone.pivot = new Vector2(0.5f, 0.5f);
            zone.anchoredPosition = anchoredPosition;
            zone.sizeDelta = size;
            zone.localScale = Vector3.one;
            zone.localEulerAngles = Vector3.zero;
            UpdateBoardZoneDebugText(zone);
        }

        void UpdateBoardZoneDebugText(RectTransform zone)
        {
            if (zone == null) return;

            Transform existingDebugText = zone.Find("Zone Debug Info");
            Text debugText = existingDebugText != null ? existingDebugText.GetComponent<Text>() : null;
            if (debugText == null) return;

            debugText.gameObject.SetActive(debugBoardZones);
            debugText.text = $"CONTROLLED\npos {zone.anchoredPosition.x:0},{zone.anchoredPosition.y:0}\nsize {zone.sizeDelta.x:0}x{zone.sizeDelta.y:0}";
        }

        void ApplyBoardZoneDebugVisualState()
        {
            ApplyBoardZoneDebugVisual(playerDeckAnchor, "PLAYER DECK", true);
            ApplyBoardZoneDebugVisual(playerDiscardAnchor, "PLAYER DISCARD", true);
            ApplyBoardZoneDebugVisual(opponentDeckAnchor, "OP DECK", false);
            ApplyBoardZoneDebugVisual(opponentDiscardAnchor, "OP DISCARD", false);
        }

        void UpdateLayoutDebugBounds()
        {
            bool visible = debugBoardZones || debugBattlefieldLayout;
            RectTransform contentRoot = battlefieldManager != null ? battlefieldManager.content : null;
            RectTransform boardRoot = GetBoardZoneLayoutRoot();
            if (!visible)
            {
                _layoutDebugBoundsLogged = false;
            }

            SetLayoutDebugFrame(contentRoot, "Content", new Color(1f, 0.05f, 0.05f, 1f), visible);
            SetLayoutDebugFrame(boardRoot, "BoardRoot", new Color(1f, 0.92f, 0.05f, 1f), visible);
            SetLayoutDebugFrame(pileSideRegionRoot, "PileRegion", new Color(0.72f, 0.16f, 1f, 1f), visible);
            SetLayoutDebugFrame(playerSidePileGroup, "PlayerPile", new Color(0.14f, 1f, 0.18f, 1f), visible);
            SetLayoutDebugFrame(opponentSidePileGroup, "OppPile", new Color(1f, 0.52f, 0.08f, 1f), visible);
            SetLaneAreaDebugFrame(contentRoot, visible);

            if (visible && !_layoutDebugBoundsLogged)
            {
                LogLayoutDebugBoundsInfo(contentRoot, boardRoot);
                _layoutDebugBoundsLogged = true;
            }
        }

        RectTransform SetLayoutDebugFrame(RectTransform target, string label, Color color, bool visible)
        {
            if (target == null) return null;
            if (!visible)
            {
                HideLayoutDebugFrame(target, label);
                return null;
            }

            RectTransform frame = EnsureLayoutDebugFrame(target, label, color, visible);
            if (frame == null) return null;

            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            frame.localScale = Vector3.one;
            frame.localEulerAngles = Vector3.zero;
            ApplyLayoutDebugFrameLayer(frame);
            frame.SetAsLastSibling();
            UpdateLayoutDebugLabel(frame, label, color);
            return frame;
        }

        void SetLaneAreaDebugFrame(RectTransform contentRoot, bool visible)
        {
            if (contentRoot == null) return;
            if (!visible)
            {
                HideLayoutDebugFrame(contentRoot, "LaneArea");
                return;
            }

            Color laneAreaColor = new Color(0.05f, 0.95f, 1f, 1f);
            RectTransform frame = EnsureLayoutDebugFrame(contentRoot, "LaneArea", laneAreaColor, visible);
            if (frame == null) return;

            if (!visible || !TryGetOpenedLaneAreaBounds(out Vector2 center, out Vector2 size))
            {
                frame.gameObject.SetActive(false);
                return;
            }

            frame.anchorMin = new Vector2(0f, 0.5f);
            frame.anchorMax = new Vector2(0f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = center;
            frame.sizeDelta = size;
            frame.localScale = Vector3.one;
            frame.localEulerAngles = Vector3.zero;
            ApplyLayoutDebugFrameLayer(frame);
            frame.SetAsLastSibling();
            frame.gameObject.SetActive(true);
            UpdateLayoutDebugLabel(frame, "LaneArea", laneAreaColor);
        }

        RectTransform EnsureLayoutDebugFrame(RectTransform target, string label, Color color, bool visible)
        {
            string frameName = GetLayoutDebugFrameName(label);
            Transform existing = target.Find(frameName);
            RectTransform frame;
            Image image;
            Outline outline;

            if (existing == null)
            {
                var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Canvas));
                frameObject.transform.SetParent(target, false);
                frame = frameObject.GetComponent<RectTransform>();
                image = frameObject.GetComponent<Image>();
                outline = frameObject.GetComponent<Outline>();
            }
            else
            {
                frame = existing as RectTransform;
                image = existing.GetComponent<Image>();
                if (image == null) image = existing.gameObject.AddComponent<Image>();
                outline = existing.GetComponent<Outline>();
                if (outline == null) outline = existing.gameObject.AddComponent<Outline>();
            }

            image.enabled = true;
            image.color = visible
                ? new Color(color.r, color.g, color.b, 0.12f)
                : Color.clear;
            image.raycastTarget = false;

            outline.enabled = visible;
            outline.useGraphicAlpha = false;
            outline.effectColor = visible ? color : Color.clear;
            outline.effectDistance = new Vector2(4f, -4f);

            ApplyLayoutDebugFrameLayer(frame);
            EnsureLayoutDebugBorderBars(frame, color, visible);

            frame.gameObject.SetActive(visible);
            return frame;
        }

        void ApplyLayoutDebugFrameLayer(RectTransform frame)
        {
            if (frame == null) return;

            Canvas frameCanvas = frame.GetComponent<Canvas>();
            if (frameCanvas == null) frameCanvas = frame.gameObject.AddComponent<Canvas>();
            frameCanvas.enabled = true;
            frameCanvas.overrideSorting = true;
            frameCanvas.sortingOrder = 31900;
        }

        void EnsureLayoutDebugBorderBars(RectTransform frame, Color color, bool visible)
        {
            if (frame == null) return;

            EnsureLayoutDebugBorderBar(frame, "Debug Border Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 4f), color, visible);
            EnsureLayoutDebugBorderBar(frame, "Debug Border Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 4f), color, visible);
            EnsureLayoutDebugBorderBar(frame, "Debug Border Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(4f, 0f), color, visible);
            EnsureLayoutDebugBorderBar(frame, "Debug Border Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(4f, 0f), color, visible);
        }

        void EnsureLayoutDebugBorderBar(
            RectTransform frame,
            string barName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            bool visible)
        {
            Transform existingBar = frame.Find(barName);
            RectTransform barRect;
            Image barImage;

            if (existingBar == null)
            {
                var barObject = new GameObject(barName, typeof(RectTransform), typeof(Image));
                barObject.transform.SetParent(frame, false);
                barRect = barObject.GetComponent<RectTransform>();
                barImage = barObject.GetComponent<Image>();
            }
            else
            {
                barRect = existingBar as RectTransform;
                barImage = existingBar.GetComponent<Image>();
                if (barImage == null) barImage = existingBar.gameObject.AddComponent<Image>();
            }

            barRect.anchorMin = anchorMin;
            barRect.anchorMax = anchorMax;
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = anchoredPosition;
            barRect.sizeDelta = sizeDelta;
            barRect.localScale = Vector3.one;
            barRect.localEulerAngles = Vector3.zero;
            barRect.gameObject.SetActive(visible);
            barRect.SetAsLastSibling();

            barImage.enabled = true;
            barImage.color = visible ? color : Color.clear;
            barImage.raycastTarget = false;
        }

        string GetLayoutDebugFrameName(string label)
        {
            return $"__Layout Debug Bounds {label}";
        }

        void HideLayoutDebugFrame(RectTransform target, string label)
        {
            if (target == null) return;

            Transform existing = target.Find(GetLayoutDebugFrameName(label));
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        void UpdateLayoutDebugLabel(RectTransform frame, string label, Color color)
        {
            if (frame == null) return;

            EnsureLayoutDebugLabelBackplate(frame);

            const string labelName = "Debug Bounds Label";
            Transform existingLabel = frame.Find(labelName);
            RectTransform labelRect;
            Text labelText;

            if (existingLabel == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text), typeof(Outline));
                labelObject.transform.SetParent(frame, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                labelText = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existingLabel as RectTransform;
                labelText = existingLabel.GetComponent<Text>();
                if (labelText == null) labelText = existingLabel.gameObject.AddComponent<Text>();
                if (existingLabel.GetComponent<Outline>() == null) existingLabel.gameObject.AddComponent<Outline>();
            }

            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(10f, -8f);
            labelRect.sizeDelta = new Vector2(150f, 30f);
            labelRect.localScale = Vector3.one;
            labelRect.localEulerAngles = Vector3.zero;
            labelRect.SetAsLastSibling();

            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null) labelText.font = placeholderFont;
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = color;
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 10;
            labelText.resizeTextMaxSize = 18;
            labelText.raycastTarget = false;

            Outline textOutline = labelText.GetComponent<Outline>();
            textOutline.enabled = true;
            textOutline.effectColor = new Color(0f, 0f, 0f, 1f);
            textOutline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        void EnsureLayoutDebugLabelBackplate(RectTransform frame)
        {
            if (frame == null) return;

            const string backplateName = "Debug Bounds Label Backplate";
            Transform existingBackplate = frame.Find(backplateName);
            RectTransform backplateRect;
            Image backplateImage;

            if (existingBackplate == null)
            {
                var backplateObject = new GameObject(backplateName, typeof(RectTransform), typeof(Image));
                backplateObject.transform.SetParent(frame, false);
                backplateRect = backplateObject.GetComponent<RectTransform>();
                backplateImage = backplateObject.GetComponent<Image>();
            }
            else
            {
                backplateRect = existingBackplate as RectTransform;
                backplateImage = existingBackplate.GetComponent<Image>();
                if (backplateImage == null) backplateImage = existingBackplate.gameObject.AddComponent<Image>();
            }

            backplateRect.anchorMin = new Vector2(0f, 1f);
            backplateRect.anchorMax = new Vector2(0f, 1f);
            backplateRect.pivot = new Vector2(0f, 1f);
            backplateRect.anchoredPosition = new Vector2(6f, -5f);
            backplateRect.sizeDelta = new Vector2(164f, 36f);
            backplateRect.localScale = Vector3.one;
            backplateRect.localEulerAngles = Vector3.zero;
            backplateRect.gameObject.SetActive(true);
            backplateRect.SetAsLastSibling();

            backplateImage.enabled = true;
            backplateImage.color = new Color(0f, 0f, 0f, 0.82f);
            backplateImage.raycastTarget = false;
        }

        bool TryGetOpenedLaneAreaBounds(out Vector2 center, out Vector2 size)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            if (battlefieldManager == null) return false;

            List<UcgBattleLane> lanes = turnManager != null
                ? battlefieldManager.GetOpenedLanes(turnManager.currentTurn)
                : battlefieldManager.GetAllVisibleLanes();
            if (lanes == null || lanes.Count == 0)
            {
                lanes = battlefieldManager.GetAllVisibleLanes();
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < lanes.Count; i++)
            {
                RectTransform laneRect = lanes[i] != null ? lanes[i].transform as RectTransform : null;
                if (laneRect == null || !laneRect.gameObject.activeInHierarchy) continue;

                Vector2 laneSize = laneRect.sizeDelta;
                float left = laneRect.anchoredPosition.x - laneRect.pivot.x * laneSize.x;
                float bottom = laneRect.anchoredPosition.y - laneRect.pivot.y * laneSize.y;
                minX = Mathf.Min(minX, left);
                maxX = Mathf.Max(maxX, left + laneSize.x);
                minY = Mathf.Min(minY, bottom);
                maxY = Mathf.Max(maxY, bottom + laneSize.y);
            }

            if (minX == float.MaxValue || maxX == float.MinValue) return false;

            center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            size = new Vector2(Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
            return true;
        }

        void LogLayoutDebugBoundsInfo(RectTransform contentRoot, RectTransform boardRoot)
        {
            if (!debugBoardZones && !debugBattlefieldLayout) return;

            RectTransform laneAreaFrame = contentRoot != null
                ? contentRoot.Find(GetLayoutDebugFrameName("LaneArea")) as RectTransform
                : null;

            Debug.Log(
                "[UCG Layout] Full diagnostic\n"
                + FormatPileNudgeDiagnostic()
                + FormatLayoutDebugRect("Content", contentRoot)
                + FormatLayoutDebugRect("BoardRoot", boardRoot)
                + FormatLayoutDebugRect("PileRegion", pileSideRegionRoot)
                + FormatLayoutDebugRect("PlayerPile", playerSidePileGroup)
                + FormatLayoutDebugRect("OppPile", opponentSidePileGroup)
                + FormatLayoutDebugRect("PlayerDeckSlot", playerDeckAnchor)
                + FormatLayoutDebugRect("PlayerDiscardSlot", playerDiscardAnchor)
                + FormatLayoutDebugRect("OpponentDeckSlot", opponentDeckAnchor)
                + FormatLayoutDebugRect("OpponentDiscardSlot", opponentDiscardAnchor)
                + FormatLayoutDebugRect("LaneArea", laneAreaFrame)
                + FormatOpenedLaneBoundsDiagnostic());
        }

        string FormatLayoutDebugRect(string label, RectTransform rect)
        {
            if (rect == null)
            {
                return $"{label}: missing\n";
            }

            return $"{label}:\n"
                + $"name={rect.name}\n"
                + $"parentName={(rect.parent != null ? rect.parent.name : "none")}\n"
                + $"parentPath={FormatParentPath(rect)}\n"
                + $"anchorMin={FormatVector2(rect.anchorMin)}\n"
                + $"anchorMax={FormatVector2(rect.anchorMax)}\n"
                + $"pivot={FormatVector2(rect.pivot)}\n"
                + $"anchoredPosition={FormatAnchoredPosition(rect)}\n"
                + $"sizeDelta={FormatSizeDelta(rect)}\n"
                + $"localScale={FormatVector3(rect.localScale)}\n"
                + $"worldCorners={FormatWorldCorners(rect)}\n"
                + $"activeInHierarchy={rect.gameObject.activeInHierarchy}\n";
        }

        string FormatPileNudgeDiagnostic()
        {
            float currentPileRegionX = pileSideRegionRoot != null ? pileSideRegionRoot.anchoredPosition.x : float.MinValue;
            bool overwrittenAfterApply = _lastPileRegionXAfterApply != float.MinValue
                && currentPileRegionX != float.MinValue
                && Mathf.Abs(currentPileRegionX - _lastPileRegionXAfterApply) > 0.1f;

            return "PileNudge:\n"
                + $"sidePileColumnNudgeX={sidePileColumnNudgeX:0.###}\n"
                + $"appliedMethod={_lastPileRegionNudgeMethod}\n"
                + $"activeLayout={(useFixedReferenceBoardLayout ? "ReferenceBoardLayout" : "DynamicBoardLayout")}\n"
                + $"layoutFrame={_lastPileRegionLayoutFrame}\n"
                + $"visibleRight={FormatDebugFloat(_lastPileRegionVisibleRight)}\n"
                + $"viewportRight={FormatDebugFloat(_lastPileRegionViewportRight)}\n"
                + $"[UCG Layout] Pile final x before nudge = {FormatDebugFloat(_lastPileRegionXBeforeNudge)}\n"
                + $"[UCG Layout] sidePileColumnNudgeX = {sidePileColumnNudgeX:0.###}\n"
                + $"[UCG Layout] Pile final x after nudge = {FormatDebugFloat(_lastPileRegionXAfterNudge)}\n"
                + $"[UCG Layout] Pile final x after clamp = {FormatDebugFloat(_lastPileRegionXAfterClamp)}\n"
                + $"methodEntryX={FormatDebugFloat(_lastPileRegionXBeforeMethod)}\n"
                + $"maxSafeClampX={FormatDebugFloat(_lastPileRegionXMaxSafeClamp)}\n"
                + $"clampApplied={_lastPileRegionClampApplied}\n"
                + $"appliedX={FormatDebugFloat(_lastPileRegionXAfterApply)}\n"
                + $"currentPileRegionX={FormatDebugFloat(currentPileRegionX)}\n"
                + $"overwrittenAfterApply={overwrittenAfterApply}\n"
                + "dynamicLayoutAlsoUsesNudge=ApplyBoardZoneLayoutForPortrait when useFixedReferenceBoardLayout=false\n";
        }

        string FormatOpenedLaneBoundsDiagnostic()
        {
            string result = "OpenedLaneBounds:\n";
            if (!TryGetOpenedLaneAreaBounds(out Vector2 center, out Vector2 size))
            {
                return result + "available=false\n";
            }

            result += $"available=true\ncenter={FormatVector2(center)}\nsize={FormatVector2(size)}\n";
            if (battlefieldManager == null || turnManager == null) return result;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                RectTransform laneRect = lanes[i] != null ? lanes[i].transform as RectTransform : null;
                result += FormatLayoutDebugRect($"OpenedLane{i + 1}", laneRect);
            }

            return result;
        }

        string FormatVector2(Vector2 value)
        {
            return $"({value.x:0.###},{value.y:0.###})";
        }

        string FormatVector3(Vector3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }

        string FormatWorldCorners(RectTransform rect)
        {
            if (rect == null) return "missing";

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return $"BL{FormatVector3(corners[0])} TL{FormatVector3(corners[1])} TR{FormatVector3(corners[2])} BR{FormatVector3(corners[3])}";
        }

        void ApplyBoardZoneDebugVisual(RectTransform zone, string debugLabel, bool restorePlayerLabel)
        {
            if (zone == null) return;

            Image image = zone.GetComponent<Image>();
            if (image != null)
            {
                image.color = debugBoardZones
                    ? new Color(0.18f, 0.06f, 0.30f, 0.86f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, Mathf.Max(sidePileBackgroundAlpha, 0.28f));
                image.raycastTarget = false;
            }

            Outline outline = zone.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = debugBoardZones
                    ? new Color(1f, 0.92f, 0.12f, 1f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, Mathf.Min(Mathf.Max(sidePileOutlineAlpha, 0.26f), 0.34f));
                outline.effectDistance = debugBoardZones ? new Vector2(3.2f, -3.2f) : new Vector2(1.2f, -1.2f);
            }

            Transform frameTransform = zone.Find("Card Frame");
            Image frameImage = frameTransform != null ? frameTransform.GetComponent<Image>() : null;
            if (frameImage != null)
            {
                frameImage.color = debugBoardZones
                    ? new Color(0.95f, 0.82f, 0.06f, 0.46f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.12f);
                frameImage.raycastTarget = false;
            }

            Outline frameOutline = frameTransform != null ? frameTransform.GetComponent<Outline>() : null;
            if (frameOutline != null)
            {
                frameOutline.enabled = true;
                frameOutline.effectColor = debugBoardZones
                    ? new Color(1f, 0.96f, 0.18f, 1f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.24f);
                frameOutline.effectDistance = debugBoardZones ? new Vector2(2f, -2f) : new Vector2(0.9f, -0.9f);
            }

            Text label = zone.Find("Zone Label") != null ? zone.Find("Zone Label").GetComponent<Text>() : null;
            if (label != null)
            {
                if (debugBoardZones)
                {
                    label.gameObject.SetActive(true);
                    label.text = debugLabel;
                    label.fontSize = 16;
                    label.color = new Color(1f, 0.96f, 0.18f, 1f);
                }
                else if (!restorePlayerLabel)
                {
                    label.text = GetOpponentPileZoneLabel(zone);
                    label.gameObject.SetActive(true);
                    label.fontSize = 16;
                    label.resizeTextMaxSize = 16;
                    label.color = UcgToolUiPalette.MutedWhite;
                }
                else if (zone == playerDeckAnchor)
                {
                    label.text = "牌庫";
                    label.gameObject.SetActive(true);
                    label.fontSize = 16;
                    label.resizeTextMaxSize = 16;
                    label.color = UcgToolUiPalette.MutedWhite;
                }
                else if (zone == playerDiscardAnchor)
                {
                    label.text = "棄牌區";
                    label.gameObject.SetActive(true);
                    label.fontSize = 16;
                    label.resizeTextMaxSize = 16;
                    label.color = UcgToolUiPalette.MutedWhite;
                }
            }

            Text count = zone.Find("Zone Count") != null ? zone.Find("Zone Count").GetComponent<Text>() : null;
            if (count != null)
            {
                count.gameObject.SetActive(true);
                count.fontSize = debugBoardZones ? 20 : 36;
                count.resizeTextMinSize = debugBoardZones ? 10 : 18;
                count.resizeTextMaxSize = debugBoardZones ? 20 : 36;
                count.color = debugBoardZones
                    ? new Color(1f, 1f, 1f, 1f)
                    : UcgToolUiPalette.BrandPinkLight;
            }

            UpdateBoardZoneDebugText(zone);
        }

        string GetOpponentPileZoneLabel(RectTransform zone)
        {
            if (zone == opponentDeckAnchor) return "牌庫";
            if (zone == opponentDiscardAnchor) return "棄牌區";
            return "";
        }

        RectTransform EnsureBattlefieldZoneRoot()
        {
            const string rootName = "Battlefield Zone Anchors";
            RectTransform parentRoot = GetBoardZoneParentRoot();
            Transform existingRoot = parentRoot != null ? parentRoot.Find(rootName) : null;
            if (existingRoot == null && battlefieldManager != null && battlefieldManager.content != null)
            {
                existingRoot = battlefieldManager.content.Find(rootName);
            }
            if (existingRoot == null && battlefieldManager != null && battlefieldManager.transform != null)
            {
                existingRoot = battlefieldManager.transform.Find(rootName);
            }
            if (existingRoot == null && battlefieldManager != null && battlefieldManager.viewport != null)
            {
                existingRoot = battlefieldManager.viewport.Find(rootName);
            }
            if (existingRoot == null && canvas != null)
            {
                existingRoot = canvas.transform.Find(rootName);
            }
            RectTransform rootRect;

            if (existingRoot == null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(Canvas));
                rootObject.transform.SetParent(parentRoot != null ? parentRoot : canvas.transform, false);
                rootRect = rootObject.GetComponent<RectTransform>();
            }
            else
            {
                rootRect = existingRoot as RectTransform;
                if (parentRoot != null && rootRect.parent != parentRoot)
                {
                    rootRect.SetParent(parentRoot, false);
                }
                if (existingRoot.GetComponent<Canvas>() == null) existingRoot.gameObject.AddComponent<Canvas>();
            }

            ApplyBoardZoneRootLayout(rootRect);
            rootRect.localScale = Vector3.one;
            rootRect.localEulerAngles = Vector3.zero;
            rootRect.gameObject.SetActive(true);

            Canvas rootCanvas = rootRect.GetComponent<Canvas>();
            rootCanvas.enabled = true;
            rootCanvas.overrideSorting = false;
            rootCanvas.sortingOrder = 0;
            EnsureVisibleCanvasGroup(rootRect, 1f);
            ApplyBoardZoneRootLayer(rootRect);
            EnsureCombatBoardRegionRoot(rootRect);
            EnsurePileSideRegionRoot(rootRect);
            return rootRect;
        }

        RectTransform EnsureCombatBoardRegionRoot(RectTransform root)
        {
            combatBoardRegionRoot = EnsureBoardRegionRoot(root, "Combat Board Region", combatRegionPos, combatRegionSize);
            if (combatBoardRegionRoot != null) combatBoardRegionRoot.SetAsFirstSibling();
            return combatBoardRegionRoot;
        }

        RectTransform EnsurePileSideRegionRoot(RectTransform root)
        {
            pileSideRegionRoot = EnsureBoardRegionRoot(root, "Pile Side Region", pileRegionPos, pileRegionSize);
            if (pileSideRegionRoot != null) pileSideRegionRoot.SetAsLastSibling();
            return pileSideRegionRoot;
        }

        RectTransform EnsureBoardRegionRoot(RectTransform root, string regionName, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (root == null) return null;

            Transform existing = root.Find(regionName);
            if (existing == null && regionName == "Pile Side Region")
            {
                existing = FindExistingZoneTransformAnywhere(regionName);
            }
            RectTransform rect;
            if (existing == null)
            {
                var regionObject = new GameObject(regionName, typeof(RectTransform));
                regionObject.transform.SetParent(root, false);
                rect = regionObject.GetComponent<RectTransform>();
            }
            else
            {
                rect = existing as RectTransform;
                if (rect != null && rect.parent != root)
                {
                    rect.SetParent(root, false);
                }
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            rect.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(rect, 1f);
            EnsureBoardRegionVisual(rect, regionName);
            return rect;
        }

        void EnsureBoardRegionVisual(RectTransform rect, string regionName)
        {
            if (rect == null) return;

            Image image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.enabled = true;
            image.raycastTarget = false;

            Outline outline = rect.GetComponent<Outline>();
            if (outline == null) outline = rect.gameObject.AddComponent<Outline>();
            outline.enabled = debugBoardZones;
            outline.useGraphicAlpha = true;
            outline.effectDistance = new Vector2(2f, -2f);

            if (regionName == "Pile Side Region")
            {
                image.color = Color.clear;
                outline.effectColor = debugBoardZones
                    ? new Color(0.25f, 1f, 0.38f, 0.95f)
                    : Color.clear;
                EnsureBoardRegionLabel(rect, "PILE SIDE REGION");
            }
            else if (regionName == "Combat Board Region")
            {
                image.color = debugBoardZones
                    ? new Color(0.26f, 0.78f, 1f, 0.26f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.08f);
                outline.effectColor = debugBoardZones
                    ? new Color(0.45f, 0.95f, 1f, 0.85f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.16f);
                EnsureBoardRegionLabel(rect, "COMBAT BOARD REGION");
            }
            else
            {
                image.color = Color.clear;
                outline.effectColor = Color.clear;
            }
        }

        void EnsureBoardRegionLabel(RectTransform parent, string text)
        {
            if (parent == null) return;

            Font font = LoadPlaceholderFont();
            Text label = EnsureZoneText(
                parent,
                "Region Debug Label",
                new Vector2(0.05f, 0.94f),
                new Vector2(0.95f, 0.995f),
                font,
                13,
                new Color(0.92f, 1f, 0.9f, debugBoardZones ? 0.92f : 0f));
            label.text = text;
            label.gameObject.SetActive(debugBoardZones);
        }

        RectTransform GetBoardZoneParentRoot()
        {
            if (battlefieldManager != null && battlefieldManager.content != null)
            {
                return battlefieldManager.content;
            }

            if (battlefieldManager != null)
            {
                return battlefieldManager.transform as RectTransform;
            }

            return canvas != null ? canvas.transform as RectTransform : null;
        }

        void ApplyBoardZoneRootLayer(RectTransform rootRect)
        {
            if (rootRect == null) return;

            if (battlefieldManager != null
                && (rootRect.parent == battlefieldManager.content || rootRect.parent == battlefieldManager.transform))
            {
                rootRect.SetAsLastSibling();
                return;
            }

            rootRect.SetAsLastSibling();
        }

        void ApplyBoardZoneRootLayout(RectTransform rootRect)
        {
            if (rootRect == null) return;

            if (IsBoardZoneRootUnderBattlefieldContent(rootRect))
            {
                rootRect.anchorMin = new Vector2(0f, 0.5f);
                rootRect.anchorMax = new Vector2(0f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = GetBoardZoneRootSize();
                rootRect.anchoredPosition = new Vector2(GetBoardZoneRootCenterXForActiveLane(), 0f);
                EnsureCombatBoardRegionRoot(rootRect);
                EnsurePileSideRegionRoot(rootRect);
                return;
            }

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            EnsureCombatBoardRegionRoot(rootRect);
            EnsurePileSideRegionRoot(rootRect);
        }

        Vector2 GetBoardZoneRootSize()
        {
            RectTransform battlefieldRect = battlefieldManager != null
                ? battlefieldManager.transform as RectTransform
                : null;
            float baseWidth = battlefieldRect != null && battlefieldRect.rect.width > 0f
                ? battlefieldRect.rect.width
                : 1040f;
            float baseHeight = battlefieldRect != null && battlefieldRect.rect.height > 0f
                ? battlefieldRect.rect.height
                : 820f;
            float width = Mathf.Max(baseWidth, baseWidth + Mathf.Max(0f, rightAuxiliaryColumnGutterWidth));
            return new Vector2(width, Mathf.Max(baseHeight, 820f));
        }

        float GetBoardZoneRootCenterXForActiveLane()
        {
            if (battlefieldManager == null) return 0f;

            int laneCount = Mathf.Max(1, battlefieldManager.maxLaneCount);
            int activeLaneIndex = turnManager != null
                ? Mathf.Clamp(turnManager.ActiveNewLaneIndex, 0, laneCount - 1)
                : 0;
            int visualOrder = Mathf.Clamp(laneCount - 1 - activeLaneIndex, 0, laneCount - 1);
            float laneStep = battlefieldManager.laneSize.x + battlefieldManager.laneSpacing;
            return visualOrder * laneStep + battlefieldManager.laneSize.x * 0.5f;
        }

        bool IsBoardZoneRootUnderBattlefieldContent(RectTransform rootRect)
        {
            return rootRect != null
                && battlefieldManager != null
                && battlefieldManager.content != null
                && rootRect.parent == battlefieldManager.content;
        }

        RectTransform EnsureBoardPileGroup(RectTransform root, string objectName)
        {
            if (root == null) return null;

            Transform existing = root.Find(objectName);
            if (existing == null && root != null && root.parent != null)
            {
                existing = root.parent.Find(objectName);
            }
            if (existing == null)
            {
                existing = FindExistingZoneTransformAnywhere(objectName);
            }
            RectTransform rect;

            if (existing == null)
            {
                var groupObject = new GameObject(objectName, typeof(RectTransform));
                groupObject.transform.SetParent(root, false);
                rect = groupObject.GetComponent<RectTransform>();
            }
            else
            {
                rect = existing as RectTransform;
                if (rect != null && root != null && rect.parent != root)
                {
                    rect.SetParent(root, false);
                }
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            rect.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(rect, 1f);
            rect.SetAsLastSibling();
            return rect;
        }

        RectTransform EnsureBattlefieldZoneFrame(RectTransform root, RectTransform group, string objectName, Vector2 size, string label, Font font, out Text countText)
        {
            Transform existing = group != null ? group.Find(objectName) : null;
            if (existing == null)
            {
                existing = root != null ? root.Find(objectName) : null;
            }
            if (existing == null && root != null)
            {
                existing = FindDescendantByName(root, objectName);
            }
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                var zoneObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline));
                zoneObject.transform.SetParent(group != null ? group : root, false);
                rect = zoneObject.GetComponent<RectTransform>();
                image = zoneObject.GetComponent<Image>();
            }
            else
            {
                rect = existing as RectTransform;
                if (group != null && rect.parent != group)
                {
                    rect.SetParent(group, false);
                }
                image = existing.GetComponent<Image>();
                if (image == null) image = existing.gameObject.AddComponent<Image>();
                if (existing.GetComponent<Outline>() == null) existing.gameObject.AddComponent<Outline>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            rect.gameObject.SetActive(true);
            EnsureVisibleCanvasGroup(rect, 1f);
            rect.SetAsLastSibling();
            image.enabled = true;
            ApplyRoundedPanelImage(image);
            image.color = debugBoardZones
                ? new Color(0.025f, 0.18f, 0.24f, 0.74f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, Mathf.Max(sidePileBackgroundAlpha, 0.28f));
            image.raycastTarget = false;

            Outline outline = rect.GetComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = debugBoardZones
                ? new Color(0.85f, 1f, 1f, 1f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, Mathf.Min(Mathf.Max(sidePileOutlineAlpha, 0.26f), 0.34f));
            outline.effectDistance = debugBoardZones ? new Vector2(1.9f, -1.9f) : new Vector2(1.2f, -1.2f);

            Shadow shadow = EnsureUiShadow(rect.gameObject);
            shadow.effectColor = new Color(0f, 0.08f, 0.16f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            EnsureZoneInnerFrame(rect);
            bool hasLabel = !string.IsNullOrWhiteSpace(label);
            Text titleText = EnsureZoneText(rect, "Zone Label", new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.87f), font, 16, UcgToolUiPalette.MutedWhite);
            titleText.text = label;
            titleText.gameObject.SetActive(hasLabel || debugBoardZones);
            countText = hasLabel
                ? EnsureZoneText(rect, "Zone Count", new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.57f), font, 36, UcgToolUiPalette.BrandPinkLight)
                : EnsureZoneText(rect, "Zone Count", new Vector2(0.18f, 0.33f), new Vector2(0.82f, 0.63f), font, 36, UcgToolUiPalette.BrandPinkLight);
            EnsureBoardZoneDebugText(rect, font);
            return rect;
        }

        void EnsureVisibleCanvasGroup(RectTransform rect, float alpha)
        {
            if (rect == null) return;

            CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.ignoreParentGroups = false;
        }

        void HideUnusedFixedBoardHudRoot()
        {
            if (canvas == null) return;

            Transform fixedRoot = canvas.transform.Find("Fixed Board HUD Anchors");
            if (fixedRoot == null) return;
            if (fixedRoot.childCount > 0) return;

            fixedRoot.gameObject.SetActive(false);
        }

        Transform FindDescendantByName(Transform root, string objectName)
        {
            if (root == null) return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == objectName) return child;

                Transform descendant = FindDescendantByName(child, objectName);
                if (descendant != null) return descendant;
            }

            return null;
        }

        Transform FindExistingZoneTransformAnywhere(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName)) return null;

            Transform found = null;
            if (battlefieldManager != null && battlefieldManager.content != null)
            {
                found = FindDescendantByName(battlefieldManager.content, objectName);
                if (found != null) return found;
            }

            if (battlefieldManager != null && battlefieldManager.viewport != null)
            {
                found = FindDescendantByName(battlefieldManager.viewport, objectName);
                if (found != null) return found;
            }

            if (battlefieldManager != null && battlefieldManager.transform != null)
            {
                found = FindDescendantByName(battlefieldManager.transform, objectName);
                if (found != null) return found;
            }

            if (canvas != null)
            {
                found = FindDescendantByName(canvas.transform, objectName);
                if (found != null) return found;
            }

            return null;
        }

        void EnsureZoneInnerFrame(RectTransform parent)
        {
            const string frameName = "Card Frame";
            Transform existingFrame = parent.Find(frameName);
            RectTransform frameRect;
            Image frameImage;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(parent, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                if (existingFrame.GetComponent<Outline>() == null) existingFrame.gameObject.AddComponent<Outline>();
            }

            frameRect.anchorMin = new Vector2(0.09f, 0.08f);
            frameRect.anchorMax = new Vector2(0.91f, 0.92f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frameRect.gameObject.SetActive(true);
            frameImage.enabled = true;
            ApplyRoundedPanelImage(frameImage);
            frameImage.color = debugBoardZones
                ? new Color(0.04f, 0.26f, 0.32f, 0.32f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.12f);
            frameImage.raycastTarget = false;

            Outline outline = frameRect.GetComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = debugBoardZones
                ? new Color(0.85f, 1f, 1f, 0.95f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.24f);
            outline.effectDistance = debugBoardZones ? new Vector2(1.1f, -1.1f) : new Vector2(0.9f, -0.9f);
        }

        void EnsureBoardZoneDebugText(RectTransform parent, Font font)
        {
            if (parent == null) return;

            Text debugText = EnsureZoneText(
                parent,
                "Zone Debug Info",
                new Vector2(0.04f, 0.39f),
                new Vector2(0.96f, 0.61f),
                font,
                11,
                new Color(0.9f, 1f, 1f, debugBoardZones ? 0.92f : 0f));
            debugText.gameObject.SetActive(debugBoardZones);
            debugText.text = $"pos {parent.anchoredPosition.x:0},{parent.anchoredPosition.y:0}\nsize {parent.sizeDelta.x:0}x{parent.sizeDelta.y:0}";
        }

        Text EnsureZoneText(RectTransform parent, string textName, Vector2 anchorMin, Vector2 anchorMax, Font font, int fontSize, Color color)
        {
            Transform existingText = parent.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            if (font != null) text.font = font;
            return text;
        }

        Button DisableZoneButton(RectTransform anchor)
        {
            if (anchor == null) return null;

            Button button = anchor.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }

            return button;
        }

        void DisableLegacyZoneHud(string objectName)
        {
            Transform legacy = canvas != null ? canvas.transform.Find(objectName) : null;
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }
        }

        void EnsureDiscardPilePanel()
        {
            const string panelName = "Discard Pile Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(canvas.transform, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 70f);
            panelRect.sizeDelta = new Vector2(720f, 620f);
            panelRect.SetAsLastSibling();

            panelImage.color = new Color(0.03f, 0.06f, 0.08f, 0.94f);
            panelImage.raycastTarget = true;

            discardPilePanel = panelRect;
            discardPilePanelText = EnsureDiscardPilePanelText(panelRect);
            closeDiscardPanelButton = EnsureDiscardPileCloseButton(panelRect);
            discardPilePanel.gameObject.SetActive(false);
        }

        Text EnsureDiscardPilePanelText(RectTransform parent)
        {
            const string textName = "Discard Pile Text";
            Transform existingText = parent.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(34f, 82f);
            textRect.offsetMax = new Vector2(-34f, -34f);

            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null) text.font = placeholderFont;
            text.fontSize = 24;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 24;
            text.raycastTarget = false;
            return text;
        }

        Button EnsureDiscardPileCloseButton(RectTransform parent)
        {
            const string buttonName = "Close Discard Pile Button";
            Transform existingButton = parent.Find(buttonName);
            RectTransform buttonRect;
            Button button;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                button = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                button = existingButton.GetComponent<Button>();
                if (button == null) button = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-22f, -18f);
            buttonRect.sizeDelta = new Vector2(128f, 52f);

            var image = button.GetComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 0.92f);
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.onClick.RemoveListener(HideDiscardPilePanel);
            button.onClick.AddListener(HideDiscardPilePanel);

            EnsureButtonLabel(buttonRect, "關閉");
            return button;
        }

        Text EnsureTurnInfoText()
        {
            const string textName = "Turn Info Text";
            Transform existingText = canvas.transform.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(canvas.transform, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = new Vector2(1f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(1f, 1f);
            textRect.anchoredPosition = new Vector2(-48f, -492f);
            textRect.sizeDelta = new Vector2(300f, 56f);

            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                text.font = placeholderFont;
            }
            text.fontSize = 22;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            text.raycastTarget = false;
            turnInfoText = text;
            text.gameObject.SetActive(false);

            return text;
        }

        Text EnsurePhaseInfoText()
        {
            const string textName = "Phase Info Text";
            Transform existingText = canvas.transform.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(canvas.transform, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = new Vector2(1f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(1f, 1f);
            textRect.anchoredPosition = new Vector2(-48f, -552f);
            textRect.sizeDelta = new Vector2(360f, 104f);

            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                text.font = placeholderFont;
            }
            text.fontSize = 20;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 20;
            text.raycastTarget = false;
            phaseInfoText = text;
            text.gameObject.SetActive(false);

            return text;
        }

        Text EnsureGameResultText()
        {
            const string textName = "Game Result Text";
            Transform existingText = canvas.transform.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(canvas.transform, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 1596f);
            textRect.sizeDelta = new Vector2(600f, 36f);

            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.94f);
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                text.font = placeholderFont;
            }
            text.fontSize = 16;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 16;
            text.raycastTarget = false;
            gameResultText = text;
            EnsureHudBackplate(
                "Game Result HUD Panel",
                textRect,
                UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.24f),
                UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.12f),
                new Vector2(14f, 6f));
            ResetGameResultText();

            return text;
        }

        void ResetGameResultText()
        {
            if (gameResultText != null)
            {
                gameResultText.text = "勝利路數：我方 0｜對手 0";
                SetGameResultHudVisible(false);
            }
        }

        void ShowPlayStatus(string message, float durationSeconds = 0f)
        {
            if (playResultText == null || _isTutorialFinishWaitingForClick) return;

            if (_playStatusRoutine != null)
            {
                StopCoroutine(_playStatusRoutine);
                _playStatusRoutine = null;
            }

            SetTopPromptAction(message);
            UpdateTopPhaseHud();
            if (durationSeconds > 0f)
            {
                _playStatusRoutine = StartCoroutine(PlayStatusReturnRoutine(durationSeconds));
            }
        }

        IEnumerator PlayStatusReturnRoutine(float durationSeconds)
        {
            yield return new WaitForSecondsRealtime(durationSeconds);
            _playStatusRoutine = null;
            RestoreCompactPlayStatus();
        }

        void RestoreCompactPlayStatus()
        {
            if (playResultText == null || _isTutorialFinishWaitingForClick || IsGameOver) return;

            UpdateTopPhaseHud();
        }

        void UpdateTopPhaseHud()
        {
            if (playResultText == null || _isTutorialFinishWaitingForClick) return;
            if (_isOpeningCameraIntro)
            {
                if (!string.IsNullOrEmpty(playResultText.text))
                {
                    playResultText.text = "";
                }
                return;
            }

            UcgGamePhase currentPhase = phaseManager != null ? phaseManager.CurrentPhase : UcgGamePhase.Start;
            if (currentPhase != _lastTopPromptPhase)
            {
                _lastTopPromptPhase = currentPhase;
                _topPromptActionText = "";
            }

            CaptureActionFromTransientHudText();
            string topText = BuildTopPromptHudText();
            if (!string.IsNullOrWhiteSpace(topText) && playResultText.text != topText)
            {
                playResultText.text = topText;
            }
        }

        string BuildTopPromptHudText()
        {
            string phaseText = GetTopPromptPhaseText();
            string actionText = string.IsNullOrWhiteSpace(_topPromptActionText)
                ? GetDefaultTopPromptActionText()
                : _topPromptActionText;
            string helperText = GetTopPromptHelperText(actionText);

            return $"<size=17><color={UcgToolUiPalette.SoftWhiteHex}>{phaseText}</color></size>\n"
                + $"<size=24><color={UcgToolUiPalette.BrandPinkLightHex}>{actionText}</color></size>\n"
                + $"<size=13><color={UcgToolUiPalette.MutedWhiteHex}>{helperText}</color></size>";
        }

        void SetTopPromptProgress(float progress, bool visible)
        {
            if (_topPromptProgressTrackRect == null)
            {
                Transform panel = canvas != null ? canvas.transform.Find("Play Result HUD Panel") : null;
                EnsureTopPromptProgress(panel as RectTransform);
            }
            if (_topPromptProgressTrackRect == null) return;

            _topPromptProgressTrackRect.gameObject.SetActive(visible);
            if (!visible) return;

            if (_topPromptProgressFillRect != null)
            {
                _topPromptProgressFillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                _topPromptProgressFillRect.offsetMin = Vector2.zero;
                _topPromptProgressFillRect.offsetMax = Vector2.zero;
            }
        }

        string GetTopPromptPhaseText()
        {
            if (IsGameOver) return "遊戲結束";
            if (phaseManager == null) return "對戰準備";

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.EnterEffect:
                case UcgGamePhase.BattleEffect:
                    return "效果處理階段";
                case UcgGamePhase.BattleJudgement:
                    return "判定階段";
                default:
                    return phaseManager.GetPhaseDisplayName();
            }
        }

        string GetDefaultTopPromptActionText()
        {
            if (IsGameOver) return "請確認對戰結果";
            if (phaseManager == null) return "請準備開始對戰";

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                    return "請選擇一張場景卡";
                case UcgGamePhase.CharacterSetup:
                    return "請選擇一張角色卡";
                case UcgGamePhase.Upgrade:
                    return "請選擇您想升級的角色";
                case UcgGamePhase.Open:
                    return "請確認翻開的卡牌";
                case UcgGamePhase.EnterEffect:
                case UcgGamePhase.BattleEffect:
                    return "請選擇效果目標";
                case UcgGamePhase.BattleJudgement:
                    return "正在比較雙方 BP";
                case UcgGamePhase.End:
                    return "正在清理本回合效果";
                case UcgGamePhase.Draw:
                    return "正在補充手牌";
                default:
                    return "請依照高亮提示操作";
            }
        }

        string GetTopPromptHelperText(string actionText)
        {
            if (IsGameOver) return "可以重新開始或切換測試";
            if (!string.IsNullOrWhiteSpace(actionText))
            {
                if (actionText.Contains("升級")) return "疊放到同名角色上，或點擊結束升級階段";
                if (actionText.Contains("效果") || actionText.Contains("目標")) return "依照高亮提示完成操作";
                if (actionText.Contains("放回") || actionText.Contains("放到牌庫") || actionText.Contains("放到底")) return "選擇後會依效果放回指定位置";
                if (actionText.Contains("場景")) return "拖放到中央場景區；沒有合適卡牌可略過";
                if (actionText.Contains("角色卡")) return "放到目前開放的 Lane 上";
                if (actionText.Contains("BP")) return "勝利的 Lane 會保留，失敗的 Lane 會橫置";
            }

            if (phaseManager == null) return "文字提示說明目標，高亮提示顯示可操作區域";

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                    return "拖放到中央場景區；沒有合適卡牌可略過";
                case UcgGamePhase.CharacterSetup:
                    return "放到目前開放的 Lane 上";
                case UcgGamePhase.Upgrade:
                    return "疊放到同名角色上，或點擊結束升級階段";
                case UcgGamePhase.EnterEffect:
                case UcgGamePhase.BattleEffect:
                    return "依照高亮提示完成操作";
                case UcgGamePhase.BattleJudgement:
                    return "勝利的 Lane 會保留，失敗的 Lane 會橫置";
                case UcgGamePhase.End:
                    return "準備進入下一回合";
                default:
                    return "文字提示說明目標，高亮提示顯示可操作區域";
            }
        }

        void SetTopPromptAction(string message)
        {
            string actionPrompt = ExtractActionPrompt(message);
            if (!string.IsNullOrWhiteSpace(actionPrompt))
            {
                _topPromptActionText = actionPrompt;
            }
        }

        void CaptureActionFromTransientHudText()
        {
            if (playResultText == null) return;
            string existing = playResultText.text;
            if (string.IsNullOrWhiteSpace(existing)) return;
            if (existing.Contains("<size=") || existing.Contains("<color=")) return;

            SetTopPromptAction(existing);
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

        void SetGameResultHudVisible(bool visible)
        {
            if (gameResultText != null)
            {
                gameResultText.gameObject.SetActive(visible);
            }

            if (canvas == null) return;
            Transform panel = canvas.transform.Find("Game Result HUD Panel");
            if (panel != null)
            {
                panel.gameObject.SetActive(visible);
            }
        }

        void EnsureCardInfoPanel()
        {
            const string panelName = "Card Info Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(UcgCardInfoPanel));
                panelObject.transform.SetParent(canvas.transform, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
                cardInfoPanel = panelObject.GetComponent<UcgCardInfoPanel>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
                cardInfoPanel = existingPanel.GetComponent<UcgCardInfoPanel>();
                if (cardInfoPanel == null) cardInfoPanel = existingPanel.gameObject.AddComponent<UcgCardInfoPanel>();
            }

            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(36f, -118f);
            panelRect.sizeDelta = new Vector2(420f, 156f);

            panelImage.color = new Color(0.025f, 0.055f, 0.08f, 0.68f);
            panelImage.raycastTarget = false;
            var panelOutline = panelRect.GetComponent<Outline>();
            if (panelOutline == null) panelOutline = panelRect.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.5f, 0.86f, 1f, 0.2f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            cardInfoPanel.infoText = EnsureCardInfoText(panelRect);
            cardInfoPanel.Clear();
        }

        Text EnsureCardInfoText(RectTransform parent)
        {
            const string textName = "Info Text";
            Transform existingText = parent.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -12f);

            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                text.font = placeholderFont;
            }
            text.fontSize = 18;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 18;
            text.raycastTarget = false;

            return text;
        }

        void EnsureTutorialGuide()
        {
            const string panelName = "Tutorial Goal Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(UcgTutorialGuide));
                panelObject.transform.SetParent(canvas.transform, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
                tutorialGuide = panelObject.GetComponent<UcgTutorialGuide>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
                tutorialGuide = existingPanel.GetComponent<UcgTutorialGuide>();
                if (tutorialGuide == null) tutorialGuide = existingPanel.gameObject.AddComponent<UcgTutorialGuide>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            panelImage.color = new Color(0.05f, 0.09f, 0.12f, 0.78f);
            panelImage.raycastTarget = false;

            EnsureTutorialIcon(panelRect);
            tutorialGuide.tutorialText = EnsureTutorialText(panelRect);
            ApplyTutorialPanelNormalStyle();
            tutorialGuide.ResetForMode(currentTestMode);
        }

        void ApplyTutorialPanelNormalStyle()
        {
            if (tutorialGuide == null) return;

            RectTransform panelRect = tutorialGuide.transform as RectTransform;
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = new Vector2(0f, 0f);
                panelRect.sizeDelta = Vector2.zero;
                panelRect.localScale = Vector3.one;
            }

            Image panelImage = tutorialGuide.GetComponent<Image>();
            if (panelImage != null)
            {
                ApplyRoundedPanelImage(panelImage);
                panelImage.color = Color.clear;
                panelImage.raycastTarget = false;
            }

            CanvasGroup panelGroup = tutorialGuide.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = tutorialGuide.gameObject.AddComponent<CanvasGroup>();
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.blocksRaycasts = false;
            }

            Outline panelOutline = tutorialGuide.GetComponent<Outline>();
            if (panelOutline == null) panelOutline = tutorialGuide.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = Color.clear;
            panelOutline.effectDistance = Vector2.zero;

            Shadow panelShadow = EnsureUiShadow(tutorialGuide.gameObject);
            panelShadow.effectColor = Color.clear;
            panelShadow.effectDistance = Vector2.zero;
            panelShadow.useGraphicAlpha = true;

            SetTutorialIconVisible(false);
            SetTutorialTextLayout(true);

            Text text = tutorialGuide.tutorialText;
            if (text != null)
            {
                text.fontSize = 24;
                text.resizeTextMinSize = 15;
                text.resizeTextMaxSize = 26;
                text.alignment = TextAnchor.MiddleCenter;
                text.lineSpacing = 0.92f;
                text.color = Color.clear;
                text.supportRichText = true;
                text.raycastTarget = false;
            }
        }

        void ApplyTutorialPanelFinishStyle()
        {
            if (tutorialGuide == null) return;

            RectTransform panelRect = tutorialGuide.transform as RectTransform;
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(780f, 360f);
                panelRect.localScale = Vector3.one;
                panelRect.SetAsLastSibling();
            }

            Image panelImage = tutorialGuide.GetComponent<Image>();
            if (panelImage != null)
            {
                ApplyRoundedPanelImage(panelImage);
                panelImage.color = UcgToolUiPalette.DeepGlass;
                panelImage.raycastTarget = false;
            }

            Outline panelOutline = tutorialGuide.GetComponent<Outline>();
            if (panelOutline == null) panelOutline = tutorialGuide.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.34f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            Shadow panelShadow = EnsureUiShadow(tutorialGuide.gameObject);
            panelShadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.28f);
            panelShadow.effectDistance = new Vector2(0f, -8f);
            panelShadow.useGraphicAlpha = true;

            CanvasGroup panelGroup = tutorialGuide.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = tutorialGuide.gameObject.AddComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;

            Canvas panelCanvas = tutorialGuide.GetComponent<Canvas>();
            if (panelCanvas == null) panelCanvas = tutorialGuide.gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 30000;

            SetTutorialIconVisible(false);
            SetTutorialTextLayout(false);

            Text text = tutorialGuide.tutorialText;
            if (text != null)
            {
                text.fontSize = 30;
                text.resizeTextMinSize = 18;
                text.resizeTextMaxSize = 30;
                text.alignment = TextAnchor.MiddleCenter;
                text.lineSpacing = 1.18f;
                text.color = UcgToolUiPalette.SoftWhite;
                text.raycastTarget = false;
            }
        }

        Text EnsureTutorialText(RectTransform parent)
        {
            const string textName = "Tutorial Text";
            Transform existingText = parent.Find(textName);
            RectTransform textRect;
            Text text;

            if (existingText == null)
            {
                var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existingText as RectTransform;
                text = existingText.GetComponent<Text>();
                if (text == null) text = existingText.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 12f);
            textRect.offsetMax = new Vector2(-22f, -42f);

            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                text.font = placeholderFont;
            }
            text.fontSize = 24;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15;
            text.resizeTextMaxSize = 26;
            text.supportRichText = true;
            text.raycastTarget = false;

            return text;
        }

        void EnsureTutorialIcon(RectTransform parent)
        {
            const string iconFrameName = "Tutorial Icon Frame";
            Transform existingFrame = parent.Find(iconFrameName);
            RectTransform iconFrameRect;
            Image iconFrameImage;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(iconFrameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(parent, false);
                iconFrameRect = frameObject.GetComponent<RectTransform>();
                iconFrameImage = frameObject.GetComponent<Image>();
            }
            else
            {
                iconFrameRect = existingFrame as RectTransform;
                iconFrameImage = existingFrame.GetComponent<Image>();
                if (iconFrameImage == null) iconFrameImage = existingFrame.gameObject.AddComponent<Image>();
                if (existingFrame.GetComponent<Outline>() == null) existingFrame.gameObject.AddComponent<Outline>();
            }

            iconFrameRect.anchorMin = new Vector2(0.5f, 1f);
            iconFrameRect.anchorMax = new Vector2(0.5f, 1f);
            iconFrameRect.pivot = new Vector2(0.5f, 1f);
            iconFrameRect.anchoredPosition = new Vector2(0f, -8f);
            iconFrameRect.sizeDelta = new Vector2(32f, 32f);
            iconFrameRect.localScale = Vector3.one;
            iconFrameRect.localEulerAngles = Vector3.zero;
            iconFrameRect.SetAsLastSibling();
            ApplyRoundedPanelImage(iconFrameImage);
            iconFrameImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.16f);
            iconFrameImage.raycastTarget = false;

            Outline iconFrameOutline = iconFrameRect.GetComponent<Outline>();
            iconFrameOutline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.68f);
            iconFrameOutline.effectDistance = new Vector2(1.5f, -1.5f);
            iconFrameOutline.useGraphicAlpha = true;

            const string iconName = "Tutorial Icon";
            Transform existingIcon = iconFrameRect.Find(iconName);
            RectTransform iconRect;
            Text iconText;
            if (existingIcon == null)
            {
                var iconObject = new GameObject(iconName, typeof(RectTransform), typeof(Text), typeof(Outline));
                iconObject.transform.SetParent(iconFrameRect, false);
                iconRect = iconObject.GetComponent<RectTransform>();
                iconText = iconObject.GetComponent<Text>();
            }
            else
            {
                iconRect = existingIcon as RectTransform;
                iconText = existingIcon.GetComponent<Text>();
                if (iconText == null) iconText = existingIcon.gameObject.AddComponent<Text>();
                if (existingIcon.GetComponent<Outline>() == null) existingIcon.gameObject.AddComponent<Outline>();
            }

            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconText.text = "!";
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = UcgToolUiPalette.BrandPinkLight;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null) iconText.font = placeholderFont;
            iconText.fontSize = 24;
            iconText.fontStyle = FontStyle.Bold;
            iconText.raycastTarget = false;

            Outline iconOutline = iconText.GetComponent<Outline>();
            iconOutline.effectColor = new Color(0f, 0f, 0f, 0.62f);
            iconOutline.effectDistance = new Vector2(1f, -1f);
        }

        void SetTutorialIconVisible(bool visible)
        {
            if (tutorialGuide == null) return;
            Transform iconFrame = tutorialGuide.transform.Find("Tutorial Icon Frame");
            if (iconFrame != null) iconFrame.gameObject.SetActive(visible);
        }

        void SetTutorialTextLayout(bool withIcon)
        {
            if (tutorialGuide == null || tutorialGuide.tutorialText == null) return;
            RectTransform textRect = tutorialGuide.tutorialText.transform as RectTransform;
            if (textRect == null) return;

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = withIcon ? new Vector2(22f, 12f) : new Vector2(36f, 32f);
            textRect.offsetMax = withIcon ? new Vector2(-22f, -42f) : new Vector2(-36f, -32f);
        }

        void EnsureTutorialPanelTopLayer()
        {
            if (_isTutorialFinishWaitingForClick) return;
            HideTutorialPanelNormalVisual();
        }

        void HideTutorialPanelNormalVisual()
        {
            if (tutorialGuide == null) return;

            Canvas panelCanvas = tutorialGuide.GetComponent<Canvas>();
            if (panelCanvas == null) panelCanvas = tutorialGuide.gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = false;
            panelCanvas.sortingOrder = 0;

            CanvasGroup panelGroup = tutorialGuide.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = tutorialGuide.gameObject.AddComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;

            RectTransform panelRect = tutorialGuide.transform as RectTransform;
            if (panelRect != null)
            {
                panelRect.sizeDelta = Vector2.zero;
            }
        }

        void EnsureRestartButton()
        {
            const string buttonName = "Restart Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                restartButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                restartButton = existingButton.GetComponent<Button>();
                if (restartButton == null) restartButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-36f, -64f);
            buttonRect.sizeDelta = new Vector2(178f, 48f);

            var image = restartButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyTopHudButtonStyle(restartButton);
            restartButton.onClick.RemoveListener(RestartDemo);
            restartButton.onClick.AddListener(RestartDemo);

            EnsureRestartButtonLabel(buttonRect);
        }

        void EnsureSwitchTestButton()
        {
            const string buttonName = "Switch Test Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                switchTestButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                switchTestButton = existingButton.GetComponent<Button>();
                if (switchTestButton == null) switchTestButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-36f, -118f);
            buttonRect.sizeDelta = new Vector2(178f, 48f);

            var image = switchTestButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyTopHudButtonStyle(switchTestButton);
            switchTestButton.onClick.RemoveListener(SwitchTestMode);
            switchTestButton.onClick.AddListener(SwitchTestMode);

            EnsureButtonLabelWithIcon(buttonRect, "⇄", "切換測試");
        }

        void EnsureSkipTutorialButton()
        {
            const string buttonName = "Skip Tutorial Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                skipTutorialButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                skipTutorialButton = existingButton.GetComponent<Button>();
                if (skipTutorialButton == null) skipTutorialButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-36f, -172f);
            buttonRect.sizeDelta = new Vector2(178f, 48f);

            var image = skipTutorialButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyTopHudButtonStyle(skipTutorialButton);
            skipTutorialButton.onClick.RemoveListener(SkipTutorialMode);
            skipTutorialButton.onClick.AddListener(SkipTutorialMode);
            skipTutorialButton.gameObject.SetActive(true);

            EnsureButtonLabelWithIcon(buttonRect, "»", "跳過教學");
        }

        void EnsureBoardDebugToggleButton()
        {
#if UNITY_EDITOR
            if (canvas == null) return;

            const string buttonName = "Board Debug Toggle Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                _boardDebugToggleButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                _boardDebugToggleButton = existingButton.GetComponent<Button>();
                if (_boardDebugToggleButton == null) _boardDebugToggleButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-36f, -220f);
            buttonRect.sizeDelta = new Vector2(188f, 42f);

            var image = _boardDebugToggleButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyHudButtonStyle(_boardDebugToggleButton, new Color(0.08f, 0.18f, 0.08f, 0.62f), new Color(0.18f, 0.56f, 0.18f, 0.9f));
            _boardDebugToggleButton.onClick.RemoveListener(ToggleBoardDebugZones);
            _boardDebugToggleButton.onClick.AddListener(ToggleBoardDebugZones);
            _boardDebugToggleButton.gameObject.SetActive(ShouldShowBoardDebugToggleButton());

            EnsureButtonLabel(buttonRect, "切換 Board Debug");
#else
            if (_boardDebugToggleButton != null)
            {
                _boardDebugToggleButton.gameObject.SetActive(false);
            }
#endif
        }

        bool ShouldShowBoardDebugToggleButton()
        {
            return debugBoardZones
                || debugBattlefieldLayout
                || debugLayoutDiagnostics
                || debugForceSidePileExtremeOffset;
        }

        void ToggleBoardDebugZones()
        {
            debugBoardZones = !debugBoardZones;
            _debugBoardZonesStateLogged = false;
            _layoutDebugBoundsLogged = false;
            UpdateDebugBoardZonesActivePanel();
            ApplyBoardZoneDebugVisualState();
            UpdateLayoutDebugBounds();
            if (debugBoardZones)
            {
                RefreshPileSideRegionDebugVisibility();
            }
            LogDebugBoardZonesState("ToggleBoardDebugZones", true);
            LogUcgHandDemoInstances("ToggleBoardDebugZones");
            LogPileRegionModeCompare("ToggleBoardDebugZones", true);
            LogPileSideInternalLayoutFromCurrent("ToggleBoardDebugZones");
            if (_boardDebugToggleButton != null)
            {
                _boardDebugToggleButton.gameObject.SetActive(ShouldShowBoardDebugToggleButton());
            }
        }

        void EnsureEffectTestToolPanel()
        {
            if (canvas == null) return;

            const string panelName = "Effect Test Tool Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelObject.transform.SetParent(canvas.transform, false);
                _effectTestToolPanel = panelObject.GetComponent<RectTransform>();
            }
            else
            {
                _effectTestToolPanel = existingPanel as RectTransform;
                if (existingPanel.GetComponent<Image>() == null) existingPanel.gameObject.AddComponent<Image>();
                if (existingPanel.GetComponent<CanvasGroup>() == null) existingPanel.gameObject.AddComponent<CanvasGroup>();
            }

            _effectTestToolPanel.anchorMin = new Vector2(1f, 1f);
            _effectTestToolPanel.anchorMax = new Vector2(1f, 1f);
            _effectTestToolPanel.pivot = new Vector2(1f, 1f);
            _effectTestToolPanel.anchoredPosition = new Vector2(-36f, -272f);
            _effectTestToolPanel.sizeDelta = new Vector2(256f, 306f);
            _effectTestToolPanel.localScale = Vector3.one;
            _effectTestToolPanel.localEulerAngles = Vector3.zero;

            Image panelImage = _effectTestToolPanel.GetComponent<Image>();
            panelImage.color = new Color(0.03f, 0.08f, 0.12f, 0.84f);
            panelImage.raycastTarget = true;
            ApplyRoundedPanelImage(panelImage);

            Outline outline = _effectTestToolPanel.GetComponent<Outline>();
            if (outline == null) outline = _effectTestToolPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.48f, 0.9f, 1f, 0.28f);
            outline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup canvasGroup = _effectTestToolPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            Font font = LoadPlaceholderFont();
            Text title = EnsureZoneText(
                _effectTestToolPanel,
                "Effect Test Tool Title",
                new Vector2(0.08f, 0.83f),
                new Vector2(0.92f, 0.98f),
                font,
                16,
                new Color(0.88f, 1f, 1f, 0.95f));
            title.text = "效果測試工具";

            _effectTestSelectedCardText = EnsureZoneText(
                _effectTestToolPanel,
                "Effect Test Selected Card",
                new Vector2(0.08f, 0.69f),
                new Vector2(0.92f, 0.84f),
                font,
                15,
                new Color(1f, 0.96f, 0.72f, 0.96f));

            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Previous Card Button", new Vector2(-62f, 58f), new Vector2(104f, 32f), "上一張", SelectPreviousEffectTestCard);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Next Card Button", new Vector2(62f, 58f), new Vector2(104f, 32f), "下一張", SelectNextEffectTestCard);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Player Hand Button", new Vector2(-62f, 16f), new Vector2(104f, 34f), "+我方手牌", AddEffectTestCardToPlayerHand);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Opponent Hand Button", new Vector2(62f, 16f), new Vector2(104f, 34f), "+對手手牌", AddEffectTestCardToOpponentHand);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Player Discard Button", new Vector2(-62f, -28f), new Vector2(104f, 34f), "+我方棄牌", AddEffectTestCardToPlayerDiscard);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Opponent Discard Button", new Vector2(62f, -28f), new Vector2(104f, 34f), "+對手棄牌", AddEffectTestCardToOpponentDiscard);
            EnsureEffectTestToolButton(_effectTestToolPanel, "Effect Test Run Self Test Button", new Vector2(0f, -76f), new Vector2(228f, 34f), "Run Effect Self Test", RunEffectSelfTestFromDebugTool);

            UpdateEffectTestSelectedCardText();
            UpdateEffectTestToolPanelVisibility();
        }

        Button EnsureEffectTestToolButton(
            RectTransform parent,
            string buttonName,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            UnityEngine.Events.UnityAction onClick)
        {
            Transform existingButton = parent.Find(buttonName);
            RectTransform buttonRect;
            Button button;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                button = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                button = existingButton.GetComponent<Button>();
                if (button == null) button = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;

            ApplyHudButtonStyle(button, new Color(0.07f, 0.18f, 0.26f, 0.82f), new Color(0.14f, 0.36f, 0.5f, 0.96f));
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            EnsureButtonLabel(buttonRect, label);
            return button;
        }

        bool ShouldShowEffectTestToolPanel()
        {
            return debugEffectTestTools && (Application.isEditor || Debug.isDebugBuild);
        }

        void UpdateEffectTestToolPanelVisibility()
        {
            if (_effectTestToolPanel == null) return;

            bool visible = ShouldShowEffectTestToolPanel();
            if (_effectTestToolPanel.gameObject.activeSelf != visible)
            {
                _effectTestToolPanel.gameObject.SetActive(visible);
            }

            if (visible)
            {
                _effectTestToolPanel.SetAsLastSibling();
            }
        }

        void SelectPreviousEffectTestCard()
        {
            if (EffectTestCardIds.Length == 0) return;
            _effectTestSelectedCardIndex = (_effectTestSelectedCardIndex - 1 + EffectTestCardIds.Length) % EffectTestCardIds.Length;
            UpdateEffectTestSelectedCardText();
        }

        void SelectNextEffectTestCard()
        {
            if (EffectTestCardIds.Length == 0) return;
            _effectTestSelectedCardIndex = (_effectTestSelectedCardIndex + 1) % EffectTestCardIds.Length;
            UpdateEffectTestSelectedCardText();
        }

        void UpdateEffectTestSelectedCardText()
        {
            if (_effectTestSelectedCardText == null) return;
            _effectTestSelectedCardText.text = $"測試卡：{GetSelectedEffectTestCardId()}";
        }

        string GetSelectedEffectTestCardId()
        {
            if (EffectTestCardIds.Length == 0) return "";
            _effectTestSelectedCardIndex = Mathf.Clamp(_effectTestSelectedCardIndex, 0, EffectTestCardIds.Length - 1);
            return EffectTestCardIds[_effectTestSelectedCardIndex];
        }

        void AddEffectTestCardToPlayerHand()
        {
            UcgCardData card = CreateEffectTestRuntimeCard();
            if (card == null) return;

            if (deckManager != null)
            {
                deckManager.playerHand.Add(card);
            }
            AddCardToHand(card);
            RefreshHandLayout();
            ShowEffectTestInjectionResult(card, "我方手牌");
        }

        void AddEffectTestCardToOpponentHand()
        {
            UcgCardData card = CreateEffectTestRuntimeCard();
            if (card == null) return;

            if (deckManager != null)
            {
                deckManager.opponentHiddenHand.Add(card);
            }
            SyncOpponentZoneCountsFromDeckManager();
            RefreshZoneInfoUI();
            ShowEffectTestInjectionResult(card, "對手手牌");
        }

        void AddEffectTestCardToPlayerDiscard()
        {
            UcgCardData card = CreateEffectTestRuntimeCard();
            if (card == null) return;

            _playerDiscardPile.Add(card);
            RefreshZoneInfoUI();
            ShowEffectTestInjectionResult(card, "我方棄牌區");
        }

        void AddEffectTestCardToOpponentDiscard()
        {
            UcgCardData card = CreateEffectTestRuntimeCard();
            if (card == null) return;

            _opponentDiscardPile.Add(card);
            RefreshZoneInfoUI();
            ShowEffectTestInjectionResult(card, "對手棄牌區");
        }

        UcgCardData CreateEffectTestRuntimeCard()
        {
            string cardId = GetSelectedEffectTestCardId();
            if (string.IsNullOrWhiteSpace(cardId))
            {
                ShowPlayStatus("效果測試：尚未選擇卡片。", 1.2f);
                return null;
            }

            UcgCardData source = externalCardDatabase != null ? externalCardDatabase.GetCardById(cardId) : null;
            if (source == null && externalCardDatabase != null)
            {
                source = externalCardDatabase.GetCardBySku(cardId);
            }
            if (source == null)
            {
                ShowPlayStatus($"效果測試：找不到 {cardId}。", 1.2f);
                Debug.LogWarning($"Effect test card not found: {cardId}");
                return null;
            }

            UcgCardData card = UcgDeckDefinitionResolver.CloneCard(source);
            UcgEffectParser.ApplyExecutableDemoMapping(card);
            return card;
        }

        void ShowEffectTestInjectionResult(UcgCardData card, string destination)
        {
            string cardName = GetCardDisplayName(card);
            string message = $"效果測試：已將 {cardName} 加入{destination}。";
            ShowPlayStatus(message, 1.2f);
            if (playResultText != null)
            {
                playResultText.text = message;
            }
            Debug.Log($"EffectTestTool: card={card?.id}, destination={destination}");
        }

        void RunEffectSelfTestFromDebugTool()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool passed = UcgDemoEffectSelfTest.RunAndLog();
            string message = passed
                ? "效果自動檢查：PASS。"
                : "效果自動檢查：FAIL，請查看 Console。";
            ShowPlayStatus(message, 1.4f);
            if (playResultText != null)
            {
                playResultText.text = message;
            }
#else
            Debug.Log("Effect self-test is only available in Unity Editor or Debug builds.");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Debug Run Effect Self Test")]
        void DebugRunEffectSelfTestContextMenu()
        {
            RunEffectSelfTestFromDebugTool();
        }
#endif

        void SkipTutorialMode()
        {
            StopTutorialCompletionRoutine();
            HideTutorialFinishClickLayer();
            if (tutorialGuide != null)
            {
                tutorialGuide.SkipTutorial();
                tutorialGuide.gameObject.SetActive(false);
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }

            ClearEffectTargetHighlights();
            RefreshInteractionHints();
            UpdateMainPrompt();
        }

        void BeginTutorialCompletionRoutine()
        {
            if (_isTutorialFinishWaitingForClick) return;
            if (tutorialGuide == null) return;

            _isTutorialFinishWaitingForClick = true;
            _tutorialFinishedNotified = false;
            tutorialGuide.gameObject.SetActive(true);
            ApplyTutorialPanelFinishStyle();
            tutorialGuide.ShowTutorialCompleteMessage();
            if (sfxController != null)
            {
                sfxController.PlayTutorialComplete();
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }

            ClearEffectTargetHighlights();
            ClearInteractionHints();
            ClearEffectFeedback();
            ShowTutorialFinishClickLayer();
            _tutorialCompletionRoutine = StartCoroutine(FadeTutorialFinishPanelRoutine());
            RefreshNextPhaseButtonState();
        }

        void StopTutorialCompletionRoutine()
        {
            if (_tutorialCompletionRoutine != null)
            {
                StopCoroutine(_tutorialCompletionRoutine);
                _tutorialCompletionRoutine = null;
            }

            _isTutorialFinishWaitingForClick = false;
            _tutorialFinishedNotified = false;
            HideTutorialFinishClickLayer();
            ApplyTutorialPanelNormalStyle();
        }

        void BeginOpeningFirstPlayerSequence()
        {
            StopOpeningFirstPlayerRoutine();
            if (phaseManager != null)
            {
                phaseManager.SetPhase(UcgGamePhase.Start);
            }

            _isOpeningFirstPlayerSequence = true;
            _openingFirstPlayerRoutine = StartCoroutine(OpeningFirstPlayerSequenceRoutine());
        }

        void StopOpeningFirstPlayerRoutine()
        {
            if (_openingFirstPlayerRoutine != null)
            {
                StopCoroutine(_openingFirstPlayerRoutine);
                _openingFirstPlayerRoutine = null;
            }

            _isOpeningFirstPlayerSequence = false;
            _isOpeningCameraIntro = false;
            RestoreOpeningCameraScrollDuration();
            StopTurnStartBannerRoutine();
            SetHandCardsInteractable(true, null);
        }

        IEnumerator OpeningFirstPlayerSequenceRoutine()
        {
            ClearInteractionHints();
            ClearPlayableHandHighlights();
            SetHandCardsInteractable(false, null);
            RefreshNextPhaseButtonState();
            _isOpeningCameraIntro = true;

            if (playResultText != null)
            {
                playResultText.text = "";
            }

            ShowOpeningBattlefieldOverview();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, openingOverviewHoldSeconds));
            yield return SmoothOpeningBattlefieldFocus();
            _isOpeningCameraIntro = false;

            string resultMessage = string.IsNullOrWhiteSpace(_openingFirstPlayerMessage)
                ? (turnOrderManager != null ? turnOrderManager.GetOpeningFirstPlayerText() : "正面，我方先攻！")
                : _openingFirstPlayerMessage;

            if (playResultText != null)
            {
                playResultText.text = resultMessage;
            }
            if (tutorialGuide != null)
            {
                tutorialGuide.ShowPhasePrompt($"{resultMessage}\n準備進入第 1 回合。");
            }

            yield return new WaitForSecondsRealtime(1.35f);

            yield return PlayTurnStartBannerForCurrentTurn();

            _openingFirstPlayerRoutine = null;
            _isOpeningFirstPlayerSequence = false;
            SetHandCardsInteractable(true, null);
            if (IsGameOver || phaseManager == null) yield break;

            phaseManager.SetPhase(UcgGamePhase.SceneSetup);
            EnterCurrentPhase();
        }

        void ShowOpeningBattlefieldOverview()
        {
            if (battlefieldManager == null) return;

            ApplyCombatFocusViewportPosition("ShowOpeningBattlefieldOverview");
            battlefieldManager.RefreshOpenedLaneVisibility(turnManager != null ? turnManager.currentTurn : 1);
            battlefieldManager.ShowOverviewInstant("OpeningCameraIntro");
            LogCombatViewportDiagnostic("ShowOpeningBattlefieldOverview", "OverviewAll", GetCurrentActiveLaneIndex());
            RefreshBoardZoneLayout();
        }

        IEnumerator SmoothOpeningBattlefieldFocus()
        {
            if (battlefieldManager == null) yield break;

            float transitionSeconds = Mathf.Clamp(openingFocusTransitionSeconds, 0.35f, 0.5f);
            _openingCameraPreviousScrollDuration = battlefieldManager.activeLaneScrollDuration;
            _openingCameraOverrodeScrollDuration = true;
            battlefieldManager.activeLaneScrollDuration = transitionSeconds;

            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : 0;
            ApplyCombatFocusViewportPosition("SmoothOpeningBattlefieldFocus");
            battlefieldManager.SmoothFocusActiveLane(activeLaneIndex < 0 ? 0 : activeLaneIndex);
            LogCombatViewportDiagnostic("SmoothOpeningBattlefieldFocus", "FocusLane", activeLaneIndex < 0 ? 0 : activeLaneIndex);
            yield return new WaitForSecondsRealtime(transitionSeconds);

            RestoreOpeningCameraScrollDuration();
            RefreshBoardZoneLayout();
        }

        void RestoreOpeningCameraScrollDuration()
        {
            if (!_openingCameraOverrodeScrollDuration) return;
            if (battlefieldManager != null)
            {
                battlefieldManager.activeLaneScrollDuration = _openingCameraPreviousScrollDuration;
            }
            _openingCameraOverrodeScrollDuration = false;
        }

        void ClearEffectFeedback()
        {
            _effectFeedbackQueue.Clear();
            _queuedEffectFeedbackMessages.Clear();
            if (_effectFeedbackRoutine != null)
            {
                StopCoroutine(_effectFeedbackRoutine);
                _effectFeedbackRoutine = null;
            }

            if (effectFeedbackText != null)
            {
                Color color = effectFeedbackText.color;
                color.a = 0f;
                effectFeedbackText.color = color;
                effectFeedbackText.text = "";
            }
        }

        void EnsureTutorialFinishClickLayer()
        {
            const string layerName = "Tutorial Finish Click Layer";
            Transform existingLayer = canvas.transform.Find(layerName);
            if (existingLayer == null)
            {
                var layerObject = new GameObject(layerName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(UcgTutorialFinishClickLayer));
                layerObject.transform.SetParent(canvas.transform, false);
                _tutorialFinishClickLayer = layerObject.GetComponent<RectTransform>();
            }
            else
            {
                _tutorialFinishClickLayer = existingLayer as RectTransform;
                if (existingLayer.GetComponent<Canvas>() == null) existingLayer.gameObject.AddComponent<Canvas>();
                if (existingLayer.GetComponent<GraphicRaycaster>() == null) existingLayer.gameObject.AddComponent<GraphicRaycaster>();
                if (existingLayer.GetComponent<Image>() == null) existingLayer.gameObject.AddComponent<Image>();
                if (existingLayer.GetComponent<UcgTutorialFinishClickLayer>() == null) existingLayer.gameObject.AddComponent<UcgTutorialFinishClickLayer>();
            }

            _tutorialFinishClickLayer.SetParent(canvas.transform, false);
            _tutorialFinishClickLayer.anchorMin = Vector2.zero;
            _tutorialFinishClickLayer.anchorMax = Vector2.one;
            _tutorialFinishClickLayer.pivot = new Vector2(0.5f, 0.5f);
            _tutorialFinishClickLayer.offsetMin = Vector2.zero;
            _tutorialFinishClickLayer.offsetMax = Vector2.zero;
            _tutorialFinishClickLayer.localScale = Vector3.one;
            _tutorialFinishClickLayer.localEulerAngles = Vector3.zero;

            var image = _tutorialFinishClickLayer.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            image.raycastTarget = true;

            var layerCanvas = _tutorialFinishClickLayer.GetComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = 25000;

            var clickLayer = _tutorialFinishClickLayer.GetComponent<UcgTutorialFinishClickLayer>();
            clickLayer.demo = this;

            _tutorialFinishClickLayer.gameObject.SetActive(false);
        }

        void ShowTutorialFinishClickLayer()
        {
            if (_tutorialFinishClickLayer == null)
            {
                EnsureTutorialFinishClickLayer();
            }

            Image image = _tutorialFinishClickLayer.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0.58f);
                image.raycastTarget = true;
            }

            _tutorialFinishClickLayer.gameObject.SetActive(true);
            _tutorialFinishClickLayer.SetAsLastSibling();
        }

        void HideTutorialFinishClickLayer()
        {
            if (_tutorialFinishClickLayer != null)
            {
                _tutorialFinishClickLayer.gameObject.SetActive(false);
            }
        }

        void EnsureDeckOperationSelectionUI()
        {
            if (canvas == null) return;

            const string rootName = "Deck Operation Selection";
            Transform existingRoot = canvas.transform.Find(rootName);
            if (existingRoot == null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
                rootObject.transform.SetParent(canvas.transform, false);
                _deckOperationSelectionRoot = rootObject.GetComponent<RectTransform>();
            }
            else
            {
                _deckOperationSelectionRoot = existingRoot as RectTransform;
                if (existingRoot.GetComponent<Canvas>() == null) existingRoot.gameObject.AddComponent<Canvas>();
                if (existingRoot.GetComponent<GraphicRaycaster>() == null) existingRoot.gameObject.AddComponent<GraphicRaycaster>();
                if (existingRoot.GetComponent<Image>() == null) existingRoot.gameObject.AddComponent<Image>();
            }

            _deckOperationSelectionRoot.anchorMin = Vector2.zero;
            _deckOperationSelectionRoot.anchorMax = Vector2.one;
            _deckOperationSelectionRoot.offsetMin = Vector2.zero;
            _deckOperationSelectionRoot.offsetMax = Vector2.zero;
            _deckOperationSelectionRoot.localScale = Vector3.one;
            _deckOperationSelectionRoot.localEulerAngles = Vector3.zero;

            var rootImage = _deckOperationSelectionRoot.GetComponent<Image>();
            rootImage.color = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.46f);
            rootImage.raycastTarget = true;

            Canvas overlayCanvas = _deckOperationSelectionRoot.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 22000;

            RectTransform panelRect = EnsureDeckOperationPanel(_deckOperationSelectionRoot);
            _deckOperationSelectionTitle = EnsureDeckOperationTitle(panelRect);
            _deckOperationCardsRoot = EnsureDeckOperationCardsRoot(panelRect);
            _deckOperationNoSelectionButton = EnsureDeckOperationNoSelectionButton(panelRect);
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }

            _deckOperationSelectionRoot.gameObject.SetActive(false);
        }

        RectTransform EnsureDeckOperationPanel(RectTransform parent)
        {
            const string panelName = "Selection Panel";
            Transform existingPanel = parent.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(Outline));
                panelObject.transform.SetParent(parent, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
                if (existingPanel.GetComponent<Outline>() == null) existingPanel.gameObject.AddComponent<Outline>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(GetRevealSelectionOffsetX(), -40f);
            panelRect.sizeDelta = new Vector2(900f, 420f);
            panelImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.24f);
            panelImage.raycastTarget = true;

            var outline = panelRect.GetComponent<Outline>();
            outline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.24f);
            outline.effectDistance = new Vector2(1f, -1f);
            return panelRect;
        }

        Text EnsureDeckOperationTitle(RectTransform parent)
        {
            const string titleName = "Selection Title";
            Transform existingTitle = parent.Find(titleName);
            RectTransform titleRect;
            Text titleText;

            if (existingTitle == null)
            {
                var titleObject = new GameObject(titleName, typeof(RectTransform), typeof(Text));
                titleObject.transform.SetParent(parent, false);
                titleRect = titleObject.GetComponent<RectTransform>();
                titleText = titleObject.GetComponent<Text>();
            }
            else
            {
                titleRect = existingTitle as RectTransform;
                titleText = existingTitle.GetComponent<Text>();
                if (titleText == null) titleText = existingTitle.gameObject.AddComponent<Text>();
            }

            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(800f, 58f);
            titleText.text = "選擇 1 張牌";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = UcgToolUiPalette.SoftWhite;
            titleText.fontSize = 23;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 16;
            titleText.resizeTextMaxSize = 23;
            titleText.raycastTarget = false;
            Font font = LoadPlaceholderFont();
            if (font != null) titleText.font = font;
            return titleText;
        }

        RectTransform EnsureDeckOperationCardsRoot(RectTransform parent)
        {
            const string cardsName = "Revealed Cards";
            Transform existingCards = parent.Find(cardsName);
            RectTransform cardsRect;

            if (existingCards == null)
            {
                var cardsObject = new GameObject(cardsName, typeof(RectTransform));
                cardsObject.transform.SetParent(parent, false);
                cardsRect = cardsObject.GetComponent<RectTransform>();
            }
            else
            {
                cardsRect = existingCards as RectTransform;
            }

            cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRect.pivot = new Vector2(0.5f, 0.5f);
            cardsRect.anchoredPosition = new Vector2(0f, -28f);
            cardsRect.sizeDelta = new Vector2(860f, 320f);
            return cardsRect;
        }

        Button EnsureDeckOperationNoSelectionButton(RectTransform parent)
        {
            const string buttonName = "No Selection Continue Button";
            Transform existingButton = parent.Find(buttonName);
            RectTransform buttonRect;
            Button button;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                button = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                button = existingButton.GetComponent<Button>();
                if (button == null) button = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 18f);
            buttonRect.sizeDelta = new Vector2(180f, 52f);

            ApplyPrimaryHudButtonStyle(button);
            button.onClick.RemoveListener(CompleteDeckOperationNoSelection);
            button.onClick.AddListener(CompleteDeckOperationNoSelection);
            EnsureButtonLabel(buttonRect, "確認");
            return button;
        }

        public void HandleTutorialFinishScreenClicked()
        {
            if (!_isTutorialFinishWaitingForClick) return;
            if (_tutorialFinishedNotified) return;

            _tutorialFinishedNotified = true;
            NotifyTutorialFinished();
        }

        void NotifyTutorialFinished()
        {
            Debug.Log($"UCG tutorial finished. Notify outer page: {UcgWebBridge.TutorialCompleteEventName}");
            UcgWebBridge.NotifyTutorialComplete();
        }

        IEnumerator FadeTutorialFinishPanelRoutine()
        {
            CanvasGroup panelGroup = tutorialGuide != null ? tutorialGuide.GetComponent<CanvasGroup>() : null;
            if (panelGroup == null) yield break;

            RectTransform panelRect = tutorialGuide.transform as RectTransform;
            const float duration = 0.34f;
            float elapsed = 0f;
            panelGroup.alpha = 0f;
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * 0.94f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                panelGroup.alpha = eased;
                if (panelRect != null)
                {
                    panelRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                }
                yield return null;
            }

            panelGroup.alpha = 1f;
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one;
            }
            _tutorialCompletionRoutine = null;
        }

        void EnsureNextTurnButton()
        {
            const string buttonName = "Next Turn Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                nextTurnButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                nextTurnButton = existingButton.GetComponent<Button>();
                if (nextTurnButton == null) nextTurnButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-36f, -276f);
            buttonRect.sizeDelta = new Vector2(190f, 62f);

            var image = nextTurnButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyPrimaryHudButtonStyle(nextTurnButton);
            nextTurnButton.onClick.RemoveListener(NextTurn);
            nextTurnButton.onClick.AddListener(NextTurn);
            nextTurnButton.gameObject.SetActive(false);

            EnsureButtonLabel(buttonRect, "下一回合");
        }

        void EnsureNextPhaseButton()
        {
            const string buttonName = "Next Phase Button";
            Transform existingButton = canvas.transform.Find(buttonName);
            RectTransform buttonRect;

            if (existingButton == null)
            {
                var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(canvas.transform, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();
                nextPhaseButton = buttonObject.GetComponent<Button>();
            }
            else
            {
                buttonRect = existingButton as RectTransform;
                nextPhaseButton = existingButton.GetComponent<Button>();
                if (nextPhaseButton == null) nextPhaseButton = existingButton.gameObject.AddComponent<Button>();
                if (existingButton.GetComponent<Image>() == null) existingButton.gameObject.AddComponent<Image>();
            }

            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, 72f);
            buttonRect.sizeDelta = new Vector2(220f, 58f);

            var image = nextPhaseButton.GetComponent<Image>();
            image.raycastTarget = true;
            ApplyHudButtonStyle(nextPhaseButton, new Color(0.02f, 0.07f, 0.11f, 0.78f), new Color(0.05f, 0.18f, 0.26f, 0.92f));
            nextPhaseButton.onClick.RemoveListener(NextPhase);
            nextPhaseButton.onClick.AddListener(NextPhase);
            nextPhaseButton.gameObject.SetActive(false);

            EnsureAdvancePromptVisual(buttonRect);
        }

        void EnsureAdvancePromptVisual(RectTransform promptRect)
        {
            if (promptRect == null || nextPhaseButton == null) return;

            promptRect.anchorMin = new Vector2(0.5f, 1f);
            promptRect.anchorMax = new Vector2(0.5f, 1f);
            promptRect.pivot = new Vector2(0.5f, 1f);
            promptRect.anchoredPosition = new Vector2(0f, -56f);
            promptRect.sizeDelta = new Vector2(704f, 132f);
            promptRect.localScale = Vector3.one;
            promptRect.localEulerAngles = Vector3.zero;

            var panelImage = nextPhaseButton.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = Color.clear;
                panelImage.raycastTarget = true;
            }

            var outline = nextPhaseButton.GetComponent<Outline>();
            if (outline == null) outline = nextPhaseButton.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.clear;
            outline.effectDistance = Vector2.zero;
            outline.useGraphicAlpha = false;

            ColorBlock colors = nextPhaseButton.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = Color.clear;
            colors.pressedColor = Color.clear;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.clear;
            colors.colorMultiplier = 1f;
            nextPhaseButton.colors = colors;

            _advancePromptMainText = EnsureAdvancePromptText(
                promptRect,
                "Text",
                new Vector2(0f, 0.48f),
                Vector2.one,
                new Vector2(20f, 0f),
                new Vector2(-20f, -5f),
                25,
                Color.clear,
                TextAnchor.MiddleCenter);
            _advancePromptCountdownText = EnsureAdvancePromptText(
                promptRect,
                "Countdown Text",
                new Vector2(0f, 0.2f),
                new Vector2(1f, 0.5f),
                new Vector2(20f, 0f),
                new Vector2(-20f, 0f),
                15,
                Color.clear,
                TextAnchor.MiddleCenter);

            EnsureAdvancePromptProgress(promptRect);
            if (_advancePromptProgressTrackRect != null)
            {
                _advancePromptProgressTrackRect.gameObject.SetActive(false);
            }

            if (_advancePromptPulse == null)
            {
                _advancePromptPulse = nextPhaseButton.GetComponent<UcgGuidancePulse>();
                if (_advancePromptPulse == null) _advancePromptPulse = nextPhaseButton.gameObject.AddComponent<UcgGuidancePulse>();
            }
            _advancePromptPulse.targetRect = promptRect;
            _advancePromptPulse.targetOutline = outline;
            _advancePromptPulse.pulseScale = true;
            _advancePromptPulse.pulseAlpha = true;
            _advancePromptPulse.scaleAmplitude = 0.012f;
            _advancePromptPulse.alphaAmplitude = 0.08f;
            _advancePromptPulse.bobAmplitude = 0f;
            _advancePromptPulse.speed = 2.2f;
            _advancePromptPulse.CaptureBaseState();
            _advancePromptPulse.enabled = false;
        }

        Text EnsureAdvancePromptText(RectTransform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, Color color, TextAnchor alignment)
        {
            Transform existing = parent.Find(objectName);
            RectTransform textRect;
            Text text;

            if (existing == null)
            {
                var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existing as RectTransform;
                text = existing.GetComponent<Text>();
                if (text == null) text = existing.gameObject.AddComponent<Text>();
                if (existing.GetComponent<Outline>() == null) existing.gameObject.AddComponent<Outline>();
            }

            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = offsetMin;
            textRect.offsetMax = offsetMax;
            textRect.localScale = Vector3.one;
            textRect.localEulerAngles = Vector3.zero;

            text.alignment = alignment;
            text.color = color;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            Font font = LoadPlaceholderFont();
            if (font != null) text.font = font;

            var outline = text.GetComponent<Outline>();
            outline.effectColor = Color.clear;
            outline.effectDistance = Vector2.zero;
            return text;
        }

        void EnsureAdvancePromptProgress(RectTransform parent)
        {
            Transform existingTrack = parent.Find("Countdown Progress Track");
            RectTransform trackRect;
            Image trackImage;

            if (existingTrack == null)
            {
                var trackObject = new GameObject("Countdown Progress Track", typeof(RectTransform), typeof(Image));
                trackObject.transform.SetParent(parent, false);
                _advancePromptProgressTrackRect = trackObject.GetComponent<RectTransform>();
                trackRect = _advancePromptProgressTrackRect;
                trackImage = trackObject.GetComponent<Image>();
            }
            else
            {
                _advancePromptProgressTrackRect = existingTrack as RectTransform;
                trackRect = _advancePromptProgressTrackRect;
                trackImage = existingTrack.GetComponent<Image>();
                if (trackImage == null) trackImage = existingTrack.gameObject.AddComponent<Image>();
            }

            trackRect.anchorMin = new Vector2(0.08f, 0f);
            trackRect.anchorMax = new Vector2(0.92f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 12f);
            trackRect.sizeDelta = new Vector2(0f, 6f);
            trackRect.localScale = Vector3.one;
            trackRect.localEulerAngles = Vector3.zero;
            trackImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.22f);
            trackImage.raycastTarget = false;

            Transform existingFill = trackRect.Find("Countdown Progress Fill");
            Image fillImage;
            if (existingFill == null)
            {
                var fillObject = new GameObject("Countdown Progress Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(trackRect, false);
                _advancePromptProgressFillRect = fillObject.GetComponent<RectTransform>();
                fillImage = fillObject.GetComponent<Image>();
            }
            else
            {
                _advancePromptProgressFillRect = existingFill as RectTransform;
                fillImage = existingFill.GetComponent<Image>();
                if (fillImage == null) fillImage = existingFill.gameObject.AddComponent<Image>();
            }

            _advancePromptProgressFillRect.anchorMin = Vector2.zero;
            _advancePromptProgressFillRect.anchorMax = Vector2.one;
            _advancePromptProgressFillRect.offsetMin = Vector2.zero;
            _advancePromptProgressFillRect.offsetMax = Vector2.zero;
            _advancePromptProgressFillRect.localScale = Vector3.one;
            _advancePromptProgressFillRect.localEulerAngles = Vector3.zero;
            fillImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.86f);
            fillImage.raycastTarget = false;
        }

        void HideBattleJudgeButton()
        {
            Transform existingButton = canvas.transform.Find("Battle Judge Button");
            if (existingButton != null)
            {
                existingButton.gameObject.SetActive(false);
            }
        }

        void EnsureDragLayer()
        {
            const string layerName = "DragLayer";
            Transform existingLayer = canvas.transform.Find(layerName);
            RectTransform layerRect;
            Canvas layerCanvas;

            if (existingLayer == null)
            {
                var layerObject = new GameObject(layerName, typeof(RectTransform), typeof(Canvas));
                layerObject.transform.SetParent(canvas.transform, false);
                layerRect = layerObject.GetComponent<RectTransform>();
                layerCanvas = layerObject.GetComponent<Canvas>();
            }
            else
            {
                layerRect = existingLayer as RectTransform;
                layerCanvas = existingLayer.GetComponent<Canvas>();
                if (layerCanvas == null) layerCanvas = existingLayer.gameObject.AddComponent<Canvas>();
            }

            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.pivot = new Vector2(0.5f, 0.5f);

            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = 9999;
            dragLayer = layerRect;
            dragLayer.SetAsLastSibling();
        }

        void EnsureRestartButtonLabel(RectTransform parent)
        {
            EnsureButtonLabelWithIcon(parent, "↻", "重新開始");
        }

        void ApplyTopHudButtonStyle(Button button)
        {
            if (button == null) return;

            Color normalColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.72f);
            Color highlightedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.9f);
            Color pressedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.82f);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                ApplyRoundedPanelImage(image);
                image.color = normalColor;
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.34f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(button.gameObject);
            shadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = highlightedColor;
            colors.disabledColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.38f);
            colors.colorMultiplier = 1.08f;
            colors.fadeDuration = 0.08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
        }

        void ApplyPrimaryHudButtonStyle(Button button)
        {
            if (button == null) return;

            Color normalColor = UcgToolUiPalette.BrandPink;
            Color highlightedColor = UcgToolUiPalette.BrandPinkLight;
            Color pressedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.78f);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                ApplyRoundedPanelImage(image);
                image.color = normalColor;
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.28f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(button.gameObject);
            shadow.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.22f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = highlightedColor;
            colors.disabledColor = new Color(148f / 255f, 163f / 255f, 184f / 255f, 0.54f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
        }

        void EnsureButtonLabelWithIcon(RectTransform parent, string icon, string text)
        {
            Text iconText = EnsureButtonChildText(parent, "Icon", new Vector2(0.06f, 0f), new Vector2(0.30f, 1f));
            iconText.text = icon;
            iconText.fontSize = 18;
            iconText.resizeTextMinSize = 13;
            iconText.resizeTextMaxSize = 18;
            iconText.color = UcgToolUiPalette.BrandPinkLight;

            Text label = EnsureButtonChildText(parent, "Text", new Vector2(0.30f, 0f), new Vector2(0.94f, 1f));
            label.text = text;
            label.fontSize = 16;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 16;
            label.color = UcgToolUiPalette.SoftWhite;
        }

        Text EnsureButtonChildText(RectTransform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existing = parent.Find(objectName);
            RectTransform textRect;
            Text text;

            if (existing == null)
            {
                var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                textRect = textObject.GetComponent<RectTransform>();
                text = textObject.GetComponent<Text>();
            }
            else
            {
                textRect = existing as RectTransform;
                text = existing.GetComponent<Text>();
                if (text == null) text = existing.gameObject.AddComponent<Text>();
            }

            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null) text.font = placeholderFont;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.raycastTarget = false;
            return text;
        }

        void EnsureButtonLabel(RectTransform parent, string text)
        {
            const string labelName = "Text";
            Transform existingLabel = parent.Find(labelName);
            RectTransform labelRect;
            Text label;

            if (existingLabel == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(parent, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                label = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existingLabel as RectTransform;
                label = existingLabel.GetComponent<Text>();
                if (label == null) label = existingLabel.gameObject.AddComponent<Text>();
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                label.font = placeholderFont;
            }
            label.fontSize = 22;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
        }

        public void RestartDemo()
        {
            RestartDemo("已重新開始");
        }

        void RestartDemo(string resultMessage)
        {
            StopOpeningFirstPlayerRoutine();
            StopTurnStartBannerRoutine();
            HideAdvanceButton();
            StopAutoPhaseRoutine();
            StopOpponentActionRoutine();
            StopSceneSetupSkipRoutine();
            StopTutorialCompletionRoutine();
            StopEffectAutoAdvanceRoutine();
            ClearPendingActionState();
            ResetGameOverState();
            ResetEffectState();
            ClearSceneSlots();
            ClearPlayableHandHighlights();
            HideDiscardPilePanel();
            _playerWonTutorialLaneIndexes.Clear();

            if (turnManager != null)
            {
                turnManager.ResetTurns();
            }

            if (turnOrderManager != null)
            {
                turnOrderManager.ResetTurnOrder();
                _openingFirstPlayerMessage = turnOrderManager.GetOpeningFirstPlayerText();
            }
            _opponentUpgradeExecutedTurn = -1;
            _advanceToUpgradeAfterOpponentSetup = false;
            _showRestoredCardsMessageOnStart = false;
            _lastTurnStartBannerShownTurn = 0;
            _sceneCardPlacedTurn = -1;
            _activatedEffectsPreparedTurn = -1;
            _enterEffectPhaseHadPendingEffects = false;
            _battleEffectPhaseHadPendingEffects = false;
            _opponentDeckCount = UcgDeckManager.DemoTemplateCount * UcgDeckManager.DemoRepeatCount - DemoCardCount;
            _opponentHandCount = DemoCardCount;
            _hasInitializedBattlefieldView = false;
            _hasInitialBattlefieldContentOffset = false;
            _initialBattlefieldContentOffsetX = 0f;
            _playerDiscardPile.Clear();
            _opponentDiscardPile.Clear();
            _temporarySceneSummons.Clear();

            if (phaseManager != null)
            {
                phaseManager.ResetPhase();
            }

            if (battlefieldManager != null)
            {
                battlefieldManager.ConfigureOpponentScript(opponentScript, currentTestMode);
                battlefieldManager.ResetBattlefield();
                ApplyInitialBattlefieldView();
                RefreshInteractionHints();
            }
            else
            {
                var playArea = playerPlayArea != null ? playerPlayArea.GetComponent<UcgPlayArea>() : null;
                if (playArea != null)
                {
                    playArea.ResetArea();
                }
            }

            if (cardInfoPanel != null)
            {
                cardInfoPanel.Clear();
            }

            if (tutorialGuide != null)
            {
                tutorialGuide.gameObject.SetActive(true);
                ApplyTutorialPanelNormalStyle();
                tutorialGuide.ResetForMode(currentTestMode);
            }
            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(true);
            }

            ResetDeckAndBuildStartingHand();
            RefreshZoneInfoUI();
            ResetGameResultText();

            if (playResultText != null)
            {
                playResultText.text = resultMessage;
            }

            BeginOpeningFirstPlayerSequence();
        }

        public void SwitchTestMode()
        {
            currentTestMode = GetNextTestMode(currentTestMode);
            RestartDemo($"目前模式：{GetTestModeName(currentTestMode)}");
        }

        public void NextTurn()
        {
            if (turnManager == null) return;
            if (_isOpeningFirstPlayerSequence) return;
            if (IsGameOver)
            {
                ShowGameOverMessage();
                return;
            }
            if (_isAutoPhaseRunning) return;
            if (_sceneSetupSkipRoutine != null) return;
            if (_isOpponentActionRunning)
            {
                ShowWaitForOpponentMessage();
                return;
            }
            if (_pendingAction != null)
            {
                ShowPendingActionMessage();
                return;
            }

            if (turnOrderManager != null)
            {
                turnOrderManager.ApplyNextFirstPlayer();
            }
            DiscardTemporarySceneSummonsForTurn(turnManager.currentTurn);
            ClearTemporaryTypeGrants();
            RestoreRestedCardsForNewTurn();
            turnManager.NextTurn();
            _showRestoredCardsMessageOnStart = true;
            if (phaseManager != null)
            {
                phaseManager.ResetPhase();
            }
            if (battlefieldManager != null)
            {
                ApplyBattlefieldViewForCurrentPhase();
            }

            UpdateMainPrompt();
        }

        public void NextPhase()
        {
            if (phaseManager == null) return;
            if (_isOpeningFirstPlayerSequence) return;
            if (_isTutorialFinishWaitingForClick) return;
            if (IsGameOver)
            {
                RestartDemo("已重新開始");
                return;
            }
            if (_isAutoPhaseRunning) return;
            if (_isOpponentActionRunning)
            {
                ShowWaitForOpponentMessage();
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.End && turnManager != null)
            {
                DiscardTemporarySceneSummonsForTurn(turnManager.currentTurn);
                ClearTemporaryBpModifiers();
                ClearTemporaryTypeGrants();
                RestoreRestedCardsForNewTurn();
                _activatedEffectsPreparedTurn = -1;
                if (effectManager != null)
                {
                    effectManager.ClearUsedActivatedEffectsThisTurn();
                }
                if (turnOrderManager != null)
                {
                    turnOrderManager.ApplyNextFirstPlayer();
                }
                turnManager.NextTurn();
                _showRestoredCardsMessageOnStart = true;
                phaseManager.SetPhase(UcgGamePhase.Start);
                EnterCurrentPhase();
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup && !CanAdvanceFromCharacterSetup())
            {
                UpdateMainPrompt();
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.SceneSetup && ShouldRunDigaOpponentSceneAfterPlayerSceneStep())
            {
                BeginOpponentSceneSetupRoutine(true);
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.Upgrade && IsCurrentFirstPlayer(UcgPlayerSide.Player))
            {
                BeginOpponentUpgradeRoutine(true);
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                if (_isSelectingEffectTarget)
                {
                    if (playResultText != null)
                    {
                        playResultText.text = "請先選擇效果目標";
                    }
                    UpdateMainPrompt();
                    return;
                }

                if (effectManager != null && effectManager.HasPendingEffects)
                {
                    SkipPlayerBattleEffectsForCurrentDecision();
                    ClearActivatedEffectSourceHighlights();
                    if (playResultText != null)
                    {
                        playResultText.text = "已結束我方戰鬥效果";
                    }
                    HandleBattleEffectEntry();
                    return;
                }
            }

            phaseManager.NextPhase();
            EnterCurrentPhase();
        }

        void EnterCurrentPhase()
        {
            ApplyBattlefieldViewForCurrentPhase();
            RefreshInteractionHints();
            if (phaseManager != null && phaseManager.CurrentPhase != UcgGamePhase.BattleEffect)
            {
                ClearActivatedEffectSourceHighlights();
            }
            RefreshNextPhaseButtonState();

            if (phaseManager.CurrentPhase == UcgGamePhase.Start)
            {
                UpdateTopPhaseHud();
                _showRestoredCardsMessageOnStart = false;
                if (ShouldPlayTurnStartBannerForCurrentTurn())
                {
                    BeginTurnStartBannerThenAutoPhase();
                }
                else
                {
                    UpdateMainPrompt();
                    BeginAutoPhaseRoutine(phaseManager.CurrentPhase);
                }
                return;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                _enterEffectPhaseHadPendingEffects = effectManager != null && effectManager.HasPendingEffects;
                _battleEffectPhaseHadPendingEffects = false;
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                _battleEffectPhaseHadPendingEffects = false;
            }
            else
            {
                _enterEffectPhaseHadPendingEffects = false;
                _battleEffectPhaseHadPendingEffects = false;
            }

            if (IsAutoPhase(phaseManager.CurrentPhase))
            {
                UpdateMainPrompt();
                BeginAutoPhaseRoutine(phaseManager.CurrentPhase);
                return;
            }

            bool handledPhaseEntryMessage = false;
            if (phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup)
            {
                handledPhaseEntryMessage = HandleCharacterSetupEntry();
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                handledPhaseEntryMessage = HandleSceneSetupEntry();
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                handledPhaseEntryMessage = HandleUpgradeEntry();
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                handledPhaseEntryMessage = HandleEnterEffectEntry();
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                handledPhaseEntryMessage = HandleBattleEffectEntry();
            }
            else if (phaseManager.CurrentPhase == UcgGamePhase.End)
            {
                handledPhaseEntryMessage = HandleEndEntry();
            }

            if (turnManager != null)
            {
                turnManager.UpdateTurnInfoText();
            }

            if (playResultText != null && !handledPhaseEntryMessage)
            {
                playResultText.text = phaseManager.GetPhaseDisplayName();
            }

            UpdateMainPrompt();
        }

        bool IsAutoPhase(UcgGamePhase phase)
        {
            return phase == UcgGamePhase.Draw
                || phase == UcgGamePhase.Open
                || phase == UcgGamePhase.BattleJudgement;
        }

        bool HandleCharacterSetupEntry()
        {
            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(GetCurrentFirstPlayer());
            if (!IsCurrentFirstPlayer(UcgPlayerSide.Opponent))
            {
                if (playResultText != null && turnManager != null)
                {
                    int laneIndex = turnManager.ActiveNewLaneIndex;
                    playResultText.text = laneIndex >= 0
                        ? $"角色設置階段：請將角色卡放到第 {laneIndex + 1} 路。"
                        : "角色設置階段：本回合不再開放新的戰鬥區。";
                }
                return true;
            }

            BeginOpponentSetupRoutine(GetActiveLane(), true);
            return true;
        }

        bool HandleSceneSetupEntry()
        {
            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(GetCurrentFirstPlayer());
            ClearPlayableHandHighlights();

            if (IsCurrentFirstPlayer(UcgPlayerSide.Opponent))
            {
                if (BeginOpponentSceneSetupRoutine(true)) return true;

                BeginSceneSetupSkipRoutine("對手目前沒有可設置的場景卡，跳過場景設置。");
                return true;
            }

            bool canPlaceScene = HasLegalSceneCardInHand();

            if (canPlaceScene)
            {
                ApplySceneCardHandHighlights(true);
                if (sharedSceneSlot != null)
                {
                    sharedSceneSlot.SetDropRaycastEnabled(true);
                    bool guideSceneSlot = IsDigaTutorialModeActive()
                        && turnManager != null
                        && turnManager.currentTurn >= 3;
                    sharedSceneSlot.SetHighlight(guideSceneSlot, false);
                }

                if (playResultText != null)
                {
                    playResultText.text = IsDigaTutorialModeActive()
                        ? "現在可以設置場景卡了，有合適的場景就放到中央場景區。"
                        : "可以設置場景卡，或跳過此階段。";
                }
                return true;
            }

            int allowedSceneLight = GetAllowedSceneLightCount();
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            string skipMessage = IsDigaTutorialModeActive() && currentTurn <= 2
                ? "場景設置階段：這回合先專心展開戰鬥區，場景卡稍後再使用。"
                : $"目前可設置最高 {allowedSceneLight} 燈場景，沒有合適場景，跳過場景設置。";
            BeginSceneSetupSkipRoutine(skipMessage);
            return true;
        }

        bool HandleUpgradeEntry()
        {
            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(GetCurrentFirstPlayer());
            if (!IsCurrentFirstPlayer(UcgPlayerSide.Opponent))
            {
                if (playResultText != null)
                {
                    playResultText.text = "升級階段：可以升級已登場角色，也可以直接結束升級。";
                }
                return true;
            }
            BeginOpponentUpgradeRoutine(false);
            return true;
        }

        bool HandleEndEntry()
        {
            if (turnOrderManager != null)
            {
                turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Player);
            }

            _isPlayerDraggingHandCard = false;
            if (_opponentActionRoutine == null)
            {
                _isOpponentActionRunning = false;
            }
            if (_playStatusRoutine != null)
            {
                StopCoroutine(_playStatusRoutine);
                _playStatusRoutine = null;
            }

            ResetAdvancePromptCountdownForNewDecision();

            if (playResultText != null)
            {
                playResultText.text =
                    "判定完成，準備進入下回合。\n" +
                    "輸掉的一路已經橫置，下一回合開始時會恢復豎置。";
            }

            RefreshNextPhaseButtonState();
            return true;
        }

        bool HandleEnterEffectEntry()
        {
            if (turnOrderManager != null && effectManager != null && effectManager.HasPendingEffects)
            {
                UcgEffectInstance nextEffect = effectManager.PeekNextEffect();
                if (nextEffect != null)
                {
                    turnOrderManager.SetCurrentActingPlayer(nextEffect.ownerSide);
                }
            }

            if (ResolveQueuedEnterEffectsInEnterPhase())
            {
                return true;
            }

            AdvanceFromEnterEffectToBattleEffect();
            return true;
        }

        void AdvanceFromEnterEffectToBattleEffect()
        {
            ClearEffectTargetSelection();
            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.EnterEffect) return;

            string message = _enterEffectPhaseHadPendingEffects
                ? "登場效果處理完成"
                : "這回合沒有可發動的登場效果，進入戰鬥效果階段。";
            BeginEffectAutoAdvanceToNextPhase(UcgGamePhase.EnterEffect, message);
        }

        bool HandleBattleEffectEntry()
        {
            PrepareActivatedEffectsForCurrentTurn();
            bool hadPendingBeforeOpponent = effectManager != null && effectManager.HasPendingEffects;
            _battleEffectPhaseHadPendingEffects |= hadPendingBeforeOpponent;
            int opponentResolvedCount = ResolveOpponentBattleEffectsInOrder();

            if (effectManager == null || !effectManager.HasPendingEffects)
            {
                ClearEffectTargetSelection();
                string message = opponentResolvedCount > 0
                    ? "對手戰鬥效果處理完成"
                    : _battleEffectPhaseHadPendingEffects
                        ? "戰鬥效果處理完成"
                        : "這回合沒有可發動的戰鬥效果，進入判定。";
                BeginEffectAutoAdvanceToJudgement(message);
                return true;
            }

            UcgEffectInstance nextEffect = effectManager.PeekNextEffect();
            if (nextEffect != null && turnOrderManager != null)
            {
                turnOrderManager.SetCurrentActingPlayer(nextEffect.ownerSide);
            }

            if (effectManager.EffectNeedsTarget(nextEffect))
            {
                if (nextEffect.ownerSide == UcgPlayerSide.Player)
                {
                    BeginEffectTargetSelection(nextEffect);
                }
                else
                {
                    StartEffectSourceHighlight(nextEffect);
                    if (TryResolveOpponentTargetedEffect(nextEffect, out string opponentTargetMessage))
                    {
                        QueueEffectFeedback(opponentTargetMessage);
                        ShowPlayStatus(opponentTargetMessage, 1.1f);
                    }
                    else
                    {
                        effectManager.RemoveEffect(nextEffect);
                        string skipMessage = string.IsNullOrWhiteSpace(opponentTargetMessage)
                            ? "對手戰鬥效果需要選擇，暫時略過"
                            : opponentTargetMessage;
                        ShowPlayStatus(skipMessage, 1.1f);
                    }
                    StopEffectSourceHighlight(nextEffect);
                    HandleBattleEffectEntry();
                }
                return true;
            }

            if (playResultText != null)
            {
                playResultText.text = "戰鬥效果階段：選擇要發動的效果，或點擊上方提示進入判定。";
            }
            return true;
        }

        void SkipPlayerBattleEffectsForCurrentDecision()
        {
            if (effectManager == null) return;

            var pendingEffects = effectManager.GetPendingEffectsSnapshot();
            for (int i = 0; i < pendingEffects.Count; i++)
            {
                UcgEffectInstance effect = pendingEffects[i];
                if (effect == null) continue;
                if (effect.timing != UcgEffectTiming.Activated) continue;
                if (effect.ownerSide != UcgPlayerSide.Player) continue;
                effectManager.RemoveEffect(effect);
            }
        }

        bool ResolveQueuedEnterEffectsInEnterPhase()
        {
            if (effectManager == null || !effectManager.HasPendingEffects) return false;

            UcgEffectInstance nextEffect = effectManager.PeekNextEffect();
            if (nextEffect == null || nextEffect.timing != UcgEffectTiming.OnRevealOrEnter) return false;

            if (playResultText != null)
            {
                string sideText = turnOrderManager != null
                    ? turnOrderManager.GetSideDisplayName(nextEffect.ownerSide)
                    : (nextEffect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手");
                playResultText.text = $"登場效果階段：{sideText}處理登場效果";
            }

            int guard = 0;
            while (effectManager.HasPendingEffects && guard < 32)
            {
                nextEffect = effectManager.PeekNextEffect();
                if (nextEffect == null || nextEffect.timing != UcgEffectTiming.OnRevealOrEnter) break;

                if (debugEffectResolution)
                {
                    UcgEffectRule rule = nextEffect.cardData != null ? UcgEffectParser.ParsePrimaryRule(nextEffect.cardData) : null;
                    int stackCount = nextEffect.ownerSide == UcgPlayerSide.Player && nextEffect.lane != null && nextEffect.lane.playerPlayArea != null
                        ? nextEffect.lane.playerPlayArea.GetStackCount()
                        : nextEffect.lane != null
                            ? nextEffect.lane.GetOpponentStackCount()
                            : 0;
                    Debug.Log(
                        "Enter effect resolving in EnterEffect phase:\n"
                        + $"card={nextEffect.cardData?.id} {nextEffect.cardData?.cardName}\n"
                        + $"effect={GetEffectText(nextEffect.cardData)}\n"
                        + $"category={(rule != null ? rule.effectCategory.ToString() : "null")}\n"
                        + $"action={(rule != null ? rule.actionType.ToString() : "null")}\n"
                        + $"stackCount={stackCount}\n"
                        + $"owner={nextEffect.ownerSide}\n"
                        + $"lane={nextEffect.LaneIndex}");
                }

                if (effectManager.EffectNeedsTarget(nextEffect))
                {
                    if (nextEffect.ownerSide == UcgPlayerSide.Player)
                    {
                        BeginEffectTargetSelection(nextEffect);
                    }
                    else
                    {
                        StartEffectSourceHighlight(nextEffect);
                        if (TryResolveOpponentTargetedEffect(nextEffect, out string opponentTargetMessage))
                        {
                            QueueEffectFeedback(opponentTargetMessage);
                            ShowPlayStatus(opponentTargetMessage, 1.1f);
                        }
                        else
                        {
                            effectManager.RemoveEffect(nextEffect);
                            string skipMessage = string.IsNullOrWhiteSpace(opponentTargetMessage)
                                ? "對手登場效果需要選擇，暫時略過"
                                : opponentTargetMessage;
                            ShowPlayStatus(skipMessage, 1.1f);
                        }
                        StopEffectSourceHighlight(nextEffect);
                        guard++;
                        continue;
                    }
                    return true;
                }

                StartEffectSourceHighlight(nextEffect);
                bool resolved = effectManager.ResolveNextEffect(this, out string message);
                if (_isSelectingDeckOperationCard)
                {
                    if (playResultText != null)
                    {
                        if (_pendingDeckSelection != null
                            && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.Hand)
                        {
                            playResultText.text = "登場效果：請選擇 1 張手牌放到底";
                        }
                        else if (_pendingDeckSelection != null
                            && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.DiscardPile)
                        {
                            playResultText.text = "登場效果：請從棄牌區選擇 1 張牌";
                        }
                        else if (_pendingDeckSelection != null
                            && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.SceneRevealCards)
                        {
                            playResultText.text = "場景效果：請選擇 1 張可升級的公開卡";
                        }
                        else if (_pendingDeckSelection != null
                            && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.TopDeckReorder)
                        {
                            playResultText.text = "登場效果：請依序選擇放回牌庫上方的順序";
                        }
                        else
                        {
                            playResultText.text = "登場效果：請選擇公開的牌";
                        }
                    }
                    return true;
                }

                if (resolved)
                {
                    QueueEffectFeedback(message);
                }

                if (playResultText != null)
                {
                    playResultText.text = message;
                }

                StopEffectSourceHighlight(nextEffect);
                guard++;
            }

            return false;
        }

        int ResolveOpponentBattleEffectsInOrder()
        {
            if (effectManager == null) return 0;

            int appliedCount = 0;
            int skippedCount = 0;
            int guard = 0;
            while (effectManager.HasPendingEffects && guard < 32)
            {
                UcgEffectInstance nextEffect = effectManager.PeekNextEffect();
                if (nextEffect == null || nextEffect.timing != UcgEffectTiming.Activated) break;
                if (nextEffect.ownerSide != UcgPlayerSide.Opponent) break;

                if (turnOrderManager != null)
                {
                    turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Opponent);
                }

                StartEffectSourceHighlight(nextEffect);
                if (effectManager.ResolveOpponentAutoBattleEffect(nextEffect, this, out string message, out string skippedReason))
                {
                    appliedCount++;
                    QueueEffectFeedback(message);
                    ShowPlayStatus(message, 1.1f);
                    effectManager.RemoveEffect(nextEffect);
                }
                else
                {
                    skippedCount++;
                    effectManager.RemoveEffect(nextEffect);
                    if (debugEffectResolution)
                    {
                        Debug.Log(
                            "Opponent battle effect skipped in BattleEffect phase:\n"
                            + $"card={nextEffect.cardData?.id} {nextEffect.cardData?.cardName}\n"
                            + $"reason={skippedReason}");
                    }
                }

                StopEffectSourceHighlight(nextEffect);
                guard++;
            }

            if (debugEffectResolution && (appliedCount > 0 || skippedCount > 0))
            {
                Debug.Log($"Opponent battle effects in order: applied={appliedCount}, skipped={skippedCount}");
            }

            return appliedCount + skippedCount;
        }

        void PrepareActivatedEffectsForCurrentTurn()
        {
            if (effectManager == null || battlefieldManager == null || turnManager == null) return;
            if (_activatedEffectsPreparedTurn == turnManager.currentTurn) return;

            effectManager.ClearQueue();
            var activatedEffects = CollectActivatedEffectsForCurrentTurn();
            effectManager.EnqueueActivatedEffects(activatedEffects, GetCurrentFirstPlayer());
            HighlightActivatedEffectSources();
            _activatedEffectsPreparedTurn = turnManager.currentTurn;
        }

        List<UcgEffectInstance> CollectActivatedEffectsForCurrentTurn()
        {
            var effects = new List<UcgEffectInstance>();
            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                CollectActivatedEffectsFromSlot(effects, lane.playerSlot, lane, UcgPlayerSide.Player);
                CollectActivatedEffectsFromSlot(effects, lane.opponentSlot, lane, UcgPlayerSide.Opponent);
            }

            CollectActivatedSceneEffect(effects);
            return effects;
        }

        void CollectActivatedEffectsFromSlot(List<UcgEffectInstance> effects, RectTransform slot, UcgBattleLane lane, UcgPlayerSide ownerSide)
        {
            if (effects == null || slot == null || lane == null) return;

            var card = GetTopCardForEffectSlot(slot, lane, ownerSide);
            if (card == null || card.CardData == null || card.IsFaceDown) return;
            if (card.CardData.effectId == UcgDemoEffectId.None) return;
            if (card.CardData.effectTiming != UcgEffectTiming.Activated) return;

            int stackCount = GetStackCountForEffectSlot(slot, lane, ownerSide);
            if (!UcgEffectParser.IsStackRequirementMet(card.CardData, stackCount, out _, out _)) return;

            if (debugEffectResolution)
            {
                Debug.Log(
                    $"Lane {lane.laneIndex + 1} battle effect source: stackCount={stackCount}, "
                    + $"topCard={card.CardData.id} {card.CardData.cardName}, owner={ownerSide}");
            }

            effects.Add(new UcgEffectInstance
            {
                effectId = card.CardData.effectId,
                cardData = card.CardData,
                sourceCard = card,
                lane = lane,
                ownerSide = ownerSide,
                timing = UcgEffectTiming.Activated,
                effectKey = $"activated:{turnManager.currentTurn}:{lane.laneIndex}:{ownerSide}:{card.CardData.id}:{card.CardData.effectId}"
            });
        }

        UcgCardView GetTopCardForEffectSlot(RectTransform slot, UcgBattleLane lane, UcgPlayerSide ownerSide)
        {
            if (ownerSide == UcgPlayerSide.Player)
            {
                if (lane != null && lane.playerPlayArea != null)
                {
                    return lane.playerPlayArea.GetTopCard();
                }

                return GetTopCardInRect(slot);
            }

            return lane != null ? lane.GetOpponentTopCard() : GetTopCardInRect(slot);
        }

        int GetStackCountForEffectSlot(RectTransform slot, UcgBattleLane lane, UcgPlayerSide ownerSide)
        {
            if (ownerSide == UcgPlayerSide.Player)
            {
                if (lane != null && lane.playerPlayArea != null)
                {
                    return lane.playerPlayArea.GetStackCount();
                }

                return CountCardViewsInRect(slot);
            }

            return lane != null ? lane.GetOpponentStackCount() : CountCardViewsInRect(slot);
        }

        UcgCardView GetTopCardInRect(RectTransform slot)
        {
            if (slot == null) return null;

            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var card = slot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null) return card;
            }

            return null;
        }

        int CountCardViewsInRect(RectTransform slot)
        {
            if (slot == null) return 0;

            int count = 0;
            for (int i = 0; i < slot.childCount; i++)
            {
                if (slot.GetChild(i).GetComponent<UcgCardView>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        void CollectActivatedSceneEffect(List<UcgEffectInstance> effects)
        {
            if (effects == null || sharedSceneSlot == null || sharedSceneSlot.SceneCardData == null) return;

            UcgCardData sceneCard = sharedSceneSlot.SceneCardData;
            if (sceneCard.sceneEffectTiming != UcgEffectTiming.Activated) return;
            UcgDemoEffectId effectId;
            switch (sceneCard.sceneEffectId)
            {
                case UcgDemoSceneEffectId.ActivatedChooseOwnLaneBpPlus1000:
                    effectId = UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000;
                    break;
                case UcgDemoSceneEffectId.ActivatedGrantOpponentTemporaryType:
                    effectId = UcgDemoEffectId.ActivatedGrantOpponentTemporaryType;
                    break;
                case UcgDemoSceneEffectId.ActivatedUpgradeFromDeckThenDiscardAtEnd:
                    effectId = UcgDemoEffectId.ActivatedSceneUpgradeFromDeckThenDiscardAtEnd;
                    break;
                default:
                    return;
            }

            effects.Add(new UcgEffectInstance
            {
                effectId = effectId,
                cardData = sceneCard,
                sourceSceneCard = sharedSceneSlot.currentSceneView,
                ownerSide = sharedSceneSlot.SceneOwner,
                timing = UcgEffectTiming.Activated,
                isSceneEffect = true,
                effectKey = $"activated-scene:{turnManager.currentTurn}:{sharedSceneSlot.SceneOwner}:{sceneCard.id}:{sceneCard.sceneEffectId}"
            });
        }

        void HighlightActivatedEffectSources()
        {
            ClearActivatedEffectSourceHighlights();
            if (effectManager == null) return;

            var pendingEffects = effectManager.GetPendingEffectsSnapshot();
            bool hasSceneEffect = false;
            for (int i = 0; i < pendingEffects.Count; i++)
            {
                UcgEffectInstance effect = pendingEffects[i];
                if (effect == null) continue;

                if (effect.sourceCard != null)
                {
                    effect.sourceCard.SetPlayableHighlight(true);
                }

                if (effect.sourceSceneCard != null)
                {
                    hasSceneEffect = true;
                }
            }

            if (hasSceneEffect && sharedSceneSlot != null)
            {
                sharedSceneSlot.SetDropRaycastEnabled(true);
                sharedSceneSlot.SetHighlight(true, false);
            }
        }

        void StartEffectSourceHighlight(UcgEffectInstance effect)
        {
            if (effect == null) return;
            if (_activeEffectSourceHighlight != null && !ReferenceEquals(_activeEffectSourceHighlight, effect))
            {
                StopCurrentEffectSourceHighlight();
            }

            if (effect.sourceCard != null)
            {
                effect.sourceCard.StartEffectSourceHighlight();
            }

            if (effect.sourceSceneCard != null)
            {
                effect.sourceSceneCard.StartEffectSourceHighlight();
            }

            _activeEffectSourceHighlight = effect;
        }

        void StopEffectSourceHighlight(UcgEffectInstance effect)
        {
            if (effect == null) return;
            if (_activeEffectSourceHighlight != null && !ReferenceEquals(_activeEffectSourceHighlight, effect)) return;

            if (effect.sourceCard != null)
            {
                effect.sourceCard.StopEffectSourceHighlight();
            }

            if (effect.sourceSceneCard != null)
            {
                effect.sourceSceneCard.StopEffectSourceHighlight();
            }

            if (_activeEffectSourceHighlight == null || ReferenceEquals(_activeEffectSourceHighlight, effect))
            {
                _activeEffectSourceHighlight = null;
            }
        }

        void StopCurrentEffectSourceHighlight()
        {
            UcgEffectInstance effect = _activeEffectSourceHighlight;
            _activeEffectSourceHighlight = null;
            if (effect == null) return;

            if (effect.sourceCard != null)
            {
                effect.sourceCard.StopEffectSourceHighlight();
            }

            if (effect.sourceSceneCard != null)
            {
                effect.sourceSceneCard.StopEffectSourceHighlight();
            }
        }

        void ClearActivatedEffectSourceHighlights()
        {
            if (battlefieldManager != null)
            {
                var lanes = battlefieldManager.GetAllLanes();
                for (int i = 0; i < lanes.Count; i++)
                {
                    ClearActivatedHighlightsInSlot(lanes[i]?.playerSlot);
                    ClearActivatedHighlightsInSlot(lanes[i]?.opponentSlot);
                }
            }

            if (sharedSceneSlot != null)
            {
                sharedSceneSlot.SetDropRaycastEnabled(false);
                sharedSceneSlot.SetHighlight(false, false);
            }
        }

        void ClearActivatedHighlightsInSlot(RectTransform slot)
        {
            if (slot == null) return;
            for (int i = 0; i < slot.childCount; i++)
            {
                var card = slot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null) card.SetPlayableHighlight(false);
            }
        }

        bool TryResolveNextEffect()
        {
            if (_isSelectingDeckOperationCard)
            {
                string selectionMessage = _pendingDeckSelection != null
                    && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.DiscardPile
                    ? "請先完成棄牌區選牌"
                    : _pendingDeckSelection != null
                        && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.SceneRevealCards
                            ? "請先完成場景公開卡選擇"
                            : _pendingDeckSelection != null
                                && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.TopDeckReorder
                                    ? "請先完成牌庫上方重排"
                                    : "請先完成選牌";
                ShowPlayStatus(selectionMessage);
                UpdateMainPrompt();
                return true;
            }

            if (_isSelectingEffectTarget)
            {
                if (playResultText != null)
                {
                    playResultText.text = "請先選擇效果目標";
                }
                UpdateMainPrompt();
                return true;
            }

            if (effectManager == null || !effectManager.HasPendingEffects) return false;

            UcgEffectInstance nextEffect = effectManager.PeekNextEffect();
            if (effectManager.EffectNeedsTarget(nextEffect))
            {
                if (nextEffect != null && nextEffect.ownerSide == UcgPlayerSide.Opponent)
                {
                    StartEffectSourceHighlight(nextEffect);
                    if (TryResolveOpponentTargetedEffect(nextEffect, out string opponentTargetMessage))
                    {
                        QueueEffectFeedback(opponentTargetMessage);
                        HighlightActivatedEffectSources();
                        ShowPlayStatus(opponentTargetMessage, 1.1f);
                        StopEffectSourceHighlight(nextEffect);
                        if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
                        {
                            HandleEnterEffectEntry();
                        }
                        else if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
                        {
                            HandleBattleEffectEntry();
                        }
                        UpdateMainPrompt();
                        return true;
                    }

                    effectManager.RemoveEffect(nextEffect);
                    string skipMessage = string.IsNullOrWhiteSpace(opponentTargetMessage)
                        ? "對手效果需要選擇，暫時略過"
                        : opponentTargetMessage;
                    ShowPlayStatus(skipMessage, 1.1f);
                    StopEffectSourceHighlight(nextEffect);
                    UpdateMainPrompt();
                    return true;
                }

                BeginEffectTargetSelection(nextEffect);
                return true;
            }

            StartEffectSourceHighlight(nextEffect);
            bool resolved = effectManager.ResolveNextEffect(this, out string message);
            if (_isSelectingDeckOperationCard)
            {
                HighlightActivatedEffectSources();
                UpdateMainPrompt();
                return true;
            }

            if (resolved)
            {
                QueueEffectFeedback(message);
            }
            HighlightActivatedEffectSources();
            ShowPlayStatus(effectManager.HasPendingEffects
                ? $"剩餘效果 {effectManager.PendingCount} 個"
                : "效果處理完畢", 1.2f);
            StopEffectSourceHighlight(nextEffect);

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
                UpdateMainPrompt();
                return true;
            }

            TryAutoAdvanceAfterTutorialEffectResolved(message);
            UpdateMainPrompt();
            return true;
        }

        void QueueEffectFeedback(string message)
        {
            if (_isTutorialFinishWaitingForClick || string.IsNullOrWhiteSpace(message)) return;

            string feedback = BuildEffectFeedbackText(message);
            if (string.IsNullOrWhiteSpace(feedback)) return;
            if (!_queuedEffectFeedbackMessages.Add(feedback)) return;

            _effectFeedbackQueue.Enqueue(feedback);
            if (_effectFeedbackRoutine == null)
            {
                _effectFeedbackRoutine = StartCoroutine(EffectFeedbackRoutine());
            }
        }

        string BuildEffectFeedbackText(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            if (message.Contains("沒有可發動效果") || message.Contains("請先選擇") || message.Contains("請選擇") || message.Contains("unsupported")) return "";

            string feedback = message;
            int separatorIndex = feedback.LastIndexOf('｜');
            if (separatorIndex >= 0 && separatorIndex < feedback.Length - 1)
            {
                feedback = feedback.Substring(separatorIndex + 1);
            }

            feedback = feedback
                .Replace("開放階段", "登場效果")
                .Replace("登場時", "登場效果")
                .Replace("效果發動階段", "戰鬥效果")
                .Replace("場景出現時", "場景效果");

            if (feedback.Length > 34)
            {
                feedback = feedback.Substring(0, 34) + "...";
            }

            return feedback;
        }

        IEnumerator EffectFeedbackRoutine()
        {
            while (_effectFeedbackQueue.Count > 0)
            {
                if (_isTutorialFinishWaitingForClick)
                {
                    _effectFeedbackQueue.Clear();
                    break;
                }

                string message = _effectFeedbackQueue.Dequeue();
                if (effectFeedbackText == null) yield break;

                effectFeedbackText.text = message;
                yield return FadeEffectFeedback(0f, 1f, 0.12f);
                yield return new WaitForSeconds(1.15f);
                yield return FadeEffectFeedback(1f, 0f, 0.35f);
            }

            if (effectFeedbackText != null)
            {
                Color color = effectFeedbackText.color;
                color.a = 0f;
                effectFeedbackText.color = color;
                effectFeedbackText.text = "";
            }
            SetEffectFeedbackToastAlpha(0f);
            _queuedEffectFeedbackMessages.Clear();
            _effectFeedbackRoutine = null;
        }

        IEnumerator FadeEffectFeedback(float fromAlpha, float toAlpha, float duration)
        {
            if (effectFeedbackText == null) yield break;

            float elapsed = 0f;
            Color color = effectFeedbackText.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                effectFeedbackText.color = color;
                SetEffectFeedbackToastAlpha(color.a);
                yield return null;
            }

            color.a = toAlpha;
            effectFeedbackText.color = color;
            SetEffectFeedbackToastAlpha(toAlpha);
        }

        void TryAutoAdvanceAfterTutorialEffectResolved(string message)
        {
            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.BattleEffect) return;
            if (_isSelectingEffectTarget) return;
            if (_isSelectingDeckOperationCard) return;
            if (effectManager != null && effectManager.HasPendingEffects) return;

            string resolvedMessage = string.IsNullOrWhiteSpace(message)
                ? "效果選擇完成，接著進入判定。"
                : $"{message}\n效果選擇完成，接著進入判定。";
            BeginEffectAutoAdvanceToJudgement(resolvedMessage);
        }

        void BeginEffectAutoAdvanceToJudgement(string message)
        {
            if (_isEffectAutoAdvancing) return;
            BeginEffectAutoAdvanceToNextPhase(UcgGamePhase.BattleEffect, message);
        }

        void BeginEffectAutoAdvanceToNextPhase(UcgGamePhase requiredPhase, string message)
        {
            if (_isEffectAutoAdvancing) return;
            _isEffectAutoAdvancing = true;
            _effectAutoAdvanceRoutine = StartCoroutine(EffectAutoAdvanceToNextPhaseRoutine(requiredPhase, message));
        }

        IEnumerator EffectAutoAdvanceToNextPhaseRoutine(UcgGamePhase requiredPhase, string message)
        {
            ClearActivatedEffectSourceHighlights();
            ClearEffectTargetSelection();

            if (playResultText != null)
            {
                playResultText.text = message;
            }
            UpdateMainPrompt();
            RefreshNextPhaseButtonState();

            yield return new WaitForSecondsRealtime(autoPhaseDelaySeconds);

            _effectAutoAdvanceRoutine = null;
            _isEffectAutoAdvancing = false;
            if (IsGameOver || phaseManager == null || phaseManager.CurrentPhase != requiredPhase) yield break;
            if (_isSelectingEffectTarget) yield break;
            if (_isSelectingDeckOperationCard) yield break;
            if (effectManager != null && effectManager.HasPendingEffects) yield break;

            phaseManager.NextPhase();
            EnterCurrentPhase();
        }

        void StopEffectAutoAdvanceRoutine()
        {
            if (_effectAutoAdvanceRoutine != null)
            {
                StopCoroutine(_effectAutoAdvanceRoutine);
                _effectAutoAdvanceRoutine = null;
            }
            _isEffectAutoAdvancing = false;
        }

        bool TryResolveOpponentTargetedEffect(UcgEffectInstance effect, out string message)
        {
            message = "";
            if (effectManager == null || effect == null || effect.ownerSide != UcgPlayerSide.Opponent) return false;
            if (IsSwapOwnCharactersEffect(effect))
            {
                return TryResolveOpponentSwapOwnCharacters(effect, out message);
            }
            if (IsBp05005MultiStepEffect(effect))
            {
                return TryResolveOpponentBp05005Effect(effect, out message);
            }
            if (IsBp05008TopDiscardSwapEffect(effect))
            {
                return TryResolveOpponentBp05008Effect(effect, out message);
            }

            string preTargetMessage = TryResolveBp01043RevealKeepOrderIfNeeded(effect);
            if (!TryFindFirstLegalEffectTarget(effect, out UcgBattleLane targetLane))
            {
                message = CombineEffectMessages(preTargetMessage, GetNoLegalEffectTargetMessage(effect));
                return false;
            }

            bool resolved = effectManager.ResolveTargetedEffect(effect, targetLane, this, out message);
            if (resolved)
            {
                message = CombineEffectMessages(preTargetMessage, message);
            }
            if (resolved)
            {
                ClearEffectTargetSelection();
            }
            return resolved;
        }

        bool TryResolveOpponentSwapOwnCharacters(UcgEffectInstance effect, out string message)
        {
            message = GetNoLegalEffectTargetMessage(effect);
            if (!HasEnoughLegalSwapTargets(effect)) return false;

            UcgBattleLane sourceLane = effect.lane;
            UcgBattleLane targetLane = FindFirstSwapTargetLane(effect, sourceLane);
            if (sourceLane == null || targetLane == null) return false;

            return ResolveSwapOwnCharactersEffect(effect, sourceLane, targetLane, out message);
        }

        bool TryResolveOpponentBp05005Effect(UcgEffectInstance effect, out string message)
        {
            message = GetNoLegalEffectTargetMessage(effect);
            UcgBattleLane stepDownLane = FindFirstBp05005StepDownTarget(effect);
            if (stepDownLane == null) return false;

            if (!ApplyBp05005StepDown(effect, stepDownLane, out string stepDownMessage))
            {
                message = stepDownMessage;
                return false;
            }

            UcgBattleLane stepUpLane = FindFirstBp05005StepUpTarget(effect);
            if (stepUpLane == null)
            {
                effectManager.RemoveEffect(effect);
                message = $"{stepDownMessage}\n目前沒有可獲得 BP 上升的迪卡。";
                return true;
            }

            bool resolved = ApplyBp05005StepUpAndFinish(effect, stepUpLane, stepDownMessage, out message);
            return resolved;
        }

        bool TryResolveOpponentBp05008Effect(UcgEffectInstance effect, out string message)
        {
            message = GetNoLegalEffectTargetMessage(effect);
            UcgBattleLane targetLane = FindFirstBp05008Target(effect);
            if (targetLane == null) return false;

            return ResolveBp05008TopDiscardSwapEffect(effect, targetLane, out message);
        }

        string TryResolveBp01043RevealKeepOrderIfNeeded(UcgEffectInstance effect)
        {
            if (!IsBp01043RevealReorderEffect(effect)) return "";

            string effectKey = GetEffectTrackingKey(effect);
            if (_bp01043RevealReorderHandledEffectKeys.Contains(effectKey)) return "";
            _bp01043RevealReorderHandledEffectKeys.Add(effectKey);

            List<UcgCardData> drawPile = GetDrawPileForOwner(effect.ownerSide);
            if (drawPile == null || drawPile.Count == 0)
            {
                return "布雷薩登場效果：牌庫已空，無法公開。";
            }

            var revealedCards = new List<UcgCardData>();
            int revealCount = Mathf.Min(3, drawPile.Count);
            for (int i = 0; i < revealCount; i++)
            {
                UcgCardData card = drawPile[0];
                drawPile.RemoveAt(0);
                if (card != null)
                {
                    revealedCards.Add(card);
                }
            }

            for (int i = revealedCards.Count - 1; i >= 0; i--)
            {
                drawPile.Insert(0, revealedCards[i]);
            }

            RefreshZoneInfoUI();

            if (debugEffectResolution || debugDeckOperation)
            {
                Debug.Log(
                    "BP01-043 reveal/reorder:\n"
                    + $"owner={effect.ownerSide}\n"
                    + $"effectKey={effectKey}\n"
                    + $"revealed={FormatCardIdList(revealedCards)}\n"
                    + "reorder=keep-original-order");
            }

            return $"布雷薩登場效果：公開 {revealedCards.Count} 張，暫以原順序放回牌庫上方。";
        }

        bool BeginBp01043TopDeckReorderSelection(UcgEffectInstance effect, out string message)
        {
            message = "";
            if (!IsBp01043RevealReorderEffect(effect)) return false;

            string effectKey = GetEffectTrackingKey(effect);
            if (_bp01043RevealReorderHandledEffectKeys.Contains(effectKey)) return false;
            _bp01043RevealReorderHandledEffectKeys.Add(effectKey);

            List<UcgCardData> drawPile = GetDrawPileForOwner(effect.ownerSide);
            if (drawPile == null || drawPile.Count == 0)
            {
                message = "布雷薩登場效果：牌庫已空，無法公開。";
                return false;
            }

            var revealedCards = new List<UcgCardData>();
            int revealCount = Mathf.Min(3, drawPile.Count);
            for (int i = 0; i < revealCount; i++)
            {
                UcgCardData card = drawPile[0];
                drawPile.RemoveAt(0);
                if (card != null)
                {
                    revealedCards.Add(card);
                }
            }

            if (revealedCards.Count == 0)
            {
                message = "布雷薩登場效果：沒有公開到卡牌。";
                RefreshZoneInfoUI();
                return false;
            }

            StopDeckOperationNoValidAutoCloseRoutine();
            EnsureDeckOperationSelectionUI();
            RestoreDeckOperationSelectionUIForRevealCards();
            ClearDeckOperationCards();

            _pendingBp01043ReorderEffect = effect;
            _pendingBp01043ReorderMessage = $"布雷薩登場效果：公開 {revealedCards.Count} 張，請依序選擇放回牌庫上方的順序。";
            _pendingBp01043RevealedCards.Clear();
            _pendingBp01043RevealedCards.AddRange(revealedCards);

            _isSelectingDeckOperationCard = true;
            _pendingDeckSelection = new UcgCardSelectionContext
            {
                sourceEffect = effect,
                rule = new UcgDeckOperationRule
                {
                    selectionFilter = UcgDeckSelectionFilter.Any,
                    selectCount = revealedCards.Count
                },
                owner = effect.ownerSide,
                sourceZone = UcgDeckOperationSourceZone.TopDeckReorder
            };
            _pendingDeckSelection.revealedCards.AddRange(revealedCards);

            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();
            RenderBp01043ReorderCards();

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(true);
                _deckOperationSelectionRoot.SetAsLastSibling();
            }

            RefreshZoneInfoUI();
            RefreshNextPhaseButtonState();
            message = _pendingBp01043ReorderMessage;

            if (debugEffectResolution || debugDeckOperation)
            {
                Debug.Log(
                    "BP01-043 reveal/reorder:\n"
                    + $"owner={effect.ownerSide}\n"
                    + $"effectKey={effectKey}\n"
                    + $"revealed={FormatCardIdList(revealedCards)}\n"
                    + "reorder=manual-pending");
            }

            return true;
        }

        void RenderBp01043ReorderCards()
        {
            if (_pendingDeckSelection == null) return;
            ClearDeckOperationCards();

            if (_deckOperationSelectionTitle != null)
            {
                int selectedCount = _pendingDeckSelection.selectedCards.Count;
                int totalCount = _pendingBp01043RevealedCards.Count;
                _deckOperationSelectionTitle.text = $"布雷薩登場效果\n依序點選放回順序：第 {selectedCount + 1}/{totalCount} 張會放回牌庫上方";
            }

            for (int i = 0; i < _pendingBp01043RevealedCards.Count; i++)
            {
                UcgCardData card = _pendingBp01043RevealedCards[i];
                bool canSelect = GetReferenceIndex(_pendingDeckSelection.selectedCards, card) < 0;
                CreateDeckOperationCardButton(card, i, _pendingBp01043RevealedCards.Count, canSelect, false);
            }

            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
                _deckOperationNoSelectionButton.onClick.RemoveAllListeners();
            }
        }

        void CompleteBp01043ReorderCardSelection(UcgCardData selectedCard)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) return;
            if (_pendingDeckSelection.sourceZone != UcgDeckOperationSourceZone.TopDeckReorder) return;
            if (_pendingBp01043ReorderEffect == null || selectedCard == null)
            {
                FinishBp01043ReorderSelectionWithOriginalOrder("重排狀態已失效，公開卡以原順序放回。");
                return;
            }

            if (GetReferenceIndex(_pendingBp01043RevealedCards, selectedCard) < 0)
            {
                ShowPlayStatus("請選擇這次公開的卡牌。", 1.1f);
                return;
            }
            if (GetReferenceIndex(_pendingDeckSelection.selectedCards, selectedCard) >= 0)
            {
                ShowPlayStatus("這張卡已經排入順序，請選擇下一張。", 1.1f);
                return;
            }

            _pendingDeckSelection.selectedCards.Add(selectedCard);
            int selectedCount = _pendingDeckSelection.selectedCards.Count;
            int totalCount = _pendingBp01043RevealedCards.Count;
            if (selectedCount < totalCount)
            {
                ShowPlayStatus($"已選 {selectedCount}/{totalCount} 張，請選下一張。", 1.1f);
                RenderBp01043ReorderCards();
                return;
            }

            var orderedCards = new List<UcgCardData>(_pendingDeckSelection.selectedCards);
            FinishBp01043ReorderSelection(orderedCards);
        }

        void FinishBp01043ReorderSelectionWithOriginalOrder(string fallbackMessage)
        {
            var orderedCards = new List<UcgCardData>(_pendingBp01043RevealedCards);
            FinishBp01043ReorderSelection(orderedCards, fallbackMessage);
        }

        void FinishBp01043ReorderSelection(List<UcgCardData> orderedCards, string overrideMessage = "")
        {
            UcgEffectInstance effect = _pendingBp01043ReorderEffect;
            UcgPlayerSide owner = effect != null ? effect.ownerSide : UcgPlayerSide.Player;
            if (orderedCards == null || orderedCards.Count == 0)
            {
                orderedCards = new List<UcgCardData>(_pendingBp01043RevealedCards);
            }

            List<UcgCardData> drawPile = GetDrawPileForOwner(owner);
            if (drawPile != null)
            {
                for (int i = orderedCards.Count - 1; i >= 0; i--)
                {
                    UcgCardData card = orderedCards[i];
                    if (card != null)
                    {
                        drawPile.Insert(0, card);
                    }
                }
            }

            string reorderMessage = string.IsNullOrWhiteSpace(overrideMessage)
                ? $"布雷薩登場效果：已依指定順序放回 {orderedCards.Count} 張牌。"
                : overrideMessage;

            if (debugEffectResolution || debugDeckOperation)
            {
                Debug.Log(
                    "BP01-043 reveal/reorder:\n"
                    + $"owner={owner}\n"
                    + $"effectKey={GetEffectTrackingKey(effect)}\n"
                    + $"ordered={FormatCardIdList(orderedCards)}\n"
                    + "reorder=manual-resolved");
            }

            CleanupBp01043ReorderSelectionState();
            RefreshZoneInfoUI();

            if (effect == null)
            {
                ShowPlayStatus(reorderMessage, 1.1f);
                return;
            }

            _isSelectingEffectTarget = true;
            _pendingTargetEffect = effect;
            _pendingTargetType = effectManager != null ? effectManager.GetTargetType(effect) : UcgEffectTargetType.OpponentCharacter;
            _pendingSwapSourceLane = null;
            _pendingBp05005StepDownLane = null;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            if (!TryFindFirstLegalEffectTarget(effect, out _))
            {
                SkipTargetEffectWithNoLegalTarget(effect, reorderMessage);
                return;
            }

            HighlightEffectTargets();
            if (playResultText != null)
            {
                string targetPrompt = effectManager != null
                    ? effectManager.GetTargetPrompt(effect)
                    : "登場效果階段｜請選擇對手一條 Lane";
                playResultText.text = CombineEffectMessages(reorderMessage, targetPrompt);
            }
            ShowPlayStatus(reorderMessage, 1.15f);
            UpdateMainPrompt();
        }

        void CleanupBp01043ReorderSelectionState()
        {
            ClearBp01043PendingReorderState();
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();
            SetNextPhaseButtonInteractable(true);
            RefreshNextPhaseButtonState();
        }

        void ClearBp01043PendingReorderState()
        {
            _pendingBp01043ReorderEffect = null;
            _pendingBp01043ReorderMessage = "";
            _pendingBp01043RevealedCards.Clear();
        }

        bool IsBp01043RevealReorderHandled(UcgEffectInstance effect)
        {
            return effect != null && _bp01043RevealReorderHandledEffectKeys.Contains(GetEffectTrackingKey(effect));
        }

        bool IsBp01043RevealReorderEffect(UcgEffectInstance effect)
        {
            return effect != null
                && effect.cardData != null
                && effect.cardData.id == "BP01-043";
        }

        string GetEffectTrackingKey(UcgEffectInstance effect)
        {
            if (effect == null) return "null";
            if (!string.IsNullOrWhiteSpace(effect.effectKey)) return effect.effectKey;

            string cardId = effect.cardData != null ? effect.cardData.id : "unknown";
            return $"{effect.ownerSide}:{effect.LaneIndex}:{cardId}:{effect.effectId}";
        }

        string CombineEffectMessages(string firstMessage, string secondMessage)
        {
            if (string.IsNullOrWhiteSpace(firstMessage)) return secondMessage ?? "";
            if (string.IsNullOrWhiteSpace(secondMessage)) return firstMessage;
            return $"{firstMessage}\n{secondMessage}";
        }

        bool TryFindFirstLegalEffectTarget(UcgEffectInstance effect, out UcgBattleLane targetLane)
        {
            targetLane = null;
            if (battlefieldManager == null || turnManager == null || effectManager == null || effect == null) return false;

            UcgEffectTargetType targetType = effectManager.GetTargetType(effect);
            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                if (IsEffectTargetSide(targetType, UcgPlayerSide.Player) && IsLegalEffectTarget(lane, UcgPlayerSide.Player, effect))
                {
                    targetLane = lane;
                    return true;
                }

                if (IsEffectTargetSide(targetType, UcgPlayerSide.Opponent) && IsLegalEffectTarget(lane, UcgPlayerSide.Opponent, effect))
                {
                    targetLane = lane;
                    return true;
                }
            }

            return false;
        }

        void BeginEffectTargetSelection(UcgEffectInstance effect)
        {
            if (effectManager == null || effect == null || IsGameOver)
            {
                ClearEffectTargetSelection();
                return;
            }

            StartEffectSourceHighlight(effect);
            _isSelectingEffectTarget = true;
            _pendingTargetEffect = effect;
            _pendingTargetType = effectManager.GetTargetType(effect);
            _pendingSwapSourceLane = null;
            _pendingBp05005StepDownLane = null;
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();
            string preTargetMessage = "";
            if (IsBp01043RevealReorderEffect(effect)
                && effect.ownerSide == UcgPlayerSide.Player
                && !IsBp01043RevealReorderHandled(effect))
            {
                if (BeginBp01043TopDeckReorderSelection(effect, out preTargetMessage))
                {
                    if (playResultText != null) playResultText.text = preTargetMessage;
                    UpdateMainPrompt();
                    return;
                }
            }
            else
            {
                preTargetMessage = TryResolveBp01043RevealKeepOrderIfNeeded(effect);
            }

            if (IsSwapOwnCharactersEffect(effect) && !HasEnoughLegalSwapTargets(effect))
            {
                SkipTargetEffectWithNoLegalTarget(effect, preTargetMessage);
                return;
            }

            if (!TryFindFirstLegalEffectTarget(effect, out _))
            {
                SkipTargetEffectWithNoLegalTarget(effect, preTargetMessage);
                return;
            }

            HighlightEffectTargets();

            if (playResultText != null)
            {
                playResultText.text = CombineEffectMessages(preTargetMessage, effectManager.GetTargetPrompt(effect));
            }

            UpdateMainPrompt();
        }

        void HighlightEffectTargets()
        {
            if (battlefieldManager == null || turnManager == null) return;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                if (IsEffectTargetSide(_pendingTargetType, UcgPlayerSide.Player))
                {
                    lane.SetEffectTargetHighlight(UcgPlayerSide.Player, IsLegalEffectTarget(lane, UcgPlayerSide.Player, _pendingTargetEffect));
                }

                if (IsEffectTargetSide(_pendingTargetType, UcgPlayerSide.Opponent))
                {
                    lane.SetEffectTargetHighlight(UcgPlayerSide.Opponent, IsLegalEffectTarget(lane, UcgPlayerSide.Opponent, _pendingTargetEffect));
                }
            }
        }

        public void HandleLaneClickedForEffect(UcgBattleLane lane, UcgPlayerSide clickedSide)
        {
            if (!_isSelectingEffectTarget || _pendingTargetEffect == null || effectManager == null) return;
            if (IsGameOver)
            {
                ClearEffectTargetSelection();
                return;
            }

            if (IsPendingBp01105LaneSelection())
            {
                HandleBp01105LaneTargetClick(lane, clickedSide);
                return;
            }

            if (!IsEffectTargetSide(_pendingTargetType, clickedSide))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidTargetSide", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = IsEffectTargetSide(_pendingTargetType, UcgPlayerSide.Player)
                        ? "此效果需要選擇我方角色"
                        : "此效果需要選擇對手角色";
                }
                return;
            }

            if (IsSwapOwnCharactersEffect(_pendingTargetEffect))
            {
                HandleSwapOwnCharactersTargetClick(lane, clickedSide);
                return;
            }
            if (IsBp05005MultiStepEffect(_pendingTargetEffect))
            {
                HandleBp05005TargetClick(lane, clickedSide);
                return;
            }
            if (IsBp05008TopDiscardSwapEffect(_pendingTargetEffect))
            {
                HandleBp05008TargetClick(lane, clickedSide);
                return;
            }

            if (!IsLegalEffectTarget(lane, clickedSide, _pendingTargetEffect))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidTarget", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = GetInvalidEffectTargetMessage(lane, clickedSide, _pendingTargetEffect);
                }
                return;
            }

            bool resolved = effectManager.ResolveTargetedEffect(_pendingTargetEffect, lane, this, out string message);
            ClearEffectTargetSelection();
            if (resolved)
            {
                QueueEffectFeedback(message);
            }

            ContinueAfterTargetEffectResolved(resolved, message);
        }

        void ContinueAfterTargetEffectResolved(bool resolved, string message)
        {
            StopCurrentEffectSourceHighlight();
            ShowPlayStatus(resolved
                ? effectManager.HasPendingEffects
                    ? $"剩餘效果 {effectManager.PendingCount} 個"
                    : "效果處理完畢"
                : message, 1.2f);

            HighlightActivatedEffectSources();
            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
                UpdateMainPrompt();
                return;
            }

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
                UpdateMainPrompt();
                return;
            }

            TryAutoAdvanceAfterTutorialEffectResolved(message);
            UpdateMainPrompt();
        }

        void HandleSwapOwnCharactersTargetClick(UcgBattleLane lane, UcgPlayerSide clickedSide)
        {
            if (!IsLegalEffectTarget(lane, clickedSide, _pendingTargetEffect))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidSwapTarget", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = GetInvalidEffectTargetMessage(lane, clickedSide, _pendingTargetEffect);
                }
                return;
            }

            if (_pendingSwapSourceLane == null)
            {
                _pendingSwapSourceLane = lane;
                HighlightEffectTargets();
                if (playResultText != null)
                {
                    playResultText.text = "請選擇要交換位置的另一名我方角色。";
                }
                UpdateMainPrompt();
                return;
            }

            bool resolved = ResolveSwapOwnCharactersEffect(
                _pendingTargetEffect,
                _pendingSwapSourceLane,
                lane,
                out string message);
            ClearEffectTargetSelection();
            if (resolved)
            {
                QueueEffectFeedback(message);
            }

            ContinueAfterTargetEffectResolved(resolved, message);
        }

        void HandleBp05005TargetClick(UcgBattleLane lane, UcgPlayerSide clickedSide)
        {
            if (!IsLegalEffectTarget(lane, clickedSide, _pendingTargetEffect))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidBp05005Target", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = GetInvalidEffectTargetMessage(lane, clickedSide, _pendingTargetEffect);
                }
                return;
            }

            if (_pendingBp05005StepDownLane == null)
            {
                if (!ApplyBp05005StepDown(_pendingTargetEffect, lane, out string stepDownMessage))
                {
                    if (playResultText != null) playResultText.text = stepDownMessage;
                    return;
                }

                _pendingBp05005StepDownLane = lane;
                QueueEffectFeedback(stepDownMessage);

                if (FindFirstBp05005StepUpTarget(_pendingTargetEffect) == null)
                {
                    if (effectManager != null) effectManager.RemoveEffect(_pendingTargetEffect);
                    ClearEffectTargetSelection();
                    string noDigaMessage = $"{stepDownMessage}\n目前沒有可獲得 BP 上升的迪卡。";
                    QueueEffectFeedback(noDigaMessage);
                    ContinueAfterTargetEffectResolved(true, noDigaMessage);
                    return;
                }

                HighlightEffectTargets();
                if (playResultText != null)
                {
                    playResultText.text = "已完成 BP 降低一階。請選擇我方一名迪卡，讓 BP 上升一階。";
                }
                UpdateMainPrompt();
                return;
            }

            bool resolved = ApplyBp05005StepUpAndFinish(
                _pendingTargetEffect,
                lane,
                "",
                out string message);
            ClearEffectTargetSelection();
            if (resolved)
            {
                QueueEffectFeedback(message);
            }

            ContinueAfterTargetEffectResolved(resolved, message);
        }

        void HandleBp05008TargetClick(UcgBattleLane lane, UcgPlayerSide clickedSide)
        {
            if (!IsLegalEffectTarget(lane, clickedSide, _pendingTargetEffect))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidBp05008Target", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = GetInvalidEffectTargetMessage(lane, clickedSide, _pendingTargetEffect);
                }
                return;
            }

            if (_pendingTargetEffect != null && _pendingTargetEffect.ownerSide == UcgPlayerSide.Player)
            {
                bool selectionStarted = BeginBp05008DiscardSelection(_pendingTargetEffect, lane, out string selectionMessage);
                ClearEffectTargetSelection();
                if (selectionStarted)
                {
                    QueueEffectFeedback(selectionMessage);
                    ShowPlayStatus(selectionMessage, 1.2f);
                    UpdateMainPrompt();
                    return;
                }

                ContinueAfterTargetEffectResolved(false, selectionMessage);
                return;
            }

            bool resolved = ResolveBp05008TopDiscardSwapEffect(_pendingTargetEffect, lane, out string message);
            ClearEffectTargetSelection();
            if (resolved)
            {
                QueueEffectFeedback(message);
            }

            ContinueAfterTargetEffectResolved(resolved, message);
        }

        bool IsPendingBp01105LaneSelection()
        {
            return _pendingBp01105Effect != null
                && _pendingBp01105SelectedCard != null
                && _pendingBp01105RevealedCards.Count > 0;
        }

        void HandleBp01105LaneTargetClick(UcgBattleLane lane, UcgPlayerSide clickedSide)
        {
            if (!IsPendingBp01105LaneSelection())
            {
                ClearEffectTargetSelection();
                return;
            }

            UcgPlayerSide owner = _pendingBp01105Effect.ownerSide;
            if (clickedSide != owner || !IsLegalBp01105UpgradeTarget(owner, lane, _pendingBp01105SelectedCard))
            {
                LogInteractionRejected("ClickEffectTarget", "InvalidBp01105Target", null, lane);
                if (playResultText != null)
                {
                    playResultText.text = "請選擇可以被這張公開卡合法升級的我方 Lane。";
                }
                return;
            }

            FinishBp01105SceneUpgradeSelection(lane);
        }

        bool IsEffectTargetSide(UcgEffectTargetType targetType, UcgPlayerSide side)
        {
            if (targetType == UcgEffectTargetType.AnyLane) return true;
            if (side == UcgPlayerSide.Player)
            {
                return targetType == UcgEffectTargetType.OwnLane
                    || targetType == UcgEffectTargetType.OwnCharacter;
            }

            return targetType == UcgEffectTargetType.OpponentLane
                || targetType == UcgEffectTargetType.OpponentCharacter;
        }

        bool IsLegalEffectTarget(UcgBattleLane lane, UcgPlayerSide side)
        {
            if (lane == null || IsGameOver) return false;
            if (battlefieldManager == null || turnManager == null) return false;

            int openedLaneCount = battlefieldManager.GetOpenedLaneCount(turnManager.currentTurn);
            if (lane.laneIndex < 0 || lane.laneIndex >= openedLaneCount) return false;

            if (side == UcgPlayerSide.Player)
            {
                UcgPlayArea playArea = lane.GetPlayerPlayArea();
                return playArea != null && playArea.GetTopCard() != null;
            }

            return lane.GetOpponentTopCard() != null;
        }

        bool IsLegalEffectTarget(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (!IsLegalEffectTarget(lane, side)) return false;
            return EffectTargetMatchesFilter(lane, side, effect);
        }

        bool EffectTargetMatchesFilter(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (effect == null || effect.cardData == null) return true;
            if (IsSwapOwnCharactersEffect(effect))
            {
                return IsLegalSwapOwnCharactersTarget(lane, side, effect);
            }
            if (IsBp05005MultiStepEffect(effect))
            {
                return _pendingBp05005StepDownLane == null
                    ? IsLegalBp05005StepDownTarget(lane, side, effect)
                    : IsLegalBp05005StepUpTarget(lane, side, effect);
            }
            if (IsBp05008TopDiscardSwapEffect(effect))
            {
                return IsLegalBp05008Target(lane, side, effect);
            }
            if (IsPendingBp01105LaneSelection() && effect == _pendingBp01105Effect)
            {
                return side == effect.ownerSide
                    && IsLegalBp01105UpgradeTarget(effect.ownerSide, lane, _pendingBp01105SelectedCard);
            }

            if (effect.effectId == UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp)
            {
                return IsAdjacentOwnZeroTarget(lane, side, effect);
            }

            if (!UcgTutorialCardEffectMap.TryGetTargetFilter(effect.cardData, out UcgEffectTargetFilter filter)) return true;
            if (filter == null) return true;
            if (!EffectTargetSideMatchesFilter(effect, side, filter)) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, side);
            if (targetCard == null) return false;

            if (filter.targetAllowedTypes != null && filter.targetAllowedTypes.Count > 0
                && !CardTypeMatchesAny(targetCard, filter.targetAllowedTypes, ""))
            {
                return false;
            }

            if (filter.targetCharacterNames != null && filter.targetCharacterNames.Count > 0
                && !CardCharacterMatchesAny(targetCard, filter.targetCharacterNames))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filter.targetCharacterNameContains)
                && !CardCharacterContains(targetCard, filter.targetCharacterNameContains))
            {
                return false;
            }

            if (filter.opposingLaneOwnerCharacterNames != null
                && filter.opposingLaneOwnerCharacterNames.Count > 0)
            {
                UcgCardData ownerLaneCard = GetLaneTopCard(lane, effect.ownerSide);
                if (!CardCharacterMatchesAny(ownerLaneCard, filter.opposingLaneOwnerCharacterNames))
                {
                    return false;
                }
            }

            return true;
        }

        bool IsAdjacentOwnZeroTarget(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (lane == null || effect == null || effect.lane == null) return false;
            if (side != effect.ownerSide) return false;
            if (Mathf.Abs(lane.laneIndex - effect.lane.laneIndex) != 1) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, side);
            return CardCharacterContains(targetCard, "傑洛");
        }

        bool IsSwapOwnCharactersEffect(UcgEffectInstance effect)
        {
            return effect != null && effect.effectId == UcgDemoEffectId.OnRevealSwapOwnCharacters;
        }

        bool IsBp05005MultiStepEffect(UcgEffectInstance effect)
        {
            return effect != null && effect.effectId == UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp;
        }

        bool IsBp05008TopDiscardSwapEffect(UcgEffectInstance effect)
        {
            return effect != null && effect.effectId == UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga;
        }

        bool IsLegalSwapOwnCharactersTarget(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (lane == null || effect == null || effect.lane == null) return false;
            if (side != effect.ownerSide) return false;
            if (GetLaneTopCard(lane, side) == null) return false;

            if (_pendingSwapSourceLane == null)
            {
                return lane == effect.lane;
            }

            return lane != _pendingSwapSourceLane;
        }

        bool HasEnoughLegalSwapTargets(UcgEffectInstance effect)
        {
            if (battlefieldManager == null || turnManager == null || effect == null || effect.lane == null) return false;
            if (GetLaneTopCard(effect.lane, effect.ownerSide) == null) return false;

            UcgBattleLane targetLane = FindFirstSwapTargetLane(effect, effect.lane);
            return targetLane != null;
        }

        UcgBattleLane FindFirstSwapTargetLane(UcgEffectInstance effect, UcgBattleLane sourceLane)
        {
            if (battlefieldManager == null || turnManager == null || effect == null || sourceLane == null) return null;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null || lane == sourceLane) continue;
                if (GetLaneTopCard(lane, effect.ownerSide) != null) return lane;
            }

            return null;
        }

        bool IsLegalBp05005StepDownTarget(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (lane == null || effect == null) return false;
            if (side != effect.ownerSide) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, side);
            if (targetCard == null) return false;
            if (targetCard.cardCategory != "超人力霸王") return false;
            if (targetCard.level != 2 && targetCard.level != 3) return false;

            int stackCount = GetLaneStackCount(lane, side);
            if (stackCount != 2 && stackCount != 3) return false;

            int currentBp = targetCard.GetBpByStackCount(stackCount);
            int previousBp = UcgBattleJudge.GetPreviousBpStep(targetCard, currentBp);
            return previousBp < currentBp;
        }

        bool IsLegalBp05005StepUpTarget(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (lane == null || effect == null) return false;
            if (side != effect.ownerSide) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, side);
            if (!CardCharacterContains(targetCard, "迪卡")) return false;

            int stackCount = GetLaneStackCount(lane, side);
            int currentBp = targetCard.GetBpByStackCount(stackCount);
            int nextBp = UcgBattleJudge.GetNextBpStep(targetCard, currentBp);
            return nextBp > currentBp;
        }

        UcgBattleLane FindFirstBp05005StepDownTarget(UcgEffectInstance effect)
        {
            if (battlefieldManager == null || turnManager == null || effect == null) return null;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (IsLegalBp05005StepDownTarget(lane, effect.ownerSide, effect)) return lane;
            }

            return null;
        }

        UcgBattleLane FindFirstBp05005StepUpTarget(UcgEffectInstance effect)
        {
            if (battlefieldManager == null || turnManager == null || effect == null) return null;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (IsLegalBp05005StepUpTarget(lane, effect.ownerSide, effect)) return lane;
            }

            return null;
        }

        bool IsLegalBp05008Target(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (lane == null || effect == null) return false;
            if (side != effect.ownerSide) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, side);
            if (!CardCharacterContains(targetCard, "迪卡")) return false;
            if (targetCard.level != 2 && targetCard.level != 3) return false;

            int stackCount = GetLaneStackCount(lane, side);
            if (stackCount != 2 && stackCount != 3) return false;

            return TryFindMatchingBp05008DiscardCard(effect.ownerSide, targetCard.level, out _, out _);
        }

        UcgBattleLane FindFirstBp05008Target(UcgEffectInstance effect)
        {
            if (battlefieldManager == null || turnManager == null || effect == null) return null;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (IsLegalBp05008Target(lane, effect.ownerSide, effect)) return lane;
            }

            return null;
        }

        bool ApplyBp05005StepDown(UcgEffectInstance effect, UcgBattleLane targetLane, out string message)
        {
            message = "無法降低 BP。";
            if (effect == null || targetLane == null) return false;

            UcgPlayerSide targetSide = effect.ownerSide;
            UcgCardData targetCard = GetLaneTopCard(targetLane, targetSide);
            if (targetCard == null) return false;

            int stackCount = GetLaneStackCount(targetLane, targetSide);
            int currentBp = targetCard.GetBpByStackCount(stackCount);
            int previousBp = UcgBattleJudge.GetPreviousBpStep(targetCard, currentBp);
            int amount = previousBp - currentBp;
            if (amount >= 0)
            {
                message = "此角色 BP 已無法再降低。";
                return false;
            }

            targetLane.AddTemporaryBpModifier(
                targetSide,
                amount,
                effect.cardData,
                "登場時效果（BP降低一階）",
                0,
                stackCount,
                false,
                false,
                currentBp,
                previousBp);

            string sideText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
            string targetName = string.IsNullOrWhiteSpace(targetCard.cardName) ? "角色" : targetCard.cardName;
            message = $"登場效果階段｜{sideText}第 {targetLane.laneIndex + 1} 路 {targetName} BP 降低一階";
            return true;
        }

        bool ApplyBp05005StepUpAndFinish(
            UcgEffectInstance effect,
            UcgBattleLane targetLane,
            string prefixMessage,
            out string message)
        {
            message = "無法讓迪卡 BP 上升。";
            if (effect == null || targetLane == null || effectManager == null) return false;

            UcgPlayerSide targetSide = effect.ownerSide;
            UcgCardData targetCard = GetLaneTopCard(targetLane, targetSide);
            if (targetCard == null)
            {
                return false;
            }

            int stackCount = GetLaneStackCount(targetLane, targetSide);
            int currentBp = targetCard.GetBpByStackCount(stackCount);
            int nextBp = UcgBattleJudge.GetNextBpStep(targetCard, currentBp);
            int amount = nextBp - currentBp;

            effectManager.RemoveEffect(effect);
            if (amount <= 0)
            {
                message = "迪卡 BP 已無法再上升。";
                return true;
            }

            targetLane.AddTemporaryBpModifier(
                targetSide,
                amount,
                effect.cardData,
                "登場時效果（BP上升一階）",
                0,
                stackCount,
                false,
                true,
                currentBp,
                nextBp);

            string sideText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
            string targetName = string.IsNullOrWhiteSpace(targetCard.cardName) ? "迪卡" : targetCard.cardName;
            string stepUpMessage = $"登場效果階段｜{sideText}第 {targetLane.laneIndex + 1} 路 {targetName} BP 上升一階";
            message = string.IsNullOrWhiteSpace(prefixMessage)
                ? stepUpMessage
                : $"{prefixMessage}\n{stepUpMessage}";
            return true;
        }

        bool ResolveSwapOwnCharactersEffect(
            UcgEffectInstance effect,
            UcgBattleLane sourceLane,
            UcgBattleLane targetLane,
            out string message)
        {
            message = "交換位置失敗";
            if (effectManager == null || effect == null || sourceLane == null || targetLane == null)
            {
                return false;
            }

            if (sourceLane == targetLane)
            {
                message = "請選擇另一條 Lane 交換位置。";
                return false;
            }

            UcgCardData sourceCard = GetLaneTopCard(sourceLane, effect.ownerSide);
            UcgCardData targetCard = GetLaneTopCard(targetLane, effect.ownerSide);
            if (sourceCard == null || targetCard == null)
            {
                message = "目前沒有足夠的我方角色可以交換位置。";
                return false;
            }

            bool swapped = sourceLane.SwapCharacterStackWith(targetLane, effect.ownerSide);
            if (!swapped)
            {
                message = "交換位置失敗，效果略過。";
                return false;
            }

            effectManager.RemoveEffect(effect);
            string sideText = effect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手";
            string sourceName = string.IsNullOrWhiteSpace(sourceCard.cardName) ? "角色" : sourceCard.cardName;
            string targetName = string.IsNullOrWhiteSpace(targetCard.cardName) ? "角色" : targetCard.cardName;
            message = $"登場效果階段｜{sideText}{sourceName} 與第 {targetLane.laneIndex + 1} 路 {targetName} 交換位置。";
            return true;
        }

        bool BeginBp05008DiscardSelection(
            UcgEffectInstance effect,
            UcgBattleLane targetLane,
            out string message)
        {
            message = GetNoLegalEffectTargetMessage(effect);
            if (effectManager == null || effect == null || targetLane == null) return false;
            if (!IsLegalBp05008Target(targetLane, effect.ownerSide, effect)) return false;
            if (!CanReturnTopCardToHand(effect.ownerSide))
            {
                message = "手牌區尚未準備好，效果略過。";
                return false;
            }

            UcgCardData targetTopCard = GetLaneTopCard(targetLane, effect.ownerSide);
            if (targetTopCard == null) return false;

            List<UcgCardData> legalDiscardCards = GetMatchingBp05008DiscardCards(effect.ownerSide, targetTopCard.level);
            if (legalDiscardCards.Count == 0)
            {
                message = "棄牌區沒有同等級的迪卡可以登場。";
                return false;
            }

            if (!targetLane.RemoveTopCardFromEffect(effect.ownerSide, targetTopCard, out UcgCardData returnedTopCard))
            {
                message = "最上層卡回手牌失敗，效果略過。";
                return false;
            }

            if (!AddReturnedTopCardToHand(effect.ownerSide, returnedTopCard))
            {
                targetLane.UpgradeCardFromEffect(
                    effect.ownerSide,
                    returnedTopCard,
                    cardInfoPanel,
                    GetTestCardSprite(effect.ownerSide == UcgPlayerSide.Player ? 0 : 1),
                    GetPlacedBattleCardSize(),
                    LoadPlaceholderFont());
                message = "最上層卡回手牌失敗，效果略過。";
                return false;
            }

            if (returnedTopCard != null)
            {
                _temporaryTypeGrants.Remove(returnedTopCard);
            }

            StopDeckOperationNoValidAutoCloseRoutine();
            EnsureDeckOperationSelectionUI();
            RestoreDeckOperationSelectionUIForRevealCards();
            ClearDeckOperationCards();

            _pendingBp05008DiscardLane = targetLane;
            _pendingBp05008ReturnedTopCard = returnedTopCard;
            _pendingBp05008ReturnedLevel = targetTopCard.level;
            _isSelectingDeckOperationCard = true;
            _pendingDeckSelection = new UcgCardSelectionContext
            {
                sourceEffect = effect,
                owner = effect.ownerSide,
                sourceZone = UcgDeckOperationSourceZone.DiscardPile
            };
            _pendingDeckSelection.revealedCards.AddRange(legalDiscardCards);

            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            if (_deckOperationSelectionTitle != null)
            {
                string cardName = effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                    ? effect.cardData.cardName
                    : "登場效果";
                _deckOperationSelectionTitle.text = $"{cardName}\n從棄牌區選擇一張同等級迪卡登場";
            }

            for (int i = 0; i < legalDiscardCards.Count; i++)
            {
                CreateDeckOperationCardButton(legalDiscardCards[i], i, legalDiscardCards.Count, true, false);
            }

            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
                _deckOperationNoSelectionButton.onClick.RemoveAllListeners();
            }

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(true);
                _deckOperationSelectionRoot.SetAsLastSibling();
            }

            RefreshZoneInfoUI();
            RefreshHandLayout();
            RefreshNextPhaseButtonState();

            string returnedName = string.IsNullOrWhiteSpace(returnedTopCard != null ? returnedTopCard.cardName : "")
                ? "最上層卡"
                : returnedTopCard.cardName;
            message = $"登場效果階段｜第 {targetLane.laneIndex + 1} 路 {returnedName} 回手牌，請從棄牌區選擇同等級迪卡。";
            return true;
        }

        void CompleteBp05008DiscardSelection(UcgCardData selectedCard)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) return;
            if (_pendingDeckSelection.sourceZone != UcgDeckOperationSourceZone.DiscardPile) return;

            UcgEffectInstance sourceEffect = _pendingDeckSelection.sourceEffect;
            UcgPlayerSide owner = _pendingDeckSelection.owner;
            if (sourceEffect == null || _pendingBp05008DiscardLane == null || selectedCard == null)
            {
                ShowPlayStatus("棄牌區選擇狀態已失效，效果略過。", 1.1f);
                CleanupBp05008DiscardSelectionUi();
                ContinueAfterTargetEffectResolved(false, "棄牌區選擇狀態已失效，效果略過。");
                return;
            }

            if (selectedCard.level != _pendingBp05008ReturnedLevel || !CardCharacterContains(selectedCard, "迪卡"))
            {
                ShowPlayStatus("只能選擇同等級的迪卡。", 1.1f);
                return;
            }

            int discardIndex = FindCardIndexInDiscardPile(owner, selectedCard);
            if (discardIndex < 0)
            {
                ShowPlayStatus("這張卡已不在棄牌區，請重新選擇。", 1.1f);
                return;
            }

            UcgCardView newTopCard = _pendingBp05008DiscardLane.UpgradeCardFromEffect(
                owner,
                selectedCard,
                cardInfoPanel,
                GetTestCardSprite(owner == UcgPlayerSide.Player ? 0 : 1),
                GetPlacedBattleCardSize(),
                LoadPlaceholderFont());
            if (newTopCard == null)
            {
                ShowPlayStatus("棄牌區卡牌登場失敗，效果略過。", 1.1f);
                return;
            }

            List<UcgCardData> discardPile = GetDiscardPileForOwner(owner);
            if (discardPile != null && discardIndex >= 0 && discardIndex < discardPile.Count)
            {
                discardPile.RemoveAt(discardIndex);
            }
            else if (discardPile != null)
            {
                discardPile.Remove(selectedCard);
            }

            if (effectManager != null)
            {
                effectManager.RemoveEffect(sourceEffect);
            }

            string returnedName = string.IsNullOrWhiteSpace(_pendingBp05008ReturnedTopCard != null ? _pendingBp05008ReturnedTopCard.cardName : "")
                ? "最上層卡"
                : _pendingBp05008ReturnedTopCard.cardName;
            string replacementName = string.IsNullOrWhiteSpace(selectedCard.cardName) ? "迪卡" : selectedCard.cardName;
            string sideText = owner == UcgPlayerSide.Player ? "我方" : "對手";
            string message = $"登場效果階段｜{sideText}第 {_pendingBp05008DiscardLane.laneIndex + 1} 路 {returnedName} 回手牌，棄牌區 {replacementName} 登場到最上層。";

            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection.selectedCard = selectedCard;
            CleanupBp05008DiscardSelectionUi();
            RefreshZoneInfoUI();
            RefreshHandLayout();
            QueueEffectFeedback(message);
            StopEffectSourceHighlight(sourceEffect);
            ContinueAfterTargetEffectResolved(true, message);
        }

        void CleanupBp05008DiscardSelectionUi()
        {
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;
            ClearBp05008DiscardSelectionState();

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshNextPhaseButtonState();
            RefreshInteractionHints();
        }

        void ClearBp05008DiscardSelectionState()
        {
            _pendingBp05008DiscardLane = null;
            _pendingBp05008ReturnedTopCard = null;
            _pendingBp05008ReturnedLevel = 0;
        }

        bool ResolveBp05008TopDiscardSwapEffect(
            UcgEffectInstance effect,
            UcgBattleLane targetLane,
            out string message)
        {
            message = GetNoLegalEffectTargetMessage(effect);
            if (effectManager == null || effect == null || targetLane == null) return false;
            if (!IsLegalBp05008Target(targetLane, effect.ownerSide, effect)) return false;
            if (!CanReturnTopCardToHand(effect.ownerSide))
            {
                message = "手牌區尚未準備好，效果略過。";
                return false;
            }

            UcgCardData targetTopCard = GetLaneTopCard(targetLane, effect.ownerSide);
            if (targetTopCard == null) return false;

            if (!TryFindMatchingBp05008DiscardCard(effect.ownerSide, targetTopCard.level, out UcgCardData replacementCard, out int discardIndex))
            {
                message = "棄牌區沒有同等級的迪卡可以登場。";
                return false;
            }

            if (!targetLane.ReplaceTopCardData(effect.ownerSide, replacementCard, out UcgCardData returnedTopCard))
            {
                message = "替換最上層卡失敗，效果略過。";
                return false;
            }

            List<UcgCardData> discardPile = GetDiscardPileForOwner(effect.ownerSide);
            if (discardPile != null && discardIndex >= 0 && discardIndex < discardPile.Count)
            {
                discardPile.RemoveAt(discardIndex);
            }
            else if (discardPile != null)
            {
                discardPile.Remove(replacementCard);
            }

            if (!AddReturnedTopCardToHand(effect.ownerSide, returnedTopCard))
            {
                message = "最上層卡回手牌失敗，效果略過。";
                return false;
            }

            if (returnedTopCard != null)
            {
                _temporaryTypeGrants.Remove(returnedTopCard);
            }

            effectManager.RemoveEffect(effect);
            RefreshZoneInfoUI();
            RefreshHandLayout();

            string sideText = effect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手";
            string returnedName = string.IsNullOrWhiteSpace(returnedTopCard != null ? returnedTopCard.cardName : "")
                ? "最上層卡"
                : returnedTopCard.cardName;
            string replacementName = string.IsNullOrWhiteSpace(replacementCard.cardName) ? "迪卡" : replacementCard.cardName;
            message = $"登場效果階段｜{sideText}第 {targetLane.laneIndex + 1} 路 {returnedName} 回手牌，棄牌區 {replacementName} 登場到最上層。";
            return true;
        }

        List<UcgCardData> GetMatchingBp05008DiscardCards(UcgPlayerSide owner, int level)
        {
            var result = new List<UcgCardData>();
            List<UcgCardData> discardPile = GetDiscardPileForOwner(owner);
            if (discardPile == null || discardPile.Count == 0) return result;

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                UcgCardData candidate = discardPile[i];
                if (candidate == null) continue;
                if (candidate.level != level) continue;
                if (!CardCharacterContains(candidate, "迪卡")) continue;
                result.Add(candidate);
            }

            return result;
        }

        bool TryFindMatchingBp05008DiscardCard(UcgPlayerSide owner, int level, out UcgCardData card, out int index)
        {
            card = null;
            index = -1;
            List<UcgCardData> discardPile = GetDiscardPileForOwner(owner);
            if (discardPile == null || discardPile.Count == 0) return false;

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                UcgCardData candidate = discardPile[i];
                if (candidate == null) continue;
                if (candidate.level != level) continue;
                if (!CardCharacterContains(candidate, "迪卡")) continue;

                card = candidate;
                index = i;
                return true;
            }

            return false;
        }

        int FindCardIndexInDiscardPile(UcgPlayerSide owner, UcgCardData selectedCard)
        {
            if (selectedCard == null) return -1;
            List<UcgCardData> discardPile = GetDiscardPileForOwner(owner);
            if (discardPile == null) return -1;

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(discardPile[i], selectedCard)) return i;
            }

            return discardPile.IndexOf(selectedCard);
        }

        List<UcgCardData> GetDiscardPileForOwner(UcgPlayerSide owner)
        {
            return owner == UcgPlayerSide.Player ? _playerDiscardPile : _opponentDiscardPile;
        }

        bool CanReturnTopCardToHand(UcgPlayerSide owner)
        {
            if (owner == UcgPlayerSide.Player)
            {
                return deckManager != null && deckManager.playerHand != null && cardHolder != null;
            }

            return deckManager != null && deckManager.opponentHiddenHand != null;
        }

        bool AddReturnedTopCardToHand(UcgPlayerSide owner, UcgCardData returnedCard)
        {
            if (returnedCard == null || deckManager == null) return false;

            if (owner == UcgPlayerSide.Player)
            {
                if (deckManager.playerHand == null || cardHolder == null) return false;
                deckManager.playerHand.Add(returnedCard);
                AddCardToHand(returnedCard);
                return true;
            }

            if (deckManager.opponentHiddenHand == null) return false;
            deckManager.opponentHiddenHand.Add(returnedCard);
            return true;
        }

        bool EffectTargetSideMatchesFilter(UcgEffectInstance effect, UcgPlayerSide side, UcgEffectTargetFilter filter)
        {
            if (effect == null || filter == null) return true;
            switch (filter.targetSide)
            {
                case UcgEffectRelativeTargetSide.Self:
                    return side == effect.ownerSide;
                case UcgEffectRelativeTargetSide.Opponent:
                    return side == GetOpponentSide(effect.ownerSide);
                default:
                    return true;
            }
        }

        bool CardCharacterMatchesAny(UcgCardData card, System.Collections.Generic.IReadOnlyList<string> characterNames)
        {
            if (card == null || characterNames == null || characterNames.Count == 0) return false;
            for (int i = 0; i < characterNames.Count; i++)
            {
                if (CardCharacterMatches(card, characterNames[i])) return true;
            }

            return false;
        }

        bool CardCharacterContains(UcgCardData card, string keyword)
        {
            if (card == null || string.IsNullOrWhiteSpace(keyword)) return false;
            if (!string.IsNullOrWhiteSpace(card.characterName) && card.characterName.Contains(keyword)) return true;
            return !string.IsNullOrWhiteSpace(card.cardName) && card.cardName.Contains(keyword);
        }

        string GetInvalidEffectTargetMessage(UcgBattleLane lane, UcgPlayerSide side, UcgEffectInstance effect)
        {
            if (!IsLegalEffectTarget(lane, side)) return "此處沒有可選擇的角色";
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp)
            {
                return "請選擇與本角色相鄰的傑洛。";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp)
            {
                return "請選擇我方的傑洛。";
            }
            if (IsSwapOwnCharactersEffect(effect))
            {
                return _pendingSwapSourceLane == null
                    ? "請先選擇此效果的來源角色。"
                    : "請選擇另一名我方角色交換位置。";
            }
            if (IsBp05005MultiStepEffect(effect))
            {
                return _pendingBp05005StepDownLane == null
                    ? "請選擇 DOUBLE/TRIPLE 且等級 2 或 3、能降低 BP 的我方超人力霸王。"
                    : "請選擇我方可獲得 BP 上升的迪卡。";
            }
            if (IsBp05008TopDiscardSwapEffect(effect))
            {
                return "請選擇 DOUBLE/TRIPLE 且等級 2 或 3、可替換最上層的我方迪卡。";
            }

            if (effect != null && effect.cardData != null && UcgTutorialCardEffectMap.TryGetTargetFilter(effect.cardData, out _))
            {
                return "這個目標不符合效果條件。";
            }

            return "此處沒有可選擇的角色";
        }

        string GetNoLegalEffectTargetMessage(UcgEffectInstance effect)
        {
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp)
            {
                return "目前沒有相鄰的傑洛可以獲得 BP 上升。";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp)
            {
                return "目前沒有可獲得 BP 上升的傑洛。";
            }
            if (IsSwapOwnCharactersEffect(effect))
            {
                return "目前沒有另一名我方角色可以交換位置。";
            }
            if (IsBp05005MultiStepEffect(effect))
            {
                return _pendingBp05005StepDownLane == null
                    ? "目前沒有符合條件、可降低 BP 的我方角色。"
                    : "目前沒有可獲得 BP 上升的迪卡。";
            }
            if (IsBp05008TopDiscardSwapEffect(effect))
            {
                return "目前沒有符合條件的迪卡，或棄牌區沒有同等級迪卡。";
            }

            return "目前沒有符合條件的角色可以選擇。";
        }

        void SkipTargetEffectWithNoLegalTarget(UcgEffectInstance effect, string prefixMessage = "")
        {
            if (effectManager != null && effect != null)
            {
                effectManager.RemoveEffect(effect);
            }

            StopEffectSourceHighlight(effect);
            ClearEffectTargetSelection();
            string message = CombineEffectMessages(prefixMessage, GetNoLegalEffectTargetMessage(effect));
            QueueEffectFeedback(message);
            ShowPlayStatus(message, 1.1f);
            HighlightActivatedEffectSources();

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
                UpdateMainPrompt();
                return;
            }

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
                UpdateMainPrompt();
                return;
            }

            UpdateMainPrompt();
        }

        void ClearEffectTargetSelection()
        {
            _isSelectingEffectTarget = false;
            _pendingTargetEffect = null;
            _pendingTargetType = UcgEffectTargetType.None;
            _pendingSwapSourceLane = null;
            _pendingBp05005StepDownLane = null;
            ClearEffectTargetHighlights();
            RefreshNextPhaseButtonState();
        }

        void ClearEffectTargetHighlights()
        {
            if (battlefieldManager == null) return;

            var lanes = battlefieldManager.GetAllLanes();
            for (int i = 0; i < lanes.Count; i++)
            {
                if (lanes[i] != null)
                {
                    lanes[i].ClearEffectTargetHighlight();
                }
            }
        }

        void BeginAutoPhaseRoutine(UcgGamePhase phase)
        {
            if (_autoPhaseRoutine != null) return;
            _autoPhaseRoutine = StartCoroutine(AutoPhaseRoutine(phase));
        }

        IEnumerator AutoPhaseRoutine(UcgGamePhase phase)
        {
            _isAutoPhaseRunning = true;
            SetNextPhaseButtonInteractable(false);

            if (phase == UcgGamePhase.Start)
            {
                yield return new WaitForSecondsRealtime(0.15f);
                if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.Start)
                {
                    phaseManager.NextPhase();
                    ApplyBattlefieldViewForCurrentPhase();
                    UpdateMainPrompt();
                    phase = phaseManager.CurrentPhase;
                }
            }

            HandleAutoPhase(phase);
            if (turnManager != null)
            {
                turnManager.UpdateTurnInfoText();
            }
            UpdateMainPrompt();

            if (phase == UcgGamePhase.BattleJudgement && _judgementVisualRoutine != null)
            {
                yield return _judgementVisualRoutine;
            }
            else
            {
                yield return new WaitForSecondsRealtime(autoPhaseDelaySeconds);
            }

            if (IsGameOver)
            {
                _autoPhaseRoutine = null;
                _isAutoPhaseRunning = false;
                SetNextPhaseButtonInteractable(false);
                UpdateMainPrompt();
                yield break;
            }

            if (phaseManager != null && phaseManager.CurrentPhase == phase)
            {
                phaseManager.NextPhase();
            }

            _autoPhaseRoutine = null;
            _isAutoPhaseRunning = false;
            SetNextPhaseButtonInteractable(true);
            EnterCurrentPhase();
        }

        void HandleAutoPhase(UcgGamePhase phase)
        {
            switch (phase)
            {
                case UcgGamePhase.Draw:
                    HandleDrawPhase();
                    break;
                case UcgGamePhase.Open:
                    if (playResultText != null)
                    {
                        playResultText.text = "開放階段：雙方角色翻開，準備確認登場效果。";
                    }
                    if (battlefieldManager != null)
                    {
                        var revealedEffects = battlefieldManager.FlipAllFaceDownCardsAndCollectEffects();
                        if (effectManager != null)
                        {
                            effectManager.ClearQueue();
                            effectManager.EnqueueRevealEffects(revealedEffects, GetCurrentFirstPlayer());
                            if (debugEffectResolution)
                            {
                                Debug.Log($"Open phase queued enter effects: count={revealedEffects.Count}, pending={effectManager.PendingCount}");
                                for (int i = 0; i < revealedEffects.Count; i++)
                                {
                                    UcgEffectInstance effect = revealedEffects[i];
                                    UcgEffectRule rule = effect != null && effect.cardData != null
                                        ? UcgEffectParser.ParsePrimaryRule(effect.cardData)
                                        : null;
                                    int stackCount = effect != null && effect.ownerSide == UcgPlayerSide.Player && effect.lane != null && effect.lane.playerPlayArea != null
                                        ? effect.lane.playerPlayArea.GetStackCount()
                                        : effect != null && effect.lane != null
                                            ? effect.lane.GetOpponentStackCount()
                                            : 0;
                                    Debug.Log(
                                        "Enter effect queued after reveal:\n"
                                        + $"card={effect?.cardData?.id} {effect?.cardData?.cardName}\n"
                                        + $"effect={GetEffectText(effect?.cardData)}\n"
                                        + $"category={(rule != null ? rule.effectCategory.ToString() : "null")}\n"
                                        + $"action={(rule != null ? rule.actionType.ToString() : "null")}\n"
                                        + $"stackCount={stackCount}\n"
                                        + $"owner={effect?.ownerSide}\n"
                                        + $"lane={effect?.LaneIndex}");
                                }
                            }
                        }
                    }

                    int effectCount = effectManager != null ? effectManager.PendingCount : 0;
                    ShowPlayStatus(effectCount > 0
                        ? $"開放階段：有 {effectCount} 個登場效果待處理。"
                        : "開放階段：沒有待處理的登場效果。", 1.2f);
                    break;
                case UcgGamePhase.BattleJudgement:
                    if (playResultText != null)
                    {
                        playResultText.text = "勝負判定階段：正在比較雙方 BP。";
                    }
                    RunBattleJudgement();
                    break;
            }
        }

        void StopAutoPhaseRoutine()
        {
            if (_autoPhaseRoutine != null)
            {
                StopCoroutine(_autoPhaseRoutine);
                _autoPhaseRoutine = null;
            }

            _isAutoPhaseRunning = false;
            SetNextPhaseButtonInteractable(true);
        }

        void StopOpponentActionRoutine()
        {
            if (_opponentActionRoutine != null)
            {
                StopCoroutine(_opponentActionRoutine);
                _opponentActionRoutine = null;
            }

            _isOpponentActionRunning = false;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
        }

        void SetNextPhaseButtonInteractable(bool interactable)
        {
            if (nextPhaseButton == null) return;

            if (!interactable)
            {
                HideAdvanceButton();
                return;
            }

            RefreshNextPhaseButtonState();
        }

        void UpdateNextPhaseButtonLabel()
        {
            if (nextPhaseButton == null) return;
            RectTransform buttonRect = nextPhaseButton.transform as RectTransform;
            if (buttonRect == null) return;

            EnsureAdvancePromptVisual(buttonRect);
            if (_advancePromptMainText != null)
            {
                _advancePromptMainText.text = GetNextPhaseButtonLabel();
            }
        }

        void RefreshNextPhaseButtonState()
        {
            RefreshAdvanceButtonState();
        }

        bool IsAdvancePromptDebugEnabled()
        {
            return debugAdvanceButton || debugAdvancePrompt;
        }

        void RefreshAdvanceButtonState()
        {
            if (nextPhaseButton == null) return;

            string label = GetNextPhaseButtonLabel();
            string hiddenReason = GetAdvanceButtonHiddenReason();
            bool canShow = string.IsNullOrWhiteSpace(hiddenReason);
            if (IsAdvancePromptDebugEnabled())
            {
                string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
                bool autoAdvance = ShouldAutoAdvancePrompt();
                string countdownText = autoAdvance ? GetAdvancePromptCountdownSeconds().ToString("0.00") : "manual";
                Debug.Log(
                    "AdvancePrompt state:\n"
                    + $"phase={phaseText}\n"
                    + $"isPlayerAction={IsPlayerActionState()}\n"
                    + $"canShow={canShow}\n"
                    + $"label={label}\n"
                    + $"countdown={countdownText}\n"
                    + $"autoAdvance={autoAdvance}\n"
                    + $"hiddenReason={(canShow ? "none" : hiddenReason)}");
            }

            if (!canShow)
            {
                HideAdvanceButton();
                LogAdvancePromptRequest(GetAdvancePromptRequestContext(), label, false, hiddenReason);
                return;
            }

            ShowAdvanceButton(label, NextPhase);
            LogAdvancePromptRequest(GetAdvancePromptRequestContext(), label, true, "");
        }

        void ShowAdvanceButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            if (nextPhaseButton == null) return;

            RectTransform buttonRect = nextPhaseButton.transform as RectTransform;
            if (buttonRect != null)
            {
                EnsureAdvancePromptVisual(buttonRect);
            }

            bool autoAdvance = ShouldAutoAdvancePrompt();
            float countdownSeconds = autoAdvance ? GetAdvancePromptCountdownSeconds() : 0f;
            bool samePrompt = nextPhaseButton.gameObject.activeSelf
                && !_advancePromptHandled
                && _currentAdvancePromptLabel == label
                && _advancePromptAutoAdvanceEnabled == autoAdvance
                && _shownAdvancePromptResetVersion == _advancePromptResetVersion
                && (!autoAdvance || _isAdvanceCountdownActive)
                && Mathf.Approximately(_advanceCountdownTotalSeconds, countdownSeconds);

            _advancePromptConfirmAction = onClick;
            if (_advancePromptMainText != null)
            {
                _advancePromptMainText.text = label;
            }

            nextPhaseButton.onClick.RemoveAllListeners();
            nextPhaseButton.onClick.AddListener(() => TriggerAdvancePrompt("click"));
            nextPhaseButton.interactable = true;
            nextPhaseButton.gameObject.SetActive(true);
            EnsureAdvancePromptVisible();
            if (dragLayer != null)
            {
                dragLayer.SetAsLastSibling();
            }
            nextPhaseButton.transform.SetAsLastSibling();

            if (samePrompt)
            {
                UpdateAdvancePromptCountdownVisual();
                return;
            }

            if (autoAdvance)
            {
                StartAdvanceCountdown(label, countdownSeconds);
            }
            else
            {
                ShowManualAdvancePrompt(label);
            }
        }

        void EnsureAdvancePromptVisible()
        {
            if (nextPhaseButton == null) return;

            CanvasGroup canvasGroup = nextPhaseButton.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = nextPhaseButton.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            RectTransform promptRect = nextPhaseButton.transform as RectTransform;
            if (promptRect != null)
            {
                EnsureAdvancePromptVisual(promptRect);
            }

            nextPhaseButton.interactable = true;
            nextPhaseButton.gameObject.SetActive(true);
        }

        void HideAdvanceButton()
        {
            if (nextPhaseButton == null) return;

            StopAdvanceCountdownRoutine();
            _advancePromptConfirmAction = null;
            _advancePromptHandled = false;
            _currentAdvancePromptLabel = "";
            _advancePromptAutoAdvanceEnabled = true;
            _shownAdvancePromptResetVersion = -1;
            SetTopPromptProgress(0f, false);
            nextPhaseButton.interactable = false;
            nextPhaseButton.gameObject.SetActive(false);
        }

        void ResetAdvancePromptCountdownForNewDecision()
        {
            _advancePromptResetVersion++;
            _advancePromptHandled = false;
            _shownAdvancePromptResetVersion = -1;
            _isAdvanceCountdownPaused = false;
            StopAdvanceCountdownRoutine();
            SetTopPromptProgress(0f, false);
        }

        bool ShouldAutoAdvancePrompt()
        {
            return !(phaseManager != null
                && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect
                && effectManager != null
                && effectManager.HasPendingEffects);
        }

        float GetAdvancePromptCountdownSeconds()
        {
            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                return Mathf.Max(1f, upgradeAdvanceCountdownSeconds);
            }

            return Mathf.Max(0.75f, advanceButtonCountdownSeconds);
        }

        void StartAdvanceCountdown(string label, float countdownSeconds)
        {
            StopAdvanceCountdownRoutine();
            _currentAdvancePromptLabel = label;
            _advancePromptHandled = false;
            _advancePromptAutoAdvanceEnabled = true;
            _shownAdvancePromptResetVersion = _advancePromptResetVersion;
            _isAdvanceCountdownPaused = false;
            _advanceCountdownTotalSeconds = Mathf.Max(0.1f, countdownSeconds);
            _advanceCountdownRemainingSeconds = _advanceCountdownTotalSeconds;
            UpdateAdvancePromptCountdownVisual();
            _advanceCountdownRoutine = StartCoroutine(AdvanceCountdownRoutine());

            if (IsAdvancePromptDebugEnabled())
            {
                string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
                Debug.Log(
                    "AdvancePrompt:\n"
                    + $"label={label}\n"
                    + $"countdown={_advanceCountdownTotalSeconds:0.00}\n"
                    + $"autoAdvance=true\n"
                    + $"phase={phaseText}\n"
                    + "hiddenReason=none");
            }
        }

        void ShowManualAdvancePrompt(string label)
        {
            StopAdvanceCountdownRoutine();
            _currentAdvancePromptLabel = label;
            _advancePromptHandled = false;
            _advancePromptAutoAdvanceEnabled = false;
            _shownAdvancePromptResetVersion = _advancePromptResetVersion;
            _isAdvanceCountdownPaused = false;
            _advanceCountdownTotalSeconds = 0f;
            _advanceCountdownRemainingSeconds = 0f;
            UpdateAdvancePromptCountdownVisual();

            if (IsAdvancePromptDebugEnabled())
            {
                string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
                Debug.Log(
                    "AdvancePrompt:\n"
                    + $"label={label}\n"
                    + "countdown=manual\n"
                    + "autoAdvance=false\n"
                    + $"phase={phaseText}\n"
                    + "hiddenReason=none");
            }
        }

        IEnumerator AdvanceCountdownRoutine()
        {
            _isAdvanceCountdownActive = true;
            while (_advanceCountdownRemainingSeconds > 0f)
            {
                if (!_isAdvanceCountdownPaused)
                {
                    _advanceCountdownRemainingSeconds -= Time.unscaledDeltaTime;
                    if (_advanceCountdownRemainingSeconds < 0f)
                    {
                        _advanceCountdownRemainingSeconds = 0f;
                    }
                }

                UpdateAdvancePromptCountdownVisual();
                yield return null;
            }

            _advanceCountdownRoutine = null;
            _isAdvanceCountdownActive = false;
            TriggerAdvancePrompt("countdown");
        }

        void StopAdvanceCountdownRoutine()
        {
            if (_advanceCountdownRoutine != null)
            {
                StopCoroutine(_advanceCountdownRoutine);
                _advanceCountdownRoutine = null;
            }

            _isAdvanceCountdownActive = false;
            _isAdvanceCountdownPaused = false;
        }

        void PauseAdvanceCountdownForDrag()
        {
            if (!_isAdvanceCountdownActive || _advancePromptHandled) return;
            _isAdvanceCountdownPaused = true;
            UpdateAdvancePromptCountdownVisual();
        }

        void ResumeAdvanceCountdownAfterDrag()
        {
            if (!_isAdvanceCountdownActive || _advancePromptHandled) return;
            _isAdvanceCountdownPaused = false;
            UpdateAdvancePromptCountdownVisual();
        }

        void UpdateAdvancePromptCountdownVisual()
        {
            if (!_advancePromptAutoAdvanceEnabled)
            {
                if (_advancePromptMainText != null)
                {
                    _advancePromptMainText.text = "";
                }

                if (_advancePromptCountdownText != null)
                {
                    _advancePromptCountdownText.text = "";
                }

                if (_advancePromptProgressTrackRect != null)
                {
                    _advancePromptProgressTrackRect.gameObject.SetActive(false);
                }
                SetTopPromptProgress(0f, false);
                return;
            }

            if (_advancePromptProgressTrackRect != null)
            {
                _advancePromptProgressTrackRect.gameObject.SetActive(false);
            }

            float total = Mathf.Max(0.1f, _advanceCountdownTotalSeconds);
            float remaining = Mathf.Clamp(_advanceCountdownRemainingSeconds, 0f, total);

            if (_advancePromptMainText != null)
            {
                _advancePromptMainText.text = "";
            }

            if (_advancePromptCountdownText != null)
            {
                _advancePromptCountdownText.text = "";
            }
            SetTopPromptProgress(remaining / total, true);

            if (_advancePromptProgressFillRect != null)
            {
                float progress = remaining / total;
                _advancePromptProgressFillRect.anchorMax = new Vector2(progress, 1f);
                _advancePromptProgressFillRect.offsetMin = Vector2.zero;
                _advancePromptProgressFillRect.offsetMax = Vector2.zero;
            }
        }

        void TriggerAdvancePrompt(string triggerSource)
        {
            if (_advancePromptHandled) return;
            bool canClickAdvancePrompt = CanPlayerClickAdvancePrompt(out string hiddenReason);
            LogAdvancePromptClick(triggerSource, canClickAdvancePrompt, hiddenReason);
            if (!canClickAdvancePrompt)
            {
                if (IsAdvancePromptDebugEnabled())
                {
                    Debug.Log(
                        "AdvancePrompt trigger cancelled:\n"
                        + $"label={_currentAdvancePromptLabel}\n"
                        + $"trigger={triggerSource}\n"
                        + $"hiddenReason={hiddenReason}");
                }
                LogInteractionRejected("Advance", hiddenReason);
                HideAdvanceButton();
                return;
            }

            _advancePromptHandled = true;
            UnityEngine.Events.UnityAction action = _advancePromptConfirmAction;
            StopAdvanceCountdownRoutine();
            _advancePromptConfirmAction = null;
            SetTopPromptProgress(0f, false);

            if (IsAdvancePromptDebugEnabled())
            {
                string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
                Debug.Log(
                    "AdvancePrompt triggered:\n"
                    + $"label={_currentAdvancePromptLabel}\n"
                    + $"trigger={triggerSource}\n"
                    + $"phase={phaseText}");
            }

            if (nextPhaseButton != null)
            {
                nextPhaseButton.interactable = false;
                nextPhaseButton.gameObject.SetActive(false);
            }

            action?.Invoke();
        }

        void LogAdvancePromptClick(string triggerSource, bool canClick, string reason)
        {
            if (!IsAdvancePromptDebugEnabled() && !debugInteractionLock) return;

            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            bool isHandReturnSelecting = IsHandReturnSelectionMode();
            bool isDeckOperationSelecting = _isSelectingDeckOperationCard && !isHandReturnSelecting;

            Debug.Log(
                "Advance click:\n"
                + $"trigger={triggerSource}\n"
                + $"phase={phaseText}\n"
                + $"label={_currentAdvancePromptLabel}\n"
                + $"canClick={canClick}\n"
                + $"reason={(canClick ? "none" : NormalizeInteractionReason(reason))}\n"
                + $"isCountdownActive={_isAdvanceCountdownActive}\n"
                + $"isCountdownPaused={_isAdvanceCountdownPaused}\n"
                + $"isDraggingHandCard={_isPlayerDraggingHandCard}\n"
                + $"isDeckOperationSelecting={isDeckOperationSelecting}\n"
                + $"isHandReturnSelecting={isHandReturnSelecting}\n"
                + $"isTargetSelecting={_isSelectingEffectTarget}\n"
                + $"isOpponentActing={_isOpponentActionRunning}");
        }

        string GetAdvancePromptRequestContext()
        {
            if (phaseManager == null) return "NoPhase";
            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.End:
                    return "AfterJudgement";
                case UcgGamePhase.Start:
                    return "BeforeNextTurn";
                case UcgGamePhase.BattleJudgement:
                    return "JudgementInProgress";
                default:
                    return phaseManager.CurrentPhase.ToString();
            }
        }

        void LogAdvancePromptRequest(string context, string label, bool canShow, string hiddenReason)
        {
            if (!IsAdvancePromptDebugEnabled() && !debugInteractionLock) return;

            bool isHandReturnSelecting = IsHandReturnSelectionMode();
            bool isDeckOperationSelecting = _isSelectingDeckOperationCard && !isHandReturnSelecting;
            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            string actingPlayerText = turnOrderManager != null ? turnOrderManager.currentActingPlayer.ToString() : "None";
            bool promptActiveSelf = nextPhaseButton != null && nextPhaseButton.gameObject.activeSelf;
            CanvasGroup promptGroup = nextPhaseButton != null ? nextPhaseButton.GetComponent<CanvasGroup>() : null;
            float promptAlpha = promptGroup != null ? promptGroup.alpha : 1f;
            string promptParent = nextPhaseButton != null && nextPhaseButton.transform.parent != null
                ? nextPhaseButton.transform.parent.name
                : "none";

            Debug.Log(
                "Advance prompt request:\n"
                + $"context={context}\n"
                + $"label={label}\n"
                + $"phase={phaseText}\n"
                + $"actingPlayer={actingPlayerText}\n"
                + $"canShow={canShow}\n"
                + $"hiddenReason={(canShow ? "none" : NormalizeInteractionReason(hiddenReason))}\n"
                + $"isJudgementInProgress={(phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleJudgement) || (_isAutoPhaseRunning && context == "JudgementInProgress")}\n"
                + $"isOpponentActionRunning={_isOpponentActionRunning}\n"
                + $"isDeckOperationSelecting={isDeckOperationSelecting}\n"
                + $"isHandReturnSelecting={isHandReturnSelecting}\n"
                + $"isTargetSelecting={_isSelectingEffectTarget}\n"
                + $"promptActiveSelf={promptActiveSelf}\n"
                + $"promptCanvasGroupAlpha={promptAlpha:0.00}\n"
                + $"promptParent={promptParent}");
        }

        string GetNextPhaseButtonLabel()
        {
            if (IsGameOver) return "重新開始";
            if (_isOpeningFirstPlayerSequence) return "決定先攻中";
            if (_pendingAction != null) return "確認中";
            if (_isEffectAutoAdvancing) return "進入判定中";
            if (_isAutoPhaseRunning || _sceneSetupSkipRoutine != null) return "處理中";
            if (_isOpponentActionRunning) return "等待對手";
            if (phaseManager == null) return "下一階段";

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                    return HasLegalSceneCardInHand() ? "跳過場景" : "處理中";
                case UcgGamePhase.CharacterSetup:
                    return CanAdvanceFromCharacterSetupQuiet() ? "進入升級" : "請先設置角色";
                case UcgGamePhase.Upgrade:
                    return "結束升級";
                case UcgGamePhase.EnterEffect:
                    if (_isSelectingDeckOperationCard) return "選牌中";
                    if (_isSelectingEffectTarget) return "選擇目標中";
                    return "處理登場效果";
                case UcgGamePhase.BattleEffect:
                    if (_isSelectingDeckOperationCard) return "選牌中";
                    if (_isSelectingEffectTarget) return "選擇目標中";
                    return effectManager != null && effectManager.HasPendingEffects ? "結束效果" : "進入判定";
                case UcgGamePhase.End:
                    return "繼續";
                case UcgGamePhase.Open:
                case UcgGamePhase.BattleJudgement:
                case UcgGamePhase.Draw:
                case UcgGamePhase.Start:
                    return "處理中";
                default:
                    return "下一階段";
            }
        }

        bool CanUseNextPhaseButtonNow()
        {
            return CanShowAdvanceButtonNow();
        }

        bool CanShowAdvanceButtonNow()
        {
            return string.IsNullOrWhiteSpace(GetAdvanceButtonHiddenReason());
        }

        string GetAdvanceButtonHiddenReason()
        {
            string lockReason = GetPlayerInteractionLockReason(false, false);
            if (!string.IsNullOrWhiteSpace(lockReason)) return lockReason;

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                    return HasLegalSceneCardInHand()
                        ? ""
                        : "NoLegalSceneCard";
                case UcgGamePhase.CharacterSetup:
                    return CanAdvanceFromCharacterSetupQuiet()
                        ? ""
                        : "CharacterSetupNotComplete";
                case UcgGamePhase.Upgrade:
                    return IsPlayerActionState()
                        ? ""
                        : "NotPlayerAction";
                case UcgGamePhase.BattleEffect:
                    return effectManager != null && effectManager.HasPendingEffects
                        ? ""
                        : "NoPendingBattleEffect";
                case UcgGamePhase.End:
                    return "";
                case UcgGamePhase.Start:
                case UcgGamePhase.Draw:
                case UcgGamePhase.Open:
                case UcgGamePhase.EnterEffect:
                case UcgGamePhase.BattleJudgement:
                    return "AutoPhaseOrEffectPhase";
                default:
                    return "UnsupportedPhase";
            }
        }

        bool CanAdvanceFromCharacterSetupQuiet()
        {
            if (turnManager == null || battlefieldManager == null) return true;

            int laneIndex = turnManager.ActiveNewLaneIndex;
            UcgBattleLane lane = battlefieldManager.GetLane(laneIndex);
            if (lane == null) return true;

            lane.RefreshPlayerStateFromPlayArea();
            return lane.playerTopCard != null && lane.opponentTopCard != null;
        }

        void ShowWaitForOpponentMessage()
        {
            if (playResultText != null)
            {
                playResultText.text = "請等待對手操作完成";
            }
        }

        void ApplyBattlefieldViewForCurrentPhase()
        {
            if (battlefieldManager == null || phaseManager == null) return;
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            ApplyCombatFocusViewportPosition("ApplyBattlefieldViewForCurrentPhase");
            battlefieldManager.RefreshOpenedLaneVisibility(currentTurn);

            if (phaseManager.CurrentPhase == UcgGamePhase.SceneSetup
                || phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                || phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : 0;
                battlefieldManager.SmoothFocusActiveLane(activeLaneIndex < 0 ? 0 : activeLaneIndex);
                LogCombatViewportDiagnostic("ApplyBattlefieldViewForCurrentPhase", "FocusLane", activeLaneIndex < 0 ? 0 : activeLaneIndex);
            }
            else
            {
                battlefieldManager.ShowOverview();
                LogCombatViewportDiagnostic("ApplyBattlefieldViewForCurrentPhase", "OverviewAll", GetCurrentActiveLaneIndex());
            }

            RefreshBoardZoneLayout();
        }

        bool CanAdvanceFromCharacterSetup()
        {
            if (turnManager == null || battlefieldManager == null) return true;

            int laneIndex = turnManager.ActiveNewLaneIndex;
            UcgBattleLane lane = battlefieldManager.GetLane(laneIndex);
            if (lane == null) return true;

            lane.RefreshPlayerStateFromPlayArea();
            if (lane.playerTopCard == null)
            {
                if (playResultText != null)
                {
                    playResultText.text = $"請先將角色牌設置到第 {laneIndex + 1} 路";
                }
                return false;
            }

            if (lane.opponentTopCard == null)
            {
                BeginOpponentSetupRoutine(lane, false);
                return false;
            }

            return true;
        }

        UcgBattleLane GetActiveLane()
        {
            if (turnManager == null || battlefieldManager == null) return null;

            int laneIndex = turnManager.ActiveNewLaneIndex;
            return battlefieldManager.GetLane(laneIndex);
        }

        public bool CanPlayerActInLane(UcgBattleLane lane, bool isNewPlacement, out string message)
        {
            if (!CanPlayerInteract(out string reason))
            {
                message = GetInteractionLockMessage(reason);
                return false;
            }

            if (isNewPlacement
                && phaseManager != null
                && phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                && IsCurrentFirstPlayer(UcgPlayerSide.Opponent)
                && lane != null
                && lane.opponentTopCard == null)
            {
                message = "請等待對手設置完成";
                return false;
            }

            message = "";
            return true;
        }

        public void RequestOpponentSetupAfterPlayerPlacement(UcgBattleLane lane)
        {
            if (!IsCurrentFirstPlayer(UcgPlayerSide.Player)) return;
            BeginOpponentSetupRoutine(lane, false);
        }

        bool IsCurrentFirstPlayer(UcgPlayerSide side)
        {
            return turnOrderManager == null
                ? side == UcgPlayerSide.Player
                : turnOrderManager.GetCurrentFirstPlayer() == side;
        }

        bool IsPlayerActionState()
        {
            return turnOrderManager == null
                || turnOrderManager.currentActingPlayer == UcgPlayerSide.Player;
        }

        public bool CanPlayerInteract(out string reason)
        {
            reason = GetPlayerInteractionLockReason(false, false);
            return string.IsNullOrWhiteSpace(reason);
        }

        public bool CanPlayerDragHandCard(UcgCardView cardView, out string reason)
        {
            reason = "";
            if (cardView == null || cardView.CardData == null)
            {
                reason = "InvalidCard";
                return false;
            }

            if (cardView.IsLockedInBattlefield)
            {
                reason = "CardLockedInBattlefield";
                return false;
            }

            if (!CanPlayerInteract(out reason))
            {
                return false;
            }

            if (phaseManager == null)
            {
                reason = "NoPhaseManager";
                return false;
            }

            UcgCardData cardData = cardView.CardData;
            if (cardData.IsSceneCard())
            {
                if (phaseManager.CurrentPhase != UcgGamePhase.SceneSetup)
                {
                    reason = "WrongPhase";
                    return false;
                }

                return CanPlayerDropToScene(cardData, out reason);
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                || phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                return HasAnyLegalLaneDrop(cardData, out reason);
            }

            reason = phaseManager.CurrentPhase == UcgGamePhase.SceneSetup
                ? "InvalidCardType"
                : "WrongPhase";
            return false;
        }

        public bool CanPlayerDropToLane(UcgCardData cardData, UcgBattleLane lane, out string reason)
        {
            bool accepted = ValidatePlayerCardDrop(
                cardData,
                lane,
                UcgPlayerCardDropTarget.Lane,
                out string message,
                out _,
                false);
            reason = accepted ? "" : NormalizeInteractionReason(message);
            return accepted;
        }

        public bool CanPlayerDropToScene(UcgCardData cardData, out string reason)
        {
            bool accepted = ValidatePlayerCardDrop(
                cardData,
                null,
                UcgPlayerCardDropTarget.SceneSlot,
                out string message,
                out _,
                false);
            reason = accepted ? "" : NormalizeInteractionReason(message);
            return accepted;
        }

        public bool CanPlayerClickHandCard(UcgCardView cardView, out string reason)
        {
            reason = "";
            if (cardView == null || cardView.CardData == null)
            {
                reason = "InvalidCard";
                return false;
            }

            if (IsHandReturnSelectionMode())
            {
                bool cardInHand = deckManager == null || deckManager.playerHand.Contains(cardView.CardData);
                reason = cardInHand ? "" : "InvalidCardZone";
                return cardInHand;
            }

            if (!CanPlayerInteract(out reason))
            {
                return false;
            }

            if (phaseManager == null)
            {
                reason = "NoPhaseManager";
                return false;
            }

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                case UcgGamePhase.CharacterSetup:
                case UcgGamePhase.Upgrade:
                case UcgGamePhase.BattleEffect:
                    return true;
                default:
                    reason = "WrongPhase";
                    return false;
            }
        }

        public bool CanPlayerClickAdvancePrompt(out string reason)
        {
            if (_isPlayerDraggingHandCard)
            {
                reason = "DraggingHandCard";
                return false;
            }

            if (_isResolvingVisualAnimation)
            {
                reason = "VisualAnimationInProgress";
                return false;
            }

            if (_isAdvanceCountdownPaused)
            {
                reason = "AdvanceCountdownBlocked";
                return false;
            }

            reason = NormalizeInteractionReason(GetAdvanceButtonHiddenReason());
            return string.IsNullOrWhiteSpace(reason);
        }

        public void ReportInteractionRejected(string action, string reason, UcgCardData cardData = null, UcgBattleLane lane = null)
        {
            LogInteractionRejected(action, reason, cardData, lane);
        }

        string GetPlayerInteractionLockReason(bool allowHandReturnSelection, bool allowEffectTargetSelection)
        {
            if (_isTutorialFinishWaitingForClick) return "Finished";
            if (_isOpeningFirstPlayerSequence) return "OpeningFirstPlayerSequence";
            if (IsGameOver) return "GameOver";
            if (_pendingAction != null) return "ModalOpen";
            if (_pendingConfirmRoot != null && _pendingConfirmRoot.gameObject.activeInHierarchy) return "ModalOpen";
            if (discardPilePanel != null && discardPilePanel.gameObject.activeInHierarchy) return "ModalOpen";
            if (_isResolvingVisualAnimation) return "VisualAnimationInProgress";

            if (_isSelectingDeckOperationCard)
            {
                if (IsHandReturnSelectionMode())
                {
                    return allowHandReturnSelection ? "" : "HandReturnSelecting";
                }

                return "DeckOperationSelecting";
            }

            if (_isSelectingEffectTarget)
            {
                return allowEffectTargetSelection ? "" : "TargetSelecting";
            }

            if (_isEffectAutoAdvancing) return "EffectResolving";
            if (_isAutoPhaseRunning || _sceneSetupSkipRoutine != null) return "AutoPhaseInProgress";
            if (_isOpponentActionRunning) return "OpponentActionInProgress";
            if (phaseManager == null) return "NoPhaseManager";
            if (phaseManager.CurrentPhase == UcgGamePhase.BattleJudgement) return "BattleJudgement";
            if (!IsPlayerActionState()) return "NotPlayerAction";
            return "";
        }

        bool HasAnyLegalLaneDrop(UcgCardData cardData, out string reason)
        {
            reason = "";
            if (battlefieldManager == null || turnManager == null)
            {
                reason = "NoBattlefield";
                return false;
            }

            string lastReason = "";
            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;
                if (CanPlayerDropToLane(cardData, lane, out lastReason))
                {
                    reason = "";
                    return true;
                }
            }

            reason = string.IsNullOrWhiteSpace(lastReason) ? "WrongLane" : lastReason;
            return false;
        }

        string GetInteractionLockMessage(string reason)
        {
            switch (NormalizeInteractionReason(reason))
            {
                case "OpponentActionInProgress":
                case "NotPlayerAction":
                    return "請等待對手操作完成";
                case "FirstPlayerResultShowing":
                case "OpeningFirstPlayerSequence":
                    return "正在決定先攻";
                case "DeckOperationSelecting":
                    return "請先完成選牌";
                case "HandReturnSelecting":
                    return "請選擇一張手牌放回牌庫最底下";
                case "TargetSelecting":
                    return "請先完成效果目標選擇";
                case "Finished":
                    return "教學已完成，請點擊畫面任意處繼續";
                case "GameOver":
                    return "遊戲已結束，請重新開始";
                case "ModalOpen":
                    return "請先關閉目前視窗";
                case "EffectResolving":
                    return "效果處理中，請稍候";
                case "VisualAnimationInProgress":
                    return "動畫處理中，請稍候";
                case "AutoPhaseInProgress":
                case "AdvanceCountdownBlocked":
                    return "請等待目前流程完成";
                case "DraggingHandCard":
                    return "拖曳中，請先放開手牌";
                case "JudgementInProgress":
                case "BattleJudgement":
                    return "判定中，請稍候";
                case "WrongPhase":
                    return "現在不能放這張牌";
                case "WrongLane":
                    return "請放到目前可操作的路線";
                case "InvalidCardType":
                    return "這張牌不能放到此位置";
                default:
                    return "現在不能操作";
            }
        }

        string NormalizeInteractionReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return "";

            if (reason == "FinishedState" || reason == "Finished") return "Finished";
            if (reason == "OpeningFirstPlayerSequence" || reason == "FirstPlayerResultShowing") return "FirstPlayerResultShowing";
            if (reason == "GameOver") return "GameOver";
            if (reason == "ModalLock" || reason == "ModalOpen") return "ModalOpen";
            if (reason == "DeckOperationSelectionOpen" || reason == "DeckOperationSelecting") return "DeckOperationSelecting";
            if (reason == "HandReturnSelectionOpen" || reason == "HandReturnSelecting") return "HandReturnSelecting";
            if (reason == "EffectTargetSelectionOpen" || reason == "TargetSelecting") return "TargetSelecting";
            if (reason == "AutoEffectAdvance" || reason == "EffectResolving") return "EffectResolving";
            if (reason == "AutoPhaseInProgress" || reason == "SceneSetupSkipInProgress") return "AutoPhaseInProgress";
            if (reason == "OpponentActionInProgress") return "OpponentActionInProgress";
            if (reason == "NoPendingBattleEffect" || reason == "UnsupportedPhase" || reason == "AutoPhaseOrEffectPhase") return "WrongPhase";
            if (reason == "CharacterSetupNotComplete") return "WrongPhase";
            if (reason == "NoLegalSceneCard") return "InvalidCardType";
            if (reason == "NotPlayerAction") return "NotPlayerAction";
            if (reason == "NoPhaseManager") return "NoPhaseManager";
            if (reason == "BattleJudgement" || reason == "JudgementInProgress") return "JudgementInProgress";
            if (reason == "InvalidCard" || reason == "InvalidCardZone" || reason == "CardLockedInBattlefield") return "InvalidCardType";
            if (reason == "InvalidTarget" || reason == "InvalidTargetSide") return "WrongLane";
            if (reason == "DraggingHandCard") return "DraggingHandCard";
            if (reason == "VisualAnimationInProgress") return "VisualAnimationInProgress";

            if (reason.Contains("對手")) return "OpponentActionInProgress";
            if (reason.Contains("先攻")) return "FirstPlayerResultShowing";
            if (reason.Contains("選擇一張手牌放回牌庫")) return "HandReturnSelecting";
            if (reason.Contains("選牌")) return "DeckOperationSelecting";
            if (reason.Contains("目標")) return "TargetSelecting";
            if (reason.Contains("教學已完成")) return "Finished";
            if (reason.Contains("遊戲已結束")) return "GameOver";
            if (reason.Contains("確認") || reason.Contains("視窗")) return "ModalOpen";
            if (reason.Contains("效果處理")) return "EffectResolving";
            if (reason.Contains("等待") || reason.Contains("流程")) return "AutoPhaseInProgress";
            if (reason.Contains("判定")) return "JudgementInProgress";
            if (reason.Contains("不是有效") || reason.Contains("不能設置到") || reason.Contains("不能放到"))
            {
                return "InvalidCardType";
            }
            if (reason.Contains("路線") || reason.Contains("Lane") || reason.Contains("戰鬥區") || reason.Contains("尚未開放"))
            {
                return "WrongLane";
            }
            if (reason.Contains("階段") || reason.Contains("目前"))
            {
                return "WrongPhase";
            }

            return reason;
        }

        void LogInteractionRejected(string action, string reason, UcgCardData cardData = null, UcgBattleLane lane = null)
        {
            if (!debugInteractionLock) return;

            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            string actingPlayerText = turnOrderManager != null ? turnOrderManager.currentActingPlayer.ToString() : "None";
            string laneText = lane != null ? (lane.laneIndex + 1).ToString() : "none";

            Debug.Log(
                "Interaction rejected:\n"
                + $"action={action}\n"
                + $"card={FormatDrawSource(cardData)}\n"
                + $"targetLane={laneText}\n"
                + $"phase={phaseText}\n"
                + $"actingPlayer={actingPlayerText}\n"
                + $"reason={NormalizeInteractionReason(reason)}\n"
                + $"stateFlags={BuildInteractionStateFlags()}");
        }

        string BuildInteractionStateFlags()
        {
            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            string actingPlayerText = turnOrderManager != null ? turnOrderManager.currentActingPlayer.ToString() : "None";
            return $"phase={phaseText}, actingPlayer={actingPlayerText}, "
                + $"opponentAction={_isOpponentActionRunning}, autoPhase={_isAutoPhaseRunning}, "
                + $"visualAnimation={_isResolvingVisualAnimation}, "
                + $"draggingHandCard={_isPlayerDraggingHandCard}, "
                + $"sceneSkip={(_sceneSetupSkipRoutine != null)}, deckSelection={_isSelectingDeckOperationCard}, "
                + $"handReturn={IsHandReturnSelectionMode()}, targetSelection={_isSelectingEffectTarget}, "
                + $"pendingAction={(_pendingAction != null)}, effectAuto={_isEffectAutoAdvancing}, "
                + $"finished={_isTutorialFinishWaitingForClick}, gameOver={IsGameOver}";
        }

        UcgPlayerSide GetCurrentFirstPlayer()
        {
            return turnOrderManager != null ? turnOrderManager.GetCurrentFirstPlayer() : UcgPlayerSide.Player;
        }

        bool HasHydratedOpponentRuntimeDeck()
        {
            return deckManager != null && deckManager.OpponentProfile != null;
        }

        void BeginOpponentSetupRoutine(UcgBattleLane lane, bool opponentFirst)
        {
            if (lane == null || lane.opponentTopCard != null) return;
            if (_opponentActionRoutine != null) return;
            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Opponent);
            _opponentActionRoutine = StartCoroutine(OpponentSetupRoutine(lane, opponentFirst));
        }

        bool BeginOpponentSceneSetupRoutine(bool advanceToCharacterSetupAfterDone)
        {
            if (_opponentActionRoutine != null) return false;
            if (sharedSceneSlot == null) return false;
            if (opponentScript == null || turnManager == null) return false;

            UcgCardData sceneCard = FindOpponentSceneCardInHiddenHand(out _);
            if (sceneCard == null && !HasHydratedOpponentRuntimeDeck())
            {
                sceneCard = opponentScript.GetOpponentSceneCard(currentTestMode, turnManager.currentTurn);
                if (sceneCard != null)
                {
                    Debug.LogWarning("Opponent scene fell back to hardcoded script card because no legal hydrated deck scene was available.");
                }
            }
            if (sceneCard == null) return false;
            if (!CanPlaceSceneCard(sceneCard, UcgPlayerSide.Opponent, out _)) return false;

            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Opponent);
            _opponentActionRoutine = StartCoroutine(OpponentSceneSetupRoutine(sceneCard, advanceToCharacterSetupAfterDone));
            return true;
        }

        IEnumerator OpponentSceneSetupRoutine(UcgCardData sceneCard, bool advanceToCharacterSetupAfterDone)
        {
            _isOpponentActionRunning = true;
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            ShowPlayStatus("對手進入場景設置階段");

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            if (sceneCard != null && TryPlaceSceneCardFromScript(sceneCard, UcgPlayerSide.Opponent, out string sceneMessage))
            {
                RemoveOpponentHiddenCard(sceneCard);
                ShowPlayStatus(string.IsNullOrWhiteSpace(sceneMessage)
                    ? $"對手設置場景：{GetCardDisplayName(sceneCard)}"
                    : sceneMessage);
                if (sfxController != null)
                {
                    sfxController.PlayCardPlace();
                }
            }
            else
            {
                ShowPlayStatus("對手沒有可設置場景卡");
            }

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            _opponentActionRoutine = null;
            _isOpponentActionRunning = false;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);

            if (advanceToCharacterSetupAfterDone && phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                phaseManager.NextPhase();
                EnterCurrentPhase();
            }
            else
            {
                UpdateMainPrompt();
                RestoreCompactPlayStatus();
                RefreshInteractionHints();
            }
        }

        void BeginSceneSetupSkipRoutine(string message)
        {
            if (_sceneSetupSkipRoutine != null) return;
            _sceneSetupSkipRoutine = StartCoroutine(SceneSetupSkipRoutine(message));
        }

        IEnumerator SceneSetupSkipRoutine(string message)
        {
            _isAutoPhaseRunning = true;
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            if (playResultText != null)
            {
                playResultText.text = message;
            }

            yield return new WaitForSecondsRealtime(0.75f);

            _sceneSetupSkipRoutine = null;
            _isAutoPhaseRunning = false;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);

            if (!IsGameOver && phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                _isAutoPhaseRunning = false;
                if (ShouldRunDigaOpponentSceneAfterPlayerSceneStep())
                {
                    BeginOpponentSceneSetupRoutine(true);
                    yield break;
                }

                phaseManager.NextPhase();
                EnterCurrentPhase();
            }
        }

        bool ShouldRunDigaOpponentSceneAfterPlayerSceneStep()
        {
            if (currentTestMode != UcgTestMode.UltramanTest) return false;
            if (turnManager == null || turnManager.currentTurn != 3) return false;
            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.SceneSetup) return false;
            if (!IsCurrentFirstPlayer(UcgPlayerSide.Player)) return false;
            if (opponentScript == null) return false;

            UcgCardData sceneCard = FindOpponentSceneCardInHiddenHand(out _);
            if (sceneCard == null && !HasHydratedOpponentRuntimeDeck())
            {
                sceneCard = opponentScript.GetOpponentSceneCard(currentTestMode, turnManager.currentTurn);
            }

            return sceneCard != null && CanPlaceSceneCard(sceneCard, UcgPlayerSide.Opponent, out _);
        }

        void StopSceneSetupSkipRoutine()
        {
            if (_sceneSetupSkipRoutine != null)
            {
                StopCoroutine(_sceneSetupSkipRoutine);
                _sceneSetupSkipRoutine = null;
            }

            _isAutoPhaseRunning = false;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
        }

        IEnumerator OpponentSetupRoutine(UcgBattleLane lane, bool opponentFirst)
        {
            _isOpponentActionRunning = true;
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            ShowPlayStatus("對手進入角色設置階段");

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            bool placed = false;
            UcgCardData placedCard = null;
            if (lane != null && lane.opponentTopCard == null && turnManager != null)
            {
                UcgCardData deckCard = TakeOpponentSetupCardFromHiddenHand(lane);
                if (deckCard != null)
                {
                    placed = lane.PlaceOpponentCard(deckCard, true);
                    if (placed)
                    {
                        placedCard = deckCard;
                    }
                    if (!placed)
                    {
                        deckManager.opponentHiddenHand.Insert(0, deckCard);
                    }
                }
                else if (!HasHydratedOpponentRuntimeDeck())
                {
                    placed = lane.PlaceScriptedOpponentCardIfEmpty(turnManager.currentTurn);
                    if (placed)
                    {
                        UcgCardView topCard = lane.GetOpponentTopCard();
                        placedCard = topCard != null ? topCard.CardData : null;
                        Debug.LogWarning("Opponent setup fell back to hardcoded script card because no legal hydrated deck card was available.");
                        ConsumeOpponentHandCard();
                    }
                }
            }

            int laneNumber = lane != null ? lane.laneIndex + 1 : 0;
            if (placed)
            {
                PlayOpponentCardActionFeedback(lane, false);
            }
            ShowPlayStatus(placed
                ? $"對手在第 {laneNumber} 路設置角色：{GetCardDisplayName(placedCard)}"
                : "對手沒有可設置角色");

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            _opponentActionRoutine = null;
            _isOpponentActionRunning = false;
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            if (opponentFirst && turnOrderManager != null)
            {
                turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Player);
                if (placed)
                {
                    ShowPlayStatus("輪到你行動", 1.2f);
                }
            }
            UpdateMainPrompt();
            RefreshZoneInfoUI();

            if (_advanceToUpgradeAfterOpponentSetup
                && phaseManager != null
                && phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                && CanAdvanceFromCharacterSetup())
            {
                _advanceToUpgradeAfterOpponentSetup = false;
                phaseManager.NextPhase();
                EnterCurrentPhase();
                yield break;
            }

            _advanceToUpgradeAfterOpponentSetup = false;
            RefreshInteractionHints();
        }

        void BeginOpponentUpgradeRoutine(bool advanceToOpenAfterDone)
        {
            if (_opponentActionRoutine != null) return;
            if (turnOrderManager != null) turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Opponent);
            _opponentActionRoutine = StartCoroutine(OpponentUpgradeRoutine(advanceToOpenAfterDone));
        }

        IEnumerator OpponentUpgradeRoutine(bool advanceToOpenAfterDone)
        {
            _isOpponentActionRunning = true;
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            ShowPlayStatus("對手進入升級階段");

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            bool upgraded = ExecuteOpponentUpgradeScript();

            if (!upgraded)
            {
                ShowPlayStatus("對手沒有可升級角色");
            }

            yield return new WaitForSecondsRealtime(opponentActionDelaySeconds);

            _opponentActionRoutine = null;
            _isOpponentActionRunning = false;
            if (!advanceToOpenAfterDone && turnOrderManager != null)
            {
                turnOrderManager.SetCurrentActingPlayer(UcgPlayerSide.Player);
            }
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);

            if (advanceToOpenAfterDone && phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                phaseManager.NextPhase();
                EnterCurrentPhase();
            }
            else
            {
                UpdateMainPrompt();
                RestoreCompactPlayStatus();
                RefreshInteractionHints();
            }
        }

        bool ExecuteOpponentUpgradeScript()
        {
            if (battlefieldManager == null || turnManager == null) return false;
            if (_opponentUpgradeExecutedTurn == turnManager.currentTurn) return false;

            _opponentUpgradeExecutedTurn = turnManager.currentTurn;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            bool upgraded = false;
            int upgradedLaneNumber = 0;
            UcgCardData upgradedCard = null;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                UcgCardData deckUpgradeCard = TakeOpponentUpgradeCardFromHiddenHand(lane);
                if (deckUpgradeCard != null)
                {
                    if (lane.UpgradeOpponentCard(deckUpgradeCard, true))
                    {
                        upgraded = true;
                        upgradedLaneNumber = lane.laneIndex + 1;
                        upgradedCard = deckUpgradeCard;
                        PlayOpponentCardActionFeedback(lane, true);
                        break;
                    }

                    deckManager.opponentHiddenHand.Insert(0, deckUpgradeCard);
                }

                if (!upgraded && !HasHydratedOpponentRuntimeDeck() && lane.TryScriptedOpponentUpgrade(turnManager.currentTurn))
                {
                    upgraded = true;
                    upgradedLaneNumber = lane.laneIndex + 1;
                    UcgCardView topCard = lane.GetOpponentTopCard();
                    upgradedCard = topCard != null ? topCard.CardData : null;
                    PlayOpponentCardActionFeedback(lane, true);
                    Debug.LogWarning("Opponent upgrade fell back to hardcoded script card because no legal hydrated deck card was available.");
                    ConsumeOpponentHandCard();
                    break;
                }
            }

            ShowPlayStatus(upgraded
                ? $"對手升級了第 {upgradedLaneNumber} 路角色：{GetCardDisplayName(upgradedCard)}"
                : "對手沒有可升級角色");

            return upgraded;
        }

        void PlayOpponentCardActionFeedback(UcgBattleLane lane, bool isUpgrade)
        {
            UcgCardView topCard = lane != null ? lane.GetOpponentTopCard() : null;
            if (topCard != null)
            {
                topCard.PlayBoardActionFeedback(isUpgrade);
            }

            if (sfxController == null) return;

            if (isUpgrade)
            {
                sfxController.PlayUpgrade();
            }
            else
            {
                sfxController.PlayCardPlace();
            }
        }

        string GetCardDisplayName(UcgCardData card)
        {
            if (card == null) return "未知卡牌";
            if (!string.IsNullOrWhiteSpace(card.cardName)) return card.cardName;
            if (!string.IsNullOrWhiteSpace(card.characterName)) return card.characterName;
            if (!string.IsNullOrWhiteSpace(card.id)) return card.id;
            return "未知卡牌";
        }

        void ConsumeOpponentHandCard()
        {
            if (deckManager != null && deckManager.opponentHiddenHand.Count > 0)
            {
                int removeIndex = 0;
                for (int i = 0; i < deckManager.opponentHiddenHand.Count; i++)
                {
                    UcgCardData hiddenCard = deckManager.opponentHiddenHand[i];
                    if (hiddenCard == null || hiddenCard.IsSceneCard()) continue;

                    removeIndex = i;
                    break;
                }

                deckManager.opponentHiddenHand.RemoveAt(removeIndex);
            }

            SyncOpponentZoneCountsFromDeckManager();
            RefreshZoneInfoUI();
        }

        UcgCardData TakeOpponentSetupCardFromHiddenHand(UcgBattleLane lane)
        {
            if (deckManager == null || lane == null || lane.opponentTopCard != null) return null;

            for (int i = 0; i < deckManager.opponentHiddenHand.Count; i++)
            {
                UcgCardData card = deckManager.opponentHiddenHand[i];
                if (card == null || card.IsSceneCard()) continue;
                if (!UcgActionValidator.CanPlayToEmptyArea(card, false, out _)) continue;

                deckManager.opponentHiddenHand.RemoveAt(i);
                if (debugOpponentRuntime) Debug.Log($"Opponent lane card from deck: lane={lane.laneIndex + 1}, {FormatOpponentRuntimeCard(card)}");
                SyncOpponentZoneCountsFromDeckManager();
                RefreshZoneInfoUI();
                return card;
            }

            Debug.LogWarning($"Opponent hidden hand has no legal setup card for lane {lane.laneIndex + 1}.");
            return null;
        }

        UcgCardData TakeOpponentUpgradeCardFromHiddenHand(UcgBattleLane lane)
        {
            if (deckManager == null || lane == null) return null;

            UcgCardView topCardView = lane.GetOpponentTopCard();
            UcgCardData topCard = topCardView != null ? topCardView.CardData : null;
            if (topCard == null) return null;

            for (int i = 0; i < deckManager.opponentHiddenHand.Count; i++)
            {
                UcgCardData card = deckManager.opponentHiddenHand[i];
                if (card == null || card.IsSceneCard()) continue;
                if (!UcgActionValidator.CanPlayOrUpgrade(card, topCard, out _, out UcgPlayActionType actionType)) continue;
                if (actionType != UcgPlayActionType.Upgrade) continue;

                deckManager.opponentHiddenHand.RemoveAt(i);
                if (debugOpponentRuntime) Debug.Log($"Opponent upgrade card from deck: lane={lane.laneIndex + 1}, {FormatOpponentRuntimeCard(card)}");
                SyncOpponentZoneCountsFromDeckManager();
                RefreshZoneInfoUI();
                return card;
            }

            return null;
        }

        UcgCardData FindOpponentSceneCardInHiddenHand(out int cardIndex)
        {
            cardIndex = -1;
            if (deckManager == null) return null;

            for (int i = 0; i < deckManager.opponentHiddenHand.Count; i++)
            {
                UcgCardData card = deckManager.opponentHiddenHand[i];
                if (card == null || !card.IsSceneCard()) continue;
                if (!CanPlaceSceneCard(card, UcgPlayerSide.Opponent, out _)) continue;

                cardIndex = i;
                if (debugOpponentRuntime) Debug.Log($"Opponent scene candidate from deck: {FormatOpponentRuntimeCard(card)}");
                return card;
            }

            return null;
        }

        void RemoveOpponentHiddenCard(UcgCardData card)
        {
            if (deckManager == null || card == null) return;

            for (int i = 0; i < deckManager.opponentHiddenHand.Count; i++)
            {
                if (!ReferenceEquals(deckManager.opponentHiddenHand[i], card)) continue;

                deckManager.opponentHiddenHand.RemoveAt(i);
                if (debugOpponentRuntime) Debug.Log($"Opponent hidden hand consumed scene: {FormatOpponentRuntimeCard(card)}");
                SyncOpponentZoneCountsFromDeckManager();
                RefreshZoneInfoUI();
                return;
            }
        }

        string FormatOpponentRuntimeCard(UcgCardData card)
        {
            if (card == null) return "null";
            return $"id={card.id}, sku={card.sku}, name={card.cardName}, category={card.cardCategory}, Lv.{card.level}, imageLocal={card.imageLocal}, hasSprite={card.cardImage != null}";
        }

        public void RunBattleJudgement()
        {
            if (battlefieldManager == null) return;

            UcgBattleJudge.debugBpBreakdown = debugBpBreakdown;
            int currentTurn = turnManager != null ? turnManager.currentTurn : battlefieldManager.maxLaneCount;
            var lanes = battlefieldManager.GetOpenedLanes(currentTurn);
            ApplySceneBpModifiers(lanes);
            ApplyConditionalBpModifiers(lanes);
            for (int i = 0; i < lanes.Count; i++)
            {
                if (lanes[i] != null)
                {
                    lanes[i].JudgeLane();
                }
            }

            int playerWinCount;
            int opponentWinCount;
            string gameResultMessage;
            UcgGameResultType gameResult = UcgBattleJudge.JudgeGameResult(lanes, out playerWinCount, out opponentWinCount, out gameResultMessage);
            string demoResultMessage = GetDemoGameResultMessage(gameResult, gameResultMessage);
            string nextFirstPlayerMessage = DecideNextFirstPlayerFromCurrentTurn();
            string restedMessage = GetJudgementRestedMessage();
            RecordPlayerTutorialLaneWins(lanes);
            PlayJudgementSfx(lanes);
            _lastPlayerWinCount = playerWinCount;
            _lastOpponentWinCount = opponentWinCount;
            Debug.Log($"Game Result Check: playerWins={playerWinCount}, opponentWins={opponentWinCount}, result={gameResult}");

            if (gameResultText != null)
            {
                SetGameResultHudVisible(true);
                gameResultText.text = $"勝利路數：我方 {playerWinCount}｜對手 {opponentWinCount}";
            }

            if (_playerWonTutorialLaneIndexes.Count >= 3)
            {
                if (playResultText != null)
                {
                    playResultText.text = $"我方已贏得 {_playerWonTutorialLaneIndexes.Count} 條不同戰鬥區。";
                }

                BeginTutorialCompletionRoutine();
                return;
            }

            if (gameResult == UcgGameResultType.PlayerWin || gameResult == UcgGameResultType.OpponentWin)
            {
                SetGameOver(gameResult, playerWinCount, opponentWinCount);
                return;
            }

            StartJudgementVisualAnimation(lanes);
            ShowPlayStatus(BuildJudgementHudMessage(lanes, playerWinCount, opponentWinCount, demoResultMessage, nextFirstPlayerMessage, restedMessage), 2.35f);
        }

        void PlayJudgementSfx(List<UcgBattleLane> lanes)
        {
            if (sfxController == null) return;

            UcgBattleLane latestLane = GetLatestJudgedLane(lanes);
            if (latestLane == null) return;

            switch (latestLane.laneResult)
            {
                case UcgLaneResultType.PlayerWin:
                    sfxController.PlayJudgementWin();
                    break;
                case UcgLaneResultType.OpponentWin:
                    sfxController.PlayJudgementLose();
                    break;
            }
        }

        void StartJudgementVisualAnimation(List<UcgBattleLane> lanes)
        {
            if (_judgementVisualRoutine != null)
            {
                StopCoroutine(_judgementVisualRoutine);
                _judgementVisualRoutine = null;
                _isResolvingVisualAnimation = false;
            }

            _judgementVisualRoutine = StartCoroutine(JudgementVisualRoutine(lanes));
        }

        IEnumerator JudgementVisualRoutine(List<UcgBattleLane> lanes)
        {
            _isResolvingVisualAnimation = true;
            HideAdvanceButton();
            ClearInteractionHints();

            float duration = Mathf.Max(0.1f, judgementResultAnimationSeconds);
            if (lanes != null)
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (lanes[i] != null)
                    {
                        lanes[i].PlayJudgementResultAnimation(duration);
                    }
                }
            }

            yield return new WaitForSecondsRealtime(duration);

            _isResolvingVisualAnimation = false;
            _judgementVisualRoutine = null;
        }

        string BuildJudgementHudMessage(List<UcgBattleLane> lanes, int playerWinCount, int opponentWinCount, string demoResultMessage, string nextFirstPlayerMessage, string restedMessage)
        {
            UcgBattleLane focusLane = GetLatestJudgedLane(lanes);
            if (focusLane == null)
            {
                return $"判定完成\n勝利路數：我方 {playerWinCount}｜對手 {opponentWinCount}";
            }

            string resultText = GetLaneResultShortText(focusLane.laneResult);

            return $"第 {focusLane.laneIndex + 1} 路判定｜結果：{resultText}\n"
                + $"我方：{FormatBpFormula(focusLane.playerBp, GetPlayerModifierTotal(focusLane))}\n"
                + $"對手：{FormatBpFormula(focusLane.opponentBp, GetOpponentModifierTotal(focusLane))}";
        }

        UcgBattleLane GetLatestJudgedLane(List<UcgBattleLane> lanes)
        {
            if (battlefieldManager != null && turnManager != null)
            {
                UcgBattleLane lane = battlefieldManager.GetLane(turnManager.currentTurn - 1);
                if (lane != null) return lane;
            }

            if (lanes == null) return null;
            for (int i = lanes.Count - 1; i >= 0; i--)
            {
                if (lanes[i] != null) return lanes[i];
            }

            return null;
        }

        string FormatBpFormula(int finalBp, int totalModifier)
        {
            int baseBp = Mathf.Max(0, finalBp - totalModifier);
            if (totalModifier == 0) return $"{baseBp} = {finalBp}";

            string sign = totalModifier > 0 ? "+" : "";
            return $"{baseBp} {sign}{totalModifier} = {finalBp}";
        }

        int GetPlayerModifierTotal(UcgBattleLane lane)
        {
            if (lane == null) return 0;
            return lane.playerTemporaryBpModifier + lane.playerSceneBpModifier + lane.playerConditionalBpModifier;
        }

        int GetOpponentModifierTotal(UcgBattleLane lane)
        {
            if (lane == null) return 0;
            return lane.opponentTemporaryBpModifier + lane.opponentSceneBpModifier + lane.opponentConditionalBpModifier;
        }

        bool HasAnyBpModifier(UcgBattleLane lane)
        {
            if (lane == null) return false;
            return GetPlayerModifierTotal(lane) != 0 || GetOpponentModifierTotal(lane) != 0;
        }

        string GetLaneResultShortText(UcgLaneResultType result)
        {
            switch (result)
            {
                case UcgLaneResultType.PlayerWin:
                    return "我方勝";
                case UcgLaneResultType.OpponentWin:
                    return "對手勝";
                case UcgLaneResultType.Draw:
                    return "平手";
                default:
                    return "無結果";
            }
        }

        void RecordPlayerTutorialLaneWins(List<UcgBattleLane> lanes)
        {
            if (lanes == null) return;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null || lane.laneResult != UcgLaneResultType.PlayerWin) continue;
                _playerWonTutorialLaneIndexes.Add(lane.laneIndex);
            }
        }

        string GetJudgementRestedMessage()
        {
            if (battlefieldManager == null || turnManager == null) return "";

            UcgBattleLane latestLane = battlefieldManager.GetLane(turnManager.currentTurn - 1);
            if (latestLane == null) return "";

            switch (latestLane.laneResult)
            {
                case UcgLaneResultType.PlayerWin:
                    return "對方這一路 BP 較低，角色橫置。";
                case UcgLaneResultType.OpponentWin:
                    return "我方這一路 BP 較低，角色橫置。";
                case UcgLaneResultType.Draw:
                    return "這一路 BP 相同，雙方都不橫置。";
                default:
                    return "這一路尚未產生有效判定。";
            }
        }

        void SetGameOver(UcgGameResultType gameResult, int playerWinCount, int opponentWinCount)
        {
            IsGameOver = true;
            CurrentGameResult = gameResult;
            _lastPlayerWinCount = playerWinCount;
            _lastOpponentWinCount = opponentWinCount;
            StopOpponentActionRoutine();
            StopEffectAutoAdvanceRoutine();
            StopCurrentEffectSourceHighlight();
            ClearPendingActionState();
            if (effectManager != null)
            {
                effectManager.Clear();
            }
            ClearEffectTargetSelection();
            ClearInteractionHints();
            HideDiscardPilePanel();

            string winnerText = gameResult == UcgGameResultType.PlayerWin ? "我方勝利" : "對手勝利";
            string message = $"遊戲結束｜{winnerText}\n我方勝利路數：{playerWinCount}｜對手勝利路數：{opponentWinCount}";

            if (playResultText != null)
            {
                playResultText.text = message;
            }

            if (gameResultText != null)
            {
                gameResultText.text = message;
            }

            ShowGameOverModal(gameResult, playerWinCount, opponentWinCount);
            UpdateMainPrompt();
        }

        void ShowGameOverModal(UcgGameResultType gameResult, int playerWinCount, int opponentWinCount)
        {
            if (_gameOverModalRoot == null)
            {
                EnsureGameOverModal();
            }

            string winnerText = gameResult == UcgGameResultType.PlayerWin ? "我方勝利" : "對手勝利";
            if (_gameOverModalText != null)
            {
                _gameOverModalText.text =
                    $"遊戲結束\n\n{winnerText}\n\n我方勝利路數：{playerWinCount}\n對手勝利路數：{opponentWinCount}";
            }

            if (_gameOverModalRoot != null)
            {
                _gameOverModalRoot.gameObject.SetActive(true);
                _gameOverModalRoot.SetAsLastSibling();
                var modalCanvas = _gameOverModalRoot.GetComponent<Canvas>();
                if (modalCanvas != null)
                {
                    modalCanvas.overrideSorting = true;
                    modalCanvas.sortingOrder = 21000;
                }
            }
        }

        void HideGameOverModal()
        {
            if (_gameOverModalRoot != null)
            {
                _gameOverModalRoot.gameObject.SetActive(false);
            }
        }

        void ResetGameOverState()
        {
            IsGameOver = false;
            CurrentGameResult = UcgGameResultType.None;
            _lastPlayerWinCount = 0;
            _lastOpponentWinCount = 0;
            SetNextPhaseButtonInteractable(true);
            HideGameOverModal();
        }

        void ResetEffectState()
        {
            StopEffectAutoAdvanceRoutine();
            StopDeckOperationNoValidAutoCloseRoutine();
            StopCurrentEffectSourceHighlight();
            if (effectManager != null)
            {
                effectManager.Clear();
            }

            ClearEffectTargetSelection();
            ClearBp05008DiscardSelectionState();
            ClearBp01105PendingSelectionState();
            ClearBp01043PendingReorderState();
            ClearActivatedEffectSourceHighlights();
            _bp01043RevealReorderHandledEffectKeys.Clear();
            _temporarySceneSummons.Clear();
            ClearTemporaryBpModifiers();
            ClearTemporaryTypeGrants();
            ClearSceneBpModifiers();
        }

        void RestoreRestedCardsForNewTurn()
        {
            if (battlefieldManager != null)
            {
                battlefieldManager.RestoreRestedCards();
            }
        }

        void ClearTemporaryBpModifiers()
        {
            if (battlefieldManager != null)
            {
                battlefieldManager.ClearTemporaryBpModifiers();
            }
        }

        void ClearTemporaryTypeGrants()
        {
            if (_temporaryTypeGrants.Count == 0) return;

            if (debugEffectResolution)
            {
                Debug.Log($"Temporary type grants cleared: count={_temporaryTypeGrants.Count}");
            }

            _temporaryTypeGrants.Clear();
        }

        void ClearSceneBpModifiers()
        {
            if (battlefieldManager != null)
            {
                battlefieldManager.ClearSceneBpModifiers();
            }
        }

        void ClearSceneSlots()
        {
            if (sharedSceneSlot != null) sharedSceneSlot.ClearSceneCard();

            ClearSceneHighlights();
        }

        public void NotifyCardDragStarted(UcgCardView cardView)
        {
            if (_pendingAction != null)
            {
                ShowPendingActionMessage();
                return;
            }

            if (cardView == null)
            {
                ClearInteractionHints();
                return;
            }

            if (!CanPlayerDragHandCard(cardView, out string reason))
            {
                ClearInteractionHints();
                if (playResultText != null)
                {
                    playResultText.text = GetInteractionLockMessage(reason);
                }
                return;
            }

            _isPlayerDraggingHandCard = true;
            PauseAdvanceCountdownForDrag();
            ShowDragHints(cardView.CardData);
        }

        public void NotifyCardDragEnded()
        {
            _isPlayerDraggingHandCard = false;
            if (_pendingAction != null) return;
            ResetAdvancePromptCountdownForNewDecision();
            RefreshInteractionHints();
            RefreshNextPhaseButtonState();
        }

        public void BeginPendingBattlefieldAction(
            UcgCardView cardView,
            UIDragCard dragCard,
            RectTransform cardRect,
            UcgBattleLane lane,
            UcgPlayActionType actionType,
            string successMessage)
        {
            if (cardView == null || cardRect == null || lane == null)
            {
                return;
            }

            var pendingType = actionType == UcgPlayActionType.Upgrade
                ? UcgPendingActionType.CharacterUpgrade
                : UcgPendingActionType.CharacterSetup;
            string actionText = pendingType == UcgPendingActionType.CharacterUpgrade ? "升級" : "設置到";
            int laneNumber = lane.laneIndex + 1;
            _isPlayerDraggingHandCard = false;
            ResetAdvancePromptCountdownForNewDecision();
            HideAdvanceButton();

            _pendingAction = new UcgPendingAction
            {
                actionType = pendingType,
                cardData = cardView.CardData,
                cardView = cardView,
                dragCard = dragCard,
                targetLane = lane,
                targetSide = UcgPlayerSide.Player,
                playActionType = actionType,
                previousParent = cardHolder,
                previousSiblingIndex = cardHolder != null ? cardHolder.childCount : 0,
                confirmMessage = $"確定{actionText}第 {laneNumber} 路嗎？",
                successMessage = successMessage
            };

            ShowPendingConfirm(_pendingAction.confirmMessage);
            SetHandCardsInteractable(false, cardView);
        }

        void BeginPendingSceneAction(UcgCardView cardView, UIDragCard dragCard, RectTransform cardRect)
        {
            if (cardView == null || cardRect == null || sharedSceneSlot == null) return;

            UcgCardData previousSceneCard = sharedSceneSlot.SceneCardData;
            UcgPlayerSide previousSceneOwner = sharedSceneSlot.SceneOwner;
            bool replacing = previousSceneCard != null;
            UcgCardData sceneData = cardView.CardData;
            _isPlayerDraggingHandCard = false;
            ResetAdvancePromptCountdownForNewDecision();
            HideAdvanceButton();

            _pendingAction = new UcgPendingAction
            {
                actionType = UcgPendingActionType.SceneSetup,
                cardData = sceneData,
                cardView = cardView,
                dragCard = dragCard,
                targetSide = UcgPlayerSide.Player,
                previousParent = cardHolder,
                previousSiblingIndex = cardHolder != null ? cardHolder.childCount : 0,
                previousSceneCard = previousSceneCard,
                previousSceneOwner = previousSceneOwner,
                confirmMessage = replacing ? "確定替換目前場景卡嗎？" : "確定設置場景卡嗎？"
            };

            cardRect.SetParent(cardHolder, false);
            cardView.gameObject.SetActive(false);
            sharedSceneSlot.PreviewSceneCard(sceneData, UcgPlayerSide.Player);
            ShowPendingConfirm(_pendingAction.confirmMessage);
            SetHandCardsInteractable(false, cardView);
        }

        void ShowPendingConfirm(string message)
        {
            EnsurePendingConfirmDialog();
            if (_pendingConfirmRoot == null) return;

            if (_pendingConfirmText != null)
            {
                _pendingConfirmText.text = string.IsNullOrWhiteSpace(message) ? "確定嗎？" : message;
            }

            var modalCanvas = _pendingConfirmRoot.GetComponent<Canvas>();
            if (modalCanvas != null)
            {
                modalCanvas.overrideSorting = true;
                modalCanvas.sortingOrder = 20000;
            }

            _pendingConfirmRoot.SetAsLastSibling();
            _pendingConfirmRoot.gameObject.SetActive(true);
        }

        void HidePendingConfirm()
        {
            if (_pendingConfirmRoot != null)
            {
                _pendingConfirmRoot.gameObject.SetActive(false);
            }
        }

        void ShowPendingActionMessage()
        {
            if (playResultText != null)
            {
                playResultText.text = "請先確認或取消目前動作";
            }
        }

        public void ConfirmPendingAction()
        {
            if (_pendingAction == null) return;

            UcgPendingAction pending = _pendingAction;
            _pendingAction = null;
            ResetAdvancePromptCountdownForNewDecision();
            HidePendingConfirm();
            SetHandCardsInteractable(true, null);

            if (pending.actionType == UcgPendingActionType.SceneSetup)
            {
                CommitPendingSceneAction(pending);
                return;
            }

            CommitPendingBattlefieldAction(pending);
        }

        public void CancelPendingAction()
        {
            if (_pendingAction == null) return;

            UcgPendingAction pending = _pendingAction;
            _pendingAction = null;
            ResetAdvancePromptCountdownForNewDecision();
            HidePendingConfirm();
            SetHandCardsInteractable(true, null);

            if (pending.actionType == UcgPendingActionType.SceneSetup)
            {
                CancelPendingSceneAction(pending);
            }
            else
            {
                CancelPendingBattlefieldAction(pending);
            }

            RefreshInteractionHints();
        }

        void ClearPendingActionState()
        {
            _pendingAction = null;
            ResetAdvancePromptCountdownForNewDecision();
            HidePendingConfirm();
            SetHandCardsInteractable(true, null);
            RefreshNextPhaseButtonState();
        }

        void CommitPendingBattlefieldAction(UcgPendingAction pending)
        {
            if (pending == null || pending.targetLane == null) return;

            if (!ValidatePlayerCardDrop(
                    pending.cardData,
                    pending.targetLane,
                    UcgPlayerCardDropTarget.Lane,
                    out string validationMessage,
                    out UcgPlayActionType validatedAction,
                    true,
                    pending.cardView)
                || validatedAction != pending.playActionType)
            {
                CancelPendingBattlefieldAction(pending, string.IsNullOrWhiteSpace(validationMessage)
                    ? "現在不能放這張牌"
                    : validationMessage);
                return;
            }

            if (deckManager != null)
            {
                deckManager.RemoveFromPlayerHand(pending.cardData);
            }

            string message = pending.successMessage;
            if (pending.playActionType == UcgPlayActionType.Upgrade && turnManager != null)
            {
                turnManager.MarkLaneUpgraded(pending.targetLane.laneIndex);
            }

            if (playResultText != null)
            {
                playResultText.text = message;
            }

            StartPlayerBattlefieldCommitAnimation(pending, message);
        }

        void StartPlayerBattlefieldCommitAnimation(UcgPendingAction pending, string message)
        {
            if (_battlefieldCommitAnimationRoutine != null)
            {
                StopCoroutine(_battlefieldCommitAnimationRoutine);
                _battlefieldCommitAnimationRoutine = null;
                _isResolvingVisualAnimation = false;
            }

            _battlefieldCommitAnimationRoutine = StartCoroutine(PlayerBattlefieldCommitRoutine(pending, message));
        }

        IEnumerator PlayerBattlefieldCommitRoutine(UcgPendingAction pending, string message)
        {
            _isResolvingVisualAnimation = true;
            HideAdvanceButton();
            ClearInteractionHints();
            SetHandCardsInteractable(false, pending != null ? pending.cardView : null);

            if (pending != null)
            {
                yield return PlayBattlefieldCardCommitAnimation(pending.cardView, pending.playActionType);
            }

            _isResolvingVisualAnimation = false;
            _battlefieldCommitAnimationRoutine = null;
            SetHandCardsInteractable(true, null);

            if (pending == null)
            {
                RefreshInteractionHints();
                RefreshNextPhaseButtonState();
                yield break;
            }

            if (pending.targetLane != null)
            {
                pending.targetLane.RefreshPlayerStateFromPlayArea();
            }

            if (pending.playActionType == UcgPlayActionType.PlayToEmptyArea)
            {
                ContinueAfterPlayerBattlefieldCommit(pending.targetLane, pending.playActionType, ref message);
                if (playResultText != null)
                {
                    playResultText.text = message;
                }
            }

            NotifyPlayerBattlefieldCardPlaced();
            if (tutorialGuide != null)
            {
                tutorialGuide.NotifyCardPlayed(pending.cardData, pending.playActionType);
            }

            RefreshInteractionHints();
            RefreshNextPhaseButtonState();
        }

        IEnumerator PlayBattlefieldCardCommitAnimation(UcgCardView cardView, UcgPlayActionType actionType)
        {
            if (cardView == null) yield break;

            RectTransform cardRect = cardView.transform as RectTransform;
            if (cardRect == null) yield break;

            float duration = actionType == UcgPlayActionType.Upgrade
                ? Mathf.Max(0.05f, characterUpgradeAnimationSeconds)
                : Mathf.Max(0.05f, characterPlayAnimationSeconds);
            Vector2 endPosition = cardRect.anchoredPosition;
            Vector2 startOffset = actionType == UcgPlayActionType.Upgrade
                ? new Vector2(-38f, -124f)
                : new Vector2(0f, -92f);
            Vector2 startPosition = endPosition + startOffset;
            Vector3 endScale = cardRect.localScale;
            Vector3 startScale = endScale * 0.9f;

            CanvasGroup canvasGroup = cardRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = cardRect.gameObject.AddComponent<CanvasGroup>();
            float endAlpha = 1f;

            Outline glow = cardRect.GetComponent<Outline>();
            if (glow == null) glow = cardRect.gameObject.AddComponent<Outline>();
            bool glowWasEnabled = glow.enabled;
            Color glowColor = glow.effectColor;
            Vector2 glowDistance = glow.effectDistance;
            glow.enabled = true;
            glow.effectColor = actionType == UcgPlayActionType.Upgrade
                ? new Color(1f, 0.82f, 0.32f, 0.9f)
                : new Color(0.38f, 0.94f, 1f, 0.82f);
            glow.effectDistance = actionType == UcgPlayActionType.Upgrade
                ? new Vector2(6f, -6f)
                : new Vector2(4f, -4f);

            cardRect.anchoredPosition = startPosition;
            cardRect.localScale = startScale;
            canvasGroup.alpha = 0.35f;
            canvasGroup.blocksRaycasts = false;
            cardRect.SetAsLastSibling();

            float elapsed = 0f;
            while (elapsed < duration && cardRect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float bump = Mathf.Sin(t * Mathf.PI) * (actionType == UcgPlayActionType.Upgrade ? 0.075f : 0.035f);

                cardRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                cardRect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased) + endScale * bump;
                canvasGroup.alpha = Mathf.Lerp(0.35f, endAlpha, eased);
                Color color = glow.effectColor;
                color.a = Mathf.Lerp(0.9f, 0.15f, t);
                glow.effectColor = color;
                yield return null;
            }

            if (cardRect != null)
            {
                cardRect.anchoredPosition = endPosition;
                cardRect.localScale = endScale;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = endAlpha;
                canvasGroup.blocksRaycasts = true;
            }

            if (glow != null)
            {
                glow.effectColor = glowColor;
                glow.effectDistance = glowDistance;
                glow.enabled = glowWasEnabled;
            }

            cardView.PlayBoardActionFeedback(actionType == UcgPlayActionType.Upgrade);
            if (sfxController != null)
            {
                if (actionType == UcgPlayActionType.Upgrade)
                {
                    sfxController.PlayUpgrade();
                }
                else
                {
                    sfxController.PlayCardPlace();
                }
            }

            Transform cardParent = cardRect != null ? cardRect.parent : null;
            UcgPlayArea playArea = cardParent != null ? cardParent.GetComponent<UcgPlayArea>() : null;
            if (playArea != null)
            {
                playArea.PlaySuccessfulActionFeedback(actionType == UcgPlayActionType.Upgrade);
            }
        }

        void ContinueAfterPlayerBattlefieldCommit(UcgBattleLane lane, UcgPlayActionType actionType, ref string message)
        {
            if (actionType != UcgPlayActionType.PlayToEmptyArea) return;

            if (IsCurrentFirstPlayer(UcgPlayerSide.Player) && lane != null && lane.opponentTopCard == null)
            {
                _advanceToUpgradeAfterOpponentSetup = true;
                RequestOpponentSetupAfterPlayerPlacement(lane);
                message = $"{message}；對手思考中...";
            }
            else
            {
                TryAutoAdvanceCharacterSetupAfterConfirmed();
            }
        }

        void TryAutoAdvanceCharacterSetupAfterConfirmed()
        {
            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.CharacterSetup) return;
            if (!CanAdvanceFromCharacterSetup()) return;

            phaseManager.NextPhase();
            EnterCurrentPhase();
        }

        void CommitPendingSceneAction(UcgPendingAction pending)
        {
            if (pending == null || pending.cardData == null || sharedSceneSlot == null) return;

            if (!ValidatePlayerCardDrop(
                    pending.cardData,
                    null,
                    UcgPlayerCardDropTarget.SceneSlot,
                    out string validationMessage,
                    out _,
                    true))
            {
                CancelPendingSceneAction(pending, string.IsNullOrWhiteSpace(validationMessage)
                    ? "現在不能設置場景卡"
                    : validationMessage);
                return;
            }

            bool replaced = pending.previousSceneCard != null;
            if (replaced)
            {
                AddSceneToDiscard(pending.previousSceneCard, pending.previousSceneOwner);
            }

            if (deckManager != null)
            {
                deckManager.RemoveFromPlayerHand(pending.cardData);
            }

            sharedSceneSlot.PlaceSceneCardFromScript(pending.cardData, UcgPlayerSide.Player);
            LogScenePlacement(pending.cardData, UcgPlayerSide.Player, replaced, pending.previousSceneCard, pending.previousSceneOwner);
            _sceneCardPlacedTurn = turnManager != null ? turnManager.currentTurn : 1;
            string drawMessage = DrawOneForSceneOwner(UcgPlayerSide.Player);
            string enterMessage = ResolveSceneEnterEffect(pending.cardData, UcgPlayerSide.Player);
            string message = BuildScenePlacementMessage(UcgPlayerSide.Player, replaced, pending.previousSceneCard, pending.previousSceneOwner, drawMessage, enterMessage);

            if (pending.cardView != null)
            {
                pending.cardView.transform.SetParent(null, false);
                Destroy(pending.cardView.gameObject);
            }

            if (playResultText != null)
            {
                playResultText.text = message;
            }

            RefreshHandLayout();
            RefreshInteractionHints();
            RefreshZoneInfoUI();

            if (IsDigaTutorialModeActive()
                && IsDigaTutorialTargetSceneCard(pending.cardData)
                && (turnManager == null || turnManager.currentTurn >= 3))
            {
                StopTutorialGuidanceAfterSceneSetup();
            }

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                phaseManager.NextPhase();
                EnterCurrentPhase();
            }
        }

        void CancelPendingBattlefieldAction(UcgPendingAction pending, string messageOverride = "")
        {
            if (pending == null || pending.cardView == null) return;

            UcgPlayArea playArea = pending.targetLane != null ? pending.targetLane.GetPlayerPlayArea() : null;
            if (playArea != null)
            {
                playArea.RemovePendingCard(pending.cardView);
            }

            ReturnCardViewToHand(pending.cardView, pending.previousSiblingIndex);
            if (playArea != null)
            {
                playArea.GetTopCard();
            }

            if (pending.targetLane != null)
            {
                pending.targetLane.RefreshPlayerStateFromPlayArea();
            }

            if (playResultText != null)
            {
                playResultText.text = string.IsNullOrWhiteSpace(messageOverride)
                    ? "已取消本次操作"
                    : messageOverride;
            }

            RefreshNextPhaseButtonState();
        }

        void CancelPendingSceneAction(UcgPendingAction pending, string messageOverride = "")
        {
            if (pending == null) return;

            if (pending.previousSceneCard != null)
            {
                sharedSceneSlot.PlaceSceneCardFromScript(pending.previousSceneCard, pending.previousSceneOwner, false);
            }
            else if (sharedSceneSlot != null)
            {
                sharedSceneSlot.ClearSceneCard();
            }

            if (pending.cardView != null)
            {
                ReturnCardViewToHand(pending.cardView, pending.previousSiblingIndex);
            }

            if (playResultText != null)
            {
                playResultText.text = string.IsNullOrWhiteSpace(messageOverride)
                    ? "已取消場景卡設置"
                    : messageOverride;
            }

            RefreshNextPhaseButtonState();
        }

        void ReturnCardViewToHand(UcgCardView cardView, int siblingIndex)
        {
            if (cardView == null || cardHolder == null) return;

            RectTransform cardRect = cardView.transform as RectTransform;
            cardView.gameObject.SetActive(true);
            cardView.SetSelected(false);
            cardView.SetDragging(false);
            cardView.SetBattlefieldLocked(false);
            cardView.SetFaceDown(false);
            cardView.SetPlayableHighlight(false);
            var clickTarget = cardView.GetComponent<UcgLaneClickTarget>();
            if (clickTarget != null)
            {
                Destroy(clickTarget);
            }

            cardRect.SetParent(cardHolder, false);
            int clampedSiblingIndex = Mathf.Clamp(siblingIndex, 0, cardHolder.childCount - 1);
            cardRect.SetSiblingIndex(clampedSiblingIndex);
            cardRect.localScale = Vector3.one;
            cardRect.localEulerAngles = Vector3.zero;

            var canvasGroup = cardRect.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            var cardCanvas = cardRect.GetComponent<Canvas>();
            if (cardCanvas != null)
            {
                cardCanvas.overrideSorting = false;
                cardCanvas.sortingOrder = 0;
            }

            RefreshHandLayout();
            RefreshZoneInfoUI();
        }

        void SetHandCardsInteractable(bool interactable, UcgCardView exceptCard)
        {
            if (cardHolder == null) return;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var card = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (card == null || card == exceptCard) continue;

                var drag = card.GetComponent<UIDragCard>();
                if (drag != null)
                {
                    bool canDrag = interactable && CanPlayerDragHandCard(card, out _);
                    drag.enabled = canDrag;
                }
            }
        }

        void RefreshHandCardDragInteractability()
        {
            if (cardHolder == null) return;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var card = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (card == null) continue;

                var drag = card.GetComponent<UIDragCard>();
                if (drag != null)
                {
                    drag.enabled = CanPlayerDragHandCard(card, out _);
                }
            }
        }

        public bool CanDropCardOnLane(UcgCardData cardData, UcgBattleLane lane, out string message)
        {
            return ValidatePlayerCardDrop(
                cardData,
                lane,
                UcgPlayerCardDropTarget.Lane,
                out message,
                out _,
                false);
        }

        public bool ValidatePlayerCardDrop(
            UcgCardView cardView,
            UcgBattleLane targetLane,
            UcgPlayerCardDropTarget target,
            out string message,
            out UcgPlayActionType actionType,
            bool logResult = false,
            UcgCardView ignoredCardView = null)
        {
            UcgCardData cardData = cardView != null ? cardView.CardData : null;
            return ValidatePlayerCardDrop(
                cardData,
                targetLane,
                target,
                out message,
                out actionType,
                logResult,
                ignoredCardView);
        }

        public bool ValidatePlayerCardDrop(
            UcgCardData cardData,
            UcgBattleLane targetLane,
            UcgPlayerCardDropTarget target,
            out string message,
            out UcgPlayActionType actionType,
            bool logResult = false,
            UcgCardView ignoredCardView = null)
        {
            actionType = UcgPlayActionType.Reject;
            message = "";

            if (!ValidatePlayerCardDropOperationState(out message))
            {
                LogDropValidation(false, cardData, targetLane, target, actionType, message, logResult);
                return false;
            }

            if (cardData == null)
            {
                message = "不是有效卡牌";
                LogDropValidation(false, cardData, targetLane, target, actionType, message, logResult);
                return false;
            }

            if (deckManager != null && !deckManager.playerHand.Contains(cardData))
            {
                message = "這張牌不在手牌中";
                LogDropValidation(false, cardData, targetLane, target, actionType, message, logResult);
                return false;
            }

            bool accepted = target == UcgPlayerCardDropTarget.SceneSlot
                ? ValidatePlayerSceneDrop(cardData, out message)
                : ValidatePlayerLaneDrop(cardData, targetLane, ignoredCardView, out message, out actionType);

            LogDropValidation(accepted, cardData, targetLane, target, actionType, message, logResult);
            return accepted;
        }

        bool ValidatePlayerCardDropOperationState(out string message)
        {
            if (!CanPlayerInteract(out string reason))
            {
                message = GetInteractionLockMessage(reason);
                return false;
            }

            message = "";
            return true;
        }

        bool ValidatePlayerSceneDrop(UcgCardData cardData, out string message)
        {
            if (cardData == null)
            {
                message = "不是有效場景卡";
                return false;
            }

            if (!cardData.IsSceneCard())
            {
                message = "角色卡不能設置到場景區";
                return false;
            }

            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.SceneSetup)
            {
                message = "目前不是場景設置階段";
                return false;
            }

            return CanPlaceSceneCard(cardData, UcgPlayerSide.Player, out message);
        }

        bool ValidatePlayerLaneDrop(UcgCardData cardData, UcgBattleLane lane, UcgCardView ignoredCardView, out string message, out UcgPlayActionType actionType)
        {
            actionType = UcgPlayActionType.Reject;

            if (cardData == null)
            {
                message = "不是有效角色卡";
                return false;
            }

            if (cardData.IsSceneCard())
            {
                message = "場景卡不能設置到戰鬥區";
                return false;
            }

            if (lane == null)
            {
                message = "不是有效戰鬥區";
                return false;
            }

            if (battlefieldManager != null && turnManager != null)
            {
                int openedLaneCount = battlefieldManager.GetOpenedLaneCount(turnManager.currentTurn);
                if (lane.laneIndex < 0 || lane.laneIndex >= openedLaneCount)
                {
                    message = "此路尚未開放";
                    return false;
                }
            }

            UcgPlayArea playArea = lane.GetPlayerPlayArea();
            if (playArea == null)
            {
                message = "不是我方可放置位置";
                return false;
            }

            UcgCardView topCard = GetPlayerTopCardForDropValidation(lane, ignoredCardView);
            UcgCardData topCardData = topCard != null ? topCard.CardData : null;

            if (phaseManager == null)
            {
                message = "目前階段尚未初始化";
                return false;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup)
            {
                if (topCard != null)
                {
                    message = "此路已有角色，角色設置階段不能放第二張";
                    return false;
                }

                if (turnManager != null && !turnManager.CanPlaceNewCardInLane(lane.laneIndex, out message))
                {
                    if (lane.laneIndex != turnManager.ActiveNewLaneIndex)
                    {
                        message = "請放到目前可操作的路線";
                    }
                    return false;
                }

                if (!phaseManager.CanPlaceCharacter(out message))
                {
                    return false;
                }

                if (!UcgActionValidator.CanPlayOrUpgrade(cardData, null, out message, out actionType)
                    || actionType != UcgPlayActionType.PlayToEmptyArea)
                {
                    return false;
                }

                return true;
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
            {
                if (topCard == null)
                {
                    message = "不能升級到空 Lane";
                    return false;
                }

                if (!phaseManager.CanUpgrade(out message))
                {
                    return false;
                }

                if (turnManager != null && !turnManager.CanUpgradeLaneThisTurn(lane.laneIndex, out message))
                {
                    return false;
                }

                if (!UcgActionValidator.CanPlayOrUpgrade(cardData, topCardData, out message, out actionType)
                    || actionType != UcgPlayActionType.Upgrade)
                {
                    return false;
                }

                return true;
            }

            message = phaseManager.CurrentPhase == UcgGamePhase.SceneSetup
                ? "目前是場景設置階段，角色卡不能放到戰鬥區"
                : "目前階段不能放置角色卡";
            return false;
        }

        UcgCardView GetPlayerTopCardForDropValidation(UcgBattleLane lane, UcgCardView ignoredCardView)
        {
            UcgPlayArea playArea = lane != null ? lane.GetPlayerPlayArea() : null;
            if (playArea == null) return null;

            UcgCardView topCard = playArea.GetTopCard();
            if (ignoredCardView == null || topCard != ignoredCardView)
            {
                return topCard;
            }

            RectTransform cardSlot = playArea.cardSlot;
            if (cardSlot == null) return null;

            for (int i = cardSlot.childCount - 1; i >= 0; i--)
            {
                var cardView = cardSlot.GetChild(i).GetComponent<UcgCardView>();
                if (cardView != null && cardView != ignoredCardView)
                {
                    return cardView;
                }
            }

            return null;
        }

        void LogDropValidation(bool accepted, UcgCardData cardData, UcgBattleLane lane, UcgPlayerCardDropTarget target, UcgPlayActionType actionType, string reason, bool logResult)
        {
            if (!logResult) return;

            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            string actingPlayerText = turnOrderManager != null ? turnOrderManager.currentActingPlayer.ToString() : "None";
            string targetText = target == UcgPlayerCardDropTarget.SceneSlot
                ? "SharedSceneSlot"
                : lane != null ? $"Lane {lane.laneIndex + 1}" : "Lane <none>";
            string actionText = target == UcgPlayerCardDropTarget.SceneSlot && accepted
                ? "SceneSetup"
                : actionType == UcgPlayActionType.PlayToEmptyArea
                    ? "CharacterSetup"
                    : actionType == UcgPlayActionType.Upgrade
                        ? "Upgrade"
                        : "Reject";

            if (!accepted)
            {
                LogInteractionRejected(
                    target == UcgPlayerCardDropTarget.SceneSlot ? "DropToScene" : "DropToLane",
                    reason,
                    cardData,
                    lane);
            }

            if (!debugDropValidation) return;

            Debug.Log(
                (accepted ? "Drop accepted:" : "Drop rejected:") + "\n"
                + $"card={FormatDrawSource(cardData)}\n"
                + $"target={targetText}\n"
                + $"targetLane={(lane != null ? (lane.laneIndex + 1).ToString() : "none")}\n"
                + $"action={actionText}\n"
                + $"phase={phaseText}\n"
                + $"actingPlayer={actingPlayerText}\n"
                + $"reason={(string.IsNullOrWhiteSpace(reason) ? "none" : reason)}");
        }

        public bool TrySnapDraggedCardToNearestTarget(
            UcgCardView cardView,
            UIDragCard dragCard,
            RectTransform cardRect,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (cardView == null || dragCard == null || cardRect == null || cardView.CardData == null)
            {
                return false;
            }

            if (_isTutorialFinishWaitingForClick || _pendingAction != null || IsGameOver || _isAutoPhaseRunning || _isOpponentActionRunning)
            {
                return false;
            }

            UcgCardData cardData = cardView.CardData;
            if (cardData.IsSceneCard())
            {
                return TrySnapSceneCardToSceneSlot(cardView, dragCard, cardRect, screenPosition, eventCamera);
            }

            return TrySnapBattlefieldCardToLane(cardView, dragCard, cardRect, screenPosition, eventCamera);
        }

        bool TrySnapSceneCardToSceneSlot(
            UcgCardView cardView,
            UIDragCard dragCard,
            RectTransform cardRect,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (sharedSceneSlot == null) return false;
            if (!ValidatePlayerCardDrop(
                    cardView,
                    null,
                    UcgPlayerCardDropTarget.SceneSlot,
                    out _,
                    out _,
                    false))
            {
                return false;
            }

            RectTransform sceneRect = sharedSceneSlot.transform as RectTransform;
            if (!IsAnySnapPointNearSceneRect(sceneRect, cardRect, screenPosition, eventCamera, out _)) return false;

            bool dropped = sharedSceneSlot.TryDropCard(cardView, dragCard, cardRect, out string message);
            if (!dropped)
            {
                ShowSceneDropMessage(message);
            }

            return dropped;
        }

        public void NotifyCardDragMoved(UcgCardView cardView, Vector2 screenPosition, Camera eventCamera)
        {
            if (cardView == null || cardView.CardData == null || sharedSceneSlot == null) return;
            if (!cardView.CardData.IsSceneCard()) return;

            bool validScene = ValidatePlayerCardDrop(
                cardView,
                null,
                UcgPlayerCardDropTarget.SceneSlot,
                out _,
                out _,
                false);
            RectTransform cardRect = cardView.transform as RectTransform;
            RectTransform sceneRect = sharedSceneSlot.transform as RectTransform;
            bool nearScene = validScene
                && IsAnySnapPointNearSceneRect(sceneRect, cardRect, screenPosition, eventCamera, out _);

            sharedSceneSlot.SetDropRaycastEnabled(validScene);
            sharedSceneSlot.SetHighlight(nearScene, false);
        }

        bool TrySnapBattlefieldCardToLane(
            UcgCardView cardView,
            UIDragCard dragCard,
            RectTransform cardRect,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (battlefieldManager == null || turnManager == null) return false;

            UcgPlayArea bestPlayArea = null;
            float bestDistance = float.MaxValue;
            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;
                if (!CanDropCardOnLane(cardView.CardData, lane, out _)) continue;

                UcgPlayArea playArea = lane.GetPlayerPlayArea();
                RectTransform playRect = playArea != null ? playArea.transform as RectTransform : null;
                if (!IsAnySnapPointNearDropRect(playRect, cardRect, screenPosition, eventCamera, out float distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPlayArea = playArea;
                }
            }

            if (bestPlayArea == null) return false;

            bool dropped = bestPlayArea.TryDropCard(cardView, dragCard, cardRect, out string message);
            if (!dropped)
            {
                bestPlayArea.ShowResult(message);
            }

            return dropped;
        }

        bool IsAnySnapPointNearDropRect(
            RectTransform targetRect,
            RectTransform draggedCardRect,
            Vector2 pointerScreenPosition,
            Camera eventCamera,
            out float distanceToCenter)
        {
            bool pointerInside = IsScreenPointNearDropRect(targetRect, pointerScreenPosition, eventCamera, out float pointerDistance);
            bool centerInside = false;
            float centerDistance = float.MaxValue;

            if (draggedCardRect != null)
            {
                Vector2 cardCenter = RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    draggedCardRect.TransformPoint(draggedCardRect.rect.center));
                centerInside = IsScreenPointNearDropRect(targetRect, cardCenter, eventCamera, out centerDistance);
            }

            distanceToCenter = Mathf.Min(pointerDistance, centerDistance);
            return pointerInside || centerInside;
        }

        bool IsAnySnapPointNearSceneRect(
            RectTransform targetRect,
            RectTransform draggedCardRect,
            Vector2 pointerScreenPosition,
            Camera eventCamera,
            out float distanceToCenter)
        {
            bool pointerInside = IsScreenPointNearDropRect(
                targetRect,
                pointerScreenPosition,
                eventCamera,
                out float pointerDistance,
                0.18f,
                44f,
                460f,
                220f);
            bool centerInside = false;
            float centerDistance = float.MaxValue;

            if (draggedCardRect != null)
            {
                Vector2 cardCenter = RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    draggedCardRect.TransformPoint(draggedCardRect.rect.center));
                centerInside = IsScreenPointNearDropRect(
                    targetRect,
                    cardCenter,
                    eventCamera,
                    out centerDistance,
                    0.18f,
                    44f,
                    460f,
                    220f);
            }

            distanceToCenter = Mathf.Min(pointerDistance, centerDistance);
            return pointerInside || centerInside;
        }

        bool IsScreenPointNearDropRect(
            RectTransform targetRect,
            Vector2 screenPosition,
            Camera eventCamera,
            out float distanceToCenter)
        {
            return IsScreenPointNearDropRect(targetRect, screenPosition, eventCamera, out distanceToCenter, 0.25f, 44f, 0f, 0f);
        }

        bool IsScreenPointNearDropRect(
            RectTransform targetRect,
            Vector2 screenPosition,
            Camera eventCamera,
            out float distanceToCenter,
            float expansionRatio,
            float minimumMargin,
            float minimumWidth,
            float minimumHeight)
        {
            distanceToCenter = float.MaxValue;
            if (targetRect == null) return false;

            Rect screenRect = GetExpandedScreenRect(targetRect, eventCamera, expansionRatio, minimumMargin, minimumWidth, minimumHeight);
            if (!screenRect.Contains(screenPosition)) return false;

            Vector2 center = screenRect.center;
            distanceToCenter = (screenPosition - center).sqrMagnitude;
            return true;
        }

        Rect GetExpandedScreenRect(
            RectTransform targetRect,
            Camera eventCamera,
            float expansionRatio,
            float minimumMargin,
            float minimumWidth,
            float minimumHeight)
        {
            var corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            float width = Mathf.Max(1f, max.x - min.x);
            float height = Mathf.Max(1f, max.y - min.y);
            float marginX = Mathf.Max(minimumMargin, width * expansionRatio, (minimumWidth - width) * 0.5f);
            float marginY = Mathf.Max(minimumMargin, height * expansionRatio, (minimumHeight - height) * 0.5f);

            min -= new Vector2(marginX, marginY);
            max += new Vector2(marginX, marginY);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public void NotifyPlayerBattlefieldCardPlaced()
        {
            RefreshZoneInfoUI();
        }

        public bool CanPlaceSceneCard(UcgCardData cardData, UcgPlayerSide side, out string message)
        {
            if (_isTutorialFinishWaitingForClick)
            {
                message = "教學已完成，請點擊畫面任意處繼續";
                return false;
            }

            if (_isOpeningFirstPlayerSequence)
            {
                message = "正在決定先攻";
                return false;
            }

            if (_isSelectingDeckOperationCard)
            {
                message = IsHandReturnSelectionMode()
                    ? "請選擇一張手牌放回牌庫最底下"
                    : "請先完成選牌";
                return false;
            }

            if (IsGameOver)
            {
                message = "遊戲已結束，請重新開始";
                return false;
            }

            if (side == UcgPlayerSide.Player && !CanPlayerInteract(out string reason))
            {
                message = GetInteractionLockMessage(reason);
                return false;
            }

            if (sharedSceneSlot == null)
            {
                message = "場景區尚未建立";
                return false;
            }

            if (_pendingAction != null)
            {
                message = "請先確認或取消目前動作";
                return false;
            }

            if (_isAutoPhaseRunning || (_isOpponentActionRunning && side == UcgPlayerSide.Player))
            {
                message = "請等待目前流程完成";
                return false;
            }

            if (cardData == null)
            {
                message = "不是有效場景卡";
                return false;
            }

            if (!cardData.IsSceneCard())
            {
                message = "角色卡不能設置到場景區";
                return false;
            }

            if (phaseManager == null || phaseManager.CurrentPhase != UcgGamePhase.SceneSetup)
            {
                message = "只能在場景牌設置階段設置場景卡";
                return false;
            }

            bool allowDigaScriptedOpponentScene = IsDigaScriptedOpponentSceneStep(side);
            if (!allowDigaScriptedOpponentScene && !IsCurrentFirstPlayer(side))
            {
                message = side == UcgPlayerSide.Player
                    ? "本回合不是我方場景牌設置時機"
                    : "本回合不是對手場景牌設置時機";
                return false;
            }

            if (side == UcgPlayerSide.Player && IsDigaTutorialModeActive())
            {
                int tutorialTurn = turnManager != null ? turnManager.currentTurn : 1;
                if (tutorialTurn <= 2)
                {
                    message = $"第 {tutorialTurn} 回合先展開戰鬥區，2 燈場景第 3 回合再使用";
                    return false;
                }

                if (tutorialTurn == 3 && !IsDigaTutorialTargetSceneCard(cardData))
                {
                    message = "請使用教學指定的 2 燈場景卡";
                    return false;
                }
            }

            int allowedSceneLight = GetAllowedSceneLightCount();
            if (cardData.sceneTurnCost > allowedSceneLight)
            {
                message = $"目前可設置最高 {allowedSceneLight} 燈場景，此卡為 {cardData.sceneTurnCost} 燈";
                return false;
            }

            if (_sceneCardPlacedTurn == (turnManager != null ? turnManager.currentTurn : 1))
            {
                message = "本回合已設置過場景卡";
                return false;
            }

            UcgCardData currentScene = sharedSceneSlot != null ? sharedSceneSlot.SceneCardData : null;
            if (currentScene != null && cardData.sceneTurnCost < currentScene.sceneTurnCost)
            {
                message = "新場景燈數低於目前場景，無法替換";
                return false;
            }

            message = "場景卡設置成功";
            return true;
        }

        bool IsDigaScriptedOpponentSceneStep(UcgPlayerSide side)
        {
            return side == UcgPlayerSide.Opponent
                && currentTestMode == UcgTestMode.UltramanTest
                && turnManager != null
                && turnManager.currentTurn == 3
                && phaseManager != null
                && phaseManager.CurrentPhase == UcgGamePhase.SceneSetup
                && IsCurrentFirstPlayer(UcgPlayerSide.Player);
        }

        bool IsDigaTutorialModeActive()
        {
            return currentTestMode == UcgTestMode.UltramanTest
                && tutorialGuide != null
                && tutorialGuide.isTutorialMode
                && !tutorialGuide.tutorialCompleted;
        }

        bool IsDigaTutorialTargetSceneCard(UcgCardData cardData)
        {
            if (cardData == null) return false;
            if (cardData.id == "BP01-105") return true;
            if (cardData.sku == "BP-01-105-null") return true;
            return cardData.cardName == UcgDigaTutorialDeckFactory.TargetTutorialSceneName;
        }

        bool IsDigaTutorialSetupCard(UcgCardData cardData, bool preferLevelOne)
        {
            if (cardData == null || cardData.IsSceneCard()) return false;
            if (cardData.characterName != "迪卡") return false;
            if (preferLevelOne) return cardData.level == 1;

            UcgBattleLane activeLane = GetActiveTutorialLane();
            UcgCardView topCard = activeLane != null && activeLane.playerPlayArea != null
                ? activeLane.playerPlayArea.GetTopCard()
                : null;
            return topCard == null;
        }

        bool IsDigaTutorialUpgradeCardForLane(UcgCardData cardData, UcgBattleLane lane)
        {
            if (cardData == null || lane == null || cardData.IsSceneCard()) return false;
            if (cardData.characterName != "迪卡") return false;
            if (turnManager != null && !turnManager.CanUpgradeLaneThisTurn(lane.laneIndex, out _)) return false;

            UcgPlayArea playArea = lane.GetPlayerPlayArea();
            UcgCardView topCard = playArea != null ? playArea.GetTopCard() : null;
            UcgCardData topCardData = topCard != null ? topCard.CardData : null;
            if (topCardData == null) return false;

            return UcgActionValidator.CanPlayOrUpgrade(cardData, topCardData, out _, out UcgPlayActionType actionType)
                && actionType == UcgPlayActionType.Upgrade;
        }

        public bool TryPlaceSceneCardFromHand(UcgCardView sceneCard, UIDragCard dragCard, RectTransform cardRect, out string message)
        {
            message = "";
            if (sceneCard == null || dragCard == null || cardRect == null)
            {
                message = "不是有效場景卡";
                return false;
            }

            UcgCardData sceneData = sceneCard.CardData;
            if (!ValidatePlayerCardDrop(
                    sceneCard,
                    null,
                    UcgPlayerCardDropTarget.SceneSlot,
                    out message,
                    out _,
                    true))
            {
                return false;
            }

            bool replacing = sharedSceneSlot != null && sharedSceneSlot.HasSceneCard;
            dragCard.MarkDropped();
            BeginPendingSceneAction(sceneCard, dragCard, cardRect);
            if (sharedSceneSlot != null)
            {
                sharedSceneSlot.SetHighlight(false, false);
            }
            message = replacing
                ? "請確認是否替換目前場景卡"
                : "請確認是否設置場景卡";
            return true;
        }

        bool TryPlaceSceneCardFromScript(UcgCardData sceneCard, UcgPlayerSide side, out string message)
        {
            message = "";
            if (!CanPlaceSceneCard(sceneCard, side, out message)) return false;

            DiscardCurrentSceneIfNeeded(out bool replaced, out UcgCardData oldSceneCard, out UcgPlayerSide oldSceneOwner);
            if (sceneCard.cardImage == null && !sceneCard.IsExternalCard())
            {
                sceneCard.cardImage = GetTestCardSprite(side == UcgPlayerSide.Opponent ? 1 : 0);
            }

            if (sharedSceneSlot != null)
            {
                sharedSceneSlot.PlaceSceneCardFromScript(sceneCard, side);
            }
            LogScenePlacement(sceneCard, side, replaced, oldSceneCard, oldSceneOwner);

            _sceneCardPlacedTurn = turnManager != null ? turnManager.currentTurn : 1;
            string drawMessage = DrawOneForSceneOwner(side);
            string enterMessage = ResolveSceneEnterEffect(sceneCard, side);
            message = BuildScenePlacementMessage(side, replaced, oldSceneCard, oldSceneOwner, drawMessage, enterMessage);
            RefreshInteractionHints();
            RefreshZoneInfoUI();
            return true;
        }

        void DiscardCurrentSceneIfNeeded(out bool replaced, out UcgCardData oldSceneCard, out UcgPlayerSide oldSceneOwner)
        {
            replaced = false;
            oldSceneCard = null;
            oldSceneOwner = UcgPlayerSide.Player;
            if (sharedSceneSlot == null || !sharedSceneSlot.HasSceneCard || sharedSceneSlot.SceneCardData == null) return;

            replaced = true;
            oldSceneCard = sharedSceneSlot.SceneCardData;
            oldSceneOwner = sharedSceneSlot.SceneOwner;
            AddSceneToDiscard(oldSceneCard, oldSceneOwner);

            RefreshZoneInfoUI();
        }

        void AddSceneToDiscard(UcgCardData sceneCard, UcgPlayerSide owner)
        {
            if (sceneCard == null) return;

            if (owner == UcgPlayerSide.Player)
            {
                _playerDiscardPile.Add(sceneCard);
            }
            else
            {
                _opponentDiscardPile.Add(sceneCard);
            }
        }

        string BuildScenePlacementMessage(UcgPlayerSide newOwner, bool replaced, UcgCardData oldSceneCard, UcgPlayerSide oldOwner, string drawMessage, string enterMessage)
        {
            string ownerText = newOwner == UcgPlayerSide.Player ? "我方" : "對手";
            string suffix = string.IsNullOrWhiteSpace(enterMessage) ? "" : $"｜{enterMessage}";
            if (!replaced || oldSceneCard == null)
            {
                return $"{ownerText}設置場景，{drawMessage}{suffix}";
            }

            return $"{ownerText}設置新場景，{oldSceneCard.cardName}送入{GetDiscardOwnerText(oldOwner)}，{drawMessage}{suffix}";
        }

        string ResolveSceneEnterEffect(UcgCardData sceneCard, UcgPlayerSide owner)
        {
            if (sceneCard == null || sceneCard.sceneEffectTiming != UcgEffectTiming.OnRevealOrEnter) return "";

            switch (sceneCard.sceneEffectId)
            {
                case UcgDemoSceneEffectId.OnEnterDrawOne:
                    if (DrawCardsFromEffect(owner, 1, sceneCard) > 0)
                    {
                        QueueEffectFeedback($"場景出現時｜{sceneCard.cardName}：抽 1 張牌");
                        return "場景出現時效果：再抽 1 張牌";
                    }

                    return "場景出現時效果：牌組已空";
                default:
                    return "";
            }
        }

        string GetDiscardOwnerText(UcgPlayerSide owner)
        {
            return owner == UcgPlayerSide.Player ? "我方棄牌區" : "對手棄牌區";
        }

        void LogScenePlacement(UcgCardData newSceneCard, UcgPlayerSide newOwner, bool replaced, UcgCardData oldSceneCard, UcgPlayerSide oldOwner)
        {
            if (!debugScenePlacement) return;
            if (newSceneCard == null) return;

            if (replaced && oldSceneCard != null)
            {
                Debug.Log($"Scene replaced: old={oldSceneCard.cardName}, oldOwner={oldOwner}, new={newSceneCard.cardName}, newOwner={newOwner}");
                Debug.Log($"Scene replace visual: old={oldSceneCard.cardName}, oldOwner={oldOwner}, new={newSceneCard.cardName}, newOwner={newOwner}");
                Debug.Log($"Discard count: player={_playerDiscardPile.Count}, opponent={_opponentDiscardPile.Count}");
            }
            else
            {
                Debug.Log($"Scene placed: new={newSceneCard.cardName}, owner={newOwner}");
            }
        }

        string DrawOneForSceneOwner(UcgPlayerSide side)
        {
            if (side == UcgPlayerSide.Player)
            {
                if (DrawOneCardFromEffect())
                {
                    RefreshZoneInfoUI();
                    return "抽 1 張牌";
                }

                RefreshZoneInfoUI();
                return "牌組已空，無法抽牌";
            }

            if (deckManager != null && deckManager.opponentDrawPile.Count > 0)
            {
                deckManager.DrawOpponentCard();
                SyncOpponentZoneCountsFromDeckManager();
                RefreshZoneInfoUI();
                return "抽 1 張牌";
            }

            if (_opponentDeckCount <= 0)
            {
                RefreshZoneInfoUI();
                return "對手牌組已空，無法抽牌";
            }

            _opponentDeckCount--;
            _opponentHandCount++;
            RefreshZoneInfoUI();
            return "抽 1 張牌";
        }

        public void NotifySceneCardPlaced(UcgPlayerSide side, UcgCardView sceneCard)
        {
            if (sceneCard == null || sceneCard.CardData == null) return;

            if (playResultText != null)
            {
                string ownerText = side == UcgPlayerSide.Player ? "我方" : "對手";
                playResultText.text = $"{ownerText}已設置場景：{sceneCard.CardData.cardName}";
            }

            RefreshInteractionHints();
        }

        public void ShowSceneDropMessage(string message)
        {
            if (playResultText != null)
            {
                playResultText.text = string.IsNullOrWhiteSpace(message) ? "不能設置場景卡" : message;
            }
        }

        int GetAllowedSceneLightCount()
        {
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            return Mathf.Max(0, currentTurn - 1);
        }

        void ApplySceneBpModifiers(System.Collections.Generic.List<UcgBattleLane> openedLanes)
        {
            if (battlefieldManager == null) return;

            battlefieldManager.ClearSceneBpModifiers();
            ApplySceneCardBpModifier(
                sharedSceneSlot != null ? sharedSceneSlot.SceneCardData : null,
                sharedSceneSlot != null ? sharedSceneSlot.SceneOwner : UcgPlayerSide.Player,
                openedLanes);
        }

        void ApplyConditionalBpModifiers(System.Collections.Generic.List<UcgBattleLane> openedLanes)
        {
            if (battlefieldManager == null || openedLanes == null) return;

            battlefieldManager.ClearConditionalBpModifiers();
            var contextCards = BuildBattlefieldEffectContext(openedLanes);

            for (int i = 0; i < openedLanes.Count; i++)
            {
                UcgBattleLane lane = openedLanes[i];
                if (lane == null) continue;

                ApplySelfConditionalBp(lane, UcgPlayerSide.Player, contextCards);
                ApplySelfConditionalBp(lane, UcgPlayerSide.Opponent, contextCards);
                ApplyAllyConditionalBp(lane, UcgPlayerSide.Player, openedLanes, contextCards);
                ApplyAllyConditionalBp(lane, UcgPlayerSide.Opponent, openedLanes, contextCards);
                ApplySceneConditionalBp(lane, contextCards);
            }
        }

        System.Collections.Generic.List<UcgCardData> BuildBattlefieldEffectContext(System.Collections.Generic.List<UcgBattleLane> openedLanes)
        {
            var cards = new System.Collections.Generic.List<UcgCardData>();
            if (openedLanes != null)
            {
                for (int i = 0; i < openedLanes.Count; i++)
                {
                    UcgBattleLane lane = openedLanes[i];
                    AddContextCard(cards, GetLaneTopCard(lane, UcgPlayerSide.Player));
                    AddContextCard(cards, GetLaneTopCard(lane, UcgPlayerSide.Opponent));
                }
            }

            AddContextCard(cards, sharedSceneSlot != null ? sharedSceneSlot.SceneCardData : null);
            return cards;
        }

        void AddContextCard(System.Collections.Generic.List<UcgCardData> cards, UcgCardData card)
        {
            if (cards == null || card == null) return;
            cards.Add(card);
        }

        void ApplySelfConditionalBp(UcgBattleLane lane, UcgPlayerSide side, System.Collections.Generic.IReadOnlyList<UcgCardData> contextCards)
        {
            UcgCardData sourceCard = GetLaneTopCard(lane, side);
            if (sourceCard == null) return;

            UcgConditionalBpRule rule = UcgConditionalBpParser.Parse(sourceCard, contextCards);
            if (!rule.supported)
            {
                if (UcgConditionalBpParser.ShouldWarnUnsupportedConditional(sourceCard))
                {
                    UcgConditionalBpParser.WarnUnsupported(sourceCard, rule);
                }
                return;
            }
            if (!IsConditionalStackRequirementMet(sourceCard, rule, lane, side, out string stackSkipReason))
            {
                DebugLogSkippedConditionalEffect(sourceCard, rule, lane, side, stackSkipReason);
                return;
            }

            UcgCardData opponentCard = GetLaneTopCard(lane, GetOpponentSide(side));
            bool applies = false;
            string condition = "";
            int multiplier = 1;

            switch (rule.category)
            {
                case UcgConditionalBpCategory.ParsedOpponentTypeCondition:
                    applies = CardTypeMatchesAny(opponentCard, rule.allowedTypes, rule.keyword);
                    condition = $"對手 type={FormatAllowedTypes(rule.allowedTypes, rule.keyword)}";
                    break;
                case UcgConditionalBpCategory.ParsedOpponentCategoryCondition:
                    applies = CardCategoryMatches(opponentCard, rule.keyword);
                    condition = $"對手 cardCategory={rule.keyword}";
                    break;
                case UcgConditionalBpCategory.ParsedCharacterNameCondition:
                    int count = CountMatchingCharacters(side, rule.keyword);
                    applies = count > 0;
                    multiplier = rule.repeatPerMatchingCharacter ? count : 1;
                    condition = $"我方 characterName={rule.keyword} count={count}";
                    break;
                case UcgConditionalBpCategory.MappedSelfCharacterNameCountBoost:
                    int mappedCount = CountMatchingCharacters(side, rule.keyword);
                    applies = mappedCount > 0;
                    multiplier = rule.repeatPerMatchingCharacter ? mappedCount : 1;
                    condition = $"我方 characterName={rule.keyword} count={mappedCount}";
                    break;
            }

            if (!applies) return;
            AddResolvedConditionalModifier(lane, side, sourceCard, rule, condition, multiplier, GetLaneStackCount(lane, side));
        }

        void ApplyAllyConditionalBp(
            UcgBattleLane targetLane,
            UcgPlayerSide side,
            System.Collections.Generic.List<UcgBattleLane> openedLanes,
            System.Collections.Generic.IReadOnlyList<UcgCardData> contextCards)
        {
            UcgCardData targetCard = GetLaneTopCard(targetLane, side);
            if (targetCard == null || openedLanes == null) return;

            for (int i = 0; i < openedLanes.Count; i++)
            {
                UcgBattleLane sourceLane = openedLanes[i];
                if (sourceLane == null) continue;

                UcgCardData sourceCard = GetLaneTopCard(sourceLane, side);
                if (sourceCard == null || sourceCard == targetCard) continue;

                UcgConditionalBpRule rule = UcgConditionalBpParser.Parse(sourceCard, contextCards);
                if (!rule.supported)
                {
                    if (UcgConditionalBpParser.ShouldWarnUnsupportedConditional(sourceCard))
                    {
                        UcgConditionalBpParser.WarnUnsupported(sourceCard, rule);
                    }
                    continue;
                }
                if (!IsConditionalStackRequirementMet(sourceCard, rule, sourceLane, side, out string stackSkipReason))
                {
                    DebugLogSkippedConditionalEffect(sourceCard, rule, sourceLane, side, stackSkipReason);
                    continue;
                }

                bool applies = false;
                string condition = "";
                switch (rule.category)
                {
                    case UcgConditionalBpCategory.ParsedAllyTypeBoost:
                        applies = CardTypeMatches(targetCard, rule.keyword);
                        condition = $"隊友 type={rule.keyword}";
                        break;
                    case UcgConditionalBpCategory.ParsedAllyCategoryBoost:
                        applies = CardCategoryMatches(targetCard, rule.keyword);
                        condition = $"隊友 cardCategory={rule.keyword}";
                        break;
                    case UcgConditionalBpCategory.ParsedCharacterNameCondition:
                        applies = CardCharacterMatches(targetCard, rule.keyword);
                        condition = $"隊友 characterName={rule.keyword}";
                        break;
                }

                if (!applies) continue;
                AddResolvedConditionalModifier(targetLane, side, sourceCard, rule, condition, 1, GetLaneStackCount(sourceLane, side));
            }
        }

        void ApplySceneConditionalBp(UcgBattleLane lane, System.Collections.Generic.IReadOnlyList<UcgCardData> contextCards)
        {
            UcgCardData sceneCard = sharedSceneSlot != null ? sharedSceneSlot.SceneCardData : null;
            if (sceneCard == null) return;

            UcgConditionalBpRule rule = UcgConditionalBpParser.Parse(sceneCard, contextCards);
            if (!rule.supported)
            {
                if (UcgConditionalBpParser.ShouldWarnUnsupportedConditional(sceneCard))
                {
                    UcgConditionalBpParser.WarnUnsupported(sceneCard, rule);
                }
                return;
            }
            UcgPlayerSide ownerSide = sharedSceneSlot != null ? sharedSceneSlot.SceneOwner : UcgPlayerSide.Player;
            if (!IsConditionalStackRequirementMet(sceneCard, rule, null, ownerSide, out string stackSkipReason))
            {
                DebugLogSkippedConditionalEffect(sceneCard, rule, null, ownerSide, stackSkipReason);
                return;
            }

            UcgCardData targetCard = GetLaneTopCard(lane, ownerSide);
            if (targetCard == null) return;

            bool applies = false;
            string condition = "";
            switch (rule.category)
            {
                case UcgConditionalBpCategory.ParsedSceneTypeBoost:
                    applies = CardTypeMatches(targetCard, rule.keyword);
                    condition = $"場景指定 type={rule.keyword}";
                    break;
                case UcgConditionalBpCategory.ParsedSceneCharacterNameBoost:
                    applies = CardCharacterMatches(targetCard, rule.keyword);
                    condition = $"場景指定 characterName={rule.keyword}";
                    break;
                case UcgConditionalBpCategory.ParsedFixedBpBoost:
                    if (SceneFixedBpAlreadyApplied(sceneCard)) return;
                    applies = true;
                    condition = "場景全體";
                    break;
                case UcgConditionalBpCategory.ParsedBpStepUp:
                    applies = true;
                    condition = "場景全體";
                    break;
            }

            if (!applies) return;
            AddResolvedConditionalModifier(lane, ownerSide, sceneCard, rule, condition, 1, 0);
        }

        void AddResolvedConditionalModifier(
            UcgBattleLane lane,
            UcgPlayerSide targetSide,
            UcgCardData sourceCard,
            UcgConditionalBpRule rule,
            string condition,
            int multiplier,
            int sourceStackCount)
        {
            UcgCardData targetCard = GetLaneTopCard(lane, targetSide);
            if (lane == null || targetCard == null || rule == null || multiplier <= 0) return;

            int amount = rule.bpAmount * multiplier;
            int baseBp = GetLaneBaseBp(lane, targetSide);
            int stepToBp = baseBp;
            if (rule.isStepUp)
            {
                stepToBp = UcgBattleJudge.GetNextBpStep(targetCard, baseBp);
                amount = stepToBp - baseBp;
            }

            if (amount == 0) return;

            var modifier = new UcgBpModifierInfo
            {
                sourceCardId = sourceCard != null ? sourceCard.id : "",
                sourceCardName = sourceCard != null ? sourceCard.cardName : "",
                reason = rule.category.ToString(),
                trigger = "BeforeJudgement",
                condition = condition,
                effectCategory = GetEffectCategoryText(sourceCard),
                duration = sourceCard != null && sourceCard.IsSceneCard() ? UcgEffectDuration.WhileSceneActive : UcgEffectDuration.None,
                amount = amount,
                requiredStackCount = rule.requiredStackCount,
                currentStackCount = sourceStackCount,
                requireExactStackCount = rule.requireExactStackCount,
                stackRequirementMet = true,
                isStepUp = rule.isStepUp,
                stepFromBp = baseBp,
                stepToBp = stepToBp
            };

            lane.AddConditionalBpModifier(targetSide, modifier);
            QueueEffectFeedback(BuildBpEffectFeedback(sourceCard, amount, rule.isStepUp));
        }

        string BuildBpEffectFeedback(UcgCardData sourceCard, int amount, bool isStepUp)
        {
            string categoryText = sourceCard != null && sourceCard.IsSceneCard() ? "場景效果" : "角色效果";
            if (isStepUp) return $"{categoryText}：BP 上升一階";

            string sign = amount >= 0 ? "+" : "";
            return $"{categoryText}：BP {sign}{amount}";
        }

        bool IsConditionalStackRequirementMet(
            UcgCardData sourceCard,
            UcgConditionalBpRule rule,
            UcgBattleLane sourceLane,
            UcgPlayerSide sourceSide,
            out string skippedReason)
        {
            skippedReason = "";
            if (rule == null || rule.requiredStackCount <= 0) return true;
            if (sourceCard != null && sourceCard.IsSceneCard()) return true;

            int currentStackCount = GetLaneStackCount(sourceLane, sourceSide);
            bool met = rule.requireExactStackCount
                ? currentStackCount == rule.requiredStackCount
                : currentStackCount >= rule.requiredStackCount;
            if (met) return true;

            string op = rule.requireExactStackCount ? "==" : ">=";
            skippedReason = $"Stack count insufficient: required {op}{rule.requiredStackCount}, current={currentStackCount}";
            return false;
        }

        void DebugLogSkippedConditionalEffect(
            UcgCardData sourceCard,
            UcgConditionalBpRule rule,
            UcgBattleLane sourceLane,
            UcgPlayerSide sourceSide,
            string skippedReason)
        {
            if (!debugBpBreakdown || sourceCard == null || rule == null) return;

            Debug.Log(
                "Effect skipped:\n"
                + $"card={sourceCard.id} {sourceCard.cardName}\n"
                + $"category={GetEffectCategoryText(sourceCard)}\n"
                + $"requiredStackCount={rule.requiredStackCount}\n"
                + $"currentStackCount={GetLaneStackCount(sourceLane, sourceSide)}\n"
                + $"reason={skippedReason}");
        }

        int GetLaneStackCount(UcgBattleLane lane, UcgPlayerSide side)
        {
            if (lane == null) return 0;
            if (side == UcgPlayerSide.Player)
            {
                return lane.playerPlayArea != null ? lane.playerPlayArea.GetStackCount() : 0;
            }

            return lane.GetOpponentStackCount();
        }

        string GetEffectCategoryText(UcgCardData card)
        {
            if (card == null) return "None";
            UcgEffectTiming timing = card.IsSceneCard() ? card.sceneEffectTiming : card.effectTiming;
            switch (timing)
            {
                case UcgEffectTiming.OnRevealOrEnter:
                    return "EnterEffect";
                case UcgEffectTiming.Activated:
                    return "BattleEffect";
                case UcgEffectTiming.Continuous:
                    return "ContinuousEffect";
                default:
                    return "None";
            }
        }

        int GetLaneBaseBp(UcgBattleLane lane, UcgPlayerSide side)
        {
            UcgCardData card = GetLaneTopCard(lane, side);
            if (card == null) return 0;

            int stackCount = side == UcgPlayerSide.Player && lane != null && lane.playerPlayArea != null
                ? lane.playerPlayArea.GetStackCount()
                : lane != null ? lane.GetOpponentStackCount() : 1;
            return card.GetBpByStackCount(stackCount);
        }

        UcgCardData GetLaneTopCard(UcgBattleLane lane, UcgPlayerSide side)
        {
            if (lane == null) return null;
            UcgCardView view = side == UcgPlayerSide.Player
                ? lane.playerPlayArea != null ? lane.playerPlayArea.GetTopCard() : lane.playerTopCard
                : lane.GetOpponentTopCard();
            return view != null ? view.CardData : null;
        }

        int CountMatchingCharacters(UcgPlayerSide side, string keyword)
        {
            if (battlefieldManager == null || turnManager == null || string.IsNullOrWhiteSpace(keyword)) return 0;

            int count = 0;
            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                if (CardCharacterMatches(GetLaneTopCard(lanes[i], side), keyword)) count++;
            }

            return count;
        }

        bool CardTypeMatches(UcgCardData card, string keyword)
        {
            return CardHasEffectiveType(card, keyword);
        }

        bool CardTypeMatchesAny(UcgCardData card, System.Collections.Generic.IReadOnlyList<string> allowedTypes, string fallbackKeyword)
        {
            if (card == null) return false;
            if (allowedTypes != null && allowedTypes.Count > 0)
            {
                for (int i = 0; i < allowedTypes.Count; i++)
                {
                    if (CardTypeMatches(card, allowedTypes[i])) return true;
                }

                return false;
            }

            return CardTypeMatches(card, fallbackKeyword);
        }

        string FormatAllowedTypes(System.Collections.Generic.IReadOnlyList<string> allowedTypes, string fallbackKeyword)
        {
            if (allowedTypes != null && allowedTypes.Count > 0)
            {
                return string.Join("/", allowedTypes);
            }

            return fallbackKeyword;
        }

        bool CardCategoryMatches(UcgCardData card, string keyword)
        {
            return card != null && !string.IsNullOrWhiteSpace(keyword) && card.cardCategory == keyword;
        }

        bool CardCharacterMatches(UcgCardData card, string keyword)
        {
            if (card == null || string.IsNullOrWhiteSpace(keyword)) return false;
            if (!string.IsNullOrWhiteSpace(card.characterName) && card.characterName == keyword) return true;
            return !string.IsNullOrWhiteSpace(card.cardName) && card.cardName.Contains(keyword);
        }

        UcgPlayerSide GetOpponentSide(UcgPlayerSide side)
        {
            return side == UcgPlayerSide.Player ? UcgPlayerSide.Opponent : UcgPlayerSide.Player;
        }

        bool SceneFixedBpAlreadyApplied(UcgCardData sceneCard)
        {
            if (sceneCard == null) return false;
            switch (sceneCard.sceneEffectId)
            {
                case UcgDemoSceneEffectId.PlayerAllBpPlus500:
                case UcgDemoSceneEffectId.PlayerAllBpPlus1000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus2000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus3000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus500:
                case UcgDemoSceneEffectId.OpponentAllBpPlus1000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus2000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus3000:
                case UcgDemoSceneEffectId.ActiveLanePlayerBpPlus1000:
                    return true;
                default:
                    return false;
            }
        }

        string GetEffectText(UcgCardData card)
        {
            if (card == null) return "";
            if (card.IsSceneCard() && !string.IsNullOrWhiteSpace(card.sceneDescription)) return card.sceneDescription;
            if (!string.IsNullOrWhiteSpace(card.effectDescription)) return card.effectDescription;
            return card.sceneDescription;
        }

        void ApplySceneCardBpModifier(UcgCardData sceneCard, UcgPlayerSide ownerSide, System.Collections.Generic.List<UcgBattleLane> openedLanes)
        {
            if (sceneCard == null || openedLanes == null) return;

            switch (sceneCard.sceneEffectId)
            {
                case UcgDemoSceneEffectId.PlayerAllBpPlus500:
                case UcgDemoSceneEffectId.PlayerAllBpPlus1000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus2000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus3000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus500:
                case UcgDemoSceneEffectId.OpponentAllBpPlus1000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus2000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus3000:
                    AddSceneModifierToLanes(openedLanes, ownerSide, GetSceneBpAmount(sceneCard.sceneEffectId), sceneCard, "場景持續效果");
                    break;
                case UcgDemoSceneEffectId.ActiveLanePlayerBpPlus1000:
                    AddActiveLaneSceneModifier(ownerSide, 1000, sceneCard, "場景持續效果");
                    break;
            }
        }

        int GetSceneBpAmount(UcgDemoSceneEffectId sceneEffectId)
        {
            switch (sceneEffectId)
            {
                case UcgDemoSceneEffectId.PlayerAllBpPlus3000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus3000:
                    return 3000;
                case UcgDemoSceneEffectId.PlayerAllBpPlus2000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus2000:
                    return 2000;
                case UcgDemoSceneEffectId.PlayerAllBpPlus1000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus1000:
                    return 1000;
                default:
                    return 500;
            }
        }

        void AddSceneModifierToLanes(System.Collections.Generic.List<UcgBattleLane> lanes, UcgPlayerSide targetSide, int amount, UcgCardData sourceCard, string reason)
        {
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;
                lane.AddSceneBpModifier(targetSide, amount, sourceCard, reason);
            }
        }

        void AddActiveLaneSceneModifier(UcgPlayerSide targetSide, int amount, UcgCardData sourceCard, string reason)
        {
            if (battlefieldManager == null || turnManager == null) return;

            UcgBattleLane lane = battlefieldManager.GetLane(turnManager.ActiveNewLaneIndex);
            if (lane == null) return;
            lane.AddSceneBpModifier(targetSide, amount, sourceCard, reason);
        }

        void RefreshInteractionHints()
        {
            if (battlefieldManager == null || phaseManager == null || turnManager == null)
            {
                return;
            }

            RefreshHandCardDragInteractability();
            battlefieldManager.ClearLaneHighlights();
            ClearSceneHighlights();
            ClearPlayableHandHighlights();

            if (IsHandReturnSelectionMode())
            {
                ApplyHandReturnSelectionHighlights(true);
                return;
            }

            if (_isSelectingEffectTarget)
            {
                HighlightEffectTargets();
                return;
            }

            if (!CanPlayerInteract(out _))
            {
                return;
            }

            RefreshActiveLaneFocus();

            if (phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                bool canPlaceScene = IsCurrentFirstPlayer(UcgPlayerSide.Player)
                    && HasLegalSceneCardInHand();

                if (sharedSceneSlot != null)
                {
                    sharedSceneSlot.SetDropRaycastEnabled(canPlaceScene);
                    bool guideSceneSlot = canPlaceScene
                        && IsDigaTutorialModeActive()
                        && turnManager.currentTurn >= 3;
                    sharedSceneSlot.SetHighlight(guideSceneSlot, false);
                }
                if (canPlaceScene)
                {
                    ApplySceneCardHandHighlights(true);
                }
                return;
            }

            if (IsDigaTutorialModeActive()
                && phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup)
            {
                ApplyDigaTutorialCharacterSetupHandHighlights(true);
            }

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            bool hasUpgradeTarget = false;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                UcgPlayArea playArea = lane != null ? lane.GetPlayerPlayArea() : null;
                if (playArea == null) continue;

                if (phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup)
                {
                    playArea.SetHighlightState(lane.laneIndex == turnManager.ActiveNewLaneIndex
                        ? UcgLaneHighlightState.ActiveSetupTarget
                        : UcgLaneHighlightState.Normal);
                }
                else if (phaseManager.CurrentPhase == UcgGamePhase.Upgrade)
                {
                    bool hasPlayerCard = playArea.GetTopCard() != null;
                    bool canUpgrade = hasPlayerCard && turnManager.CanUpgradeLaneThisTurn(lane.laneIndex, out _);
                    playArea.SetHighlightState(canUpgrade ? UcgLaneHighlightState.UpgradeAvailable : UcgLaneHighlightState.Normal);
                    hasUpgradeTarget |= canUpgrade;
                }
            }

            if (phaseManager.CurrentPhase == UcgGamePhase.Upgrade && !hasUpgradeTarget && playResultText != null)
            {
                playResultText.text = "目前沒有可升級角色，可以結束升級";
            }
        }

        void ShowDragHints(UcgCardData cardData)
        {
            if (battlefieldManager == null || turnManager == null)
            {
                return;
            }

            battlefieldManager.ClearLaneHighlights();
            ClearSceneHighlights();
            ClearPlayableHandHighlights();

            if (cardData != null && cardData.IsSceneCard())
            {
                bool validScene = ValidatePlayerCardDrop(
                    cardData,
                    null,
                    UcgPlayerCardDropTarget.SceneSlot,
                    out _,
                    out _,
                    false);
                if (sharedSceneSlot != null)
                {
                    sharedSceneSlot.SetDropRaycastEnabled(validScene);
                    sharedSceneSlot.SetHighlight(false, false);
                }
                return;
            }

            if (sharedSceneSlot != null)
            {
                sharedSceneSlot.SetDropRaycastEnabled(false);
            }

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                UcgPlayArea playArea = lane != null ? lane.GetPlayerPlayArea() : null;
                if (playArea == null) continue;

                bool valid = CanDropCardOnLane(cardData, lane, out _);
                playArea.SetHighlightState(valid ? UcgLaneHighlightState.ValidDropTarget : UcgLaneHighlightState.InvalidDropTarget);
            }
        }

        void ClearInteractionHints()
        {
            if (battlefieldManager != null)
            {
                battlefieldManager.ClearLaneHighlights();
            }

            ClearSceneHighlights();
            ClearPlayableHandHighlights();
        }

        void RefreshActiveLaneFocus()
        {
            if (battlefieldManager == null || turnManager == null) return;

            var lanes = battlefieldManager.GetOpenedLanes(turnManager.currentTurn);
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                lane.SetActiveLaneFocus(lane.laneIndex == turnManager.ActiveNewLaneIndex);
            }
        }

        void ClearSceneHighlights()
        {
            if (sharedSceneSlot != null)
            {
                sharedSceneSlot.SetDropRaycastEnabled(false);
                sharedSceneSlot.SetHighlight(false, false);
            }
        }

        bool HasLegalSceneCardInHand()
        {
            if (cardHolder == null) return false;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView != null
                    && cardView.CardData != null
                    && cardView.CardData.IsSceneCard()
                    && ValidatePlayerCardDrop(
                        cardView,
                        null,
                        UcgPlayerCardDropTarget.SceneSlot,
                        out _,
                        out _,
                        false))
                {
                    return true;
                }
            }

            return false;
        }

        void ApplySceneCardHandHighlights(bool active)
        {
            if (cardHolder == null) return;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView == null || cardView.CardData == null) continue;

                bool legalScene = active
                    && cardView.CardData.IsSceneCard()
                    && ValidatePlayerCardDrop(
                        cardView,
                        null,
                        UcgPlayerCardDropTarget.SceneSlot,
                        out _,
                        out _,
                        false);
                cardView.SetPlayableHighlight(legalScene);
            }
        }

        void ApplyDigaTutorialCharacterSetupHandHighlights(bool active)
        {
            if (cardHolder == null) return;

            bool hasLevelOneDiga = HasDigaLevelOneInHand();
            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView == null || cardView.CardData == null) continue;

                bool targetCard = active && IsDigaTutorialSetupCard(cardView.CardData, hasLevelOneDiga);
                cardView.SetPlayableHighlight(targetCard);
            }
        }

        bool HasDigaLevelOneInHand()
        {
            if (cardHolder == null) return false;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                UcgCardData cardData = cardView != null ? cardView.CardData : null;
                if (cardData != null && !cardData.IsSceneCard() && cardData.characterName == "迪卡" && cardData.level == 1)
                {
                    return true;
                }
            }

            return false;
        }

        void ApplyDigaTutorialUpgradeHandHighlights(bool active)
        {
            if (cardHolder == null) return;

            UcgBattleLane activeLane = GetActiveTutorialLane();
            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView == null || cardView.CardData == null) continue;

                bool targetCard = active && IsDigaTutorialUpgradeCardForLane(cardView.CardData, activeLane);
                cardView.SetPlayableHighlight(targetCard);
            }
        }

        bool HasDigaTutorialUpgradeCardForActiveLane()
        {
            if (cardHolder == null) return false;

            UcgBattleLane activeLane = GetActiveTutorialLane();
            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView != null && IsDigaTutorialUpgradeCardForLane(cardView.CardData, activeLane))
                {
                    return true;
                }
            }

            return false;
        }

        UcgBattleLane GetActiveTutorialLane()
        {
            if (battlefieldManager == null || turnManager == null) return null;
            return battlefieldManager.GetLane(turnManager.ActiveNewLaneIndex);
        }

        void ClearPlayableHandHighlights()
        {
            if (cardHolder == null) return;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView != null)
                {
                    cardView.SetPlayableHighlight(false);
                }
            }
        }

        bool IsHandReturnSelectionMode()
        {
            return _isSelectingDeckOperationCard
                && _pendingDeckSelection != null
                && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.Hand;
        }

        bool IsCurrentHandCardView(UcgCardView cardView)
        {
            return cardView != null
                && cardHolder != null
                && cardView.transform.parent == cardHolder;
        }

        void ApplyHandReturnSelectionHighlights(bool active)
        {
            if (cardHolder == null) return;

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                var cardView = cardHolder.GetChild(i).GetComponent<UcgCardView>();
                if (cardView == null || cardView.CardData == null) continue;

                bool selectable = active && deckManager != null && deckManager.playerHand.Contains(cardView.CardData);
                cardView.SetPlayableHighlight(selectable);
            }
        }

        void ShowGameOverMessage()
        {
            string winnerText = CurrentGameResult == UcgGameResultType.PlayerWin ? "我方勝利" : "對手勝利";
            string message = $"遊戲已結束｜{winnerText}";
            if (playResultText != null)
            {
                playResultText.text = message;
            }
        }

        public void DebugPrintGameState()
        {
            int currentTurn = turnManager != null ? turnManager.currentTurn : 0;
            string phaseText = phaseManager != null ? phaseManager.CurrentPhase.ToString() : "None";
            string firstPlayerText = turnOrderManager != null ? turnOrderManager.GetCurrentFirstPlayer().ToString() : "None";
            string openingFirstPlayerText = turnOrderManager != null ? turnOrderManager.firstPlayer.ToString() : "None";
            string actingPlayerText = turnOrderManager != null ? turnOrderManager.currentActingPlayer.ToString() : "None";
            int openedLaneCount = battlefieldManager != null && turnManager != null
                ? battlefieldManager.GetOpenedLaneCount(turnManager.currentTurn)
                : 0;
            UcgCardData sceneCard = sharedSceneSlot != null ? sharedSceneSlot.SceneCardData : null;
            string sceneName = sceneCard != null ? sceneCard.cardName : "None";
            string sceneOwnerText = sharedSceneSlot != null && sceneCard != null ? sharedSceneSlot.SceneOwner.ToString() : "None";
            int playerHandCount = deckManager != null && deckManager.PlayerHand != null
                ? deckManager.PlayerHand.Count
                : (cardHolder != null ? cardHolder.childCount : 0);
            int playerDeckCount = deckManager != null ? deckManager.RemainingCount : 0;
            int effectQueueCount = effectManager != null ? effectManager.PendingCount : 0;

            Debug.Log(
                "UCG QA State\n"
                + $"turn={currentTurn}, phase={phaseText}, firstPlayer={firstPlayerText}, openingFirstPlayer={openingFirstPlayerText}, actingPlayer={actingPlayerText}, openedLaneCount={openedLaneCount}\n"
                + $"scene={sceneName}, sceneOwner={sceneOwnerText}\n"
                + $"playerHand={playerHandCount}, playerDeck={playerDeckCount}, playerDiscard={_playerDiscardPile.Count}\n"
                + $"opponentHand={_opponentHandCount}, opponentDeck={_opponentDeckCount}, opponentDiscard={_opponentDiscardPile.Count}\n"
                + $"pendingAction={(_pendingAction != null)}, effectQueue={effectQueueCount}, selectingEffectTarget={_isSelectingEffectTarget}\n"
                + $"tutorialFinishedWaiting={_isTutorialFinishWaitingForClick}, playerWonTutorialLanes={_playerWonTutorialLaneIndexes.Count}\n"
                + $"autoPhase={_isAutoPhaseRunning}, opponentAction={_isOpponentActionRunning}, sceneSkipRoutine={(_sceneSetupSkipRoutine != null)}, gameOver={IsGameOver}, result={CurrentGameResult}");
        }

        string DecideNextFirstPlayerFromCurrentTurn()
        {
            if (turnOrderManager == null || battlefieldManager == null || turnManager == null)
            {
                return "";
            }

            int latestLaneIndex = Mathf.Clamp(turnManager.currentTurn - 1, 0, battlefieldManager.maxLaneCount - 1);
            UcgBattleLane latestLane = battlefieldManager.GetLane(latestLaneIndex);
            UcgPlayerSide currentFirstPlayer = turnOrderManager.GetCurrentFirstPlayer();
            UcgPlayerSide nextFirstPlayer = turnOrderManager.DecideNextFirstPlayerFromLatestLane(latestLane, currentFirstPlayer);

            if (latestLane == null || latestLane.laneResult == UcgLaneResultType.Draw || latestLane.laneResult == UcgLaneResultType.None)
            {
                return $"平局或無結果，{turnOrderManager.GetNextFirstPlayerText()}";
            }

            return turnOrderManager.GetNextFirstPlayerText();
        }

        string GetDemoGameResultMessage(UcgGameResultType gameResult, string fallbackMessage)
        {
            switch (gameResult)
            {
                case UcgGameResultType.PlayerWin:
                    return "Demo 判定：我方目前達成 3 路勝利";
                case UcgGameResultType.OpponentWin:
                    return "Demo 判定：對手目前達成 3 路勝利";
                default:
                    return fallbackMessage;
            }
        }

        void UpdateMainPrompt()
        {
            if (tutorialGuide == null || phaseManager == null) return;
            RefreshNextPhaseButtonState();
            if (_isTutorialFinishWaitingForClick)
            {
                return;
            }

            if (_isEffectAutoAdvancing)
            {
                tutorialGuide.ShowPhasePrompt("效果選擇完成，接著進入判定。");
                return;
            }

            SyncTutorialStepForCurrentState();
            if (IsGameOver)
            {
                string winnerText = CurrentGameResult == UcgGameResultType.PlayerWin ? "我方勝利" : "對手勝利";
                tutorialGuide.ShowPhasePrompt($"遊戲結束｜{winnerText}\n我方勝利路數：{_lastPlayerWinCount}｜對手勝利路數：{_lastOpponentWinCount}");
                return;
            }

            if (_isSelectingEffectTarget && effectManager != null && _pendingTargetEffect != null)
            {
                tutorialGuide.ShowPhasePrompt(effectManager.GetTargetPrompt(_pendingTargetEffect));
                return;
            }

            string prompt = phaseManager.GetPhaseInfoText();
            if (IsDigaTutorialModeActive()
                && phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup
                && turnManager != null
                && turnManager.currentTurn == 2)
            {
                prompt = "第 2 路開放了，選一張角色卡放上場吧！";
            }
            if (IsDigaTutorialModeActive() && phaseManager.CurrentPhase == UcgGamePhase.SceneSetup)
            {
                int sceneTurn = turnManager != null ? turnManager.currentTurn : 1;
                prompt = sceneTurn <= 2
                    ? "這回合先專心展開戰鬥區，場景卡稍後再使用。"
                    : "現在可以設置場景卡了，有合適的場景就放到中央場景區。";
            }
            tutorialGuide.ShowPhasePrompt(prompt);
            if (phaseManager.CurrentPhase == UcgGamePhase.End)
            {
                RefreshNextPhaseButtonState();
            }
        }

        void StopTutorialGuidanceAfterSceneSetup()
        {
            if (tutorialGuide != null)
            {
                tutorialGuide.CompleteTutorial();
                tutorialGuide.gameObject.SetActive(false);
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }

            ClearEffectTargetHighlights();
            ClearPlayableHandHighlights();
            RefreshInteractionHints();
        }

        void SyncTutorialStepForCurrentState()
        {
            if (tutorialGuide == null || !tutorialGuide.isTutorialMode || phaseManager == null) return;

            if (IsGameOver)
            {
                tutorialGuide.SetStep(UcgTutorialStep.Complete);
                return;
            }

            if (_isOpponentActionRunning && phaseManager.CurrentPhase == UcgGamePhase.CharacterSetup)
            {
                tutorialGuide.SetStep(UcgTutorialStep.WaitOpponentSetup);
                return;
            }

            switch (phaseManager.CurrentPhase)
            {
                case UcgGamePhase.SceneSetup:
                    int sceneTurn = turnManager != null ? turnManager.currentTurn : 1;
                    if (IsDigaTutorialModeActive() && sceneTurn <= 1)
                    {
                        tutorialGuide.SetStep(UcgTutorialStep.SetupLane1);
                    }
                    else if (IsDigaTutorialModeActive() && sceneTurn == 2)
                    {
                        tutorialGuide.SetStep(UcgTutorialStep.SetupLane2);
                    }
                    else
                    {
                        tutorialGuide.SetStep(UcgTutorialStep.SceneSetup);
                    }
                    break;
                case UcgGamePhase.CharacterSetup:
                    int turn = turnManager != null ? turnManager.currentTurn : 1;
                    tutorialGuide.SetStep(turn >= 2 ? UcgTutorialStep.SetupLane2 : UcgTutorialStep.SetupLane1);
                    break;
                case UcgGamePhase.Upgrade:
                    tutorialGuide.SetStep(UcgTutorialStep.Upgrade);
                    break;
                case UcgGamePhase.Open:
                    tutorialGuide.SetStep(UcgTutorialStep.Open);
                    break;
                case UcgGamePhase.EnterEffect:
                    tutorialGuide.SetStep(UcgTutorialStep.Effect);
                    break;
                case UcgGamePhase.BattleEffect:
                    tutorialGuide.SetStep(UcgTutorialStep.Effect);
                    break;
                case UcgGamePhase.BattleJudgement:
                    tutorialGuide.SetStep(UcgTutorialStep.BattleJudgement);
                    break;
                case UcgGamePhase.End:
                    tutorialGuide.SetStep(UcgTutorialStep.WinCondition);
                    break;
            }
        }

        void ShowCurrentTestMode()
        {
            if (playResultText != null)
            {
                playResultText.text = $"目前模式：{GetTestModeName(currentTestMode)}";
            }
        }

        void EnsureCardHolder()
        {
            if (cardHolder == null)
            {
                Transform existingHolder = canvas.transform.Find("CardHolder");
                if (existingHolder != null)
                {
                    cardHolder = existingHolder as RectTransform;
                }
            }

            if (cardHolder == null)
            {
                var holderObject = new GameObject("CardHolder", typeof(RectTransform));
                holderObject.transform.SetParent(canvas.transform, false);
                cardHolder = holderObject.GetComponent<RectTransform>();
            }

            cardHolder.anchorMin = new Vector2(0f, 0f);
            cardHolder.anchorMax = new Vector2(1f, 0f);
            cardHolder.pivot = new Vector2(0.5f, 0.5f);
            float gameHandHeight = 500f;
            float gameHandBottomPadding = 16f;
            float gameHandHorizontalPadding = 0f;
            float gameHandWidth = 1080f;

            cardHolder.anchoredPosition = new Vector2(0f, gameHandBottomPadding + gameHandHeight * 0.5f);
            cardHolder.offsetMin = new Vector2(gameHandHorizontalPadding, gameHandBottomPadding);
            cardHolder.offsetMax = new Vector2(-gameHandHorizontalPadding, gameHandBottomPadding + gameHandHeight);
            cardHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(cardHolder.rect.width, gameHandWidth));
            cardHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gameHandHeight);
            EnsureHandZoneBackplate(cardHolder);

            var layout = cardHolder.GetComponent<UIHandLayout>();
            if (layout == null) layout = cardHolder.gameObject.AddComponent<UIHandLayout>();

            layout.rotateWithArc = true;
            layout.invertRotation = true;
            layout.invertY = false;
            layout.useSiblingOrder = true;
            layout.perItemExtraAngle = 0f;
            layout.adaptiveSpread = true;
            layout.useBottomBaseline = true;
            layout.smooth = true;
            layout.smoothSpeed = 16f;

            ApplyHandStyleByCount(cardHolder.childCount);
        }

        void EnsureHandZoneBackplate(RectTransform holderRect)
        {
            if (canvas == null || holderRect == null) return;

            const string panelName = "Hand Zone HUD Panel";
            Transform existingPanel = canvas.transform.Find(panelName);
            RectTransform panelRect;
            Image panelImage;

            if (existingPanel == null)
            {
                var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image), typeof(Outline));
                panelObject.transform.SetParent(canvas.transform, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelImage = panelObject.GetComponent<Image>();
            }
            else
            {
                panelRect = existingPanel as RectTransform;
                panelImage = existingPanel.GetComponent<Image>();
                if (panelImage == null) panelImage = existingPanel.gameObject.AddComponent<Image>();
                if (existingPanel.GetComponent<Outline>() == null) existingPanel.gameObject.AddComponent<Outline>();
            }

            panelRect.anchorMin = holderRect.anchorMin;
            panelRect.anchorMax = holderRect.anchorMax;
            panelRect.pivot = holderRect.pivot;
            panelRect.anchoredPosition = holderRect.anchoredPosition + new Vector2(0f, -18f);
            panelRect.offsetMin = holderRect.offsetMin + new Vector2(0f, -10f);
            panelRect.offsetMax = holderRect.offsetMax + new Vector2(0f, 16f);
            panelRect.localScale = Vector3.one;
            panelRect.localEulerAngles = Vector3.zero;
            panelRect.SetSiblingIndex(Mathf.Max(0, holderRect.GetSiblingIndex()));

            panelImage.color = new Color(0.015f, 0.035f, 0.055f, 0.22f);
            panelImage.raycastTarget = false;

            var outline = panelRect.GetComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.82f, 1f, 0.035f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        void ResetDeckAndBuildStartingHand()
        {
            if (deckManager != null)
            {
                deckManager.ResetDeck(currentTestMode);
            }

            ClearHand();
            if (ShouldUseDigaTutorialOpeningHand())
            {
                DrawTutorialOpeningHand();
            }
            else
            {
                DrawCardsToHand(DemoCardCount, "起手抽 6 張");
            }

            UpdateDeckCountText();
        }

        bool ShouldUseDigaTutorialOpeningHand()
        {
            return currentTestMode == UcgTestMode.UltramanTest
                && tutorialGuide != null
                && tutorialGuide.isTutorialMode
                && !tutorialGuide.tutorialCompleted;
        }

        void DrawTutorialOpeningHand()
        {
            if (deckManager == null) return;

            var drawnCards = deckManager.DrawDigaTutorialOpeningHand(DemoCardCount);
            for (int i = 0; i < drawnCards.Count; i++)
            {
                AddCardToHand(drawnCards[i]);
            }

            RefreshHandLayout();
            SyncOpponentZoneCountsFromDeckManager();
            UpdateDeckCountText();

            if (playResultText != null)
            {
                playResultText.text = drawnCards.Count > 0 ? "教學起手抽 6 張" : "牌組已空，無法抽牌";
            }
        }

        void BuildDemoHand()
        {
            ResetDeckAndBuildStartingHand();
        }

        void ClearHand()
        {
            _createdHandCardSerial = 0;

            for (int i = cardHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = cardHolder.GetChild(i);
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

        void DrawCardsToHand(int count, string resultMessage)
        {
            if (deckManager == null) return;

            var drawnCards = deckManager.DrawCards(count);
            for (int i = 0; i < drawnCards.Count; i++)
            {
                AddCardToHand(drawnCards[i]);
            }

            RefreshHandLayout();
            UpdateDeckCountText();

            if (playResultText != null && !string.IsNullOrEmpty(resultMessage))
            {
                playResultText.text = drawnCards.Count > 0 ? resultMessage : "牌組已空，無法抽牌";
            }
        }

        public bool DrawOneCardFromEffect()
        {
            return DrawCardsFromEffect(UcgPlayerSide.Player, 1, null) > 0;
        }

        public int DrawCardsFromEffect(UcgPlayerSide owner, int count, UcgCardData sourceCard)
        {
            if (IsGameOver || _isTutorialFinishWaitingForClick || count <= 0) return 0;
            if (deckManager == null)
            {
                Debug.LogWarning("DrawCards failed: deckManager is missing.");
                return 0;
            }

            int handBefore = owner == UcgPlayerSide.Player
                ? (deckManager.PlayerHand != null ? deckManager.PlayerHand.Count : 0)
                : (deckManager.OpponentHiddenHand != null ? deckManager.OpponentHiddenHand.Count : 0);
            int deckBefore = owner == UcgPlayerSide.Player
                ? deckManager.RemainingCount
                : deckManager.opponentDrawPile.Count;

            int drawnCount = 0;
            if (owner == UcgPlayerSide.Player)
            {
                var drawnCards = deckManager.DrawCards(count);
                drawnCount = drawnCards.Count;
                for (int i = 0; i < drawnCards.Count; i++)
                {
                    AddCardToHand(drawnCards[i]);
                }

                RefreshHandLayout();
                UpdateDeckCountText();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    UcgCardData drawnCard = deckManager.DrawOpponentCard();
                    if (drawnCard == null) break;
                    drawnCount++;
                }

                SyncOpponentZoneCountsFromDeckManager();
                RefreshZoneInfoUI();
            }

            int handAfter = owner == UcgPlayerSide.Player
                ? (deckManager.PlayerHand != null ? deckManager.PlayerHand.Count : 0)
                : (deckManager.OpponentHiddenHand != null ? deckManager.OpponentHiddenHand.Count : 0);
            int deckAfter = owner == UcgPlayerSide.Player
                ? deckManager.RemainingCount
                : deckManager.opponentDrawPile.Count;

            if (drawnCount == 0)
            {
                Debug.LogWarning($"DrawCards failed: owner={owner}, deck empty, source={FormatDrawSource(sourceCard)}");
            }
            else if (drawnCount < count)
            {
                Debug.LogWarning($"DrawCards partially resolved: owner={owner}, requested={count}, drawn={drawnCount}, source={FormatDrawSource(sourceCard)}");
            }

            Debug.Log(
                "DrawCards:\n"
                + $"source={FormatDrawSource(sourceCard)}\n"
                + $"owner={owner}\n"
                + $"count={count}\n"
                + $"drawn={drawnCount}\n"
                + $"handBefore={handBefore}\n"
                + $"handAfter={handAfter}\n"
                + $"deckBefore={deckBefore}\n"
                + $"deckAfter={deckAfter}");

            return drawnCount;
        }

        public bool TryGrantTemporaryTypeToLaneTarget(
            UcgBattleLane lane,
            UcgPlayerSide targetSide,
            string grantedType,
            UcgCardData sourceCard,
            out string targetName)
        {
            targetName = "";
            if (lane == null || string.IsNullOrWhiteSpace(grantedType)) return false;

            UcgCardData targetCard = GetLaneTopCard(lane, targetSide);
            if (targetCard == null) return false;

            if (!_temporaryTypeGrants.TryGetValue(targetCard, out List<string> grantedTypes) || grantedTypes == null)
            {
                grantedTypes = new List<string>();
                _temporaryTypeGrants[targetCard] = grantedTypes;
            }

            if (!ContainsTextInList(grantedTypes, grantedType))
            {
                grantedTypes.Add(grantedType);
            }

            targetName = !string.IsNullOrWhiteSpace(targetCard.cardName) ? targetCard.cardName : "角色";
            QueueEffectFeedback($"{targetName} 本回合獲得 TYPE {grantedType}");

            if (debugEffectResolution)
            {
                Debug.Log(
                    "Temporary type granted:\n"
                    + $"source={FormatDrawSource(sourceCard)}\n"
                    + $"target={targetCard.id} {targetCard.cardName}\n"
                    + $"targetSide={targetSide}\n"
                    + $"lane={lane.laneIndex + 1}\n"
                    + $"type={grantedType}");
            }

            return true;
        }

        bool CardHasEffectiveType(UcgCardData card, string typeKeyword)
        {
            if (card == null || string.IsNullOrWhiteSpace(typeKeyword)) return false;
            if (ContainsText(card.type, typeKeyword)) return true;
            if (!_temporaryTypeGrants.TryGetValue(card, out List<string> grantedTypes) || grantedTypes == null) return false;
            return ContainsTextInList(grantedTypes, typeKeyword);
        }

        bool ContainsTextInList(List<string> values, string keyword)
        {
            if (values == null || string.IsNullOrWhiteSpace(keyword)) return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (ContainsText(values[i], keyword)) return true;
            }

            return false;
        }

        public bool IsEffectConditionMet(UcgEffectInstance effect, UcgEffectConditionRule condition, out string message)
        {
            message = "";
            if (condition == null) return true;

            UcgPlayerSide sourceSide = effect != null ? effect.ownerSide : UcgPlayerSide.Player;
            UcgPlayerSide targetSide = GetConditionSide(sourceSide, condition.side);
            if (HasMatchingTopCharacterInPlay(targetSide, condition))
            {
                return true;
            }

            message = string.IsNullOrWhiteSpace(condition.failureMessage)
                ? "條件未達成，效果不發動。"
                : condition.failureMessage;

            if (debugEffectResolution)
            {
                Debug.Log(
                    "Effect condition not met:\n"
                    + $"source={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                    + $"owner={sourceSide}\n"
                    + $"conditionSide={condition.side}\n"
                    + $"targetSide={targetSide}\n"
                    + $"requiredTypes={FormatStringList(condition.requiredTypes)}\n"
                    + $"characterNameContains={condition.characterNameContains}");
            }

            return false;
        }

        UcgPlayerSide GetConditionSide(UcgPlayerSide sourceSide, UcgEffectConditionSide conditionSide)
        {
            switch (conditionSide)
            {
                case UcgEffectConditionSide.Opponent:
                    return sourceSide == UcgPlayerSide.Player ? UcgPlayerSide.Opponent : UcgPlayerSide.Player;
                case UcgEffectConditionSide.Self:
                case UcgEffectConditionSide.None:
                default:
                    return sourceSide;
            }
        }

        bool HasMatchingTopCharacterInPlay(UcgPlayerSide side, UcgEffectConditionRule condition)
        {
            if (battlefieldManager == null || condition == null) return false;

            List<UcgBattleLane> lanes = turnManager != null
                ? battlefieldManager.GetOpenedLanes(turnManager.currentTurn)
                : battlefieldManager.GetAllLanes();

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null) continue;

                UcgCardView topCard = side == UcgPlayerSide.Player
                    ? lane.playerPlayArea != null ? lane.playerPlayArea.GetTopCard() : null
                    : lane.GetOpponentTopCard();
                if (topCard == null || topCard.IsFaceDown || topCard.CardData == null) continue;
                if (DoesCardMatchEffectCondition(topCard.CardData, condition)) return true;
            }

            return false;
        }

        bool DoesCardMatchEffectCondition(UcgCardData card, UcgEffectConditionRule condition)
        {
            if (card == null || condition == null) return false;
            if (condition.requiredTypes != null && condition.requiredTypes.Count > 0
                && !CardTypeMatchesAny(card, condition.requiredTypes, ""))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(condition.characterNameContains))
            {
                return ContainsText(card.characterName, condition.characterNameContains)
                    || ContainsText(card.cardName, condition.characterNameContains);
            }

            return true;
        }

        bool MatchesAnyText(string source, List<string> allowedKeywords)
        {
            if (allowedKeywords == null || allowedKeywords.Count == 0) return true;

            for (int i = 0; i < allowedKeywords.Count; i++)
            {
                if (ContainsText(source, allowedKeywords[i])) return true;
            }

            return false;
        }

        bool ContainsText(string source, string keyword)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(keyword)) return false;
            return source.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        string FormatStringList(List<string> values)
        {
            if (values == null || values.Count == 0) return "";
            return string.Join(",", values);
        }

        public bool ResolveBp01105SceneActivatedEffect(UcgEffectInstance effect, out string message)
        {
            message = "場景發動效果處理完成。";
            if (effect == null || effect.cardData == null)
            {
                message = "場景效果來源不存在，效果略過。";
                return true;
            }

            UcgPlayerSide owner = effect.ownerSide;
            string sceneName = string.IsNullOrWhiteSpace(effect.cardData.cardName) ? "場景" : effect.cardData.cardName;
            if (!HasBlazarInPlay(owner))
            {
                message = $"{sceneName}：條件未達成，效果不發動。";
                ShowPlayStatus(message, 1.1f);
                QueueEffectFeedback(message);
                return true;
            }

            UcgRevealResult revealResult = RevealTopCards(owner, 5);
            if (revealResult.revealedCards.Count == 0)
            {
                message = $"{sceneName}：牌庫已空，無法公開。";
                ShowPlayStatus(message, 1.1f);
                QueueEffectFeedback(message);
                RefreshZoneInfoUI();
                return true;
            }

            if (owner == UcgPlayerSide.Player)
            {
                List<UcgCardData> legalCards = GetLegalBp01105RevealCards(owner, revealResult.revealedCards);
                if (legalCards.Count > 0)
                {
                    BeginBp01105SceneRevealSelection(effect, revealResult.revealedCards, legalCards);
                    message = $"{sceneName}：公開 {revealResult.revealedCards.Count} 張，請選擇可升級的超人力霸王。";
                    ShowPlayStatus(message, 1.2f);
                    return true;
                }
            }

            UcgCardData selectedCard;
            UcgBattleLane targetLane;
            bool hasTarget = TryFindBp01105TemporaryUpgradeTarget(owner, revealResult.revealedCards, out selectedCard, out targetLane);
            int sentToTrash = 0;
            for (int i = 0; i < revealResult.revealedCards.Count; i++)
            {
                UcgCardData card = revealResult.revealedCards[i];
                if (hasTarget && ReferenceEquals(card, selectedCard)) continue;
                ApplyDeckOperationDestination(owner, card, UcgDeckOperationDestination.Trash);
                sentToTrash++;
            }

            if (!hasTarget || selectedCard == null || targetLane == null)
            {
                message = $"{sceneName}：公開 {revealResult.revealedCards.Count} 張，但沒有可登場的超人力霸王；公開卡送入棄牌區。";
                ShowPlayStatus(message, 1.2f);
                QueueEffectFeedback(message);
                RefreshZoneInfoUI();
                LogBp01105SceneEffect(effect, revealResult.revealedCards, null, null, sentToTrash, "no legal upgrade target");
                return true;
            }

            if (!PlaceBp01105TemporaryUpgrade(owner, targetLane, selectedCard, effect.cardData))
            {
                ApplyDeckOperationDestination(owner, selectedCard, UcgDeckOperationDestination.Trash);
                sentToTrash++;
                message = $"{sceneName}：{selectedCard.cardName} 登場失敗，公開卡送入棄牌區。";
                ShowPlayStatus(message, 1.2f);
                QueueEffectFeedback(message);
                RefreshZoneInfoUI();
                LogBp01105SceneEffect(effect, revealResult.revealedCards, selectedCard, targetLane, sentToTrash, "placement failed");
                return true;
            }

            string selectedName = string.IsNullOrWhiteSpace(selectedCard.cardName) ? selectedCard.id : selectedCard.cardName;
            message = $"{sceneName}：公開 {revealResult.revealedCards.Count} 張，{selectedName} 登場到第 {targetLane.laneIndex + 1} 路；其餘 {sentToTrash} 張送入棄牌區。回合結束時會棄置該卡。";
            ShowPlayStatus(message, 1.3f);
            QueueEffectFeedback(message);
            RefreshZoneInfoUI();
            RefreshInteractionHints();
            LogBp01105SceneEffect(effect, revealResult.revealedCards, selectedCard, targetLane, sentToTrash, "resolved");
            return true;
        }

        void BeginBp01105SceneRevealSelection(
            UcgEffectInstance effect,
            List<UcgCardData> revealedCards,
            List<UcgCardData> legalCards)
        {
            StopDeckOperationNoValidAutoCloseRoutine();
            EnsureDeckOperationSelectionUI();
            RestoreDeckOperationSelectionUIForRevealCards();
            ClearDeckOperationCards();

            _pendingBp01105Effect = effect;
            _pendingBp01105SelectedCard = null;
            _pendingBp01105RevealedCards.Clear();
            if (revealedCards != null)
            {
                _pendingBp01105RevealedCards.AddRange(revealedCards);
            }

            _isSelectingDeckOperationCard = true;
            _pendingDeckSelection = new UcgCardSelectionContext
            {
                sourceEffect = effect,
                rule = new UcgDeckOperationRule
                {
                    selectionFilter = UcgDeckSelectionFilter.UltramanCard,
                    selectCount = 1
                },
                owner = UcgPlayerSide.Player,
                sourceZone = UcgDeckOperationSourceZone.SceneRevealCards
            };
            _pendingDeckSelection.revealedCards.AddRange(_pendingBp01105RevealedCards);

            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();

            if (_deckOperationSelectionTitle != null)
            {
                string sceneName = effect != null && effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                    ? effect.cardData.cardName
                    : "場景效果";
                _deckOperationSelectionTitle.text = $"{sceneName}\n選擇 1 張可合法升級到我方 Lane 的超人力霸王";
            }

            for (int i = 0; i < _pendingBp01105RevealedCards.Count; i++)
            {
                UcgCardData card = _pendingBp01105RevealedCards[i];
                bool canSelect = ContainsCardReference(legalCards, card);
                CreateDeckOperationCardButton(card, i, _pendingBp01105RevealedCards.Count, canSelect, false);
            }

            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
                _deckOperationNoSelectionButton.onClick.RemoveAllListeners();
            }

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(true);
                _deckOperationSelectionRoot.SetAsLastSibling();
            }

            RefreshZoneInfoUI();
            RefreshNextPhaseButtonState();
        }

        void CompleteBp01105SceneRevealSelection(UcgCardData selectedCard)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) return;
            if (_pendingDeckSelection.sourceZone != UcgDeckOperationSourceZone.SceneRevealCards) return;
            if (_pendingBp01105Effect == null || selectedCard == null)
            {
                CompleteBp01105SceneRevealWithoutSelection("場景效果選擇狀態已失效，公開卡送入棄牌區。", "selection state missing");
                return;
            }

            if (!IsUltramanCard(selectedCard) || !HasLegalBp01105LaneForCard(_pendingBp01105Effect.ownerSide, selectedCard))
            {
                ShowPlayStatus("只能選擇可合法升級的超人力霸王。", 1.1f);
                return;
            }

            _pendingBp01105SelectedCard = selectedCard;
            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection.selectedCard = selectedCard;
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            _isSelectingEffectTarget = true;
            _pendingTargetEffect = _pendingBp01105Effect;
            _pendingTargetType = UcgEffectTargetType.OwnLane;
            _pendingSwapSourceLane = null;
            _pendingBp05005StepDownLane = null;
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();
            HighlightEffectTargets();

            if (playResultText != null)
            {
                string selectedName = string.IsNullOrWhiteSpace(selectedCard.cardName) ? selectedCard.id : selectedCard.cardName;
                playResultText.text = $"已選擇 {selectedName}。請選擇要升級的我方 Lane。";
            }
            ShowPlayStatus("請選擇要升級的我方 Lane。", 1.1f);
            UpdateMainPrompt();
        }

        void FinishBp01105SceneUpgradeSelection(UcgBattleLane targetLane)
        {
            UcgEffectInstance effect = _pendingBp01105Effect;
            UcgCardData selectedCard = _pendingBp01105SelectedCard;
            var revealedCards = new List<UcgCardData>(_pendingBp01105RevealedCards);
            UcgPlayerSide owner = effect != null ? effect.ownerSide : UcgPlayerSide.Player;
            string sceneName = effect != null && effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                ? effect.cardData.cardName
                : "場景";

            int sentToTrash = SendBp01105UnselectedRevealedCardsToTrash(owner, revealedCards, selectedCard);
            bool placed = selectedCard != null && PlaceBp01105TemporaryUpgrade(owner, targetLane, selectedCard, effect != null ? effect.cardData : null);
            if (!placed)
            {
                if (selectedCard != null)
                {
                    ApplyDeckOperationDestination(owner, selectedCard, UcgDeckOperationDestination.Trash);
                    sentToTrash++;
                }

                string failedMessage = $"{sceneName}：{(selectedCard != null ? selectedCard.cardName : "公開卡")} 登場失敗，公開卡送入棄牌區。";
                LogBp01105SceneEffect(effect, revealedCards, selectedCard, targetLane, sentToTrash, "placement failed after manual selection");
                CleanupBp01105SelectionState();
                ClearEffectTargetSelection();
                RefreshZoneInfoUI();
                QueueEffectFeedback(failedMessage);
                ContinueAfterTargetEffectResolved(true, failedMessage);
                return;
            }

            string selectedName = string.IsNullOrWhiteSpace(selectedCard.cardName) ? selectedCard.id : selectedCard.cardName;
            string message = $"{sceneName}：公開 {revealedCards.Count} 張，{selectedName} 登場到第 {targetLane.laneIndex + 1} 路；其餘 {sentToTrash} 張送入棄牌區。回合結束時會棄置該卡。";
            LogBp01105SceneEffect(effect, revealedCards, selectedCard, targetLane, sentToTrash, "manual resolved");
            CleanupBp01105SelectionState();
            ClearEffectTargetSelection();
            RefreshZoneInfoUI();
            RefreshHandLayout();
            QueueEffectFeedback(message);
            ContinueAfterTargetEffectResolved(true, message);
        }

        void CompleteBp01105SceneRevealWithoutSelection(string message, string logResult)
        {
            UcgEffectInstance effect = _pendingBp01105Effect;
            UcgPlayerSide owner = effect != null ? effect.ownerSide : UcgPlayerSide.Player;
            var revealedCards = new List<UcgCardData>(_pendingBp01105RevealedCards);
            int sentToTrash = SendBp01105UnselectedRevealedCardsToTrash(owner, revealedCards, null);
            LogBp01105SceneEffect(effect, revealedCards, null, null, sentToTrash, logResult);
            CleanupBp01105SelectionState();
            ClearEffectTargetSelection();
            RefreshZoneInfoUI();
            QueueEffectFeedback(message);
            ContinueAfterTargetEffectResolved(true, message);
        }

        int SendBp01105UnselectedRevealedCardsToTrash(UcgPlayerSide owner, List<UcgCardData> revealedCards, UcgCardData selectedCard)
        {
            int sentToTrash = 0;
            if (revealedCards == null) return sentToTrash;

            for (int i = 0; i < revealedCards.Count; i++)
            {
                UcgCardData card = revealedCards[i];
                if (card == null) continue;
                if (selectedCard != null && ReferenceEquals(card, selectedCard)) continue;
                ApplyDeckOperationDestination(owner, card, UcgDeckOperationDestination.Trash);
                sentToTrash++;
            }

            return sentToTrash;
        }

        void CleanupBp01105SelectionState()
        {
            ClearBp01105PendingSelectionState();
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();
            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshNextPhaseButtonState();
            RefreshInteractionHints();
        }

        void ClearBp01105PendingSelectionState()
        {
            _pendingBp01105Effect = null;
            _pendingBp01105SelectedCard = null;
            _pendingBp01105RevealedCards.Clear();
        }

        List<UcgCardData> GetLegalBp01105RevealCards(UcgPlayerSide owner, List<UcgCardData> revealedCards)
        {
            var result = new List<UcgCardData>();
            if (revealedCards == null) return result;

            for (int i = 0; i < revealedCards.Count; i++)
            {
                UcgCardData card = revealedCards[i];
                if (card == null || !IsUltramanCard(card)) continue;
                if (!HasLegalBp01105LaneForCard(owner, card)) continue;
                result.Add(card);
            }

            return result;
        }

        bool HasLegalBp01105LaneForCard(UcgPlayerSide owner, UcgCardData card)
        {
            return FindFirstBp01105LaneForCard(owner, card) != null;
        }

        UcgBattleLane FindFirstBp01105LaneForCard(UcgPlayerSide owner, UcgCardData card)
        {
            if (battlefieldManager == null || card == null) return null;

            List<UcgBattleLane> lanes = turnManager != null
                ? battlefieldManager.GetOpenedLanes(turnManager.currentTurn)
                : battlefieldManager.GetAllLanes();
            if (lanes == null) return null;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (IsLegalBp01105UpgradeTarget(owner, lane, card)) return lane;
            }

            return null;
        }

        bool ContainsCardReference(List<UcgCardData> cards, UcgCardData target)
        {
            return GetReferenceIndex(cards, target) >= 0;
        }

        int GetReferenceIndex(List<UcgCardData> cards, UcgCardData target)
        {
            if (cards == null || target == null) return -1;
            for (int i = 0; i < cards.Count; i++)
            {
                if (ReferenceEquals(cards[i], target)) return i;
            }

            return -1;
        }

        bool HasBlazarInPlay(UcgPlayerSide owner)
        {
            if (battlefieldManager == null) return false;
            List<UcgBattleLane> lanes = turnManager != null
                ? battlefieldManager.GetOpenedLanes(turnManager.currentTurn)
                : battlefieldManager.GetAllLanes();
            if (lanes == null) return false;

            for (int i = 0; i < lanes.Count; i++)
            {
                if (CardCharacterContains(GetLaneTopCard(lanes[i], owner), "布雷薩")) return true;
            }

            return false;
        }

        bool TryFindBp01105TemporaryUpgradeTarget(
            UcgPlayerSide owner,
            List<UcgCardData> revealedCards,
            out UcgCardData selectedCard,
            out UcgBattleLane targetLane)
        {
            selectedCard = null;
            targetLane = null;
            if (battlefieldManager == null || revealedCards == null) return false;

            List<UcgBattleLane> lanes = turnManager != null
                ? battlefieldManager.GetOpenedLanes(turnManager.currentTurn)
                : battlefieldManager.GetAllLanes();
            if (lanes == null || lanes.Count == 0) return false;

            for (int i = 0; i < revealedCards.Count; i++)
            {
                UcgCardData card = revealedCards[i];
                if (card == null || !IsUltramanCard(card)) continue;
                for (int j = 0; j < lanes.Count; j++)
                {
                    if (!IsLegalBp01105UpgradeTarget(owner, lanes[j], card)) continue;
                    selectedCard = card;
                    targetLane = lanes[j];
                    return true;
                }
            }

            return false;
        }

        bool IsLegalBp01105UpgradeTarget(UcgPlayerSide owner, UcgBattleLane lane, UcgCardData candidate)
        {
            if (lane == null || candidate == null || !IsUltramanCard(candidate)) return false;
            UcgCardData topCard = GetLaneTopCard(lane, owner);
            if (topCard == null) return false;

            if (!UcgActionValidator.CanPlayOrUpgrade(candidate, topCard, out _, out UcgPlayActionType actionType))
            {
                return false;
            }

            return actionType == UcgPlayActionType.Upgrade;
        }

        bool PlaceBp01105TemporaryUpgrade(UcgPlayerSide owner, UcgBattleLane lane, UcgCardData card, UcgCardData sourceSceneCard)
        {
            if (lane == null || card == null) return false;

            Sprite fallbackSprite = GetTestCardSprite(owner == UcgPlayerSide.Player ? 0 : 1);
            UcgCardView view = lane.UpgradeCardFromEffect(
                owner,
                card,
                cardInfoPanel,
                fallbackSprite,
                GetPlacedBattleCardSize(),
                LoadPlaceholderFont());
            if (view == null) return false;

            _temporarySceneSummons.Add(new UcgTemporarySceneSummon
            {
                lane = lane,
                ownerSide = owner,
                cardData = card,
                sourceSceneCard = sourceSceneCard,
                turnNumber = turnManager != null ? turnManager.currentTurn : 0
            });
            return true;
        }

        void DiscardTemporarySceneSummonsForTurn(int turnNumber)
        {
            if (_temporarySceneSummons.Count == 0) return;

            int discardedCount = 0;
            for (int i = _temporarySceneSummons.Count - 1; i >= 0; i--)
            {
                UcgTemporarySceneSummon entry = _temporarySceneSummons[i];
                if (entry == null)
                {
                    _temporarySceneSummons.RemoveAt(i);
                    continue;
                }
                if (entry.turnNumber > 0 && turnNumber > 0 && entry.turnNumber != turnNumber) continue;

                UcgBattleLane lane = FindLaneWithTopCard(entry.cardData, entry.ownerSide);
                if (lane == null) lane = entry.lane;

                if (lane != null && lane.RemoveTopCardFromEffect(entry.ownerSide, entry.cardData, out UcgCardData removedCard))
                {
                    List<UcgCardData> discardPile = GetDiscardPileForOwner(entry.ownerSide);
                    if (discardPile != null && removedCard != null)
                    {
                        discardPile.Add(removedCard);
                        _temporaryTypeGrants.Remove(removedCard);
                        discardedCount++;
                    }
                }

                _temporarySceneSummons.RemoveAt(i);
            }

            if (discardedCount > 0)
            {
                RefreshZoneInfoUI();
                QueueEffectFeedback($"場景效果：回合結束，臨時登場的角色送入棄牌區。");
                if (debugEffectResolution)
                {
                    Debug.Log($"BP01-105 temporary summons discarded: count={discardedCount}");
                }
            }
        }

        UcgBattleLane FindLaneWithTopCard(UcgCardData card, UcgPlayerSide owner)
        {
            if (card == null || battlefieldManager == null) return null;
            List<UcgBattleLane> lanes = battlefieldManager.GetAllLanes();
            if (lanes == null) return null;

            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (ReferenceEquals(GetLaneTopCard(lane, owner), card)) return lane;
            }

            return null;
        }

        void LogBp01105SceneEffect(
            UcgEffectInstance effect,
            List<UcgCardData> revealedCards,
            UcgCardData selectedCard,
            UcgBattleLane targetLane,
            int sentToTrash,
            string result)
        {
            if (!debugEffectResolution) return;

            Debug.Log(
                "BP01-105 scene effect:\n"
                + $"source={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                + $"owner={(effect != null ? effect.ownerSide.ToString() : "unknown")}\n"
                + $"revealed={FormatCardIdList(revealedCards)}\n"
                + $"selected={FormatDrawSource(selectedCard)}\n"
                + $"targetLane={(targetLane != null ? (targetLane.laneIndex + 1).ToString() : "none")}\n"
                + $"sentToTrash={sentToTrash}\n"
                + $"temporarySummons={_temporarySceneSummons.Count}\n"
                + $"result={result}");
        }

        public bool ResolveDeckOperationFromEffect(UcgEffectInstance effect, UcgDeckOperationRule rule, out string message)
        {
            message = "牌庫操作暫不支援";
            if (IsGameOver || _isTutorialFinishWaitingForClick || deckManager == null || effect == null || rule == null)
            {
                return false;
            }

            UcgPlayerSide owner = effect.ownerSide;
            List<UcgCardData> drawPile = GetDrawPileForOwner(owner);
            if (drawPile == null)
            {
                Debug.LogWarning($"DeckOperation failed: missing draw pile, owner={owner}, source={FormatDrawSource(effect.cardData)}");
                return false;
            }

            int handBefore = GetHandCountForOwner(owner);
            int deckBefore = drawPile.Count;
            if (rule.operationType == UcgDeckOperationType.SelectHandToBottomThenDrawSameCount)
            {
                return ResolveSelectHandToBottomThenDrawSameCount(effect, rule, owner, handBefore, deckBefore, out message);
            }

            if (rule.operationType == UcgDeckOperationType.DrawThenPutHandToBottom)
            {
                return ResolveDrawThenPutHandToBottom(effect, rule, owner, handBefore, deckBefore, out message);
            }

            UcgRevealResult revealResult = RevealTopCards(owner, Mathf.Max(1, rule.revealCount));
            if (revealResult.revealedCards.Count == 0)
            {
                Debug.LogWarning($"DeckOperation failed: deck empty, owner={owner}, source={FormatDrawSource(effect.cardData)}");
                message = "牌庫已空，無法處理效果";
                return true;
            }

            if (owner == UcgPlayerSide.Opponent)
            {
                ResolveOpponentDeckOperation(effect, rule, revealResult.revealedCards, handBefore, deckBefore);
                message = $"對手處理牌庫效果：公開 {revealResult.revealedCards.Count} 張";
                return true;
            }

            BeginDeckOperationSelection(effect, rule, revealResult.revealedCards, handBefore, deckBefore);
            message = "請選擇要處理的牌";
            return true;
        }

        bool ResolveDrawThenPutHandToBottom(UcgEffectInstance effect, UcgDeckOperationRule rule, UcgPlayerSide owner, int handBefore, int deckBefore, out string message)
        {
            message = "請選擇 1 張手牌放到牌庫最底下";
            int drawCount = Mathf.Max(1, rule.drawCount);
            var drawnCards = new List<UcgCardData>();
            bool isBlazarDrawBottomEffect = effect != null
                && effect.cardData != null
                && effect.cardData.id == "BP01-037";

            if (ShouldDebugDeckOperation(effect) && isBlazarDrawBottomEffect)
            {
                UcgEffectRule parsedRule = UcgEffectParser.ParsePrimaryRule(effect.cardData);
                int stackCount = effect.lane != null && owner == UcgPlayerSide.Player && effect.lane.playerPlayArea != null
                    ? effect.lane.playerPlayArea.GetStackCount()
                    : effect.lane != null
                        ? effect.lane.GetOpponentStackCount()
                        : 0;
                Debug.Log(
                    "BP01-037 effect check:\n"
                    + $"rawEffect={GetEffectText(effect.cardData)}\n"
                    + $"parsedCategory={(parsedRule != null ? parsedRule.effectCategory.ToString() : "null")}\n"
                    + $"parsedOperation={(parsedRule != null && parsedRule.deckOperation != null ? parsedRule.deckOperation.operationType.ToString() : "null")}\n"
                    + $"drawCount={drawCount}\n"
                    + $"handSelectCount={(rule != null ? rule.handSelectCount : 0)}\n"
                    + $"stackCount={stackCount}\n"
                    + $"canTrigger=true\n"
                    + $"triggerPhase={(phaseManager != null ? phaseManager.CurrentPhase.ToString() : "unknown")}");
            }

            if (owner == UcgPlayerSide.Player)
            {
                ShowPlayStatus("布雷薩登場效果：抽 2 張牌");
                QueueEffectFeedback("布雷薩登場效果：抽 2 張牌");

                var drawn = deckManager.DrawCards(drawCount);
                drawnCards.AddRange(drawn);
                for (int i = 0; i < drawn.Count; i++)
                {
                    AddCardToHand(drawn[i]);
                }

                RefreshHandLayout();
                RefreshZoneInfoUI();
                int handAfterDraw = GetHandCountForOwner(owner);
                int deckAfterDraw = GetDrawPileForOwner(owner) != null ? GetDrawPileForOwner(owner).Count : 0;
                if (drawnCards.Count == 0)
                {
                    LogBp01037Execute(effect, drawnCards, null, handBefore, handAfterDraw, deckBefore, deckAfterDraw, false, false, "no cards drawn");
                    message = "牌庫已空，無法抽牌";
                    return true;
                }

                if (deckManager.playerHand.Count == 0)
                {
                    LogBp01037Execute(effect, drawnCards, null, handBefore, handAfterDraw, deckBefore, deckAfterDraw, false, false, "player hand empty after draw");
                    message = "手牌為空，無法放回牌庫底";
                    return true;
                }

                BeginDeckOperationHandSelection(effect, rule, handBefore, deckBefore, drawnCards);
                LogBp01037Execute(effect, drawnCards, null, handBefore, handAfterDraw, deckBefore, deckAfterDraw, true, false, "");
                message = "請從手牌中選擇 1 張放回牌庫最底下";
                return true;
            }

            for (int i = 0; i < drawCount; i++)
            {
                UcgCardData drawnCard = deckManager.DrawOpponentCard();
                if (drawnCard == null) break;
                drawnCards.Add(drawnCard);
            }

            UcgCardData selectedCard = deckManager.opponentHiddenHand.Count > 0 ? deckManager.opponentHiddenHand[0] : null;
            if (selectedCard != null)
            {
                deckManager.opponentHiddenHand.Remove(selectedCard);
                deckManager.opponentDrawPile.Add(selectedCard);
            }

            SyncOpponentZoneCountsFromDeckManager();
            RefreshZoneInfoUI();
            LogDrawThenPutHandToBottom(effect, rule, owner, drawnCards, selectedCard, handBefore, deckBefore);
            message = "對手處理牌庫效果：抽牌後放 1 張到牌庫底";
            return true;
        }

        bool ResolveSelectHandToBottomThenDrawSameCount(UcgEffectInstance effect, UcgDeckOperationRule rule, UcgPlayerSide owner, int handBefore, int deckBefore, out string message)
        {
            int maxSelectCount = GetMaxHandSelectionCount(rule);
            message = $"選擇最多 {maxSelectCount} 張手牌放回牌庫底，然後抽同等數量。";
            if (maxSelectCount <= 0)
            {
                message = "沒有可處理的手牌張數，效果結束。";
                return true;
            }

            if (owner == UcgPlayerSide.Player)
            {
                if (deckManager == null || deckManager.playerHand == null || deckManager.playerHand.Count == 0)
                {
                    message = "目前沒有手牌可放回牌庫底，效果結束。";
                    ShowPlayStatus(message, 1.1f);
                    QueueEffectFeedback(message);
                    return true;
                }

                BeginDeckOperationHandSelection(effect, rule, handBefore, deckBefore, null);
                return true;
            }

            int selectedCount = Mathf.Min(maxSelectCount, deckManager.opponentHiddenHand.Count);
            var returnedCards = new List<UcgCardData>();
            for (int i = 0; i < selectedCount; i++)
            {
                UcgCardData selectedCard = deckManager.opponentHiddenHand.Count > 0 ? deckManager.opponentHiddenHand[0] : null;
                if (selectedCard == null) break;
                deckManager.opponentHiddenHand.RemoveAt(0);
                deckManager.opponentDrawPile.Add(selectedCard);
                returnedCards.Add(selectedCard);
            }

            var drawnCards = new List<UcgCardData>();
            for (int i = 0; i < returnedCards.Count; i++)
            {
                UcgCardData drawnCard = deckManager.DrawOpponentCard();
                if (drawnCard == null) break;
                drawnCards.Add(drawnCard);
            }

            SyncOpponentZoneCountsFromDeckManager();
            RefreshZoneInfoUI();
            LogSelectHandToBottomThenDrawSameCount(effect, rule, owner, returnedCards, drawnCards, handBefore, deckBefore);
            message = returnedCards.Count > 0
                ? $"對手處理牌庫效果：放回 {returnedCards.Count} 張後抽 {drawnCards.Count} 張"
                : "對手沒有手牌可放回，效果結束";
            return true;
        }

        List<UcgCardData> GetDrawPileForOwner(UcgPlayerSide owner)
        {
            if (deckManager == null) return null;
            return owner == UcgPlayerSide.Player ? deckManager.deck : deckManager.opponentDrawPile;
        }

        int GetHandCountForOwner(UcgPlayerSide owner)
        {
            if (deckManager == null) return 0;
            return owner == UcgPlayerSide.Player ? deckManager.playerHand.Count : deckManager.opponentHiddenHand.Count;
        }

        UcgRevealResult RevealTopCards(UcgPlayerSide owner, int count)
        {
            var result = new UcgRevealResult();
            List<UcgCardData> drawPile = GetDrawPileForOwner(owner);
            if (drawPile == null) return result;

            result.drawPileBefore = drawPile.Count;
            int revealCount = Mathf.Min(count, drawPile.Count);
            for (int i = 0; i < revealCount; i++)
            {
                UcgCardData card = drawPile[0];
                drawPile.RemoveAt(0);
                if (card != null)
                {
                    result.revealedCards.Add(card);
                }
            }

            result.drawPileAfterReveal = drawPile.Count;
            return result;
        }

        void BeginDeckOperationSelection(UcgEffectInstance effect, UcgDeckOperationRule rule, List<UcgCardData> revealedCards, int handBefore, int deckBefore)
        {
            if (effect != null && effect.cardData != null && effect.cardData.id == "BP01-037")
            {
                Debug.LogWarning("BP01-037 should not use RevealTopSelect UI.");
            }

            StopDeckOperationNoValidAutoCloseRoutine();
            if (_deckOperationSelectionRoot == null)
            {
                EnsureDeckOperationSelectionUI();
            }
            RestoreDeckOperationSelectionUIForRevealCards();

            _isSelectingDeckOperationCard = true;
            _pendingDeckSelection = new UcgCardSelectionContext
            {
                sourceEffect = effect,
                rule = rule,
                owner = UcgPlayerSide.Player,
                sourceZone = UcgDeckOperationSourceZone.RevealedCards,
                handBefore = handBefore,
                deckBefore = deckBefore
            };
            _pendingDeckSelection.revealedCards.AddRange(revealedCards);

            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();
            ClearDeckOperationCards();

            if (_deckOperationSelectionTitle != null)
            {
                _deckOperationSelectionTitle.text = BuildDeckOperationSelectionTitle(effect, rule, revealedCards.Count);
            }

            List<UcgCardData> selectableCards = GetSelectableDeckOperationCards(revealedCards, rule.selectionFilter);
            bool noValidSelection = selectableCards.Count == 0;
            if (noValidSelection && _deckOperationSelectionTitle != null)
            {
                _deckOperationSelectionTitle.text = BuildDeckOperationNoSelectionTitle(effect, rule, revealedCards.Count);
            }

            for (int i = 0; i < revealedCards.Count; i++)
            {
                bool canSelect = IsCardAllowedByDeckSelectionFilter(revealedCards[i], rule.selectionFilter);
                CreateDeckOperationCardButton(revealedCards[i], i, revealedCards.Count, canSelect, false);
            }

            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.onClick.RemoveAllListeners();
                _deckOperationNoSelectionButton.onClick.AddListener(CompleteDeckOperationNoSelection);
                EnsureButtonLabel(_deckOperationNoSelectionButton.transform as RectTransform, "確認");
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }

            _deckOperationSelectionRoot.gameObject.SetActive(true);
            _deckOperationSelectionRoot.SetAsLastSibling();
            ShowPlayStatus(noValidSelection
                ? GetDeckOperationNoValidSelectionMessage(rule.selectionFilter)
                : GetDeckOperationSelectionPrompt(rule.selectionFilter));
            RefreshNextPhaseButtonState();
            LogRevealTopSelectUiOpened(effect, rule, revealedCards, selectableCards, noValidSelection);
            if (debugEffectResolution)
            {
                Canvas selectionCanvas = _deckOperationSelectionRoot.GetComponent<Canvas>();
                Debug.Log(
                    "DeckOperation selection UI opened:\n"
                    + $"modalActive={_deckOperationSelectionRoot.gameObject.activeInHierarchy}\n"
                    + $"sortingOrder={(selectionCanvas != null ? selectionCanvas.sortingOrder : 0)}\n"
                    + $"revealedCardCount={revealedCards.Count}\n"
                    + $"cardsRootChildren={(_deckOperationCardsRoot != null ? _deckOperationCardsRoot.childCount : 0)}");
            }

            if (noValidSelection)
            {
                _deckOperationNoValidAutoCloseRoutine = StartCoroutine(AutoCompleteDeckOperationNoSelectionAfterDelay(effect));
            }
        }

        void RestoreDeckOperationSelectionUIForRevealCards()
        {
            if (_deckOperationSelectionRoot == null) return;

            Image rootImage = _deckOperationSelectionRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.46f);
                rootImage.raycastTarget = true;
            }

            Transform panelTransform = _deckOperationSelectionRoot.Find("Selection Panel");
            RectTransform panelRect = panelTransform as RectTransform;
            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(GetRevealSelectionOffsetX(), -40f);
                panelRect.sizeDelta = new Vector2(900f, 420f);
                Image panelImage = panelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.24f);
                    panelImage.raycastTarget = true;
                }
            }

            if (_deckOperationCardsRoot != null)
            {
                _deckOperationCardsRoot.gameObject.SetActive(true);
            }
        }

        void BeginDeckOperationHandSelection(UcgEffectInstance effect, UcgDeckOperationRule rule, int handBefore, int deckBefore, List<UcgCardData> drawnCards)
        {
            _isSelectingDeckOperationCard = true;
            _pendingDeckSelection = new UcgCardSelectionContext
            {
                sourceEffect = effect,
                rule = rule,
                owner = UcgPlayerSide.Player,
                sourceZone = UcgDeckOperationSourceZone.Hand,
                handBefore = handBefore,
                handAfterDraw = GetHandCountForOwner(UcgPlayerSide.Player),
                deckBefore = deckBefore
            };
            _pendingDeckSelection.revealedCards.AddRange(deckManager.playerHand);
            if (drawnCards != null)
            {
                _pendingDeckSelection.drawnCards.AddRange(drawnCards);
            }

            if (IsSelectHandToBottomThenDrawSameCountRule(rule))
            {
                ShowDeckOperationHandSelectionConfirmUI(effect);
            }
            else
            {
                HideDeckOperationSelectionUIForHandReturn(effect);
            }
            SetHandCardsInteractable(false, null);
            SetNextPhaseButtonInteractable(false);
            ClearInteractionHints();
            ApplyHandReturnSelectionHighlights(true);
            ShowPlayStatus(GetHandSelectionPrompt(rule));
            RefreshNextPhaseButtonState();
            if (ShouldDebugDeckOperation(effect) && effect != null && effect.cardData != null && effect.cardData.id == "BP01-037")
            {
                Debug.Log(
                    "BP01-037 hand selection opened:\n"
                    + "selectionSource=Hand\n"
                    + $"modalActive={(_deckOperationSelectionRoot != null && _deckOperationSelectionRoot.gameObject.activeInHierarchy)}\n"
                    + $"handCards={_pendingDeckSelection.revealedCards.Count}\n"
                    + $"drawnCards={FormatCardIdList(drawnCards)}\n"
                    + $"drawPileCount={(deckManager != null ? deckManager.deck.Count : 0)}");
            }
        }

        void ShowDeckOperationHandSelectionConfirmUI(UcgEffectInstance effect)
        {
            EnsureDeckOperationSelectionUI();
            if (_deckOperationSelectionRoot == null) return;

            _deckOperationSelectionRoot.gameObject.SetActive(true);
            _deckOperationSelectionRoot.SetAsLastSibling();

            Image rootImage = _deckOperationSelectionRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0f, 0f, 0f, 0f);
                rootImage.raycastTarget = false;
            }

            Transform panelTransform = _deckOperationSelectionRoot.Find("Selection Panel");
            RectTransform panelRect = panelTransform as RectTransform;
            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(GetRevealSelectionOffsetX(), -92f);
                panelRect.sizeDelta = new Vector2(720f, 168f);
                Image panelImage = panelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.72f);
                    panelImage.raycastTarget = false;
                }
            }

            if (_deckOperationCardsRoot != null)
            {
                _deckOperationCardsRoot.gameObject.SetActive(false);
            }

            if (_deckOperationSelectionTitle != null)
            {
                string cardName = effect != null && effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                    ? effect.cardData.cardName
                    : "效果";
                _deckOperationSelectionTitle.text = $"{cardName}\n{GetHandSelectionPrompt(_pendingDeckSelection != null ? _pendingDeckSelection.rule : null)}";
            }

            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(true);
                _deckOperationNoSelectionButton.onClick.RemoveAllListeners();
                _deckOperationNoSelectionButton.onClick.AddListener(CompleteDeckOperationHandSelectionConfirm);
                UpdateHandSelectionConfirmButtonLabel();
            }
        }

        bool IsSelectHandToBottomThenDrawSameCountRule(UcgDeckOperationRule rule)
        {
            return rule != null && rule.operationType == UcgDeckOperationType.SelectHandToBottomThenDrawSameCount;
        }

        int GetMinHandSelectionCount(UcgDeckOperationRule rule)
        {
            if (rule == null) return 0;
            return Mathf.Max(0, rule.minHandSelectCount);
        }

        int GetMaxHandSelectionCount(UcgDeckOperationRule rule)
        {
            if (rule == null) return 0;
            int configuredCount = rule.handSelectCount > 0 ? rule.handSelectCount : rule.selectCount;
            return Mathf.Max(GetMinHandSelectionCount(rule), configuredCount);
        }

        string GetHandSelectionPrompt(UcgDeckOperationRule rule)
        {
            if (IsSelectHandToBottomThenDrawSameCountRule(rule))
            {
                return $"選擇最多 {GetMaxHandSelectionCount(rule)} 張手牌放回牌庫底，然後抽同等數量。";
            }

            return "請從手牌中選擇 1 張放回牌庫最底下";
        }

        void UpdateHandSelectionConfirmButtonLabel()
        {
            if (_deckOperationNoSelectionButton == null || _pendingDeckSelection == null) return;

            int selectedCount = _pendingDeckSelection.selectedCards.Count;
            int maxCount = GetMaxHandSelectionCount(_pendingDeckSelection.rule);
            string label = selectedCount <= 0
                ? "不選擇"
                : $"完成 {selectedCount}/{maxCount}";
            EnsureButtonLabel(_deckOperationNoSelectionButton.transform as RectTransform, label);
        }

        void HideDeckOperationSelectionUIForHandReturn(UcgEffectInstance effect)
        {
            StopDeckOperationNoValidAutoCloseRoutine();
            bool wasActive = _deckOperationSelectionRoot != null && _deckOperationSelectionRoot.gameObject.activeInHierarchy;
            if (wasActive && effect != null && effect.cardData != null && effect.cardData.id == "BP01-037")
            {
                Debug.LogWarning("BP01-037 should not use RevealTopSelect UI.");
            }

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();
        }

        string BuildDeckOperationSelectionTitle(UcgEffectInstance effect, UcgDeckOperationRule rule, int revealedCount)
        {
            string cardName = effect != null && effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                ? effect.cardData.cardName
                : "效果";
            if (rule != null && rule.operationType == UcgDeckOperationType.DrawThenSelectBottom)
            {
                return $"{cardName}\n選擇 1 張放到牌庫最底下";
            }
            if (rule != null && rule.selectionFilter != UcgDeckSelectionFilter.Any)
            {
                return $"{cardName}\n{GetDeckOperationSelectionPrompt(rule.selectionFilter)}";
            }

            return $"{cardName}\n從公開的 {revealedCount} 張中選擇 1 張加入手牌";
        }

        string BuildDeckOperationNoSelectionTitle(UcgEffectInstance effect, UcgDeckOperationRule rule, int revealedCount)
        {
            string cardName = effect != null && effect.cardData != null && !string.IsNullOrWhiteSpace(effect.cardData.cardName)
                ? effect.cardData.cardName
                : "效果";
            string noTargetMessage = GetDeckOperationNoValidSelectionMessage(rule != null ? rule.selectionFilter : UcgDeckSelectionFilter.Any);
            return $"{cardName}\n公開了 {revealedCount} 張，{noTargetMessage}\n稍後全部放到棄牌區";
        }

        void CreateDeckOperationCardButton(UcgCardData cardData, int index, int totalCount, bool canSelect, bool selectingFromHand)
        {
            if (_deckOperationCardsRoot == null || cardData == null) return;

            var cardObject = new GameObject($"Revealed Card {index + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Button));
            cardObject.transform.SetParent(_deckOperationCardsRoot, false);

            var cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            float spacing = 228f;
            float startX = -(totalCount - 1) * spacing * 0.5f;
            Vector2 finalPosition = new Vector2(startX + index * spacing, 0f);
            cardRect.anchoredPosition = finalPosition;
            cardRect.sizeDelta = new Vector2(190f, 276f);

            var image = cardObject.GetComponent<Image>();
            image.raycastTarget = true;
            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            var labelObject = new GameObject("PlaceholderText", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(cardObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.12f, 0.16f);
            labelRect.anchorMax = new Vector2(0.88f, 0.84f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
            Font font = LoadPlaceholderFont();
            if (font != null) label.font = font;

            var view = cardObject.AddComponent<UcgCardView>();
            view.cardImage = image;
            view.placeholderText = label;
            view.SetInfoPanel(cardInfoPanel);
            view.Initialize(cardData);
            view.SetFaceDown(false);
            view.SetBattlefieldLocked(true);
            ApplyDeckOperationCardSelectionVisual(cardObject, canSelect, cardData);

            var button = cardObject.GetComponent<Button>();
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = canSelect
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.18f)
                : Color.white;
            buttonColors.pressedColor = canSelect
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.2f)
                : Color.white;
            buttonColors.selectedColor = buttonColors.highlightedColor;
            buttonColors.disabledColor = new Color(0.86f, 0.86f, 0.86f, 0.94f);
            buttonColors.colorMultiplier = 1f;
            button.colors = buttonColors;
            button.interactable = canSelect;
            if (canSelect)
            {
                button.onClick.AddListener(() =>
                {
                    if (selectingFromHand)
                    {
                        CompleteDeckOperationHandSelection(cardData);
                    }
                    else
                    {
                        CompleteDeckOperationSelection(cardData);
                    }
                });
            }

            StartCoroutine(AnimateDeckOperationCardReveal(cardRect, canvasGroup, finalPosition, index));
        }

        void ApplyDeckOperationCardSelectionVisual(GameObject cardObject, bool canSelect, UcgCardData cardData)
        {
            if (cardObject == null) return;

            var outline = cardObject.GetComponent<Outline>();
            if (outline == null) outline = cardObject.AddComponent<Outline>();
            outline.effectColor = canSelect
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.86f)
                : new Color(1f, 0.38f, 0.48f, 0.62f);
            outline.effectDistance = canSelect ? new Vector2(3.2f, -3.2f) : new Vector2(2.8f, -2.8f);
            outline.useGraphicAlpha = false;

            bool selectingSceneReveal = _pendingDeckSelection != null
                && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.SceneRevealCards;
            bool selectingTopDeckReorder = _pendingDeckSelection != null
                && _pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.TopDeckReorder;
            UcgDeckSelectionFilter filter = _pendingDeckSelection != null && _pendingDeckSelection.rule != null
                ? _pendingDeckSelection.rule.selectionFilter
                : UcgDeckSelectionFilter.Any;
            int reorderIndex = selectingTopDeckReorder
                ? GetReferenceIndex(_pendingDeckSelection.selectedCards, cardData)
                : -1;
            string labelText = selectingTopDeckReorder
                ? reorderIndex >= 0 ? $"第 {reorderIndex + 1} 張" : "點選順序"
                : selectingSceneReveal
                    ? canSelect ? "可升級" : "無合法升級目標"
                    : canSelect ? "可選" : GetDeckOperationInvalidSelectionReason(filter);
            Color labelColor = canSelect
                ? UcgToolUiPalette.FocusCyan
                : new Color(1f, 0.58f, 0.62f, 0.96f);
            CreateDeckOperationCardStatusLabel(cardObject.transform, labelText, labelColor);
        }

        void CreateDeckOperationCardStatusLabel(Transform parent, string labelText, Color labelColor)
        {
            if (parent == null || string.IsNullOrWhiteSpace(labelText)) return;

            var labelObject = new GameObject("Selection Status", typeof(RectTransform), typeof(Text), typeof(Outline));
            labelObject.transform.SetParent(parent, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);
            labelRect.sizeDelta = new Vector2(156f, 26f);

            var label = labelObject.GetComponent<Text>();
            label.text = labelText;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = labelColor;
            label.fontSize = 16;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 16;
            label.raycastTarget = false;
            Font font = LoadPlaceholderFont();
            if (font != null) label.font = font;

            var outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        IEnumerator AutoCompleteDeckOperationNoSelectionAfterDelay(UcgEffectInstance effect)
        {
            float delay = Mathf.Max(0.1f, noValidSelectionAutoCloseDelay);
            yield return new WaitForSecondsRealtime(delay);

            _deckOperationNoValidAutoCloseRoutine = null;
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) yield break;
            if (_pendingDeckSelection.sourceZone != UcgDeckOperationSourceZone.RevealedCards) yield break;

            UcgDeckOperationRule rule = _pendingDeckSelection.rule;
            List<UcgCardData> selectableCards = GetSelectableDeckOperationCards(
                _pendingDeckSelection.revealedCards,
                rule != null ? rule.selectionFilter : UcgDeckSelectionFilter.Any);
            if (selectableCards.Count > 0) yield break;

            if (ShouldDebugDeckOperation(effect))
            {
                Debug.Log(
                    "RevealTopSelect no valid target auto close:\n"
                    + $"sourceCard={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                    + $"delay={delay:0.00}\n"
                    + $"revealed={FormatCardIdList(_pendingDeckSelection.revealedCards)}\n"
                    + $"destination={(rule != null ? rule.restDestination.ToString() : "unknown")}");
            }

            CompleteDeckOperationNoSelection();
        }

        void StopDeckOperationNoValidAutoCloseRoutine()
        {
            if (_deckOperationNoValidAutoCloseRoutine == null) return;
            StopCoroutine(_deckOperationNoValidAutoCloseRoutine);
            _deckOperationNoValidAutoCloseRoutine = null;
        }

        IEnumerator AnimateDeckOperationCardReveal(RectTransform cardRect, CanvasGroup canvasGroup, Vector2 finalPosition, int index)
        {
            if (cardRect == null || canvasGroup == null) yield break;

            yield return new WaitForSecondsRealtime(index * 0.06f);

            float duration = 0.22f;
            float elapsed = 0f;
            Vector2 startPosition = new Vector2(finalPosition.x * 0.35f, finalPosition.y - 18f);
            cardRect.anchoredPosition = startPosition;
            cardRect.localScale = Vector3.one * 0.82f;
            canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                cardRect.anchoredPosition = Vector2.Lerp(startPosition, finalPosition, eased);
                cardRect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
                yield return null;
            }

            cardRect.anchoredPosition = finalPosition;
            cardRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        void ClearDeckOperationCards()
        {
            if (_deckOperationCardsRoot == null) return;

            for (int i = _deckOperationCardsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _deckOperationCardsRoot.GetChild(i);
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

        void ToggleDeckOperationHandSelection(UcgCardView selectedCardView)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null || selectedCardView == null) return;
            if (!IsCurrentHandCardView(selectedCardView) || selectedCardView.CardData == null)
            {
                ShowPlayStatus("請選擇目前手牌中的卡", 1.1f);
                if (selectedCardView != null) selectedCardView.SetSelected(false);
                return;
            }

            UcgCardData selectedCard = selectedCardView.CardData;
            if (_pendingDeckSelection.selectedCards.Contains(selectedCard))
            {
                _pendingDeckSelection.selectedCards.Remove(selectedCard);
                selectedCardView.SetSelected(false);
                ShowPlayStatus($"已選 {_pendingDeckSelection.selectedCards.Count}/{GetMaxHandSelectionCount(_pendingDeckSelection.rule)} 張。");
                UpdateHandSelectionConfirmButtonLabel();
                return;
            }

            int maxSelectCount = GetMaxHandSelectionCount(_pendingDeckSelection.rule);
            if (_pendingDeckSelection.selectedCards.Count >= maxSelectCount)
            {
                selectedCardView.SetSelected(false);
                ShowPlayStatus($"最多只能選擇 {maxSelectCount} 張手牌。", 1.1f);
                return;
            }

            _pendingDeckSelection.selectedCards.Add(selectedCard);
            selectedCardView.SetSelected(true);
            ShowPlayStatus($"已選 {_pendingDeckSelection.selectedCards.Count}/{maxSelectCount} 張。點擊完成後抽同等數量。");
            UpdateHandSelectionConfirmButtonLabel();
        }

        void CompleteDeckOperationHandSelectionConfirm()
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) return;
            if (!IsSelectHandToBottomThenDrawSameCountRule(_pendingDeckSelection.rule)) return;

            UcgEffectInstance sourceEffect = _pendingDeckSelection.sourceEffect;
            UcgDeckOperationRule rule = _pendingDeckSelection.rule;
            int selectedCount = _pendingDeckSelection.selectedCards.Count;
            int minSelectCount = GetMinHandSelectionCount(rule);
            if (selectedCount < minSelectCount)
            {
                ShowPlayStatus($"至少需要選擇 {minSelectCount} 張手牌。", 1.1f);
                return;
            }

            var selectedCards = new List<UcgCardData>(_pendingDeckSelection.selectedCards);
            int handBefore = _pendingDeckSelection.handBefore;
            int deckBefore = _pendingDeckSelection.deckBefore;
            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            ApplyHandReturnSelectionHighlights(false);
            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            var returnedCards = new List<UcgCardData>();
            for (int i = 0; i < selectedCards.Count; i++)
            {
                UcgCardData card = selectedCards[i];
                if (card == null || deckManager == null || !deckManager.playerHand.Remove(card)) continue;
                RemoveCardViewFromHand(card);
                deckManager.deck.Add(card);
                returnedCards.Add(card);
            }

            var drawnCards = new List<UcgCardData>();
            if (returnedCards.Count > 0 && deckManager != null)
            {
                List<UcgCardData> drawn = deckManager.DrawCards(returnedCards.Count);
                drawnCards.AddRange(drawn);
                for (int i = 0; i < drawn.Count; i++)
                {
                    AddCardToHand(drawn[i]);
                }
            }

            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshHandLayout();
            RefreshZoneInfoUI();

            string message = returnedCards.Count > 0
                ? $"已將 {returnedCards.Count} 張手牌放回牌庫底，抽 {drawnCards.Count} 張。"
                : "沒有放回手牌，效果處理完成。";
            ShowPlayStatus(message, 1.15f);
            QueueEffectFeedback(message);
            LogSelectHandToBottomThenDrawSameCount(sourceEffect, rule, UcgPlayerSide.Player, returnedCards, drawnCards, handBefore, deckBefore);
            StopEffectSourceHighlight(sourceEffect);

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
            }
            else if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
            }
            else
            {
                TryAutoAdvanceAfterTutorialEffectResolved("牌庫效果處理完成");
            }
            UpdateMainPrompt();
            RefreshInteractionHints();
        }

        void CompleteDeckOperationSelection(UcgCardData selectedCard)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null || selectedCard == null) return;
            if (_pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.DiscardPile)
            {
                CompleteBp05008DiscardSelection(selectedCard);
                return;
            }
            if (_pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.SceneRevealCards)
            {
                CompleteBp01105SceneRevealSelection(selectedCard);
                return;
            }
            if (_pendingDeckSelection.sourceZone == UcgDeckOperationSourceZone.TopDeckReorder)
            {
                CompleteBp01043ReorderCardSelection(selectedCard);
                return;
            }

            if (!IsCardAllowedByDeckSelectionFilter(selectedCard, _pendingDeckSelection.rule.selectionFilter))
            {
                ShowPlayStatus($"只能選擇{GetSelectionFilterDisplayName(_pendingDeckSelection.rule.selectionFilter)}", 1.1f);
                return;
            }

            StopDeckOperationNoValidAutoCloseRoutine();
            UcgEffectInstance sourceEffect = _pendingDeckSelection.sourceEffect;
            ResolveDeckOperationDestinations(sourceEffect, _pendingDeckSelection.rule, _pendingDeckSelection.revealedCards, selectedCard, _pendingDeckSelection.owner, true);
            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection.selectedCard = selectedCard;
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshHandLayout();
            RefreshZoneInfoUI();
            QueueEffectFeedback("牌庫效果：處理完成");
            StopEffectSourceHighlight(sourceEffect);
            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
            }
            else if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
            }
            else
            {
                TryAutoAdvanceAfterTutorialEffectResolved("牌庫效果處理完成");
            }
            UpdateMainPrompt();
            RefreshInteractionHints();
        }

        void CompleteDeckOperationNoSelection()
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null) return;

            StopDeckOperationNoValidAutoCloseRoutine();
            UcgEffectInstance sourceEffect = _pendingDeckSelection.sourceEffect;
            UcgDeckOperationRule rule = _pendingDeckSelection.rule;
            List<UcgCardData> revealedCards = _pendingDeckSelection.revealedCards;
            UcgPlayerSide owner = _pendingDeckSelection.owner;
            string noTargetMessage = GetDeckOperationNoValidSelectionMessage(rule != null ? rule.selectionFilter : UcgDeckSelectionFilter.Any);

            ResolveDeckOperationDestinations(sourceEffect, rule, revealedCards, null, owner, true);
            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshHandLayout();
            RefreshZoneInfoUI();
            QueueEffectFeedback($"牌庫效果：{noTargetMessage}");
            StopEffectSourceHighlight(sourceEffect);

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
            }
            else if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
            }
            else
            {
                TryAutoAdvanceAfterTutorialEffectResolved("牌庫效果處理完成");
            }
            UpdateMainPrompt();
            RefreshInteractionHints();
        }

        void CompleteDeckOperationHandSelection(UcgCardData selectedCard)
        {
            if (selectedCard == null) return;
            if (_pendingDeckSelection != null
                && _pendingDeckSelection.sourceEffect != null
                && _pendingDeckSelection.sourceEffect.cardData != null
                && _pendingDeckSelection.sourceEffect.cardData.id == "BP01-037")
            {
                Debug.LogWarning("BP01-037 should not use RevealTopSelect UI.");
            }

            CompleteDeckOperationHandSelection(FindHandCardView(selectedCard));
        }

        void CompleteDeckOperationHandSelection(UcgCardView selectedCardView)
        {
            if (!_isSelectingDeckOperationCard || _pendingDeckSelection == null || selectedCardView == null) return;
            if (!IsCurrentHandCardView(selectedCardView) || selectedCardView.CardData == null)
            {
                ShowPlayStatus("請選擇一張手牌放回牌庫最底下", 1.1f);
                if (selectedCardView != null) selectedCardView.SetSelected(false);
                return;
            }

            UcgEffectInstance sourceEffect = _pendingDeckSelection.sourceEffect;
            UcgDeckOperationRule rule = _pendingDeckSelection.rule;
            UcgCardData selectedCard = selectedCardView.CardData;
            List<UcgCardData> drawnCards = new List<UcgCardData>(_pendingDeckSelection.drawnCards);
            int handBefore = _pendingDeckSelection.handBefore;
            int handAfterDraw = _pendingDeckSelection.handAfterDraw > 0
                ? _pendingDeckSelection.handAfterDraw
                : GetHandCountForOwner(UcgPlayerSide.Player);
            int deckBefore = _pendingDeckSelection.deckBefore;

            bool removedFromHand = deckManager.playerHand.Remove(selectedCard);
            if (!removedFromHand)
            {
                LogBp01037Execute(sourceEffect, drawnCards, selectedCard, handBefore, handAfterDraw, deckBefore, GetDrawPileForOwner(UcgPlayerSide.Player) != null ? GetDrawPileForOwner(UcgPlayerSide.Player).Count : 0, true, false, "selected card is not in playerHand");
                ShowPlayStatus("請選擇目前手牌中的卡", 1.1f);
                return;
            }

            selectedCardView.SetSelected(false);
            selectedCardView.SetPlayableHighlight(false);
            selectedCardView.transform.SetParent(null, false);
            Destroy(selectedCardView.gameObject);
            deckManager.deck.Add(selectedCard);

            _pendingDeckSelection.resolved = true;
            _pendingDeckSelection.selectedCard = selectedCard;
            _pendingDeckSelection = null;
            _isSelectingDeckOperationCard = false;

            ApplyHandReturnSelectionHighlights(false);
            if (_deckOperationSelectionRoot != null)
            {
                _deckOperationSelectionRoot.gameObject.SetActive(false);
            }
            if (_deckOperationNoSelectionButton != null)
            {
                _deckOperationNoSelectionButton.gameObject.SetActive(false);
            }
            ClearDeckOperationCards();

            SetHandCardsInteractable(true, null);
            SetNextPhaseButtonInteractable(true);
            RefreshHandLayout();
            RefreshZoneInfoUI();
            ShowPlayStatus("已將 1 張手牌放回牌庫最底下", 1.15f);
            QueueEffectFeedback("已將 1 張手牌放回牌庫最底下");
            LogBp01037Execute(sourceEffect, drawnCards, selectedCard, handBefore, handAfterDraw, deckBefore, GetDrawPileForOwner(UcgPlayerSide.Player) != null ? GetDrawPileForOwner(UcgPlayerSide.Player).Count : 0, true, true, "");
            LogDrawThenPutHandToBottom(sourceEffect, rule, UcgPlayerSide.Player, drawnCards, selectedCard, handBefore, deckBefore);
            StopEffectSourceHighlight(sourceEffect);

            if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.EnterEffect)
            {
                HandleEnterEffectEntry();
            }
            else if (phaseManager != null && phaseManager.CurrentPhase == UcgGamePhase.BattleEffect)
            {
                HandleBattleEffectEntry();
            }
            else
            {
                TryAutoAdvanceAfterTutorialEffectResolved("牌庫效果處理完成");
            }
            UpdateMainPrompt();
            RefreshInteractionHints();
        }

        void ResolveOpponentDeckOperation(UcgEffectInstance effect, UcgDeckOperationRule rule, List<UcgCardData> revealedCards, int handBefore, int deckBefore)
        {
            List<UcgCardData> selectableCards = GetSelectableDeckOperationCards(revealedCards, rule.selectionFilter);
            UcgCardData selectedCard = selectableCards.Count > 0 ? selectableCards[0] : null;
            ResolveDeckOperationDestinations(effect, rule, revealedCards, selectedCard, UcgPlayerSide.Opponent, false, handBefore, deckBefore);
            RefreshZoneInfoUI();
            QueueEffectFeedback("對手處理了牌庫效果");
        }

        void ResolveDeckOperationDestinations(UcgEffectInstance effect, UcgDeckOperationRule rule, List<UcgCardData> revealedCards, UcgCardData selectedCard, UcgPlayerSide owner, bool logWithCurrentCounts, int handBeforeOverride = -1, int deckBeforeOverride = -1)
        {
            if (rule == null || revealedCards == null) return;

            int handBefore = handBeforeOverride >= 0 ? handBeforeOverride : GetHandCountForOwner(owner);
            int deckBefore = deckBeforeOverride >= 0 ? deckBeforeOverride : (GetDrawPileForOwner(owner) != null ? GetDrawPileForOwner(owner).Count + revealedCards.Count : 0);
            var restCards = new List<UcgCardData>();
            for (int i = 0; i < revealedCards.Count; i++)
            {
                UcgCardData card = revealedCards[i];
                if (ReferenceEquals(card, selectedCard)) continue;
                restCards.Add(card);
            }

            if (selectedCard != null)
            {
                ApplyDeckOperationDestination(owner, selectedCard, rule.selectedDestination);
            }
            for (int i = 0; i < restCards.Count; i++)
            {
                ApplyDeckOperationDestination(owner, restCards[i], rule.restDestination);
            }

            int handAfter = GetHandCountForOwner(owner);
            int deckAfter = GetDrawPileForOwner(owner) != null ? GetDrawPileForOwner(owner).Count : 0;
            int trashAfter = owner == UcgPlayerSide.Player ? _playerDiscardPile.Count : _opponentDiscardPile.Count;
            List<UcgCardData> validSelectableCards = GetSelectableDeckOperationCards(revealedCards, rule.selectionFilter);
            bool noValidSelection = selectedCard == null && validSelectableCards.Count == 0 && rule.sendAllToRestDestinationIfNoValidSelection;

            string operationLogTitle = rule.operationType == UcgDeckOperationType.RevealTopSelectToHandRestTrash
                ? "DeckOperation RevealTopSelect"
                : "DeckOperation";
            Debug.Log(
                operationLogTitle + ":\n"
                + $"source={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                + $"owner={owner}\n"
                + $"operation={rule.operationType}\n"
                + $"revealCount={revealedCards.Count}\n"
                + $"selectionFilter={rule.selectionFilter}\n"
                + $"revealed={FormatCardIdList(revealedCards)}\n"
                + $"validSelectable={FormatCardIdList(validSelectableCards)}\n"
                + $"noValidSelection={noValidSelection}\n"
                + $"onConfirm={(noValidSelection ? "SendAllRevealedToTrash" : "SelectCard")}\n"
                + $"selectCount={rule.selectCount}\n"
                + $"selected={FormatDrawSource(selectedCard)}\n"
                + $"selectedDestination={rule.selectedDestination}\n"
                + $"restDestination={rule.restDestination}\n"
                + $"drawPileBefore={deckBefore}\n"
                + $"drawPileAfter={deckAfter}\n"
                + $"handBefore={handBefore}\n"
                + $"handAfter={handAfter}\n"
                + $"trashAfter={trashAfter}");
        }

        void ApplyDeckOperationDestination(UcgPlayerSide owner, UcgCardData card, UcgDeckOperationDestination destination)
        {
            if (card == null || deckManager == null) return;

            switch (destination)
            {
                case UcgDeckOperationDestination.Hand:
                    if (owner == UcgPlayerSide.Player)
                    {
                        deckManager.playerHand.Add(card);
                        AddCardToHand(card);
                    }
                    else
                    {
                        deckManager.opponentHiddenHand.Add(card);
                    }
                    break;
                case UcgDeckOperationDestination.BottomOfDeck:
                    GetDrawPileForOwner(owner)?.Add(card);
                    break;
                case UcgDeckOperationDestination.Trash:
                    if (owner == UcgPlayerSide.Player)
                    {
                        _playerDiscardPile.Add(card);
                    }
                    else
                    {
                        _opponentDiscardPile.Add(card);
                    }
                    break;
                case UcgDeckOperationDestination.KeepOrder:
                    GetDrawPileForOwner(owner)?.Insert(0, card);
                    break;
                case UcgDeckOperationDestination.ShuffleBack:
                    GetDrawPileForOwner(owner)?.Add(card);
                    break;
            }
        }

        List<UcgCardData> GetSelectableDeckOperationCards(List<UcgCardData> cards, UcgDeckSelectionFilter filter)
        {
            var result = new List<UcgCardData>();
            if (cards == null) return result;

            for (int i = 0; i < cards.Count; i++)
            {
                UcgCardData card = cards[i];
                if (IsCardAllowedByDeckSelectionFilter(card, filter))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        List<UcgCardData> GetInvalidDeckOperationCards(List<UcgCardData> cards, UcgDeckSelectionFilter filter)
        {
            var result = new List<UcgCardData>();
            if (cards == null) return result;

            for (int i = 0; i < cards.Count; i++)
            {
                UcgCardData card = cards[i];
                if (!IsCardAllowedByDeckSelectionFilter(card, filter))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        bool IsCardAllowedByDeckSelectionFilter(UcgCardData card, UcgDeckSelectionFilter filter)
        {
            if (card == null) return false;
            switch (filter)
            {
                case UcgDeckSelectionFilter.SceneCard:
                    return card.IsSceneCard();
                case UcgDeckSelectionFilter.UltramanCard:
                    return IsUltramanCard(card);
                case UcgDeckSelectionFilter.Any:
                default:
                    return true;
            }
        }

        bool IsUltramanCard(UcgCardData card)
        {
            if (card == null || card.IsSceneCard()) return false;
            if (ContainsCardText(card.cardCategory, "超人", "超人力霸王", "Ultraman")) return true;
            if (ContainsCardText(card.type, "超人", "超人力霸王", "Ultraman")) return true;
            if (ContainsCardText(card.characterName, "超人力霸王", "Ultraman")) return true;
            return ContainsCardText(card.cardName, "超人力霸王", "Ultraman");
        }

        bool ContainsCardText(string value, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(value) || keywords == null) return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = keywords[i];
                if (!string.IsNullOrWhiteSpace(keyword) && value.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        string GetSelectionFilterDisplayName(UcgDeckSelectionFilter filter)
        {
            switch (filter)
            {
                case UcgDeckSelectionFilter.SceneCard:
                    return "場景卡";
                case UcgDeckSelectionFilter.UltramanCard:
                    return "超人卡";
                default:
                    return "卡牌";
            }
        }

        string GetDeckOperationSelectionPrompt(UcgDeckSelectionFilter filter)
        {
            switch (filter)
            {
                case UcgDeckSelectionFilter.SceneCard:
                    return "選擇 1 張場景卡加入手牌";
                case UcgDeckSelectionFilter.UltramanCard:
                    return "選擇 1 張超人卡加入手牌";
                default:
                    return "選擇 1 張卡加入手牌";
            }
        }

        string GetDeckOperationNoValidSelectionMessage(UcgDeckSelectionFilter filter)
        {
            switch (filter)
            {
                case UcgDeckSelectionFilter.SceneCard:
                    return "沒有可加入手牌的場景卡";
                case UcgDeckSelectionFilter.UltramanCard:
                    return "沒有可加入手牌的超人卡";
                default:
                    return "沒有可加入手牌的目標";
            }
        }

        string GetDeckOperationInvalidSelectionReason(UcgDeckSelectionFilter filter)
        {
            switch (filter)
            {
                case UcgDeckSelectionFilter.SceneCard:
                    return "非場景卡";
                case UcgDeckSelectionFilter.UltramanCard:
                    return "非超人卡";
                default:
                    return "不符合條件";
            }
        }

        string GetDeckOperationFinalDestinationText(UcgDeckOperationRule rule, bool noValidSelection)
        {
            if (rule == null) return "unknown";
            return noValidSelection
                ? $"allRevealed->{rule.restDestination}"
                : $"selected->{rule.selectedDestination}, rest->{rule.restDestination}";
        }

        void RemoveCardViewFromHand(UcgCardData card)
        {
            if (cardHolder == null || card == null) return;

            for (int i = cardHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = cardHolder.GetChild(i);
                var cardView = child != null ? child.GetComponent<UcgCardView>() : null;
                if (cardView == null || !ReferenceEquals(cardView.CardData, card)) continue;

                child.SetParent(null, false);
                Destroy(child.gameObject);
                return;
            }
        }

        UcgCardView FindHandCardView(UcgCardData card)
        {
            if (cardHolder == null || card == null) return null;

            for (int i = cardHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = cardHolder.GetChild(i);
                var cardView = child != null ? child.GetComponent<UcgCardView>() : null;
                if (cardView != null && ReferenceEquals(cardView.CardData, card))
                {
                    return cardView;
                }
            }

            return null;
        }

        string FormatCardIdList(List<UcgCardData> cards)
        {
            if (cards == null || cards.Count == 0) return "<none>";
            var parts = new List<string>();
            for (int i = 0; i < cards.Count; i++)
            {
                UcgCardData card = cards[i];
                parts.Add(card != null && !string.IsNullOrWhiteSpace(card.id) ? card.id : "unknown");
            }

            return string.Join(", ", parts);
        }

        void LogRevealTopSelectUiOpened(UcgEffectInstance effect, UcgDeckOperationRule rule, List<UcgCardData> revealedCards, List<UcgCardData> selectableCards, bool noValidSelection)
        {
            if (!ShouldDebugDeckOperation(effect)) return;

            UcgDeckSelectionFilter filter = rule != null ? rule.selectionFilter : UcgDeckSelectionFilter.Any;
            List<UcgCardData> invalidCards = GetInvalidDeckOperationCards(revealedCards, filter);
            Debug.Log(
                "RevealTopSelect UI opened:\n"
                + $"sourceCard={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                + $"revealCount={(revealedCards != null ? revealedCards.Count : 0)}\n"
                + $"selectionFilter={filter}\n"
                + $"revealedCardIds={FormatCardIdList(revealedCards)}\n"
                + $"validSelectableIds={FormatCardIdList(selectableCards)}\n"
                + $"invalidIds={FormatCardIdList(invalidCards)}\n"
                + $"noValidSelection={noValidSelection}\n"
                + $"autoCloseDelay={(noValidSelection ? Mathf.Max(0.1f, noValidSelectionAutoCloseDelay) : 0f):0.00}\n"
                + $"finalDestination={GetDeckOperationFinalDestinationText(rule, noValidSelection)}");
        }

        void LogDrawThenPutHandToBottom(UcgEffectInstance effect, UcgDeckOperationRule rule, UcgPlayerSide owner, List<UcgCardData> drawnCards, UcgCardData selectedCard, int handBefore, int deckBefore)
        {
            if (!debugEffectResolution) return;

            int handAfter = GetHandCountForOwner(owner);
            int deckAfter = GetDrawPileForOwner(owner) != null ? GetDrawPileForOwner(owner).Count : 0;
            Debug.Log(
                "DeckOperation DrawThenPutHandToBottom:\n"
                + $"source={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                + $"owner={owner}\n"
                + $"drawCount={(rule != null ? rule.drawCount : 0)}\n"
                + $"drawn={FormatCardIdList(drawnCards)}\n"
                + $"selectedHandCardToBottom={FormatDrawSource(selectedCard)}\n"
                + $"handBefore={handBefore}\n"
                + $"handAfter={handAfter}\n"
                + $"drawPileBefore={deckBefore}\n"
                + $"drawPileAfter={deckAfter}");
        }

        void LogSelectHandToBottomThenDrawSameCount(UcgEffectInstance effect, UcgDeckOperationRule rule, UcgPlayerSide owner, List<UcgCardData> returnedCards, List<UcgCardData> drawnCards, int handBefore, int deckBefore)
        {
            if (!debugEffectResolution && !debugDeckOperation) return;

            int handAfter = GetHandCountForOwner(owner);
            int deckAfter = GetDrawPileForOwner(owner) != null ? GetDrawPileForOwner(owner).Count : 0;
            Debug.Log(
                "DeckOperation SelectHandToBottomThenDrawSameCount:\n"
                + $"source={FormatDrawSource(effect != null ? effect.cardData : null)}\n"
                + $"owner={owner}\n"
                + $"minSelectCount={(rule != null ? rule.minHandSelectCount : 0)}\n"
                + $"maxSelectCount={(rule != null ? rule.handSelectCount : 0)}\n"
                + $"returned={FormatCardIdList(returnedCards)}\n"
                + $"drawn={FormatCardIdList(drawnCards)}\n"
                + $"handBefore={handBefore}\n"
                + $"handAfter={handAfter}\n"
                + $"drawPileBefore={deckBefore}\n"
                + $"drawPileAfter={deckAfter}");
        }

        bool ShouldDebugDeckOperation(UcgEffectInstance effect)
        {
            return debugDeckOperation || debugEffectResolution;
        }

        void LogBp01037Execute(
            UcgEffectInstance effect,
            List<UcgCardData> drawnCards,
            UcgCardData selectedCard,
            int handBefore,
            int handAfterDraw,
            int deckBefore,
            int deckAfter,
            bool handSelectionOpened,
            bool movedToBottom,
            string notOpenedReason)
        {
            if (!ShouldDebugDeckOperation(effect)) return;
            if (effect == null || effect.cardData == null || effect.cardData.id != "BP01-037") return;

            int handAfter = GetHandCountForOwner(UcgPlayerSide.Player);
            Debug.Log(
                "BP01-037 execute:\n"
                + $"drawPileBefore={deckBefore}\n"
                + $"handBefore={handBefore}\n"
                + $"drawnCards={FormatCardIdList(drawnCards)}\n"
                + $"handAfterDraw={handAfterDraw}\n"
                + $"handSelectionModeOpened={handSelectionOpened}\n"
                + $"selectedHandCard={FormatDrawSource(selectedCard)}\n"
                + $"movedToBottom={movedToBottom}\n"
                + $"drawPileAfter={deckAfter}\n"
                + $"handAfter={handAfter}\n"
                + $"reason={(string.IsNullOrWhiteSpace(notOpenedReason) ? "none" : notOpenedReason)}");
        }

        string FormatDrawSource(UcgCardData sourceCard)
        {
            if (sourceCard == null) return "manual/unknown";
            string cardId = string.IsNullOrWhiteSpace(sourceCard.id) ? "no-id" : sourceCard.id;
            string cardName = string.IsNullOrWhiteSpace(sourceCard.cardName) ? "card" : sourceCard.cardName;
            return $"{cardId} {cardName}";
        }

        void HandleDrawPhase()
        {
            if (turnManager != null && turnManager.currentTurn <= 1)
            {
                if (playResultText != null)
                {
                    playResultText.text = "抽牌階段：第 1 回合不抽牌，準備進入場景設置。";
                }
                return;
            }

            if (deckManager == null || deckManager.RemainingCount <= 0)
            {
                if (playResultText != null)
                {
                    playResultText.text = "抽牌階段：牌庫已空，無法抽牌。";
                }
                UpdateDeckCountText();
                return;
            }

            DrawCardsToHand(1, "抽牌階段：抽 1 張牌。");
        }

        void AddCardToHand(UcgCardData data)
        {
            if (data == null) return;

            _createdHandCardSerial++;
            var cardObject = new GameObject($"UCG Card {_createdHandCardSerial}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(cardHolder, false);

            var rectTransform = cardObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = GetHandCardSizeForCount(cardHolder.childCount);

            var image = cardObject.GetComponent<Image>();
            image.raycastTarget = true;

            var labelObject = new GameObject("PlaceholderText", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(cardObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.12f, 0.16f);
            labelRect.anchorMax = new Vector2(0.88f, 0.84f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                label.font = placeholderFont;
            }
            label.fontSize = 24;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 28;
            label.raycastTarget = false;

            bool shouldUseTestSprite = data != null && string.IsNullOrWhiteSpace(data.imageLocal);
            if (shouldUseTestSprite)
            {
                Sprite sprite = GetTestCardSprite(GetSpriteIndexForCard(data));
                data.cardImage = sprite;
                if (sprite == null)
                {
                    Debug.LogWarning($"Hand card image fallback to placeholder: {data.id} / {data.cardName}, imageLocal=<empty>, local test sprite missing");
                }
            }

            var view = cardObject.AddComponent<UcgCardView>();
            view.cardImage = image;
            view.placeholderText = label;
            view.SetInfoPanel(cardInfoPanel);
            view.Initialize(data);
            view.SetFaceDown(false);
            view.SetBattlefieldLocked(false);
            view.OnCardSelected += HandleCardSelected;
            EnsureHandCardCornerFrame(rectTransform);

            var hover = cardObject.AddComponent<UIHandCardHover>();
            hover.lift = 24f;
            hover.scale = 1.02f;
            hover.rotAdd = 0f;
            hover.speed = 14f;
            hover.straightenOnHover = true;
            hover.straightenSpeed = 14f;
            hover.bringToFrontOnHover = false;
            hover.useOverlaySorting = true;
            hover.hoverSortingOrder = 1450;

            var dragLayerCard = cardObject.AddComponent<UcgDragLayerCard>();
            dragLayerCard.draggingSortingOrder = 10000;

            var drag = cardObject.AddComponent<UIDragCard>();
            drag.rootCanvas = canvas;
            drag.dragLayerOverride = dragLayer != null ? dragLayer : canvas.transform;
        }

        void RefreshHandLayout()
        {
            ApplyHandStyleByCount(cardHolder.childCount);

            UIHandLayout layout = cardHolder.GetComponent<UIHandLayout>();
            if (layout != null)
            {
                layout.NotifyLayoutChanged(true);
            }
        }

        void ApplyHandStyleByCount(int cardCount)
        {
            if (cardHolder == null) return;

            UIHandLayout layout = cardHolder.GetComponent<UIHandLayout>();
            if (layout == null) return;

            Vector2 handCardSize = GetHandCardSizeForCount(cardCount);
            float selectedScale;
            float hoverLift;
            float hoverScale;

            if (cardCount <= 5)
            {
                layout.radius = 620f;
                layout.totalAngle = 32f;
                layout.cardsForFullSpread = 5;
                layout.minAngle = 12f;
                layout.baselinePadding = 148f;
                hoverLift = 26f;
                hoverScale = 1.025f;
                selectedScale = 1.06f;
            }
            else if (cardCount <= 8)
            {
                layout.radius = 500f;
                layout.totalAngle = 46f;
                layout.cardsForFullSpread = 8;
                layout.minAngle = 22f;
                layout.baselinePadding = 138f;
                hoverLift = 22f;
                hoverScale = 1.02f;
                selectedScale = 1.05f;
            }
            else if (cardCount <= 12)
            {
                layout.radius = 440f;
                layout.totalAngle = 54f;
                layout.cardsForFullSpread = 10;
                layout.minAngle = 30f;
                layout.baselinePadding = 130f;
                hoverLift = 18f;
                hoverScale = 1.015f;
                selectedScale = 1.045f;
            }
            else
            {
                layout.radius = 410f;
                layout.totalAngle = 62f;
                layout.cardsForFullSpread = 12;
                layout.minAngle = 36f;
                layout.baselinePadding = 124f;
                hoverLift = 16f;
                hoverScale = 1.01f;
                selectedScale = 1.04f;
            }

            for (int i = 0; i < cardHolder.childCount; i++)
            {
                RectTransform cardRect = cardHolder.GetChild(i) as RectTransform;
                if (cardRect == null) continue;

                cardRect.sizeDelta = handCardSize;
                EnsureHandCardCornerFrame(cardRect);

                var view = cardRect.GetComponent<UcgCardView>();
                if (view != null)
                {
                    view.SetBaseSize(handCardSize);
                    view.selectedSizeMultiplier = selectedScale;
                    view.selectedSortingOrder = 1600;
                }

                var hover = cardRect.GetComponent<UIHandCardHover>();
                if (hover != null)
                {
                    hover.enabled = true;
                    hover.lift = hoverLift;
                    hover.scale = hoverScale;
                    hover.rotAdd = 0f;
                    hover.hoverSortingOrder = 1500 + i;
                    hover.speed = 14f;
                    hover.straightenOnHover = true;
                    hover.straightenSpeed = 14f;
                    hover.bringToFrontOnHover = false;
                    hover.useOverlaySorting = true;
                }
            }
        }

        Vector2 GetHandCardSizeForCount(int cardCount)
        {
            if (cardCount <= 5) return new Vector2(214f, 310f);
            if (cardCount <= 8) return new Vector2(204f, 296f);
            if (cardCount <= 12) return new Vector2(194f, 281f);
            return new Vector2(184f, 267f);
        }

        void EnsureHandCardCornerFrame(RectTransform cardRect)
        {
            if (cardRect == null) return;

            const string frameName = "Hand Card Soft Corners";
            Transform existingFrame = cardRect.Find(frameName);
            RectTransform frameRect;
            Image frameImage;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(cardRect, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                if (existingFrame.GetComponent<Outline>() == null) existingFrame.gameObject.AddComponent<Outline>();
            }

            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frameRect.localScale = Vector3.one;
            frameRect.localEulerAngles = Vector3.zero;
            frameRect.SetAsLastSibling();

            ApplyRoundedPanelImage(frameImage);
            frameImage.color = new Color(1f, 1f, 1f, 0.018f);
            frameImage.raycastTarget = false;

            Outline outline = frameRect.GetComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = new Color(0.52f, 0.9f, 1f, 0.20f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
            outline.useGraphicAlpha = true;
        }

        void UpdateDeckCountText()
        {
            RefreshZoneInfoUI();
        }

        void SyncOpponentZoneCountsFromDeckManager()
        {
            if (deckManager == null) return;
            if (deckManager.OpponentProfile == null && deckManager.opponentDrawPile.Count == 0 && deckManager.opponentHiddenHand.Count == 0) return;

            _opponentDeckCount = deckManager.opponentDrawPile.Count;
            _opponentHandCount = deckManager.opponentHiddenHand.Count;
        }

        void RefreshZoneInfoUI()
        {
            RefreshBoardZoneLayout();
            SyncOpponentZoneCountsFromDeckManager();
            int playerDeckCount = GetPlayerDeckCount();
            int playerDiscardCount = GetPlayerDiscardCount();
            int opponentDeckCount = GetOpponentDeckCount();
            int opponentDiscardCount = GetOpponentDiscardCount();

            if (deckCountText != null)
            {
                deckCountText.text = $"{playerDeckCount}";
            }

            if (playerDiscardZoneText != null)
            {
                playerDiscardZoneText.text = $"{playerDiscardCount}";
            }

            if (opponentZoneText != null)
            {
                opponentZoneText.text = $"{opponentDeckCount}";
            }

            if (opponentDiscardZoneText != null)
            {
                opponentDiscardZoneText.text = $"{opponentDiscardCount}";
            }
        }

        void RefreshBoardZoneLayout()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            if (root == null) return;

            ApplyBoardZoneRootLayout(root);
            ApplyBoardZoneLayoutForPortrait(root, GetBoardZoneCardSize());
            LogBoardZoneDebug("RefreshBoardZoneLayout", false);
            LogSidePileLayoutDebug("RefreshBoardZoneLayout");
            LogPileRegionModeCompare("RefreshBoardZoneLayout", false);
        }

        RectTransform GetBoardZoneLayoutRoot()
        {
            if (playerSidePileGroup != null && playerSidePileGroup.parent != null)
            {
                RectTransform parent = playerSidePileGroup.parent as RectTransform;
                if (parent != null && parent.parent != null && parent.name == "Pile Side Region")
                {
                    return parent.parent as RectTransform;
                }

                return parent;
            }

            if (playerDeckAnchor != null && playerDeckAnchor.parent != null)
            {
                RectTransform parent = playerDeckAnchor.parent as RectTransform;
                if (parent != null && parent.parent != null && parent.name.EndsWith("PileGroup"))
                {
                    RectTransform regionParent = parent.parent as RectTransform;
                    if (regionParent != null && regionParent.parent != null && regionParent.name == "Pile Side Region")
                    {
                        return regionParent.parent as RectTransform;
                    }

                    return regionParent;
                }

                return parent;
            }

            return null;
        }

        void LogBoardZoneDebug(string context, bool force)
        {
            if (!debugBoardZones) return;

            _boardZoneDebugPrinted = true;
            LogDebugBoardZonesState(context, false);
            Debug.Log(
                $"BoardZone Debug: context={context}\n"
                + FormatBattlefieldLayoutDebug(context)
                + FormatBoardZoneDebug("PlayerDiscard", playerDiscardAnchor)
                + FormatBoardZoneDebug("PlayerDeck", playerDeckAnchor)
                + FormatBoardZoneDebug("OpponentDiscard", opponentDiscardAnchor)
                + FormatBoardZoneDebug("OpponentDeck", opponentDeckAnchor)
                + FormatBoardZoneDebug("SceneAreaFrame", GetSceneZoneFrameRect())
                + FormatBoardZoneDebug("SharedSceneSlot", sceneZoneAnchor));
        }

        void LogDebugBoardZonesState(string context, bool force)
        {
            if (_debugBoardZonesStateLogged && !force) return;
            if (!force && !debugBoardZones && !debugBattlefieldLayout && !debugForceSidePileExtremeOffset) return;

            _debugBoardZonesStateLogged = true;
            Debug.Log(
                $"DebugBoardZonesState: context={context}\n"
                + $"debugBoardZones={debugBoardZones}\n"
                + $"gameObject={FormatTransformPath(transform)}\n"
                + $"instanceID={gameObject.GetInstanceID()}\n"
                + $"activeInHierarchy={gameObject.activeInHierarchy}\n"
                + $"enabled={enabled}\n"
                + $"canvas={FormatTransformPath(canvas != null ? canvas.transform : null)}\n"
                + $"debugBattlefieldLayout={debugBattlefieldLayout}\n"
                + $"debugForceSidePileExtremeOffset={debugForceSidePileExtremeOffset}\n"
                + $"debugSidePileExtremeOffsetX={debugSidePileExtremeOffsetX:0.#}\n"
                + $"playerSidePileGroup={FormatTransformPath(playerSidePileGroup)}\n"
                + $"opponentSidePileGroup={FormatTransformPath(opponentSidePileGroup)}\n"
                + $"playerSidePileGroupInstanceID={FormatInstanceId(playerSidePileGroup)}\n"
                + $"opponentSidePileGroupInstanceID={FormatInstanceId(opponentSidePileGroup)}\n"
                + $"playerSidePileGroupActive={IsActiveInHierarchy(playerSidePileGroup)}\n"
                + $"opponentSidePileGroupActive={IsActiveInHierarchy(opponentSidePileGroup)}\n"
                + $"playerDeckZone={FormatTransformPath(playerDeckAnchor)}\n"
                + $"playerDiscardZone={FormatTransformPath(playerDiscardAnchor)}\n"
                + $"opponentDeckZone={FormatTransformPath(opponentDeckAnchor)}\n"
                + $"opponentDiscardZone={FormatTransformPath(opponentDiscardAnchor)}\n");
        }

        void LogUcgHandDemoInstances(string context)
        {
            UcgHandDemo[] demos = FindObjectsByType<UcgHandDemo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var output = new System.Text.StringBuilder();
            output.Append("UcgHandDemoInstanceScan:\n");
            output.Append("context=").Append(context).Append('\n');
            output.Append("count=").Append(demos != null ? demos.Length : 0).Append('\n');

            if (demos != null)
            {
                for (int i = 0; i < demos.Length; i++)
                {
                    UcgHandDemo demo = demos[i];
                    if (demo == null) continue;

                    output.Append("UcgHandDemoInstance:\n");
                    output.Append("gameObject=").Append(FormatTransformPath(demo.transform)).Append('\n');
                    output.Append("instanceID=").Append(demo.gameObject.GetInstanceID()).Append('\n');
                    output.Append("activeInHierarchy=").Append(demo.gameObject.activeInHierarchy).Append('\n');
                    output.Append("enabled=").Append(demo.enabled).Append('\n');
                    output.Append("debugBoardZones=").Append(demo.debugBoardZones).Append('\n');
                    output.Append("scene=").Append(demo.gameObject.scene.name).Append('\n');
                    output.Append("controlsCurrentInstance=").Append(demo == this).Append('\n');
                }
            }

            Debug.Log(output.ToString());
        }

        string FormatBattlefieldLayoutDebug(string context)
        {
            RectTransform laneRoot = battlefieldManager != null ? battlefieldManager.lanesRoot : null;
            RectTransform contentRoot = battlefieldManager != null ? battlefieldManager.content : null;

            return "BattlefieldLayout:\n"
                + $"context={context}\n"
                + FormatBoardLayoutRefresh()
                + $"combatAreaOffsetX={GetCombatAreaOffsetX():0.#}\n"
                + $"rightAuxiliaryColumnGutterWidth={rightAuxiliaryColumnGutterWidth:0.#}\n"
                + $"rightSidePileColumnDownShift={rightSidePileColumnDownShift:0.#}\n"
                + $"appliedRightSidePileColumnDownShift={GetDebugAppliedRightSidePileColumnDownShift():0.#}\n"
                + $"sidePileScale={sidePileScale:0.00}\n"
                + $"sidePileGap={sidePileGap:0.#}\n"
                + $"deckDiscardGroupGap={deckDiscardGroupGap:0.#}\n"
                + $"combatToPileGapX={combatToPileGapX:0.#}\n"
                + $"pileGroupVerticalSeparation={pileGroupVerticalSeparation:0.#}\n"
                + $"sidePileColumnMargin={sidePileColumnMargin:0.#}\n"
                + $"sidePanelWidth={sidePanelWidth:0.#}\n"
                + $"sidePanelRightMargin={sidePanelRightMargin:0.#}\n"
                + $"sceneAreaScale={sceneAreaScale:0.00}\n"
                + $"sceneAreaSize=({GetSceneAreaSize().x:0.#},{GetSceneAreaSize().y:0.#})\n"
                + $"sceneToOpponentLaneGap={sceneToOpponentLaneGap:0.#}\n"
                + $"sceneToPlayerLaneGap={sceneToPlayerLaneGap:0.#}\n"
                + $"sceneAreaAlpha={sceneAreaAlpha:0.00}\n"
                + $"combatFocusViewportPosition={combatFocusViewportPosition:0.00}\n"
                + $"revealSelectionOffsetX={GetRevealSelectionOffsetX():0.#}\n"
                + $"laneRootPos={FormatBoardLayoutPosition(laneRoot)}\n"
                + $"contentPos={FormatBoardLayoutPosition(contentRoot)}\n"
                + $"sceneAreaPos={FormatBoardLayoutPosition(sceneZoneAnchor)}\n"
                + $"playerSidePileGroupPos={FormatBoardLayoutPosition(playerSidePileGroup)}\n"
                + $"opponentSidePileGroupPos={FormatBoardLayoutPosition(opponentSidePileGroup)}\n"
                + FormatBoardZoneParentCheck()
                + FormatBattlefieldRegionLayout()
                + FormatBattlefieldRegionVisibility()
                + FormatActiveLaneFocusCheck()
                + FormatBoardZoneDistanceCheck()
                + FormatBoardZoneTuning()
                + FormatReferenceBoardLayout()
                + FormatSidePanelLayout()
                + FormatSidePileRightSpaceTuning()
                + FormatSidePileFinalPositionTrace()
                + FormatBoardZoneObjectScan()
                + FormatVisibleBoardZoneHierarchy("PlayerDeck", playerDeckAnchor)
                + FormatVisibleBoardZoneHierarchy("PlayerDiscard", playerDiscardAnchor)
                + FormatVisibleBoardZoneHierarchy("OpponentDeck", opponentDeckAnchor)
                + FormatVisibleBoardZoneHierarchy("OpponentDiscard", opponentDiscardAnchor)
                + FormatSidePileLayoutDebug();
        }

        string FormatBoardLayoutRefresh()
        {
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            bool overlapCheck =
                DoesBoardZoneOverlapAnyLane(playerDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(playerDiscardAnchor)
                || DoesBoardZoneOverlapAnyLane(opponentDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(opponentDiscardAnchor)
                || DoesBoardZoneOverlapRevealArea(playerDeckAnchor)
                || DoesBoardZoneOverlapRevealArea(playerDiscardAnchor)
                || DoesBoardZoneOverlapRevealArea(opponentDeckAnchor)
                || DoesBoardZoneOverlapRevealArea(opponentDiscardAnchor);

            return "BoardLayoutRefresh:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"boardCardSlotWidth={GetPortraitCardSlotSize().x:0.#}\n"
                + $"boardCardSlotHeight={GetPortraitCardSlotSize().y:0.#}\n"
                + $"portraitSlotWidth={GetPortraitCardSlotSize().x:0.#}\n"
                + $"portraitSlotHeight={GetPortraitCardSlotSize().y:0.#}\n"
                + $"battleSlotSize=({GetBattleSlotSize().x:0.#},{GetBattleSlotSize().y:0.#})\n"
                + $"pileSlotSize=({GetBoardZoneCardSize().x:0.#},{GetBoardZoneCardSize().y:0.#})\n"
                + $"laneSlotSize=({GetBattleSlotSize().x:0.#},{GetBattleSlotSize().y:0.#})\n"
                + $"deckSlotSize=({GetBoardZoneCardSize().x:0.#},{GetBoardZoneCardSize().y:0.#})\n"
                + $"discardSlotSize=({GetBoardZoneCardSize().x:0.#},{GetBoardZoneCardSize().y:0.#})\n"
                + $"horizontalCardSafeWidth={(GetPortraitCardSlotSize().y + Mathf.Max(0f, horizontalCardSafePadding)):0.#}\n"
                + $"battleLaneWidth={GetBattleLaneWidth():0.#}\n"
                + $"battleLaneSpacing={GetBattleLaneSpacing():0.#}\n"
                + $"sceneSlotSize=({GetSceneAreaSize().x:0.#},{GetSceneAreaSize().y:0.#})\n"
                + $"combatToPileGap={combatToPileGap:0.#}\n"
                + $"deckDiscardGroupGap={deckDiscardGroupGap:0.#}\n"
                + $"opponentRowY={opponentRowY:0.#}\n"
                + $"playerRowY={playerRowY:0.#}\n"
                + $"sceneAreaY={sceneAreaY:0.#}\n"
                + $"fieldColumnX={fieldColumnX:0.#}\n"
                + $"pileColumnRightInset={pileColumnRightInset:0.#}\n"
                + $"opponentZoneRect={FormatWorldRect(opponentSidePileGroup)}\n"
                + $"playerZoneRect={FormatWorldRect(playerSidePileGroup)}\n"
                + $"sceneZoneRect={FormatWorldRect(sceneZoneAnchor)}\n"
                + $"opponentDeckRect={FormatWorldRect(opponentDeckAnchor)}\n"
                + $"opponentDiscardRect={FormatWorldRect(opponentDiscardAnchor)}\n"
                + $"playerDeckRect={FormatWorldRect(playerDeckAnchor)}\n"
                + $"playerDiscardRect={FormatWorldRect(playerDiscardAnchor)}\n"
                + $"playerDeckPos={FormatAnchoredPosition(playerDeckAnchor)}\n"
                + $"playerDiscardPos={FormatAnchoredPosition(playerDiscardAnchor)}\n"
                + $"opponentDeckPos={FormatAnchoredPosition(opponentDeckAnchor)}\n"
                + $"opponentDiscardPos={FormatAnchoredPosition(opponentDiscardAnchor)}\n"
                + $"overlapCheck={overlapCheck}\n"
                + $"clippedCheck={IsAnyBoardZoneClippedByViewport()}\n";
        }

        string FormatBattlefieldRegionLayout()
        {
            bool pileZonesVisible =
                IsActiveInHierarchy(opponentDeckAnchor)
                && IsActiveInHierarchy(opponentDiscardAnchor)
                && IsActiveInHierarchy(playerDeckAnchor)
                && IsActiveInHierarchy(playerDiscardAnchor);
            bool zoneOverlapCheck =
                DoesBoardZoneOverlapAnyLane(opponentDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(opponentDiscardAnchor)
                || DoesBoardZoneOverlapAnyLane(playerDeckAnchor)
                || DoesBoardZoneOverlapAnyLane(playerDiscardAnchor);

            return "BattlefieldRegionLayout:\n"
                + $"combatRegionRect={FormatWorldRect(combatBoardRegionRoot)}\n"
                + $"pileRegionRect={FormatWorldRect(pileSideRegionRoot)}\n"
                + $"sceneRegionRect={FormatWorldRect(sceneZoneAnchor)}\n"
                + $"opponentDiscardRect={FormatWorldRect(opponentDiscardAnchor)}\n"
                + $"opponentDeckRect={FormatWorldRect(opponentDeckAnchor)}\n"
                + $"playerDeckRect={FormatWorldRect(playerDeckAnchor)}\n"
                + $"playerDiscardRect={FormatWorldRect(playerDiscardAnchor)}\n"
                + $"pileRegionActive={IsActiveInHierarchy(pileSideRegionRoot)}\n"
                + $"pileRegionAlpha={GetCanvasGroupAlpha(pileSideRegionRoot):0.00}\n"
                + $"opponentSidePileGroupActive={IsActiveInHierarchy(opponentSidePileGroup)}\n"
                + $"opponentSidePileGroupAlpha={GetCanvasGroupAlpha(opponentSidePileGroup):0.00}\n"
                + $"playerSidePileGroupActive={IsActiveInHierarchy(playerSidePileGroup)}\n"
                + $"playerSidePileGroupAlpha={GetCanvasGroupAlpha(playerSidePileGroup):0.00}\n"
                + $"opponentDiscardActive={IsActiveInHierarchy(opponentDiscardAnchor)}\n"
                + $"opponentDeckActive={IsActiveInHierarchy(opponentDeckAnchor)}\n"
                + $"playerDeckActive={IsActiveInHierarchy(playerDeckAnchor)}\n"
                + $"playerDiscardActive={IsActiveInHierarchy(playerDiscardAnchor)}\n"
                + $"pileRegionClipped={IsBoardZoneClippedByViewport(pileSideRegionRoot)}\n"
                + $"combatRegionClipped={IsBoardZoneClippedByViewport(combatBoardRegionRoot)}\n"
                + $"pileZonesVisible={pileZonesVisible}\n"
                + $"zoneOverlapCheck={zoneOverlapCheck}\n"
                + $"handHolderParent={FormatParentPath(cardHolder)}\n"
                + $"handIsHud={IsHandHolderHud()}\n";
        }

        string FormatBattlefieldRegionVisibility()
        {
            RectTransform viewportRect = battlefieldManager != null ? battlefieldManager.viewport : null;
            RectTransform contentRect = battlefieldManager != null ? battlefieldManager.content : null;

            return "BattlefieldRegionVisibility:\n"
                + $"combatRegionActive={IsActiveInHierarchy(combatBoardRegionRoot)}\n"
                + $"combatRegionAlpha={GetCanvasGroupAlpha(combatBoardRegionRoot):0.00}\n"
                + $"combatRegionRect={FormatWorldRect(combatBoardRegionRoot)}\n"
                + "PileSideRegionVisibility:\n"
                + $"debugBoardZones={debugBoardZones}\n"
                + $"pileRegionActiveSelf={FormatActiveSelf(pileSideRegionRoot)}\n"
                + $"pileRegionActiveInHierarchy={IsActiveInHierarchy(pileSideRegionRoot)}\n"
                + $"pileRegionActive={IsActiveInHierarchy(pileSideRegionRoot)}\n"
                + $"pileRegionAlpha={GetCanvasGroupAlpha(pileSideRegionRoot):0.00}\n"
                + $"pileRegionParent={FormatParentPath(pileSideRegionRoot)}\n"
                + $"pileRegionAnchoredPosition={FormatAnchoredPosition(pileSideRegionRoot)}\n"
                + $"pileRegionWorldPosition={FormatWorldPosition(pileSideRegionRoot)}\n"
                + $"pileRegionRect={FormatWorldRect(pileSideRegionRoot)}\n"
                + $"pileRegionSizeDelta={FormatSizeDelta(pileSideRegionRoot)}\n"
                + $"pileRegionClipped={IsBoardZoneClippedByViewport(pileSideRegionRoot)}\n"
                + $"viewportRect={FormatWorldRect(viewportRect)}\n"
                + $"contentRect={FormatWorldRect(contentRect)}\n"
                + $"clippedByViewport={IsBoardZoneClippedByViewport(pileSideRegionRoot)}\n"
                + $"siblingIndex={FormatSiblingIndex(pileSideRegionRoot)}\n"
                + $"imageEnabled={FormatImageEnabled(pileSideRegionRoot)}\n"
                + $"imageColor={FormatImageColor(pileSideRegionRoot)}\n"
                + FormatPileZoneVisibility("OpponentDiscard", opponentDiscardAnchor)
                + FormatPileZoneVisibility("OpponentDeck", opponentDeckAnchor)
                + FormatPileZoneVisibility("PlayerDeck", playerDeckAnchor)
                + FormatPileZoneVisibility("PlayerDiscard", playerDiscardAnchor);
        }

        string FormatPileZoneVisibility(string zoneName, RectTransform zone)
        {
            return "PileZoneVisibility:\n"
                + $"zone={zoneName}\n"
                + $"activeSelf={FormatActiveSelf(zone)}\n"
                + $"activeInHierarchy={IsActiveInHierarchy(zone)}\n"
                + $"active={IsActiveInHierarchy(zone)}\n"
                + $"alpha={GetCanvasGroupAlpha(zone):0.00}\n"
                + $"parent={FormatParentPath(zone)}\n"
                + $"anchoredPosition={FormatAnchoredPosition(zone)}\n"
                + $"worldPosition={FormatWorldPosition(zone)}\n"
                + $"sizeDelta={FormatSizeDelta(zone)}\n"
                + $"worldRect={FormatWorldRect(zone)}\n"
                + $"clippedByViewport={IsBoardZoneClippedByViewport(zone)}\n"
                + $"siblingIndex={FormatSiblingIndex(zone)}\n"
                + $"imageEnabled={FormatImageEnabled(zone)}\n"
                + $"labelText={FormatZoneLabelText(zone)}\n";
        }

        string FormatPileRegionModeCompare()
        {
            RectTransform label = pileSideRegionRoot != null
                ? pileSideRegionRoot.Find("Region Debug Label") as RectTransform
                : null;

            return "PileRegionModeCompare:\n"
                + $"debugBoardZones={debugBoardZones}\n"
                + $"pileRegionActive={IsActiveInHierarchy(pileSideRegionRoot)}\n"
                + $"pileRegionAlpha={GetCanvasGroupAlpha(pileSideRegionRoot):0.00}\n"
                + $"pileRegionImageEnabled={FormatImageEnabled(pileSideRegionRoot)}\n"
                + $"pileRegionImageColor={FormatImageColor(pileSideRegionRoot)}\n"
                + $"pileRegionOutlineEnabled={FormatOutlineEnabled(pileSideRegionRoot)}\n"
                + $"pileRegionLabelActive={FormatActiveSelf(label)}\n"
                + $"pileRegionLabelText={FormatTextValue(label)}\n"
                + $"pileRegionAnchoredPosition={FormatAnchoredPosition(pileSideRegionRoot)}\n"
                + $"pileRegionSizeDelta={FormatSizeDelta(pileSideRegionRoot)}\n"
                + $"pileRegionWorldRect={FormatWorldRect(pileSideRegionRoot)}\n"
                + $"pileRegionSiblingIndex={FormatSiblingIndex(pileSideRegionRoot)}\n"
                + $"pileRegionClippedByViewport={IsBoardZoneClippedByViewport(pileSideRegionRoot)}\n";
        }

        string FormatPileZoneModeCompare(string zoneName, RectTransform zone)
        {
            RectTransform label = zone != null ? zone.Find("Zone Label") as RectTransform : null;
            RectTransform count = zone != null ? zone.Find("Zone Count") as RectTransform : null;

            return "PileZoneModeCompare:\n"
                + $"zone={zoneName}\n"
                + $"debugBoardZones={debugBoardZones}\n"
                + $"active={IsActiveInHierarchy(zone)}\n"
                + $"alpha={GetCanvasGroupAlpha(zone):0.00}\n"
                + $"backgroundAlpha={FormatImageAlpha(zone)}\n"
                + $"frameAlpha={FormatFrameAlpha(zone)}\n"
                + $"imageEnabled={FormatImageEnabled(zone)}\n"
                + $"imageColor={FormatImageColor(zone)}\n"
                + $"outlineEnabled={FormatOutlineEnabled(zone)}\n"
                + $"outlineAlpha={FormatOutlineAlpha(zone)}\n"
                + $"labelActive={FormatActiveSelf(label)}\n"
                + $"labelText={FormatTextValue(label)}\n"
                + $"labelAlpha={FormatTextAlpha(label)}\n"
                + $"numberText={FormatTextValue(count)}\n"
                + $"numberAlpha={FormatTextAlpha(count)}\n"
                + $"visibleInFormalMode={IsPileZoneVisibleInFormalMode(zone)}\n"
                + $"anchoredPosition={FormatAnchoredPosition(zone)}\n"
                + $"sizeDelta={FormatSizeDelta(zone)}\n"
                + $"worldRect={FormatWorldRect(zone)}\n"
                + $"siblingIndex={FormatSiblingIndex(zone)}\n"
                + $"clippedByViewport={IsBoardZoneClippedByViewport(zone)}\n";
        }

        string FormatSizeDelta(RectTransform rect)
        {
            if (rect == null) return "missing";
            return $"({rect.sizeDelta.x:0.#},{rect.sizeDelta.y:0.#})";
        }

        string FormatActiveSelf(RectTransform rect)
        {
            if (rect == null) return "missing";
            return rect.gameObject.activeSelf.ToString();
        }

        string FormatSiblingIndex(RectTransform rect)
        {
            if (rect == null) return "missing";
            return rect.GetSiblingIndex().ToString();
        }

        string FormatImageEnabled(RectTransform rect)
        {
            if (rect == null) return "missing";

            Image image = rect.GetComponent<Image>();
            return image != null ? image.enabled.ToString() : "no-image";
        }

        string FormatImageColor(RectTransform rect)
        {
            if (rect == null) return "missing";

            Image image = rect.GetComponent<Image>();
            return image != null ? FormatColor(image.color) : "no-image";
        }

        string FormatImageAlpha(RectTransform rect)
        {
            if (rect == null) return "missing";

            Image image = rect.GetComponent<Image>();
            return image != null ? image.color.a.ToString("0.00") : "no-image";
        }

        string FormatFrameAlpha(RectTransform rect)
        {
            if (rect == null) return "missing";

            RectTransform frame = rect.Find("Card Frame") as RectTransform;
            if (frame == null) return "missing";

            Image image = frame.GetComponent<Image>();
            return image != null ? image.color.a.ToString("0.00") : "no-image";
        }

        string FormatOutlineEnabled(RectTransform rect)
        {
            if (rect == null) return "missing";

            Outline outline = rect.GetComponent<Outline>();
            return outline != null ? outline.enabled.ToString() : "no-outline";
        }

        string FormatOutlineAlpha(RectTransform rect)
        {
            if (rect == null) return "missing";

            Outline outline = rect.GetComponent<Outline>();
            return outline != null ? outline.effectColor.a.ToString("0.00") : "no-outline";
        }

        string FormatTextValue(RectTransform rect)
        {
            if (rect == null) return "missing";

            Text text = rect.GetComponent<Text>();
            return text != null ? text.text : "no-text";
        }

        string FormatTextAlpha(RectTransform rect)
        {
            if (rect == null) return "missing";

            Text text = rect.GetComponent<Text>();
            return text != null ? text.color.a.ToString("0.00") : "no-text";
        }

        bool IsPileZoneVisibleInFormalMode(RectTransform zone)
        {
            if (zone == null || !zone.gameObject.activeInHierarchy) return false;
            if (GetCanvasGroupAlpha(zone) < 0.95f) return false;
            if (IsBoardZoneClippedByViewport(zone)) return false;

            Image image = zone.GetComponent<Image>();
            if (image == null || !image.enabled || image.color.a < 0.18f) return false;

            Outline outline = zone.GetComponent<Outline>();
            if (outline == null || !outline.enabled || outline.effectColor.a < 0.45f) return false;

            Text count = null;
            Transform countTransform = zone.Find("Zone Count");
            if (countTransform != null) count = countTransform.GetComponent<Text>();
            if (count == null || !count.gameObject.activeInHierarchy || count.color.a < 0.95f) return false;

            Text label = null;
            Transform labelTransform = zone.Find("Zone Label");
            if (labelTransform != null) label = labelTransform.GetComponent<Text>();
            if (label == null || !label.gameObject.activeInHierarchy || label.color.a < 0.95f) return false;

            return true;
        }

        string FormatZoneLabelText(RectTransform rect)
        {
            if (rect == null) return "missing";

            Transform labelTransform = rect.Find("Zone Label");
            Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
            if (label == null) return "missing";
            return label.text;
        }

        bool IsHandHolderHud()
        {
            if (cardHolder == null) return false;
            Transform current = cardHolder;
            while (current != null)
            {
                if (battlefieldManager != null && current == battlefieldManager.content) return false;
                if (battlefieldManager != null && current == battlefieldManager.transform) return false;
                current = current.parent;
            }

            return true;
        }

        string FormatReferenceBoardLayout()
        {
            return "ReferenceBoardLayout:\n"
                + $"usingFixedReferenceLayout={useFixedReferenceBoardLayout}\n"
                + $"combatRegionPos=({combatRegionPos.x:0.#},{combatRegionPos.y:0.#})\n"
                + $"combatRegionSize=({combatRegionSize.x:0.#},{combatRegionSize.y:0.#})\n"
                + $"pileRegionPos=({pileRegionPos.x:0.#},{pileRegionPos.y:0.#})\n"
                + $"pileRegionSize=({pileRegionSize.x:0.#},{pileRegionSize.y:0.#})\n"
                + $"opponentDiscardPos=({pileRegionPos.x + referenceOpponentPileGroupPos.x:0.#},{pileRegionPos.y + referenceOpponentPileGroupPos.y + referenceDeckLocalY:0.#})\n"
                + $"opponentDeckPos=({pileRegionPos.x + referenceOpponentPileGroupPos.x:0.#},{pileRegionPos.y + referenceOpponentPileGroupPos.y + referenceDiscardLocalY:0.#})\n"
                + $"playerDeckPos=({pileRegionPos.x + referencePlayerPileGroupPos.x:0.#},{pileRegionPos.y + referencePlayerPileGroupPos.y + referenceDeckLocalY:0.#})\n"
                + $"playerDiscardPos=({pileRegionPos.x + referencePlayerPileGroupPos.x:0.#},{pileRegionPos.y + referencePlayerPileGroupPos.y + referenceDiscardLocalY:0.#})\n"
                + $"sceneAreaPos=({combatRegionPos.x + GetReferenceSceneAreaPosition().x:0.#},{combatRegionPos.y + GetReferenceSceneAreaPosition().y:0.#})\n"
                + $"opponentBattleSlotPos=({GetReferenceOpponentBattleSlotPosition().x:0.#},{GetReferenceOpponentBattleSlotPosition().y:0.#})\n"
                + $"playerBattleSlotPos=({GetReferencePlayerBattleSlotPosition().x:0.#},{GetReferencePlayerBattleSlotPosition().y:0.#})\n";
        }

        string FormatSidePileLayoutDebug()
        {
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            return "SidePileLayout:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"battlefieldFocusOffsetX={_lastBattlefieldFocusOffsetX:0.#}\n"
                + $"baseSidePileColumnX={_lastBaseSidePileColumnX:0.#}\n"
                + $"finalSidePileColumnX={_lastFinalSidePileColumnX:0.#}\n"
                + $"sidePileScale={_lastSidePileScale:0.00}\n"
                + $"minGapFromLane={_lastSidePileMinGapFromLane:0.#}\n"
                + $"sidePileToLaneGap={FormatDebugFloat(_lastSidePileToLaneGap)}\n"
                + $"overlapWithLane={_lastSidePileOverlapWithLane}\n"
                + $"overlapWithRevealArea={_lastSidePileOverlapWithRevealArea}\n"
                + $"tooFar={_lastSidePileTooFar}\n"
                + $"clamped={_lastSidePileClamped}\n";
        }

        void LogSidePileLayoutDebug(string context)
        {
            if (!debugBoardZones && !debugBattlefieldLayout && !debugForceSidePileExtremeOffset) return;

            Debug.Log(
                $"SidePileLayout Debug: context={context}\n"
                + FormatBoardZoneParentCheck()
                + FormatBattlefieldRegionLayout()
                + FormatBattlefieldRegionVisibility()
                + FormatActiveLaneFocusCheck()
                + FormatBoardZoneDistanceCheck()
                + FormatBoardZoneTuning()
                + FormatReferenceBoardLayout()
                + FormatSidePanelLayout()
                + FormatSidePileRightSpaceTuning()
                + FormatSidePileFinalPositionTrace()
                + FormatBoardZoneObjectScan()
                + FormatSidePileLayoutDebug());
        }

        void LogPileRegionModeCompare(string context, bool force)
        {
            if (!force && !debugBoardZones && !debugBattlefieldLayout && !debugForceSidePileExtremeOffset) return;

            Debug.Log(
                $"PileRegionModeCompare Debug: context={context}\n"
                + FormatPileRegionModeCompare()
                + FormatPileZoneModeCompare("OpponentDiscard", opponentDiscardAnchor)
                + FormatPileZoneModeCompare("OpponentDeck", opponentDeckAnchor)
                + FormatPileZoneModeCompare("PlayerDeck", playerDeckAnchor)
                + FormatPileZoneModeCompare("PlayerDiscard", playerDiscardAnchor));
        }

        void LogPileSideInternalLayout(Vector2 zoneSize, float zoneGap, float topPadding, float bottomPadding, string source)
        {
            if (!debugBoardZones && !debugBattlefieldLayout) return;

            Debug.Log(
                "PileSideInternalLayout:\n"
                + $"source={source}\n"
                + $"pileRegionRect={FormatWorldRect(pileSideRegionRoot)}\n"
                + $"zoneWidth={zoneSize.x:0.#}\n"
                + $"zoneHeight={zoneSize.y:0.#}\n"
                + $"zoneGap={zoneGap:0.#}\n"
                + $"topPadding={topPadding:0.#}\n"
                + $"bottomPadding={bottomPadding:0.#}\n"
                + $"opDiscardRect={FormatWorldRect(opponentDiscardAnchor)}\n"
                + $"opDeckRect={FormatWorldRect(opponentDeckAnchor)}\n"
                + $"playerDeckRect={FormatWorldRect(playerDeckAnchor)}\n"
                + $"playerDiscardRect={FormatWorldRect(playerDiscardAnchor)}\n"
                + $"zoneInsidePileRegion={ArePileZonesInsidePileRegion()}\n"
                + $"zoneOverlap={DoPileZonesOverlap()}\n"
                + $"clippedByViewport={ArePileZonesClippedByViewport()}");
        }

        void LogPileSideInternalLayoutFromCurrent(string source)
        {
            if (!debugBoardZones && !debugBattlefieldLayout) return;

            Vector2 zoneSize = playerDeckAnchor != null
                ? playerDeckAnchor.sizeDelta
                : GetBoardZoneCardSize();
            float zoneGap = EstimatePileZoneGapFromCurrent();
            float topPadding = EstimatePileTopPaddingFromCurrent();
            float bottomPadding = EstimatePileBottomPaddingFromCurrent();
            LogPileSideInternalLayout(zoneSize, zoneGap, topPadding, bottomPadding, source);
        }

        bool ArePileZonesInsidePileRegion()
        {
            if (pileSideRegionRoot == null) return false;
            if (opponentDiscardAnchor == null
                || opponentDeckAnchor == null
                || playerDeckAnchor == null
                || playerDiscardAnchor == null)
            {
                return false;
            }

            Rect pileRect = pileSideRegionRoot.rect;
            return IsRectInside(GetRectInReferenceSpace(pileSideRegionRoot, opponentDiscardAnchor), pileRect)
                && IsRectInside(GetRectInReferenceSpace(pileSideRegionRoot, opponentDeckAnchor), pileRect)
                && IsRectInside(GetRectInReferenceSpace(pileSideRegionRoot, playerDeckAnchor), pileRect)
                && IsRectInside(GetRectInReferenceSpace(pileSideRegionRoot, playerDiscardAnchor), pileRect);
        }

        bool DoPileZonesOverlap()
        {
            if (pileSideRegionRoot == null) return false;

            RectTransform[] zones =
            {
                opponentDiscardAnchor,
                opponentDeckAnchor,
                playerDeckAnchor,
                playerDiscardAnchor
            };

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null) continue;
                Rect first = GetRectInReferenceSpace(pileSideRegionRoot, zones[i]);
                for (int j = i + 1; j < zones.Length; j++)
                {
                    if (zones[j] == null) continue;
                    Rect second = GetRectInReferenceSpace(pileSideRegionRoot, zones[j]);
                    if (first.Overlaps(second)) return true;
                }
            }

            return false;
        }

        bool ArePileZonesClippedByViewport()
        {
            return IsBoardZoneClippedByViewport(opponentDiscardAnchor)
                || IsBoardZoneClippedByViewport(opponentDeckAnchor)
                || IsBoardZoneClippedByViewport(playerDeckAnchor)
                || IsBoardZoneClippedByViewport(playerDiscardAnchor);
        }

        bool IsRectInside(Rect inner, Rect outer)
        {
            const float tolerance = 0.1f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        float EstimatePileZoneGapFromCurrent()
        {
            if (pileSideRegionRoot == null || opponentDiscardAnchor == null || opponentDeckAnchor == null) return 0f;

            Rect top = GetRectInReferenceSpace(pileSideRegionRoot, opponentDiscardAnchor);
            Rect next = GetRectInReferenceSpace(pileSideRegionRoot, opponentDeckAnchor);
            return Mathf.Max(0f, top.yMin - next.yMax);
        }

        float EstimatePileTopPaddingFromCurrent()
        {
            if (pileSideRegionRoot == null || opponentDiscardAnchor == null) return 0f;

            Rect pileRect = pileSideRegionRoot.rect;
            Rect top = GetRectInReferenceSpace(pileSideRegionRoot, opponentDiscardAnchor);
            return Mathf.Max(0f, pileRect.yMax - top.yMax);
        }

        float EstimatePileBottomPaddingFromCurrent()
        {
            if (pileSideRegionRoot == null || playerDiscardAnchor == null) return 0f;

            Rect pileRect = pileSideRegionRoot.rect;
            Rect bottom = GetRectInReferenceSpace(pileSideRegionRoot, playerDiscardAnchor);
            return Mathf.Max(0f, bottom.yMin - pileRect.yMin);
        }

        string FormatBoardZoneObjectScan()
        {
            var output = new System.Text.StringBuilder();
            output.Append("BoardZoneObjectScan:\n");

            RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int printedCount = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (!IsBoardZoneScanName(rect.name)) continue;

                Image image = rect.GetComponent<Image>();
                CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
                output.Append("name=").Append(rect.name).Append('\n');
                output.Append("instanceID=").Append(rect.gameObject.GetInstanceID()).Append('\n');
                output.Append("path=").Append(FormatTransformPath(rect)).Append('\n');
                output.Append("activeSelf=").Append(rect.gameObject.activeSelf).Append('\n');
                output.Append("activeInHierarchy=").Append(rect.gameObject.activeInHierarchy).Append('\n');
                output.Append("parent=").Append(FormatParentPath(rect)).Append('\n');
                output.Append("anchoredPosition=").Append(FormatAnchoredPosition(rect)).Append('\n');
                output.Append("worldPosition=").Append(FormatWorldPosition(rect)).Append('\n');
                output.Append("sizeDelta=(").Append(rect.sizeDelta.x.ToString("0.#")).Append(',').Append(rect.sizeDelta.y.ToString("0.#")).Append(")\n");
                output.Append("alpha=").Append(image != null ? image.color.a.ToString("0.00") : "none").Append('\n');
                output.Append("canvasGroupAlpha=").Append(canvasGroup != null ? canvasGroup.alpha.ToString("0.00") : "none").Append('\n');
                output.Append("hasImage=").Append(image != null).Append('\n');
                output.Append("imageColor=").Append(image != null ? FormatColor(image.color) : "none").Append('\n');
                output.Append("raycastTarget=").Append(image != null && image.raycastTarget).Append('\n');
                output.Append("siblingIndex=").Append(rect.GetSiblingIndex()).Append('\n');
                printedCount++;
                if (printedCount >= 80)
                {
                    output.Append("scanTruncated=true\n");
                    break;
                }
            }

            output.Append("scanCount=").Append(printedCount).Append('\n');
            return output.ToString();
        }

        bool IsBoardZoneScanName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;

            string lower = objectName.ToLowerInvariant();
            return lower.Contains("deck")
                || lower.Contains("discard")
                || lower.Contains("boardzone")
                || lower.Contains("board zone")
                || lower.Contains("pile")
                || lower.Contains("sidepile")
                || lower.Contains("playerdeck")
                || lower.Contains("playerdiscard")
                || lower.Contains("opponentdeck")
                || lower.Contains("opponentdiscard");
        }

        string FormatVisibleBoardZoneHierarchy(string zoneName, RectTransform zoneRoot)
        {
            RectTransform frame = zoneRoot != null ? zoneRoot.Find("Card Frame") as RectTransform : null;
            RectTransform cardBack = frame;
            RectTransform label = zoneRoot != null ? zoneRoot.Find("Zone Label") as RectTransform : null;
            RectTransform count = zoneRoot != null ? zoneRoot.Find("Zone Count") as RectTransform : null;

            return "VisibleBoardZoneHierarchy:\n"
                + $"zone={zoneName}\n"
                + $"rootPath={FormatTransformPath(zoneRoot)}\n"
                + $"rootInstanceID={FormatInstanceId(zoneRoot)}\n"
                + $"rootAnchoredPosition={FormatAnchoredPosition(zoneRoot)}\n"
                + $"rootWorldPos={FormatWorldPosition(zoneRoot)}\n"
                + $"framePath={FormatTransformPath(frame)}\n"
                + $"frameInstanceID={FormatInstanceId(frame)}\n"
                + $"frameAnchoredPosition={FormatAnchoredPosition(frame)}\n"
                + $"frameWorldPos={FormatWorldPosition(frame)}\n"
                + $"cardBackPath={FormatTransformPath(cardBack)}\n"
                + $"cardBackAnchoredPosition={FormatAnchoredPosition(cardBack)}\n"
                + $"cardBackWorldPos={FormatWorldPosition(cardBack)}\n"
                + $"labelPath={FormatTransformPath(label)}\n"
                + $"labelAnchoredPosition={FormatAnchoredPosition(label)}\n"
                + $"labelWorldPos={FormatWorldPosition(label)}\n"
                + $"countPath={FormatTransformPath(count)}\n"
                + $"countAnchoredPosition={FormatAnchoredPosition(count)}\n"
                + $"countWorldPos={FormatWorldPosition(count)}\n";
        }

        string FormatColor(Color color)
        {
            return $"({color.r:0.00},{color.g:0.00},{color.b:0.00},{color.a:0.00})";
        }

        string FormatSidePileFinalPositionTrace()
        {
            RectTransform boardZoneRoot = GetBoardZoneLayoutRoot();
            RectTransform contentRoot = battlefieldManager != null ? battlefieldManager.content : null;
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            float nearestLaneRightEdgeLocal = Mathf.Max(_lastPlayerLaneRightEdge, _lastOpponentLaneRightEdge);
            float playerSidePileWorldX = GetSidePileGroupWorldX("Player");
            float opponentSidePileWorldX = GetSidePileGroupWorldX("Opponent");

            return "SidePileFinalPositionTrace:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"nearestLaneRightEdgeLocal={FormatDebugFloat(nearestLaneRightEdgeLocal)}\n"
                + $"nearestLaneRightEdgeWorld={FormatDebugFloat(_lastNearestLaneRightEdgeWorld)}\n"
                + $"baseSidePileColumnX={FormatDebugFloat(_lastBaseSidePileColumnX)}\n"
                + $"sidePileToLaneGap={sidePileToLaneGap:0.#}\n"
                + $"sidePileMinGapFromLane={sidePileMinGapFromLane:0.#}\n"
                + $"sidePileColumnNudgeX={sidePileColumnNudgeX:0.#}\n"
                + $"computedSidePileColumnX={FormatDebugFloat(_lastComputedSidePileColumnX)}\n"
                + $"nudgedSidePileColumnX={FormatDebugFloat(_lastNudgedSidePileColumnX)}\n"
                + $"beforeClampSidePileColumnX={FormatDebugFloat(_lastBeforeClampSidePileColumnX)}\n"
                + $"clampMinX={FormatDebugFloat(_lastClampMinX)}\n"
                + $"clampMaxX={FormatDebugFloat(_lastClampMaxX)}\n"
                + $"clampedSidePileColumnX={FormatDebugFloat(_lastClampedSidePileColumnX)}\n"
                + $"finalSidePileColumnX={FormatDebugFloat(_lastFinalSidePileColumnX)}\n"
                + $"sidePileColumnAnchoredPositionBefore={FormatDebugFloat(_lastSidePileColumnBeforeX)}\n"
                + $"sidePileColumnAnchoredPositionAfter={FormatDebugFloat(_lastSidePileColumnAfterX)}\n"
                + $"sidePileLocalX={FormatDebugFloat(_lastSidePileColumnAfterX)}\n"
                + $"playerSidePileWorldX={FormatDebugFloat(playerSidePileWorldX)}\n"
                + $"opponentSidePileWorldX={FormatDebugFloat(opponentSidePileWorldX)}\n"
                + $"boardZoneRootAnchoredPosition={FormatBoardLayoutPosition(boardZoneRoot)}\n"
                + $"battlefieldContentAnchoredPosition={FormatBoardLayoutPosition(contentRoot)}\n"
                + $"battlefieldContentLocalX={FormatDebugFloat(_lastBattlefieldContentAnchoredX)}\n"
                + $"activeLaneFocusOffsetX={FormatDebugFloat(_lastBattlefieldFocusOffsetX)}\n"
                + $"isClamped={_lastSidePileClamped}\n"
                + $"clampReason={_lastSidePileClampReason}\n"
                + $"overwrittenByLayout={_lastSidePileOverwrittenByLayout}\n"
                + $"usedGapInFormula={_lastSidePileUsedGapInFormula}\n"
                + $"extremeOffsetApplied={_lastSidePileExtremeOffsetApplied}\n";
        }

        string FormatBoardZoneTuning()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            float nearestLaneRightEdge = Mathf.Max(_lastPlayerLaneRightEdge, _lastOpponentLaneRightEdge);

            return "BoardZoneTuning:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"sidePileColumnX={FormatDebugFloat(_lastFinalSidePileColumnX)}\n"
                + $"playerDeckX={FormatDebugFloat(GetRectCenterXInReference(root, playerDeckAnchor))}\n"
                + $"playerDiscardX={FormatDebugFloat(GetRectCenterXInReference(root, playerDiscardAnchor))}\n"
                + $"opponentDeckX={FormatDebugFloat(GetRectCenterXInReference(root, opponentDeckAnchor))}\n"
                + $"opponentDiscardX={FormatDebugFloat(GetRectCenterXInReference(root, opponentDiscardAnchor))}\n"
                + $"nearestLaneRightEdge={FormatDebugFloat(nearestLaneRightEdge)}\n"
                + $"sidePileToLaneGap={FormatDebugFloat(_lastSidePileToLaneGap)}\n"
                + $"tooFar={_lastSidePileTooFar}\n"
                + $"overlapWithLane={_lastSidePileOverlapWithLane}\n";
        }

        string FormatSidePanelLayout()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            Vector2 zoneSize = GetBoardZoneCardSize();
            float boardWidth = root != null && root.rect.width > 0f ? root.rect.width : 1040f;
            float sidePanelRightX = GetSidePanelRightX(root, boardWidth);
            float sidePanelLeftX = GetSidePanelLeftX(root, boardWidth, zoneSize);
            float sidePanelColumnX = GetSidePanelColumnX(root, boardWidth, zoneSize);
            float sidePanelWorldX = root != null
                ? root.TransformPoint(new Vector3(sidePanelColumnX, 0f, 0f)).x
                : float.MinValue;
            float battlefieldRightEdge = GetVisibleBattlefieldRightInReference(root, boardWidth);
            float currentSidePileX = playerSidePileGroup != null ? playerSidePileGroup.anchoredPosition.x : float.MinValue;
            float currentSidePileRightEdge = currentSidePileX != float.MinValue
                ? currentSidePileX + zoneSize.x * 0.5f
                : float.MinValue;
            bool clippedByRightEdge = currentSidePileRightEdge != float.MinValue
                && battlefieldRightEdge != float.MinValue
                && currentSidePileRightEdge > battlefieldRightEdge + 0.1f;

            return "SidePanelLayout:\n"
                + $"sidePanelRootPath={FormatTransformPath(root)}\n"
                + $"sidePanelLocalX={FormatDebugFloat(sidePanelColumnX)}\n"
                + $"sidePanelWorldX={FormatDebugFloat(sidePanelWorldX)}\n"
                + $"sidePanelLeftEdge={FormatDebugFloat(sidePanelLeftX)}\n"
                + $"sidePanelRightEdge={FormatDebugFloat(sidePanelRightX)}\n"
                + $"sidePanelWidth={FormatDebugFloat(GetEffectiveSidePanelWidth(zoneSize))}\n"
                + $"battlefieldRightEdge={FormatDebugFloat(battlefieldRightEdge)}\n"
                + $"rightEmptySpaceAfter={FormatDebugFloat(battlefieldRightEdge - currentSidePileRightEdge)}\n"
                + $"overlapWithLane={_lastSidePileOverlapWithLane}\n"
                + $"clippedByRightEdge={clippedByRightEdge}\n"
                + $"OpponentDeckWorldRect={FormatWorldRect(opponentDeckAnchor)}\n"
                + $"OpponentDiscardWorldRect={FormatWorldRect(opponentDiscardAnchor)}\n"
                + $"PlayerDeckWorldRect={FormatWorldRect(playerDeckAnchor)}\n"
                + $"PlayerDiscardWorldRect={FormatWorldRect(playerDiscardAnchor)}\n";
        }

        string FormatSidePileRightSpaceTuning()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            Vector2 zoneSize = GetBoardZoneCardSize();
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;
            float currentSidePileX = playerSidePileGroup != null
                ? playerSidePileGroup.anchoredPosition.x
                : _lastFinalSidePileColumnX;
            float targetSidePileX = _lastClampMaxX;
            float rightEdgeOfSidePile = currentSidePileX + zoneSize.x * 0.5f;
            float boardWidth = root != null && root.rect.width > 0f ? root.rect.width : 1040f;
            float battlefieldRightEdge = GetVisibleBattlefieldRightInReference(root, boardWidth);
            float sidePanelRightEdge = root != null
                ? GetSidePanelRightX(root, boardWidth)
                : float.MinValue;
            float computedRightEdge = _lastComputedSidePileColumnX != float.MinValue
                ? _lastComputedSidePileColumnX + zoneSize.x * 0.5f
                : float.MinValue;
            float rightEmptySpaceBefore = battlefieldRightEdge != float.MinValue && computedRightEdge != float.MinValue
                ? battlefieldRightEdge - computedRightEdge
                : float.MinValue;
            float rightEmptySpaceAfter = battlefieldRightEdge != float.MinValue
                ? battlefieldRightEdge - rightEdgeOfSidePile
                : float.MinValue;

            return "SidePileRightSpaceTuning:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"currentSidePileX={FormatDebugFloat(currentSidePileX)}\n"
                + $"targetSidePileX={FormatDebugFloat(targetSidePileX)}\n"
                + $"rightSafeMargin={GetSidePanelRightMargin():0.#}\n"
                + $"legacySidePileRightMargin={Mathf.Clamp(sidePileRightMargin, 0f, 40f):0.#}\n"
                + $"rightEdgeOfSidePile={FormatDebugFloat(rightEdgeOfSidePile)}\n"
                + $"sidePanelRightEdge={FormatDebugFloat(sidePanelRightEdge)}\n"
                + $"battlefieldRightEdge={FormatDebugFloat(battlefieldRightEdge)}\n"
                + $"rightEmptySpaceBefore={FormatDebugFloat(rightEmptySpaceBefore)}\n"
                + $"rightEmptySpaceAfter={FormatDebugFloat(rightEmptySpaceAfter)}\n"
                + $"overlapWithLane={_lastSidePileOverlapWithLane}\n"
                + $"clamped={_lastSidePileClamped}\n";
        }

        string FormatBoardZoneDistanceCheck()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;

            return "BoardZoneDistanceCheck:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"playerLaneRightEdge={FormatDebugFloat(_lastPlayerLaneRightEdge)}\n"
                + $"opponentLaneRightEdge={FormatDebugFloat(_lastOpponentLaneRightEdge)}\n"
                + $"playerDeckX={FormatDebugFloat(GetRectCenterXInReference(root, playerDeckAnchor))}\n"
                + $"playerDiscardX={FormatDebugFloat(GetRectCenterXInReference(root, playerDiscardAnchor))}\n"
                + $"opponentDeckX={FormatDebugFloat(GetRectCenterXInReference(root, opponentDeckAnchor))}\n"
                + $"opponentDiscardX={FormatDebugFloat(GetRectCenterXInReference(root, opponentDiscardAnchor))}\n"
                + $"sidePileToLaneGap={FormatDebugFloat(_lastSidePileToLaneGap)}\n"
                + $"tooFar={_lastSidePileTooFar}\n"
                + $"overlapWithLane={_lastSidePileOverlapWithLane}\n";
        }

        string FormatBoardZoneParentCheck()
        {
            RectTransform boardZoneRoot = GetBoardZoneLayoutRoot();
            RectTransform contentRoot = battlefieldManager != null ? battlefieldManager.content : null;
            Transform laneParent = GetFirstBattleLaneParent();
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;

            return "BoardZoneParentCheck:\n"
                + $"sceneParent={FormatParentPath(sceneZoneAnchor)}\n"
                + $"playerDeckParent={FormatParentPath(playerDeckAnchor)}\n"
                + $"playerDiscardParent={FormatParentPath(playerDiscardAnchor)}\n"
                + $"opponentDeckParent={FormatParentPath(opponentDeckAnchor)}\n"
                + $"opponentDiscardParent={FormatParentPath(opponentDiscardAnchor)}\n"
                + $"laneRootParent={FormatTransformPath(laneParent)}\n"
                + $"boardZoneRootParent={FormatParentPath(boardZoneRoot)}\n"
                + $"battlefieldContentPos={FormatBoardLayoutPosition(contentRoot)}\n"
                + $"boardZoneRootPos={FormatBoardLayoutPosition(boardZoneRoot)}\n"
                + $"activeLaneIndex={activeLaneIndex}\n";
        }

        string FormatActiveLaneFocusCheck()
        {
            RectTransform laneRoot = battlefieldManager != null ? battlefieldManager.lanesRoot : null;
            RectTransform contentRoot = battlefieldManager != null ? battlefieldManager.content : null;
            int activeLaneIndex = turnManager != null ? turnManager.ActiveNewLaneIndex : -1;

            return "ActiveLaneFocusCheck:\n"
                + $"activeLaneIndex={activeLaneIndex}\n"
                + $"battlefieldContentAnchoredPosition={FormatAnchoredPosition(contentRoot)}\n"
                + $"laneRootWorldPos={FormatWorldPosition(laneRoot)}\n"
                + $"sceneWorldPos={FormatWorldPosition(sceneZoneAnchor)}\n"
                + $"playerDeckWorldPos={FormatWorldPosition(playerDeckAnchor)}\n"
                + $"playerDiscardWorldPos={FormatWorldPosition(playerDiscardAnchor)}\n"
                + $"opponentDeckWorldPos={FormatWorldPosition(opponentDeckAnchor)}\n"
                + $"opponentDiscardWorldPos={FormatWorldPosition(opponentDiscardAnchor)}\n";
        }

        Transform GetFirstBattleLaneParent()
        {
            if (battlefieldManager == null) return null;

            List<UcgBattleLane> lanes = battlefieldManager.GetAllLanes();
            for (int i = 0; i < lanes.Count; i++)
            {
                if (lanes[i] != null && lanes[i].transform != null)
                {
                    return lanes[i].transform.parent;
                }
            }

            return battlefieldManager.lanesRoot;
        }

        string FormatParentPath(RectTransform rect)
        {
            if (rect == null) return "missing";
            return FormatTransformPath(rect.parent);
        }

        string FormatTransformPath(Transform transform)
        {
            if (transform == null) return "none";

            var path = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        string FormatInstanceId(Component component)
        {
            return component != null ? component.gameObject.GetInstanceID().ToString() : "missing";
        }

        bool IsActiveInHierarchy(Component component)
        {
            return component != null && component.gameObject.activeInHierarchy;
        }

        string FormatAnchoredPosition(RectTransform rect)
        {
            if (rect == null) return "missing";
            return $"({rect.anchoredPosition.x:0.#},{rect.anchoredPosition.y:0.#})";
        }

        string FormatWorldPosition(RectTransform rect)
        {
            if (rect == null) return "missing";

            Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
            return $"({worldCenter.x:0.#},{worldCenter.y:0.#},{worldCenter.z:0.#})";
        }

        string FormatWorldRect(RectTransform rect)
        {
            if (rect == null) return "missing";

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                maxX = Mathf.Max(maxX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y);
                maxY = Mathf.Max(maxY, corners[i].y);
            }

            return $"x=({minX:0.#},{maxX:0.#}), y=({minY:0.#},{maxY:0.#})";
        }

        bool IsAnyBoardZoneClippedByViewport()
        {
            return IsBoardZoneClippedByViewport(playerDeckAnchor)
                || IsBoardZoneClippedByViewport(playerDiscardAnchor)
                || IsBoardZoneClippedByViewport(opponentDeckAnchor)
                || IsBoardZoneClippedByViewport(opponentDiscardAnchor)
                || IsBoardZoneClippedByViewport(sceneZoneAnchor);
        }

        bool IsBoardZoneClippedByViewport(RectTransform rect)
        {
            if (rect == null || battlefieldManager == null || battlefieldManager.viewport == null) return false;

            Vector3[] zoneCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            rect.GetWorldCorners(zoneCorners);
            battlefieldManager.viewport.GetWorldCorners(viewportCorners);

            float minX = zoneCorners[0].x;
            float maxX = zoneCorners[0].x;
            float minY = zoneCorners[0].y;
            float maxY = zoneCorners[0].y;
            for (int i = 1; i < zoneCorners.Length; i++)
            {
                minX = Mathf.Min(minX, zoneCorners[i].x);
                maxX = Mathf.Max(maxX, zoneCorners[i].x);
                minY = Mathf.Min(minY, zoneCorners[i].y);
                maxY = Mathf.Max(maxY, zoneCorners[i].y);
            }

            float viewportMinX = viewportCorners[0].x;
            float viewportMaxX = viewportCorners[0].x;
            float viewportMinY = viewportCorners[0].y;
            float viewportMaxY = viewportCorners[0].y;
            for (int i = 1; i < viewportCorners.Length; i++)
            {
                viewportMinX = Mathf.Min(viewportMinX, viewportCorners[i].x);
                viewportMaxX = Mathf.Max(viewportMaxX, viewportCorners[i].x);
                viewportMinY = Mathf.Min(viewportMinY, viewportCorners[i].y);
                viewportMaxY = Mathf.Max(viewportMaxY, viewportCorners[i].y);
            }

            return minX < viewportMinX - 0.1f
                || maxX > viewportMaxX + 0.1f
                || minY < viewportMinY - 0.1f
                || maxY > viewportMaxY + 0.1f;
        }

        float GetRectCenterXInReference(RectTransform reference, RectTransform rectTransform)
        {
            if (reference == null || rectTransform == null) return float.MinValue;

            Rect rect = GetRectInReferenceSpace(reference, rectTransform);
            return rect.center.x;
        }

        string FormatDebugFloat(float value)
        {
            return value == float.MinValue ? "missing" : $"{value:0.#}";
        }

        float GetDebugAppliedRightSidePileColumnDownShift()
        {
            RectTransform root = GetBoardZoneLayoutRoot();
            if (root == null) return 0f;

            Vector2 zoneSize = GetBoardZoneCardSize();
            float boardHeight = root.rect.height > 0f ? root.rect.height : 820f;
            float zoneVerticalGap = GetBoardZoneVerticalGap(zoneSize);
            float groupHeight = zoneSize.y * 2f + zoneVerticalGap;
            float defaultGroupY = Mathf.Clamp(
                boardHeight * 0.5f - groupHeight * 0.5f - 18f,
                groupHeight * 0.28f,
                boardHeight * 0.5f - groupHeight * 0.48f);
            float separatedGroupY = (groupHeight + Mathf.Max(0f, pileGroupVerticalSeparation)) * 0.5f;
            float maxGroupY = Mathf.Max(groupHeight * 0.28f, boardHeight * 0.5f - groupHeight * 0.5f - 6f);
            float groupY = Mathf.Clamp(Mathf.Max(defaultGroupY, separatedGroupY), groupHeight * 0.28f, maxGroupY);
            float stackOffsetY = zoneSize.y * 0.5f + zoneVerticalGap * 0.5f;
            return GetAppliedRightSidePileColumnDownShift(boardHeight, groupY, stackOffsetY, zoneSize);
        }

        string FormatBoardLayoutPosition(RectTransform rect)
        {
            if (rect == null) return "missing";
            return $"({rect.anchoredPosition.x:0.#},{rect.anchoredPosition.y:0.#}) size=({rect.sizeDelta.x:0.#},{rect.sizeDelta.y:0.#})";
        }

        RectTransform GetSceneZoneFrameRect()
        {
            if (sceneZoneAnchor == null || sceneZoneAnchor.parent == null) return null;
            return sceneZoneAnchor.parent.Find("Scene Area Mat Frame") as RectTransform;
        }

        string FormatBoardZoneDebug(string zoneName, RectTransform rect)
        {
            if (rect == null)
            {
                return $"BoardZone:\nname={zoneName}\nactive=false\nreason=missing\n";
            }

            Image image = rect.GetComponent<Image>();
            Outline outline = rect.GetComponent<Outline>();
            float alpha = image != null ? image.color.a : -1f;
            float outlineAlpha = outline != null ? outline.effectColor.a : -1f;
            bool raycastTarget = image != null && image.raycastTarget;
            string parentName = rect.parent != null ? rect.parent.name : "none";
            RectTransform groupRect = rect.parent as RectTransform;
            Vector2 groupPosition = groupRect != null ? groupRect.anchoredPosition : Vector2.zero;
            Vector2 innerCardSize = GetBoardZoneInnerCardSize(rect);
            float zoneGap = GetBoardZoneVerticalGap(GetBoardZoneCardSize());
            bool overlapWithLane = DoesBoardZoneOverlapAnyLane(rect);
            bool overlapWithRevealArea = DoesBoardZoneOverlapRevealArea(rect);

            return "VisibleBoardZone:\n"
                + $"name={zoneName}\n"
                + $"instanceID={rect.gameObject.GetInstanceID()}\n"
                + $"path={FormatTransformPath(rect)}\n"
                + $"parent={FormatParentPath(rect)}\n"
                + $"anchoredPosition=({rect.anchoredPosition.x:0.#},{rect.anchoredPosition.y:0.#})\n"
                + $"worldPosition={FormatWorldPosition(rect)}\n"
                + $"activeInHierarchy={rect.gameObject.activeInHierarchy}\n"
                + $"canvasGroupAlpha={GetCanvasGroupAlpha(rect):0.00}\n"
                + $"layoutControllers={FormatLayoutControllers(rect)}\n"
                + "BoardZone:\n"
                + $"name={zoneName}\n"
                + $"group={parentName}\n"
                + $"activeSelf={rect.gameObject.activeSelf}\n"
                + $"activeInHierarchy={rect.gameObject.activeInHierarchy}\n"
                + $"alpha={alpha:0.00}\n"
                + $"outlineAlpha={outlineAlpha:0.00}\n"
                + $"groupPos=({groupPosition.x:0.#},{groupPosition.y:0.#})\n"
                + $"pos=({rect.anchoredPosition.x:0.#},{rect.anchoredPosition.y:0.#})\n"
                + $"size=({rect.sizeDelta.x:0.#},{rect.sizeDelta.y:0.#})\n"
                + $"innerCardSize=({innerCardSize.x:0.#},{innerCardSize.y:0.#})\n"
                + $"gap={zoneGap:0.#}\n"
                + $"siblingIndex={rect.GetSiblingIndex()}\n"
                + $"raycastTarget={raycastTarget}\n"
                + $"overlapWithLane={overlapWithLane}\n"
                + $"overlapWithRevealArea={overlapWithRevealArea}\n"
                + $"parent={parentName}\n";
        }

        float GetCanvasGroupAlpha(RectTransform rect)
        {
            if (rect == null) return -1f;

            CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
            return canvasGroup != null ? canvasGroup.alpha : 1f;
        }

        string FormatLayoutControllers(RectTransform rect)
        {
            if (rect == null) return "missing";

            var controllers = new List<string>();
            if (rect.GetComponent<HorizontalLayoutGroup>() != null) controllers.Add("HorizontalLayoutGroup");
            if (rect.GetComponent<VerticalLayoutGroup>() != null) controllers.Add("VerticalLayoutGroup");
            if (rect.GetComponent<GridLayoutGroup>() != null) controllers.Add("GridLayoutGroup");
            if (rect.GetComponent<ContentSizeFitter>() != null) controllers.Add("ContentSizeFitter");
            if (rect.GetComponent<AspectRatioFitter>() != null) controllers.Add("AspectRatioFitter");
            if (rect.GetComponent<LayoutElement>() != null) controllers.Add("LayoutElement");

            return controllers.Count > 0 ? string.Join(",", controllers) : "none";
        }

        Vector2 GetBoardZoneInnerCardSize(RectTransform zone)
        {
            if (zone == null) return Vector2.zero;

            RectTransform innerCard = zone.Find("Card Frame") as RectTransform;
            if (innerCard == null) return Vector2.zero;

            Vector2 parentSize = zone.sizeDelta;
            return new Vector2(
                parentSize.x * Mathf.Abs(innerCard.anchorMax.x - innerCard.anchorMin.x) + innerCard.sizeDelta.x,
                parentSize.y * Mathf.Abs(innerCard.anchorMax.y - innerCard.anchorMin.y) + innerCard.sizeDelta.y);
        }

        bool DoesBoardZoneOverlapAnyLane(RectTransform zone)
        {
            if (zone == null || battlefieldManager == null) return false;

            List<UcgBattleLane> lanes = battlefieldManager.GetAllLanes();
            for (int i = 0; i < lanes.Count; i++)
            {
                UcgBattleLane lane = lanes[i];
                if (lane == null || !lane.gameObject.activeInHierarchy) continue;

                if (RectTransformsOverlapOnBattlefield(zone, lane.playerSlot)) return true;
                if (RectTransformsOverlapOnBattlefield(zone, lane.opponentSlot)) return true;
            }

            return false;
        }

        bool DoesBoardZoneOverlapRevealArea(RectTransform zone)
        {
            if (zone == null) return false;

            RectTransform revealRect = _deckOperationCardsRoot != null
                ? _deckOperationCardsRoot
                : _deckOperationSelectionRoot;
            if (revealRect == null || !revealRect.gameObject.activeInHierarchy) return false;

            return RectTransformsOverlapOnBattlefield(zone, revealRect);
        }

        bool RectTransformsOverlapOnBattlefield(RectTransform first, RectTransform second)
        {
            if (first == null || second == null) return false;

            RectTransform reference = battlefieldManager != null
                ? battlefieldManager.transform as RectTransform
                : canvas != null ? canvas.transform as RectTransform : null;
            if (reference == null) return false;

            Rect firstRect = GetRectInReferenceSpace(reference, first);
            Rect secondRect = GetRectInReferenceSpace(reference, second);
            return firstRect.Overlaps(secondRect);
        }

        Rect GetRectInReferenceSpace(RectTransform reference, RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector3 firstCorner = reference.InverseTransformPoint(corners[0]);
            float minX = firstCorner.x;
            float maxX = firstCorner.x;
            float minY = firstCorner.y;
            float maxY = firstCorner.y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 localCorner = reference.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, localCorner.x);
                maxX = Mathf.Max(maxX, localCorner.x);
                minY = Mathf.Min(minY, localCorner.y);
                maxY = Mathf.Max(maxY, localCorner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        int GetPlayerDeckCount()
        {
            return deckManager != null ? deckManager.RemainingCount : 0;
        }

        int GetOpponentDeckCount()
        {
            return _opponentDeckCount;
        }

        int GetPlayerHandCount()
        {
            return deckManager != null && deckManager.PlayerHand != null
                ? deckManager.PlayerHand.Count
                : (cardHolder != null ? cardHolder.childCount : 0);
        }

        int GetOpponentHandCount()
        {
            return _opponentHandCount;
        }

        int GetPlayerDiscardCount()
        {
            return _playerDiscardPile.Count;
        }

        int GetOpponentDiscardCount()
        {
            return _opponentDiscardPile.Count;
        }

        void ShowPlayerDiscardPile()
        {
            ShowDiscardPilePanel(UcgPlayerSide.Player);
        }

        void ShowOpponentDiscardPile()
        {
            ShowDiscardPilePanel(UcgPlayerSide.Opponent);
        }

        void ShowDiscardPilePanel(UcgPlayerSide side)
        {
            if (discardPilePanel == null || discardPilePanelText == null) return;

            var pile = side == UcgPlayerSide.Player ? _playerDiscardPile : _opponentDiscardPile;
            string ownerText = side == UcgPlayerSide.Player ? "我方" : "對手";
            string text = $"{ownerText}棄牌區\n\n";

            if (pile.Count == 0)
            {
                text += "棄牌區目前沒有卡牌";
            }
            else
            {
                for (int i = 0; i < pile.Count; i++)
                {
                    UcgCardData card = pile[i];
                    string typeText = card != null && card.IsSceneCard() ? "場景" : "角色";
                    string costText = card != null && card.IsSceneCard() ? $"｜{card.sceneTurnCost}燈" : "";
                    string nameText = card != null ? card.cardName : "未知卡牌";
                    text += $"{i + 1}. {nameText}｜{typeText}{costText}\n";
                }
            }

            discardPilePanelText.text = text;
            discardPilePanel.gameObject.SetActive(true);
            discardPilePanel.SetAsLastSibling();
        }

        void HideDiscardPilePanel()
        {
            if (discardPilePanel != null)
            {
                discardPilePanel.gameObject.SetActive(false);
            }
        }

        int GetSpriteIndexForCard(UcgCardData data)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) return 0;

            int lastDash = data.id.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= data.id.Length - 1) return 0;

            int parsedIndex;
            if (int.TryParse(data.id.Substring(lastDash + 1), out parsedIndex))
            {
                return Mathf.Clamp(parsedIndex - 1, 0, DemoCardCount - 1);
            }

            return 0;
        }

        UcgCardData CreateDemoCardData(int index)
        {
            switch (currentTestMode)
            {
                case UcgTestMode.MonsterAlienTest:
                    return CreateMonsterAlienTestCard(index);
                case UcgTestMode.TeamTest:
                    return CreateTeamTestCard(index);
                default:
                    return CreateUltramanTestCard(index);
            }
        }

        UcgCardData CreateUltramanTestCard(int index)
        {
            return UcgDigaTutorialDeckFactory.CreateTemplateCard(index);
        }

        UcgCardData CreateMonsterAlienTestCard(int index)
        {
            switch (index)
            {
                case 0:
                    return CreateDemoCard(index, "測試哥莫拉 Lv.5", "哥莫拉", "怪獸", 5, "");
                case 1:
                    return CreateDemoCard(index, "測試艾雷王 Lv.5", "艾雷王", "怪獸", 5, "");
                case 2:
                    return CreateDemoCard(index, "測試哥莫拉 Lv.6", "哥莫拉", "怪獸", 6, "");
                case 3:
                    return CreateDemoCard(index, "測試艾雷王 Lv.6", "艾雷王", "怪獸", 6, "");
                case 4:
                    return CreateDemoCard(index, "測試哥莫拉 Lv.7", "哥莫拉", "怪獸", 7, "");
                case 5:
                    return CreateDemoCard(index, "測試巴爾坦星人 Lv.5", "巴爾坦星人", "宇宙人", 5, "");
                default:
                    return CreateDemoCard(index, $"測試怪獸 {index + 1}", $"怪獸 {index + 1}", "怪獸", 5, "");
            }
        }

        UcgCardData CreateTeamTestCard(int index)
        {
            switch (index)
            {
                case 0:
                    return CreateDemoCard(index, "測試三人突擊隊 A Lv.1", "隊員A", "超人力霸王", 1, "三人突擊隊");
                case 1:
                    return CreateDemoCard(index, "測試三人突擊隊 B Lv.2", "隊員B", "超人力霸王", 2, "三人突擊隊");
                case 2:
                    return CreateDemoCard(index, "測試三人突擊隊 C Lv.3", "隊員C", "超人力霸王", 3, "三人突擊隊");
                case 3:
                    return CreateDemoCard(index, "測試非突擊隊 Lv.2", "其他角色", "超人力霸王", 2, "");
                case 4:
                    return CreateDemoCard(index, "測試三人突擊隊 D Lv.1", "隊員D", "超人力霸王", 1, "三人突擊隊");
                case 5:
                    return CreateDemoCard(index, "測試三人突擊隊 E Lv.3", "隊員E", "超人力霸王", 3, "三人突擊隊");
                default:
                    return CreateDemoCard(index, $"測試三人突擊隊 {index + 1}", $"隊員{index + 1}", "超人力霸王", 1, "三人突擊隊");
            }
        }

        UcgCardData CreateDemoCard(int index, string cardName, string characterName, string cardCategory, int level, string teamTag)
        {
            var card = new UcgCardData
            {
                id = $"ucg-demo-{index + 1}",
                cardName = cardName,
                characterName = characterName,
                cardCategory = cardCategory,
                level = level,
                teamTag = teamTag,
            };

            ApplyDemoBp(card);
            ApplyDemoEffect(card);
            return card;
        }

        void ApplyDemoEffect(UcgCardData card)
        {
            if (card == null) return;

            card.effectId = UcgDemoEffectId.None;
            card.effectDescription = "";

            if (card.characterName == "蓋亞" && card.level == 1)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealSelfBpPlus1000, "翻開時，本回合此 Lane 我方 BP +1000");
            }
            else if (card.characterName == "阿古茹" && card.level == 1)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealDrawOne, "翻開時，抽 1 張牌");
            }
            else if (card.characterName == "蓋亞" && card.level == 2)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000, "翻開時，選擇我方一條 Lane，BP +1000");
            }
            else if (card.characterName == "阿古茹" && card.level == 2)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000, "翻開時，選擇對手一條 Lane，BP -1000");
            }
            else if (card.characterName == "哥莫拉" && card.level == 5)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealSelfBpPlus1000, "翻開時，本回合此 Lane 我方 BP +1000");
            }
            else if (card.characterName == "哥莫拉" && card.level == 6)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000, "翻開時，選擇我方一條 Lane，BP +1000");
            }
            else if (card.characterName == "巴爾坦星人" && card.level == 5)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000, "翻開時，選擇對手一條 Lane，BP -1000");
            }
            else if (card.characterName == "隊員A" && card.level == 1)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealSelfBpPlus1000, "翻開時，本回合此 Lane 我方 BP +1000");
            }
            else if (card.characterName == "隊員B" && card.level == 2)
            {
                SetDemoEffect(card, UcgDemoEffectId.OnRevealDrawOne, "翻開時，抽 1 張牌");
            }
        }

        void SetDemoEffect(UcgCardData card, UcgDemoEffectId effectId, string description)
        {
            card.effectId = effectId;
            card.effectDescription = description;
        }

        void ApplyDemoBp(UcgCardData card)
        {
            if (card == null) return;

            if (card.cardCategory == "怪獸" || card.cardCategory == "宇宙人")
            {
                ApplyMonsterAlienBp(card);
            }
            else
            {
                ApplyUltramanBp(card);
            }
        }

        void ApplyUltramanBp(UcgCardData card)
        {
            switch (card.level)
            {
                case 2:
                    SetBp(card, 5000, 8000, 10000, 12000);
                    break;
                case 3:
                    SetBp(card, 6000, 9000, 11000, 13000);
                    break;
                default:
                    SetBp(card, 4000, 7000, 9000, 11000);
                    break;
            }
        }

        void ApplyMonsterAlienBp(UcgCardData card)
        {
            switch (card.level)
            {
                case 6:
                    SetBp(card, 7000, 10000, 12000, 14000);
                    break;
                case 7:
                    SetBp(card, 8000, 11000, 13000, 15000);
                    break;
                default:
                    SetBp(card, 6000, 9000, 11000, 13000);
                    break;
            }
        }

        void SetBp(UcgCardData card, int singleBp, int doubleBp, int tripleBp, int quadBp)
        {
            card.singleBp = singleBp;
            card.doubleBp = doubleBp;
            card.tripleBp = tripleBp;
            card.quadBp = quadBp;
        }

        UcgTestMode GetNextTestMode(UcgTestMode mode)
        {
            switch (mode)
            {
                case UcgTestMode.UltramanTest:
                    return UcgTestMode.MonsterAlienTest;
                case UcgTestMode.MonsterAlienTest:
                    return UcgTestMode.TeamTest;
                default:
                    return UcgTestMode.UltramanTest;
            }
        }

        string GetTestModeName(UcgTestMode mode)
        {
            switch (mode)
            {
                case UcgTestMode.MonsterAlienTest:
                    return "怪獸 / 宇宙人測試";
                case UcgTestMode.TeamTest:
                    return "三人突擊隊測試";
                default:
                    return "迪卡實戰教學";
            }
        }

        Sprite GetTestCardSprite(int index)
        {
            if (testCardSprites == null) return null;
            if (index < 0 || index >= DemoCardCount) return null;
            if (index >= testCardSprites.Length) return null;

            return testCardSprites[index];
        }

        Font LoadPlaceholderFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"UCG placeholder font could not be loaded: {exception.Message}");
                return null;
            }
        }

        void HandleCardSelected(UcgCardView selectedCard)
        {
            if (selectedCard == null || canvas == null) return;

            if (IsHandReturnSelectionMode())
            {
                if (!CanPlayerClickHandCard(selectedCard, out string handReturnReason))
                {
                    selectedCard.SetSelected(false);
                    LogInteractionRejected("ClickHand", handReturnReason, selectedCard.CardData);
                    if (playResultText != null)
                    {
                        playResultText.text = GetInteractionLockMessage(handReturnReason);
                    }
                    return;
                }

                selectedCard.PlayTapFeedback();
                if (sfxController != null)
                {
                    sfxController.PlayCardTap();
                }

                if (_pendingDeckSelection != null && IsSelectHandToBottomThenDrawSameCountRule(_pendingDeckSelection.rule))
                {
                    ToggleDeckOperationHandSelection(selectedCard);
                    return;
                }

                CompleteDeckOperationHandSelection(selectedCard);
                return;
            }

            if (!selectedCard.IsSelected) return;

            if (!CanPlayerClickHandCard(selectedCard, out string reason))
            {
                selectedCard.SetSelected(false);
                LogInteractionRejected("ClickHand", reason, selectedCard.CardData);
                if (playResultText != null)
                {
                    playResultText.text = GetInteractionLockMessage(reason);
                }
                return;
            }

            selectedCard.PlayTapFeedback();
            if (sfxController != null)
            {
                sfxController.PlayCardTap();
            }

            TryStartActivatedEffectFromCard(selectedCard);

            var cardViews = canvas.GetComponentsInChildren<UcgCardView>(true);
            for (int i = 0; i < cardViews.Length; i++)
            {
                UcgCardView cardView = cardViews[i];
                if (cardView == null || cardView == selectedCard) continue;
                if (cardView.IsSelected)
                {
                    cardView.SetSelected(false);
                }
            }
        }

        void TryStartActivatedEffectFromCard(UcgCardView selectedCard)
        {
            if (selectedCard == null || effectManager == null || phaseManager == null) return;
            if (!CanPlayerInteract(out string reason))
            {
                LogInteractionRejected("ClickBattlefieldCard", reason, selectedCard.CardData);
                return;
            }
            if (phaseManager.CurrentPhase != UcgGamePhase.BattleEffect || IsGameOver) return;
            if (_isEffectAutoAdvancing) return;

            PrepareActivatedEffectsForCurrentTurn();
            UcgEffectInstance effect = effectManager.FindQueuedEffectForCard(selectedCard);
            if (effect == null)
            {
                if (playResultText != null && selectedCard.CardData != null && selectedCard.CardData.effectTiming == UcgEffectTiming.Activated)
                {
                    playResultText.text = "此發動效果本回合已使用，或目前不能發動";
                }
                return;
            }

            effectManager.MoveEffectToFront(effect);
            StartFrontActivatedEffect();
        }

        public void HandleSceneCardClickedForEffect(UcgSceneCardView sceneCardView)
        {
            if (sceneCardView == null || effectManager == null || phaseManager == null) return;
            if (!CanPlayerInteract(out string reason))
            {
                LogInteractionRejected("ClickSceneCard", reason, sceneCardView.cardData);
                return;
            }
            if (phaseManager.CurrentPhase != UcgGamePhase.BattleEffect || IsGameOver) return;
            if (_isEffectAutoAdvancing) return;

            PrepareActivatedEffectsForCurrentTurn();
            UcgEffectInstance effect = effectManager.FindQueuedEffectForScene(sceneCardView);
            if (effect == null)
            {
                if (playResultText != null)
                {
                    playResultText.text = "此場景本回合沒有可發動效果";
                }
                return;
            }

            effectManager.MoveEffectToFront(effect);
            StartFrontActivatedEffect();
        }

        void StartFrontActivatedEffect()
        {
            UcgEffectInstance effect = effectManager != null ? effectManager.PeekNextEffect() : null;
            if (effect == null) return;

            if (effectManager.EffectNeedsTarget(effect))
            {
                BeginEffectTargetSelection(effect);
                return;
            }

            TryResolveNextEffect();
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(null);
        }
    }
}
