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
        const string ProjectSpritesFolder = "UCG/Sprites";

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

        public class ImageLoadAttempt
        {
            public string imageLocal;
            public string url;
            public string finalPath;
            public bool exists;
            public string extension;
            public string unityWebRequestResult;
            public string error;
            public bool textureNull;
            public int textureWidth;
            public int textureHeight;
            public string decodeResult;
            public string fallbackUsed;
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
            bool anyExistingCandidate = false;
            var loadAttempts = new List<ImageLoadAttempt>();

            if (CanUseLocalImageFiles())
            {
                UcgCardImageIndex imageIndex = UcgCardImageIndex.GetOrCreate();
                imageIndex.BuildIndex(
                    cardDatabase != null ? cardDatabase.publicRootPath : "",
                    unityPngRootPath);
                if (imageIndex.TryResolve(cardId, out UcgCardImageIndex.ImageIndexEntry indexEntry))
                {
                    triedCandidates = "ImageIndex:\n" + imageIndex.FormatEntry(indexEntry);
                    if (debugImageLoading) Debug.Log($"ImageIndex resolved {cardId} {GetCardName(cardData)}:\n{imageIndex.FormatEntry(indexEntry)}");
                    anyExistingCandidate = anyExistingCandidate || File.Exists(indexEntry.selectedPath);
                    yield return StartCoroutine(LoadSpriteFromUrl(indexEntry.selectedUrl, cardData, indexEntry.selectedPath, (loadedSprite, attempt) =>
                    {
                        sprite = loadedSprite;
                        AddLoadAttempt(loadAttempts, attempt);
                    }));
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
                    triedCandidates = $"ImageIndex: no entry for cardId={cardId}\n{imageIndex.FormatRootDiagnostics()}\n";
                    if (debugImageLoading)
                    {
                        Debug.LogWarning(
                            "ImageIndex missing card image entry:\n" +
                            $"card={cardId} {GetCardName(cardData)}\n" +
                            $"imageLocal={imageLocal}\n" +
                            imageIndex.FormatRootDiagnostics());
                    }
                }
            }
            else
            {
                triedCandidates = "ImageIndex: skipped because local file image scan is disabled for this platform.\n";
            }

            if (sprite == null && cardDatabase != null)
            {
                List<ImageCandidate> candidates = BuildImageCandidates(cardDatabase, cardData, imageLocal);
                anyExistingCandidate = anyExistingCandidate || HasExistingCandidate(candidates);
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

                    yield return StartCoroutine(LoadSpriteFromUrl(candidate.url, cardData, candidate.imageLocal, (loadedSprite, attempt) =>
                    {
                        sprite = loadedSprite;
                        AddLoadAttempt(loadAttempts, attempt);
                    }));
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
                    "[UCG CardImage Missing]\n" +
                    $"cardId={GetCardId(cardData)}\n" +
                    $"cardName={GetCardName(cardData)}\n" +
                    $"imageLocal={imageLocal}\n" +
                    $"triedPaths=\n{FormatAttemptPaths(loadAttempts, triedCandidates)}" +
                    $"exists={anyExistingCandidate}\n" +
                    $"decodeResult={FormatDecodeResult(loadAttempts, anyExistingCandidate)}\n" +
                    "fallbackUsed=placeholder sprite expected\n" +
                    $"cacheKey={cacheKey}");
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
                string extension = Path.GetExtension(imageLocals[i]);
                if (!CanUseLocalImageFiles())
                {
                    AddUrlCandidate(candidates, seenPaths, $"public url {extension}", imageLocals[i], UcgExternalCardDatabase.ResolvePublicImageUrl(imageLocals[i]));
                    continue;
                }

                AddCandidate(candidates, seenPaths, $"adjacent public {extension}", imageLocals[i], UcgExternalCardDatabase.ResolveAdjacentUcgToolPublicImagePath(imageLocals[i]));
                AddCandidate(candidates, seenPaths, "project sprite .png", imageLocals[i], ResolveProjectSpritePath(imageLocals[i]));
                AddCandidate(candidates, seenPaths, $"public {extension}", imageLocals[i], cardDatabase.ResolveConfiguredImageFilePath(imageLocals[i]));
                AddCandidate(candidates, seenPaths, $"unity mirror {extension}", imageLocals[i], ResolveUnityMirrorPath(imageLocals[i]));
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
            AddImageLocalVariant(variants, BuildImageLocalFromCardId(cardData != null ? cardData.id : "", ".jpg"));
            AddImageLocalVariant(variants, BuildImageLocalFromCardId(cardData != null ? cardData.id : "", ".jpeg"));
            return variants;
        }

        void AddImageLocalExtensionVariants(List<string> variants, string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal)) return;
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".webp"));
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".png"));
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".jpg"));
            AddImageLocalVariant(variants, Path.ChangeExtension(imageLocal, ".jpeg"));
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

        void AddUrlCandidate(List<ImageCandidate> candidates, HashSet<string> seenPaths, string label, string imageLocal, string url)
        {
            if (candidates == null || seenPaths == null || string.IsNullOrWhiteSpace(url)) return;
            if (!seenPaths.Add(url)) return;

            candidates.Add(new ImageCandidate
            {
                label = label,
                imageLocal = imageLocal,
                fullPath = "",
                url = url,
                exists = false
            });
        }

        public string FormatCandidates(List<ImageCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0) return "<none>";

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                ImageCandidate candidate = candidates[i];
                builder.AppendLine($"candidate {i + 1}: {candidate.label}, imageLocal={candidate.imageLocal}, path={candidate.fullPath}, url={candidate.url}, exists={candidate.exists}");
            }

            return builder.ToString();
        }

        public string FormatCandidatePaths(List<ImageCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0) return "<none>\n";

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                ImageCandidate candidate = candidates[i];
                string path = !string.IsNullOrWhiteSpace(candidate.fullPath) ? candidate.fullPath : candidate.url;
                builder.AppendLine($"{i + 1}. {path}, exists={candidate.exists}");
            }

            return builder.ToString();
        }

        static bool HasExistingCandidate(List<ImageCandidate> candidates)
        {
            if (candidates == null) return false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].exists) return true;
            }

            return false;
        }

        int CompareImageCandidates(ImageCandidate a, ImageCandidate b)
        {
            return GetCandidatePriority(a).CompareTo(GetCandidatePriority(b));
        }

        int GetCandidatePriority(ImageCandidate candidate)
        {
            if (candidate == null) return 999;
            bool isAdjacentPublic = candidate.label != null && candidate.label.StartsWith("adjacent public", StringComparison.OrdinalIgnoreCase);
            bool isProjectSprite = candidate.label != null && candidate.label.StartsWith("project sprite", StringComparison.OrdinalIgnoreCase);
            bool isPublic = candidate.label != null && candidate.label.StartsWith("public", StringComparison.OrdinalIgnoreCase);
            bool isUnity = candidate.label != null && candidate.label.StartsWith("unity", StringComparison.OrdinalIgnoreCase);
            string pathOrUrl = GetCandidatePathOrUrl(candidate);
            bool isWebp = pathOrUrl.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
            bool isPng = pathOrUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            bool isJpg = pathOrUrl.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         pathOrUrl.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

            if (candidate.exists && isAdjacentPublic && isPng) return 0;
            if (candidate.exists && isAdjacentPublic && isJpg) return 1;
            if (candidate.exists && isProjectSprite && isPng) return 2;
            if (candidate.exists && isAdjacentPublic && isWebp) return 3;
            if (candidate.exists && isUnity && isPng) return 4;
            if (candidate.exists && isPublic && isPng) return 5;
            if (candidate.exists && isPublic && isJpg) return 6;
            if (candidate.exists && isPublic && isWebp) return 7;
            if (candidate.exists && isUnity && isJpg) return 8;
            if (candidate.exists && isUnity && isWebp) return 9;
            if (candidate.exists) return 10;
            if (isAdjacentPublic && isPng) return 11;
            if (isAdjacentPublic && isJpg) return 12;
            if (isProjectSprite && isPng) return 13;
            if (isAdjacentPublic && isWebp) return 14;
            if (isUnity && isPng) return 15;
            if (isPublic && isPng) return 16;
            if (isPublic && isJpg) return 17;
            if (isPublic && isWebp) return 18;
            if (isUnity && isJpg) return 19;
            if (isUnity && isWebp) return 20;
            return 99;
        }

        static string GetCandidatePathOrUrl(ImageCandidate candidate)
        {
            if (candidate == null) return "";
            return !string.IsNullOrWhiteSpace(candidate.fullPath) ? candidate.fullPath : candidate.url ?? "";
        }

        static string FormatAttemptPaths(List<ImageLoadAttempt> attempts, string fallbackCandidates)
        {
            if (attempts == null || attempts.Count == 0)
            {
                return string.IsNullOrWhiteSpace(fallbackCandidates) ? "<none>\n" : fallbackCandidates + "\n";
            }

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < attempts.Count; i++)
            {
                ImageLoadAttempt attempt = attempts[i];
                builder.AppendLine($"{i + 1}. {attempt.finalPath}, exists={attempt.exists}, extension={attempt.extension}, decodeResult={attempt.decodeResult}");
            }

            return builder.ToString();
        }

        static string FormatDecodeResult(List<ImageLoadAttempt> attempts, bool anyExistingCandidate)
        {
            if (attempts == null || attempts.Count == 0)
            {
                return anyExistingCandidate ? "Decode failed" : "Missing file";
            }

            for (int i = attempts.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(attempts[i].decodeResult)) return attempts[i].decodeResult;
            }

            return anyExistingCandidate ? "Decode failed" : "Missing file";
        }

        IEnumerator LoadSpriteFromUrl(string url, UcgCardData cardData, string imageLocal, Action<Sprite, ImageLoadAttempt> onLoaded)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onLoaded?.Invoke(null, new ImageLoadAttempt
                {
                    imageLocal = imageLocal,
                    url = url,
                    finalPath = "",
                    exists = false,
                    decodeResult = "Missing file: empty URL",
                    fallbackUsed = "none"
                });
                yield return null;
                yield break;
            }

            if (debugImageLoading) Debug.Log($"UCG card image load url: {url}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                string localPath = GetLocalFilePath(url);
                string extension = Path.GetExtension(localPath);
                bool isLocalFile = CanUseLocalImageFiles() && !string.IsNullOrWhiteSpace(localPath);
                bool fileExists = isLocalFile && File.Exists(localPath);
                long fileSize = fileExists ? new FileInfo(localPath).Length : -1;
                string webpNote = string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase)
                    ? "candidate extension is .webp; Unity may not decode WebP on this platform"
                    : "";
                var attempt = new ImageLoadAttempt
                {
                    imageLocal = imageLocal,
                    url = url,
                    finalPath = localPath,
                    exists = fileExists,
                    extension = extension,
                    unityWebRequestResult = request.result.ToString(),
                    error = request.error,
                    textureNull = true,
                    textureWidth = 0,
                    textureHeight = 0,
                    fallbackUsed = "none"
                };

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string cardId = cardData != null ? cardData.id : "";
                    string cardName = GetCardName(cardData);
                    Debug.LogWarning(
                        "[UCG CardImage] UnityWebRequest failed:\n" +
                        $"card = {cardName} ({cardId})\n" +
                        $"imageLocal = {imageLocal}\n" +
                        $"finalCandidatePath = {localPath}\n" +
                        $"fileUrl = {url}\n" +
                        $"fileExists = {fileExists}\n" +
                        $"extension = {extension}\n" +
                        $"unityWebRequestResult = {request.result}\n" +
                        $"fileSizeBytes = {fileSize}\n" +
                        $"error = {request.error}\n" +
                        $"responseCode = {request.responseCode}\n" +
                        $"textureNull = true\n" +
                        $"textureWidth = 0\n" +
                        $"textureHeight = 0\n" +
                        $"{webpNote}");
                    if (TryLoadSpriteFromLocalFile(localPath, cardData, imageLocal, out Sprite localSprite))
                    {
                        attempt.decodeResult = "UnityWebRequest failed; local byte decode OK";
                        attempt.fallbackUsed = "local byte decode";
                        onLoaded?.Invoke(localSprite, attempt);
                    }
                    else
                    {
                        attempt.decodeResult = fileExists
                            ? "Decode failed: UnityWebRequest failed and local byte decode failed"
                            : "Missing file";
                        if (fileExists)
                        {
                            Debug.LogWarning($"[UCG CardImage] failed: file exists but decode failed, imageLocal={imageLocal}, finalCandidatePath={localPath}, extension={extension}");
                        }

                        onLoaded?.Invoke(null, attempt);
                    }

                    yield return null;
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                bool textureIsNull = texture == null;
                int textureWidth = texture != null ? texture.width : 0;
                int textureHeight = texture != null ? texture.height : 0;
                attempt.textureNull = textureIsNull;
                attempt.textureWidth = textureWidth;
                attempt.textureHeight = textureHeight;
                if (debugImageLoading)
                {
                    Debug.Log(
                        "[UCG CardImage] UnityWebRequest texture result:\n" +
                        $"imageLocal = {imageLocal}\n" +
                        $"finalCandidatePath = {localPath}\n" +
                        $"fileExists = {fileExists}\n" +
                        $"extension = {extension}\n" +
                        $"unityWebRequestResult = {request.result}\n" +
                        $"error = {request.error}\n" +
                        $"textureNull = {textureIsNull}\n" +
                        $"textureWidth = {textureWidth}\n" +
                        $"textureHeight = {textureHeight}\n" +
                        $"{webpNote}");
                }

                if (texture == null || texture.width <= 0 || texture.height <= 0)
                {
                    Debug.LogWarning(
                        "[UCG CardImage] UnityWebRequest returned invalid texture:\n" +
                        $"imageLocal = {imageLocal}\n" +
                        $"finalCandidatePath = {localPath}\n" +
                        $"fileExists = {fileExists}\n" +
                        $"extension = {extension}\n" +
                        $"unityWebRequestResult = {request.result}\n" +
                        $"error = {request.error}\n" +
                        $"textureNull = {textureIsNull}\n" +
                        $"textureWidth = {textureWidth}\n" +
                        $"textureHeight = {textureHeight}\n" +
                        $"{webpNote}");
                    if (TryLoadSpriteFromLocalFile(localPath, cardData, imageLocal, out Sprite localSprite))
                    {
                        attempt.decodeResult = "UnityWebRequest invalid texture; local byte decode OK";
                        attempt.fallbackUsed = "local byte decode";
                        onLoaded?.Invoke(localSprite, attempt);
                    }
                    else
                    {
                        attempt.decodeResult = fileExists
                            ? "Decode failed: UnityWebRequest invalid texture and local byte decode failed"
                            : "Missing file";
                        if (fileExists)
                        {
                            Debug.LogWarning($"[UCG CardImage] failed: file exists but decode failed, imageLocal={imageLocal}, finalCandidatePath={localPath}, extension={extension}");
                        }

                        onLoaded?.Invoke(null, attempt);
                    }

                    yield return null;
                    yield break;
                }

                attempt.decodeResult = $"UnityWebRequestTexture OK {textureWidth}x{textureHeight}";
                onLoaded?.Invoke(CreateSpriteFromTexture(texture, cardData, imageLocal), attempt);
            }
        }

        static void AddLoadAttempt(List<ImageLoadAttempt> attempts, ImageLoadAttempt attempt)
        {
            if (attempts == null || attempt == null) return;
            attempts.Add(attempt);
        }

        bool TryLoadSpriteFromLocalFile(string localPath, UcgCardData cardData, string imageLocal, out Sprite sprite)
        {
            sprite = null;
            if (!CanUseLocalImageFiles() || string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath)) return false;

            string extension = Path.GetExtension(localPath);
            try
            {
                byte[] bytes = File.ReadAllBytes(localPath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bool decoded = texture.LoadImage(bytes);
                bool textureIsNull = texture == null;
                int textureWidth = texture != null ? texture.width : 0;
                int textureHeight = texture != null ? texture.height : 0;
                if (debugImageLoading || !decoded || textureWidth <= 0 || textureHeight <= 0)
                {
                    Debug.Log(
                        "[UCG CardImage] local byte decode result:\n" +
                        $"imageLocal = {imageLocal}\n" +
                        $"finalCandidatePath = {localPath}\n" +
                        $"fileExists = True\n" +
                        $"extension = {extension}\n" +
                        $"decoded = {decoded}\n" +
                        $"textureNull = {textureIsNull}\n" +
                        $"textureWidth = {textureWidth}\n" +
                        $"textureHeight = {textureHeight}");
                }

                if (!decoded || texture == null || texture.width <= 0 || texture.height <= 0)
                {
                    return false;
                }

                sprite = CreateSpriteFromTexture(texture, cardData, imageLocal);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[UCG CardImage] local byte decode exception:\n" +
                    $"imageLocal = {imageLocal}\n" +
                    $"finalCandidatePath = {localPath}\n" +
                    $"fileExists = True\n" +
                    $"extension = {extension}\n" +
                    $"error = {exception.Message}");
                return false;
            }
        }

        static Sprite CreateSpriteFromTexture(Texture2D texture, UcgCardData cardData, string imageLocal)
        {
            texture.name = cardData != null && !string.IsNullOrWhiteSpace(cardData.id)
                ? cardData.id
                : Path.GetFileNameWithoutExtension(imageLocal);

            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            return sprite;
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

        static string ResolveProjectSpritePath(string imageLocal)
        {
            if (string.IsNullOrWhiteSpace(imageLocal) || string.IsNullOrWhiteSpace(Application.dataPath)) return "";

            string fileName = Path.GetFileNameWithoutExtension(imageLocal.Trim());
            if (string.IsNullOrWhiteSpace(fileName)) return "";

            string relativeFolder = ProjectSpritesFolder.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relativeFolder, fileName + ".png");
        }

        static string ResolveFileUrl(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return "";
            return new Uri(fullPath).AbsoluteUri;
        }

        static bool CanUseLocalImageFiles()
        {
            return UcgExternalCardDatabase.CanUseLocalPublicFiles();
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
