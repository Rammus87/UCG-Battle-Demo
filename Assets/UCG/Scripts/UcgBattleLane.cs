using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
        Color _opponentSlotDefaultColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.18f);
        Color _effectTargetColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.62f);
        Image _laneFocusImage;
        Outline _laneFocusOutline;
        UcgGuidancePulse _laneFocusPulse;
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

            opponentSlot = EnsureSlot("Opponent Slot", new Vector2(0f, 310f), opponentSlotSize, _opponentSlotDefaultColor, true);
            EnsureSlotLabel(opponentSlot, "對手");

            resultLabel = EnsureResultLabel();

            playerSlot = EnsureSlot("Player Slot", new Vector2(0f, -310f), playerSlotSize, UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.2f), true);
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
            playerPlayArea.defaultColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.2f);
            playerPlayArea.hoverColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.18f);
            playerPlayArea.occupiedColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.24f);
            playerPlayArea.activeSetupColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.2f);
            playerPlayArea.upgradeAvailableColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.WarningGold, 0.24f);
            playerPlayArea.validDropColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.24f);
            playerPlayArea.invalidDropColor = new Color(0.46f, 0.08f, 0.12f, 0.22f);
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
            ApplySlotPosition(opponentSlot, opponentSlotPosition);
            ApplySlotPosition(playerSlot, playerSlotPosition);
        }

        static void ApplySlotPosition(RectTransform slot, Vector2 anchoredPosition)
        {
            if (slot == null) return;

            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = anchoredPosition;
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
            cardRect.anchoredPosition = new Vector2(0f, 18f) + new Vector2(8f, 8f) * stackIndex;
            cardRect.sizeDelta = _fixedOpponentCardSize;
            cardRect.localScale = new Vector3(1f, 0.94f, 1f);
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
                resultLabel.text = $"Lane {laneIndex + 1}：待判定";
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
                resultLabel.text = $"Lane {laneIndex + 1}\n{message}";
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
                        card.SetPlayableHighlight(active);
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
                opponentCard.SetPlayableHighlight(active);
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
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.06f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.035f);

            if (_laneFocusOutline != null)
            {
                _laneFocusOutline.enabled = true;
                _laneFocusOutline.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.28f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.12f);
                _laneFocusOutline.effectDistance = active
                    ? new Vector2(2.2f, -2.2f)
                    : new Vector2(1.7f, -1.7f);
            }

            ApplySlotFocusState(playerSlot, active, false);
            ApplySlotFocusState(opponentSlot, false, true);

            if (_laneFocusPulse != null)
            {
                _laneFocusPulse.alphaAmplitude = active ? 0.018f : 0.025f;
                _laneFocusPulse.CaptureBaseState();
                _laneFocusPulse.enabled = active;
            }
        }

        void ApplySlotFocusState(RectTransform slot, bool active, bool opponent)
        {
            if (slot == null) return;

            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.color = active
                    ? opponent
                        ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.18f)
                        : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPink, 0.2f)
                    : opponent
                        ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.18f)
                        : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.DeepGlass, 0.2f);
            }

            Outline outline = slot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = active
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.68f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.18f);
                outline.effectDistance = active
                    ? new Vector2(3.2f, -3.2f)
                    : new Vector2(1.5f, -1.5f);
            }
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
            backdropShadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.2f);
            backdropShadow.effectDistance = new Vector2(0f, -3f);
            backdropShadow.useGraphicAlpha = true;

            _laneFocusPulse = backdropRect.GetComponent<UcgGuidancePulse>();
            if (_laneFocusPulse == null) _laneFocusPulse = backdropRect.gameObject.AddComponent<UcgGuidancePulse>();
            _laneFocusPulse.targetImage = _laneFocusImage;
            _laneFocusPulse.targetRect = backdropRect;
            _laneFocusPulse.pulseAlpha = true;
            _laneFocusPulse.alphaAmplitude = 0.055f;
            _laneFocusPulse.pulseScale = false;
            _laneFocusPulse.speed = 1.8f;
            _laneFocusPulse.enabled = false;
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
            slotImage.color = color;
            slotImage.raycastTarget = raycastTarget;

            var outline = slotRect.GetComponent<Outline>();
            if (outline == null) outline = slotRect.gameObject.AddComponent<Outline>();
            outline.effectColor = slotName.Contains("Player")
                ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.42f)
                : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.GlassBorder, 0.3f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);
            outline.useGraphicAlpha = true;

            var shadow = EnsureUiShadow(slotRect.gameObject);
            shadow.effectColor = new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            return slotRect;
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

            label.text = $"Lane {laneIndex + 1}：待判定";
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
