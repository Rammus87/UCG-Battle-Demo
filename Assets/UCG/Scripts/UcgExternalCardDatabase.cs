using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgExternalCardDatabase : MonoBehaviour
    {
        const string PublicCardsRelativeRoot = "images/cards";
        const string UcgToolProjectFolderName = "UCG-tool";
        static readonly string[] AdjacentCardsJsonRelativePaths =
        {
            Path.Combine("src", "data", "cards.json"),
            Path.Combine("public", "data", "cards.json"),
            Path.Combine("src", "assets", "data", "cards.json"),
            "cards.json"
        };

        public string cardsJsonPath = "/Users/xiaoma/UCGShared/ucg-tool-data/cards.json/cards.json";
        public string publicRootPath = "/Users/xiaoma/UCGShared/ucg-tool-public";
        public bool debugLoadOnStart;

        readonly Dictionary<string, UcgCardData> _cardsById = new Dictionary<string, UcgCardData>();
        readonly Dictionary<string, UcgCardData> _cardsBySku = new Dictionary<string, UcgCardData>();
        readonly List<UcgCardData> _cards = new List<UcgCardData>();

        bool _loaded;
        bool _loggedMissingCardsJson;
        string _loadedCardsJsonPath = "";

        public int Count => _cards.Count;
        public IReadOnlyList<UcgCardData> Cards => _cards;
        public string LoadedCardsJsonPath => _loadedCardsJsonPath;

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
            bool cardsJsonExists = !string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath);
            if (!cardsJsonExists)
            {
                if (!_loggedMissingCardsJson)
                {
                    Debug.LogWarning(
                        "[UCG Data] external cards.json not found, using fallback\n" +
                        $"cardsJsonPath = {cardsJsonPath}\n" +
                        $"resolvedPath = {resolvedPath}\n" +
                        $"exists = {cardsJsonExists}\n" +
                        FormatCardsJsonCandidateDiagnostics());
                    _loggedMissingCardsJson = true;
                }
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
                _loadedCardsJsonPath = resolvedPath;
                LogCardsJsonStartupDiagnostics(resolvedPath, true);
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
#if UNITY_WEBGL && !UNITY_EDITOR
            return "";
#else
            if (string.IsNullOrWhiteSpace(imageLocal)) return "";

            string adjacentPath = ResolveAdjacentUcgToolPublicImagePath(imageLocal);
            if (!string.IsNullOrWhiteSpace(adjacentPath) && File.Exists(adjacentPath))
            {
                return adjacentPath;
            }

            string relativePath = NormalizeImageLocalToRelativePath(imageLocal);
            if (string.IsNullOrWhiteSpace(relativePath)) return "";
            return Path.Combine(publicRootPath, relativePath);
#endif
        }

        public string ResolveConfiguredImageFilePath(string imageLocal)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "";
#else
            string relativePath = NormalizeImageLocalToRelativePath(imageLocal);
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(publicRootPath)) return "";
            return Path.Combine(publicRootPath, relativePath);
#endif
        }

        public string ResolveImageFileUrl(string imageLocal)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return ResolvePublicImageUrl(imageLocal);
#else
            string fullPath = ResolveImageFilePath(imageLocal);
            if (string.IsNullOrWhiteSpace(fullPath)) return "";
            return new Uri(fullPath).AbsoluteUri;
