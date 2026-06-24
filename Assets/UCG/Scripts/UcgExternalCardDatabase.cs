using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgExternalCardDatabase : MonoBehaviour
    {
        public string cardsJsonPath = "/Users/xiaoma/UCGShared/ucg-tool-data/cards.json/cards.json";
        public string publicRootPath = "/Users/xiaoma/UCGShared/ucg-tool-public";
        public bool debugLoadOnStart;

        readonly Dictionary<string, UcgCardData> _cardsById = new Dictionary<string, UcgCardData>();
        readonly Dictionary<string, UcgCardData> _cardsBySku = new Dictionary<string, UcgCardData>();
        readonly List<UcgCardData> _cards = new List<UcgCardData>();

        bool _loaded;

        public int Count => _cards.Count;
        public IReadOnlyList<UcgCardData> Cards => _cards;

        public static UcgExternalCardDatabase Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            if (debugLoadOnStart)
            {
                DebugLoadExternalCards();
            }
        }

        public static UcgExternalCardDatabase GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<UcgExternalCardDatabase>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var databaseObject = new GameObject("UCG External Card Database", typeof(UcgExternalCardDatabase));
            DontDestroyOnLoad(databaseObject);
            Instance = databaseObject.GetComponent<UcgExternalCardDatabase>();
            return Instance;
        }

        public bool LoadDatabase()
        {
            if (_loaded) return true;

            _cards.Clear();
            _cardsById.Clear();
            _cardsBySku.Clear();

            string resolvedPath = ResolveCardsJsonPath();
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                Debug.LogWarning($"UCG external cards.json not found: {cardsJsonPath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(resolvedPath);
                var wrapper = JsonUtility.FromJson<ExternalCardList>($"{{\"cards\":{json}}}");
                if (wrapper == null || wrapper.cards == null)
                {
                    Debug.LogWarning($"UCG external cards.json parse failed: {resolvedPath}");
                    return false;
                }

                for (int i = 0; i < wrapper.cards.Length; i++)
                {
                    UcgCardData cardData = ConvertExternalCard(wrapper.cards[i]);
                    if (cardData == null) continue;

                    _cards.Add(cardData);
                    AddCardKey(_cardsById, cardData.id, cardData);
                    AddCardKey(_cardsBySku, cardData.sku, cardData);
                    if (string.IsNullOrWhiteSpace(cardData.id) && !string.IsNullOrWhiteSpace(cardData.sku))
                    {
                        AddCardKey(_cardsById, cardData.sku, cardData);
                    }
                    if (string.IsNullOrWhiteSpace(cardData.sku) && !string.IsNullOrWhiteSpace(cardData.id))
                    {
                        AddCardKey(_cardsBySku, cardData.id, cardData);
                    }
                }

                _loaded = true;
                Debug.Log($"UCG external card database loaded: count={_cards.Count}, path={resolvedPath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"UCG external cards.json load failed: {resolvedPath}\n{exception.Message}");
                return false;
            }
        }

        public UcgCardData GetCardById(string id)
        {
            if (!_loaded) LoadDatabase();
            if (string.IsNullOrWhiteSpace(id)) return null;
            _cardsById.TryGetValue(id, out UcgCardData cardData);
            return cardData;
        }

        public UcgCardData GetCardBySku(string sku)
        {
            if (!_loaded) LoadDatabase();
            if (string.IsNullOrWhiteSpace(sku)) return null;
            _cardsBySku.TryGetValue(sku, out UcgCardData cardData);
            return cardData;
        }

        public List<UcgCardData> FindCardsByCharacterName(string characterName)
        {
            return FindCards(card => card != null && TextContains(card.characterName, characterName));
        }

        public List<UcgCardData> FindCardsByNameContains(string keyword)
        {
            return FindCards(card => card != null && TextContains(card.cardName, keyword));
        }

        public List<UcgCardData> FindCards(Func<UcgCardData, bool> predicate)
        {
            if (!_loaded) LoadDatabase();

            var results = new List<UcgCardData>();
            if (predicate == null) return results;

            for (int i = 0; i < _cards.Count; i++)
            {
                UcgCardData card = _cards[i];
                if (predicate(card))
                {
                    results.Add(card);
                }
            }

            return results;
        }

        public string ResolveImageFilePath(string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal)) return "";

            string relativePath = imageLocal.Trim();
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }

            return Path.Combine(publicRootPath, relativePath);
        }

        public string ResolveImageFileUrl(string imageLocal)
        {
            string fullPath = ResolveImageFilePath(imageLocal);
            if (string.IsNullOrWhiteSpace(fullPath)) return "";
            return new Uri(fullPath).AbsoluteUri;
        }

        [ContextMenu("Debug Load External Cards")]
        public void DebugLoadExternalCards()
        {
            bool loaded = LoadDatabase();
            Debug.Log($"UCG external card debug load: loaded={loaded}, count={_cards.Count}");
            int previewCount = Mathf.Min(3, _cards.Count);
            for (int i = 0; i < previewCount; i++)
            {
                UcgCardData card = _cards[i];
                Debug.Log($"External card {i + 1}: id={card.id}, sku={card.sku}, name={card.cardName}, imageLocal={card.imageLocal}");
            }

            if (!loaded || _cards.Count == 0) return;

            UcgCardData firstImageCard = _cards.Find(card => card != null && !string.IsNullOrWhiteSpace(card.imageLocal));
            if (firstImageCard == null) return;

            Debug.Log($"External image URL test: {ResolveImageFileUrl(firstImageCard.imageLocal)}");
            var imageLoader = UcgCardImageLoader.GetOrCreate();
            imageLoader.database = this;
            imageLoader.LoadCardImage(firstImageCard, sprite =>
            {
                if (sprite != null)
                {
                    Debug.Log($"External card image load success: {firstImageCard.id}, sprite={sprite.name}");
                }
                else
                {
                    Debug.LogWarning($"External card image load failed, placeholder expected: {firstImageCard.id}");
                }
            });
        }

        [ContextMenu("Debug Print Diga Candidates")]
        public void DebugPrintDigaCandidates()
        {
            bool loaded = LoadDatabase();
            Debug.Log($"Diga candidate debug: loaded={loaded}, total={_cards.Count}");
            if (!loaded) return;

            DebugPrintCandidateGroup(
                "迪卡 Lv.1 候選",
                FindCards(card => IsDigaCard(card) && card.level == 1 && !card.IsSceneCard()));
            DebugPrintCandidateGroup(
                "迪卡 Lv.2 候選",
                FindCards(card => IsDigaCard(card) && card.level == 2 && !card.IsSceneCard()));
            DebugPrintCandidateGroup(
                "迪卡 Lv.3 候選",
                FindCards(card => IsDigaCard(card) && card.level == 3 && !card.IsSceneCard()));
            DebugPrintCandidateGroup(
                "場景卡候選",
                FindCards(card => card != null && card.IsSceneCard()));
            DebugPrintCandidateGroup(
                "0 燈場景候選",
                FindCards(card => card != null && card.IsSceneCard() && card.sceneTurnCost == 0));
        }

        string ResolveCardsJsonPath()
        {
            if (string.IsNullOrWhiteSpace(cardsJsonPath)) return "";
            if (File.Exists(cardsJsonPath)) return cardsJsonPath;
            if (Directory.Exists(cardsJsonPath))
            {
                if (Path.GetExtension(cardsJsonPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"UCG cardsJsonPath points to a directory named like a JSON file: {cardsJsonPath}");
                }

                string nestedPath = Path.Combine(cardsJsonPath, "cards.json");
                if (File.Exists(nestedPath)) return nestedPath;
            }

            return cardsJsonPath;
        }

        static void AddCardKey(Dictionary<string, UcgCardData> dictionary, string key, UcgCardData cardData)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(key) || cardData == null) return;
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, cardData);
            }
        }

        static UcgCardData ConvertExternalCard(ExternalCard externalCard)
        {
            if (externalCard == null) return null;

            var cardData = new UcgCardData
            {
                id = SafeString(externalCard.id),
                sku = SafeString(externalCard.sku),
                cardName = SafeString(externalCard.name),
                characterName = SafeString(externalCard.characterName),
                cardCategory = SafeString(externalCard.cardCategory),
                level = Mathf.Max(0, externalCard.level),
                type = SafeString(externalCard.type),
                seriesText = SafeString(externalCard.seriesText),
                imageLocal = ResolveImageLocal(externalCard),
                imageUrl = SafeString(externalCard.image),
                teamTag = "",
                effectDescription = SafeString(externalCard.effect),
                sceneDescription = SafeString(externalCard.effect),
                effectTiming = UcgEffectTiming.None,
                effectId = UcgDemoEffectId.None,
                sceneEffectTiming = UcgEffectTiming.None,
                sceneEffectId = UcgDemoSceneEffectId.None,
                sceneTurnCost = SafeString(externalCard.cardCategory) == "場景"
                    ? Mathf.Max(0, externalCard.level)
                    : 0
            };

            ApplyBp(cardData, externalCard.bp);
            return cardData;
        }

        static void ApplyBp(UcgCardData cardData, int[] bp)
        {
            if (cardData == null || bp == null || bp.Length == 0) return;

            cardData.singleBp = GetBp(bp, 0);
            cardData.doubleBp = GetBp(bp, 1, cardData.singleBp);
            cardData.tripleBp = GetBp(bp, 2, cardData.doubleBp);
            cardData.quadBp = GetBp(bp, 3, cardData.tripleBp);
        }

        static int GetBp(int[] bp, int index, int fallback = 0)
        {
            return bp != null && index >= 0 && index < bp.Length ? bp[index] : fallback;
        }

        static string SafeString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }

        static string ResolveImageLocal(ExternalCard externalCard)
        {
            string imageLocal = SafeString(externalCard.imageLocal);
            if (!string.IsNullOrWhiteSpace(imageLocal)) return imageLocal;

            string image = SafeString(externalCard.image);
            if (string.IsNullOrWhiteSpace(image)) return "";

            const string rootedMarker = "/images/cards/";
            int markerIndex = image.IndexOf(rootedMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return StripUrlSuffix(image.Substring(markerIndex));
            }

            const string relativeMarker = "images/cards/";
            markerIndex = image.IndexOf(relativeMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return StripUrlSuffix("/" + image.Substring(markerIndex));
            }

            return "";
        }

        static string StripUrlSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            int queryIndex = value.IndexOf('?');
            int hashIndex = value.IndexOf('#');
            int cutIndex = -1;
            if (queryIndex >= 0) cutIndex = queryIndex;
            if (hashIndex >= 0) cutIndex = cutIndex < 0 ? hashIndex : Math.Min(cutIndex, hashIndex);

            return cutIndex >= 0 ? value.Substring(0, cutIndex) : value;
        }

        static bool TextContains(string text, string keyword)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword)) return false;
            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsDigaCard(UcgCardData card)
        {
            if (card == null) return false;
            return TextContains(card.characterName, "迪卡") || TextContains(card.cardName, "迪卡");
        }

        static void DebugPrintCandidateGroup(string title, List<UcgCardData> cards)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"{title}: count={(cards != null ? cards.Count : 0)}");
            if (cards != null)
            {
                int previewCount = Mathf.Min(20, cards.Count);
                for (int i = 0; i < previewCount; i++)
                {
                    UcgCardData card = cards[i];
                    builder.AppendLine($"{i + 1}. {FormatCandidate(card)}");
                }
            }

            Debug.Log(builder.ToString());
        }

        static string FormatCandidate(UcgCardData card)
        {
            if (card == null) return "null";
            return $"{card.id} / {card.sku} / {card.cardName} / {card.characterName} / Lv.{card.level} / sceneLight={card.sceneTurnCost} / {card.imageLocal}";
        }

        [Serializable]
        class ExternalCardList
        {
            public ExternalCard[] cards;
        }

        [Serializable]
        class ExternalCard
        {
            public string id;
            public string sku;
            public string name;
            public string characterName;
            public string cardCategory;
            public int level;
            public string type;
            public string seriesText;
            public string effect;
            public int[] bp;
            public string imageLocal;
            public string image;
        }
    }
}
