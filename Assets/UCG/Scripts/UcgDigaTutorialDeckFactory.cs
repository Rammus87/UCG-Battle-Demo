using System.Collections.Generic;
using UnityEngine;

namespace UCG
{
    public static class UcgDigaTutorialDeckFactory
    {
        public const int TemplateCount = 8;
        public const string TargetTutorialSceneName = "創立SKaRD的男人";
        const string DigaLv1Id = "BP01-001";
        const string DigaLv1Sku = "BP-01-001-null";
        const string DigaLv2Id = "BP01-004";
        const string DigaLv2Sku = "BP-01-004-null";
        const string TargetSceneId = "BP01-105";
        const string TargetSceneSku = "BP-01-105-null";

        public static UcgDeckDefinition CreatePlayerDeckDefinition()
        {
            return new UcgDeckDefinition
            {
                deckId = "tutorial-diga-player",
                name = "迪卡教學牌組",
                cards =
                {
                    new UcgDeckDefinitionEntry("BP01-001", 4),
                    new UcgDeckDefinitionEntry("BP05-001", 4),
                    new UcgDeckDefinitionEntry("SD01-005", 2),
                    new UcgDeckDefinitionEntry("BP01-004", 4),
                    new UcgDeckDefinitionEntry("BP05-002", 4),
                    new UcgDeckDefinitionEntry("BP05-003", 4),
                    new UcgDeckDefinitionEntry("BP01-008", 2),
                    new UcgDeckDefinitionEntry("BP01-007", 4),
                    new UcgDeckDefinitionEntry("BP05-005", 4),
                    new UcgDeckDefinitionEntry("BP05-008", 2),
                    new UcgDeckDefinitionEntry("BP01-037", 4),
                    new UcgDeckDefinitionEntry("BP01-006", 4),
                    new UcgDeckDefinitionEntry("BP01-043", 4),
                    new UcgDeckDefinitionEntry("BP01-105", 4)
                }
            };
        }

        public static UcgDeckDefinition CreateOpponentDeckDefinition()
        {
            return new UcgDeckDefinition
            {
                deckId = "tutorial-zero-opponent",
                name = "傑洛教學對手牌組",
                cards =
                {
                    new UcgDeckDefinitionEntry("BP01-055", 4),
                    new UcgDeckDefinitionEntry("BP03-031", 4),
                    new UcgDeckDefinitionEntry("BP02-009", 4),
                    new UcgDeckDefinitionEntry("BP03-032", 4),
                    new UcgDeckDefinitionEntry("BP03-033", 4),
                    new UcgDeckDefinitionEntry("SD02-005", 4),
                    new UcgDeckDefinitionEntry("BP05-038", 4),
                    new UcgDeckDefinitionEntry("BP05-059", 4),
                    new UcgDeckDefinitionEntry("BP01-061", 4),
                    new UcgDeckDefinitionEntry("BP02-012", 4),
                    new UcgDeckDefinitionEntry("BP01-062", 4),
                    new UcgDeckDefinitionEntry("BP05-044", 3),
                    new UcgDeckDefinitionEntry("SD02-014", 3)
                }
            };
        }

        public static UcgDeckProfile BuildProfile(int repeatCount)
        {
            var profile = new UcgDeckProfile
            {
                deckId = "diga-tutorial",
                deckName = "迪卡實戰教學牌組",
                source = "Fallback"
            };

            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                for (int index = 0; index < TemplateCount; index++)
                {
                    UcgCardData card = CreateTemplateCard(index);
                    card.id = $"{card.id}-deck-{repeat + 1}-{index + 1}";
                    profile.cards.Add(card);

                    if (repeat == 0)
                    {
                        if (profile.guaranteedBaseCard == null && IsDigaLevel(card, 1))
                        {
                            profile.guaranteedBaseCard = card;
                        }
                        else if (profile.guaranteedUpgradeCard == null && IsDigaLevel(card, 2))
                        {
                            profile.guaranteedUpgradeCard = card;
                        }
                        else if (profile.guaranteedSceneCard == null && card.IsSceneCard())
                        {
                            profile.guaranteedSceneCard = card;
                        }
                    }
                }
            }

            return profile;
        }

