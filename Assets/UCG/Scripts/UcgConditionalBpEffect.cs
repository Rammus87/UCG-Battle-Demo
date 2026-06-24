using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UCG
{
    public enum UcgConditionalBpCategory
    {
        Unsupported,
        ParsedFixedBpBoost,
        ParsedBpStepUp,
        ParsedOpponentTypeCondition,
        ParsedOpponentCategoryCondition,
        ParsedAllyTypeBoost,
        ParsedAllyCategoryBoost,
        ParsedCharacterNameCondition,
        MappedSelfCharacterNameCountBoost,
        ParsedSceneTypeBoost,
        ParsedSceneCharacterNameBoost
    }

    public class UcgConditionalBpRule
    {
        public bool supported;
        public UcgConditionalBpCategory category;
        public int bpAmount;
        public bool isStepUp;
        public string keyword;
        public List<string> allowedTypes;
        public bool repeatPerMatchingCharacter;
        public int requiredStackCount;
        public bool requireExactStackCount;
        public string unsupportedReason;
    }

    public static class UcgConditionalBpParser
    {
        static readonly HashSet<string> WarnedUnsupportedEffects = new HashSet<string>();
        static readonly List<string> UnsupportedEffectSummaries = new List<string>();
        public static bool debugEffectParsingVerbose;

        public static UcgConditionalBpRule Parse(UcgCardData sourceCard, IReadOnlyList<UcgCardData> contextCards)
        {
            if (UcgTutorialCardEffectMap.TryGetConditionalRule(sourceCard, out UcgConditionalBpRule mappedRule))
            {
                return mappedRule;
            }

            string text = GetEffectText(sourceCard);
            var rule = new UcgConditionalBpRule
            {
                supported = false,
                category = UcgConditionalBpCategory.Unsupported,
                bpAmount = ParseBpAmount(text),
                isStepUp = IsBpStepUp(text),
                keyword = "",
                repeatPerMatchingCharacter = IsRepeatPerMatchingCharacter(text),
                requiredStackCount = 0,
                requireExactStackCount = false,
                unsupportedReason = ""
            };

            UcgEffectParser.ParseStackRequirement(text, out rule.requiredStackCount, out rule.requireExactStackCount);

            if (sourceCard == null || string.IsNullOrWhiteSpace(text))
            {
                rule.unsupportedReason = "empty effect";
                return rule;
            }

            if (rule.bpAmount == 0 && !rule.isStepUp)
            {
                rule.unsupportedReason = "no supported BP boost or BP step-up pattern";
                return rule;
            }

            if (sourceCard.IsSceneCard())
            {
                if (TryFindCharacterName(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedSceneCharacterNameBoost;
                    return rule;
                }

                if (TryFindType(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedSceneTypeBoost;
                    return rule;
                }

                if (ContainsAny(text, "全體", "所有", "すべて", "全部"))
                {
                    rule.supported = true;
                    rule.category = rule.isStepUp
                        ? UcgConditionalBpCategory.ParsedBpStepUp
                        : UcgConditionalBpCategory.ParsedFixedBpBoost;
                    return rule;
                }

                rule.unsupportedReason = "scene BP effect has no clear type or characterName condition";
                return rule;
            }

            if (ContainsOpponentKeyword(text))
            {
                if (TryFindType(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedOpponentTypeCondition;
                    return rule;
                }

                if (TryFindCategory(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedOpponentCategoryCondition;
                    return rule;
                }
            }

            if (ContainsAllyKeyword(text))
            {
                if (TryFindType(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedAllyTypeBoost;
                    return rule;
                }

                if (TryFindCategory(text, contextCards, out rule.keyword))
                {
                    rule.supported = true;
                    rule.category = UcgConditionalBpCategory.ParsedAllyCategoryBoost;
                    return rule;
                }
            }

            if (TryFindCharacterName(text, contextCards, out rule.keyword))
            {
                rule.supported = true;
                rule.category = UcgConditionalBpCategory.ParsedCharacterNameCondition;
                return rule;
            }

            rule.unsupportedReason = "BP effect has no clear supported condition";
            return rule;
        }

        public static bool IsRepeatPerMatchingCharacter(string text)
        {
            return ContainsAny(text, "每", "每有", "每有一", "1名につき", "1体につき", "１名につき", "１体につき");
        }

        public static bool ContainsUnsupportedBpPattern(UcgCardData card)
        {
            string text = GetEffectText(card);
            if (string.IsNullOrWhiteSpace(text)) return false;
            return ParseBpAmount(text) != 0 || IsBpStepUp(text);
        }

        public static bool ShouldWarnUnsupportedConditional(UcgCardData card)
        {
            if (UcgTutorialCardEffectMap.HasMapping(card)) return false;

            string text = GetEffectText(card);
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (IsBpStepUp(text)) return true;
            if (ParseBpAmount(text) == 0) return false;
            return ContainsOpponentKeyword(text) || ContainsAllyKeyword(text) || ContainsAny(text, "場上", "有一名", "每有", "持有", "特徵", "類型");
        }

        public static void WarnUnsupported(UcgCardData card, UcgConditionalBpRule rule)
        {
            if (card == null || rule == null || string.IsNullOrWhiteSpace(rule.unsupportedReason)) return;
            string key = $"{card.id}:{rule.unsupportedReason}";
            if (WarnedUnsupportedEffects.Contains(key)) return;
            WarnedUnsupportedEffects.Add(key);
            UnsupportedEffectSummaries.Add($"{card.id} {card.cardName}, reason={rule.unsupportedReason}, effect={GetEffectText(card)}");
            if (debugEffectParsingVerbose)
            {
                UnityEngine.Debug.LogWarning($"Unsupported effect pattern: {card.id} {card.cardName}, reason={rule.unsupportedReason}, effect={GetEffectText(card)}");
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
                return "Conditional BP unsupported summary: unsupported count = 0";
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Conditional BP unsupported summary: unsupported count = {UnsupportedEffectSummaries.Count}");
            for (int i = 0; i < UnsupportedEffectSummaries.Count; i++)
            {
                builder.AppendLine($"- {UnsupportedEffectSummaries[i]}");
            }

            return builder.ToString();
        }

        static bool TryFindType(string text, IReadOnlyList<UcgCardData> contextCards, out string keyword)
        {
            return TryFindField(text, contextCards, card => card != null ? card.type : "", out keyword);
        }

        static bool TryFindCategory(string text, IReadOnlyList<UcgCardData> contextCards, out string keyword)
        {
            return TryFindField(text, contextCards, card => card != null ? card.cardCategory : "", out keyword);
        }

        static bool TryFindCharacterName(string text, IReadOnlyList<UcgCardData> contextCards, out string keyword)
        {
            if (TryFindField(text, contextCards, card => card != null ? card.characterName : "", out keyword)) return true;
            return TryFindField(text, contextCards, card => card != null ? card.cardName : "", out keyword);
        }

        static bool TryFindField(string text, IReadOnlyList<UcgCardData> contextCards, System.Func<UcgCardData, string> getter, out string keyword)
        {
            keyword = "";
            if (string.IsNullOrWhiteSpace(text) || contextCards == null || getter == null) return false;

            for (int i = 0; i < contextCards.Count; i++)
            {
                string value = getter(contextCards[i]);
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (value.Length < 2) continue;
                if (!text.Contains(value)) continue;
                keyword = value;
                return true;
            }

            return false;
        }

        static bool ContainsOpponentKeyword(string text)
        {
            return ContainsAny(text, "對手", "敵方", "相手", "敵");
        }

        static bool ContainsAllyKeyword(string text)
        {
            return ContainsAny(text, "我方", "己方", "隊友", "味方", "自分", "仲間", "他の");
        }

        static bool IsBpStepUp(string text)
        {
            return ContainsAny(text, "BP上升一階", "BP 上升一階", "BP上昇", "BPを1段階", "BPを１段階", "BPが1段階", "BPが１段階");
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
                if (string.IsNullOrWhiteSpace(keywords[i])) continue;
                if (text.Contains(keywords[i])) return true;
            }

            return false;
        }
    }
}
