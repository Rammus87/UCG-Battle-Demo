using System.Collections.Generic;

namespace UCG
{
    public enum UcgTutorialCardEffectSupport
    {
        Supported,
        Partial,
        Unsupported
    }

    public sealed class UcgTutorialCardEffectMapping
    {
        public string cardId;
        public UcgTutorialCardEffectSupport support;
        public string note;
        public UcgEffectTiming effectTiming;
        public UcgDemoEffectId effectId;
        public UcgEffectTiming sceneEffectTiming;
        public UcgDemoSceneEffectId sceneEffectId;
        public UcgEffectRule primaryRule;
        public UcgConditionalBpRule conditionalRule;
        public UcgEffectTargetFilter targetFilter;
        public UcgCardPlayRestriction playRestriction;
    }

    public sealed class UcgCardPlayRestriction
    {
        public bool forbidSinglePlay;
        public bool forbidSingleState;
        public string singleRestrictionMessage;
    }

    public enum UcgEffectRelativeTargetSide
    {
        None,
        Self,
        Opponent
    }

    public sealed class UcgEffectTargetFilter
    {
        public UcgEffectRelativeTargetSide targetSide;
        public int targetCount = 1;
        public List<string> targetAllowedTypes;
        public List<string> targetCharacterNames;
        public string targetCharacterNameContains;
        public List<string> opposingLaneOwnerCharacterNames;
    }