        public static UcgDeckProfile BuildProfile(int repeatCount, UcgExternalCardDatabase externalDatabase)
        {
            if (externalDatabase != null && externalDatabase.LoadDatabase())
            {
                if (TryBuildExternalProfile(repeatCount, externalDatabase, out UcgDeckProfile externalProfile, out string fallbackReason))
                {
                    Debug.Log("Diga tutorial deck source: External");
                    Debug.Log($"Diga tutorial guaranteed base: {FormatCard(externalProfile.guaranteedBaseCard)}");
                    Debug.Log($"Diga tutorial guaranteed upgrade: {FormatCard(externalProfile.guaranteedUpgradeCard)}");
                    Debug.Log($"Diga tutorial guaranteed scene: {FormatCard(externalProfile.guaranteedSceneCard)}");
                    return externalProfile;
                }

                Debug.LogWarning($"Fallback reason: {fallbackReason}");
            }
            else
            {
                Debug.LogWarning("Fallback reason: external database unavailable");
            }

            Debug.Log("Diga tutorial deck source: Fallback");
            return BuildProfile(repeatCount);
        }

        static bool TryBuildExternalProfile(
            int repeatCount,
            UcgExternalCardDatabase externalDatabase,
            out UcgDeckProfile profile,
            out string fallbackReason)
        {
            profile = null;
            fallbackReason = "";

            UcgCardData digaLv1 = ResolveDigaLv1(externalDatabase);
            if (digaLv1 == null)
            {
                fallbackReason = "Diga Lv1 missing";
                return false;
            }

            UcgCardData digaLv2 = ResolveDigaLv2(externalDatabase, digaLv1);
            if (digaLv2 == null)
            {
                fallbackReason = "Diga Lv2 missing";
                return false;
            }

            UcgCardData targetScene = FindTargetTutorialScene(externalDatabase);
            if (targetScene == null)
            {
                fallbackReason = "Diga target scene BP01-105 missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(digaLv1.imageLocal))
            {
                fallbackReason = "Diga Lv1 imageLocal missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(digaLv2.imageLocal))
            {
                fallbackReason = "Diga Lv2 imageLocal missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetScene.imageLocal))
            {
                fallbackReason = "Diga target scene imageLocal missing";
                return false;
            }

            UcgDeckDefinition deckDefinition = CreatePlayerDeckDefinition();
            if (!UcgDeckDefinitionResolver.TryBuildProfile(deckDefinition, externalDatabase, "External", out profile))
            {
                fallbackReason = "player deck definition could not be resolved";
                return false;
            }

            ApplyDigaTutorialOverrides(profile);

            if (profile.guaranteedBaseCard == null || profile.guaranteedUpgradeCard == null || profile.guaranteedSceneCard == null)
            {
                fallbackReason = $"external guaranteed opening cards unresolved: base={FormatCard(profile.guaranteedBaseCard)}, upgrade={FormatCard(profile.guaranteedUpgradeCard)}, scene={FormatCard(profile.guaranteedSceneCard)}";
                return false;
            }

            return true;
        }

        static void ApplyDigaTutorialOverrides(UcgDeckProfile profile)
        {
            if (profile == null) return;

            for (int i = 0; i < profile.cards.Count; i++)
            {
                UcgCardData card = profile.cards[i];
                if (card == null) continue;

                if (card.id == DigaLv1Id)
                {
                    if (profile.guaranteedBaseCard == null) profile.guaranteedBaseCard = card;
                    if (card.effectId == UcgDemoEffectId.None && !UcgTutorialCardEffectMap.HasMapping(card))
                    {
                        card.effectTiming = UcgEffectTiming.OnRevealOrEnter;
                        card.effectId = UcgDemoEffectId.OnRevealSelfBpPlus1000;
                    }
                }
                else if (card.id == DigaLv2Id)
                {
                    if (profile.guaranteedUpgradeCard == null) profile.guaranteedUpgradeCard = card;
                    if (card.effectId == UcgDemoEffectId.None && !UcgTutorialCardEffectMap.HasMapping(card))
                    {
                        card.effectTiming = UcgEffectTiming.Activated;
                        card.effectId = UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000;
                    }
                }
                else if (card.id == TargetSceneId)
                {
                    card.cardCategory = "場景";
                    card.sceneTurnCost = 2;
                    if (profile.guaranteedSceneCard == null) profile.guaranteedSceneCard = card;
                    if (card.sceneEffectId == UcgDemoSceneEffectId.None && !UcgTutorialCardEffectMap.HasMapping(card))
                    {
                        card.sceneEffectTiming = UcgEffectTiming.Continuous;
                        card.sceneEffectId = UcgDemoSceneEffectId.PlayerAllBpPlus500;
                    }
                }
            }
        }

        public static UcgCardData CreateTemplateCard(int index)
        {
            switch (index)
            {
                case 0:
                    return CreateSceneCard(
                        "diga-scene-super-time-battle",
                        TargetTutorialSceneName,
                        2,
                        UcgDemoSceneEffectId.PlayerAllBpPlus500,
                        "常駐：持有者所有角色 BP +500");
                case 1:
                    return CreateSceneCard(
                        "diga-scene-command-base",
                        "宇宙警備隊基地",
                        2,
                        UcgDemoSceneEffectId.ActivatedChooseOwnLaneBpPlus1000,
                        "紅色：[發動] 選擇持有者一條 Lane，BP +1000");
                case 2:
                    return CreateCharacterCard(
                        "diga-lv1",
                        "迪卡 Lv.1",
                        "迪卡",
                        1,
                        UcgEffectTiming.OnRevealOrEnter,
                        UcgDemoEffectId.OnRevealSelfBpPlus1000,
                        "藍色：登場時，本回合此 Lane 我方 BP +1000");
                case 3:
                    return CreateCharacterCard(
                        "diga-lv2",
                        "迪卡 Lv.2",
                        "迪卡",
                        2,
                        UcgEffectTiming.Activated,
                        UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000,
                        "紅色：[發動] 選擇對手一條 Lane，BP -1000");
                case 4:
                    return CreateCharacterCard(
                        "diga-lv3",
                        "迪卡 Lv.3",
                        "迪卡",
                        3,
                        UcgEffectTiming.None,
                        UcgDemoEffectId.None,
                        "");
                case 5:
                    return CreateCharacterCard(
                        "aguru-lv1",
                        "阿古茹 Lv.1",
                        "阿古茹",
                        1,
                        UcgEffectTiming.OnRevealOrEnter,
                        UcgDemoEffectId.OnRevealDrawOne,
                        "藍色：登場時，抽 1 張牌");
                case 6:
                    return CreateCharacterCard(
                        "aguru-lv2",
                        "阿古茹 Lv.2",
                        "阿古茹",
                        2,
                        UcgEffectTiming.Activated,
                        UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000,
                        "紅色：[發動] 選擇對手一條 Lane，BP -1000");
                default:
                    return CreateCharacterCard(
                        "support-ultra-lv1",
                        "教學支援角色 Lv.1",
                        "教學支援角色",
                        1,
                        UcgEffectTiming.None,
                        UcgDemoEffectId.None,
                        "");
            }
        }

        static UcgCardData CreateSceneCard(
            string id,
            string cardName,
            int sceneTurnCost,
            UcgDemoSceneEffectId sceneEffectId,
            string sceneDescription)
        {
            return new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = "",
                cardCategory = "場景",
                level = 0,
                teamTag = "",
                effectTiming = UcgEffectTiming.None,
                effectId = UcgDemoEffectId.None,
                effectDescription = "",
                sceneTurnCost = sceneTurnCost,
                sceneEffectTiming = GetSceneEffectTiming(sceneEffectId),
                sceneEffectId = sceneEffectId,
                sceneDescription = sceneDescription
            };
        }

        static UcgCardData CreateCharacterCard(
            string id,
            string cardName,
            string characterName,
            int level,
            UcgEffectTiming effectTiming,
            UcgDemoEffectId effectId,
            string effectDescription)
        {
            var card = new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = characterName,
                cardCategory = "超人力霸王",
                level = level,
                teamTag = "",
                effectTiming = effectTiming,
                effectId = effectId,
                effectDescription = effectDescription,
                sceneTurnCost = 0,
                sceneEffectTiming = UcgEffectTiming.None,
                sceneEffectId = UcgDemoSceneEffectId.None,
                sceneDescription = ""
            };

            ApplyUltramanBp(card);
            return card;
        }

        static UcgEffectTiming GetSceneEffectTiming(UcgDemoSceneEffectId sceneEffectId)
        {
            switch (sceneEffectId)
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
                    return UcgEffectTiming.Continuous;
                case UcgDemoSceneEffectId.ActivatedChooseOwnLaneBpPlus1000:
                    return UcgEffectTiming.Activated;
                case UcgDemoSceneEffectId.OnEnterDrawOne:
                    return UcgEffectTiming.OnRevealOrEnter;
                default:
                    return UcgEffectTiming.None;
            }
        }

