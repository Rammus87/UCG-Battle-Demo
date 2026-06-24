using UnityEngine;

namespace UCG
{
    public static class UcgEffectRegistry
    {
        public static bool TryApplyDemoMapping(UcgCardData card, UcgEffectRule rule, string effectText, out string unsupportedReason)
        {
            unsupportedReason = "";
            if (card == null || rule == null || !rule.supported) return false;

            return card.IsSceneCard()
                ? TryApplySceneMapping(card, rule, out unsupportedReason)
                : TryApplyCharacterMapping(card, rule, effectText, out unsupportedReason);
        }

        static bool TryApplyCharacterMapping(UcgCardData card, UcgEffectRule rule, string effectText, out string unsupportedReason)
        {
            unsupportedReason = "";
            card.effectTiming = ToTiming(rule.trigger);

            if (rule.actionType == UcgEffectActionType.DeckOperation && rule.trigger == UcgEffectTrigger.OnRevealOrEnter)
            {
                card.effectId = UcgDemoEffectId.OnRevealDeckOperation;
                return true;
            }

            if (rule.actionType == UcgEffectActionType.DeckOperation && rule.trigger == UcgEffectTrigger.Activated)
            {
                card.effectId = UcgDemoEffectId.ActivatedDeckOperation;
                return true;
            }

            if (rule.actionType == UcgEffectActionType.DrawCards && rule.trigger == UcgEffectTrigger.OnRevealOrEnter)
            {
                card.effectId = rule.drawCount >= 2 ? UcgDemoEffectId.OnRevealDrawTwo : UcgDemoEffectId.OnRevealDrawOne;
                return true;
            }

            if (rule.actionType == UcgEffectActionType.DrawCards && rule.trigger == UcgEffectTrigger.Activated)
            {
                card.effectId = rule.drawCount >= 2 ? UcgDemoEffectId.ActivatedDrawTwo : UcgDemoEffectId.ActivatedDrawOne;
                return true;
            }

            if (rule.actionType != UcgEffectActionType.ModifyBp)
            {
                unsupportedReason = $"unsupported character action: {rule.actionType}";
                return false;
            }

            if (rule.bpStepUp)
            {
                if (rule.trigger == UcgEffectTrigger.Activated && !ContainsOpponentText(effectText) && !ContainsChoiceText(effectText))
                {
                    card.effectId = UcgDemoEffectId.ActivatedSelfBpStepUp;
                    return true;
                }

                unsupportedReason = $"unsupported executable BP step-up mapping: trigger={rule.trigger}";
                return false;
            }

            int absoluteBp = Mathf.Abs(rule.bpAmount);
            if (rule.trigger == UcgEffectTrigger.OnRevealOrEnter && IsSupportedBpAmount(absoluteBp))
            {
                if (ContainsOpponentText(effectText) || rule.bpAmount < 0)
                {
                    card.effectId = ContainsTargetCharacterText(effectText)
                        ? GetOnRevealChooseOpponentMinusEffect(absoluteBp)
                        : GetOnRevealOpponentMinusEffect(absoluteBp);
                }
                else
                {
                    card.effectId = GetOnRevealSelfPlusEffect(absoluteBp);
                }
                return true;
            }

            if (rule.trigger == UcgEffectTrigger.Activated && IsSupportedBpAmount(absoluteBp))
            {
                if (ContainsOpponentText(effectText) || rule.bpAmount < 0)
                {
                    card.effectId = GetActivatedOpponentMinusEffect(absoluteBp);
                }
                else if (!ContainsChoiceText(effectText))
                {
                    card.effectId = GetActivatedSelfPlusEffect(absoluteBp);
                }
                else
                {
                    card.effectId = GetActivatedOwnPlusEffect(absoluteBp);
                }
                return true;
            }

            unsupportedReason = $"unsupported executable character BP mapping: trigger={rule.trigger}, bp={rule.bpAmount}";
            return false;
        }

