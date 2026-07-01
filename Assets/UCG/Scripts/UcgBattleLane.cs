using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UcgBattleLane : MonoBehaviour
    {
        public int laneIndex;
        public RectTransform opponentSlot;
        public RectTransform playerSlot;
        public Text resultLabel;
        public UcgPlayArea playerPlayArea;
        public UcgCardView playerTopCard;
        public UcgCardView opponentTopCard;
        public int playerBp;
        public int opponentBp;
        public int playerTemporaryBpModifier;
        public int opponentTemporaryBpModifier;
        public int playerSceneBpModifier;
        public int opponentSceneBpModifier;
        public int playerConditionalBpModifier;
        public int opponentConditionalBpModifier;
        public readonly List<UcgBpModifierInfo> playerTemporaryBpModifiers = new List<UcgBpModifierInfo>();
        public readonly List<UcgBpModifierInfo> opponentTemporaryBpModifiers = new List<UcgBpModifierInfo>();
        public readonly List<UcgBpModifierInfo> playerSceneBpModifiers = new List<UcgBpModifierInfo>();
        public readonly List<UcgBpModifierInfo> opponentSceneBpModifiers = new List<UcgBpModifierInfo>();
        public readonly List<UcgBpModifierInfo> playerConditionalBpModifiers = new List<UcgBpModifierInfo>();
        public readonly List<UcgBpModifierInfo> opponentConditionalBpModifiers = new List<UcgBpModifierInfo>();
        public UcgLaneResultType laneResult;
        public UcgOpponentScript opponentScript;
        public UcgTestMode opponentTestMode;
        public bool playerRestedThisTurn;
        public bool opponentRestedThisTurn;
        public float restRotationAnimationSeconds = 0.28f;
        public float judgementPulseSeconds = 0.85f;

        Font _uiFont;
        UcgCardInfoPanel _cardInfoPanel;
        UcgCardData _fixedOpponentCardData;
        Sprite _fixedOpponentCardSprite;
        Vector2 _fixedOpponentCardSize;
        Vector2 _referenceOpponentSlotPosition;
        Vector2 _referencePlayerSlotPosition;
        Vector2 _referenceOpponentSlotSize;
        Vector2 _referencePlayerSlotSize;
        Vector2 _overviewCardSize;
        bool _hasReferenceSlotPositions;
        bool _useOverviewCardLayout;
        static Sprite _overviewEmptySlotSprite;
        const string OverviewEmptySlotMainFrameName = "Overview Empty Slot Main Frame";
        const string OverviewEmptySlotVisibleFrameName = "Overview Empty Slot Visible Frame";
        const bool TraceOverviewSlotRectsEnabled = true;
        Color _opponentSlotDefaultColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.110f);
        Color _effectTargetColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.62f);
        Image _laneFocusImage;
        Outline _laneFocusOutline;
        UcgGuidancePulse _laneFocusPulse;
        RectTransform _laneDuelAxisRect;
        Image _laneDuelAxisImage;
        RectTransform _laneDuelAxisGlowRect;
        Image _laneDuelAxisGlowImage;
        RectTransform _laneDuelAxisCoreRect;
        Image _laneDuelAxisCoreImage;
        Text _opponentLaneNumberLabel;
        Text _playerLaneNumberLabel;
        Coroutine _playerRestRotationRoutine;
        Coroutine _opponentRestRotationRoutine;
        Coroutine _judgementResultRoutine;

        public void Initialize(
            int index,
            Font uiFont,
            Text sharedResultText,
            UcgTutorialGuide tutorialGuide,
            UcgTurnManager turnManager,
            UcgPhaseManager phaseManager,
            Vector2 playerSlotSize,
            Vector2 opponentSlotSize,
            Vector2 placedCardSize)
        {
            laneIndex = index;
            _uiFont = uiFont;

            RectTransform laneRect = transform as RectTransform;
            laneRect.anchorMin = new Vector2(0.5f, 0.5f);
            laneRect.anchorMax = new Vector2(0.5f, 0.5f);
            laneRect.pivot = new Vector2(0.5f, 0.5f);
            laneRect.sizeDelta = new Vector2(300f, 820f);
            EnsureLaneFocusBackdrop(laneRect);
            EnsureLaneDuelAxis(laneRect);

            opponentSlot = EnsureSlot("Opponent Slot", new Vector2(0f, 310f), opponentSlotSize, _opponentSlotDefaultColor, true);
            EnsureSlotLabel(opponentSlot, "對手");

            resultLabel = EnsureResultLabel();

            playerSlot = EnsureSlot("Player Slot", new Vector2(0f, -310f), playerSlotSize, UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.120f), true);
            EnsureLaneNumberLabels();
            playerPlayArea = playerSlot.GetComponent<UcgPlayArea>();
            if (playerPlayArea == null) playerPlayArea = playerSlot.gameObject.AddComponent<UcgPlayArea>();

            playerPlayArea.cardSlot = playerSlot;
            playerPlayArea.ownerLane = this;
            playerPlayArea.highlightImage = playerSlot.GetComponent<Image>();
            playerPlayArea.resultText = sharedResultText;
            playerPlayArea.tutorialGuide = tutorialGuide;
            playerPlayArea.turnManager = turnManager;
            playerPlayArea.phaseManager = phaseManager;
            playerPlayArea.placedCardSize = placedCardSize;
            playerPlayArea.upgradeStackOffset = new Vector2(10f, 6f);
            playerPlayArea.defaultColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.070f);
            playerPlayArea.hoverColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.060f);
            playerPlayArea.occupiedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.040f);
            playerPlayArea.activeSetupColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.052f);
            playerPlayArea.upgradeAvailableColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.075f);
            playerPlayArea.validDropColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.050f);
            playerPlayArea.invalidDropColor = new Color(0.46f, 0.08f, 0.12f, 0.04f);
            ResetLaneState();
            SetActiveLaneFocus(false);
        }

        public void ConfigureFixedOpponentCard(UcgCardData cardData, Sprite cardSprite, UcgCardInfoPanel cardInfoPanel, Vector2 cardSize)
        {
            _fixedOpponentCardData = cardData;
            _fixedOpponentCardSprite = cardSprite;
            _cardInfoPanel = cardInfoPanel;
            _fixedOpponentCardSize = cardSize;
        }

        public void ApplyReferenceSlotLayout(Vector2 opponentSlotPosition, Vector2 playerSlotPosition)
        {
            _referenceOpponentSlotPosition = opponentSlotPosition;
            _referencePlayerSlotPosition = playerSlotPosition;
            _referenceOpponentSlotSize = opponentSlot != null ? opponentSlot.sizeDelta : Vector2.zero;
            _referencePlayerSlotSize = playerSlot != null ? playerSlot.sizeDelta : Vector2.zero;
            _hasReferenceSlotPositions = true;
            ApplySlotRect(opponentSlot, opponentSlotPosition, _referenceOpponentSlotSize);
            ApplySlotRect(playerSlot, playerSlotPosition, _referencePlayerSlotSize);
            ApplyLaneNumberLabelLayout();
            TraceOverviewSlotRectState("After ApplyReferenceSlotLayout");
        }

        public void ApplyOverviewSlotLayout(Vector2 opponentSlotPosition, Vector2 playerSlotPosition, Vector2 overviewCardSize)
        {
            _overviewCardSize = overviewCardSize;
            _useOverviewCardLayout = overviewCardSize.x > 0f && overviewCardSize.y > 0f;
            ApplySlotRect(opponentSlot, opponentSlotPosition, overviewCardSize);
            ApplySlotRect(playerSlot, playerSlotPosition, overviewCardSize);
            ApplyOverviewSlotVisualSize(opponentSlot, overviewCardSize, false);
            ApplyOverviewSlotVisualSize(playerSlot, overviewCardSize, true);
            ApplyPlacedCardSize(opponentSlot, overviewCardSize);
            ApplyPlacedCardSize(playerSlot, overviewCardSize);
            if (playerPlayArea != null)
            {
                playerPlayArea.cardSlot = playerSlot;
                playerPlayArea.placedCardSize = overviewCardSize;
                playerPlayArea.centerPlacedCardsInSlot = true;
            }
            ApplyLaneNumberLabelLayout();
            TraceOverviewSlotRectState("After ApplyOverviewSlotLayout");
        }

        public void RestoreReferenceSlotLayout()
        {
            if (!_hasReferenceSlotPositions) return;

            ApplySlotRect(opponentSlot, _referenceOpponentSlotPosition, _referenceOpponentSlotSize);
            ApplySlotRect(playerSlot, _referencePlayerSlotPosition, _referencePlayerSlotSize);
            _useOverviewCardLayout = false;
            if (playerPlayArea != null)
            {
                playerPlayArea.centerPlacedCardsInSlot = false;
            }
            ApplyLaneNumberLabelLayout();
        }

        static void ApplySlotRect(RectTransform slot, Vector2 anchoredPosition, Vector2 size)
        {
            if (slot == null) return;

            ApplyOverviewCardSize(slot, size);
            slot.anchoredPosition = anchoredPosition;
            slot.localEulerAngles = Vector3.zero;
        }

        static void ApplyOverviewCardSize(RectTransform rect, Vector2 size)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (size.x > 0f && size.y > 0f)
            {
                rect.sizeDelta = size;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
            rect.localScale = Vector3.one;
        }

        static void ApplyPlacedCardSize(RectTransform slot, Vector2 size)
        {
            if (slot == null || size.x <= 0f || size.y <= 0f) return;

            UcgCardView[] cardViews = slot.GetComponentsInChildren<UcgCardView>(false);
            for (int i = 0; i < cardViews.Length; i++)
            {
                RectTransform cardRect = cardViews[i] != null ? cardViews[i].transform as RectTransform : null;
                if (cardRect == null) continue;

                ApplyOverviewCardSize(cardRect, size);
                cardRect.anchoredPosition = Vector2.zero;
            }
        }

        void ApplyOverviewSlotVisualSize(RectTransform slot, Vector2 size, bool isPlayerSlot)
        {
            if (slot == null || size.x <= 0f || size.y <= 0f) return;

            ApplyOverviewCardSize(slot, size);
            EnsureSlotSurfaceDetails(slot, isPlayerSlot);

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.enabled = true;
                slotImage.color = UcgToolUiPalette.WithAlpha(
                    UcgToolUiPalette.DeepGlass,
                    isPlayerSlot ? 0.135f : 0.120f);
            }

            Image interiorImage = GetSlotChildImage(slot, "Slot Interior Shade");
            if (interiorImage != null)
            {
                interiorImage.enabled = true;
                interiorImage.color = UcgToolUiPalette.WithAlpha(
                    UcgToolUiPalette.DeepGlass,
                    isPlayerSlot ? 0.105f : 0.105f);
            }

            bool isEmpty = CountCardsInSlot(slot) == 0;
            EnsureOverviewEmptySlotMainFrame(slot, size, isPlayerSlot, isEmpty);
            EnsureOverviewEmptySlotVisibleFrame(slot, size, isPlayerSlot, isEmpty);

            for (int i = 0; i < slot.childCount; i++)
            {
                RectTransform child = slot.GetChild(i) as RectTransform;
                if (child == null) continue;

                child.localScale = Vector3.one;
                child.localEulerAngles = Vector3.zero;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(slot);
            TraceOverviewSlotRectState($"After ApplyOverviewSlotVisualSize:{slot.name}");
        }

        static Image GetSlotChildImage(RectTransform slot, string childName)
        {
            if (slot == null || string.IsNullOrEmpty(childName)) return null;

            Transform child = slot.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        void TraceOverviewSlotRectState(string stage)
        {
            if (!TraceOverviewSlotRectsEnabled || !_useOverviewCardLayout) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[UCG SlotTrace] lane={GetLaneDisplayNumber()} stage={stage}\n" +
                FormatSlotRectState("Opponent", opponentSlot) + "\n" +
                FormatSlotRectState("Player", playerSlot));
            if (opponentSlot != null && CountCardsInSlot(opponentSlot) == 0)
            {
                Debug.Log(FormatDetailedEmptySlotTrace(stage));
            }
#endif
        }

        string FormatSlotRectState(string side, RectTransform slot)
        {
            if (slot == null) return $"{side}: <null>";

            RectTransform mainFrame = slot.Find(OverviewEmptySlotMainFrameName) as RectTransform;
            RectTransform frame = slot.Find(OverviewEmptySlotVisibleFrameName) as RectTransform;
            RectTransform interior = slot.Find("Slot Interior Shade") as RectTransform;
            RectTransform shadow = slot.Find("Slot Card Ground Shadow") as RectTransform;
            RectTransform parent = slot.parent as RectTransform;
            UcgPlayArea playArea = slot.GetComponent<UcgPlayArea>();
            bool hasPlacedCard = CountCardsInSlot(slot) > 0;
            bool hasMask = SlotOrParentHasMask(slot);

            return
                $"{side}: empty={!hasPlacedCard}, placed={hasPlacedCard}, root={FormatRect(slot)}, " +
                $"mainFrame={FormatRect(mainFrame)}, frame={FormatRect(frame)}, interior={FormatRect(interior)}, shadow={FormatRect(shadow)}, " +
                $"hitArea={(playArea != null ? FormatRect(playArea.cardSlot != null ? playArea.cardSlot : slot) : "<none>")}, " +
                $"parent={(parent != null ? parent.name : "<none>")} {FormatRect(parent)}, " +
                $"maskOrRectMask={hasMask}";
        }

        static string FormatRect(RectTransform rect)
        {
            if (rect == null) return "<null>";

            Vector2 size = rect.sizeDelta;
            Vector3 scale = rect.localScale;
            float ratio = size.y > 0.001f ? size.x / size.y : 0f;
            return
                $"{rect.name}[size=({size.x:0.##},{size.y:0.##}), " +
                $"scale=({scale.x:0.###},{scale.y:0.###},{scale.z:0.###}), " +
                $"anchored=({rect.anchoredPosition.x:0.##},{rect.anchoredPosition.y:0.##}), " +
                $"ratio={ratio:0.###}, active={rect.gameObject.activeSelf}]";
        }

        static bool SlotOrParentHasMask(RectTransform slot)
        {
            if (slot == null) return false;

            Transform current = slot;
            while (current != null)
            {
                if (current.GetComponent<Mask>() != null || current.GetComponent<RectMask2D>() != null)
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        string FormatDetailedEmptySlotTrace(string stage)
        {
            var builder = new StringBuilder(4096);
            RectTransform opponentFrame = opponentSlot != null
                ? opponentSlot.Find(OverviewEmptySlotVisibleFrameName) as RectTransform
                : null;
            RectTransform opponentParent = opponentSlot != null ? opponentSlot.parent as RectTransform : null;
            RectTransform maskRect = FindNearestMaskRect(opponentSlot);
            bool frameInsideLaneParent = RectContainsWorldCorners(opponentParent, opponentFrame);
            bool frameInsideMask = RectContainsWorldCorners(maskRect, opponentFrame);

            builder.AppendLine($"[UCG SlotTraceDetail] lane={GetLaneDisplayNumber()} stage={stage}");
            builder.AppendLine($"Opponent root corners={FormatWorldCorners(opponentSlot)}");
            builder.AppendLine($"Opponent frame corners={FormatWorldCorners(opponentFrame)}");
            builder.AppendLine($"Lane parent={(opponentParent != null ? opponentParent.name : "<none>")} corners={FormatWorldCorners(opponentParent)}");
            builder.AppendLine($"Nearest mask={(maskRect != null ? maskRect.name : "<none>")} corners={FormatWorldCorners(maskRect)}");
            builder.AppendLine($"frameInsideLaneParent={frameInsideLaneParent}");
            builder.AppendLine($"frameInsideMask={frameInsideMask}");
            builder.AppendLine($"parentTransform={FormatTransformDetails(opponentParent)}");
            builder.AppendLine($"rootTransform={FormatTransformDetails(opponentSlot)}");
            builder.AppendLine($"frameTransform={FormatTransformDetails(opponentFrame)}");
            builder.AppendLine(FormatSlotParentBounds("Opponent", opponentSlot));
            builder.AppendLine(FormatSlotParentBounds("Player", playerSlot));
            builder.AppendLine("Opponent active image children:");
            AppendSlotGraphicChildren(builder, "Opponent", opponentSlot, maskRect);
            builder.AppendLine("Player active image children:");
            AppendSlotGraphicChildren(builder, "Player", playerSlot, FindNearestMaskRect(playerSlot));
            return builder.ToString();
        }

        static RectTransform FindNearestMaskRect(RectTransform slot)
        {
            if (slot == null) return null;

            Transform current = slot;
            while (current != null)
            {
                if (current.GetComponent<Mask>() != null || current.GetComponent<RectMask2D>() != null)
                {
                    return current as RectTransform;
                }
                current = current.parent;
            }

            return null;
        }

        static bool RectContainsWorldCorners(RectTransform container, RectTransform target)
        {
            if (container == null || target == null) return false;

            Rect rect = container.rect;
            Vector3[] corners = GetWorldCorners(target);
            const float epsilon = 0.01f;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = container.InverseTransformPoint(corners[i]);
                if (local.x < rect.xMin - epsilon || local.x > rect.xMax + epsilon ||
                    local.y < rect.yMin - epsilon || local.y > rect.yMax + epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        static Vector3[] GetWorldCorners(RectTransform rect)
        {
            var corners = new Vector3[4];
            if (rect != null) rect.GetWorldCorners(corners);
            return corners;
        }

        static string FormatWorldCorners(RectTransform rect)
        {
            if (rect == null) return "<null>";

            Vector3[] corners = GetWorldCorners(rect);
            return
                $"BL=({corners[0].x:0.##},{corners[0].y:0.##}) " +
                $"TL=({corners[1].x:0.##},{corners[1].y:0.##}) " +
                $"TR=({corners[2].x:0.##},{corners[2].y:0.##}) " +
                $"BR=({corners[3].x:0.##},{corners[3].y:0.##})";
        }

        static string FormatTransformDetails(RectTransform rect)
        {
            if (rect == null) return "<null>";

            return
                $"{rect.name}[anchorMin=({rect.anchorMin.x:0.###},{rect.anchorMin.y:0.###}), " +
                $"anchorMax=({rect.anchorMax.x:0.###},{rect.anchorMax.y:0.###}), " +
                $"pivot=({rect.pivot.x:0.###},{rect.pivot.y:0.###}), " +
                $"size=({rect.sizeDelta.x:0.##},{rect.sizeDelta.y:0.##}), " +
                $"anchored=({rect.anchoredPosition.x:0.##},{rect.anchoredPosition.y:0.##}), " +
                $"local=({rect.localPosition.x:0.##},{rect.localPosition.y:0.##},{rect.localPosition.z:0.##}), " +
                $"world=({rect.position.x:0.##},{rect.position.y:0.##},{rect.position.z:0.##}), " +
                $"scale=({rect.localScale.x:0.###},{rect.localScale.y:0.###},{rect.localScale.z:0.###})]";
        }

        static string FormatSlotParentBounds(string side, RectTransform slot)
        {
            RectTransform parent = slot != null ? slot.parent as RectTransform : null;
            if (slot == null || parent == null) return $"{side}InsideParent=<unknown>";

            float centerY = slot.anchoredPosition.y;
            float halfHeight = slot.sizeDelta.y * 0.5f;
            float slotYMin = centerY - halfHeight;
            float slotYMax = centerY + halfHeight;
            Rect parentRect = parent.rect;
            bool insideParent = slotYMin >= parentRect.yMin - 0.01f && slotYMax <= parentRect.yMax + 0.01f;

            return
                $"{side} parentRectY=({parentRect.yMin:0.##},{parentRect.yMax:0.##}) " +
                $"centerY={centerY:0.##} cardYMin={slotYMin:0.##} cardYMax={slotYMax:0.##} " +
                $"{side.ToLowerInvariant()}InsideParent={insideParent}";
        }

        void AppendSlotGraphicChildren(StringBuilder builder, string side, RectTransform slot, RectTransform maskRect)
        {
            if (builder == null) return;
            if (slot == null)
            {
                builder.AppendLine($"{side}: <slot null>");
                return;
            }

            Graphic[] graphics = slot.GetComponentsInChildren<Graphic>(false);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null) continue;
                Image image = graphic as Image;
                RawImage rawImage = graphic as RawImage;
                if (image == null && rawImage == null) continue;

                RectTransform rect = graphic.transform as RectTransform;
                if (rect == null) continue;

                Color color = graphic.color;
                Sprite sprite = image != null ? image.sprite : null;
                Texture texture = rawImage != null ? rawImage.texture : null;
                bool insideMask = maskRect == null || RectContainsWorldCorners(maskRect, rect);
                bool horizontalStripLikely = IsHorizontalStripCandidate(slot, rect);
                CanvasGroup canvasGroup = graphic.GetComponentInParent<CanvasGroup>();
                float canvasAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
                string assetName = image != null
                    ? (sprite != null ? sprite.name : "<null>")
                    : (texture != null ? texture.name : "<null>");

                builder.AppendLine(
                    $"{side}/{GetRelativePath(slot, rect)} " +
                    $"activeSelf={graphic.gameObject.activeSelf} activeInHierarchy={graphic.gameObject.activeInHierarchy} " +
                    $"siblingIndex={rect.GetSiblingIndex()} size=({rect.sizeDelta.x:0.##},{rect.sizeDelta.y:0.##}) " +
                    $"scale=({rect.localScale.x:0.###},{rect.localScale.y:0.###},{rect.localScale.z:0.###}) " +
                    $"anchored=({rect.anchoredPosition.x:0.##},{rect.anchoredPosition.y:0.##}) " +
                    $"corners={FormatWorldCorners(rect)} " +
                    $"graphicType={(image != null ? "Image" : "RawImage")} enabled={graphic.enabled} alpha={color.a:0.###} " +
                    $"asset={assetName} canvasGroupAlpha={canvasAlpha:0.###} insideMask={insideMask} " +
                    $"horizontalStripLikely={horizontalStripLikely}");
            }
        }

        static bool IsHorizontalStripCandidate(RectTransform slot, RectTransform rect)
        {
            if (slot == null || rect == null) return false;

            float slotHeight = Mathf.Max(0.001f, slot.sizeDelta.y);
            float rectHeight = Mathf.Abs(rect.rect.height);
            float rectWidth = Mathf.Abs(rect.rect.width);
            return rectHeight < slotHeight * 0.35f || rectWidth / Mathf.Max(0.001f, rectHeight) > 1.2f;
        }

        static string GetRelativePath(RectTransform root, RectTransform child)
        {
            if (root == null || child == null) return "<null>";
            if (root == child) return child.name;

            var names = new List<string>();
            Transform current = child;
            while (current != null)
            {
                names.Add(current.name);
                if (current == root) break;
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        void EnsureOverviewEmptySlotMainFrame(RectTransform slot, Vector2 size, bool isPlayerSlot, bool visible)
        {
            if (slot == null || size.x <= 0f || size.y <= 0f) return;

            Transform existingFrame = slot.Find(OverviewEmptySlotMainFrameName);
            RectTransform frameRect;
            Image frameImage;
            Outline frameOutline;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(OverviewEmptySlotMainFrameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(slot, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
                frameOutline = frameObject.GetComponent<Outline>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                frameOutline = existingFrame.GetComponent<Outline>();
                if (frameOutline == null) frameOutline = existingFrame.gameObject.AddComponent<Outline>();
            }

            ApplyOverviewCardSize(frameRect, size);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.localEulerAngles = Vector3.zero;
            frameRect.gameObject.SetActive(visible);

            frameImage.sprite = GetOverviewEmptySlotSprite();
            frameImage.type = Image.Type.Simple;
            frameImage.enabled = true;
            frameImage.raycastTarget = false;
            frameImage.color = UcgToolUiPalette.WithAlpha(
                UcgToolUiPalette.DeepGlass,
                isPlayerSlot ? 0.070f : 0.145f);

            frameOutline.enabled = true;
            frameOutline.useGraphicAlpha = true;
            frameOutline.effectColor = UcgToolUiPalette.WithAlpha(
                isPlayerSlot ? UcgToolUiPalette.FocusCyan : UcgToolUiPalette.GlassBorder,
                isPlayerSlot ? 0.22f : 0.30f);
            frameOutline.effectDistance = new Vector2(1.25f, -1.25f);
            MoveEmptySlotFrameAboveDecorBelowCards(slot, frameRect);
        }

        void EnsureOverviewEmptySlotVisibleFrame(RectTransform slot, Vector2 size, bool isPlayerSlot, bool visible)
        {
            if (slot == null || size.x <= 0f || size.y <= 0f) return;

            Transform existingFrame = slot.Find(OverviewEmptySlotVisibleFrameName);
            RectTransform frameRect;
            Image frameImage;
            Outline frameOutline;

            if (existingFrame == null)
            {
                var frameObject = new GameObject(OverviewEmptySlotVisibleFrameName, typeof(RectTransform), typeof(Image), typeof(Outline));
                frameObject.transform.SetParent(slot, false);
                frameRect = frameObject.GetComponent<RectTransform>();
                frameImage = frameObject.GetComponent<Image>();
                frameOutline = frameObject.GetComponent<Outline>();
            }
            else
            {
                frameRect = existingFrame as RectTransform;
                frameImage = existingFrame.GetComponent<Image>();
                if (frameImage == null) frameImage = existingFrame.gameObject.AddComponent<Image>();
                frameOutline = existingFrame.GetComponent<Outline>();
                if (frameOutline == null) frameOutline = existingFrame.gameObject.AddComponent<Outline>();
            }

            ApplyOverviewCardSize(frameRect, size);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.localEulerAngles = Vector3.zero;
            frameRect.gameObject.SetActive(visible);
            MoveEmptySlotFrameAboveDecorBelowCards(slot, frameRect);

            frameImage.sprite = GetOverviewEmptySlotSprite();
            frameImage.type = Image.Type.Simple;
            frameImage.enabled = true;
            frameImage.raycastTarget = false;
            frameImage.color = UcgToolUiPalette.WithAlpha(
                UcgToolUiPalette.DeepGlass,
                isPlayerSlot ? 0.015f : 0.055f);

            frameOutline.enabled = true;
            frameOutline.useGraphicAlpha = true;
            frameOutline.effectColor = UcgToolUiPalette.WithAlpha(
                isPlayerSlot ? UcgToolUiPalette.FocusCyan : UcgToolUiPalette.GlassBorder,
                isPlayerSlot ? 0.05f : 0.22f);
            frameOutline.effectDistance = new Vector2(1.1f, -1.1f);
        }

        static void MoveEmptySlotFrameAboveDecorBelowCards(RectTransform slot, RectTransform frameRect)
        {
            if (slot == null || frameRect == null) return;

            int firstCardIndex = -1;
            for (int i = 0; i < slot.childCount; i++)
            {
                Transform child = slot.GetChild(i);
                if (child == null || child == frameRect) continue;
                if (child.GetComponent<UcgCardView>() == null) continue;

                firstCardIndex = i;
                break;
            }

            if (firstCardIndex >= 0)
            {
                frameRect.SetSiblingIndex(Mathf.Max(0, firstCardIndex));
            }
            else
            {
                frameRect.SetAsLastSibling();
            }
        }

        static Sprite GetOverviewEmptySlotSprite()
        {
            if (_overviewEmptySlotSprite != null) return _overviewEmptySlotSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            _overviewEmptySlotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _overviewEmptySlotSprite.name = "UCG Overview Empty Slot Sprite";
            _overviewEmptySlotSprite.hideFlags = HideFlags.HideAndDontSave;
            return _overviewEmptySlotSprite;
        }

        public void ConfigureOpponentScript(UcgOpponentScript script, UcgTestMode mode)
        {
            opponentScript = script;
            opponentTestMode = mode;
        }

        public bool PlaceFixedOpponentCardIfEmpty()
        {
            if (opponentTopCard != null) return false;
            if (_fixedOpponentCardData == null) return false;

            SetupFixedOpponentCard(_fixedOpponentCardData, _fixedOpponentCardSprite, _cardInfoPanel, _fixedOpponentCardSize);
            return opponentTopCard != null;
        }

        public bool PlaceScriptedOpponentCardIfEmpty(int turnNumber)
        {
            if (opponentTopCard != null) return false;
            if (opponentScript == null) return false;

            UcgCardData cardData = opponentScript.GetOpponentSetupCard(opponentTestMode, turnNumber, laneIndex);
            return PlaceOpponentCard(cardData, true);
        }

        public bool TryScriptedOpponentUpgrade(int turnNumber)
        {
            UcgCardView top = GetOpponentTopCard();
            if (top == null || top.CardData == null) return false;
            if (opponentScript == null) return false;

            UcgCardData upgradeData = opponentScript.GetOpponentUpgradeCard(opponentTestMode, turnNumber, laneIndex, top.CardData);
            if (upgradeData == null) return false;

            if (!UcgActionValidator.CanPlayOrUpgrade(upgradeData, top.CardData, out _, out UcgPlayActionType actionType))
            {
                return false;
            }

            if (actionType != UcgPlayActionType.Upgrade) return false;
            return UpgradeOpponentCard(upgradeData, true);
        }

        public void SetupFixedOpponentCard(UcgCardData cardData, Sprite cardSprite, UcgCardInfoPanel cardInfoPanel, Vector2 cardSize)
        {
            _cardInfoPanel = cardInfoPanel;
            _fixedOpponentCardSprite = cardSprite;
            _fixedOpponentCardSize = cardSize;
            PlaceOpponentCard(cardData, true);
        }

        public bool PlaceOpponentCard(UcgCardData cardData, bool faceDown)
        {
            if (UcgTutorialCardEffectMap.ForbidsSingleState(cardData, out _))
            {
                return false;
            }

            ClearOpponentCards();

            if (cardData == null || opponentSlot == null)
            {
                opponentTopCard = null;
                return false;
            }

            UcgCardView view = CreateOpponentCardView(cardData, faceDown, 0);
            opponentTopCard = view;
            opponentBp = 0;
            return opponentTopCard != null;
        }

        public bool UpgradeOpponentCard(UcgCardData cardData, bool faceDown)
        {
            if (cardData == null || opponentSlot == null || opponentTopCard == null) return false;

            int existingCardCount = GetOpponentStackCount();
            UcgCardView view = CreateOpponentCardView(cardData, faceDown, existingCardCount);
            opponentTopCard = view;
            opponentBp = 0;
            return opponentTopCard != null;
        }

        UcgCardView CreateOpponentCardView(UcgCardData cardData, bool faceDown, int stackIndex)
        {
            if (!cardData.IsExternalCard())
            {
                cardData.cardImage = _fixedOpponentCardSprite;
            }
            var cardObject = new GameObject("Opponent Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(opponentSlot, false);

            var cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = _useOverviewCardLayout
                ? Vector2.zero
                : new Vector2(0f, 18f) + new Vector2(8f, 8f) * stackIndex;
            cardRect.sizeDelta = _useOverviewCardLayout && _overviewCardSize.x > 0f && _overviewCardSize.y > 0f
                ? _overviewCardSize
                : _fixedOpponentCardSize;
            cardRect.localScale = Vector3.one;
            cardRect.localEulerAngles = Vector3.zero;

            var image = cardObject.GetComponent<Image>();
            image.raycastTarget = true;

            var label = CreateOpponentPlaceholderText(cardRect);

            var view = cardObject.AddComponent<UcgCardView>();
            view.cardImage = image;
            view.placeholderText = label;
            view.selectedSizeMultiplier = 1.12f;
            view.SetInfoPanel(_cardInfoPanel);
            view.Initialize(cardData);
            view.SetFaceDown(faceDown);
            view.SetBattlefieldLocked(true);
            ConfigureOpponentCardClickTarget(view);

            ApplyOpponentCardSorting();
            ApplySlotFocusState(opponentSlot, false, true);
            return view;
        }

        void ConfigureOpponentCardClickTarget(UcgCardView view)
        {
            if (view == null) return;

            var clickTarget = view.GetComponent<UcgLaneClickTarget>();
            if (clickTarget == null) clickTarget = view.gameObject.AddComponent<UcgLaneClickTarget>();

            clickTarget.demo = FindFirstObjectByType<UcgHandDemo>();
            clickTarget.ownerLane = this;
            clickTarget.targetSide = UcgPlayerSide.Opponent;
        }

        public void ResetLane()
        {
            if (playerPlayArea != null)
            {
                playerPlayArea.ResetArea();
            }

            if (resultLabel != null)
            {
                resultLabel.text = GetLaneDisplayNumber();
                resultLabel.color = new Color(1f, 1f, 1f, 0.92f);
            }

            ClearOpponentCards();
            ResetLaneState();
        }

        public UcgPlayArea GetPlayerPlayArea()
        {
            return playerPlayArea;
        }

        public bool SwapCharacterStackWith(UcgBattleLane otherLane, UcgPlayerSide side)
        {
            if (otherLane == null || otherLane == this) return false;

            RectTransform thisSlot = GetSlotForSide(side);
            RectTransform otherSlot = otherLane.GetSlotForSide(side);
            if (thisSlot == null || otherSlot == null) return false;

            List<RectTransform> thisCards = GetCardRectsInSlot(thisSlot);
            List<RectTransform> otherCards = GetCardRectsInSlot(otherSlot);
            if (thisCards.Count == 0 || otherCards.Count == 0) return false;

            MoveCardsToSlot(thisCards, otherSlot);
            MoveCardsToSlot(otherCards, thisSlot);
            SwapRuntimeStateWith(otherLane, side);

            RefreshStackAfterMove(side);
            otherLane.RefreshStackAfterMove(side);
            return true;
        }

        public bool ReplaceTopCardData(UcgPlayerSide side, UcgCardData replacementCard, out UcgCardData returnedTopCard)
        {
            returnedTopCard = null;
            if (replacementCard == null) return false;

            UcgCardView topCardView = side == UcgPlayerSide.Player
                ? playerPlayArea != null ? playerPlayArea.GetTopCard() : playerTopCard
                : GetOpponentTopCard();
            if (topCardView == null || topCardView.CardData == null) return false;

            returnedTopCard = topCardView.CardData;
            topCardView.Initialize(replacementCard);
            topCardView.SetFaceDown(false);
            topCardView.SetBattlefieldLocked(true);
            topCardView.EnterEffectResolved = true;

            if (side == UcgPlayerSide.Player)
            {
                if (playerPlayArea != null)
                {
                    playerPlayArea.RefreshPlacedCardsAfterStackMove();
                    playerTopCard = playerPlayArea.GetTopCard();
                }
                RefreshPlayerStateFromPlayArea();
                return true;
            }

            ConfigureOpponentCardClickTarget(topCardView);
            RefreshOpponentTopCard();
            RefreshOpponentCardsAfterStackMove();
            opponentBp = 0;
            return true;
        }

        public UcgCardView UpgradeCardFromEffect(
            UcgPlayerSide side,
            UcgCardData cardData,
            UcgCardInfoPanel cardInfoPanel,
            Sprite fallbackSprite,
            Vector2 cardSize,
            Font placeholderFont)
        {
            if (cardData == null) return null;

            if (side == UcgPlayerSide.Player)
            {
                if (playerPlayArea == null) return null;
                playerPlayArea.placedCardSize = cardSize;
                UcgCardView view = playerPlayArea.UpgradeFromEffect(cardData, cardInfoPanel, fallbackSprite, placeholderFont);
                if (view == null) return null;

                playerTopCard = playerPlayArea.GetTopCard();
                RefreshPlayerStateFromPlayArea();
                return view;
            }

            if (opponentSlot == null || opponentTopCard == null) return null;
            _cardInfoPanel = cardInfoPanel;
            _fixedOpponentCardSprite = fallbackSprite;
            _fixedOpponentCardSize = cardSize;
            if (!UpgradeOpponentCard(cardData, false)) return null;

            RefreshOpponentTopCard();
            RefreshOpponentCardsAfterStackMove();
            return opponentTopCard;
        }

        public bool RemoveTopCardFromEffect(UcgPlayerSide side, UcgCardData expectedCard, out UcgCardData removedCard)
        {
            removedCard = null;
            if (side == UcgPlayerSide.Player)
            {
                if (playerPlayArea == null) return false;
                bool removed = playerPlayArea.RemoveTopCardFromEffect(expectedCard, out removedCard);
                if (!removed) return false;

                playerTopCard = playerPlayArea.GetTopCard();
                RefreshPlayerStateFromPlayArea();
                return true;
            }

            UcgCardView topView = GetOpponentTopCard();
            if (topView == null || topView.CardData == null) return false;
            if (expectedCard != null && !ReferenceEquals(topView.CardData, expectedCard)) return false;

            removedCard = topView.CardData;
            if (Application.isPlaying)
            {
                topView.transform.SetParent(null, false);
                Destroy(topView.gameObject);
            }
            else
            {
                DestroyImmediate(topView.gameObject);
            }

            RefreshOpponentTopCard();
            RefreshOpponentCardsAfterStackMove();
            opponentBp = 0;
            return true;
        }

        RectTransform GetSlotForSide(UcgPlayerSide side)
        {
            return side == UcgPlayerSide.Player ? playerSlot : opponentSlot;
        }

        static List<RectTransform> GetCardRectsInSlot(RectTransform slot)
        {
            var result = new List<RectTransform>();
            if (slot == null) return result;

            for (int i = 0; i < slot.childCount; i++)
            {
                var cardView = slot.GetChild(i).GetComponent<UcgCardView>();
                var rect = slot.GetChild(i) as RectTransform;
                if (cardView != null && rect != null) result.Add(rect);
            }

            return result;
        }

        static void MoveCardsToSlot(List<RectTransform> cards, RectTransform targetSlot)
        {
            if (cards == null || targetSlot == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform cardRect = cards[i];
                if (cardRect == null) continue;

                cardRect.SetParent(targetSlot, false);
                cardRect.SetAsLastSibling();
            }
        }

        void RefreshStackAfterMove(UcgPlayerSide side)
        {
            if (side == UcgPlayerSide.Player)
            {
                if (playerPlayArea != null)
                {
                    playerPlayArea.RefreshPlacedCardsAfterStackMove();
                    playerTopCard = playerPlayArea.GetTopCard();
                }
                RefreshPlayerStateFromPlayArea();
                return;
            }

            RefreshOpponentTopCard();
            RefreshOpponentCardsAfterStackMove();
            opponentBp = 0;
        }

        void RefreshOpponentCardsAfterStackMove()
        {
            if (opponentSlot == null) return;

            for (int i = 0; i < opponentSlot.childCount; i++)
            {
                var card = opponentSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card == null) continue;

                card.SetBattlefieldLocked(true);
                ConfigureOpponentCardClickTarget(card);
            }

            ApplyOpponentCardSorting();
        }

        void SwapRuntimeStateWith(UcgBattleLane otherLane, UcgPlayerSide side)
        {
            if (side == UcgPlayerSide.Player)
            {
                Swap(ref playerTemporaryBpModifier, ref otherLane.playerTemporaryBpModifier);
                SwapListContents(playerTemporaryBpModifiers, otherLane.playerTemporaryBpModifiers);
                Swap(ref playerRestedThisTurn, ref otherLane.playerRestedThisTurn);
                return;
            }

            Swap(ref opponentTemporaryBpModifier, ref otherLane.opponentTemporaryBpModifier);
            SwapListContents(opponentTemporaryBpModifiers, otherLane.opponentTemporaryBpModifiers);
            Swap(ref opponentRestedThisTurn, ref otherLane.opponentRestedThisTurn);
        }

        static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        static void SwapListContents<T>(List<T> left, List<T> right)
        {
            if (left == null || right == null) return;

            var temp = new List<T>(left);
            left.Clear();
            left.AddRange(right);
            right.Clear();
            right.AddRange(temp);
        }

        public void RefreshPlayerStateFromPlayArea()
        {
            playerTopCard = playerPlayArea != null ? playerPlayArea.topCard : null;
            int stackCount = playerPlayArea != null ? playerPlayArea.GetStackCount() : 0;
            playerBp = UcgBattleJudge.CalculateLaneBp(
                playerTopCard,
                stackCount,
                playerTemporaryBpModifier + playerSceneBpModifier + playerConditionalBpModifier);
            laneResult = UcgLaneResultType.None;
        }

        public void AddTemporaryBpModifier(
            UcgPlayerSide side,
            int amount,
            UcgCardData sourceCard,
            string reason,
            int requiredStackCount = 0,
            int currentStackCount = 0,
            bool requireExactStackCount = false,
            bool isStepUp = false,
            int stepFromBp = 0,
            int stepToBp = 0)
        {
            var modifier = CreateBpModifier(amount, sourceCard, reason, UcgEffectDuration.UntilEndOfTurn);
            modifier.effectCategory = GetEffectCategoryText(sourceCard);
            modifier.requiredStackCount = requiredStackCount;
            modifier.currentStackCount = currentStackCount;
            modifier.requireExactStackCount = requireExactStackCount;
            modifier.stackRequirementMet = requiredStackCount <= 0
                || (requireExactStackCount ? currentStackCount == requiredStackCount : currentStackCount >= requiredStackCount);
            modifier.isStepUp = isStepUp;
            modifier.stepFromBp = stepFromBp;
            modifier.stepToBp = stepToBp;
            if (side == UcgPlayerSide.Player)
            {
                playerTemporaryBpModifier += amount;
                playerTemporaryBpModifiers.Add(modifier);
            }
            else
            {
                opponentTemporaryBpModifier += amount;
                opponentTemporaryBpModifiers.Add(modifier);
            }
        }

        public void AddSceneBpModifier(UcgPlayerSide side, int amount, UcgCardData sourceCard, string reason)
        {
            var modifier = CreateBpModifier(amount, sourceCard, reason, UcgEffectDuration.WhileSceneActive);
            modifier.effectCategory = GetEffectCategoryText(sourceCard);
            modifier.stackRequirementMet = true;
            if (side == UcgPlayerSide.Player)
            {
                playerSceneBpModifier += amount;
                playerSceneBpModifiers.Add(modifier);
            }
            else
            {
                opponentSceneBpModifier += amount;
                opponentSceneBpModifiers.Add(modifier);
            }
        }

        public void AddConditionalBpModifier(UcgPlayerSide side, UcgBpModifierInfo modifier)
        {
            if (modifier == null || modifier.amount == 0) return;

            if (side == UcgPlayerSide.Player)
            {
                playerConditionalBpModifier += modifier.amount;
                playerConditionalBpModifiers.Add(modifier);
            }
            else
            {
                opponentConditionalBpModifier += modifier.amount;
                opponentConditionalBpModifiers.Add(modifier);
            }
        }

        static UcgBpModifierInfo CreateBpModifier(int amount, UcgCardData sourceCard, string reason, UcgEffectDuration duration)
        {
            return new UcgBpModifierInfo
            {
                sourceCardId = sourceCard != null ? sourceCard.id : "",
                sourceCardName = sourceCard != null ? sourceCard.cardName : "",
                reason = reason,
                duration = duration,
                amount = amount
            };
        }

        static string GetEffectCategoryText(UcgCardData card)
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

        public UcgLaneResultType JudgeLane()
        {
            RefreshPlayerStateFromPlayArea();
            string message;
            laneResult = UcgBattleJudge.JudgeLane(this, out message);

            if (resultLabel != null)
            {
                resultLabel.text = $"{GetLaneDisplayNumber()}\n{message}";
            }

            ApplyJudgementVisual(laneResult);

            return laneResult;
        }

        void ApplyJudgementVisual(UcgLaneResultType result)
        {
            ClearJudgementCardHighlights();

            if (resultLabel != null)
            {
                switch (result)
                {
                    case UcgLaneResultType.PlayerWin:
                        resultLabel.color = new Color(0.35f, 1f, 0.55f, 0.98f);
                        break;
                    case UcgLaneResultType.OpponentWin:
                        resultLabel.color = new Color(1f, 0.42f, 0.42f, 0.98f);
                        break;
                    case UcgLaneResultType.Draw:
                        resultLabel.color = new Color(1f, 0.86f, 0.34f, 0.98f);
                        break;
                    default:
                        resultLabel.color = new Color(1f, 1f, 1f, 0.74f);
                        break;
                }
            }

            if (result == UcgLaneResultType.PlayerWin)
            {
                UcgCardView playerCard = playerPlayArea != null ? playerPlayArea.GetTopCard() : playerTopCard;
                if (playerCard != null)
                {
                    playerCard.SetPlayableHighlight(true);
                    playerCard.PlayJudgementFeedback(true);
                }

                UcgCardView opponentCard = GetOpponentTopCard();
                SetCardRested(UcgPlayerSide.Opponent, true);
                if (opponentCard != null) opponentCard.PlayJudgementFeedback(false);
            }
            else if (result == UcgLaneResultType.OpponentWin)
            {
                UcgCardView opponentCard = GetOpponentTopCard();
                if (opponentCard != null)
                {
                    opponentCard.SetPlayableHighlight(true);
                    opponentCard.PlayJudgementFeedback(true);
                }

                UcgCardView playerCard = playerPlayArea != null ? playerPlayArea.GetTopCard() : playerTopCard;
                SetCardRested(UcgPlayerSide.Player, true);
                if (playerCard != null) playerCard.PlayJudgementFeedback(false);
            }
        }

        public void RestoreRestedCards()
        {
            SetCardRested(UcgPlayerSide.Player, false);
            SetCardRested(UcgPlayerSide.Opponent, false);
            ClearJudgementCardHighlights();
        }

        void SetCardRested(UcgPlayerSide side, bool rested)
        {
            if (side == UcgPlayerSide.Player)
            {
                playerRestedThisTurn = rested;
                ApplyCardRestRotation(playerPlayArea != null ? playerPlayArea.GetTopCard() : playerTopCard, rested, -90f, true, UcgPlayerSide.Player);
                return;
            }

            opponentRestedThisTurn = rested;
            ApplyCardRestRotation(GetOpponentTopCard(), rested, 90f, true, UcgPlayerSide.Opponent);
        }

        void ApplyCardRestRotation(UcgCardView card, bool rested, float restedAngle, bool animate, UcgPlayerSide side)
        {
            if (card == null) return;

            RectTransform cardRect = card.transform as RectTransform;
            if (cardRect == null) return;

            if (side == UcgPlayerSide.Player && _playerRestRotationRoutine != null)
            {
                StopCoroutine(_playerRestRotationRoutine);
                _playerRestRotationRoutine = null;
            }
            else if (side == UcgPlayerSide.Opponent && _opponentRestRotationRoutine != null)
            {
                StopCoroutine(_opponentRestRotationRoutine);
                _opponentRestRotationRoutine = null;
            }

            Quaternion targetRotation = rested
                ? Quaternion.Euler(0f, 0f, restedAngle)
                : Quaternion.identity;
            if (!animate || !rested || !Application.isPlaying)
            {
                cardRect.localRotation = targetRotation;
                return;
            }

            Coroutine routine = StartCoroutine(CardRestRotationRoutine(cardRect, targetRotation, Mathf.Max(0.05f, restRotationAnimationSeconds), side));
            if (side == UcgPlayerSide.Player)
            {
                _playerRestRotationRoutine = routine;
            }
            else
            {
                _opponentRestRotationRoutine = routine;
            }
        }

        IEnumerator CardRestRotationRoutine(RectTransform cardRect, Quaternion targetRotation, float duration, UcgPlayerSide side)
        {
            if (cardRect == null) yield break;

            Quaternion startRotation = cardRect.localRotation;
            float elapsed = 0f;
            while (elapsed < duration && cardRect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cardRect.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (cardRect != null)
            {
                cardRect.localRotation = targetRotation;
            }

            if (side == UcgPlayerSide.Player)
            {
                _playerRestRotationRoutine = null;
            }
            else
            {
                _opponentRestRotationRoutine = null;
            }
        }

        public void PlayJudgementResultAnimation(float duration)
        {
            if (!Application.isPlaying) return;

            if (_judgementResultRoutine != null)
            {
                StopCoroutine(_judgementResultRoutine);
                _judgementResultRoutine = null;
            }

            _judgementResultRoutine = StartCoroutine(JudgementResultAnimationRoutine(Mathf.Max(0.1f, duration)));
        }

        IEnumerator JudgementResultAnimationRoutine(float duration)
        {
            RectTransform labelRect = resultLabel != null ? resultLabel.transform as RectTransform : null;
            Vector3 labelScale = labelRect != null ? labelRect.localScale : Vector3.one;
            Color labelColor = resultLabel != null ? resultLabel.color : Color.white;
            Image playerImage = playerSlot != null ? playerSlot.GetComponent<Image>() : null;
            Image opponentImage = opponentSlot != null ? opponentSlot.GetComponent<Image>() : null;
            Color playerBaseColor = playerImage != null ? playerImage.color : Color.white;
            Color opponentBaseColor = opponentImage != null ? opponentImage.color : Color.white;
            Color playerTargetColor = playerBaseColor;
            Color opponentTargetColor = opponentBaseColor;

            switch (laneResult)
            {
                case UcgLaneResultType.PlayerWin:
                    playerTargetColor = new Color(0.12f, 0.42f, 0.24f, 0.5f);
                    opponentTargetColor = new Color(0.14f, 0.05f, 0.06f, 0.42f);
                    break;
                case UcgLaneResultType.OpponentWin:
                    playerTargetColor = new Color(0.08f, 0.06f, 0.07f, 0.36f);
                    opponentTargetColor = new Color(0.42f, 0.12f, 0.14f, 0.52f);
                    break;
                case UcgLaneResultType.Draw:
                    playerTargetColor = new Color(0.42f, 0.35f, 0.1f, 0.42f);
                    opponentTargetColor = new Color(0.42f, 0.35f, 0.1f, 0.42f);
                    break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);

                if (labelRect != null)
                {
                    labelRect.localScale = labelScale * (1f + pulse * 0.08f);
                }

                if (resultLabel != null)
                {
                    Color color = labelColor;
                    color.a = Mathf.Clamp01(labelColor.a + pulse * 0.18f);
                    resultLabel.color = color;
                }

                if (playerImage != null)
                {
                    playerImage.color = Color.Lerp(playerBaseColor, playerTargetColor, pulse);
                }

                if (opponentImage != null)
                {
                    opponentImage.color = Color.Lerp(opponentBaseColor, opponentTargetColor, pulse);
                }

                yield return null;
            }

            if (labelRect != null) labelRect.localScale = labelScale;
            if (resultLabel != null) resultLabel.color = labelColor;
            if (playerImage != null) playerImage.color = playerBaseColor;
            if (opponentImage != null) opponentImage.color = opponentBaseColor;
            _judgementResultRoutine = null;
        }

        public void ClearJudgementCardHighlights()
        {
            UcgCardView playerCard = playerPlayArea != null ? playerPlayArea.GetTopCard() : playerTopCard;
            if (playerCard != null) playerCard.SetPlayableHighlight(false);

            UcgCardView opponentCard = GetOpponentTopCard();
            if (opponentCard != null) opponentCard.SetPlayableHighlight(false);
        }

        public int GetOpponentStackCount()
        {
            if (opponentSlot == null) return 0;

            int count = 0;
            for (int i = 0; i < opponentSlot.childCount; i++)
            {
                if (opponentSlot.GetChild(i).GetComponent<UcgCardView>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        public UcgCardView GetOpponentTopCard()
        {
            RefreshOpponentTopCard();
            return opponentTopCard;
        }

        public void FlipAllFaceDownCards()
        {
            FlipFaceDownCardsInSlot(playerSlot);
            FlipFaceDownCardsInSlot(opponentSlot);
        }

        public void FlipAllFaceDownCards(List<UcgEffectInstance> revealedEffects)
        {
            FlipFaceDownCardsInSlot(playerSlot, UcgPlayerSide.Player, revealedEffects);
            FlipFaceDownCardsInSlot(opponentSlot, UcgPlayerSide.Opponent, revealedEffects);
        }

        public void ClearTemporaryBpModifiers()
        {
            playerTemporaryBpModifier = 0;
            opponentTemporaryBpModifier = 0;
            playerTemporaryBpModifiers.Clear();
            opponentTemporaryBpModifiers.Clear();
        }

        public void ClearSceneBpModifiers()
        {
            playerSceneBpModifier = 0;
            opponentSceneBpModifier = 0;
            playerSceneBpModifiers.Clear();
            opponentSceneBpModifiers.Clear();
        }

        public void ClearConditionalBpModifiers()
        {
            playerConditionalBpModifier = 0;
            opponentConditionalBpModifier = 0;
            playerConditionalBpModifiers.Clear();
            opponentConditionalBpModifiers.Clear();
        }

        void ResetLaneState()
        {
            playerTopCard = null;
            playerBp = 0;
            opponentBp = 0;
            ClearTemporaryBpModifiers();
            ClearSceneBpModifiers();
            ClearConditionalBpModifiers();
            RestoreRestedCards();
            laneResult = UcgLaneResultType.None;
        }

        public void ClearOpponentCards()
        {
            if (opponentSlot == null) return;

            for (int i = opponentSlot.childCount - 1; i >= 0; i--)
            {
                Transform child = opponentSlot.GetChild(i);
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

            opponentTopCard = null;
            ApplySlotFocusState(opponentSlot, false, true);
            TraceOverviewSlotRectState("After ClearOpponentCards");
        }

        public void SetEffectTargetHighlight(UcgPlayerSide side, bool active)
        {
            if (side == UcgPlayerSide.Player)
            {
                if (playerPlayArea != null)
                {
                    playerPlayArea.SetHighlightState(UcgLaneHighlightState.Normal);
                    UcgCardView card = playerPlayArea.GetTopCard();
                    if (card != null)
                    {
                        card.SetEffectTargetHighlight(active);
                    }
                }

                return;
            }

            Image opponentImage = opponentSlot != null ? opponentSlot.GetComponent<Image>() : null;
            if (opponentImage != null)
            {
                opponentImage.color = _opponentSlotDefaultColor;
            }

            UcgCardView opponentCard = GetOpponentTopCard();
            if (opponentCard != null)
            {
                opponentCard.SetEffectTargetHighlight(active);
            }
        }

        public void ClearEffectTargetHighlight()
        {
            SetEffectTargetHighlight(UcgPlayerSide.Player, false);
            SetEffectTargetHighlight(UcgPlayerSide.Opponent, false);
        }

        public void SetActiveLaneFocus(bool active)
        {
            if (_laneFocusImage == null) return;

            _laneFocusImage.enabled = true;
            _laneFocusImage.color = active
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.012f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.003f);

            if (_laneFocusOutline != null)
            {
                _laneFocusOutline.enabled = true;
                _laneFocusOutline.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.34f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.035f);
                _laneFocusOutline.effectDistance = active
                    ? new Vector2(1.8f, -1.8f)
                    : new Vector2(0.8f, -0.8f);
            }

            ApplySlotFocusState(playerSlot, active, false);
            ApplySlotFocusState(opponentSlot, active, true);

            if (_laneFocusPulse != null)
            {
                _laneFocusPulse.alphaAmplitude = active ? 0.035f : 0.004f;
                _laneFocusPulse.CaptureBaseState();
                _laneFocusPulse.enabled = active;
            }

            Shadow focusShadow = EnsureUiShadow(_laneFocusImage.gameObject);
            if (focusShadow != null)
            {
                focusShadow.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.12f)
                    : new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.04f);
                focusShadow.effectDistance = active ? new Vector2(0f, -4f) : new Vector2(0f, -1f);
            }

            ApplyLaneDuelAxisState(active);
        }

        void ApplySlotFocusState(RectTransform slot, bool active, bool opponent)
        {
            if (slot == null) return;

            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.color = active
                    ? opponent
                        ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.025f)
                        : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.040f)
                    : opponent
                        ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.025f)
                        : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.025f);
            }

            Outline outline = slot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.62f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, opponent ? 0.14f : 0.18f);
                outline.effectDistance = active
                    ? new Vector2(2.4f, -2.4f)
                    : new Vector2(1.1f, -1.1f);
            }

            Shadow shadow = EnsureUiShadow(slot.gameObject);
            if (shadow != null)
            {
                shadow.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.11f)
                    : new Color(4f / 255f, 9f / 255f, 18f / 255f, opponent ? 0.10f : 0.14f);
                shadow.effectDistance = active
                    ? new Vector2(0f, -4f)
                    : new Vector2(0f, -2.5f);
            }

            SetSlotCardGroundShadow(slot, CountCardsInSlot(slot) > 0, active, opponent);
            if (_useOverviewCardLayout && _overviewCardSize.x > 0f && _overviewCardSize.y > 0f)
            {
                ApplyOverviewSlotVisualSize(slot, _overviewCardSize, !opponent);
            }
            TraceOverviewSlotRectState($"After ApplySlotFocusState:{slot.name}");
        }

        void RefreshOpponentTopCard()
        {
            opponentTopCard = null;
            if (opponentSlot == null) return;

            for (int i = 0; i < opponentSlot.childCount; i++)
            {
                var card = opponentSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null)
                {
                    opponentTopCard = card;
                }
            }
        }

        void ApplyOpponentCardSorting()
        {
            if (opponentSlot == null) return;

            const int opponentCardSortingBase = 2600;
            for (int i = 0; i < opponentSlot.childCount; i++)
            {
                var card = opponentSlot.GetChild(i).GetComponent<UcgCardView>();
                if (card == null) continue;

                var cardCanvas = card.GetComponent<Canvas>();
                if (cardCanvas == null) cardCanvas = card.gameObject.AddComponent<Canvas>();

                if (card.GetComponent<GraphicRaycaster>() == null)
                {
                    card.gameObject.AddComponent<GraphicRaycaster>();
                }

                cardCanvas.overrideSorting = true;
                cardCanvas.sortingOrder = opponentCardSortingBase + i;
                card.selectedSortingOrder = opponentCardSortingBase + i;
            }
        }

        void FlipFaceDownCardsInSlot(RectTransform slot)
        {
            if (slot == null) return;

            for (int i = 0; i < slot.childCount; i++)
            {
                var card = slot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null && card.IsFaceDown)
                {
                    card.FlipFaceUp();
                }
            }
        }

        void FlipFaceDownCardsInSlot(RectTransform slot, UcgPlayerSide ownerSide, List<UcgEffectInstance> revealedEffects)
        {
            if (slot == null) return;

            UcgCardView topCard = GetTopCardInSlot(slot);
            bool topCardWasFaceDown = topCard != null && topCard.IsFaceDown;
            for (int i = 0; i < slot.childCount; i++)
            {
                var card = slot.GetChild(i).GetComponent<UcgCardView>();
                if (card == null || !card.IsFaceDown) continue;

                card.FlipFaceUp();
            }

            if (revealedEffects == null || topCard == null || topCard.CardData == null) return;
            if (!topCardWasFaceDown) return;
            if (topCard.EnterEffectResolved) return;
            if (topCard.CardData.effectId == UcgDemoEffectId.None) return;
            if (topCard.CardData.effectTiming != UcgEffectTiming.OnRevealOrEnter) return;

            int stackCount = CountCardsInSlot(slot);
            if (!UcgEffectParser.IsStackRequirementMet(topCard.CardData, stackCount, out _, out _)) return;
            topCard.EnterEffectResolved = true;

            revealedEffects.Add(new UcgEffectInstance
            {
                effectId = topCard.CardData.effectId,
                cardData = topCard.CardData,
                sourceCard = topCard,
                lane = this,
                ownerSide = ownerSide,
                timing = topCard.CardData.effectTiming,
                effectKey = $"reveal:{laneIndex}:{ownerSide}:{topCard.CardData.id}:{topCard.CardData.effectId}"
            });
        }

        UcgCardView GetTopCardInSlot(RectTransform slot)
        {
            if (slot == null) return null;

            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var card = slot.GetChild(i).GetComponent<UcgCardView>();
                if (card != null) return card;
            }

            return null;
        }

        int CountCardsInSlot(RectTransform slot)
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

        Text CreateOpponentPlaceholderText(RectTransform parent)
        {
            var labelObject = new GameObject("PlaceholderText", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.16f);
            labelRect.anchorMax = new Vector2(0.9f, 0.84f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            if (_uiFont != null) label.font = _uiFont;
            label.fontSize = 18;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;
            label.raycastTarget = false;

            return label;
        }

        void EnsureLaneFocusBackdrop(RectTransform laneRect)
        {
            const string backdropName = "Lane Focus Backdrop";
            Transform existingBackdrop = transform.Find(backdropName);
            RectTransform backdropRect;

            if (existingBackdrop == null)
            {
                var backdropObject = new GameObject(backdropName, typeof(RectTransform), typeof(Image), typeof(Outline));
                backdropObject.transform.SetParent(transform, false);
                backdropRect = backdropObject.GetComponent<RectTransform>();
                _laneFocusImage = backdropObject.GetComponent<Image>();
                _laneFocusOutline = backdropObject.GetComponent<Outline>();
            }
            else
            {
                backdropRect = existingBackdrop as RectTransform;
                _laneFocusImage = existingBackdrop.GetComponent<Image>();
                if (_laneFocusImage == null) _laneFocusImage = existingBackdrop.gameObject.AddComponent<Image>();
                _laneFocusOutline = existingBackdrop.GetComponent<Outline>();
                if (_laneFocusOutline == null) _laneFocusOutline = existingBackdrop.gameObject.AddComponent<Outline>();
            }

            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(28f, 18f);
            backdropRect.offsetMax = new Vector2(-28f, -18f);
            backdropRect.localScale = Vector3.one;
            backdropRect.localEulerAngles = Vector3.zero;
            backdropRect.SetAsFirstSibling();

            _laneFocusImage.raycastTarget = false;
            _laneFocusOutline.effectDistance = new Vector2(2.4f, -2.4f);
            _laneFocusOutline.useGraphicAlpha = true;

            Shadow backdropShadow = EnsureUiShadow(backdropRect.gameObject);
            backdropShadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.05f);
            backdropShadow.effectDistance = new Vector2(0f, -1f);
            backdropShadow.useGraphicAlpha = true;

            _laneFocusPulse = backdropRect.GetComponent<UcgGuidancePulse>();
            if (_laneFocusPulse == null) _laneFocusPulse = backdropRect.gameObject.AddComponent<UcgGuidancePulse>();
            _laneFocusPulse.targetImage = _laneFocusImage;
            _laneFocusPulse.targetOutline = _laneFocusOutline;
            _laneFocusPulse.targetRect = backdropRect;
            _laneFocusPulse.pulseAlpha = true;
            _laneFocusPulse.alphaAmplitude = 0.006f;
            _laneFocusPulse.pulseScale = false;
            _laneFocusPulse.speed = 1.8f;
            _laneFocusPulse.enabled = false;
        }

        void EnsureLaneDuelAxis(RectTransform laneRect)
        {
            if (laneRect == null) return;

            const string axisName = "Lane Duel Axis";
            Transform existingAxis = transform.Find(axisName);
            if (existingAxis == null)
            {
                var axisObject = new GameObject(axisName, typeof(RectTransform), typeof(Image));
                axisObject.transform.SetParent(transform, false);
                _laneDuelAxisRect = axisObject.GetComponent<RectTransform>();
                _laneDuelAxisImage = axisObject.GetComponent<Image>();
            }
            else
            {
                _laneDuelAxisRect = existingAxis as RectTransform;
                _laneDuelAxisImage = existingAxis.GetComponent<Image>();
                if (_laneDuelAxisImage == null) _laneDuelAxisImage = existingAxis.gameObject.AddComponent<Image>();
            }

            _laneDuelAxisRect.anchorMin = new Vector2(0.5f, 0.5f);
            _laneDuelAxisRect.anchorMax = new Vector2(0.5f, 0.5f);
            _laneDuelAxisRect.pivot = new Vector2(0.5f, 0.5f);
            _laneDuelAxisRect.localScale = Vector3.one;
            _laneDuelAxisRect.localEulerAngles = Vector3.zero;
            _laneDuelAxisRect.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));

            _laneDuelAxisImage.raycastTarget = false;
            EnsureLaneDuelAxisLayer("Lane Duel Axis Soft Light", ref _laneDuelAxisGlowRect, ref _laneDuelAxisGlowImage);
            EnsureLaneDuelAxisLayer("Lane Duel Axis Center Core", ref _laneDuelAxisCoreRect, ref _laneDuelAxisCoreImage);
            ApplyLaneDuelAxisState(false);
        }

        void EnsureLaneDuelAxisLayer(string objectName, ref RectTransform rect, ref Image image)
        {
            Transform existingLayer = transform.Find(objectName);
            if (existingLayer == null)
            {
                var layerObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                layerObject.transform.SetParent(transform, false);
                rect = layerObject.GetComponent<RectTransform>();
                image = layerObject.GetComponent<Image>();
            }
            else
            {
                rect = existingLayer as RectTransform;
                image = existingLayer.GetComponent<Image>();
                if (image == null) image = existingLayer.gameObject.AddComponent<Image>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            image.raycastTarget = false;
            image.enabled = false;
            image.color = Color.clear;
            ApplySlicedUiSprite(image);
        }

        void ApplyLaneDuelAxisState(bool active)
        {
            if (_laneDuelAxisImage == null) return;

            UpdateLaneDuelAxisLayout(active);
            _laneDuelAxisImage.enabled = active;
            _laneDuelAxisImage.color = active
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.30f)
                : Color.clear;

            if (_laneDuelAxisGlowImage != null)
            {
                _laneDuelAxisGlowImage.enabled = active;
                _laneDuelAxisGlowImage.color = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.070f)
                    : Color.clear;
            }

            if (_laneDuelAxisCoreImage != null)
            {
                _laneDuelAxisCoreImage.enabled = active;
                _laneDuelAxisCoreImage.color = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.20f)
                    : Color.clear;
            }
        }

        void UpdateLaneDuelAxisLayout(bool active)
        {
            if (_laneDuelAxisRect == null) return;

            float playerY = playerSlot != null ? playerSlot.anchoredPosition.y : -150f;
            float opponentY = opponentSlot != null ? opponentSlot.anchoredPosition.y : 150f;
            float playerHeight = playerSlot != null ? playerSlot.sizeDelta.y : 224f;
            float opponentHeight = opponentSlot != null ? opponentSlot.sizeDelta.y : 224f;
            float lowerCardTop = Mathf.Min(playerY, opponentY) + playerHeight * 0.5f + 12f;
            float upperCardBottom = Mathf.Max(playerY, opponentY) - opponentHeight * 0.5f - 12f;
            float axisHeight = Mathf.Max(0f, upperCardBottom - lowerCardTop);

            _laneDuelAxisRect.anchoredPosition = new Vector2(0f, (lowerCardTop + upperCardBottom) * 0.5f);
            _laneDuelAxisRect.sizeDelta = new Vector2(active ? 3f : 2f, axisHeight);

            if (_laneDuelAxisGlowRect != null)
            {
                _laneDuelAxisGlowRect.anchoredPosition = _laneDuelAxisRect.anchoredPosition;
                _laneDuelAxisGlowRect.sizeDelta = new Vector2(active ? 14f : 8f, axisHeight);
                _laneDuelAxisGlowRect.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
            }

            if (_laneDuelAxisRect != null)
            {
                _laneDuelAxisRect.SetSiblingIndex(Mathf.Min(2, transform.childCount - 1));
            }

            if (_laneDuelAxisCoreRect != null)
            {
                _laneDuelAxisCoreRect.anchoredPosition = _laneDuelAxisRect.anchoredPosition;
                _laneDuelAxisCoreRect.sizeDelta = new Vector2(active ? 13f : 8f, active ? 13f : 8f);
                _laneDuelAxisCoreRect.SetSiblingIndex(Mathf.Min(3, transform.childCount - 1));
            }
        }

        RectTransform EnsureSlot(string slotName, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
        {
            Transform existingSlot = transform.Find(slotName);
            RectTransform slotRect;
            Image slotImage;

            if (existingSlot == null)
            {
                var slotObject = new GameObject(slotName, typeof(RectTransform), typeof(Image));
                slotObject.transform.SetParent(transform, false);
                slotRect = slotObject.GetComponent<RectTransform>();
                slotImage = slotObject.GetComponent<Image>();
            }
            else
            {
                slotRect = existingSlot as RectTransform;
                slotImage = existingSlot.GetComponent<Image>();
                if (slotImage == null) slotImage = existingSlot.gameObject.AddComponent<Image>();
            }

            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = anchoredPosition;
            slotRect.sizeDelta = size;

            ApplySlicedUiSprite(slotImage);
            bool isPlayerSlot = slotName.Contains("Player");
            slotImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, isPlayerSlot ? 0.135f : 0.110f);
            slotImage.raycastTarget = raycastTarget;

            var outline = slotRect.GetComponent<Outline>();
            if (outline == null) outline = slotRect.gameObject.AddComponent<Outline>();
            outline.effectColor = isPlayerSlot
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.26f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.22f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(slotRect.gameObject);
            shadow.effectColor = new Color(4f / 255f, 9f / 255f, 18f / 255f, isPlayerSlot ? 0.24f : 0.20f);
            shadow.effectDistance = new Vector2(0f, -4.5f);
            shadow.useGraphicAlpha = true;

            EnsureSlotSurfaceDetails(slotRect, isPlayerSlot);

            return slotRect;
        }

        void EnsureSlotSurfaceDetails(RectTransform slotRect, bool isPlayerSlot)
        {
            if (slotRect == null) return;

            RectTransform interior = EnsureSlotDecorImage(
                slotRect,
                "Slot Interior Shade",
                Vector2.zero,
                Vector2.one,
                new Vector2(6f, 6f),
                new Vector2(-6f, -6f),
                UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, isPlayerSlot ? 0.105f : 0.085f),
                true);
            Color edgeColor = UcgToolUiPalette.WithAlpha(
                isPlayerSlot ? UcgToolUiPalette.FocusCyan : UcgToolUiPalette.GlassBorder,
                isPlayerSlot ? 0.16f : 0.13f);
            RectTransform topEdge = EnsureSlotDecorImage(
                slotRect,
                "Slot Edge Top",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(16f, -2f),
                new Vector2(-16f, 0f),
                edgeColor,
                false);
            RectTransform bottomEdge = EnsureSlotDecorImage(
                slotRect,
                "Slot Edge Bottom",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(16f, 0f),
                new Vector2(-16f, 2f),
                edgeColor,
                false);
            RectTransform leftEdge = EnsureSlotDecorImage(
                slotRect,
                "Slot Edge Left",
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 16f),
                new Vector2(2f, -16f),
                edgeColor,
                false);
            RectTransform rightEdge = EnsureSlotDecorImage(
                slotRect,
                "Slot Edge Right",
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-2f, 16f),
                new Vector2(0f, -16f),
                edgeColor,
                false);
            RectTransform bottomShade = EnsureSlotDecorImage(
                slotRect,
                "Slot Bottom Shade",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(18f, 4f),
                new Vector2(-18f, 15f),
                new Color(0f, 0f, 0f, isPlayerSlot ? 0.105f : 0.085f),
                true);
            RectTransform topHighlight = EnsureSlotDecorImage(
                slotRect,
                "Slot Top Highlight",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(18f, -5f),
                new Vector2(-18f, -3f),
                UcgToolUiPalette.WithAlpha(UcgToolUiPalette.SoftWhite, isPlayerSlot ? 0.060f : 0.046f),
                true);
            RectTransform cardShadow = EnsureSlotDecorImage(
                slotRect,
                "Slot Card Ground Shadow",
                new Vector2(0.14f, 0.17f),
                new Vector2(0.86f, 0.17f),
                new Vector2(0f, -14f),
                new Vector2(0f, 8f),
                new Color(0f, 0f, 0f, 0.070f),
                true);

            if (interior != null) interior.SetAsFirstSibling();
            if (bottomShade != null) bottomShade.SetSiblingIndex(Mathf.Min(1, slotRect.childCount - 1));
            if (topHighlight != null) topHighlight.SetSiblingIndex(Mathf.Min(2, slotRect.childCount - 1));
            if (cardShadow != null) cardShadow.SetSiblingIndex(Mathf.Min(3, slotRect.childCount - 1));
            if (topEdge != null) topEdge.SetSiblingIndex(Mathf.Min(4, slotRect.childCount - 1));
            if (bottomEdge != null) bottomEdge.SetSiblingIndex(Mathf.Min(5, slotRect.childCount - 1));
            if (leftEdge != null) leftEdge.SetSiblingIndex(Mathf.Min(6, slotRect.childCount - 1));
            if (rightEdge != null) rightEdge.SetSiblingIndex(Mathf.Min(7, slotRect.childCount - 1));
            EnsureSlotCornerMarkers(slotRect, isPlayerSlot);
            TraceOverviewSlotRectState($"After EnsureSlotSurfaceDetails:{slotRect.name}");
        }

        void EnsureSlotCornerMarkers(RectTransform slotRect, bool isPlayerSlot)
        {
            if (slotRect == null) return;

            Color color = UcgToolUiPalette.WithAlpha(
                isPlayerSlot ? UcgToolUiPalette.FocusCyan : UcgToolUiPalette.GlassBorder,
                isPlayerSlot ? 0.22f : 0.16f);
            EnsureSlotDecorImage(slotRect, "Slot Corner TL H", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(13f, -11f), new Vector2(28f, -9f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner TL V", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(11f, -28f), new Vector2(13f, -13f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner TR H", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -11f), new Vector2(-13f, -9f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner TR V", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-13f, -28f), new Vector2(-11f, -13f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner BL H", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(13f, 9f), new Vector2(28f, 11f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner BL V", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(11f, 13f), new Vector2(13f, 28f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner BR H", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 9f), new Vector2(-13f, 11f), color, false);
            EnsureSlotDecorImage(slotRect, "Slot Corner BR V", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-13f, 13f), new Vector2(-11f, 28f), color, false);
        }

        void SetSlotCardGroundShadow(RectTransform slot, bool occupied, bool active, bool opponent)
        {
            if (slot == null) return;

            Transform shadowTransform = slot.Find("Slot Card Ground Shadow");
            if (shadowTransform == null)
            {
                EnsureSlotSurfaceDetails(slot, !opponent);
                shadowTransform = slot.Find("Slot Card Ground Shadow");
            }
            RectTransform shadowRect = shadowTransform as RectTransform;
            Image shadowImage = shadowTransform != null ? shadowTransform.GetComponent<Image>() : null;
            if (shadowRect == null || shadowImage == null) return;

            float baseAlpha = occupied ? 0.30f : 0.070f;
            float stateLift = active ? 0.018f : 0f;
            shadowImage.enabled = true;
            shadowImage.color = active
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, occupied ? baseAlpha + stateLift : 0.070f)
                : new Color(2f / 255f, 6f / 255f, 14f / 255f, baseAlpha);
            shadowImage.raycastTarget = false;

            shadowRect.anchorMin = new Vector2(occupied ? 0.10f : 0.18f, occupied ? 0.17f : 0.12f);
            shadowRect.anchorMax = new Vector2(occupied ? 0.90f : 0.82f, occupied ? 0.17f : 0.12f);
            shadowRect.offsetMin = new Vector2(0f, occupied ? -18f : -9f);
            shadowRect.offsetMax = new Vector2(0f, occupied ? 10f : 6f);
            shadowRect.localScale = Vector3.one;
            shadowRect.localEulerAngles = Vector3.zero;
            shadowRect.SetSiblingIndex(Mathf.Min(3, slot.childCount - 1));
            TraceOverviewSlotRectState($"After SetSlotCardGroundShadow:{slot.name}");
        }

        RectTransform EnsureSlotDecorImage(
            RectTransform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            bool sliced)
        {
            if (parent == null) return null;

            Transform existing = parent.Find(objectName);
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                var decorObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                decorObject.transform.SetParent(parent, false);
                rect = decorObject.GetComponent<RectTransform>();
                image = decorObject.GetComponent<Image>();
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
                if (image == null) image = existing.gameObject.AddComponent<Image>();
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;

            if (sliced) ApplySlicedUiSprite(image);
            image.color = color;
            image.raycastTarget = false;
            image.enabled = color.a > 0.001f;

            return rect;
        }

        static void ApplySlicedUiSprite(Image image)
        {
            if (image == null) return;

            image.sprite = GetOverviewEmptySlotSprite();
            image.type = Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1f;
        }

        static Shadow EnsureUiShadow(GameObject target)
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

        Text EnsureResultLabel()
        {
            const string labelName = "Result Label";
            Transform existingLabel = transform.Find(labelName);
            RectTransform labelRect;
            Text label;

            if (existingLabel == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(transform, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                label = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existingLabel as RectTransform;
                label = existingLabel.GetComponent<Text>();
                if (label == null) label = existingLabel.gameObject.AddComponent<Text>();
            }

            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(224f, 42f);

            label.text = GetLaneDisplayNumber();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 1f, 1f, 0.54f);
            if (_uiFont != null) label.font = _uiFont;
            label.fontSize = 13;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 13;
            label.raycastTarget = false;

            return label;
        }

        string GetLaneDisplayNumber()
        {
            return (laneIndex + 1).ToString("00");
        }

        void EnsureLaneNumberLabels()
        {
            _opponentLaneNumberLabel = EnsureLaneNumberLabel(
                "Opponent Lane Number Label",
                new Color(1f, 0.50f, 0.55f, 0.78f));
            _playerLaneNumberLabel = EnsureLaneNumberLabel(
                "Player Lane Number Label",
                new Color(0.42f, 0.82f, 1f, 0.78f));
            ApplyLaneNumberLabelLayout();
        }

        Text EnsureLaneNumberLabel(string labelName, Color color)
        {
            Transform existingLabel = transform.Find(labelName);
            RectTransform labelRect;
            Text label;

            if (existingLabel == null)
            {
                var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(transform, false);
                labelRect = labelObject.GetComponent<RectTransform>();
                label = labelObject.GetComponent<Text>();
            }
            else
            {
                labelRect = existingLabel as RectTransform;
                label = existingLabel.GetComponent<Text>();
                if (label == null) label = existingLabel.gameObject.AddComponent<Text>();
            }

            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            label.text = GetLaneDisplayNumber();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            if (_uiFont != null) label.font = _uiFont;
            label.fontSize = 14;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 14;
            label.raycastTarget = false;

            return label;
        }

        void ApplyLaneNumberLabelLayout()
        {
            ApplyLaneNumberLabelLayout(_opponentLaneNumberLabel, opponentSlot);
            ApplyLaneNumberLabelLayout(_playerLaneNumberLabel, playerSlot);
        }

        static void ApplyLaneNumberLabelLayout(Text label, RectTransform slot)
        {
            if (label == null || slot == null) return;

            RectTransform labelRect = label.transform as RectTransform;
            if (labelRect == null) return;

            float labelHeight = 26f;
            float gap = 20f;
            labelRect.sizeDelta = new Vector2(Mathf.Max(72f, slot.sizeDelta.x), labelHeight);
            labelRect.anchoredPosition = new Vector2(
                slot.anchoredPosition.x,
                slot.anchoredPosition.y - slot.sizeDelta.y * 0.5f - gap);
        }

        void EnsureSlotLabel(RectTransform parent, string text)
        {
            const string labelName = "Slot Label";
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
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);

            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.82f, 0.96f, 1f, 0.70f);
            if (_uiFont != null) label.font = _uiFont;
            label.fontSize = 24;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 24;
            label.raycastTarget = false;
        }
    }
}
