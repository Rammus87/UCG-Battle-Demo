using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UcgSceneCardView : MonoBehaviour, IPointerClickHandler
    {
        public UcgCardData cardData;
        public UcgPlayerSide sceneOwner;
        public UcgCardInfoPanel infoPanel;
        public Font uiFont;
        public UcgHandDemo demo;

        Image _backgroundImage;
        RectTransform _cardShadowRect;
        Image _cardShadowImage;
        Image _glowImage;
        Image _rotatedCardImage;
        Outline _rotatedCardOutline;
        UcgGuidancePulse _glowPulse;
        Coroutine _effectSourceHighlightRoutine;
        bool _effectSourceHighlightActive;
        RectTransform _effectSourceHighlightRect;
        Image _effectSourceHighlightImage;
        Outline _effectSourceHighlightOutline;
        Text _text;
        string _requestedImageLocal;

        public Image CardArtImage => _rotatedCardImage;
        public RectTransform CardArtRect => _rotatedCardImage != null ? _rotatedCardImage.rectTransform : null;
        public Image GlowImage => _glowImage;
        public RectTransform GlowRect => _glowImage != null ? _glowImage.rectTransform : null;
        public Text FallbackText => _text;
        public bool HasVisibleCardArt => _rotatedCardImage != null
            && _rotatedCardImage.enabled
            && _rotatedCardImage.sprite != null
            && _rotatedCardImage.color.a > 0.01f;
        public float FinalDisplayedRotation => NormalizeZ(GetRootRotationZ() + GetCardArtRotationZ());

        public void Initialize(UcgCardData data, UcgPlayerSide owner, UcgCardInfoPanel panel, Font font)
        {
            cardData = data;
            sceneOwner = owner;
            infoPanel = panel;
            uiFont = font;
            _requestedImageLocal = null;
            EnsureVisuals();
            Refresh();
        }

        public void Refresh()
        {
            EnsureVisuals();
            ApplySceneCardOrientation();

            bool shouldUseExternalImage = cardData != null && cardData.IsExternalCard();
            bool externalImageRequested = shouldUseExternalImage && _requestedImageLocal == cardData.imageLocal;
            bool hasSprite = cardData != null && cardData.cardImage != null && (!shouldUseExternalImage || externalImageRequested);
            if (_rotatedCardImage != null)
            {
                UcgCardImageApplier.ApplySprite(_rotatedCardImage, hasSprite ? cardData.cardImage : null);
                _rotatedCardImage.color = Color.white;
                _rotatedCardImage.preserveAspect = true;
                ForceSceneCardBoardOrientation();
                if (hasSprite)
                {
                    string visibilityReason;
                    UcgCardImageApplier.ValidateVisibility(cardData, gameObject, _rotatedCardImage, _text, false, out visibilityReason);
                }
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.enabled = false;
                _backgroundImage.color = Color.clear;
                _backgroundImage.raycastTarget = false;
            }

            if (_text != null)
            {
                string ownerText = sceneOwner == UcgPlayerSide.Player ? "我方場景" : "對手場景";
                string cardName = cardData != null ? cardData.cardName : "場景卡";
                string costText = cardData != null ? $"{cardData.sceneTurnCost} 燈" : "";
                string description = cardData != null && !string.IsNullOrWhiteSpace(cardData.sceneDescription)
                    ? $"\n{cardData.sceneDescription}"
                    : "";

                _text.text = hasSprite
                    ? $"{ownerText}｜{costText}\n{cardName}"
                    : $"{ownerText}｜{costText}\n{cardName}{description}";
                _text.enabled = !hasSprite;
                _text.raycastTarget = false;
            }

            ApplySceneSlotStateVisual(true, false);
            ForceSceneCardBoardOrientation();
            TryLoadExternalImage(hasSprite);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (infoPanel != null)
            {
                infoPanel.ShowCard(cardData);
            }

            demo?.HandleSceneCardClickedForEffect(this);
        }

        void EnsureVisuals()
        {
            RectTransform rootRect = transform as RectTransform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.localScale = Vector3.one;
            rootRect.localEulerAngles = Vector3.zero;

            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage == null) _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.enabled = false;
            _backgroundImage.color = Color.clear;
            _backgroundImage.raycastTarget = false;

            RectTransform shadowRect = EnsureChildRect("Scene Card Base Shadow", out _cardShadowImage);
            ConfigureSceneCardShadow(shadowRect);

            RectTransform glowRect = EnsureChildRect("Scene Active Glow", out _glowImage);
            ConfigureGlowImage(glowRect);

            RectTransform cardImageRect = EnsureChildRect("Rotated Card Image", out _rotatedCardImage);
            _rotatedCardImage.raycastTarget = false;
            _rotatedCardOutline = _rotatedCardImage.GetComponent<Outline>();
            if (_rotatedCardOutline == null) _rotatedCardOutline = _rotatedCardImage.gameObject.AddComponent<Outline>();

            RectTransform textRect = EnsureChildText("Scene Card Text", out _text);
            bool shouldUseExternalImage = cardData != null && cardData.IsExternalCard();
            bool externalImageRequested = shouldUseExternalImage && _requestedImageLocal == cardData.imageLocal;
            bool hasSprite = cardData != null && cardData.cardImage != null && (!shouldUseExternalImage || externalImageRequested);
            textRect.anchorMin = hasSprite ? new Vector2(0.06f, 0.04f) : new Vector2(0.05f, 0.08f);
            textRect.anchorMax = hasSprite ? new Vector2(0.94f, 0.3f) : new Vector2(0.95f, 0.92f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localEulerAngles = Vector3.zero;

            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = Color.white;
            if (uiFont != null) _text.font = uiFont;
            _text.fontSize = 16;
            _text.resizeTextForBestFit = true;
            _text.resizeTextMinSize = 8;
            _text.resizeTextMaxSize = 16;
            _text.raycastTarget = false;
        }

        public void ApplySceneCardOrientation()
        {
            ForceSceneCardBoardOrientation();
        }

        public void ForceSceneCardBoardOrientation()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null) return;

            rootRect.localScale = Vector3.one;
            rootRect.localEulerAngles = Vector3.zero;

            if (_rotatedCardImage != null)
            {
                RectTransform cardImageRect = _rotatedCardImage.rectTransform;
                ConfigureBoardCardArtRect(cardImageRect, rootRect, _rotatedCardImage.sprite, 0.94f);
            }

            if (_cardShadowImage != null)
            {
                ConfigureSceneCardShadow(_cardShadowImage.rectTransform);
            }

            if (_glowImage != null)
            {
                ConfigureBoardGlowRect(_glowImage.rectTransform, rootRect, 1.08f);
            }
        }

        void ConfigureBoardCardArtRect(RectTransform rect, RectTransform rootRect, Sprite sprite, float scale)
        {
            if (rect == null || rootRect == null) return;

            Vector2 rootSize = rootRect.sizeDelta;
            if (Mathf.Abs(rootSize.x) <= 0.01f || Mathf.Abs(rootSize.y) <= 0.01f)
            {
                rootSize = rootRect.rect.size;
            }

            bool sourceAlreadyLandscape = IsLandscapeSprite(sprite);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = sourceAlreadyLandscape
                ? Vector3.zero
                : new Vector3(0f, 0f, -90f);
            rect.sizeDelta = sourceAlreadyLandscape
                ? new Vector2(rootSize.x * scale, rootSize.y * scale)
                : new Vector2(rootSize.y * scale, rootSize.x * scale);
        }

        void ConfigureBoardGlowRect(RectTransform rect, RectTransform rootRect, float scale)
        {
            if (rect == null || rootRect == null) return;

            Vector2 rootSize = rootRect.sizeDelta;
            if (Mathf.Abs(rootSize.x) <= 0.01f || Mathf.Abs(rootSize.y) <= 0.01f)
            {
                rootSize = rootRect.rect.size;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            rect.sizeDelta = new Vector2(rootSize.x * scale, rootSize.y * scale);
        }

        void ConfigureSceneCardShadow(RectTransform shadowRect)
        {
            if (shadowRect == null || _cardShadowImage == null) return;

            _cardShadowRect = shadowRect;
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.pivot = new Vector2(0.5f, 0.5f);
            shadowRect.localScale = Vector3.one;
            shadowRect.localEulerAngles = Vector3.zero;

            RectTransform rootRect = transform as RectTransform;
            Vector2 rootSize = rootRect != null ? rootRect.sizeDelta : new Vector2(468f, 184f);
            if (Mathf.Abs(rootSize.x) <= 0.01f || Mathf.Abs(rootSize.y) <= 0.01f)
            {
                rootSize = rootRect != null ? rootRect.rect.size : new Vector2(468f, 184f);
            }

            shadowRect.anchoredPosition = new Vector2(0f, -rootSize.y * 0.12f);
            shadowRect.sizeDelta = new Vector2(rootSize.x * 0.82f, rootSize.y * 0.22f);
            shadowRect.SetAsFirstSibling();

            ApplySlicedUiSprite(_cardShadowImage);
            _cardShadowImage.enabled = true;
            _cardShadowImage.color = new Color(2f / 255f, 6f / 255f, 14f / 255f, 0.24f);
            _cardShadowImage.raycastTarget = false;
        }

        bool IsLandscapeSprite(Sprite sprite)
        {
            if (sprite == null) return false;
            Rect rect = sprite.rect;
            return rect.width >= rect.height;
        }

        static void ApplySlicedUiSprite(Image image)
        {
            if (image == null) return;

            try
            {
                Sprite roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                if (roundedSprite == null) return;

                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            catch
            {
                // Built-in UI skin can be unavailable in some stripped player contexts.
            }
        }


        void ConfigureGlowImage(RectTransform glowRect)
        {
            if (_glowImage == null || glowRect == null) return;

            _glowImage.raycastTarget = false;
            _glowImage.enabled = true;
            _glowImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.026f);

            var outline = _glowImage.GetComponent<Outline>();
            if (outline == null) outline = _glowImage.gameObject.AddComponent<Outline>();
            outline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.2f);
            outline.effectDistance = new Vector2(9f, -9f);
            outline.useGraphicAlpha = true;

            _glowPulse = _glowImage.GetComponent<UcgGuidancePulse>();
            if (_glowPulse == null) _glowPulse = _glowImage.gameObject.AddComponent<UcgGuidancePulse>();
            _glowPulse.targetImage = _glowImage;
            _glowPulse.targetOutline = outline;
            _glowPulse.targetRect = glowRect;
            _glowPulse.pulseAlpha = true;
            _glowPulse.alphaAmplitude = 0.018f;
            _glowPulse.pulseScale = true;
            _glowPulse.scaleAmplitude = 0.006f;
            _glowPulse.speed = 1.45f;
            _glowPulse.enabled = true;
            _glowPulse.CaptureBaseState();

            glowRect.SetAsFirstSibling();
        }

        public void ApplySceneSlotStateVisual(bool glowActive, bool boosted)
        {
            EnsureVisuals();
            ForceSceneCardBoardOrientation();

            if (_backgroundImage != null)
            {
                _backgroundImage.enabled = false;
                _backgroundImage.color = Color.clear;
                _backgroundImage.raycastTarget = false;
            }

            if (_rotatedCardImage != null)
            {
                _rotatedCardImage.raycastTarget = false;
                _rotatedCardImage.preserveAspect = true;
                if (_rotatedCardImage.sprite != null)
                {
                    _rotatedCardImage.enabled = true;
                    _rotatedCardImage.color = Color.white;
                }
            }

            ForceSceneCardBoardOrientation();

            if (_text != null)
            {
                _text.raycastTarget = false;
                _text.enabled = !HasVisibleCardArt;
            }

            RefreshActiveSceneGlow(glowActive, boosted);
        }

        public void PlayEffectSourcePulse()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;
            StartCoroutine(EffectSourcePulseOnceRoutine());
        }

        IEnumerator EffectSourcePulseOnceRoutine()
        {
            StartEffectSourceHighlight();
            yield return new WaitForSecondsRealtime(0.58f);
            StopEffectSourceHighlight();
        }

        public void StartEffectSourceHighlight()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;
            EnsureVisuals();
            EnsureEffectSourceHighlightOverlay();

            _effectSourceHighlightActive = true;
            if (_effectSourceHighlightRoutine != null)
            {
                StopCoroutine(_effectSourceHighlightRoutine);
                _effectSourceHighlightRoutine = null;
            }

            _effectSourceHighlightRoutine = StartCoroutine(EffectSourceHighlightRoutine());
        }

        public void StopEffectSourceHighlight()
        {
            _effectSourceHighlightActive = false;
            if (!Application.isPlaying || !gameObject.activeInHierarchy || _effectSourceHighlightRoutine == null)
            {
                HideEffectSourceHighlightOverlay();
            }
        }

        IEnumerator EffectSourceHighlightRoutine()
        {
            RectTransform rect = _effectSourceHighlightRect;
            if (rect == null) yield break;

            rect.SetAsLastSibling();
            rect.gameObject.SetActive(true);

            while (_effectSourceHighlightActive && rect != null)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 5.8f) * 0.5f;

                rect.localScale = Vector3.one * Mathf.Lerp(1.004f, 1.028f, pulse);
                if (_effectSourceHighlightImage != null)
                {
                    _effectSourceHighlightImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, Mathf.Lerp(0.03f, 0.085f, pulse));
                }

                if (_effectSourceHighlightOutline != null)
                {
                    _effectSourceHighlightOutline.enabled = true;
                    _effectSourceHighlightOutline.effectColor = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, Mathf.Lerp(0.32f, 0.54f, pulse));
                    _effectSourceHighlightOutline.effectDistance = new Vector2(3.8f, -3.8f);
                }

                yield return null;
            }

            float fadeDuration = 0.16f;
            float elapsed = 0f;
            float startFillAlpha = _effectSourceHighlightImage != null ? _effectSourceHighlightImage.color.a : 0f;
            float startOutlineAlpha = _effectSourceHighlightOutline != null ? _effectSourceHighlightOutline.effectColor.a : 0f;
            while (elapsed < fadeDuration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                rect.localScale = Vector3.one;

                if (_effectSourceHighlightImage != null)
                {
                    Color color = _effectSourceHighlightImage.color;
                    color.a = startFillAlpha * alpha;
                    _effectSourceHighlightImage.color = color;
                }

                if (_effectSourceHighlightOutline != null)
                {
                    Color color = _effectSourceHighlightOutline.effectColor;
                    color.a = startOutlineAlpha * alpha;
                    _effectSourceHighlightOutline.effectColor = color;
                }

                yield return null;
            }

            HideEffectSourceHighlightOverlay();
            _effectSourceHighlightRoutine = null;
        }

        void EnsureEffectSourceHighlightOverlay()
        {
            const string overlayName = "Scene Effect Source Highlight";
            if (_effectSourceHighlightRect != null && _effectSourceHighlightImage != null && _effectSourceHighlightOutline != null) return;

            Transform existing = transform.Find(overlayName);
            if (existing == null)
            {
                var overlayObject = new GameObject(overlayName, typeof(RectTransform), typeof(Image), typeof(Outline));
                overlayObject.transform.SetParent(transform, false);
                existing = overlayObject.transform;
            }

            _effectSourceHighlightRect = existing as RectTransform;
            _effectSourceHighlightImage = existing.GetComponent<Image>();
            if (_effectSourceHighlightImage == null) _effectSourceHighlightImage = existing.gameObject.AddComponent<Image>();
            _effectSourceHighlightOutline = existing.GetComponent<Outline>();
            if (_effectSourceHighlightOutline == null) _effectSourceHighlightOutline = existing.gameObject.AddComponent<Outline>();

            _effectSourceHighlightRect.anchorMin = Vector2.zero;
            _effectSourceHighlightRect.anchorMax = Vector2.one;
            _effectSourceHighlightRect.pivot = new Vector2(0.5f, 0.5f);
            _effectSourceHighlightRect.offsetMin = Vector2.zero;
            _effectSourceHighlightRect.offsetMax = Vector2.zero;
            _effectSourceHighlightRect.localScale = Vector3.one;
            _effectSourceHighlightRect.localEulerAngles = Vector3.zero;
            _effectSourceHighlightRect.SetAsLastSibling();

            Sprite roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (roundedSprite != null)
            {
                _effectSourceHighlightImage.sprite = roundedSprite;
                _effectSourceHighlightImage.type = Image.Type.Sliced;
            }
            _effectSourceHighlightImage.color = Color.clear;
            _effectSourceHighlightImage.raycastTarget = false;

            _effectSourceHighlightOutline.enabled = false;
            _effectSourceHighlightOutline.useGraphicAlpha = true;
            _effectSourceHighlightRect.gameObject.SetActive(false);
        }

        void HideEffectSourceHighlightOverlay()
        {
            if (_effectSourceHighlightRect != null)
            {
                _effectSourceHighlightRect.localScale = Vector3.one;
                _effectSourceHighlightRect.gameObject.SetActive(false);
            }
            if (_effectSourceHighlightImage != null) _effectSourceHighlightImage.color = Color.clear;
            if (_effectSourceHighlightOutline != null) _effectSourceHighlightOutline.enabled = false;
        }

        void RefreshActiveSceneGlow(bool glowActive, bool boosted)
        {
            if (_glowImage != null)
            {
                _glowImage.gameObject.SetActive(glowActive);
                _glowImage.enabled = glowActive;
                _glowImage.raycastTarget = false;
                _glowImage.color = boosted
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.055f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.026f);
            }

            Outline glowOutline = _glowImage != null ? _glowImage.GetComponent<Outline>() : null;
            if (glowOutline != null)
            {
                glowOutline.enabled = glowActive;
                glowOutline.effectColor = boosted
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.36f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.2f);
                glowOutline.effectDistance = boosted
                    ? new Vector2(12f, -12f)
                    : new Vector2(9f, -9f);
                glowOutline.useGraphicAlpha = true;
            }

            if (_glowPulse != null)
            {
                _glowPulse.enabled = glowActive;
                _glowPulse.alphaAmplitude = boosted ? 0.032f : 0.018f;
                _glowPulse.scaleAmplitude = boosted ? 0.012f : 0.006f;
                if (glowActive) _glowPulse.CaptureBaseState();
            }

            if (_rotatedCardOutline != null)
            {
                _rotatedCardOutline.enabled = true;
                _rotatedCardOutline.effectColor = boosted
                    ? new Color(0.75f, 0.98f, 1f, 0.52f)
                    : new Color(0.66f, 0.96f, 1f, 0.32f);
                _rotatedCardOutline.effectDistance = boosted
                    ? new Vector2(4f, -4f)
                    : new Vector2(2.5f, -2.5f);
                _rotatedCardOutline.useGraphicAlpha = true;
            }
        }

        void TryLoadExternalImage(bool hasSprite)
        {
            if (hasSprite || cardData == null) return;
            if (!cardData.IsExternalCard()) return;
            if (_requestedImageLocal == cardData.imageLocal) return;

            string requestImageLocal = cardData.imageLocal;
            _requestedImageLocal = requestImageLocal;
            UcgCardData requestCardData = cardData;

            UcgCardImageLoader.GetOrCreate().LoadCardImage(requestCardData, loadedSprite =>
            {
                if (this == null) return;
                if (cardData != requestCardData || requestImageLocal != _requestedImageLocal) return;
                if (loadedSprite == null) return;

                requestCardData.cardImage = loadedSprite;
                Refresh();
                ForceSceneCardBoardOrientation();
            });
        }

        float GetRootRotationZ()
        {
            RectTransform rootRect = transform as RectTransform;
            return rootRect != null ? NormalizeZ(rootRect.localEulerAngles.z) : 0f;
        }

        public float GetCardArtRotationZ()
        {
            RectTransform artRect = CardArtRect;
            return artRect != null ? NormalizeZ(artRect.localEulerAngles.z) : 0f;
        }

        static float NormalizeZ(float z)
        {
            z %= 360f;
            if (z > 180f) z -= 360f;
            if (z <= -180f) z += 360f;
            return z;
        }

        RectTransform EnsureChildRect(string childName, out Image image)
        {
            Transform child = transform.Find(childName);
            RectTransform rect;
            if (child == null)
            {
                var childObject = new GameObject(childName, typeof(RectTransform), typeof(Image));
                childObject.transform.SetParent(transform, false);
                rect = childObject.GetComponent<RectTransform>();
                image = childObject.GetComponent<Image>();
            }
            else
            {
                rect = child as RectTransform;
                image = child.GetComponent<Image>();
                if (image == null) image = child.gameObject.AddComponent<Image>();
            }

            return rect;
        }

        RectTransform EnsureChildText(string childName, out Text text)
        {
            Transform child = transform.Find(childName);
            RectTransform rect;
            if (child == null)
            {
                var childObject = new GameObject(childName, typeof(RectTransform), typeof(Text));
                childObject.transform.SetParent(transform, false);
                rect = childObject.GetComponent<RectTransform>();
                text = childObject.GetComponent<Text>();
            }
            else
            {
                rect = child as RectTransform;
                text = child.GetComponent<Text>();
                if (text == null) text = child.gameObject.AddComponent<Text>();
            }

            return rect;
        }
    }
}