    public static class UcgTutorialCardEffectMap
    {
        static readonly Dictionary<string, UcgTutorialCardEffectMapping> Mappings =
            new Dictionary<string, UcgTutorialCardEffectMapping>
            {
                // Player Diga deck
                { "BP01-001", DeckOperation("BP01-001", "登場，公開 3 張，選 1 張超人力霸王加入手牌，其餘棄牌。", RevealSelectRule(3, UcgDeckSelectionFilter.UltramanCard)) },
                { "BP05-001", Conditional("BP05-001", "對手 TYPE 力量或毀滅時 BP 上升一級。", OpponentTypeStepUpRule("力量", "毀滅")) },
                { "SD01-005", Conditional("SD01-005", "對手 TYPE 力量時 BP 上升一級。", OpponentTypeStepUpRule("力量")) },
                { "BP01-004", DeckOperation("BP01-004", "登場，公開 5 張，選 1 張場景加入手牌，其餘棄牌。", RevealSelectRule(5, UcgDeckSelectionFilter.SceneCard)) },
                { "BP05-002", DeckOperation("BP05-002", "登場，最多 2 張手牌回到牌庫底，然後抽同等數量。", HandToBottomThenDrawSameCountRule(2)) },
                { "BP05-003", CharacterEffect("BP05-003", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000, "登場，選擇我方 TYPE 基礎角色 BP+1000。", TargetedBpRule(UcgEffectTrigger.OnRevealOrEnter, 1000), OwnTargetTypes("基礎")) },
                { "BP01-008", CharacterEffect("BP01-008", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus1000, "登場，選擇對手角色 BP-1000。", TargetedBpRule(UcgEffectTrigger.OnRevealOrEnter, -1000)) },
                { "BP01-007", CharacterEffect("BP01-007", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealSwapOwnCharacters, "登場，可將本角色與我方另一名超人力霸王交換位置。", SwapOwnCharactersRule(UcgEffectTrigger.OnRevealOrEnter), OwnTargetAny(2)) },
                { "BP05-005", CharacterEffect("BP05-005", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealOwnDoubleTripleStepDownThenDigaStepUp, "DBL/TRP 登場，選擇我方 DOUBLE/TRIPLE 且等級 2 或 3 的超人力霸王 BP 降低一級；若如此做，再選擇我方迪卡 BP 上升一級。", StepDownThenStepUpRule(UcgEffectTrigger.OnRevealOrEnter, 2, 3), OwnTargetAny(2)) },
                { "BP05-008", CharacterEffect("BP05-008", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealSwapTopWithDiscardDiga, "QUAD 登場，選擇我方 DOUBLE/TRIPLE 且等級 2 或 3 的迪卡，最上層回手牌；再從棄牌區手動選擇一張同等級迪卡登場到原位置。", SwapTopWithDiscardRule(UcgEffectTrigger.OnRevealOrEnter, 4), OwnTargetAny()) },
                { "BP01-037", DeckOperation("BP01-037", "登場，抽 2 張，從手牌選 1 張回牌庫底。", DrawThenPutHandToBottomRule(2, 1)) },
                { "BP01-006", NoEffect("BP01-006", "無效果。") },
                { "BP01-043", CharacterEffect("BP01-043", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOpponentLaneBpMinus3000, "登場公開上方最多 3 張，玩家依序指定放回牌庫上方的順序；之後選擇一名對手角色 BP-3000。對手流程以原順序自動放回。", RevealTopReorderThenTargetedBpRule(UcgEffectTrigger.OnRevealOrEnter, 3, -3000), OpponentTargetAny()) },
                { "BP01-105", SceneEffect("BP01-105", UcgEffectTiming.Activated, UcgDemoSceneEffectId.ActivatedUpgradeFromDeckThenDiscardAtEnd, "場景發動，若我方有布雷薩在場，公開 5 張；玩家從可合法升級的公開卡中選 1 張並指定我方 Lane 登場，其餘棄牌；回合結束棄置該卡。", SceneUpgradeFromDeckRule(5, OwnConditionNameContains("布雷薩"))) },

                // Opponent Zero deck
                { "BP01-055", DeckOperation("BP01-055", "登場，抽 2 張，從手牌選 1 張回牌庫底。", DrawThenPutHandToBottomRule(2, 1)) },
                { "BP03-031", Conditional("BP03-031", "我方場上每有 1 名傑洛，此卡 BP+1000。", OwnCharacterNameCountFixedBpRule(1000, "傑洛")) },
                { "BP02-009", Conditional("BP02-009", "對手 TYPE 基礎或武裝時 BP+1000。", OpponentTypeFixedBpRule(1000, "基礎", "武裝")) },
                { "BP03-032", CharacterEffect("BP03-032", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus1000, "登場，選擇我方 TYPE 力量或武裝角色 BP+1000。", TargetedBpRule(UcgEffectTrigger.OnRevealOrEnter, 1000), OwnTargetTypes("力量", "武裝")) },
                { "BP03-033", Conditional("BP03-033", "對手 TYPE 武裝時 BP 上升一級。", OpponentTypeStepUpRule("武裝")) },
                { "SD02-005", Conditional("SD02-005", "對手 TYPE 武裝時 BP 上升一級。", OpponentTypeStepUpRule("武裝")) },
                { "BP05-038", CharacterEffect("BP05-038", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealDrawOne, "[DBL] 登場，若我方有 TYPE 敏捷的傑洛在場，抽 1 張。", ConditionalDrawRule(1, 2, OwnConditionTypesAndName("敏捷", "傑洛"))) },
                { "BP05-059", CharacterEffect("BP05-059", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealGrantOwnTemporaryType, "登場，選擇我方一名角色，本回合賦予 TYPE 力量。", TemporaryTypeRule(UcgEffectTrigger.OnRevealOrEnter, "力量"), OwnTargetAny()) },
                { "BP01-061", Conditional("BP01-061", "對手 TYPE 力量或武裝時 BP 上升一級。", OpponentTypeStepUpRule("力量", "武裝")) },
                { "BP02-012", CharacterEffect("BP02-012", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealGrantOpponentTemporaryType, "登場，選擇對手角色，本回合賦予 TYPE 武裝；再選與本角色相鄰的我方傑洛 BP 上升一級。", TemporaryTypeRule(UcgEffectTrigger.OnRevealOrEnter, "武裝"), OpponentTargetAny()) },
                { "BP01-062", CharacterEffect("BP01-062", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOwnLaneBpPlus2000, "登場，選擇我方傑洛 BP+2000。", TargetedBpRule(UcgEffectTrigger.OnRevealOrEnter, 2000), OwnTargetCharacterNameContains("傑洛")) },
                { "BP05-044", WithPlayRestriction(CharacterEffect("BP05-044", UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealChooseOwnZeroBpStepUp, "QUAD 登場，選擇我方傑洛 BP 上升一級；此卡不能以 SINGLE 狀態登場。", TargetedBpStepUpRule(UcgEffectTrigger.OnRevealOrEnter, 4), OwnTargetCharacterNameContains("傑洛")), ForbidSingleState("BP05-044 不能以 SINGLE 狀態登場。")) },
                { "SD02-014", SceneEffect("SD02-014", UcgEffectTiming.Activated, UcgDemoSceneEffectId.ActivatedGrantOpponentTemporaryType, "場景發動，選擇對手角色，本回合賦予 TYPE 武裝；目標必須是我方傑洛/捷德/傑特的對戰對手。", TemporaryTypeRule(UcgEffectTrigger.Activated, "武裝"), OpponentTargetFacingOwnerCharacters("傑洛", "捷德", "傑特")) }
            };

