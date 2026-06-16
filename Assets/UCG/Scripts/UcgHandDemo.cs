using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgHandDemo : MonoBehaviour
    {
        const int DemoCardCount = 6;

        [Header("Optional Scene References")]
        public Canvas canvas;
        public RectTransform cardHolder;
        public RectTransform playerPlayArea;

        [Header("Optional Demo Sprites")]
        public Sprite[] testCardSprites = new Sprite[DemoCardCount];

        [Header("Layout")]
        public Vector2 cardSize = new Vector2(190f, 276f);
        public float holderHeight = 430f;
        public float bottomSafePadding = 64f;
        public float horizontalSafePadding = 48f;
        public float minimumHolderWidth = 984f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapBattleDemo()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "BattleDemo") return;
            if (FindObjectOfType<UcgHandDemo>() != null) return;

            var demoObject = new GameObject("UCGHandDemo");
            demoObject.AddComponent<UcgHandDemo>();
        }

        void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsurePlayerPlayArea();
            EnsureCardHolder();
            BuildDemoHand();
        }

        void EnsureCanvas()
        {
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
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

        void EnsurePlayerPlayArea()
        {
            if (playerPlayArea == null)
            {
                Transform existingPlayArea = canvas.transform.Find("Player Play Area");
                if (existingPlayArea != null)
                {
                    playerPlayArea = existingPlayArea as RectTransform;
                }
            }

            if (playerPlayArea == null)
            {
                var playAreaObject = new GameObject("Player Play Area", typeof(RectTransform), typeof(Image));
                playAreaObject.transform.SetParent(canvas.transform, false);
                playerPlayArea = playAreaObject.GetComponent<RectTransform>();
            }

            playerPlayArea.anchorMin = new Vector2(0.5f, 0f);
            playerPlayArea.anchorMax = new Vector2(0.5f, 0f);
            playerPlayArea.pivot = new Vector2(0.5f, 0.5f);
            playerPlayArea.anchoredPosition = new Vector2(0f, 820f);
            playerPlayArea.sizeDelta = new Vector2(340f, 430f);

            var image = playerPlayArea.GetComponent<Image>();
            if (image == null) image = playerPlayArea.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = new Color(0.08f, 0.16f, 0.22f, 0.35f);

            var playArea = playerPlayArea.GetComponent<UcgPlayArea>();
            if (playArea == null) playArea = playerPlayArea.gameObject.AddComponent<UcgPlayArea>();
            playArea.cardSlot = playerPlayArea;
            playArea.highlightImage = image;
            playArea.placedCardSize = cardSize;

            EnsurePlayAreaLabel(playerPlayArea);
        }

        void EnsurePlayAreaLabel(RectTransform parent)
        {
            const string labelName = "PlayAreaHint";
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

            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -12f);
            labelRect.sizeDelta = new Vector2(-24f, 42f);

            label.text = "拖到這裡出牌";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 1f, 1f, 0.82f);
            Font placeholderFont = LoadPlaceholderFont();
            if (placeholderFont != null)
            {
                label.font = placeholderFont;
            }
            label.fontSize = 24;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 24;
            label.raycastTarget = false;
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
            cardHolder.anchoredPosition = new Vector2(0f, bottomSafePadding + holderHeight * 0.5f);
            cardHolder.offsetMin = new Vector2(horizontalSafePadding, bottomSafePadding);
            cardHolder.offsetMax = new Vector2(-horizontalSafePadding, bottomSafePadding + holderHeight);
            cardHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(cardHolder.rect.width, minimumHolderWidth));
            cardHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, holderHeight);

            var layout = cardHolder.GetComponent<UIHandLayout>();
            if (layout == null) layout = cardHolder.gameObject.AddComponent<UIHandLayout>();

            layout.radius = 500f;
            layout.totalAngle = 46f;
            layout.rotateWithArc = true;
            layout.invertRotation = true;
            layout.invertY = false;
            layout.useSiblingOrder = true;
            layout.perItemExtraAngle = 0f;
            layout.adaptiveSpread = true;
            layout.cardsForFullSpread = DemoCardCount;
            layout.minAngle = 10f;
            layout.useBottomBaseline = true;
            layout.baselinePadding = 128f;
            layout.smooth = true;
            layout.smoothSpeed = 12f;
        }

        void BuildDemoHand()
        {
            for (int i = cardHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = cardHolder.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            for (int i = 0; i < DemoCardCount; i++)
            {
                CreateCard(i);
            }

            UIHandLayout layout = cardHolder.GetComponent<UIHandLayout>();
            if (layout != null)
            {
                layout.NotifyLayoutChanged(true);
            }
        }

        void CreateCard(int index)
        {
            string cardName = $"Test Card {index + 1}";
            var cardObject = new GameObject($"UCG Card {index + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(cardHolder, false);

            var rectTransform = cardObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = cardSize;

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

            Sprite sprite = GetTestCardSprite(index);
            var data = new UcgCardData
            {
                id = $"ucg-demo-{index + 1}",
                cardName = cardName,
                level = index + 1,
                cardCategory = index % 2 == 0 ? "Unit" : "Skill",
                cardImage = sprite
            };

            var view = cardObject.AddComponent<UcgCardView>();
            view.cardImage = image;
            view.placeholderText = label;
            view.selectedSizeMultiplier = 1.23f;
            view.Initialize(data);
            view.OnCardSelected += HandleCardSelected;

            var hover = cardObject.AddComponent<UIHandCardHover>();
            hover.lift = 48f;
            hover.scale = 1.05f;
            hover.straightenOnHover = true;
            hover.bringToFrontOnHover = true;
            hover.useOverlaySorting = true;
            hover.hoverSortingOrder = 1000;

            var drag = cardObject.AddComponent<UIDragCard>();
            drag.rootCanvas = canvas;
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
            // Future UCG play-card logic can subscribe here.
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(null);
        }
    }
}
