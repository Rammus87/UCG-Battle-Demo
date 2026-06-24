using System.Collections.Generic;
using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgEffectManager : MonoBehaviour
    {
        readonly List<UcgEffectInstance> _effectQueue = new List<UcgEffectInstance>();
        readonly HashSet<string> _usedActivatedEffectKeysThisTurn = new HashSet<string>();

        public int PendingCount => _effectQueue.Count;
        public bool HasPendingEffects => _effectQueue.Count > 0;

        public UcgEffectInstance PeekNextEffect()
        {
            return _effectQueue.Count > 0 ? _effectQueue[0] : null;
        }

        public List<UcgEffectInstance> GetPendingEffectsSnapshot()
        {
            return new List<UcgEffectInstance>(_effectQueue);
        }

        public void Clear()
        {
            _effectQueue.Clear();
            _usedActivatedEffectKeysThisTurn.Clear();
        }

        public void ClearQueue()
        {
            _effectQueue.Clear();
        }

        public void ClearUsedActivatedEffectsThisTurn()
        {
            _usedActivatedEffectKeysThisTurn.Clear();
        }

        public void EnqueueRevealEffects(List<UcgEffectInstance> revealedEffects, UcgPlayerSide firstPlayer)
        {
            EnqueueEffects(revealedEffects, firstPlayer, UcgEffectTiming.OnRevealOrEnter);
        }

        public void EnqueueActivatedEffects(List<UcgEffectInstance> activatedEffects, UcgPlayerSide firstPlayer)
        {
            EnqueueEffects(activatedEffects, firstPlayer, UcgEffectTiming.Activated);
        }

        void EnqueueEffects(List<UcgEffectInstance> effects, UcgPlayerSide firstPlayer, UcgEffectTiming timing)
        {
            if (effects == null || effects.Count == 0) return;

            effects.Sort((left, right) =>
            {
                int sideCompare = GetSideOrder(left.ownerSide, firstPlayer).CompareTo(GetSideOrder(right.ownerSide, firstPlayer));
                if (sideCompare != 0) return sideCompare;
                return left.LaneIndex.CompareTo(right.LaneIndex);
            });

            for (int i = 0; i < effects.Count; i++)
            {
                UcgEffectInstance effect = effects[i];
                if (effect == null || effect.effectId == UcgDemoEffectId.None) continue;
                if (effect.timing != timing) continue;
                if (timing == UcgEffectTiming.Activated && IsActivatedEffectUsed(effect)) continue;

                if (!_effectQueue.Contains(effect))
                {
                    _effectQueue.Add(effect);
                }
            }
        }

        public bool IsActivatedEffectUsed(UcgEffectInstance effect)
        {
            if (effect == null || string.IsNullOrEmpty(effect.effectKey)) return false;
            return _usedActivatedEffectKeysThisTurn.Contains(effect.effectKey);
        }

        public void MarkActivatedEffectUsed(UcgEffectInstance effect)
        {
            if (effect == null || effect.timing != UcgEffectTiming.Activated || string.IsNullOrEmpty(effect.effectKey)) return;
            _usedActivatedEffectKeysThisTurn.Add(effect.effectKey);
        }

        public UcgEffectInstance FindQueuedEffectForCard(UcgCardView cardView)
        {
            if (cardView == null) return null;
            for (int i = 0; i < _effectQueue.Count; i++)
            {
                UcgEffectInstance effect = _effectQueue[i];
                if (effect != null && effect.sourceCard == cardView) return effect;
            }

            return null;
        }

        public UcgEffectInstance FindQueuedEffectForScene(UcgSceneCardView sceneCardView)
        {
            if (sceneCardView == null) return null;
            for (int i = 0; i < _effectQueue.Count; i++)
            {
                UcgEffectInstance effect = _effectQueue[i];
                if (effect != null && effect.sourceSceneCard == sceneCardView) return effect;
            }

            return null;
        }

        public void MoveEffectToFront(UcgEffectInstance effect)
        {
            if (effect == null) return;
            if (!_effectQueue.Remove(effect)) return;
            _effectQueue.Insert(0, effect);
        }

        public bool ResolveNextEffect(UcgHandDemo demo, out string message)
        {
            message = "沒有可發動效果";
            if (_effectQueue.Count == 0) return false;

            UcgEffectInstance effect = _effectQueue[0];
            if (!IsStackRequirementMet(effect, out int requiredStackCount, out int currentStackCount, out bool requireExactStackCount, out string stackMessage))
            {
                _effectQueue.RemoveAt(0);
                message = "沒有可發動效果";
                return true;
            }

            if (!IsAdditionalConditionMet(effect, demo, out string conditionMessage))
            {
                _effectQueue.RemoveAt(0);
                message = string.IsNullOrWhiteSpace(conditionMessage)
                    ? "條件未達成，效果不發動。"
                    : conditionMessage;
                return true;
            }

            if (EffectNeedsTarget(effect))
            {
                message = "請先選擇效果目標";
                return false;
            }

            _effectQueue.RemoveAt(0);

            if (effect == null || effect.cardData == null || effect.lane == null)
            {
                if (effect != null && effect.isSceneEffect)
                {
                    return ResolveSceneEffect(effect, demo, out message);
                }

                message = "效果來源不存在，略過";
                return true;
            }

            string sideText = effect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手";
            string cardName = string.IsNullOrEmpty(effect.cardData.cardName) ? "角色" : effect.cardData.cardName;
            string phaseText = effect.timing == UcgEffectTiming.OnRevealOrEnter ? "登場效果階段" : "戰鬥效果階段";

            switch (effect.effectId)
            {
                case UcgDemoEffectId.OnRevealSelfBpPlus1000:
                case UcgDemoEffectId.OnRevealSelfBpPlus2000:
                case UcgDemoEffectId.OnRevealSelfBpPlus3000:
                case UcgDemoEffectId.ActivatedSelfBpPlus1000:
                case UcgDemoEffectId.ActivatedSelfBpPlus2000:
                case UcgDemoEffectId.ActivatedSelfBpPlus3000:
                    int selfAmount = GetPlusAmount(effect.effectId);
                    string selfReason = effect.timing == UcgEffectTiming.Activated ? "戰鬥效果" : "登場時效果";
                    AddBpModifier(effect.lane, effect.ownerSide, selfAmount, effect.cardData, selfReason, requiredStackCount, currentStackCount, requireExactStackCount);
                    if (effect.timing == UcgEffectTiming.Activated) MarkActivatedEffectUsed(effect);
                    LogEffectResolved(effect, "ModifyBp", selfAmount);
                    message = $"{phaseText}｜{sideText} {cardName}：BP +{selfAmount}";
                    return true;
                case UcgDemoEffectId.ActivatedSelfBpStepUp:
                    int stackCount = GetEffectStackCount(effect);
                    if (currentStackCount <= 0) currentStackCount = stackCount;
                    int currentBp = effect.cardData.GetBpByStackCount(stackCount);
                    int stepToBp = UcgBattleJudge.GetNextBpStep(effect.cardData, currentBp);
                    int stepAmount = stepToBp - currentBp;
                    AddBpStepModifier(effect.lane, effect.ownerSide, stepAmount, effect.cardData, "戰鬥效果", requiredStackCount, currentStackCount, requireExactStackCount, currentBp, stepToBp);
                    MarkActivatedEffectUsed(effect);
                    LogEffectResolved(effect, "BpStepUp", stepAmount);
                    message = $"{phaseText}｜{sideText} {cardName}：BP 上升一階";
                    return true;
                case UcgDemoEffectId.OnRevealDrawOne:
                case UcgDemoEffectId.OnRevealDrawTwo:
                case UcgDemoEffectId.ActivatedDrawOne:
                case UcgDemoEffectId.ActivatedDrawTwo:
                    int drawCount = GetDrawCount(effect.effectId);
                    int drawn = demo != null ? demo.DrawCardsFromEffect(effect.ownerSide, drawCount, effect.cardData) : 0;
                    if (effect.timing == UcgEffectTiming.Activated) MarkActivatedEffectUsed(effect);
                    if (drawn > 0) LogEffectResolved(effect, "DrawCards", drawn);
                    message = drawn > 0
                        ? $"{phaseText}｜{sideText} {cardName}：抽 {drawn} 張牌"
                        : $"{phaseText}｜{sideText}牌組已空，無法抽牌";
                    return true;
                case UcgDemoEffectId.OnRevealDeckOperation:
                case UcgDemoEffectId.ActivatedDeckOperation:
                    UcgEffectRule operationRule = UcgEffectParser.ParsePrimaryRule(effect.cardData);
                    if (operationRule == null || operationRule.deckOperation == null || operationRule.actionType != UcgEffectActionType.DeckOperation)
                    {
                        message = $"{phaseText}｜{sideText} {cardName}：牌庫操作暫不支援";
                        return true;
                    }

                    if (effect.timing == UcgEffectTiming.Activated) MarkActivatedEffectUsed(effect);
                    bool deckOperationStarted = demo != null && demo.ResolveDeckOperationFromEffect(effect, operationRule.deckOperation, out message);
                    if (deckOperationStarted) LogEffectResolved(effect, "DeckOperation", operationRule.deckOperation.revealCount);
                    return true;
                case UcgDemoEffectId.OnRevealOpponentBpMinus1000:
                case UcgDemoEffectId.OnRevealOpponentBpMinus2000:
                case UcgDemoEffectId.OnRevealOpponentBpMinus3000:
                    UcgPlayerSide targetSide = GetOpponentSide(effect.ownerSide);
                    string targetText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
                    int minusAmount = -GetMinusAmount(effect.effectId);
                    AddBpModifier(effect.lane, targetSide, minusAmount, effect.cardData, "登場時效果", requiredStackCount, currentStackCount, requireExactStackCount);
                    LogEffectResolved(effect, "ModifyBp", minusAmount);
                    message = $"{phaseText}｜{sideText} {cardName}：{targetText}角色 BP {minusAmount}";
                    return true;
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000:
                    message = "請先選擇效果目標";
                    _effectQueue.Insert(0, effect);
                    return false;
                case UcgDemoEffectId.OnRevealGrantOwnTemporaryType:
                case UcgDemoEffectId.OnRevealGrantOpponentTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOwnTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOpponentTemporaryType:
                case UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp:
                case UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp:
                case UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp:
                case UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga:
                    message = "請先選擇效果目標";
                    _effectQueue.Insert(0, effect);
                    return false;
                default:
                    message = $"{phaseText}｜{sideText} {cardName} 沒有效果";
                    return true;
            }
        }

        public bool ResolveImmediateEffect(UcgEffectInstance effect, UcgHandDemo demo, out string message)
        {
            message = "沒有可發動效果";
            if (effect == null) return false;

            _effectQueue.Insert(0, effect);
            return ResolveNextEffect(demo, out message);
        }

        bool ResolveSceneEffect(UcgEffectInstance effect, UcgHandDemo demo, out string message)
        {
            string sideText = effect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手";
            string cardName = effect.cardData != null && !string.IsNullOrEmpty(effect.cardData.cardName) ? effect.cardData.cardName : "場景";

            switch (effect.effectId)
            {
                case UcgDemoEffectId.OnRevealDrawOne:
                case UcgDemoEffectId.OnRevealDrawTwo:
                    int drawCount = GetDrawCount(effect.effectId);
                    int drawn = demo != null ? demo.DrawCardsFromEffect(effect.ownerSide, drawCount, effect.cardData) : 0;
                    if (drawn > 0) LogEffectResolved(effect, "DrawCards", drawn);
                    message = drawn > 0
                        ? $"場景出現時｜{sideText} {cardName}：抽 {drawn} 張牌"
                        : $"場景出現時｜{sideText}牌組已空，無法抽牌";
                    return true;
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000:
                    message = "請先選擇效果目標";
                    _effectQueue.Insert(0, effect);
                    return false;
                case UcgDemoEffectId.ActivatedGrantOpponentTemporaryType:
                    message = "請先選擇效果目標";
                    _effectQueue.Insert(0, effect);
                    return false;
                case UcgDemoEffectId.ActivatedSceneUpgradeFromDeckThenDiscardAtEnd:
                    MarkActivatedEffectUsed(effect);
                    if (demo == null)
                    {
                        message = "場景效果無法處理：Demo 尚未建立";
                        return false;
                    }
                    return demo.ResolveBp01105SceneActivatedEffect(effect, out message);
                default:
                    message = $"場景效果｜{sideText} {cardName} 沒有一次性效果";
                    return true;
            }
        }

        public bool EffectNeedsTarget(UcgEffectInstance effect)
        {
            if (effect == null) return false;

            return effect.effectId == UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus2000
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus3000
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus2000
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000
                || effect.effectId == UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000
                || effect.effectId == UcgDemoEffectId.OnRevealGrantOwnTemporaryType
                || effect.effectId == UcgDemoEffectId.OnRevealGrantOpponentTemporaryType
                || effect.effectId == UcgDemoEffectId.ActivatedGrantOwnTemporaryType
                || effect.effectId == UcgDemoEffectId.ActivatedGrantOpponentTemporaryType
                || effect.effectId == UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp
                || effect.effectId == UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp
                || effect.effectId == UcgDemoEffectId.OnRevealSwapOwnCharacters
                || effect.effectId == UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp
                || effect.effectId == UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga;
        }

        public UcgEffectTargetType GetTargetType(UcgEffectInstance effect)
        {
            if (effect == null) return UcgEffectTargetType.None;

            switch (effect.effectId)
            {
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000:
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus2000:
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus3000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000:
                    return effect.ownerSide == UcgPlayerSide.Player
                        ? UcgEffectTargetType.OwnCharacter
                        : UcgEffectTargetType.OpponentCharacter;
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000:
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus2000:
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000:
                    return effect.ownerSide == UcgPlayerSide.Player
                        ? UcgEffectTargetType.OpponentCharacter
                        : UcgEffectTargetType.OwnCharacter;
                case UcgDemoEffectId.OnRevealGrantOwnTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOwnTemporaryType:
                    return effect.ownerSide == UcgPlayerSide.Player
                        ? UcgEffectTargetType.OwnCharacter
                        : UcgEffectTargetType.OpponentCharacter;
                case UcgDemoEffectId.OnRevealGrantOpponentTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOpponentTemporaryType:
                    return effect.ownerSide == UcgPlayerSide.Player
                        ? UcgEffectTargetType.OpponentCharacter
                        : UcgEffectTargetType.OwnCharacter;
                case UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp:
                case UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp:
                case UcgDemoEffectId.OnRevealSwapOwnCharacters:
                case UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp:
                case UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga:
                    return effect.ownerSide == UcgPlayerSide.Player
                        ? UcgEffectTargetType.OwnCharacter
                        : UcgEffectTargetType.OpponentCharacter;
                default:
                    return UcgEffectTargetType.None;
            }
        }

        public string GetTargetPrompt(UcgEffectInstance effect)
        {
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp)
            {
                return "登場效果階段｜請選擇相鄰的傑洛角色";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp)
            {
                return "登場效果階段｜請選擇我方一名傑洛角色";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealSwapOwnCharacters)
            {
                return "登場效果階段｜請選擇要交換位置的本角色";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp)
            {
                return "登場效果階段｜請選擇 BP 降低一階的我方角色";
            }
            if (effect != null && effect.effectId == UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga)
            {
                return "登場效果階段｜請選擇要替換最上層的迪卡";
            }

            UcgEffectTargetType targetType = GetTargetType(effect);
            string phaseText = effect != null && effect.timing == UcgEffectTiming.OnRevealOrEnter
                ? "登場效果階段"
                : "戰鬥效果階段";
            switch (targetType)
            {
                case UcgEffectTargetType.OwnLane:
                case UcgEffectTargetType.OwnCharacter:
                    return $"{phaseText}｜請選擇我方一條 Lane";
                case UcgEffectTargetType.OpponentLane:
                case UcgEffectTargetType.OpponentCharacter:
                    return $"{phaseText}｜請選擇對手一條 Lane";
                case UcgEffectTargetType.AnyLane:
                    return $"{phaseText}｜請選擇一條 Lane";
                default:
                    return $"{phaseText}｜請選擇效果目標";
            }
        }

        public bool ResolveTargetedEffect(UcgEffectInstance effect, UcgBattleLane targetLane, out string message)
        {
            return ResolveTargetedEffect(effect, targetLane, null, out message);
        }

        public bool ResolveTargetedEffect(UcgEffectInstance effect, UcgBattleLane targetLane, UcgHandDemo demo, out string message)
        {
            message = "效果目標不存在";
            if (effect == null || effect.cardData == null || targetLane == null) return false;

            if (!_effectQueue.Contains(effect))
            {
                message = "效果已不在隊列中";
                return false;
            }

            string sideText = effect.ownerSide == UcgPlayerSide.Player ? "我方" : "對手";
            string cardName = string.IsNullOrEmpty(effect.cardData.cardName) ? "角色" : effect.cardData.cardName;
            int laneNumber = targetLane.laneIndex + 1;
            if (!IsStackRequirementMet(effect, out int requiredStackCount, out int currentStackCount, out bool requireExactStackCount, out string stackMessage))
            {
                RemoveEffect(effect);
                message = "沒有可發動效果";
                return true;
            }

            switch (effect.effectId)
            {
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000:
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus2000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000:
                case UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus3000:
                case UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000:
                {
                    UcgPlayerSide targetSide = effect.ownerSide;
                    int amount = GetPlusAmount(effect.effectId);
                    AddBpModifier(targetLane, targetSide, amount, effect.cardData, "發動效果", requiredStackCount, currentStackCount, requireExactStackCount);
                    string targetText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
                    MarkActivatedEffectUsed(effect);
                    RemoveEffect(effect);
                    LogEffectResolved(effect, "ModifyBp", amount);
                    message = $"{(effect.timing == UcgEffectTiming.OnRevealOrEnter ? "登場效果階段" : "戰鬥效果階段")}｜{sideText} {cardName}：{targetText}第 {laneNumber} 路 BP +{amount}";
                    return true;
                }
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000:
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus2000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000:
                case UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000:
                case UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000:
                {
                    UcgPlayerSide targetSide = GetOpponentSide(effect.ownerSide);
                    int amount = -GetMinusAmount(effect.effectId);
                    AddBpModifier(targetLane, targetSide, amount, effect.cardData, "發動效果", requiredStackCount, currentStackCount, requireExactStackCount);
                    string targetText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
                    MarkActivatedEffectUsed(effect);
                    RemoveEffect(effect);
                    LogEffectResolved(effect, "ModifyBp", amount);
                    message = $"{(effect.timing == UcgEffectTiming.OnRevealOrEnter ? "登場效果階段" : "戰鬥效果階段")}｜{sideText} {cardName}：{targetText}第 {laneNumber} 路 BP {amount}";
                    return true;
                }
                case UcgDemoEffectId.OnRevealGrantOwnTemporaryType:
                case UcgDemoEffectId.OnRevealGrantOpponentTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOwnTemporaryType:
                case UcgDemoEffectId.ActivatedGrantOpponentTemporaryType:
                {
                    if (demo == null)
                    {
                        message = "效果目標處理器不存在";
                        return false;
                    }

                    UcgEffectRule rule = UcgEffectParser.ParsePrimaryRule(effect.cardData);
                    string grantedType = rule != null ? rule.grantedType : "";
                    if (string.IsNullOrWhiteSpace(grantedType))
                    {
                        message = "暫時 TYPE 效果缺少 TYPE 設定";
                        RemoveEffect(effect);
                        return true;
                    }

                    UcgPlayerSide targetSide = IsGrantOwnTemporaryTypeEffect(effect.effectId)
                        ? effect.ownerSide
                        : GetOpponentSide(effect.ownerSide);
                    string targetText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
                    bool granted = demo.TryGrantTemporaryTypeToLaneTarget(
                        targetLane,
                        targetSide,
                        grantedType,
                        effect.cardData,
                        out string targetName);
                    if (!granted)
                    {
                        message = "此處沒有可賦予 TYPE 的角色";
                        return false;
                    }

                    MarkActivatedEffectUsed(effect);
                    RemoveEffect(effect);
                    QueueBp02012AdjacentZeroStepUpIfNeeded(effect);
                    LogEffectResolved(effect, "GrantTemporaryType", 0);
                    message = $"{(effect.timing == UcgEffectTiming.OnRevealOrEnter ? "登場效果階段" : "戰鬥效果階段")}｜{sideText} {cardName}：{targetText}第 {laneNumber} 路 {targetName} 本回合獲得 TYPE {grantedType}";
                    return true;
                }
                case UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp:
                case UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp:
                {
                    UcgPlayerSide targetSide = effect.ownerSide;
                    UcgCardData targetCard = GetLaneTopCard(targetLane, targetSide);
                    if (targetCard == null)
                    {
                        message = "此處沒有可選擇的傑洛";
                        return false;
                    }

                    int targetStackCount = GetLaneStackCount(targetLane, targetSide);
                    int currentBp = targetCard.GetBpByStackCount(targetStackCount);
                    int stepToBp = UcgBattleJudge.GetNextBpStep(targetCard, currentBp);
                    int stepAmount = stepToBp - currentBp;
                    RemoveEffect(effect);

                    string targetSideText = targetSide == UcgPlayerSide.Player ? "我方" : "對手";
                    if (stepAmount <= 0)
                    {
                        message = $"登場效果階段｜{sideText} {cardName}：{targetSideText}第 {laneNumber} 路傑洛 BP 已無法再上升";
                        return true;
                    }

                    AddBpStepModifier(targetLane, targetSide, stepAmount, effect.cardData, "登場時效果", requiredStackCount, currentStackCount, requireExactStackCount, currentBp, stepToBp);
                    LogEffectResolved(effect, "BpStepUp", stepAmount);
                    message = $"登場效果階段｜{sideText} {cardName}：{targetSideText}第 {laneNumber} 路傑洛 BP 上升一階";
                    return true;
                }
                default:
                    message = "此效果不需要選擇目標";
                    return false;
            }
        }

        void QueueBp02012AdjacentZeroStepUpIfNeeded(UcgEffectInstance sourceEffect)
        {
            if (sourceEffect == null || sourceEffect.cardData == null || sourceEffect.lane == null) return;
            if (sourceEffect.cardData.id != "BP02-012") return;

            _effectQueue.Insert(0, new UcgEffectInstance
            {
                effectId = UcgDemoEffectId.OnRevealChooseAdjacentOwnZeroBpStepUp,
                cardData = sourceEffect.cardData,
                sourceCard = sourceEffect.sourceCard,
                lane = sourceEffect.lane,
                ownerSide = sourceEffect.ownerSide,
                timing = sourceEffect.timing,
                effectKey = $"{sourceEffect.effectKey}:adjacent-zero-stepup"
            });
        }

        static bool IsGrantOwnTemporaryTypeEffect(UcgDemoEffectId effectId)
        {
            return effectId == UcgDemoEffectId.OnRevealGrantOwnTemporaryType
                || effectId == UcgDemoEffectId.ActivatedGrantOwnTemporaryType;
        }

        UcgCardData GetLaneTopCard(UcgBattleLane lane, UcgPlayerSide side)
        {
            if (lane == null) return null;
            UcgCardView view = side == UcgPlayerSide.Player
                ? lane.playerPlayArea != null ? lane.playerPlayArea.GetTopCard() : lane.playerTopCard
                : lane.GetOpponentTopCard();
            return view != null ? view.CardData : null;
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

        public bool CanAutoResolveOpponentBattleEffect(UcgEffectInstance effect, out string skippedReason)
        {
            skippedReason = "";
            if (effect == null)
            {
                skippedReason = "unsupported pattern";
                return false;
            }

            if (effect.ownerSide != UcgPlayerSide.Opponent || effect.timing != UcgEffectTiming.Activated)
            {
                skippedReason = "not opponent battle effect";
                return false;
            }

            if (EffectNeedsTarget(effect))
            {
                skippedReason = "requires choice";
                return false;
            }

            if (EffectHasCostText(effect))
            {
                skippedReason = "requires cost";
                return false;
            }

            switch (effect.effectId)
            {
                case UcgDemoEffectId.ActivatedSelfBpPlus1000:
                case UcgDemoEffectId.ActivatedSelfBpPlus2000:
                case UcgDemoEffectId.ActivatedSelfBpPlus3000:
                case UcgDemoEffectId.ActivatedSelfBpStepUp:
                    return true;
                case UcgDemoEffectId.ActivatedDrawOne:
                case UcgDemoEffectId.ActivatedDrawTwo:
                    skippedReason = "unsupported pattern";
                    return false;
                default:
                    skippedReason = "unsupported pattern";
                    return false;
            }
        }

        public bool ResolveOpponentAutoBattleEffect(UcgEffectInstance effect, UcgHandDemo demo, out string message, out string skippedReason)
        {
            message = "沒有可發動效果";
            skippedReason = "";
            if (!CanAutoResolveOpponentBattleEffect(effect, out skippedReason)) return false;

            if (!IsStackRequirementMet(effect, out int requiredStackCount, out int currentStackCount, out bool requireExactStackCount, out _))
            {
                skippedReason = "stack requirement not met";
                return false;
            }

            if (effect.cardData == null || effect.lane == null)
            {
                skippedReason = "unsupported pattern";
                return false;
            }

            string cardName = string.IsNullOrEmpty(effect.cardData.cardName) ? "角色" : effect.cardData.cardName;
            int stepFromBp = 0;
            int stepToBp = 0;
            int amount = effect.effectId == UcgDemoEffectId.ActivatedSelfBpStepUp
                ? GetStepUpAmount(effect, out stepFromBp, out stepToBp)
                : GetPlusAmount(effect.effectId);
            if (effect.effectId == UcgDemoEffectId.ActivatedSelfBpStepUp)
            {
                if (currentStackCount <= 0) currentStackCount = GetEffectStackCount(effect);
                AddBpStepModifier(effect.lane, effect.ownerSide, amount, effect.cardData, "戰鬥效果", requiredStackCount, currentStackCount, requireExactStackCount, stepFromBp, stepToBp);
            }
            else
            {
                AddBpModifier(effect.lane, effect.ownerSide, amount, effect.cardData, "戰鬥效果", requiredStackCount, currentStackCount, requireExactStackCount);
            }
            MarkActivatedEffectUsed(effect);
            LogEffectResolved(effect, effect.effectId == UcgDemoEffectId.ActivatedSelfBpStepUp ? "BpStepUp" : "ModifyBp", amount);
            message = effect.effectId == UcgDemoEffectId.ActivatedSelfBpStepUp
                ? $"對手戰鬥效果發動｜{cardName}：BP 上升一階"
                : $"對手戰鬥效果發動｜{cardName}：BP +{amount}";
            return true;
        }

        int GetStepUpAmount(UcgEffectInstance effect, out int stepFromBp, out int stepToBp)
        {
            stepFromBp = 0;
            stepToBp = 0;
            if (effect == null || effect.cardData == null) return 0;
            stepFromBp = effect.cardData.GetBpByStackCount(GetEffectStackCount(effect));
            stepToBp = UcgBattleJudge.GetNextBpStep(effect.cardData, stepFromBp);
            return stepToBp - stepFromBp;
        }

        int GetEffectStackCount(UcgEffectInstance effect)
        {
            if (effect == null || effect.lane == null) return 1;
            if (effect.ownerSide == UcgPlayerSide.Player && effect.lane.playerPlayArea != null)
            {
                return effect.lane.playerPlayArea.GetStackCount();
            }

            return effect.lane.GetOpponentStackCount();
        }

        bool EffectHasCostText(UcgEffectInstance effect)
        {
            string text = effect != null && effect.cardData != null ? effect.cardData.effectDescription : "";
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Contains("棄牌")
                || text.Contains("支付")
                || text.Contains("成本")
                || text.Contains("代價")
                || text.Contains("作為費用");
        }

        public void RemoveEffect(UcgEffectInstance effect)
        {
            if (effect == null) return;
            _effectQueue.Remove(effect);
        }

        int GetPlusAmount(UcgDemoEffectId effectId)
        {
            if (effectId == UcgDemoEffectId.OnRevealSelfBpPlus3000
                || effectId == UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus3000
                || effectId == UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000
                || effectId == UcgDemoEffectId.ActivatedSelfBpPlus3000)
            {
                return 3000;
            }

            if (effectId == UcgDemoEffectId.OnRevealSelfBpPlus2000
                || effectId == UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus2000
                || effectId == UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000
                || effectId == UcgDemoEffectId.ActivatedSelfBpPlus2000)
            {
                return 2000;
            }

            return 1000;
        }

        int GetMinusAmount(UcgDemoEffectId effectId)
        {
            if (effectId == UcgDemoEffectId.OnRevealOpponentBpMinus3000
                || effectId == UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000
                || effectId == UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000)
            {
                return 3000;
            }

            if (effectId == UcgDemoEffectId.OnRevealOpponentBpMinus2000
                || effectId == UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus2000
                || effectId == UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000)
            {
                return 2000;
            }

            return 1000;
        }

        int GetDrawCount(UcgDemoEffectId effectId)
        {
            return effectId == UcgDemoEffectId.OnRevealDrawTwo
                || effectId == UcgDemoEffectId.ActivatedDrawTwo
                ? 2
                : 1;
        }

        void LogEffectResolved(UcgEffectInstance effect, string action, int value)
        {
            if (effect == null || effect.cardData == null) return;

            Debug.Log(
                "Effect resolved:\n"
                + $"source={effect.cardData.id} {effect.cardData.cardName}\n"
                + $"category={GetEffectCategoryText(effect.cardData)}\n"
                + $"action={action}\n"
                + $"value={value}");
        }

        int GetSideOrder(UcgPlayerSide side, UcgPlayerSide firstPlayer)
        {
            return side == firstPlayer ? 0 : 1;
        }

        UcgPlayerSide GetOpponentSide(UcgPlayerSide side)
        {
            return side == UcgPlayerSide.Player ? UcgPlayerSide.Opponent : UcgPlayerSide.Player;
        }

        bool IsStackRequirementMet(
            UcgEffectInstance effect,
            out int requiredStackCount,
            out int currentStackCount,
            out bool requireExactStackCount,
            out string message)
        {
            requiredStackCount = 0;
            currentStackCount = 0;
            requireExactStackCount = false;
            message = "";
            if (effect == null || effect.cardData == null || effect.cardData.IsSceneCard()) return true;

            UcgEffectRule rule = UcgEffectParser.ParsePrimaryRule(effect.cardData);
            requiredStackCount = rule != null ? rule.requiredStackCount : 0;
            requireExactStackCount = rule != null && rule.requireExactStackCount;
            currentStackCount = effect.ownerSide == UcgPlayerSide.Player && effect.lane != null && effect.lane.playerPlayArea != null
                ? effect.lane.playerPlayArea.GetStackCount()
                : effect.lane != null ? effect.lane.GetOpponentStackCount() : 0;
            if (rule != null && rule.allowedStackCounts != null && rule.allowedStackCounts.Count > 0)
            {
                bool allowedStack = rule.allowedStackCounts.Contains(currentStackCount);
                if (allowedStack) return true;

                message = "stack requirement not met";
                if (UcgBattleJudge.debugBpBreakdown)
                {
                    Debug.Log(
                        "Effect skipped:\n"
                        + $"card={effect.cardData.id} {effect.cardData.cardName}\n"
                        + $"category={GetEffectCategoryText(effect.cardData)}\n"
                        + $"allowedStackCounts={string.Join(",", rule.allowedStackCounts)}\n"
                        + $"currentStackCount={currentStackCount}\n"
                        + "reason=Stack count not allowed");
                }
                return false;
            }

            if (requiredStackCount <= 0) return true;

            bool met = requireExactStackCount
                ? currentStackCount == requiredStackCount
                : currentStackCount >= requiredStackCount;
            if (met) return true;

            message = "stack requirement not met";
            if (UcgBattleJudge.debugBpBreakdown)
            {
                Debug.Log(
                    "Effect skipped:\n"
                    + $"card={effect.cardData.id} {effect.cardData.cardName}\n"
                    + $"category={GetEffectCategoryText(effect.cardData)}\n"
                    + $"requiredStackCount={requiredStackCount}\n"
                    + $"currentStackCount={currentStackCount}\n"
                    + "reason=Stack count insufficient");
            }
            return false;
        }

        bool IsAdditionalConditionMet(UcgEffectInstance effect, UcgHandDemo demo, out string message)
        {
            message = "";
            if (effect == null || effect.cardData == null) return true;

            UcgEffectRule rule = UcgEffectParser.ParsePrimaryRule(effect.cardData);
            if (rule == null || rule.condition == null) return true;

            if (demo == null)
            {
                message = string.IsNullOrWhiteSpace(rule.condition.failureMessage)
                    ? "條件未達成，效果不發動。"
                    : rule.condition.failureMessage;
                return false;
            }

            bool met = demo.IsEffectConditionMet(effect, rule.condition, out message);
            if (met) return true;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.IsNullOrWhiteSpace(rule.condition.failureMessage)
                    ? "條件未達成，效果不發動。"
                    : rule.condition.failureMessage;
            }
            return false;
        }

        void AddBpModifier(UcgBattleLane lane, UcgPlayerSide side, int amount, UcgCardData sourceCard, string reason, int requiredStackCount, int currentStackCount, bool requireExactStackCount)
        {
            if (lane == null) return;
            lane.AddTemporaryBpModifier(side, amount, sourceCard, reason, requiredStackCount, currentStackCount, requireExactStackCount);
        }

        void AddBpStepModifier(UcgBattleLane lane, UcgPlayerSide side, int amount, UcgCardData sourceCard, string reason, int requiredStackCount, int currentStackCount, bool requireExactStackCount, int stepFromBp, int stepToBp)
        {
            if (lane == null) return;
            lane.AddTemporaryBpModifier(side, amount, sourceCard, reason, requiredStackCount, currentStackCount, requireExactStackCount, true, stepFromBp, stepToBp);
        }

        void AddBpModifier(UcgBattleLane lane, UcgPlayerSide side, int amount, UcgCardData sourceCard, string reason)
        {
            if (lane == null) return;
            UcgEffectParser.ParseStackRequirement(
                sourceCard != null ? sourceCard.effectDescription : "",
                out int requiredStackCount,
                out bool requireExactStackCount);
            int currentStackCount = sourceCard != null && sourceCard.IsSceneCard()
                ? 0
                : side == UcgPlayerSide.Player && lane.playerPlayArea != null
                    ? lane.playerPlayArea.GetStackCount()
                    : lane.GetOpponentStackCount();
            lane.AddTemporaryBpModifier(side, amount, sourceCard, reason, requiredStackCount, currentStackCount, requireExactStackCount);
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
    }
}