        public static bool TryGetMapping(string cardId, out UcgTutorialCardEffectMapping mapping)
        {
            mapping = null;
            if (string.IsNullOrWhiteSpace(cardId)) return false;
            return Mappings.TryGetValue(cardId, out mapping);
        }

        public static bool HasMapping(UcgCardData card)
        {
            return card != null && Mappings.ContainsKey(card.id);
        }

        public static bool TryApplyExecutableMapping(UcgCardData card, out UcgTutorialCardEffectMapping mapping)
        {
            mapping = null;
            if (card == null || !TryGetMapping(card.id, out mapping)) return false;

            card.effectTiming = mapping.effectTiming;
            card.effectId = mapping.effectId;
            card.sceneEffectTiming = mapping.sceneEffectTiming;
            card.sceneEffectId = mapping.sceneEffectId;
            return true;
        }

        public static bool TryGetPrimaryRule(UcgCardData card, out UcgEffectRule rule)
        {
            rule = null;
            if (card == null || !TryGetMapping(card.id, out UcgTutorialCardEffectMapping mapping)) return false;

            if (mapping.primaryRule != null)
            {
                rule = CloneRule(mapping.primaryRule);
                rule.rawText = GetEffectText(card);
                return true;
            }

            rule = UnsupportedPrimaryRule(GetEffectText(card), mapping.note);
            return true;
        }

        public static bool TryGetConditionalRule(UcgCardData card, out UcgConditionalBpRule rule)
        {
            rule = null;
            if (card == null || !TryGetMapping(card.id, out UcgTutorialCardEffectMapping mapping)) return false;

            if (mapping.conditionalRule != null)
            {
                rule = CloneConditionalRule(mapping.conditionalRule);
                return true;
            }

            rule = new UcgConditionalBpRule
            {
                supported = false,
                category = UcgConditionalBpCategory.Unsupported,
                unsupportedReason = "explicit tutorial card mapping has no conditional BP effect"
            };
            return true;
        }

        public static bool TryGetTargetFilter(UcgCardData card, out UcgEffectTargetFilter filter)
        {
            filter = null;
            if (card == null || !TryGetMapping(card.id, out UcgTutorialCardEffectMapping mapping)) return false;
            if (mapping.targetFilter == null) return false;

            filter = CloneTargetFilter(mapping.targetFilter);
            return true;
        }

        public static bool TryGetPlayRestriction(UcgCardData card, out UcgCardPlayRestriction restriction)
        {
            restriction = null;
            if (card == null || !TryGetMapping(card.id, out UcgTutorialCardEffectMapping mapping)) return false;
            if (mapping.playRestriction == null) return false;

            restriction = ClonePlayRestriction(mapping.playRestriction);
            return true;
        }