        static void ApplyUltramanBp(UcgCardData card)
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

        static void SetBp(UcgCardData card, int singleBp, int doubleBp, int tripleBp, int quadBp)
        {
            card.singleBp = singleBp;
            card.doubleBp = doubleBp;
            card.tripleBp = tripleBp;
            card.quadBp = quadBp;
        }

        static bool IsDigaLevel(UcgCardData card, int level)
        {
            return card != null
                && !card.IsSceneCard()
                && IsDigaCard(card)
                && card.level == level;
        }

        static bool IsDigaCard(UcgCardData card)
        {
            return card != null
                && (ContainsText(card.characterName, "迪卡") || ContainsText(card.cardName, "迪卡"));
        }

        static bool IsDigaUpgradeFor(UcgCardData card, UcgCardData baseCard)
        {
            return card != null
                && baseCard != null
                && IsDigaCard(card)
                && card.characterName == baseCard.characterName
                && card.level == baseCard.level + 1;
        }

        static UcgCardData FindDigaLevel(
            UcgExternalCardDatabase externalDatabase,
            int level,
            string characterName,
            UcgCardData excludedCard = null)
        {
            if (externalDatabase == null) return null;

            List<UcgCardData> candidates = externalDatabase.FindCards(card =>
                card != null
                && !card.IsSceneCard()
                && IsDigaCard(card)
                && card.level == level
                && (string.IsNullOrWhiteSpace(characterName) || card.characterName == characterName)
                && !ReferenceEquals(card, excludedCard));

            return candidates.Count > 0 ? candidates[0] : null;
        }

