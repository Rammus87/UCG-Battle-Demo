using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgCardImageIndex : MonoBehaviour
    {
        public string publicImagesRootPath = "/Users/xiaoma/UCGShared/ucg-tool-public/images/cards";
        public string unityImagesRootPath = "/Users/xiaoma/UCGShared/ucg-tool-unity-images/images/cards";
        public bool logResolvedCards;

        readonly Dictionary<string, ImageIndexEntry> _entries = new Dictionary<string, ImageIndexEntry>();
        string _builtPublicRootPath = "";
        string _builtUnityRootPath = "";
        int _publicFileCount;
        int _unityFileCount;
        bool _built;

        static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".webp",
            ".png",
            ".jpg",
            ".jpeg"
        };

        public static UcgCardImageIndex Instance { get; private set; }

        public int Count => _entries.Count;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public static UcgCardImageIndex GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<UcgCardImageIndex>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var indexObject = new GameObject("UCG Card Image Index", typeof(UcgCardImageIndex));
            DontDestroyOnLoad(indexObject);
            Instance = indexObject.GetComponent<UcgCardImageIndex>();
            return Instance;
        }

        public void BuildIndex(string publicRootPath, string unityRootPath)
        {
            string normalizedPublicRoot = NormalizeCardsRoot(publicRootPath, publicImagesRootPath);
            string normalizedUnityRoot = NormalizeCardsRoot(unityRootPath, unityImagesRootPath);

            if (_built &&
                string.Equals(_builtPublicRootPath, normalizedPublicRoot, StringComparison.Ordinal) &&
                string.Equals(_builtUnityRootPath, normalizedUnityRoot, StringComparison.Ordinal))
            {
                return;
            }

            _entries.Clear();
            _builtPublicRootPath = normalizedPublicRoot;
            _builtUnityRootPath = normalizedUnityRoot;
            _publicFileCount = 0;
            _unityFileCount = 0;

            _publicFileCount = ScanRoot(normalizedPublicRoot, ImageRootKind.Public);
            _unityFileCount = ScanRoot(normalizedUnityRoot, ImageRootKind.UnityMirror);

            foreach (ImageIndexEntry entry in _entries.Values)
            {
                entry.candidates.Sort(CompareCandidates);
                entry.selectedPath = entry.candidates.Count > 0 ? entry.candidates[0].path : "";
                entry.selectedUrl = ToFileUrl(entry.selectedPath);
            }

            _built = true;
            Debug.Log(
                "UCG card image index built:\n" +
                $"publicRoot={normalizedPublicRoot}\n" +
                $"unityRoot={normalizedUnityRoot}\n" +
                $"publicRootExists={Directory.Exists(normalizedPublicRoot)}\n" +
                $"unityRootExists={Directory.Exists(normalizedUnityRoot)}\n" +
                $"publicImageFiles={_publicFileCount}\n" +
                $"unityImageFiles={_unityFileCount}\n" +
                $"indexedCards={_entries.Count}");
        }

        public bool TryResolve(string cardId, out ImageIndexEntry entry)
        {
            entry = null;
            string key = NormalizeCardId(cardId);
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (!_entries.TryGetValue(key, out entry)) return false;

            if (logResolvedCards)
            {
                Debug.Log($"ImageIndex resolved {cardId}:\n{FormatEntry(entry)}");
            }

            return true;
        }

        public string FormatEntry(ImageIndexEntry entry)
        {
            if (entry == null) return "<no image index entry>";

            var builder = new StringBuilder();
            builder.AppendLine($"cardId={entry.cardId}");
            builder.AppendLine($"candidateCount={entry.candidates.Count}");
            for (int i = 0; i < entry.candidates.Count; i++)
            {
                ImageIndexCandidate candidate = entry.candidates[i];
                builder.AppendLine($"{i + 1}. {candidate.label}, priority={candidate.priority}, path={candidate.path}, exists={File.Exists(candidate.path)}");
            }

            builder.AppendLine($"selected={entry.selectedPath}");
            builder.AppendLine($"selectedSourceType={GetSelectedSourceType(entry)}");
            return builder.ToString();
        }

        public string GetSelectedSourceType(ImageIndexEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.selectedPath)) return "placeholder";
            if (entry.candidates == null || entry.candidates.Count == 0) return "placeholder";

            ImageIndexCandidate selected = entry.candidates[0];
            bool isPublic = selected.label != null && selected.label.StartsWith("public", StringComparison.OrdinalIgnoreCase);
            bool isUnity = selected.label != null && selected.label.StartsWith("unitymirror", StringComparison.OrdinalIgnoreCase);
            string extension = Path.GetExtension(selected.path).ToLowerInvariant();

            if (isUnity && extension == ".png") return "unity-png-mirror";
            if (isUnity && (extension == ".jpg" || extension == ".jpeg")) return "unity-jpg-mirror";
            if (isUnity && extension == ".webp") return "unity-webp-mirror";
            if (isPublic && extension == ".png") return "public-png";
            if (isPublic && (extension == ".jpg" || extension == ".jpeg")) return "public-jpg";
            if (isPublic && extension == ".webp") return "public-webp";
            return selected.label ?? "unknown";
        }

        public string FormatRootDiagnostics()
        {
            var builder = new StringBuilder();
            builder.AppendLine("ImageIndex root diagnostics:");
            builder.AppendLine($"publicRoot={_builtPublicRootPath}");
            builder.AppendLine($"publicRootExists={Directory.Exists(_builtPublicRootPath)}");
            builder.AppendLine($"publicImageFiles={_publicFileCount}");
            builder.AppendLine($"unityRoot={_builtUnityRootPath}");
            builder.AppendLine($"unityRootExists={Directory.Exists(_builtUnityRootPath)}");
            builder.AppendLine($"unityImageFiles={_unityFileCount}");
            builder.AppendLine($"indexedCards={_entries.Count}");
            return builder.ToString();
        }

        public string FormatCardDiagnostics(string cardId)
        {
            string key = NormalizeCardId(cardId);
            var builder = new StringBuilder();
            builder.AppendLine($"ImageIndex check {key}:");
            builder.AppendLine($"public webp exists = {File.Exists(BuildExpectedPath(_builtPublicRootPath, key, ".webp"))}");
            builder.AppendLine($"public png exists = {File.Exists(BuildExpectedPath(_builtPublicRootPath, key, ".png"))}");
            builder.AppendLine($"unity webp exists = {File.Exists(BuildExpectedPath(_builtUnityRootPath, key, ".webp"))}");
            builder.AppendLine($"unity png exists = {File.Exists(BuildExpectedPath(_builtUnityRootPath, key, ".png"))}");
            if (_entries.TryGetValue(key, out ImageIndexEntry entry))
            {
                builder.Append(FormatEntry(entry));
            }
            else
            {
                builder.AppendLine("selected=<none>");
            }

            return builder.ToString();
        }

        int ScanRoot(string rootPath, ImageRootKind rootKind)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                Debug.LogWarning($"UCG card image index root missing: {rootKind}, path={rootPath}");
                return 0;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"UCG card image index scan failed: root={rootPath}\n{exception.Message}");
                return 0;
            }

            int indexedFileCount = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string extension = Path.GetExtension(path);
                if (!SupportedExtensions.Contains(extension)) continue;
                indexedFileCount++;

                string cardId = Path.GetFileNameWithoutExtension(path);
                string key = NormalizeCardId(cardId);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (!_entries.TryGetValue(key, out ImageIndexEntry entry))
                {
                    entry = new ImageIndexEntry { cardId = key };
                    _entries.Add(key, entry);
                }

                entry.candidates.Add(new ImageIndexCandidate
                {
                    path = path,
                    label = $"{rootKind.ToString().ToLowerInvariant()} {extension.ToLowerInvariant()}",
                    priority = GetPriority(rootKind, extension)
                });
            }

            return indexedFileCount;
        }

        static int CompareCandidates(ImageIndexCandidate a, ImageIndexCandidate b)
        {
            int priorityCompare = a.priority.CompareTo(b.priority);
            if (priorityCompare != 0) return priorityCompare;
            return string.CompareOrdinal(a.path, b.path);
        }

        static int GetPriority(ImageRootKind rootKind, string extension)
        {
            bool isWebp = string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
            bool isPng = string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase);
            bool isJpg = string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);

            if (rootKind == ImageRootKind.UnityMirror && isPng) return 0;
            if (rootKind == ImageRootKind.UnityMirror && isJpg) return 1;
            if (rootKind == ImageRootKind.Public && isPng) return 2;
            if (rootKind == ImageRootKind.Public && isJpg) return 3;
            if (rootKind == ImageRootKind.Public && isWebp) return 4;
            if (rootKind == ImageRootKind.UnityMirror && isWebp) return 5;
            return 99;
        }

        static string NormalizeCardsRoot(string configuredRoot, string fallbackRoot)
        {
            string root = string.IsNullOrWhiteSpace(configuredRoot) ? fallbackRoot : configuredRoot.Trim();
            if (string.IsNullOrWhiteSpace(root)) return "";

            string nestedCardsRoot = Path.Combine(root, "images", "cards");
            if (Directory.Exists(nestedCardsRoot)) return nestedCardsRoot;
            return root;
        }

        static string NormalizeCardId(string cardId)
        {
            return string.IsNullOrWhiteSpace(cardId) ? "" : cardId.Trim().ToUpperInvariant();
        }

        static string BuildExpectedPath(string cardsRoot, string cardId, string extension)
        {
            if (string.IsNullOrWhiteSpace(cardsRoot) || string.IsNullOrWhiteSpace(cardId)) return "";
            int dashIndex = cardId.IndexOf("-");
            string folder = dashIndex > 0 ? cardId.Substring(0, dashIndex) : cardId;
            return Path.Combine(cardsRoot, folder, cardId + extension);
        }

        static string ToFileUrl(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? "" : new Uri(path).AbsoluteUri;
        }

        enum ImageRootKind
        {
            Public,
            UnityMirror
        }

        public class ImageIndexCandidate
        {
            public string path;
            public string label;
            public int priority;
        }

        public class ImageIndexEntry
        {
            public string cardId;
            public string selectedPath;
            public string selectedUrl;
            public readonly List<ImageIndexCandidate> candidates = new List<ImageIndexCandidate>();
        }
    }
}
