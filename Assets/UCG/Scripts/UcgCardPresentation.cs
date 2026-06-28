using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    internal enum UcgCardPresentationState
    {
        Normal,
        Hover,
        Selected,
        Disabled,
        Flip
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    internal sealed class UcgCardPresentation : MonoBehaviour
    {
        const string DefaultCardBackResourcePath = "UCG/CardBacks/ucg_card_back_standard";
        const string ShadowName = "Card Presentation Shadow";
        const string RimName = "Card Presentation Rim";
        const string HighlightName = "Card Presentation Highlight";
        const string DisabledWashName = "Card Presentation Disabled Wash";

        static Sprite _defaultCardBackSprite;

        Image _targetImage;
        RectTransform _rectTransform;
        RectTransform _shadowRect;
        Image _shadowImage;
        RectTransform _rimRect;
        Image _rimImage;
        Outline _rimOutline;
        RectTransform _highlightRect;
        Image _highlightImage;
        RectTransform _disabledWashRect;
        Image _disabledWashImage;
        Coroutine _flipRoutine;

        bool _isCardBack;
        bool _isHovered;
        bool _isSelected;
        bool _isDisabled;
        float _flipBoost;
        Color _baseTint = Color.white;

        public static Sprite DefaultCardBackSprite
        {
            get
            {
                if (_defaultCardBackSprite == null)
                {
                    _defaultCardBackSprite = Resources.Load<Sprite>(DefaultCardBackResourcePath);
                }

                return _defaultCardBackSprite;
            }
        }

        public UcgCardPresentationState CurrentState
        {
            get
            {
                if (_flipBoost > 0.01f) return UcgCardPresentationState.Flip;
                if (_isDisabled) return UcgCardPresentationState.Disabled;
                if (_isSelected) return UcgCardPresentationState.Selected;
                if (_isHovered) return UcgCardPresentationState.Hover;
                return UcgCardPresentationState.Normal;
            }
        }

        public void Configure(Image targetImage)
        {
            _targetImage = targetImage;
            _rectTransform = transform as RectTransform;
            EnsureVisuals();
            RefreshVisuals();
        }

        public void ApplyState(bool isCardBack, bool isHovered, bool isSelected, bool isDisabled, Color baseTint)
        {
            _isCardBack = isCardBack;
            _isHovered = isHovered;
            _isSelected = isSelected;
            _isDisabled = isDisabled;
            _baseTint = baseTint;
            EnsureVisuals();
            RefreshVisuals();
        }

        public void PlayFlipFeedback()
        {
            if (!isActiveAndEnabled)
            {
                _flipBoost = 0f;
                RefreshVisuals();
                return;
            }

            if (_flipRoutine != null)
            {
                StopCoroutine(_flipRoutine);
            }

            _flipRoutine = StartCoroutine(FlipFeedbackRoutine());
        }

        void OnDisable()
        {
            if (_flipRoutine != null)
            {
                StopCoroutine(_flipRoutine);
                _flipRoutine = null;
            }

            _flipBoost = 0f;
        }

        IEnumerator FlipFeedbackRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.26f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _flipBoost = Mathf.Sin(t * Mathf.PI);
                RefreshVisuals();
                yield return null;
            }

            _flipBoost = 0f;
            _flipRoutine = null;
            RefreshVisuals();
        }

        void EnsureVisuals()
        {
            if (_rectTransform == null) _rectTransform = transform as RectTransform;

            EnsureShadow();
            EnsureRim();
            EnsureHighlight();
            EnsureDisabledWash();
        }

        void EnsureShadow()
        {
            if (_shadowRect != null && _shadowImage != null) return;

            Transform existing = transform.Find(ShadowName);
            if (existing == null)
            {
                var shadowObject = new GameObject(ShadowName, typeof(RectTransform), typeof(Image));
                shadowObject.transform.SetParent(transform, false);
                existing = shadowObject.transform;
            }

            _shadowRect = existing as RectTransform;
            _shadowImage = existing.GetComponent<Image>();
            if (_shadowImage == null) _shadowImage = existing.gameObject.AddComponent<Image>();
            ApplySlicedUiSprite(_shadowImage);
            _shadowImage.raycastTarget = false;
            _shadowRect.SetAsFirstSibling();
        }

        void EnsureRim()
        {
            if (_rimRect != null && _rimImage != null && _rimOutline != null) return;

            Transform existing = transform.Find(RimName);
            if (existing == null)
            {
                var rimObject = new GameObject(RimName, typeof(RectTransform), typeof(Image), typeof(Outline));
                rimObject.transform.SetParent(transform, false);
                existing = rimObject.transform;
            }

            _rimRect = existing as RectTransform;
            _rimImage = existing.GetComponent<Image>();
            if (_rimImage == null) _rimImage = existing.gameObject.AddComponent<Image>();
            _rimOutline = existing.GetComponent<Outline>();
            if (_rimOutline == null) _rimOutline = existing.gameObject.AddComponent<Outline>();
            ApplySlicedUiSprite(_rimImage);
            _rimImage.raycastTarget = false;
            _rimOutline.useGraphicAlpha = true;
            _rimRect.SetAsLastSibling();
        }

        void EnsureHighlight()
        {
            if (_highlightRect != null && _highlightImage != null) return;

            Transform existing = transform.Find(HighlightName);
            if (existing == null)
            {
                var highlightObject = new GameObject(HighlightName, typeof(RectTransform), typeof(Image));
                highlightObject.transform.SetParent(transform, false);
                existing = highlightObject.transform;
            }

            _highlightRect = existing as RectTransform;
            _highlightImage = existing.GetComponent<Image>();
            if (_highlightImage == null) _highlightImage = existing.gameObject.AddComponent<Image>();
            ApplySlicedUiSprite(_highlightImage);
            _highlightImage.raycastTarget = false;
            _highlightRect.SetAsLastSibling();
        }

        void EnsureDisabledWash()
        {
            if (_disabledWashRect != null && _disabledWashImage != null) return;

            Transform existing = transform.Find(DisabledWashName);
            if (existing == null)
            {
                var washObject = new GameObject(DisabledWashName, typeof(RectTransform), typeof(Image));
                washObject.transform.SetParent(transform, false);
                existing = washObject.transform;
            }

            _disabledWashRect = existing as RectTransform;
            _disabledWashImage = existing.GetComponent<Image>();
            if (_disabledWashImage == null) _disabledWashImage = existing.gameObject.AddComponent<Image>();
            ApplySlicedUiSprite(_disabledWashImage);
            _disabledWashImage.raycastTarget = false;
            _disabledWashRect.SetAsLastSibling();
        }

        void RefreshVisuals()
        {
            if (_targetImage == null) _targetImage = GetComponent<Image>();
            float hover = _isHovered ? 1f : 0f;
            float selected = _isSelected ? 1f : 0f;
            float disabled = _isDisabled ? 1f : 0f;
            float materialLift = Mathf.Max(hover, selected, _flipBoost);

            if (_shadowRect != null)
            {
                _shadowRect.anchorMin = new Vector2(0.08f, -0.10f);
                _shadowRect.anchorMax = new Vector2(0.92f, 0.12f);
                _shadowRect.offsetMin = new Vector2(-2f, Mathf.Lerp(-3f, -8f, materialLift));
                _shadowRect.offsetMax = new Vector2(2f, Mathf.Lerp(3f, 1f, materialLift));
                _shadowRect.localScale = Vector3.one;
                _shadowRect.localEulerAngles = Vector3.zero;
                _shadowRect.gameObject.SetActive(true);
                _shadowRect.SetAsFirstSibling();
            }

            if (_shadowImage != null)
            {
                _shadowImage.color = new Color(4f / 255f, 9f / 255f, 18f / 255f, Mathf.Lerp(0.10f, 0.22f, materialLift) * (1f - disabled * 0.25f));
            }

            if (_rimRect != null)
            {
                _rimRect.anchorMin = Vector2.zero;
                _rimRect.anchorMax = Vector2.one;
                _rimRect.offsetMin = new Vector2(-0.75f, -0.75f);
                _rimRect.offsetMax = new Vector2(0.75f, 0.75f);
                _rimRect.localScale = Vector3.one;
                _rimRect.localEulerAngles = Vector3.zero;
                _rimRect.gameObject.SetActive(true);
                _rimRect.SetAsLastSibling();
            }

            if (_rimImage != null)
            {
                _rimImage.color = Color.clear;
            }

            if (_rimOutline != null)
            {
                Color rimBase = UcgToolUiPalette.WithAlpha(
                    _isCardBack ? UcgToolUiPalette.SoftWhite : UcgToolUiPalette.GlassBorder,
                    _isCardBack ? 0.18f : 0.14f);
                Color rimActive = _isSelected
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.76f)
                    : UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, Mathf.Lerp(0.16f, 0.32f, hover));

                _rimOutline.enabled = true;
                _rimOutline.effectColor = Color.Lerp(
                    rimBase,
                    rimActive,
                    Mathf.Max(selected, hover * 0.62f, _flipBoost * 0.42f));
                _rimOutline.effectDistance = new Vector2(Mathf.Lerp(0.75f, 1.8f, materialLift), Mathf.Lerp(-0.75f, -1.8f, materialLift));
            }

            if (_highlightRect != null)
            {
                _highlightRect.anchorMin = new Vector2(0.05f, 0.72f);
                _highlightRect.anchorMax = new Vector2(0.72f, 0.98f);
                _highlightRect.offsetMin = Vector2.zero;
                _highlightRect.offsetMax = Vector2.zero;
                _highlightRect.localScale = Vector3.one;
                _highlightRect.localEulerAngles = new Vector3(0f, 0f, -8f);
                _highlightRect.gameObject.SetActive(true);
                _highlightRect.SetAsLastSibling();
            }

            if (_highlightImage != null)
            {
                float baseHighlightAlpha = _isCardBack ? 0.006f : 0.004f;
                float activeHighlightAlpha = _isCardBack ? 0.026f : 0.018f;
                float alpha = Mathf.Lerp(baseHighlightAlpha, activeHighlightAlpha, Mathf.Max(hover, _flipBoost * 0.6f));
                _highlightImage.color = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.SoftWhite, alpha * (1f - disabled * 0.4f));
            }

            if (_disabledWashRect != null)
            {
                _disabledWashRect.anchorMin = Vector2.zero;
                _disabledWashRect.anchorMax = Vector2.one;
                _disabledWashRect.offsetMin = Vector2.zero;
                _disabledWashRect.offsetMax = Vector2.zero;
                _disabledWashRect.localScale = Vector3.one;
                _disabledWashRect.localEulerAngles = Vector3.zero;
                _disabledWashRect.gameObject.SetActive(_isDisabled);
                _disabledWashRect.SetAsLastSibling();
            }

            if (_disabledWashImage != null)
            {
                _disabledWashImage.color = new Color(0.08f, 0.09f, 0.10f, _isDisabled ? 0.36f : 0f);
            }

            if (_targetImage != null)
            {
                Color tint = _baseTint;
                if (_isCardBack && !_isDisabled)
                {
                    tint = Color.white;
                }
                else if (_isDisabled)
                {
                    tint = Color.Lerp(tint, new Color(0.48f, 0.48f, 0.48f, tint.a), 0.64f);
                    tint.a = Mathf.Min(tint.a, 0.96f);
                }

                if (!_isDisabled) tint.a = 1f;
                _targetImage.color = tint;
            }
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
                // Built-in UI skin may be unavailable in stripped player contexts.
            }
        }
    }
}