#endif
        }

        public static bool CanUseLocalPublicFiles()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }

        public static string ResolveAdjacentUcgToolPublicImagePath(string imageLocal)
        {
            if (!CanUseLocalPublicFiles() || !IsCardsImageLocal(imageLocal)) return "";

            string publicRoot = GetAdjacentUcgToolPublicRootPath();
            string relativePath = NormalizeImageLocalToRelativePath(imageLocal);
            if (string.IsNullOrWhiteSpace(publicRoot) || string.IsNullOrWhiteSpace(relativePath)) return "";
            return Path.Combine(publicRoot, relativePath);
        }

        public static string GetAdjacentUcgToolPublicRootPath()
        {
            string projectRoot = GetProjectRootPath();
            if (string.IsNullOrWhiteSpace(projectRoot)) return "";

            try
            {
                return Path.GetFullPath(Path.Combine(projectRoot, "..", "UCG-tool", "public"));
            }
            catch
            {
                return "";
            }
        }

        public static string GetAdjacentUcgToolCardsRootPath()
        {
            string publicRoot = GetAdjacentUcgToolPublicRootPath();
            return string.IsNullOrWhiteSpace(publicRoot) ? "" : Path.Combine(publicRoot, "images", "cards");
        }

        public static string ResolveAdjacentUcgToolCardsJsonPath()
        {
            List<string> candidates = GetAdjacentUcgToolCardsJsonCandidatePaths();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i])) return candidates[i];
            }

            return candidates.Count > 0 ? candidates[0] : "";
        }

        public static List<string> GetAdjacentUcgToolCardsJsonCandidatePaths()
        {
            var candidates = new List<string>();
            if (!CanUseLocalPublicFiles()) return candidates;

            string projectRoot = GetProjectRootPath();
            if (string.IsNullOrWhiteSpace(projectRoot)) return candidates;

            string ucgToolRoot;
            try
            {
                ucgToolRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", UcgToolProjectFolderName));
            }
            catch
            {
                return candidates;
            }

            for (int i = 0; i < AdjacentCardsJsonRelativePaths.Length; i++)
            {
                candidates.Add(Path.Combine(ucgToolRoot, AdjacentCardsJsonRelativePaths[i]));
            }

            return candidates;
        }

        public static string ResolvePublicImageUrl(string imageLocal)
        {
            if (!string.IsNullOrWhiteSpace(imageLocal) &&
                (imageLocal.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 imageLocal.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                return imageLocal.Trim();
            }

            string relativePath = NormalizeImageLocalToRelativePath(imageLocal);
            if (string.IsNullOrWhiteSpace(relativePath)) return "";
            return "/" + relativePath.Replace('\\', '/').TrimStart('/');
        }

        public static string NormalizeImageLocalToRelativePath(string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal)) return "";

            string relativePath = imageLocal.Trim();
            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return relativePath.TrimStart('/', '\\');
        }

        public static bool IsCardsImageLocal(string imageLocal)
        {
            string relativePath = NormalizeImageLocalToRelativePath(imageLocal).Replace('\\', '/');
            return relativePath.StartsWith(PublicCardsRelativeRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        static string GetProjectRootPath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Application.dataPath)) return "";
                DirectoryInfo assetsDirectory = new DirectoryInfo(Application.dataPath);
                return assetsDirectory.Parent != null ? assetsDirectory.Parent.FullName : "";
            }
            catch
            {
                return "";
            }
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
            string adjacentPath = ResolveAdjacentUcgToolCardsJsonPath();
            if (!string.IsNullOrWhiteSpace(adjacentPath) && File.Exists(adjacentPath))
            {
                return adjacentPath;
            }

            string configuredPath = ResolveConfiguredCardsJsonPath(cardsJsonPath);
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                return configuredPath;
            }

            return !string.IsNullOrWhiteSpace(adjacentPath) ? adjacentPath : configuredPath;
        }

        static string ResolveConfiguredCardsJsonPath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath)) return "";

            string path = configuredPath.Trim();
            if (!Path.IsPathRooted(path))
            {
                string projectRoot = GetProjectRootPath();
                if (!string.IsNullOrWhiteSpace(projectRoot))
                {
                    path = Path.GetFullPath(Path.Combine(projectRoot, path));
                }
            }

            if (File.Exists(path)) return path;
            if (Directory.Exists(path))
            {
                if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"UCG cardsJsonPath points to a directory named like a JSON file: {path}");
                }

                string nestedPath = Path.Combine(path, "cards.json");
                if (File.Exists(nestedPath)) return nestedPath;
            }

            return path;
        }

        string FormatCardsJsonCandidateDiagnostics()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("cards.json candidate paths:");

            List<string> adjacentCandidates = GetAdjacentUcgToolCardsJsonCandidatePaths();
            for (int i = 0; i < adjacentCandidates.Count; i++)
            {
                string candidate = adjacentCandidates[i];
                builder.AppendLine($"adjacent[{i}] = {candidate}, exists={File.Exists(candidate)}");
            }

            string configuredPath = ResolveConfiguredCardsJsonPath(cardsJsonPath);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                builder.AppendLine($"configured = {configuredPath}, exists={File.Exists(configuredPath)}");
            }

            return builder.ToString();
        }

        void LogCardsJsonStartupDiagnostics(string resolvedPath, bool exists)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("[UCG Data] external cards.json loaded");
            builder.AppendLine($"cards.json path = {resolvedPath}");
            builder.AppendLine($"exists = {exists}");
            builder.AppendLine($"cardCount = {_cards.Count}");
            AppendCardStartupDiagnostics(builder, "BP01-105", true);
            AppendCardStartupDiagnostics(builder, "BP01-037", false);
            AppendCardStartupDiagnostics(builder, "BP05-002", false);
            Debug.Log(builder.ToString());
        }

        void AppendCardStartupDiagnostics(System.Text.StringBuilder builder, string cardId, bool includeSceneLight)
        {
            _cardsById.TryGetValue(cardId, out UcgCardData card);
            if (card == null)
            {
                builder.AppendLine($"{cardId} = <missing>");
                return;
            }

            string sceneLight = includeSceneLight ? $", sceneLight={card.sceneTurnCost}" : "";
            builder.AppendLine($"{cardId} = name={card.cardName}, imageLocal={card.imageLocal}{sceneLight}");
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