        static UcgCardData ResolveDigaLv1(UcgExternalCardDatabase externalDatabase)
        {
            UcgCardData card = ResolveCardByIdSku(externalDatabase, DigaLv1Id, DigaLv1Sku);
            if (card == null)
            {
                card = FindDigaLevel(externalDatabase, 1, null);
            }

            Debug.Log(card != null
                ? $"Resolve Diga Lv1: success {FormatCard(card)}"
                : "Resolve Diga Lv1: failed Diga Lv1 missing");
            return card;
        }

        static UcgCardData ResolveDigaLv2(UcgExternalCardDatabase externalDatabase, UcgCardData digaLv1)
        {
            UcgCardData card = ResolveCardByIdSku(externalDatabase, DigaLv2Id, DigaLv2Sku);
            if (card == null || !IsDigaUpgradeFor(card, digaLv1))
            {
                card = FindDigaLevel(externalDatabase, digaLv1 != null ? digaLv1.level + 1 : 2, digaLv1 != null ? digaLv1.characterName : "迪卡");
            }

            Debug.Log(card != null
                ? $"Resolve Diga Lv2: success {FormatCard(card)}"
                : "Resolve Diga Lv2: failed Diga Lv2 missing");
            return card;
        }

        static UcgCardData ResolveCardByIdSku(UcgExternalCardDatabase externalDatabase, string id, string sku)
        {
            if (externalDatabase == null) return null;

            UcgCardData card = externalDatabase.GetCardById(id);
            if (card != null) return card;

            return externalDatabase.GetCardBySku(sku);
        }

        static UcgCardData FindSceneByLight(UcgExternalCardDatabase externalDatabase, int sceneTurnCost)
        {
            if (externalDatabase == null) return null;

            List<UcgCardData> candidates = externalDatabase.FindCards(card =>
                card != null
                && card.IsSceneCard()
                && card.sceneTurnCost == sceneTurnCost);

            return candidates.Count > 0 ? candidates[0] : null;
        }

