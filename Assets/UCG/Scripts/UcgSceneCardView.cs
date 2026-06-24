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
        Image _glowImage;
        Image _rotatedCardImage;
        Outline _rotatedCardOutline;
        UcgGuidancePulse _glowPulse;
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

        bool IsLandscapeSprite(Sprite sprite)
        {
            if (sprite == null) return false;
            Rect rect = sprite.rect;
            return rect.width >= rect.height;
        }

        void ConfigureGlowImage(RectTransform glowRect)
        {
            if (_glowImage == null || glowRect == null) return;

            _glowImage.raycastTarget = false;
            _glowImage.enabled = true;
            _glowImage.color = new Color(0.32f, 0.88f, 1f, 0.026f);

            var outline = _glowImage.GetComponent<Outline>();
            if (outline == null) outline = _glowImage.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.62f, 0.96f, 1f, 0.2f);
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

        void RefreshActiveSceneGlow(bool glowActive, bool boosted)
        {
            if (_glowImage != null)
            {
                _glowImage.gameObject.SetActive(glowActive);
                _glowImage.enabled = glowActive;
                _glowImage.raycastTarget = false;
                _glowImage.color = boosted
                    ? new Color(0.45f, 0.95f, 1f, 0.055f)
                    : new Color(0.32f, 0.88f, 1f, 0.026f);
            }

            Outline glowOutline = _glowImage != null ? _glowImage.GetComponent<Outline>() : null;
            if (glowOutline != null)
            {
                glowOutline.enabled = glowActive;
                glowOutline.effectColor = boosted
                    ? new Color(0.7f, 0.98f, 1f, 0.36f)
                    : new Color(0.62f, 0.96f, 1f, 0.2f);
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
