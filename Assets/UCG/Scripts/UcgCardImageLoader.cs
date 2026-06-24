using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgCardImageLoader : MonoBehaviour
    {
        public UcgExternalCardDatabase database;
        public string unityPngRootPath = "/Users/xiaoma/UCGShared/ucg-tool-unity-images";
        public bool debugImageLoading;

        readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        readonly Dictionary<string, List<Action<Sprite>>> _pendingCallbacks = new Dictionary<string, List<Action<Sprite>>>();
        readonly HashSet<string> _failedImages = new HashSet<string>();

        public class ImageCandidate
        {
            public string label;
            public string imageLocal;
            public string fullPath;
            public string url;
            public bool exists;
        }

        public int CacheCount => _spriteCache.Count;

        public static UcgCardImageLoader Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public static UcgCardImageLoader GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<UcgCardImageLoader>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var loaderObject = new GameObject("UCG Card Image Loader", typeof(UcgCardImageLoader));
            DontDestroyOnLoad(loaderObject);
            Instance = loaderObject.GetComponent<UcgCardImageLoader>();
            return Instance;
        }

        public void LoadCardImage(UcgCardData cardData, Action<Sprite> onLoaded)
        {
            if (cardData == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            string imageLocal = !string.IsNullOrWhiteSpace(cardData.imageLocal)
                ? cardData.imageLocal
                : ExtractImageLocalFromUrl(cardData.imageUrl);
            string cacheKey = GetCacheKey(cardData, imageLocal);
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                if (cardData.cardImage != null)
                {
                    onLoaded?.Invoke(cardData.cardImage);
                    return;
                }

                Debug.LogWarning(
                    "Card image fallback to placeholder:\n" +
                    $"card = {GetCardId(cardData)} {GetCardName(cardData)}\n" +
                    "imageLocal = <empty>\n" +
                    "reason = card has no id, no imageLocal, and no local sprite");
                onLoaded?.Invoke(null);
                return;
            }

            if (string.IsNullOrWhiteSpace(imageLocal) &&
                string.IsNullOrWhiteSpace(cardData.imageUrl) &&
                cardData.cardImage != null)
            {
                onLoaded?.Invoke(cardData.cardImage);
                return;
            }

            if (_spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                cardData.cardImage = cachedSprite;
                if (debugImageLoading) Debug.Log($"UCG card image cache hit: {cacheKey}");
                onLoaded?.Invoke(cachedSprite);
                return;
            }

            if (_failedImages.Contains(cacheKey))
            {
                onLoaded?.Invoke(null);
                return;
            }

            if (_pendingCallbacks.TryGetValue(cacheKey, out List<Action<Sprite>> callbacks))
            {
                callbacks.Add(onLoaded);
                return;
            }

            _pendingCallbacks[cacheKey] = new List<Action<Sprite>> { onLoaded };
            StartCoroutine(LoadCardImageRoutine(cardData, imageLocal, cacheKey));
        }

        IEnumerator LoadCardImageRoutine(UcgCardData cardData, string imageLocal, string cacheKey)
        {
            UcgExternalCardDatabase cardDatabase = GetDatabase();
            Sprite sprite = null;
            string triedCandidates = "";
            string cardId = GetCardId(cardData);

            UcgCardImageIndex imageIndex = UcgCardImageIndex.GetOrCreate();
            imageIndex.BuildIndex(
                cardDatabase != null ? cardDatabase.publicRootPath : "",
                unityPngRootPath);
            if (imageIndex.TryResolve(cardId, out UcgCardImageIndex.ImageIndexEntry indexEntry))
            {
                triedCandidates = "ImageIndex:\n" + imageIndex.FormatEntry(indexEntry);
                if (debugImageLoading) Debug.Log($"ImageIndex resolved {cardId} {GetCardName(cardData)}:\n{imageIndex.FormatEntry(indexEntry)}");
                yield return StartCoroutine(LoadSpriteFromUrl(indexEntry.selectedUrl, cardData, indexEntry.selectedPath, loadedSprite => sprite = loadedSprite));
                if (debugImageLoading)
                {
                    Debug.Log(
                        "ImageIndex load result:\n" +
                        $"card={cardId} {GetCardName(cardData)}\n" +
                        $"selectedPath={indexEntry.selectedPath}\n" +
                        $"fileExists={File.Exists(indexEntry.selectedPath)}\n" +
                        $"result={(sprite != null ? "success" : "failed")}");
                }
            }
            else
            {
                triedCandidates = $"ImageIndex: no entry for cardId={cardId}\n";
                if (debugImageLoading)
                {
                    Debug.LogWarning(
                        "ImageIndex missing card image entry:\n" +
                        $"card={cardId} {GetCardName(cardData)}\n" +
                        $"imageLocal={imageLocal}");
                }
            }

            if (sprite == null && cardDatabase != null)
            {
                List<ImageCandidate> candidates = BuildImageCandidates(cardDatabase, cardData, imageLocal);
                triedCandidates += "Fallback candidates:\n" + FormatCandidates(candidates);
                if (debugImageLoading) Debug.Log($"Resolve image candidates for {GetCardId(cardData)} {GetCardName(cardData)}:\n{triedCandidates}");

                for (int i = 0; i < candidates.Count; i++)
                {
                    ImageCandidate candidate = candidates[i];
                    if (debugImageLoading)
                    {
                        Debug.Log(
                            "Try load image candidate:\n" +
                            $"card = {GetCardId(cardData)} {GetCardName(cardData)}\n" +
                            $"label = {candidate.label}\n" +
                            $"imageLocal = {candidate.imageLocal}\n" +
                            $"path = {candidate.fullPath}\n" +
                            $"url = {candidate.url}\n" +
                            $"exists = {candidate.exists}");
                    }

                    yield return StartCoroutine(LoadSpriteFromUrl(candidate.url, cardData, candidate.imageLocal, loadedSprite => sprite = loadedSprite));
                    if (debugImageLoading) Debug.Log($"Try load image candidate result: card={GetCardId(cardData)}, label={candidate.label}, imageLocal={candidate.imageLocal}, result={(sprite != null ? "success" : "failed")}");

                    if (sprite != null)
                    {
                        if (debugImageLoading) Debug.Log($"Selected image candidate for {GetCardId(cardData)} {GetCardName(cardData)}: {candidate.label}, path={candidate.fullPath}");
                        break;
                    }
                }
            }

            if (sprite != null)
            {
                _spriteCache[cacheKey] = sprite;
                if (cardData != null) cardData.cardImage = sprite;
            }
            else
            {
                _failedImages.Add(cacheKey);
                Debug.LogWarning(
                    "Card image fallback to placeholder:\n" +
                    $"card = {GetCardId(cardData)} {GetCardName(cardData)}\n" +
                    $"cacheKey = {cacheKey}\n" +
                    $"imageLocal = {imageLocal}\n" +
                    $"triedCandidates =\n{triedCandidates}\n" +
                    "reason = all image candidates failed or image database unavailable");
            }

            if (_pendingCallbacks.TryGetValue(cacheKey, out List<Action<Sprite>> callbacks))
            {
                _pendingCallbacks.Remove(cacheKey);
                for (int i = 0; i < callbacks.Count; i++)
                {
                    callbacks[i]?.Invoke(sprite);
                }
            }
        }

        public List<ImageCandidate> BuildImageCandidates(UcgExternalCardDatabase cardDatabase, UcgCardData cardData, string imageLocal)
        {
            var candidates = new List<ImageCandidate>();
            var seenPaths = new HashSet<string>();
            if (cardDatabase == null) return candidates;

            var imageLocals = BuildImageLocalVariants(cardData, imageLocal);
            for (int i = 0; i < imageLocals.Count; i++)
            {
                AddCandidate(candidates, seenPaths, $"public {Path.GetExtension(imageLocals[i])}", imageLocals[i], cardDatabase.ResolveImageFilePath(imageLocals[i]));
                AddCandidate(candidates, seenPaths, $"unity mirror {Path.GetExtension(imageLocals[i])}", imageLocals[i], ResolveUnityMirrorPath(imageLocals[i]));
            }

            candidates.Sort(CompareImageCandidates);
            return candidates;
        }

        List<string> BuildImageLocalVariants(UcgCardData cardData, string imageLocal)
        {
            var variants = new List<string>();
            AddImageLocalVariant(variants, imageLocal);
            AddImageLocalExtensionVariants(variants, imageLocal);

            string imageUrlLocal = ExtractImageLocalFromUrl(cardData != null ? cardData.imageUrl : "");
            AddImageLocalVariant(variants, imageUrlLocal);
            AddImageLocalExtensionVariants(variants, imageUrlLocal);

            AddImageLocalVariant(variants, BuildImageLocalFromCardId(cardData != null ? cardData.id : "", ".webp"));
            AddImageLocalVariant(variants, BuildImageLocalFromCardId(cardData != null ? cardData.id : "", ".png"));
            return variants;
        }

        void AddImageLocalExtensionVariants(List<string> variants, string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal)) return;
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".webp"));
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".png"));
        }

        string ExtractImageLocalFromUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return "";

            const string rootedMarker = "/images/cards/";
            int markerIndex = imageUrl.IndexOf(rootedMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return StripUrlSuffix(imageUrl.Substring(markerIndex));
            }

            const string relativeMarker = "images/cards/";
            markerIndex = imageUrl.IndexOf(relativeMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return StripUrlSuffix("/" + imageUrl.Substring(markerIndex));
            }

            return "";
        }

        string BuildImageLocalFromCardId(string cardId, string extension)
        {
            if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(extension)) return "";

            int dashIndex = cardId.IndexOf("-");
            string folder = dashIndex > 0 ? cardId.Substring(0, dashIndex) : cardId;
            return $"/images/cards/{folder}/{cardId}{extension}";
        }

        void AddImageLocalVariant(List<string> variants, string imageLocal)
        {
            if (variants == null || string.IsNullOrWhiteSpace(imageLocal)) return;
            for (int i = 0; i < variants.Count; i++)
            {
                if (string.Equals(variants[i], imageLocal, StringComparison.OrdinalIgnoreCase)) return;
            }

            variants.Add(imageLocal);
        }

        void AddCandidate(List<ImageCandidate> candidates, HashSet<string> seenPaths, string label, string imageLocal, string fullPath)
        {
            if (candidates == null || seenPaths == null || string.IsNullOrWhiteSpace(fullPath)) return;
            if (!seenPaths.Add(fullPath)) return;

            candidates.Add(new ImageCandidate
            {
                label = label,
                imageLocal = imageLocal,
                fullPath = fullPath,
                url = ResolveFileUrl(fullPath),
                exists = File.Exists(fullPath)
            });
        }

        public string FormatCandidates(List<ImageCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0) return "<none>";

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                ImageCandidate candidate = candidates[i];
                builder.AppendLine($"candidate {i + 1}: {candidate.label}, imageLocal={candidate.imageLocal}, path={candidate.fullPath}, exists={candidate.exists}");
            }

            return builder.ToString();
        }

        int CompareImageCandidates(ImageCandidate a, ImageCandidate b)
        {
            return GetCandidatePriority(a).CompareTo(GetCandidatePriority(b));
        }

        int GetCandidatePriority(ImageCandidate candidate)
        {
            if (candidate == null) return 999;
            bool isPublic = candidate.label != null && candidate.label.StartsWith("public", StringComparison.OrdinalIgnoreCase);
            bool isUnity = candidate.label != null && candidate.label.StartsWith("unity", StringComparison.OrdinalIgnoreCase);
            bool isWebp = candidate.fullPath != null && candidate.fullPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
            bool isPng = candidate.fullPath != null && candidate.fullPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

            if (candidate.exists && isUnity && isPng) return 0;
            if (candidate.exists && isPublic && isPng) return 1;
            if (candidate.exists && isPublic && isWebp) return 2;
            if (candidate.exists && isUnity && isWebp) return 3;
            if (candidate.exists) return 4;
            if (isUnity && isPng) return 10;
            if (isPublic && isPng) return 11;
            if (isPublic && isWebp) return 12;
            if (isUnity && isWebp) return 13;
            return 99;
        }

        IEnumerator LoadSpriteFromUrl(string url, UcgCardData cardData, string imageLocal, Action<Sprite> onLoaded)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onLoaded?.Invoke(null);
                yield return null;
                yield break;
            }

            if (debugImageLoading) Debug.Log($"UCG card image load url: {url}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string cardId = cardData != null ? cardData.id : "";
                    string cardName = GetCardName(cardData);
                    string localPath = GetLocalFilePath(url);
                    long fileSize = !string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath)
                        ? new FileInfo(localPath).Length
                        : -1;
                    Debug.LogWarning(
                        "Image load failed:\n" +
                        $"card = {cardName} ({cardId})\n" +
                        $"imageLocal = {imageLocal}\n" +
                        $"fileUrl = {url}\n" +
                        $"localPath = {localPath}\n" +
                        $"extension = {Path.GetExtension(localPath)}\n" +
                        $"fileExists = {File.Exists(localPath)}\n" +
                        $"fileSizeBytes = {fileSize}\n" +
                        $"error = {request.error}\n" +
                        $"responseCode = {request.responseCode}");
                    onLoaded?.Invoke(null);
                    yield return null;
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                {
                    Debug.LogWarning($"UCG card image returned empty texture: imageLocal={imageLocal}, url={url}");
                    onLoaded?.Invoke(null);
                    yield return null;
                    yield break;
                }

                texture.name = cardData != null && !string.IsNullOrWhiteSpace(cardData.id)
                    ? cardData.id
                    : Path.GetFileNameWithoutExtension(imageLocal);

                Rect rect = new Rect(0f, 0f, texture.width, texture.height);
                Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
                sprite.name = texture.name;
                onLoaded?.Invoke(sprite);
            }
        }

        UcgExternalCardDatabase GetDatabase()
        {
            if (database != null) return database;
            database = UcgExternalCardDatabase.GetOrCreate();
            return database;
        }

        static string GetCardName(UcgCardData cardData)
        {
            return cardData != null ? cardData.cardName : "";
        }

        static string GetCardId(UcgCardData cardData)
        {
            return cardData != null ? cardData.id : "";
        }

        static string GetCacheKey(UcgCardData cardData, string imageLocal)
        {
            if (cardData != null && !string.IsNullOrWhiteSpace(cardData.id)) return cardData.id.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(imageLocal)) return imageLocal;
            return cardData != null ? cardData.imageUrl : "";
        }

        string ResolveUnityMirrorPath(string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal)) return "";

            string relativePath = imageLocal.Trim();
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }

            return Path.Combine(unityPngRootPath, relativePath);
        }

        static string ResolveFileUrl(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return "";
            return new Uri(fullPath).AbsoluteUri;
        }

        static string GetLocalFilePath(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return "";
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri uri) && uri.IsFile)
            {
                return uri.LocalPath;
            }

            return fileUrl;
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
    }
}
