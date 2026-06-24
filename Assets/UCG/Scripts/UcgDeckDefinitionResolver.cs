using UnityEngine;

namespace UCG
{
    public static class UcgDeckDefinitionResolver
    {
        public const int RequiredDeckCount = 50;

        public static bool TryBuildProfile(
            UcgDeckDefinition definition,
            UcgExternalCardDatabase database,
            string sourceLabel,
            out UcgDeckProfile profile)
        {
            profile = null;
            if (definition == null)
            {
                Debug.LogWarning("Deck definition is null.");
                return false;
            }

            if (definition.TotalCount != RequiredDeckCount)
            {
                Debug.LogWarning($"Deck definition {definition.deckId} has {definition.TotalCount} cards; expected {RequiredDeckCount}.");
            }

            if (database == null || !database.LoadDatabase())
            {
                Debug.LogWarning($"Deck definition {definition.deckId} could not load external cards database.");
                return false;
            }

            profile = new UcgDeckProfile
            {
                deckId = definition.deckId,
                deckName = definition.name,
                source = sourceLabel
            };

            for (int i = 0; i < definition.cards.Count; i++)
            {
                UcgDeckDefinitionEntry entry = definition.cards[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.count <= 0) continue;

                UcgCardData sourceCard = database.GetCardById(entry.id);
                if (sourceCard == null)
                {
                    Debug.LogWarning($"Deck definition {definition.deckId} card id not found: {entry.id}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sourceCard.imageLocal))
                {
                    Debug.LogWarning($"Deck definition {definition.deckId} card has no imageLocal: {sourceCard.id} / {sourceCard.cardName}");
                }

                for (int copy = 0; copy < entry.count; copy++)
                {
                    UcgCardData runtimeCard = CloneCard(sourceCard);
                    UcgEffectParser.ApplyExecutableDemoMapping(runtimeCard);
                    profile.cards.Add(runtimeCard);
                }
            }

            Debug.Log($"Deck definition resolved: id={definition.deckId}, source={sourceLabel}, runtimeCards={profile.cards.Count}");
            return profile.cards.Count > 0;
        }

        public static UcgCardData CloneCard(UcgCardData source)
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
    }
}
