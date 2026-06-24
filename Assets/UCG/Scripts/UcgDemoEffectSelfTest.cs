#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UCG
{
    public static class UcgDemoEffectSelfTest
    {
        static readonly string[] PlayerDigaDeckCardIds =
        {
            "BP01-001",
            "BP05-001",
            "SD01-005",
            "BP01-004",
            "BP05-002",
            "BP05-003",
            "BP01-008",
            "BP01-007",
            "BP05-005",
            "BP05-008",
            "BP01-037",
            "BP01-006",
            "BP01-043",
            "BP01-105"
        };

        static readonly string[] OpponentZeroDeckCardIds =
        {
            "BP01-055",
            "BP03-031",
            "BP02-009",
            "BP03-032",
            "BP03-033",
            "SD02-005",
            "BP05-038",
            "BP05-059",
            "BP01-061",
            "BP02-012",
            "BP01-062",
            "BP05-044",
            "SD02-014"
        };

        public sealed class Result
        {
            public int passedCount;
            public readonly List<Failure> failures = new List<Failure>();

            public bool Passed => failures.Count == 0;
        }

        public sealed class Failure
        {
            public string itemName;
            public string reason;
            public string cardId;
        }

        public static bool RunAndLog()
        {
            Result result = Run();
            Debug.Log(FormatResult(result));
            return result.Passed;
        }

        public static Result Run()
        {
            var result = new Result();

            CheckAllCardsMappedAndSupported(result, "我方迪卡牌組", PlayerDigaDeckCardIds);
            CheckAllCardsMappedAndSupported(result, "對手傑洛牌組", OpponentZeroDeckCardIds);
            CheckBp05002(result);
            CheckBp05008(result);
            CheckBp01043(result);
            CheckBp01105(result);
            CheckBp05044(result);
            CheckSd02014(result);
            CheckBp03031(result);

            return result;
        }

        static void CheckAllCardsMappedAndSupported(Result result, string deckName, string[] cardIds)
        {
            for (int i = 0; i < cardIds.Length; i++)
            {
                string cardId = cardIds[i];
                bool hasMapping = UcgTutorialCardEffectMap.TryGetMapping(cardId, out UcgTutorialCardEffectMapping mapping);
                Check(result, $"{deckName}/{cardId} has mapping", hasMapping);
                Check(result, $"{deckName}/{cardId} mapping Supported", hasMapping && mapping.support == UcgTutorialCardEffectSupport.Supported);
            }
        }

        static void CheckBp05002(Result result)
        {
            UcgEffectRule rule = GetPrimaryRule("BP05-002");
            UcgDeckOperationRule operation = rule != null ? rule.deckOperation : null;
            Check(result, "BP05-002 action is DeckOperation", rule != null && rule.actionType == UcgEffectActionType.DeckOperation);
            Check(result, "BP05-002 operation is SelectHandToBottomThenDrawSameCount",
                operation != null && operation.operationType == UcgDeckOperationType.SelectHandToBottomThenDrawSameCount);
            Check(result, "BP05-002 max selectable hand cards is 2",
                operation != null && operation.selectCount == 2 && operation.handSelectCount == 2);
            Check(result, "BP05-002 min selectable hand cards is 0",
                operation != null && operation.minSelectCount == 0 && operation.minHandSelectCount == 0);
            Check(result, "BP05-002 selected hand card destination is BottomOfDeck",
                operation != null && operation.selectedHandCardDestination == UcgDeckOperationDestination.BottomOfDeck);
        }

        static void CheckBp05008(Result result)
        {
            UcgEffectRule rule = GetPrimaryRule("BP05-008");
            Check(result, "BP05-008 action is SwapTopWithDiscard",
                rule != null && rule.actionType == UcgEffectActionType.SwapTopWithDiscard);
            Check(result, "BP05-008 selection source is DiscardPile",
                rule != null && rule.selectionSourceZone == UcgDeckOperationSourceZone.DiscardPile);
            Check(result, "BP05-008 requires stack count 4",
                rule != null && rule.requiredStackCount == 4 && !rule.requireExactStackCount);
        }

        static void CheckBp01043(Result result)
        {
            UcgEffectRule rule = GetPrimaryRule("BP01-043");
            Check(result, "BP01-043 action is ModifyBp",
                rule != null && rule.actionType == UcgEffectActionType.ModifyBp);
            Check(result, "BP01-043 has top deck reorder metadata",
                rule != null
                && rule.reorderSourceZone == UcgDeckOperationSourceZone.TopDeckReorder
                && rule.reorderTopDeckCount == 3);
            Check(result, "BP01-043 applies BP-3000",
                rule != null && rule.bpAmount == -3000 && rule.duration == UcgEffectDuration.UntilEndOfTurn);
        }

        static void CheckBp01105(Result result)
        {
            bool hasMapping = UcgTutorialCardEffectMap.TryGetMapping("BP01-105", out UcgTutorialCardEffectMapping mapping);
            UcgEffectRule rule = GetPrimaryRule("BP01-105");
            UcgDeckOperationRule operation = rule != null ? rule.deckOperation : null;
            Check(result, "BP01-105 scene effect id is upgrade from deck then discard at end",
                hasMapping && mapping.sceneEffectId == UcgDemoSceneEffectId.ActivatedUpgradeFromDeckThenDiscardAtEnd);
            Check(result, "BP01-105 action is SceneUpgradeFromDeck",
                rule != null && rule.actionType == UcgEffectActionType.SceneUpgradeFromDeck);
            Check(result, "BP01-105 reveal count is 5",
                operation != null && operation.revealCount == 5);
            Check(result, "BP01-105 duration is UntilEndOfTurn",
                rule != null && rule.duration == UcgEffectDuration.UntilEndOfTurn);
            Check(result, "BP01-105 condition requires Blazar",
                rule != null
                && rule.condition != null
                && rule.condition.side == UcgEffectConditionSide.Self
                && rule.condition.characterNameContains == "布雷薩");
        }

        static void CheckBp05044(Result result)
        {
            UcgEffectRule rule = GetPrimaryRule("BP05-044");
            bool hasRestriction = UcgTutorialCardEffectMap.TryGetPlayRestriction(
                MakeCard("BP05-044"),
                out UcgCardPlayRestriction restriction);
            Check(result, "BP05-044 requires QUAD stack for effect",
                rule != null && rule.requiredStackCount == 4 && !rule.requireExactStackCount && rule.bpStepUp);
            Check(result, "BP05-044 forbids single play/state",
                hasRestriction
                && restriction.forbidSinglePlay
                && restriction.forbidSingleState);
        }

        static void CheckSd02014(Result result)
        {
            bool hasFilter = UcgTutorialCardEffectMap.TryGetTargetFilter(
                MakeCard("SD02-014"),
                out UcgEffectTargetFilter filter);
            Check(result, "SD02-014 has target filter",
                hasFilter && filter != null);
            Check(result, "SD02-014 targets opponent",
                hasFilter && filter.targetSide == UcgEffectRelativeTargetSide.Opponent);
            Check(result, "SD02-014 requires opposing lane owner names",
                hasFilter
                && Contains(filter.opposingLaneOwnerCharacterNames, "傑洛")
                && Contains(filter.opposingLaneOwnerCharacterNames, "捷德")
                && Contains(filter.opposingLaneOwnerCharacterNames, "傑特"));
        }

        static void CheckBp03031(Result result)
        {
            bool hasRule = UcgTutorialCardEffectMap.TryGetConditionalRule(
                MakeCard("BP03-031"),
                out UcgConditionalBpRule rule);
            Check(result, "BP03-031 has conditional BP rule",
                hasRule && rule != null && rule.supported);
            Check(result, "BP03-031 uses self character count BP boost",
                hasRule
                && rule.category == UcgConditionalBpCategory.MappedSelfCharacterNameCountBoost
                && rule.repeatPerMatchingCharacter
                && rule.keyword == "傑洛"
                && rule.bpAmount == 1000);
        }

        static UcgEffectRule GetPrimaryRule(string cardId)
        {
            UcgTutorialCardEffectMap.TryGetPrimaryRule(MakeCard(cardId), out UcgEffectRule rule);
            return rule;
        }

        static UcgCardData MakeCard(string cardId)
        {
            return new UcgCardData { id = cardId };
        }

        static void Check(Result result, string itemName, bool passed, string reason = "Expected metadata condition was not met.")
        {
            if (passed)
            {
                result.passedCount++;
                return;
            }

            result.failures.Add(new Failure
            {
                itemName = itemName,
                reason = reason,
                cardId = InferCardId(itemName)
            });
        }

        static bool Contains(List<string> values, string expected)
        {
            return values != null && values.Contains(expected);
        }

        static string InferCardId(string itemName)
        {
            string cardId = FindCardIdInList(itemName, PlayerDigaDeckCardIds);
            if (!string.IsNullOrEmpty(cardId)) return cardId;
            cardId = FindCardIdInList(itemName, OpponentZeroDeckCardIds);
            return !string.IsNullOrEmpty(cardId) ? cardId : "N/A";
        }

        static string FindCardIdInList(string itemName, string[] cardIds)
        {
            if (string.IsNullOrEmpty(itemName) || cardIds == null) return "";
            for (int i = 0; i < cardIds.Length; i++)
            {
                string cardId = cardIds[i];
                if (!string.IsNullOrEmpty(cardId) && itemName.Contains(cardId))
                {
                    return cardId;
                }
            }

            return "";
        }

        static string FormatResult(Result result)
        {
            var builder = new StringBuilder();
            builder.Append("UCG Effect Self Test: ")
                .Append(result.passedCount)
                .Append(" passed, ")
                .Append(result.failures.Count)
                .Append(" failed");

            if (result.Passed)
            {
                builder.Append('\n').Append("All tutorial card effect mappings passed.");
                return builder.ToString();
            }

            builder.Append('\n').Append("Failed items:");
            for (int i = 0; i < result.failures.Count; i++)
            {
                Failure failure = result.failures[i];
                builder.Append('\n')
                    .Append("- card=").Append(failure.cardId)
                    .Append(" | item=").Append(failure.itemName)
                    .Append(" | reason=").Append(failure.reason);
            }

            return builder.ToString();
        }
    }
}
#endif