        static bool TryApplySceneMapping(UcgCardData card, UcgEffectRule rule, out string unsupportedReason)
        {
            unsupportedReason = "";
            card.sceneEffectTiming = ToTiming(rule.trigger);

            if (rule.actionType == UcgEffectActionType.ModifyBp
                && rule.trigger == UcgEffectTrigger.Continuous
                && (rule.bpAmount == 500 || rule.bpAmount == 1000 || rule.bpAmount == 2000 || rule.bpAmount == 3000))
            {
                card.sceneEffectId = GetContinuousSceneBpEffect(rule.bpAmount);
                return true;
            }

            if (rule.actionType == UcgEffectActionType.ModifyBp
                && rule.trigger == UcgEffectTrigger.Activated
                && rule.bpAmount == 1000)
            {
                card.sceneEffectId = UcgDemoSceneEffectId.ActivatedChooseOwnLaneBpPlus1000;
                return true;
            }

            if (rule.actionType == UcgEffectActionType.DrawCards
                && rule.trigger == UcgEffectTrigger.OnRevealOrEnter)
            {
                if (rule.drawCount == 1)
                {
                    card.sceneEffectId = UcgDemoSceneEffectId.OnEnterDrawOne;
                    return true;
                }

                unsupportedReason = $"unsupported scene draw count: {rule.drawCount}";
                return false;
            }

            unsupportedReason = $"unsupported executable scene mapping: trigger={rule.trigger}, action={rule.actionType}, bp={rule.bpAmount}";
            return false;
        }

        static UcgEffectTiming ToTiming(UcgEffectTrigger trigger)
        {
            switch (trigger)
            {
                case UcgEffectTrigger.OnRevealOrEnter:
                    return UcgEffectTiming.OnRevealOrEnter;
                case UcgEffectTrigger.Activated:
                    return UcgEffectTiming.Activated;
                case UcgEffectTrigger.Continuous:
                    return UcgEffectTiming.Continuous;
                default:
                    return UcgEffectTiming.None;
            }
        }

        static bool ContainsOpponentText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Contains("對手") || text.Contains("敵方");
        }

        static bool ContainsChoiceText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Contains("選擇") || text.Contains("選1") || text.Contains("選一") || text.Contains("選出");
        }

        static bool ContainsTargetCharacterText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return ContainsChoiceText(text)
                || text.Contains("指定")
                || text.Contains("一名角色")
                || text.Contains("1名角色")
                || text.Contains("一個角色")
                || text.Contains("1個角色")
                || text.Contains("一體角色")
                || text.Contains("1體角色");
        }

        static bool IsSupportedBpAmount(int amount)
        {
            return amount == 1000 || amount == 2000 || amount == 3000;
        }

        static UcgDemoEffectId GetOnRevealSelfPlusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.OnRevealSelfBpPlus3000;
            if (amount == 2000) return UcgDemoEffectId.OnRevealSelfBpPlus2000;
            return UcgDemoEffectId.OnRevealSelfBpPlus1000;
        }

        static UcgDemoEffectId GetOnRevealOpponentMinusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.OnRevealOpponentBpMinus3000;
            if (amount == 2000) return UcgDemoEffectId.OnRevealOpponentBpMinus2000;
            return UcgDemoEffectId.OnRevealOpponentBpMinus1000;
        }

        static UcgDemoEffectId GetOnRevealChooseOpponentMinusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000;
            if (amount == 2000) return UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus2000;
            return UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000;
        }

        static UcgDemoEffectId GetActivatedOwnPlusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus3000;
            if (amount == 2000) return UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus2000;
            return UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000;
        }

        static UcgDemoEffectId GetActivatedSelfPlusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.ActivatedSelfBpPlus3000;
            if (amount == 2000) return UcgDemoEffectId.ActivatedSelfBpPlus2000;
            return UcgDemoEffectId.ActivatedSelfBpPlus1000;
        }

        static UcgDemoEffectId GetActivatedOpponentMinusEffect(int amount)
        {
            if (amount == 3000) return UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus3000;
            if (amount == 2000) return UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus2000;
            return UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000;
        }

        static UcgDemoSceneEffectId GetContinuousSceneBpEffect(int amount)
        {
            if (amount == 3000) return UcgDemoSceneEffectId.PlayerAllBpPlus3000;
            if (amount == 2000) return UcgDemoSceneEffectId.PlayerAllBpPlus2000;
            if (amount == 1000) return UcgDemoSceneEffectId.PlayerAllBpPlus1000;
            return UcgDemoSceneEffectId.PlayerAllBpPlus500;
        }
    }
}