        static UcgCardData FindTargetTutorialScene(UcgExternalCardDatabase externalDatabase)
        {
            if (externalDatabase == null) return null;

            UcgCardData byIdOrSku = ResolveCardByIdSku(externalDatabase, TargetSceneId, TargetSceneSku);
            if (byIdOrSku != null && byIdOrSku.IsSceneCard())
            {
                Debug.Log($"Resolve Diga Scene: success {FormatCard(byIdOrSku)}");
                return byIdOrSku;
            }

            List<UcgCardData> exactCandidates = externalDatabase.FindCards(card =>
                card != null
                && card.IsSceneCard()
                && card.cardName == TargetTutorialSceneName);
            if (exactCandidates.Count > 0)
            {
                Debug.Log($"Resolve Diga Scene: success {FormatCard(exactCandidates[0])}");
                return exactCandidates[0];
            }

            Debug.LogWarning($"Diga tutorial target scene not found: {TargetTutorialSceneName}");
            List<UcgCardData> containsCandidates = externalDatabase.FindCards(card =>
                card != null
                && card.IsSceneCard()
                && ContainsText(card.cardName, TargetTutorialSceneName));

            if (containsCandidates.Count > 0)
            {
                Debug.LogWarning($"Diga tutorial target scene found by contains fallback: {FormatCard(containsCandidates[0])}");
                Debug.Log($"Resolve Diga Scene: success {FormatCard(containsCandidates[0])}");
                return containsCandidates[0];
            }

            List<UcgCardData> twoLightScenes = externalDatabase.FindCards(card =>
                card != null
                && card.IsSceneCard()
                && card.sceneTurnCost == 2);
            if (twoLightScenes.Count > 0)
            {
                Debug.LogWarning($"Diga tutorial target scene fallback to available 2-light scene: {FormatCard(twoLightScenes[0])}");
                Debug.Log($"Resolve Diga Scene: success {FormatCard(twoLightScenes[0])}");
                return twoLightScenes[0];
            }

            List<UcgCardData> anyScenes = externalDatabase.FindCards(card => card != null && card.IsSceneCard());
            if (anyScenes.Count > 0)
            {
                Debug.LogWarning($"Diga tutorial target scene fallback to available scene: {FormatCard(anyScenes[0])}");
                Debug.Log($"Resolve Diga Scene: success {FormatCard(anyScenes[0])}");
                return anyScenes[0];
            }

            Debug.Log("Resolve Diga Scene: failed Diga target scene BP01-105 missing");
            return null;
        }

        static bool IsTargetTutorialScene(UcgCardData card)
        {
            if (card == null || !card.IsSceneCard()) return false;
            if (card.cardName == TargetTutorialSceneName) return true;

            return ContainsText(card.cardName, TargetTutorialSceneName);
        }

        static UcgCardData CreateExternalCharacterTemplate(
            UcgCardData source,
            UcgEffectTiming effectTiming,
            UcgDemoEffectId effectId,
            string effectDescription)
        {
            if (source == null) return null;

            UcgCardData card = CloneCard(source);
            card.effectTiming = effectTiming;
            card.effectId = effectId;
            card.effectDescription = effectDescription;
            card.sceneEffectTiming = UcgEffectTiming.None;
            card.sceneEffectId = UcgDemoSceneEffectId.None;
            card.sceneDescription = "";
            card.sceneTurnCost = 0;
            return card;
        }

        static UcgCardData CreateExternalSceneTemplate(
            UcgCardData source,
            int sceneTurnCost,
            UcgDemoSceneEffectId sceneEffectId,
            string sceneDescription)
        {
            if (source == null) return null;

            UcgCardData card = CloneCard(source);
            card.cardCategory = "場景";
            card.sceneTurnCost = sceneTurnCost;
            card.sceneEffectTiming = GetSceneEffectTiming(sceneEffectId);
            card.sceneEffectId = sceneEffectId;
            card.sceneDescription = sceneDescription;
            card.effectTiming = UcgEffectTiming.None;
            card.effectId = UcgDemoEffectId.None;
            card.effectDescription = "";
            return card;
        }

        static UcgCardData CloneCard(UcgCardData source)
        {
            if (source == null) return null;

            return new UcgCardData
            {
                id = source.id,
                sku = source.sku,
                cardName = source.cardName,
                characterName = source.characterName,
                cardCategory = source.cardCategory,
                level = source.level,
                type = source.type,
                seriesText = source.seriesText,
                imageLocal = source.imageLocal,
                imageUrl = source.imageUrl,
                teamTag = source.teamTag,
                cardImage = source.cardImage,
                singleBp = source.singleBp,
                doubleBp = source.doubleBp,
                tripleBp = source.tripleBp,
                quadBp = source.quadBp,
                effectTiming = source.effectTiming,
                effectId = source.effectId,
                effectDescription = source.effectDescription,
                sceneTurnCost = source.sceneTurnCost,
                sceneEffectTiming = source.sceneEffectTiming,
                sceneEffectId = source.sceneEffectId,
                sceneDescription = source.sceneDescription
            };
        }

        static bool ContainsText(string value, string keyword)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(keyword)) return false;
            return value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string FormatCard(UcgCardData card)
        {
            if (card == null) return "null";
            return $"{card.id} / {card.sku} / {card.cardName} / sceneLight={card.sceneTurnCost} / imageLocal={card.imageLocal}";
        }
    }
}