        public static bool ForbidsSingleState(UcgCardData card, out string message)
        {
            message = "";
            if (!TryGetPlayRestriction(card, out UcgCardPlayRestriction restriction)) return false;
            if (!restriction.forbidSinglePlay && !restriction.forbidSingleState) return false;

            message = string.IsNullOrWhiteSpace(restriction.singleRestrictionMessage)
                ? "這張卡不能以 SINGLE 狀態登場。"
                : restriction.singleRestrictionMessage;
            return true;
        }

        static UcgTutorialCardEffectMapping CharacterEffect(
            string cardId,
            UcgEffectTiming timing,
            UcgDemoEffectId effectId,
            string note,
            UcgEffectRule rule,
            UcgEffectTargetFilter targetFilter = null)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Supported,
                note = note,
                effectTiming = timing,
                effectId = effectId,
                primaryRule = rule,
                targetFilter = targetFilter
            };
        }

        static UcgTutorialCardEffectMapping PartialCharacterEffect(
            string cardId,
            UcgEffectTiming timing,
            UcgDemoEffectId effectId,
            string note,
            UcgEffectRule rule,
            UcgEffectTargetFilter targetFilter = null)
        {
            UcgTutorialCardEffectMapping mapping = CharacterEffect(cardId, timing, effectId, note, rule, targetFilter);
            mapping.support = UcgTutorialCardEffectSupport.Partial;
            return mapping;
        }

        static UcgTutorialCardEffectMapping WithPlayRestriction(
            UcgTutorialCardEffectMapping mapping,
            UcgCardPlayRestriction restriction)
        {
            if (mapping != null)
            {
                mapping.playRestriction = restriction;
            }

            return mapping;
        }

        static UcgCardPlayRestriction ForbidSingleState(string message)
        {
            return new UcgCardPlayRestriction
            {
                forbidSinglePlay = true,
                forbidSingleState = true,
                singleRestrictionMessage = message
            };
        }

        static UcgTutorialCardEffectMapping PartialSceneEffect(
            string cardId,
            UcgEffectTiming timing,
            UcgDemoSceneEffectId sceneEffectId,
            string note,
            UcgEffectRule rule,
            UcgEffectTargetFilter targetFilter = null)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Partial,
                note = note,
                sceneEffectTiming = timing,
                sceneEffectId = sceneEffectId,
                primaryRule = rule,
                targetFilter = targetFilter
            };
        }

        static UcgTutorialCardEffectMapping SceneEffect(
            string cardId,
            UcgEffectTiming timing,
            UcgDemoSceneEffectId sceneEffectId,
            string note,
            UcgEffectRule rule,
            UcgEffectTargetFilter targetFilter = null)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Supported,
                note = note,
                sceneEffectTiming = timing,
                sceneEffectId = sceneEffectId,
                primaryRule = rule,
                targetFilter = targetFilter
            };
        }

        static UcgTutorialCardEffectMapping DeckOperation(string cardId, string note, UcgEffectRule rule)
        {
            return CharacterEffect(cardId, UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealDeckOperation, note, rule);
        }

        static UcgTutorialCardEffectMapping Conditional(string cardId, string note, UcgConditionalBpRule conditionalRule)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Supported,
                note = note,
                effectTiming = UcgEffectTiming.Continuous,
                effectId = UcgDemoEffectId.None,
                primaryRule = ContinuousBpRule(conditionalRule),
                conditionalRule = conditionalRule
            };
        }

        static UcgTutorialCardEffectMapping NoEffect(string cardId, string note)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Supported,
                note = note,
                primaryRule = new UcgEffectRule
                {
                    supported = true,
                    effectCategory = UcgEffectCategory.None,
                    trigger = UcgEffectTrigger.None,
                    actionType = UcgEffectActionType.None
                }
            };
        }

        static UcgTutorialCardEffectMapping Partial(string cardId, string note)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Partial,
                note = note,
                primaryRule = UnsupportedPrimaryRule("", note)
            };
        }

        static UcgTutorialCardEffectMapping Unsupported(string cardId, string note)
        {
            return new UcgTutorialCardEffectMapping
            {
                cardId = cardId,
                support = UcgTutorialCardEffectSupport.Unsupported,
                note = note,
                primaryRule = UnsupportedPrimaryRule("", note)
            };
        }

        static UcgEffectRule RevealSelectRule(int revealCount, UcgDeckSelectionFilter filter)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.EnterEffect,
                trigger = UcgEffectTrigger.OnRevealOrEnter,
                actionType = UcgEffectActionType.DeckOperation,
                deckOperation = new UcgDeckOperationRule
                {
                    operationType = UcgDeckOperationType.RevealTopSelectToHandRestTrash,
                    revealCount = revealCount,
                    selectCount = 1,
                    selectionFilter = filter,
                    selectedDestination = UcgDeckOperationDestination.Hand,
                    restDestination = UcgDeckOperationDestination.Trash,
                    sendAllToRestDestinationIfNoValidSelection = true,
                    requiresPlayerSelection = true
                }
            };
        }

        static UcgEffectRule DrawThenPutHandToBottomRule(int drawCount, int handSelectCount)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.EnterEffect,
                trigger = UcgEffectTrigger.OnRevealOrEnter,
                actionType = UcgEffectActionType.DeckOperation,
                deckOperation = new UcgDeckOperationRule
                {
                    operationType = UcgDeckOperationType.DrawThenPutHandToBottom,
                    drawCount = drawCount,
                    selectCount = handSelectCount,
                    minSelectCount = handSelectCount,
                    handSelectCount = handSelectCount,
                    minHandSelectCount = handSelectCount,
                    handSelectionFilter = UcgDeckSelectionFilter.Any,
                    selectedHandCardDestination = UcgDeckOperationDestination.BottomOfDeck,
                    requiresPlayerSelection = true
                }
            };
        }

        static UcgEffectRule HandToBottomThenDrawSameCountRule(int maxHandSelectCount)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.EnterEffect,
                trigger = UcgEffectTrigger.OnRevealOrEnter,
                actionType = UcgEffectActionType.DeckOperation,
                deckOperation = new UcgDeckOperationRule
                {
                    operationType = UcgDeckOperationType.SelectHandToBottomThenDrawSameCount,
                    selectCount = maxHandSelectCount,
                    minSelectCount = 0,
                    handSelectCount = maxHandSelectCount,
                    minHandSelectCount = 0,
                    handSelectionFilter = UcgDeckSelectionFilter.Any,
                    selectedHandCardDestination = UcgDeckOperationDestination.BottomOfDeck,
                    requiresPlayerSelection = true
                }
            };
        }

        static UcgEffectRule TargetedBpRule(UcgEffectTrigger trigger, int bpAmount)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.ModifyBp,
                duration = UcgEffectDuration.UntilEndOfTurn,
                bpAmount = bpAmount
            };
        }

        static UcgEffectRule TargetedBpStepUpRule(UcgEffectTrigger trigger, int requiredStackCount)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.ModifyBp,
                duration = UcgEffectDuration.UntilEndOfTurn,
                bpStepUp = true,
                requiredStackCount = requiredStackCount,
                requireExactStackCount = false
            };
        }

        static UcgEffectRule TemporaryTypeRule(UcgEffectTrigger trigger, string grantedType)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.GrantTemporaryType,
                duration = UcgEffectDuration.UntilEndOfTurn,
                grantedType = grantedType
            };
        }

        static UcgEffectRule SwapOwnCharactersRule(UcgEffectTrigger trigger)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.SwapOwnCharacters,
                duration = UcgEffectDuration.None
            };
        }

        static UcgEffectRule StepDownThenStepUpRule(UcgEffectTrigger trigger, params int[] allowedStackCounts)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.StepDownThenStepUp,
                duration = UcgEffectDuration.UntilEndOfTurn,
                allowedStackCounts = BuildAllowedStackCounts(allowedStackCounts)
            };
        }

        static UcgEffectRule SwapTopWithDiscardRule(UcgEffectTrigger trigger, int requiredStackCount)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = trigger == UcgEffectTrigger.OnRevealOrEnter
                    ? UcgEffectCategory.EnterEffect
                    : UcgEffectCategory.BattleEffect,
                trigger = trigger,
                actionType = UcgEffectActionType.SwapTopWithDiscard,
                duration = UcgEffectDuration.None,
                selectionSourceZone = UcgDeckOperationSourceZone.DiscardPile,
                requiredStackCount = requiredStackCount,
                requireExactStackCount = false
            };
        }

        static UcgEffectRule SceneUpgradeFromDeckRule(int revealCount, UcgEffectConditionRule condition)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.BattleEffect,
                trigger = UcgEffectTrigger.Activated,
                actionType = UcgEffectActionType.SceneUpgradeFromDeck,
                duration = UcgEffectDuration.UntilEndOfTurn,
                deckOperation = new UcgDeckOperationRule
                {
                    revealCount = revealCount,
                    selectCount = 1,
                    selectionFilter = UcgDeckSelectionFilter.UltramanCard,
                    restDestination = UcgDeckOperationDestination.Trash,
                    requiresPlayerSelection = false
                },
                condition = condition
            };
        }

        static UcgEffectRule RevealTopReorderThenTargetedBpRule(UcgEffectTrigger trigger, int revealCount, int bpAmount)
        {
            UcgEffectRule rule = TargetedBpRule(trigger, bpAmount);
            rule.reorderSourceZone = UcgDeckOperationSourceZone.TopDeckReorder;
            rule.reorderTopDeckCount = revealCount;
            return rule;
        }

        static UcgEffectRule ConditionalDrawRule(int drawCount, int requiredStackCount, UcgEffectConditionRule condition)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.EnterEffect,
                trigger = UcgEffectTrigger.OnRevealOrEnter,
                actionType = UcgEffectActionType.DrawCards,
                drawCount = drawCount,
                requiredStackCount = requiredStackCount,
                requireExactStackCount = false,
                condition = condition
            };
        }

        static UcgEffectRule ContinuousBpRule(UcgConditionalBpRule conditionalRule)
        {
            return new UcgEffectRule
            {
                supported = true,
                effectCategory = UcgEffectCategory.ContinuousEffect,
                trigger = UcgEffectTrigger.Continuous,
                actionType = UcgEffectActionType.ModifyBp,
                bpAmount = conditionalRule != null ? conditionalRule.bpAmount : 0,
                bpStepUp = conditionalRule != null && conditionalRule.isStepUp,
                requiredStackCount = conditionalRule != null ? conditionalRule.requiredStackCount : 0,
                requireExactStackCount = conditionalRule != null && conditionalRule.requireExactStackCount
            };
        }

        static UcgConditionalBpRule OpponentTypeStepUpRule(params string[] typeKeywords)
        {
            return new UcgConditionalBpRule
            {
                supported = true,
                category = UcgConditionalBpCategory.ParsedOpponentTypeCondition,
                bpAmount = 0,
                isStepUp = true,
                keyword = GetPrimaryKeyword(typeKeywords),
                allowedTypes = BuildAllowedTypes(typeKeywords)
            };
        }

        static UcgConditionalBpRule OpponentTypeFixedBpRule(int bpAmount, params string[] typeKeywords)
        {
            return new UcgConditionalBpRule
            {
                supported = true,
                category = UcgConditionalBpCategory.ParsedOpponentTypeCondition,
                bpAmount = bpAmount,
                isStepUp = false,
                keyword = GetPrimaryKeyword(typeKeywords),
                allowedTypes = BuildAllowedTypes(typeKeywords)
            };
        }

        static UcgConditionalBpRule OwnCharacterNameCountFixedBpRule(int bpAmount, string characterNameKeyword)
        {
            return new UcgConditionalBpRule
            {
                supported = true,
                category = UcgConditionalBpCategory.MappedSelfCharacterNameCountBoost,
                bpAmount = bpAmount,
                isStepUp = false,
                keyword = characterNameKeyword,
                repeatPerMatchingCharacter = true
            };
        }

        static UcgEffectTargetFilter OwnTargetTypes(params string[] typeKeywords)
        {
            return new UcgEffectTargetFilter
            {
                targetSide = UcgEffectRelativeTargetSide.Self,
                targetCount = 1,
                targetAllowedTypes = BuildAllowedTypes(typeKeywords)
            };
        }

        static UcgEffectTargetFilter OwnTargetAny(int targetCount = 1)
        {
            return new UcgEffectTargetFilter
            {
                targetSide = UcgEffectRelativeTargetSide.Self,
                targetCount = targetCount
            };
        }

        static UcgEffectTargetFilter OpponentTargetAny()
        {
            return new UcgEffectTargetFilter
            {
                targetSide = UcgEffectRelativeTargetSide.Opponent,
                targetCount = 1
            };
        }

        static UcgEffectTargetFilter OpponentTargetFacingOwnerCharacters(params string[] ownerCharacterNames)
        {
            UcgEffectTargetFilter filter = OpponentTargetAny();
            filter.opposingLaneOwnerCharacterNames = BuildAllowedTypes(ownerCharacterNames);
            return filter;
        }

        static UcgEffectTargetFilter OwnTargetCharacterNameContains(string keyword)
        {
            return new UcgEffectTargetFilter
            {
                targetSide = UcgEffectRelativeTargetSide.Self,
                targetCount = 1,
                targetCharacterNameContains = keyword
            };
        }

        static UcgEffectConditionRule OwnConditionTypesAndName(string typeKeyword, string characterNameContains)
        {
            return new UcgEffectConditionRule
            {
                side = UcgEffectConditionSide.Self,
                requiredTypes = BuildAllowedTypes(new[] { typeKeyword }),
                characterNameContains = characterNameContains,
                failureMessage = "條件未達成，效果不發動。"
            };
        }

        static UcgEffectConditionRule OwnConditionNameContains(string characterNameContains)
        {
            return new UcgEffectConditionRule
            {
                side = UcgEffectConditionSide.Self,
                characterNameContains = characterNameContains,
                failureMessage = "條件未達成，效果不發動。"
            };
        }

        static UcgEffectRule UnsupportedPrimaryRule(string rawText, string reason)
        {
            return new UcgEffectRule
            {
                supported = false,
                effectCategory = UcgEffectCategory.None,
                trigger = UcgEffectTrigger.None,
                actionType = UcgEffectActionType.None,
                rawText = rawText,
                unsupportedReason = reason
            };
        }

        static UcgEffectRule CloneRule(UcgEffectRule source)
        {
            if (source == null) return null;
            return new UcgEffectRule
            {
                supported = source.supported,
                effectCategory = source.effectCategory,
                trigger = source.trigger,
                actionType = source.actionType,
                duration = source.duration,
                bpAmount = source.bpAmount,
                bpStepUp = source.bpStepUp,
                drawCount = source.drawCount,
                grantedType = source.grantedType,
                deckOperation = CloneDeckOperation(source.deckOperation),
                condition = CloneCondition(source.condition),
                selectionSourceZone = source.selectionSourceZone,
                reorderSourceZone = source.reorderSourceZone,
                reorderTopDeckCount = source.reorderTopDeckCount,
                requiredStackCount = source.requiredStackCount,
                requireExactStackCount = source.requireExactStackCount,
                allowedStackCounts = source.allowedStackCounts != null ? new List<int>(source.allowedStackCounts) : null,
                rawText = source.rawText,
                unsupportedReason = source.unsupportedReason
            };
        }

        static UcgDeckOperationRule CloneDeckOperation(UcgDeckOperationRule source)
        {
            if (source == null) return null;
            return new UcgDeckOperationRule
            {
                operationType = source.operationType,
                revealCount = source.revealCount,
                drawCount = source.drawCount,
                selectCount = source.selectCount,
                minSelectCount = source.minSelectCount,
                handSelectCount = source.handSelectCount,
                minHandSelectCount = source.minHandSelectCount,
                selectionFilter = source.selectionFilter,
                handSelectionFilter = source.handSelectionFilter,
                selectedDestination = source.selectedDestination,
                restDestination = source.restDestination,
                selectedHandCardDestination = source.selectedHandCardDestination,
                sendAllToRestDestinationIfNoValidSelection = source.sendAllToRestDestinationIfNoValidSelection,
                requiresPlayerSelection = source.requiresPlayerSelection,
                unsupportedReason = source.unsupportedReason
            };
        }

        static UcgConditionalBpRule CloneConditionalRule(UcgConditionalBpRule source)
        {
            if (source == null) return null;
            return new UcgConditionalBpRule
            {
                supported = source.supported,
                category = source.category,
                bpAmount = source.bpAmount,
                isStepUp = source.isStepUp,
                keyword = source.keyword,
                allowedTypes = source.allowedTypes != null ? new List<string>(source.allowedTypes) : null,
                repeatPerMatchingCharacter = source.repeatPerMatchingCharacter,
                requiredStackCount = source.requiredStackCount,
                requireExactStackCount = source.requireExactStackCount,
                unsupportedReason = source.unsupportedReason
            };
        }

        static UcgEffectTargetFilter CloneTargetFilter(UcgEffectTargetFilter source)
        {
            if (source == null) return null;
            return new UcgEffectTargetFilter
            {
                targetSide = source.targetSide,
                targetCount = source.targetCount,
                targetAllowedTypes = source.targetAllowedTypes != null ? new List<string>(source.targetAllowedTypes) : null,
                targetCharacterNames = source.targetCharacterNames != null ? new List<string>(source.targetCharacterNames) : null,
                targetCharacterNameContains = source.targetCharacterNameContains,
                opposingLaneOwnerCharacterNames = source.opposingLaneOwnerCharacterNames != null ? new List<string>(source.opposingLaneOwnerCharacterNames) : null
            };
        }

        static UcgCardPlayRestriction ClonePlayRestriction(UcgCardPlayRestriction source)
        {
            if (source == null) return null;
            return new UcgCardPlayRestriction
            {
                forbidSinglePlay = source.forbidSinglePlay,
                forbidSingleState = source.forbidSingleState,
                singleRestrictionMessage = source.singleRestrictionMessage
            };
        }

        static UcgEffectConditionRule CloneCondition(UcgEffectConditionRule source)
        {
            if (source == null) return null;
            return new UcgEffectConditionRule
            {
                side = source.side,
                requiredTypes = source.requiredTypes != null ? new List<string>(source.requiredTypes) : null,
                characterNameContains = source.characterNameContains,
                failureMessage = source.failureMessage
            };
        }

        static string GetPrimaryKeyword(string[] values)
        {
            if (values == null) return "";
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i])) return values[i];
            }

            return "";
        }

        static List<string> BuildAllowedTypes(string[] values)
        {
            var result = new List<string>();
            if (values == null) return result;

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (result.Contains(value)) continue;
                result.Add(value);
            }

            return result;
        }

        static List<int> BuildAllowedStackCounts(int[] values)
        {
            var result = new List<int>();
            if (values == null) return result;

            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                if (value <= 0 || result.Contains(value)) continue;
                result.Add(value);
            }

            return result;
        }

        static string GetEffectText(UcgCardData card)
        {
            if (card == null) return "";
            if (card.IsSceneCard() && !string.IsNullOrWhiteSpace(card.sceneDescription)) return card.sceneDescription;
            if (!string.IsNullOrWhiteSpace(card.effectDescription)) return card.effectDescription;
            return card.sceneDescription;
        }
    }
}
