using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgDeckManager : MonoBehaviour
    {
        public const int DemoTemplateCount = 8;
        public const int DemoRepeatCount = 4;

        public readonly List<UcgCardData> deck = new List<UcgCardData>();
        public readonly List<UcgCardData> playerHand = new List<UcgCardData>();
        public readonly List<UcgCardData> opponentDrawPile = new List<UcgCardData>();
        public readonly List<UcgCardData> opponentHiddenHand = new List<UcgCardData>();
        public UcgDeckDefinition playerDeckDefinition;
        public UcgDeckDefinition opponentDeckDefinition;
        public bool debugValidateDeckImagesOnBuild;
        public bool debugImageValidation;
        public bool debugDeckBuildDetails;
        public bool debugEffectParsing;
        public bool debugEffectParsingVerbose;
        public bool debugDeckOperation;
        UcgDeckProfile _activeProfile;
        UcgDeckProfile _opponentProfile;

        public int RemainingCount => deck.Count;
        public IReadOnlyList<UcgCardData> PlayerDrawPile => deck;
        public IReadOnlyList<UcgCardData> PlayerHand => playerHand;
        public IReadOnlyList<UcgCardData> OpponentDrawPile => opponentDrawPile;
        public IReadOnlyList<UcgCardData> OpponentHiddenHand => opponentHiddenHand;
        public UcgDeckProfile ActiveProfile => _activeProfile;
        public UcgDeckProfile OpponentProfile => _opponentProfile;

        public void ResetDeck(UcgTestMode mode)
        {
            BuildDeckForMode(mode);
        }

        public void BuildDeckForMode(UcgTestMode mode)
        {
            deck.Clear();
            playerHand.Clear();
            opponentDrawPile.Clear();
            opponentHiddenHand.Clear();
            _activeProfile = null;
            _opponentProfile = null;
            UcgEffectParser.debugEffectParsing = debugEffectParsing;
            UcgEffectParser.debugEffectParsingVerbose = debugEffectParsingVerbose;
            UcgEffectParser.debugDeckOperation = debugDeckOperation;
            UcgConditionalBpParser.debugEffectParsingVerbose = debugEffectParsingVerbose;
            UcgEffectParser.ResetUnsupportedSummary();
            UcgConditionalBpParser.ResetUnsupportedSummary();

            if (mode == UcgTestMode.UltramanTest)
            {
                UcgExternalCardDatabase externalDatabase = UcgExternalCardDatabase.GetOrCreate();
                playerDeckDefinition = UcgDigaTutorialDeckFactory.CreatePlayerDeckDefinition();
                opponentDeckDefinition = UcgDigaTutorialDeckFactory.CreateOpponentDeckDefinition();
                _activeProfile = UcgDigaTutorialDeckFactory.BuildProfile(DemoRepeatCount, externalDatabase);
                deck.AddRange(_activeProfile.cards);
                Shuffle(deck);
                if (UcgDeckDefinitionResolver.TryBuildProfile(opponentDeckDefinition, externalDatabase, "ExternalOpponent", out _opponentProfile))
                {
                    opponentDrawPile.AddRange(_opponentProfile.cards);
                    Shuffle(opponentDrawPile);
                    EnsureOpponentOpeningSceneCard(0);
                    DrawOpponentHiddenCards(6 - opponentHiddenHand.Count);
                    LogOpponentDeckDebug();
                }
                if (debugEffectParsing)
                {
                    DebugPrintDeckEffectSummary();
                }
                else
                {
                    DebugPrintUnsupportedEffectSummary();
                }
                if (debugValidateDeckImagesOnBuild)
                {
                    DebugValidateDeckImages();
                }
                Debug.Log($"Deck build completed: playerDeck={deck.Count}, playerHand={playerHand.Count}, opponentDeck={opponentDrawPile.Count}, opponentHand={opponentHiddenHand.Count}");
                return;
            }

            for (int repeat = 0; repeat < DemoRepeatCount; repeat++)
            {
                for (int index = 0; index < DemoTemplateCount; index++)
                {
                    deck.Add(CreateCardForMode(mode, index, repeat));
                }
            }
        }

        public UcgCardData DrawCard()
        {
            if (deck.Count == 0) return null;

            UcgCardData card = deck[0];
            deck.RemoveAt(0);
            return card;
        }

        public List<UcgCardData> DrawCards(int count)
        {
            var cards = new List<UcgCardData>();
            for (int i = 0; i < count; i++)
            {
                UcgCardData card = DrawCard();
                if (card == null) break;
                cards.Add(card);
                playerHand.Add(card);
            }

            return cards;
        }

        public void RemoveFromPlayerHand(UcgCardData card)
        {
            if (card == null) return;
            playerHand.Remove(card);
        }

        public UcgCardData DrawOpponentCard()
        {
            if (opponentDrawPile.Count == 0) return null;

            UcgCardData card = opponentDrawPile[0];
            opponentDrawPile.RemoveAt(0);
            opponentHiddenHand.Add(card);
            return card;
        }

        public void DrawOpponentHiddenCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (DrawOpponentCard() == null) break;
            }
        }

        void EnsureOpponentOpeningSceneCard(int sceneTurnCost)
        {
            int selectedIndex = GetRandomCardIndex(
                opponentDrawPile,
                card => card != null && card.IsSceneCard() && card.sceneTurnCost == sceneTurnCost);
            if (selectedIndex >= 0)
            {
                UcgCardData card = opponentDrawPile[selectedIndex];
                opponentDrawPile.RemoveAt(selectedIndex);
                opponentHiddenHand.Add(card);
                if (debugDeckBuildDetails) Debug.Log($"Opponent guaranteed opening scene: {card.id} / {card.cardName} / sceneLight={card.sceneTurnCost}");
                return;
            }

            Debug.LogWarning($"Opponent opening hand missing {sceneTurnCost}-light scene card");
        }

        void LogOpponentDeckDebug()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Opponent tutorial deck source: {(_opponentProfile != null ? _opponentProfile.source : "Unknown")}");
            builder.AppendLine($"Opponent runtime deck count = {(_opponentProfile != null ? _opponentProfile.cards.Count : 0)}");
            builder.AppendLine($"Opponent draw pile remaining = {opponentDrawPile.Count}");
            builder.AppendLine($"Opponent opening hand count = {opponentHiddenHand.Count}");
            for (int i = 0; i < opponentHiddenHand.Count; i++)
            {
                UcgCardData card = opponentHiddenHand[i];
                builder.AppendLine($"{i + 1}. {FormatOpeningHandCard(card)}");
            }
            builder.AppendLine($"Opponent has 0-light scene = {opponentHiddenHand.Exists(card => card != null && card.IsSceneCard() && card.sceneTurnCost == 0)}");
            builder.AppendLine($"Opponent opening hand duplicated in draw pile = {HandCardsStillInDrawPile(opponentHiddenHand, opponentDrawPile)}");

            if (debugDeckBuildDetails)
            {
                Debug.Log(builder.ToString());
            }
            else
            {
                Debug.Log($"Opening hand completed: opponentHand={opponentHiddenHand.Count}, opponentDrawPile={opponentDrawPile.Count}, has0LightScene={opponentHiddenHand.Exists(card => card != null && card.IsSceneCard() && card.sceneTurnCost == 0)}");
            }
            if (opponentHiddenHand.Count != 6) Debug.LogWarning($"Opponent opening hand count is {opponentHiddenHand.Count}; expected 6.");
            if (opponentDrawPile.Count != 44) Debug.LogWarning($"Opponent draw pile count is {opponentDrawPile.Count}; expected 44 after opening hand.");
        }

        public void DebugPrintDeckEffectSummary()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("UCG Deck Effect Summary");
            AppendDeckEffectSummary(builder, "Player", _activeProfile);
            AppendDeckEffectSummary(builder, "Opponent", _opponentProfile);
            builder.AppendLine(UcgEffectParser.GetUnsupportedSummary());
            builder.AppendLine(UcgConditionalBpParser.GetUnsupportedSummary());
            Debug.Log(builder.ToString());
        }

        public void DebugPrintUnsupportedEffectSummary()
        {
            Debug.Log(
                "UCG Effect Parsing Summary\n" +
                UcgEffectParser.GetUnsupportedSummary() + "\n" +
                UcgConditionalBpParser.GetUnsupportedSummary());
        }

        public void DebugValidateDeckImages()
        {
            StopCoroutine(nameof(DebugValidateDeckImagesRoutine));
            StartCoroutine(DebugValidateDeckImagesRoutine());
        }

        [ContextMenu("Debug Validate All Deck Images")]
        public void DebugValidateAllDeckImages()
        {
            DebugValidateDeckImages();
        }

        [ContextMenu("Debug Validate Tutorial Deck Images")]
        public void DebugValidateTutorialDeckImages()
        {
            DebugValidateDeckImages();
        }

        IEnumerator DebugValidateDeckImagesRoutine()
        {
            var cards = CollectUniqueDeckCards();
            UcgExternalCardDatabase externalDatabase = UcgExternalCardDatabase.GetOrCreate();
            UcgCardImageLoader loader = UcgCardImageLoader.GetOrCreate();
            UcgCardImageIndex imageIndex = UcgCardImageIndex.GetOrCreate();
            imageIndex.BuildIndex(
                externalDatabase != null ? externalDatabase.publicRootPath : "",
                loader != null ? loader.unityPngRootPath : "");
            var missingReports = new List<string>();
            var statusReports = new List<string>();
            int resolvedCount = 0;
            int missingCount = 0;
            int decodeFailedCount = 0;
            int fallbackSpriteCount = 0;

            if (debugImageValidation)
            {
                Debug.Log(imageIndex.FormatRootDiagnostics());
                Debug.Log(
                    imageIndex.FormatCardDiagnostics("BP05-001") +
                    imageIndex.FormatCardDiagnostics("BP05-002") +
                    imageIndex.FormatCardDiagnostics("BP05-003") +
                    imageIndex.FormatCardDiagnostics("BP05-005") +
                    imageIndex.FormatCardDiagnostics("BP05-008") +
                    imageIndex.FormatCardDiagnostics("SD01-005") +
                    imageIndex.FormatCardDiagnostics("BP01-001") +
                    imageIndex.FormatCardDiagnostics("BP01-004") +
                    imageIndex.FormatCardDiagnostics("BP01-062") +
                    imageIndex.FormatCardDiagnostics("BP01-105"));
                Debug.Log($"DebugValidateDeckImages start: uniqueCards={cards.Count}");
            }
            for (int i = 0; i < cards.Count; i++)
            {
                UcgCardData card = cards[i];
                if (card == null) continue;

                string imageLocal = card.imageLocal;
                bool indexResolved = imageIndex.TryResolve(card.id, out UcgCardImageIndex.ImageIndexEntry indexEntry);
                string indexReport = indexResolved ? imageIndex.FormatEntry(indexEntry) : "<no index entry>";
                string indexResolvedPath = indexResolved ? indexEntry.selectedPath : "";
                string selectedSourceType = indexResolved ? imageIndex.GetSelectedSourceType(indexEntry) : "placeholder";
                string selectedPath = indexResolvedPath;
                var candidates = loader.BuildImageCandidates(externalDatabase, card, imageLocal);
                string candidateReport = loader.FormatCandidates(candidates);
                string triedPaths = loader.FormatCandidatePaths(candidates);
                string resolvedPath = externalDatabase != null ? externalDatabase.ResolveImageFilePath(imageLocal) : "";
                bool fileExists = !string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath);
                bool anyCandidateExists = fileExists || (indexResolved && File.Exists(indexResolvedPath)) || HasExistingImageCandidate(candidates);
                bool done = false;
                Sprite loadedSprite = null;

                if (!indexResolved && string.IsNullOrWhiteSpace(imageLocal) && string.IsNullOrWhiteSpace(card.imageUrl))
                {
                    missingCount++;
                    fallbackSpriteCount++;
                    statusReports.Add($"Fallback sprite used | {card.id} | {card.cardName} | imageLocal=<empty>");
                    missingReports.Add(
                        $"{card.id} {card.cardName}\n" +
                        $"imageLocal=<empty>\n" +
                        $"imageUrl={card.imageUrl}\n" +
                        $"indexResolvedPath=<none>\n" +
                        "tried=<none>");
                    continue;
                }

                if (debugImageValidation)
                {
                    Debug.Log(
                        "Deck image validation candidates:\n" +
                        $"card={card.id} {card.cardName}\n" +
                        $"imageLocal={imageLocal}\n" +
                        $"imageUrl={card.imageUrl}\n" +
                        $"indexResolved={indexResolved}\n" +
                        $"indexResolvedPath={indexResolvedPath}\n" +
                        $"selectedSourceType={selectedSourceType}\n" +
                        $"indexReport=\n{indexReport}\n" +
                        $"fallbackCandidates=\n{candidateReport}");
                }

                loader.LoadCardImage(card, sprite =>
                {
                    loadedSprite = sprite;
                    done = true;
                });

                while (!done)
                {
                    yield return null;
                }

                if (loadedSprite == null)
                {
                    missingCount++;
                    fallbackSpriteCount++;
                    string status = anyCandidateExists ? "Decode failed" : "Missing file";
                    if (anyCandidateExists) decodeFailedCount++;
                    statusReports.Add($"{status} | {card.id} | {card.cardName} | imageLocal={imageLocal}");
                    missingReports.Add(
                        $"{card.id} {card.cardName}\n" +
                        $"imageLocal={imageLocal}\n" +
                        $"imageUrl={card.imageUrl}\n" +
                        $"indexResolvedPath={indexResolvedPath}\n" +
                        $"selectedSourceType={selectedSourceType}\n" +
                        $"selectedPath={selectedPath}\n" +
                        $"exists={anyCandidateExists}\n" +
                        $"decodeResult={status}\n" +
                        "fallbackUsed=placeholder sprite expected\n" +
                        $"indexReport:\n{indexReport}\n" +
                        $"fallbackTried:\n{candidateReport}");
                    if (debugImageValidation)
                    {
                        Debug.LogWarning(
                            "[UCG CardImage Missing]\n" +
                            $"cardId={card.id}\n" +
                            $"cardName={card.cardName}\n" +
                            $"imageLocal={imageLocal}\n" +
                            $"triedPaths=\n{triedPaths}" +
                            $"exists={anyCandidateExists}\n" +
                            $"decodeResult={status}\n" +
                            "fallbackUsed=placeholder sprite expected");
                    }
                }
                else
                {
                    resolvedCount++;
                    statusReports.Add($"OK | {card.id} | {card.cardName} | imageLocal={imageLocal} | sprite={loadedSprite.name}");
                    if (debugImageValidation)
                    {
                        Debug.Log(
                            "Deck image validation success:\n" +
                            $"card={card.id} {card.cardName}\n" +
                            $"imageLocal={imageLocal}\n" +
                            $"imageUrl={card.imageUrl}\n" +
                            $"indexResolved={indexResolved}\n" +
                            $"indexResolvedPath={indexResolvedPath}\n" +
                            $"selectedSourceType={selectedSourceType}\n" +
                            $"resolvedLocalPath={resolvedPath}\n" +
                            $"fileExists={fileExists}\n" +
                            $"sprite={loadedSprite.name}");
                    }
                }
            }

            Debug.Log(
                "Deck image validation:\n" +
                $"unique cards = {cards.Count}\n" +
                $"resolved = {resolvedCount}\n" +
                $"missing = {missingCount}\n" +
                $"decode failed = {decodeFailedCount}\n" +
                $"fallback sprite used = {fallbackSpriteCount}\n" +
                $"failed load count = {missingCount}");
            if (statusReports.Count > 0)
            {
                var statusBuilder = new System.Text.StringBuilder();
                statusBuilder.AppendLine("[UCG DeckImage Diagnostics]");
                for (int i = 0; i < statusReports.Count; i++)
                {
                    statusBuilder.AppendLine(statusReports[i]);
                }

                Debug.Log(statusBuilder.ToString());
            }

            if (missingReports.Count > 0)
            {
                var missingBuilder = new System.Text.StringBuilder();
                missingBuilder.AppendLine("Missing deck images:");
                for (int i = 0; i < missingReports.Count; i++)
                {
                    missingBuilder.AppendLine($"* {missingReports[i]}");
                }

                Debug.LogWarning(missingBuilder.ToString());
            }

            Debug.Log("DebugValidateDeckImages complete.");
        }

        static bool HasExistingImageCandidate(List<UcgCardImageLoader.ImageCandidate> candidates)
        {
            if (candidates == null) return false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].exists) return true;
            }

            return false;
        }

        List<UcgCardData> CollectUniqueDeckCards()
        {
            var cards = new List<UcgCardData>();
            var seen = new HashSet<string>();
            AddUniqueProfileCards(cards, seen, _activeProfile);
            AddUniqueProfileCards(cards, seen, _opponentProfile);
            if (cards.Count == 0)
            {
                UcgExternalCardDatabase externalDatabase = UcgExternalCardDatabase.GetOrCreate();
                AddUniqueDefinitionCards(cards, seen, UcgDigaTutorialDeckFactory.CreatePlayerDeckDefinition(), externalDatabase);
                AddUniqueDefinitionCards(cards, seen, UcgDigaTutorialDeckFactory.CreateOpponentDeckDefinition(), externalDatabase);
            }

            return cards;
        }

        void AddUniqueDefinitionCards(
            List<UcgCardData> cards,
            HashSet<string> seen,
            UcgDeckDefinition definition,
            UcgExternalCardDatabase externalDatabase)
        {
            if (cards == null || seen == null || definition == null || definition.cards == null || externalDatabase == null) return;
            externalDatabase.LoadDatabase();

            for (int i = 0; i < definition.cards.Count; i++)
            {
                UcgDeckDefinitionEntry entry = definition.cards[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !seen.Add(entry.id)) continue;

                UcgCardData sourceCard = externalDatabase.GetCardById(entry.id);
                if (sourceCard == null)
                {
                    cards.Add(new UcgCardData
                    {
                        id = entry.id,
                        cardName = "<missing card data>"
                    });
                    continue;
                }

                cards.Add(UcgDeckDefinitionResolver.CloneCard(sourceCard));
            }
        }

        void AddUniqueProfileCards(List<UcgCardData> cards, HashSet<string> seen, UcgDeckProfile profile)
        {
            if (cards == null || seen == null || profile == null || profile.cards == null) return;

            for (int i = 0; i < profile.cards.Count; i++)
            {
                UcgCardData card = profile.cards[i];
                if (card == null) continue;
                string key = !string.IsNullOrWhiteSpace(card.id) ? card.id : card.imageLocal;
                if (string.IsNullOrWhiteSpace(key) || !seen.Add(key)) continue;
                cards.Add(card);
            }
        }

        void AppendDeckEffectSummary(System.Text.StringBuilder builder, string label, UcgDeckProfile profile)
        {
            builder.AppendLine($"{label} deck: id={(profile != null ? profile.deckId : "null")}, cards={(profile != null && profile.cards != null ? profile.cards.Count : 0)}");
            if (profile == null || profile.cards == null) return;

            var seenIds = new HashSet<string>();
            var uniqueEffectCards = new List<UcgCardData>();
            for (int i = 0; i < profile.cards.Count; i++)
            {
                UcgCardData card = profile.cards[i];
                if (card == null) continue;
                string key = string.IsNullOrWhiteSpace(card.id) ? $"{card.sku}:{card.cardName}" : card.id;
                if (!seenIds.Add(key)) continue;

                string effectText = card.IsSceneCard() ? card.sceneDescription : card.effectDescription;
                if (string.IsNullOrWhiteSpace(effectText)) continue;
                uniqueEffectCards.Add(card);
            }

            int enterCount = 0;
            int battleCount = 0;
            int continuousCount = 0;
            int stackRequirementCount = 0;
            int drawCardsCount = 0;
            int revealTopCardsCount = 0;
            int putToBottomCount = 0;
            int unsupportedCount = 0;
            for (int i = 0; i < uniqueEffectCards.Count; i++)
            {
                UcgCardData card = uniqueEffectCards[i];
                UcgEffectRule rule = UcgEffectParser.ParsePrimaryRule(card);
                UcgConditionalBpRule conditionalRule = UcgConditionalBpParser.Parse(card, profile.cards);

                switch (rule.effectCategory)
                {
                    case UcgEffectCategory.EnterEffect:
                        enterCount++;
                        break;
                    case UcgEffectCategory.BattleEffect:
                        battleCount++;
                        break;
                    case UcgEffectCategory.ContinuousEffect:
                        continuousCount++;
                        break;
                }

                if (rule.requiredStackCount > 0 || conditionalRule.requiredStackCount > 0) stackRequirementCount++;
                if (rule.supported && rule.actionType == UcgEffectActionType.DrawCards) drawCardsCount++;
                if (rule.supported && rule.actionType == UcgEffectActionType.DeckOperation && rule.deckOperation != null)
                {
                    if (rule.deckOperation.operationType == UcgDeckOperationType.RevealTopSelectToHandRestTrash) revealTopCardsCount++;
                    if (rule.deckOperation.operationType == UcgDeckOperationType.DrawThenSelectBottom
                        || rule.deckOperation.operationType == UcgDeckOperationType.DrawThenPutHandToBottom
                        || rule.deckOperation.operationType == UcgDeckOperationType.SelectHandToBottomThenDrawSameCount) putToBottomCount++;
                }
                if (!rule.supported || UcgConditionalBpParser.ShouldWarnUnsupportedConditional(card) && !conditionalRule.supported) unsupportedCount++;
            }

            builder.AppendLine($"{label} effect summary: uniqueEffectCards={uniqueEffectCards.Count}, enter={enterCount}, battle={battleCount}, continuous={continuousCount}, stackLimited={stackRequirementCount}, drawCards={drawCardsCount}, revealTopSelect={revealTopCardsCount}, putToBottom={putToBottomCount}, unsupported={unsupportedCount}");
            if (!debugEffectParsingVerbose) return;

            for (int i = 0; i < uniqueEffectCards.Count; i++)
            {
                UcgCardData card = uniqueEffectCards[i];
                string effectText = card.IsSceneCard() ? card.sceneDescription : card.effectDescription;

                UcgEffectRule rule = UcgEffectParser.ParsePrimaryRule(card);
                UcgConditionalBpRule conditionalRule = UcgConditionalBpParser.Parse(card, profile.cards);
                string conditionalSummary = conditionalRule.supported
                    ? $"{conditionalRule.category}, keyword={conditionalRule.keyword}, bp={conditionalRule.bpAmount}, stepUp={conditionalRule.isStepUp}, requiredStack={(conditionalRule.requireExactStackCount ? "==" : ">=")}{conditionalRule.requiredStackCount}"
                    : $"unsupported: {conditionalRule.unsupportedReason}";
                builder.AppendLine(
                    $"{label}: {card.id} / {card.cardName} / effect={effectText} / " +
                    $"{UcgEffectParser.DescribeRule(rule)} / conditional={conditionalSummary} / executable={(card.IsSceneCard() ? card.sceneEffectId.ToString() : card.effectId.ToString())}");
            }
        }

        public List<UcgCardData> DrawDigaTutorialOpeningHand(int handSize = 6)
        {
            var hand = new List<UcgCardData>();

            UcgCardData digaLevel1 = TakeSpecificCard(_activeProfile != null ? _activeProfile.guaranteedBaseCard : null);
            if (digaLevel1 == null) digaLevel1 = TakeRandomCard(IsDigaLevel1);
            if (digaLevel1 == null)
            {
                Debug.LogWarning("Tutorial opening hand missing Diga Lv.1");
            }
            else
            {
                hand.Add(digaLevel1);
            }

            UcgCardData digaLevel2 = TakeSpecificCard(_activeProfile != null ? _activeProfile.guaranteedUpgradeCard : null);
            if (!IsDigaLevel2For(digaLevel2, digaLevel1))
            {
                if (digaLevel2 != null) deck.Insert(0, digaLevel2);
                digaLevel2 = TakeRandomCard(card => IsDigaLevel2For(card, digaLevel1));
            }
            if (digaLevel2 == null)
            {
                Debug.LogWarning("Tutorial opening hand missing Diga Lv.2");
            }
            else
            {
                hand.Add(digaLevel2);
            }

            UcgCardData sceneCard = TakeSpecificCard(_activeProfile != null ? _activeProfile.guaranteedSceneCard : null);
            if (sceneCard == null) sceneCard = TakeRandomCard(IsTwoLightSceneCard);
            if (sceneCard == null)
            {
                Debug.LogWarning("Tutorial opening hand missing scene card");
            }
            else
            {
                hand.Add(sceneCard);
            }

            while (hand.Count < handSize)
            {
                UcgCardData card = DrawCard();
                if (card == null) break;
                hand.Add(card);
            }

            playerHand.AddRange(hand);
            LogDigaTutorialOpeningHand(hand, digaLevel1, digaLevel2, sceneCard);
            return hand;
        }

        UcgCardData TakeSpecificCard(UcgCardData targetCard)
        {
            if (targetCard == null) return null;

            for (int i = 0; i < deck.Count; i++)
            {
                if (!ReferenceEquals(deck[i], targetCard)) continue;

                UcgCardData card = deck[i];
                deck.RemoveAt(i);
                return card;
            }

            return null;
        }

        UcgCardData TakeRandomCard(System.Predicate<UcgCardData> predicate)
        {
            if (predicate == null) return null;

            int selectedIndex = GetRandomCardIndex(deck, predicate);
            if (selectedIndex < 0) return null;

            UcgCardData card = deck[selectedIndex];
            deck.RemoveAt(selectedIndex);
            return card;
        }

        int GetRandomCardIndex(List<UcgCardData> cards, System.Predicate<UcgCardData> predicate)
        {
            if (cards == null || predicate == null) return -1;

            var candidateIndexes = new List<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (predicate(cards[i])) candidateIndexes.Add(i);
            }

            if (candidateIndexes.Count == 0) return -1;
            return candidateIndexes[Random.Range(0, candidateIndexes.Count)];
        }

        void Shuffle(List<UcgCardData> cards)
        {
            if (cards == null) return;

            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                UcgCardData temp = cards[i];
                cards[i] = cards[swapIndex];
                cards[swapIndex] = temp;
            }
        }

        bool IsDigaLevel1(UcgCardData card)
        {
            return card != null
                && IsDigaCard(card)
                && card.level == 1;
        }

        bool IsDigaLevel2For(UcgCardData card, UcgCardData level1Card)
        {
            return card != null
                && level1Card != null
                && card.characterName == level1Card.characterName
                && IsDigaCard(card)
                && card.level == level1Card.level + 1;
        }

        bool IsTwoLightSceneCard(UcgCardData card)
        {
            return card != null && card.IsSceneCard() && card.sceneTurnCost == 2;
        }

        bool IsDigaCard(UcgCardData card)
        {
            if (card == null) return false;
            return ContainsText(card.characterName, "迪卡") || ContainsText(card.cardName, "迪卡");
        }

        void LogDigaTutorialOpeningHand(List<UcgCardData> hand, UcgCardData digaLevel1, UcgCardData digaLevel2, UcgCardData sceneCard)
        {
            bool hasDigaLv1 = hand != null && hand.Exists(IsDigaLevel1);
            bool hasDigaLv2 = hand != null && hand.Exists(card => IsDigaLevel2For(card, digaLevel1));
            bool hasSceneCard = hand != null && hand.Exists(card => card != null && card.IsSceneCard());
            bool sceneIsTwoLight = sceneCard != null && sceneCard.IsSceneCard() && sceneCard.sceneTurnCost == 2;

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Diga tutorial deck source: {(_activeProfile != null ? _activeProfile.source : "Unknown")}");
            builder.AppendLine($"Diga tutorial guaranteed scene: {FormatSceneDebugCard(sceneCard)}");
            builder.AppendLine("Player opening hand:");
            if (hand != null)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    UcgCardData card = hand[i];
                    builder.AppendLine($"{i + 1}. {FormatOpeningHandCard(card)}");
                }
            }

            builder.AppendLine($"Has Diga Lv.1 = {hasDigaLv1}");
            builder.AppendLine($"Has Diga Lv.2 = {hasDigaLv2}");
            builder.AppendLine($"Has Scene Card = {hasSceneCard}");
            builder.AppendLine($"Scene Light = {(sceneCard != null ? sceneCard.sceneTurnCost.ToString() : "None")}");
            builder.AppendLine($"Diga tutorial scene is 2-light: {sceneIsTwoLight}");
            builder.AppendLine($"Player draw pile count after opening hand = {deck.Count}");
            builder.AppendLine($"Player opening hand duplicated in draw pile = {HandCardsStillInDrawPile(hand, deck)}");
            builder.AppendLine("Turn 1 allowedSceneLight = 0");
            builder.AppendLine("Turn 2 allowedSceneLight = 1");
            builder.AppendLine("Turn 3 allowedSceneLight = 2");
            if (debugDeckBuildDetails)
            {
                Debug.Log(builder.ToString());
                DebugPrintDigaTutorialTargetSceneImage(sceneCard);
                DebugPrintDigaCardVisualSources(hand);
            }
            else
            {
                Debug.Log($"Opening hand completed: playerHand={(hand != null ? hand.Count : 0)}, playerDrawPile={deck.Count}, hasDigaLv1={hasDigaLv1}, hasDigaLv2={hasDigaLv2}, has2LightScene={sceneIsTwoLight}");
            }

            if (digaLevel1 == null) Debug.LogWarning("Tutorial opening hand missing Diga Lv.1");
            if (digaLevel2 == null) Debug.LogWarning("Tutorial opening hand missing Diga Lv.2");
            if (sceneCard == null) Debug.LogWarning("Tutorial opening hand missing scene card");
            if (!sceneIsTwoLight) Debug.LogWarning("Tutorial opening hand scene is not 2-light");
            if (hand == null || hand.Count != 6) Debug.LogWarning($"Player opening hand count is {hand?.Count ?? 0}; expected 6.");
            if (deck.Count != 44) Debug.LogWarning($"Player draw pile count is {deck.Count}; expected 44 after opening hand.");
        }

        bool HandCardsStillInDrawPile(List<UcgCardData> hand, List<UcgCardData> drawPile)
        {
            if (hand == null || drawPile == null) return false;

            for (int i = 0; i < hand.Count; i++)
            {
                UcgCardData card = hand[i];
                if (card == null) continue;
                for (int j = 0; j < drawPile.Count; j++)
                {
                    if (ReferenceEquals(card, drawPile[j])) return true;
                }
            }

            return false;
        }

        string FormatOpeningHandCard(UcgCardData card)
        {
            if (card == null) return "null";
            return $"{card.id} / {card.sku} / {card.cardName} / {card.characterName} / Lv.{card.level} / sceneLight={card.sceneTurnCost} / {card.imageLocal}";
        }

        string FormatSceneDebugCard(UcgCardData card)
        {
            if (card == null) return "null";
            return $"{card.id} / {card.sku} / {card.cardName} / {card.sceneTurnCost} / {card.imageLocal}";
        }

        void DebugPrintDigaTutorialTargetSceneImage(UcgCardData sceneCard)
        {
            if (sceneCard == null || sceneCard.cardName != UcgDigaTutorialDeckFactory.TargetTutorialSceneName) return;

            UcgExternalCardDatabase externalDatabase = UcgExternalCardDatabase.GetOrCreate();
            string publicRootPath = externalDatabase != null ? externalDatabase.publicRootPath : "";
            string fullLocalPath = externalDatabase != null ? externalDatabase.ResolveImageFilePath(sceneCard.imageLocal) : "";
            string fileUrl = externalDatabase != null ? externalDatabase.ResolveImageFileUrl(sceneCard.imageLocal) : "";
            bool fileExists = !string.IsNullOrWhiteSpace(fullLocalPath) && File.Exists(fullLocalPath);

            Debug.Log(
                "Diga tutorial target scene image debug:\n" +
                $"id = {sceneCard.id}\n" +
                $"sku = {sceneCard.sku}\n" +
                $"cardName = {sceneCard.cardName}\n" +
                $"cardCategory = {sceneCard.cardCategory}\n" +
                $"sceneTurnCost = {sceneCard.sceneTurnCost}\n" +
                $"imageLocal = {sceneCard.imageLocal}\n" +
                $"publicRootPath = {publicRootPath}\n" +
                $"fullLocalPath = {fullLocalPath}\n" +
                $"fileUrl = {fileUrl}\n" +
                $"File.Exists = {fileExists}");

            if (!fileExists)
            {
                Debug.LogWarning($"Diga tutorial target scene image file missing: {fullLocalPath}");
            }
        }

        void DebugPrintDigaCardVisualSources(List<UcgCardData> hand)
        {
            if (hand == null) return;

            for (int i = 0; i < hand.Count; i++)
            {
                UcgCardData card = hand[i];
                if (card == null) continue;
                if (!IsDigaCard(card) && card.cardName != UcgDigaTutorialDeckFactory.TargetTutorialSceneName) continue;

                Debug.Log(
                    "Diga card visual source:\n" +
                    $"cardName = {card.cardName}\n" +
                    $"imageLocal = {card.imageLocal}\n" +
                    $"cardImage assigned = {card.cardImage != null}\n" +
                    $"expected visual source = {(card.IsExternalCard() ? "ExternalImage" : "LocalTestSprite")}");
            }
        }

        bool ContainsText(string value, string keyword)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(keyword)) return false;
            return value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        UcgCardData CreateCardForMode(UcgTestMode mode, int index, int repeat)
        {
            UcgCardData card;
            switch (mode)
            {
                case UcgTestMode.MonsterAlienTest:
                    card = CreateMonsterAlienTestCard(index);
                    break;
                case UcgTestMode.TeamTest:
                    card = CreateTeamTestCard(index);
                    break;
                default:
                    card = UcgDigaTutorialDeckFactory.CreateTemplateCard(index);
                    break;
            }

            card.id = $"{card.id}-deck-{repeat + 1}-{index + 1}";
            return card;
        }

        UcgCardData CreateMonsterAlienTestCard(int index)
        {
            if (index == 0)
            {
                return CreateSceneCard(index, "怪獸島", 1, UcgDemoSceneEffectId.PlayerAllBpPlus500, "持有者所有角色 BP +500");
            }

            if (index == 1)
            {
                return CreateSceneCard(index, "怪獸墓場", 2, UcgDemoSceneEffectId.PlayerAllBpPlus500, "持有者所有角色 BP +500");
            }

            int cardIndex = index - 2;
            switch (index)
            {
                case 2:
                    return CreateDemoCard(cardIndex, "測試哥莫拉 Lv.5", "哥莫拉", "怪獸", 5, "");
                case 3:
                    return CreateDemoCard(cardIndex, "測試艾雷王 Lv.5", "艾雷王", "怪獸", 5, "");
                case 4:
                    return CreateDemoCard(cardIndex, "測試哥莫拉 Lv.6", "哥莫拉", "怪獸", 6, "");
                case 5:
                    return CreateDemoCard(cardIndex, "測試艾雷王 Lv.6", "艾雷王", "怪獸", 6, "");
                case 6:
                    return CreateDemoCard(cardIndex, "測試哥莫拉 Lv.7", "哥莫拉", "怪獸", 7, "");
                default:
                    return CreateDemoCard(cardIndex, "測試巴爾坦星人 Lv.5", "巴爾坦星人", "宇宙人", 5, "");
            }
        }

        UcgCardData CreateTeamTestCard(int index)
        {
            if (index == 0)
            {
                return CreateSceneCard(index, "作戰基地", 1, UcgDemoSceneEffectId.ActiveLanePlayerBpPlus1000, "本回合最新 Lane 的持有者角色 BP +1000");
            }

            if (index == 1)
            {
                return CreateSceneCard(index, "聯合作戰指揮所", 2, UcgDemoSceneEffectId.PlayerAllBpPlus500, "持有者所有角色 BP +500");
            }

            int cardIndex = index - 2;
            switch (index)
            {
                case 2:
                    return CreateDemoCard(cardIndex, "測試三人突擊隊 A Lv.1", "隊員A", "超人力霸王", 1, "三人突擊隊");
                case 3:
                    return CreateDemoCard(cardIndex, "測試三人突擊隊 B Lv.2", "隊員B", "超人力霸王", 2, "三人突擊隊");
                case 4:
                    return CreateDemoCard(cardIndex, "測試三人突擊隊 C Lv.3", "隊員C", "超人力霸王", 3, "三人突擊隊");
                case 5:
                    return CreateDemoCard(cardIndex, "測試非突擊隊 Lv.2", "其他角色", "超人力霸王", 2, "");
                case 6:
                    return CreateDemoCard(cardIndex, "測試三人突擊隊 D Lv.1", "隊員D", "超人力霸王", 1, "三人突擊隊");
                default:
                    return CreateDemoCard(cardIndex, "測試三人突擊隊 E Lv.3", "隊員E", "超人力霸王", 3, "三人突擊隊");
            }
        }

        UcgCardData CreateSceneCard(int index, string cardName, int sceneTurnCost, UcgDemoSceneEffectId sceneEffectId, string sceneDescription)
        {
            return new UcgCardData
            {
                id = $"ucg-demo-scene-{index + 1}",
                cardName = cardName,
                characterName = "",
                cardCategory = "場景",
                level = 0,
                teamTag = "",
                sceneTurnCost = sceneTurnCost,
                sceneEffectTiming = GetSceneEffectTiming(sceneEffectId),
                sceneEffectId = sceneEffectId,
                sceneDescription = sceneDescription,
            };
        }

        UcgEffectTiming GetSceneEffectTiming(UcgDemoSceneEffectId sceneEffectId)
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

        UcgCardData CreateDemoCard(int index, string cardName, string characterName, string cardCategory, int level, string teamTag)
        {
            var card = new UcgCardData
            {
                id = $"ucg-demo-template-{index + 1}",
                cardName = cardName,
                characterName = characterName,
                cardCategory = cardCategory,
                level = level,
                teamTag = teamTag,
            };

            ApplyDemoBp(card);
            ApplyDemoEffect(card);
            return card;
        }

        void ApplyDemoEffect(UcgCardData card)
        {
            if (card == null) return;

            card.effectId = UcgDemoEffectId.None;
            card.effectTiming = UcgEffectTiming.None;
            card.effectDescription = "";

            if (card.characterName == "阿古茹" && card.level == 1)
            {
                SetDemoEffect(card, UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealDrawOne, "藍色：登場時，抽 1 張牌");
            }
            else if (card.characterName == "阿古茹" && card.level == 2)
            {
                SetDemoEffect(card, UcgEffectTiming.Activated, UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000, "紅色：[發動] 選擇對手一條 Lane，BP -1000");
            }
            else if (card.characterName == "哥莫拉" && card.level == 5)
            {
                SetDemoEffect(card, UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealSelfBpPlus1000, "藍色：登場時，本回合此 Lane 我方 BP +1000");
            }
            else if (card.characterName == "哥莫拉" && card.level == 6)
            {
                SetDemoEffect(card, UcgEffectTiming.Activated, UcgDemoEffectId.ActivatedChooseOwnLaneBpPlus1000, "紅色：[發動] 選擇我方一條 Lane，BP +1000");
            }
            else if (card.characterName == "巴爾坦星人" && card.level == 5)
            {
                SetDemoEffect(card, UcgEffectTiming.Activated, UcgDemoEffectId.ActivatedChooseOpponentLaneBpMinus1000, "紅色：[發動] 選擇對手一條 Lane，BP -1000");
            }
            else if (card.characterName == "隊員A" && card.level == 1)
            {
                SetDemoEffect(card, UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealSelfBpPlus1000, "藍色：登場時，本回合此 Lane 我方 BP +1000");
            }
            else if (card.characterName == "隊員B" && card.level == 2)
            {
                SetDemoEffect(card, UcgEffectTiming.OnRevealOrEnter, UcgDemoEffectId.OnRevealDrawOne, "藍色：登場時，抽 1 張牌");
            }
        }

        void SetDemoEffect(UcgCardData card, UcgEffectTiming timing, UcgDemoEffectId effectId, string description)
        {
            card.effectTiming = timing;
            card.effectId = effectId;
            card.effectDescription = description;
        }

        void ApplyDemoBp(UcgCardData card)
        {
            if (card == null) return;

            if (card.cardCategory == "怪獸" || card.cardCategory == "宇宙人")
            {
                switch (card.level)
                {
                    case 6:
                        SetBp(card, 7000, 10000, 12000, 14000);
                        break;
                    case 7:
                        SetBp(card, 8000, 11000, 13000, 15000);
                        break;
                    default:
                        SetBp(card, 6000, 9000, 11000, 13000);
                        break;
                }
            }
            else
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
        }

        void SetBp(UcgCardData card, int singleBp, int doubleBp, int tripleBp, int quadBp)
        {
            card.singleBp = singleBp;
            card.doubleBp = doubleBp;
            card.tripleBp = tripleBp;
            card.quadBp = quadBp;
        }
    }
}
