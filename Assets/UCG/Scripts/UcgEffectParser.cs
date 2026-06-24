using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UCG
{
    public static class UcgEffectParser
    {
        static readonly HashSet<string> WarnedUnsupportedEffects = new HashSet<string>();
        static readonly List<string> UnsupportedEffectSummaries = new List<string>();

        public static bool debugEffectParsing;
        public static bool debugEffectParsingVerbose;
        public static bool debugDeckOperation;

        public static UcgEffectRule ParsePrimaryRule(UcgCardData card)
        {
            if (UcgTutorialCardEffectMap.TryGetPrimaryRule(card, out UcgEffectRule mappedRule))
            {
                return mappedRule;
            }

            string text = GetEffectText(card);
            var rule = new UcgEffectRule
            {
                rawText = text,
                supported = false,
                effectCategory = UcgEffectCategory.None,
                trigger = UcgEffectTrigger.None,
                actionType = UcgEffectActionType.None,
                bpStepUp = false,
                drawCount = 0,
                requiredStackCount = 0,
                requireExactStackCount = false,
                unsupportedReason = ""
            };

            if (card == null || string.IsNullOrWhiteSpace(text))
            {
                rule.supported = true;
                return rule;
            }

            rule.trigger = DetectTrigger(card, text);
            rule.effectCategory = GetEffectCategory(rule.trigger);
            rule.duration = DetectDuration(card, text, rule.trigger);
            rule.bpAmount = ParseBpAmount(text);
            rule.bpStepUp = IsBpStepUp(text);
            rule.drawCount = ParseDrawCardCount(text);
            rule.deckOperation = ParseDeckOperation(text);
            ParseStackRequirement(text, out rule.requiredStackCount, out rule.requireExactStackCount);

            if (rule.deckOperation != null && rule.deckOperation.operationType != UcgDeckOperationType.None)
            {
                rule.actionType = UcgEffectActionType.DeckOperation;
                rule.supported = IsSupportedDrawTiming(rule.trigger);
                if (!rule.supported)
                {
                    rule.unsupportedReason = rule.trigger == UcgEffectTrigger.Continuous
                        ? "continuous deck operation timing is not supported yet"
                        : "deck operation timing is not supported yet";
                }
                return rule;
            }

            if (rule.drawCount > 0)
            {
                rule.actionType = UcgEffectActionType.DrawCards;
                rule.supported = IsSupportedDrawTiming(rule.trigger) && !HasUnsupportedDrawContext(text);
                if (!rule.supported)
                {
                    rule.unsupportedReason = rule.trigger == UcgEffectTrigger.Continuous
                        ? "continuous draw effect timing is not supported yet"
                        : HasUnsupportedDrawContext(text)
                            ? "draw effect includes unsupported search/select/discard context"
                            : "draw effect timing is not supported yet";
                }
                return rule;
            }

            if (rule.bpAmount != 0)
            {
                rule.supported = true;
                rule.actionType = UcgEffectActionType.ModifyBp;
                return rule;
            }

            if (rule.bpStepUp)
            {
                rule.supported = true;
                rule.actionType = UcgEffectActionType.ModifyBp;
                return rule;
            }

            rule.unsupportedReason = "no supported BP or draw pattern detected";
            return rule;
        }

        public static void ApplyExecutableDemoMapping(UcgCardData card)
        {
            if (card == null) return;

            if (UcgTutorialCardEffectMap.TryApplyExecutableMapping(card, out UcgTutorialCardEffectMapping mapping))
            {
                if (mapping != null && !mapping.primaryRule.supported)
                {
                    UcgEffectRule explicitRule = ParsePrimaryRule(card);
                    WarnUnsupported(card, explicitRule, $"{mapping.support}: {mapping.note}");
                }

                if (debugEffectParsingVerbose && mapping != null)
                {
                    Debug.Log(
                        "Tutorial card effect mapping:\n"
                        + $"card={card.id} {card.cardName}\n"
                        + $"support={mapping.support}\n"
                        + $"effectTiming={card.effectTiming}\n"
                        + $"effectId={card.effectId}\n"
                        + $"sceneEffectTiming={card.sceneEffectTiming}\n"
                        + $"sceneEffectId={card.sceneEffectId}\n"
                        + $"note={mapping.note}");
                }
                return;
            }

            string text = GetEffectText(card);
            UcgEffectRule rule = ParsePrimaryRule(card);
            if ((debugEffectParsing || debugEffectParsingVerbose || debugDeckOperation) && card.id == "BP01-037")
            {
                bool isHandReturnOperation = rule.deckOperation != null
                    && rule.deckOperation.operationType == UcgDeckOperationType.DrawThenPutHandToBottom;
                Debug.Log(
                    "BP01-037 parser:\n"
                    + $"rawEffect={text}\n"
                    + $"matchedPattern={GetDeckOperationPatternName(rule.deckOperation)}\n"
                    + $"drawCount={(rule.deckOperation != null ? rule.deckOperation.drawCount : rule.drawCount)}\n"
                    + $"handSelectCount={(rule.deckOperation != null ? rule.deckOperation.handSelectCount : 0)}\n"
                    + $"selectionSource={(isHandReturnOperation ? "Hand" : "None")}\n"
                    + $"destination={(isHandReturnOperation ? rule.deckOperation.selectedHandCardDestination.ToString() : "None")}");
            }
            else if (debugEffectParsingVerbose && card.id == "BP01-004")
            {
                Debug.Log(
                    "DeckOperation parse:\n"
                    + $"card={card.id}\n"
                    + $"rawEffect={text}\n"
                    + $"matchedPattern={GetDeckOperationPatternName(rule.deckOperation)}\n"
                    + $"revealCount={(rule.deckOperation != null ? rule.deckOperation.revealCount : 0)}\n"
                    + $"drawCount={(rule.deckOperation != null ? rule.deckOperation.drawCount : rule.drawCount)}\n"
                    + $"selectCount={(rule.deckOperation != null ? rule.deckOperation.selectCount : 0)}\n"
                    + $"handSelectCount={(rule.deckOperation != null ? rule.deckOperation.handSelectCount : 0)}\n"
                    + $"selectionFilter={(rule.deckOperation != null ? rule.deckOperation.selectionFilter.ToString() : "Any")}");
            }
            if (!rule.supported)
            {
                WarnUnsupported(card, rule);
                return;
            }

            if (!UcgEffectRegistry.TryApplyDemoMapping(card, rule, text, out string unsupportedReason))
            {
                WarnUnsupported(card, rule, unsupportedReason);
            }
        }

        static UcgEffectTrigger DetectTrigger(UcgCardData card, string text)
        {
            if (ContainsAny(text, "發動", "[發動]", "【起動", "起動", "戰鬥效果", "戰鬥時")) return UcgEffectTrigger.Activated;
            if (ContainsAny(text, "登場", "出現", "配置", "放置")) return UcgEffectTrigger.OnRevealOrEnter;
            if (card != null && card.IsSceneCard()) return UcgEffectTrigger.Continuous;
            return UcgEffectTrigger.None;
        }

        static int ParseBpAmount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            Match match = Regex.Match(text, @"BP\s*([+\-＋－])\s*([0-9０-９,，]+)");
            if (!match.Success) return 0;

            int amount = 0;
            int.TryParse(NormalizeDigits(match.Groups[2].Value), out amount);
            string sign = match.Groups[1].Value;
            return sign == "-" || sign == "－" ? -amount : amount;
        }

        static bool IsBpStepUp(string text)
        {
            return ContainsAny(text, "BP上升一階", "BP 上升一階", "BP上昇", "BPを1段階", "BPを１段階", "BPが1段階", "BPが１段階");
        }

        static UcgEffectDuration DetectDuration(UcgCardData card, string text, UcgEffectTrigger trigger)
        {
            if (card != null && card.IsSceneCard() && trigger == UcgEffectTrigger.Continuous)
            {
                return UcgEffectDuration.WhileSceneActive;
            }

            if (ContainsAny(text, "本回合", "這個回合", "此回合", "回合期間"))
            {
                return UcgEffectDuration.UntilEndOfTurn;
            }

            return UcgEffectDuration.None;
        }

        static string NormalizeDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '０' && chars[i] <= '９')
                {
                    chars[i] = (char)('0' + chars[i] - '０');
                }
            }

            return new string(chars).Replace(",", "").Replace("，", "");
        }

        public static string DescribeRule(UcgEffectRule rule)
        {
            if (rule == null) return "null";
            string stackText = rule.requiredStackCount > 0
                ? $", requiredStack={(rule.requireExactStackCount ? "==" : ">=")}{rule.requiredStackCount}"
                : rule.allowedStackCounts != null && rule.allowedStackCounts.Count > 0
                    ? $", allowedStack={string.Join("/", rule.allowedStackCounts)}"
                : "";
            string valueText = rule.actionType == UcgEffectActionType.DrawCards
                ? $"draw={rule.drawCount}"
                : rule.actionType == UcgEffectActionType.DeckOperation && rule.deckOperation != null
                    ? $"deckOperation={rule.deckOperation.operationType}, reveal={rule.deckOperation.revealCount}, select={rule.deckOperation.selectCount}, selectedDest={rule.deckOperation.selectedDestination}, restDest={rule.deckOperation.restDestination}"
                : $"value={rule.bpAmount}, stepUp={rule.bpStepUp}";
            return $"parsed={rule.supported}, category={rule.effectCategory}, trigger={rule.trigger}, action={rule.actionType}, duration={rule.duration}, {valueText}{stackText}, reason={rule.unsupportedReason}";
        }

        public static bool ParseStackRequirement(string text, out int requiredStackCount, out bool requireExactStackCount)
        {
            requiredStackCount = 0;
            requireExactStackCount = false;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string normalized = NormalizeDigits(text);
            if (ContainsAny(normalized, "三疊", "3疊", "3叠", "三叠", "3枚重ね", "３枚重ね"))
            {
                requiredStackCount = 3;
                return true;
            }

            if (ContainsAny(normalized, "兩疊", "二疊", "2疊", "兩叠", "二叠", "2叠", "2枚重ね", "２枚重ね"))
            {
                requiredStackCount = 2;
                return true;
            }

            if (ContainsAny(normalized, "單張", "单张", "1疊", "一疊", "1叠", "一叠", "1枚", "１枚"))
            {
                requiredStackCount = 1;
                requireExactStackCount = ContainsAny(normalized, "僅單張", "仅单张", "只有單張", "只有单张", "限單張", "限单张", "單張才能", "单张才能");
                return true;
            }

            return false;
        }

        public static bool IsStackRequirementMet(UcgCardData card, int stackCount, out int requiredStackCount, out bool requireExactStackCount)
        {
            UcgEffectRule rule = ParsePrimaryRule(card);
            requiredStackCount = rule != null ? rule.requiredStackCount : 0;
            requireExactStackCount = rule != null && rule.requireExactStackCount;
            if (card != null && card.IsSceneCard()) return true;
            if (rule != null && rule.allowedStackCounts != null && rule.allowedStackCounts.Count > 0)
            {
                return rule.allowedStackCounts.Contains(stackCount);
            }
            if (requiredStackCount <= 0) return true;

            return requireExactStackCount
                ? stackCount == requiredStackCount
                : stackCount >= requiredStackCount;
        }

        static UcgEffectCategory GetEffectCategory(UcgEffectTrigger trigger)
        {
            switch (trigger)
            {
                case UcgEffectTrigger.OnRevealOrEnter:
                    return UcgEffectCategory.EnterEffect;
                case UcgEffectTrigger.Activated:
                    return UcgEffectCategory.BattleEffect;
                case UcgEffectTrigger.Continuous:
                    return UcgEffectCategory.ContinuousEffect;
                default:
                    return UcgEffectCategory.None;
            }
        }

        public static int ParseDrawCardCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            string normalized = NormalizeDigits(text);
            Match match = Regex.Match(normalized, @"(?:抽|拿)\s*([12一二兩])\s*張\s*牌?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int numericCount))
            {
                return numericCount;
            }
            if (match.Success && TryParseSmallNumber(match.Groups[1].Value, out numericCount))
            {
                return numericCount;
            }

            if (ContainsAny(normalized, "抽一張", "抽壹張")) return 1;
            if (ContainsAny(normalized, "抽兩張", "抽二張", "抽貳張")) return 2;
            if (ContainsAny(normalized, "拿一張", "拿壹張")) return 1;
            if (ContainsAny(normalized, "拿兩張", "拿二張", "拿貳張")) return 2;
            return 0;
        }

        public static UcgDeckOperationRule ParseDeckOperation(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            string normalized = NormalizeDigits(text);
            int revealCount = ParseRevealTopCount(normalized);
            int selectCount = ParseSelectCount(normalized);
            int drawCount = ParseDrawCardCount(normalized);
            bool hasRevealTopCue = ContainsAny(normalized, "公開", "翻開", "展示");
            bool hasHandToBottomCue = ContainsAny(normalized, "手牌中", "我方手牌", "手牌")
                && ContainsDeckBottomCue(normalized);
            if (hasRevealTopCue && revealCount > 0 && drawCount > 0 && hasHandToBottomCue)
            {
                Debug.LogWarning($"Ambiguous deck operation pattern, prefer DrawThenPutHandToBottom: {text}");
            }
            if (drawCount > 0 && hasHandToBottomCue)
            {
                int handSelectCount = Mathf.Max(1, selectCount);
                return new UcgDeckOperationRule
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
                };
            }

            if (hasRevealTopCue
                && revealCount > 0
                && selectCount == 1
                && ContainsAny(normalized, "加入手牌", "加入我方手牌", "加到手牌", "加到我方手牌", "放入手牌", "放入我方手牌", "手牌中")
                && ContainsAny(normalized, "其餘", "剩下", "剩餘", "餘下", "其他", "其他卡牌", "未加入手牌", "沒加入手牌")
                && ContainsAny(normalized, "棄牌", "棄牌區", "丟棄", "捨棄", "丟到棄牌區", "放到棄牌區", "放入棄牌區", "置入棄牌區"))
            {
                return new UcgDeckOperationRule
                {
                    operationType = UcgDeckOperationType.RevealTopSelectToHandRestTrash,
                    revealCount = revealCount,
                    selectCount = selectCount,
                    selectionFilter = DetectSelectionFilter(normalized),
                    selectedDestination = UcgDeckOperationDestination.Hand,
                    restDestination = UcgDeckOperationDestination.Trash,
                    sendAllToRestDestinationIfNoValidSelection = true,
                    requiresPlayerSelection = true
                };
            }

            if (drawCount > 0
                && selectCount > 0
                && ContainsDeckBottomCue(normalized))
            {
                return new UcgDeckOperationRule
                {
                    operationType = UcgDeckOperationType.DrawThenSelectBottom,
                    revealCount = drawCount,
                    drawCount = drawCount,
                    selectCount = selectCount,
                    selectionFilter = UcgDeckSelectionFilter.Any,
                    selectedDestination = UcgDeckOperationDestination.BottomOfDeck,
                    restDestination = UcgDeckOperationDestination.Hand,
                    requiresPlayerSelection = true
                };
            }

            return null;
        }

        static bool ContainsDeckBottomCue(string text)
        {
            return ContainsAny(text,
                "放回牌組最底",
                "放回牌庫最底",
                "放到牌組最底",
                "放到牌庫最底",
                "放入牌組最底",
                "放入牌庫最底",
                "牌組最底下",
                "牌庫最底下",
                "牌組底下",
                "牌庫底下",
                "牌組最下方",
                "牌庫最下方",
                "牌組下方",
                "牌庫下方");
        }

        static string GetDeckOperationPatternName(UcgDeckOperationRule rule)
        {
            if (rule == null) return "None";
            switch (rule.operationType)
            {
                case UcgDeckOperationType.RevealTopSelectToHandRestTrash:
                    return rule.selectionFilter == UcgDeckSelectionFilter.SceneCard
                        ? "RevealTopSelectScene"
                        : "RevealTopSelect";
                case UcgDeckOperationType.DrawThenPutHandToBottom:
                    return "DrawThenPutHandToBottom";
                case UcgDeckOperationType.SelectHandToBottomThenDrawSameCount:
                    return "SelectHandToBottomThenDrawSameCount";
                case UcgDeckOperationType.DrawThenSelectBottom:
                    return "DrawThenSelectBottom";
                default:
                    return rule.operationType.ToString();
            }
        }

        static int ParseRevealTopCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            Match match = Regex.Match(text, @"(?:公開|展示|翻開).{0,12}(?:牌組|牌庫).{0,6}(?:上方|上面|頂端).{0,3}([1-9一二兩三四五六七八九])\s*張");
            if (!match.Success)
            {
                match = Regex.Match(text, @"(?:牌組|牌庫).{0,6}(?:上方|上面|頂端).{0,6}(?:公開|展示|翻開).{0,3}([1-9一二兩三四五六七八九])\s*張");
            }
            if (!match.Success)
            {
                match = Regex.Match(text, @"(?:從)?(?:我方)?(?:牌組|牌庫).{0,6}(?:上方|上面|頂端).{0,3}([1-9一二兩三四五六七八九])\s*張.{0,8}(?:公開|展示|翻開)");
            }
            if (match.Success && TryParseSmallNumber(match.Groups[1].Value, out int numericCount))
            {
                return numericCount;
            }

            if (ContainsAny(text, "公開牌組上方五張", "公開牌庫上方五張", "牌組上方五張公開", "牌庫上方五張公開")) return 5;
            if (ContainsAny(text, "公開牌組上方三張", "公開牌庫上方三張", "牌組上方三張公開", "牌庫上方三張公開")) return 3;
            if (ContainsAny(text, "公開牌組上方兩張", "公開牌庫上方兩張", "牌組上方兩張公開", "牌庫上方兩張公開")) return 2;
            if (ContainsAny(text, "公開牌組上方一張", "公開牌庫上方一張", "牌組上方一張公開", "牌庫上方一張公開")) return 1;
            return 0;
        }

        static UcgDeckSelectionFilter DetectSelectionFilter(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return UcgDeckSelectionFilter.Any;
            if (ContainsAny(text, "場景卡", "場景牌", "場景")) return UcgDeckSelectionFilter.SceneCard;
            if (ContainsAny(text, "超人力霸王", "超人卡", "超人牌", "超人")) return UcgDeckSelectionFilter.UltramanCard;
            return UcgDeckSelectionFilter.Any;
        }

        static int ParseSelectCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            Match match = Regex.Match(text, @"(?:選擇|選|其中|找)\s*([1-9一二兩三四五六七八九])\s*張");
            if (match.Success && TryParseSmallNumber(match.Groups[1].Value, out int numericCount))
            {
                return numericCount;
            }

            if (ContainsAny(text, "選擇一張", "選一張", "其中一張", "將其中一張", "找一張")) return 1;
            if (ContainsAny(text, "選擇兩張", "選兩張", "其中兩張", "將其中兩張", "找兩張", "找二張")) return 2;
            return 0;
        }

        static bool TryParseSmallNumber(string value, out int number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (int.TryParse(value, out number)) return true;

            switch (value)
            {
                case "一":
                    number = 1;
                    return true;
                case "二":
                case "兩":
                    number = 2;
                    return true;
                case "三":
                    number = 3;
                    return true;
                case "四":
                    number = 4;
                    return true;
                case "五":
                    number = 5;
                    return true;
                case "六":
                    number = 6;
                    return true;
                case "七":
                    number = 7;
                    return true;
                case "八":
                    number = 8;
                    return true;
                case "九":
                    number = 9;
                    return true;
                default:
                    return false;
            }
        }

        static bool IsSupportedDrawTiming(UcgEffectTrigger trigger)
        {
            return trigger == UcgEffectTrigger.OnRevealOrEnter
                || trigger == UcgEffectTrigger.Activated;
        }

        static bool HasUnsupportedDrawContext(string text)
        {
            return ContainsAny(
                text,
                "檢索",
                "搜尋",
                "搜索",
                "選擇",
                "選1張",
                "選一張",
                "加入手牌",
                "棄牌",
                "展示",
                "公開",
                "洗牌");
        }

        static string GetEffectText(UcgCardData card)
        {
            if (card == null) return "";
            if (card.IsSceneCard() && !string.IsNullOrWhiteSpace(card.sceneDescription)) return card.sceneDescription;
            if (!string.IsNullOrWhiteSpace(card.effectDescription)) return card.effectDescription;
            return card.sceneDescription;
        }

        static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text) || keywords == null) return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = keywords[i];
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                if (text.Contains(keyword)) return true;
            }

            return false;
        }

        static void WarnUnsupported(UcgCardData card, UcgEffectRule rule, string overrideReason = "")
        {
            string key = $"{card.id}:{rule.rawText}:{overrideReason}";
            if (WarnedUnsupportedEffects.Contains(key)) return;
            WarnedUnsupportedEffects.Add(key);

            string reason = !string.IsNullOrWhiteSpace(overrideReason) ? overrideReason : rule.unsupportedReason;
            UnsupportedEffectSummaries.Add($"id={card.id}, name={card.cardName}, reason={reason}, effect={rule.rawText}");
            if (debugEffectParsingVerbose)
            {
                Debug.LogWarning($"Unsupported effect pattern: id={card.id}, name={card.cardName}, reason={reason}, effect={rule.rawText}");
            }
        }

        public static void ResetUnsupportedSummary()
        {
            WarnedUnsupportedEffects.Clear();
            UnsupportedEffectSummaries.Clear();
        }

        public static string GetUnsupportedSummary()
        {
            if (UnsupportedEffectSummaries.Count == 0)
            {
                return "Primary effect unsupported summary: unsupported count = 0";
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Primary effect unsupported summary: unsupported count = {UnsupportedEffectSummaries.Count}");
            for (int i = 0; i < UnsupportedEffectSummaries.Count; i++)
            {
                builder.AppendLine($"- {UnsupportedEffectSummaries[i]}");
            }

            return builder.ToString();
        }
    }
}
