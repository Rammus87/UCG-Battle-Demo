using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    internal enum UcgCardPresentationState
    {
        Normal,
        Hover,
        Focus,
        Disabled,
        Back
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
        const float MinFloatAmplitude = 1f;
        const float MaxFloatAmplitude = 2f;
        const float MinFloatPeriod = 3f;
        const float MaxFloatPeriod = 5f;
        const float MaxBreathScale = 1.008f;
        const float HoverScaleBoost = 0.002f;
        const float FocusScaleBoost = 0.012f;

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
        float _motionPhase;
        float _motionSpeed;
        float _floatAmplitude;
        bool _motionSeedInitialized;
        bool _allowPositionFloating;
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
                if (_isDisabled) return UcgCardPresentationState.Disabled;
                if (_isCardBack) return UcgCardPresentationState.Back;
                if (_isSelected || _flipBoost > 0.01f) return UcgCardPresentationState.Focus;
                if (_isHovered) return UcgCardPresentationState.Hover;
                return UcgCardPresentationState.Normal;
            }
        }

        public void Configure(Image targetImage)
        {
            _targetImage = targetImage;
            _rectTransform = transform as RectTransform;
            EnsureMotionSeed();
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
            EnsureMotionSeed();
            EnsureVisuals();
            RefreshVisuals();
        }

        public void SetPositionFloatingEnabled(bool enabled)
        {
            _allowPositionFloating = enabled;
        }

        void LateUpdate()
        {
            ApplyPresentationMotion();
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

            EnsureMotionSeed();
            EnsureShadow();
            EnsureRim();
            EnsureHighlight();
            EnsureDisabledWash();
        }

        void EnsureMotionSeed()
        {
            if (_motionSeedInitialized) return;

            string entityKey = gameObject.GetEntityId().ToString();
            int hash = entityKey.GetHashCode();
            if (hash == int.MinValue) hash = 0;
            hash = Mathf.Abs(hash);
            float phaseSeed = (hash % 997) / 997f;
            float periodSeed = ((hash / 7) % 991) / 991f;
            float amplitudeSeed = ((hash / 17) % 983) / 983f;

            _motionPhase = phaseSeed * Mathf.PI * 2f;
            _motionSpeed = (Mathf.PI * 2f) / Mathf.Lerp(MinFloatPeriod, MaxFloatPeriod, periodSeed);
            _floatAmplitude = Mathf.Lerp(MinFloatAmplitude, MaxFloatAmplitude, amplitudeSeed);
            _motionSeedInitialized = true;
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
            float focus = _isSelected ? 1f : 0f;
            float disabled = _isDisabled ? 1f : 0f;
            float back = _isCardBack ? 1f : 0f;
            float focusLevel = Mathf.Max(focus, _flipBoost);
            float materialLift = Mathf.Max(hover * 0.34f, focusLevel * 0.72f);

            if (_shadowRect != null)
            {
                _shadowRect.anchorMin = new Vector2(0.08f, -0.10f);
                _shadowRect.anchorMax = new Vector2(0.92f, 0.11f);
                _shadowRect.offsetMin = new Vector2(-2f, Mathf.Lerp(-3f, -6f, materialLift));
                _shadowRect.offsetMax = new Vector2(2f, Mathf.Lerp(3f, 1f, materialLift));
                _shadowRect.localScale = Vector3.one;
                _shadowRect.localEulerAngles = Vector3.zero;
                _shadowRect.gameObject.SetActive(true);
                _shadowRect.SetAsFirstSibling();
            }

            if (_shadowImage != null)
            {
                float shadowAlpha = Mathf.Lerp(0.098f + back * 0.012f, 0.19f, materialLift) * (1f - disabled * 0.35f);
                _shadowImage.color = new Color(58f / 255f, 48f / 255f, 120f / 255f, shadowAlpha);
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
                    _isCardBack ? UcgToolUiPalette.BrandPinkLight : UcgToolUiPalette.GlassBorder,
                    _isCardBack ? 0.16f : 0.14f);
                Color hoverRim = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.24f);
                Color focusRim = UcgToolUiPalette.WithAlpha(UcgToolUiPalette.FocusCyan, 0.58f);
                Color rimActive = Color.Lerp(hoverRim, focusRim, Mathf.Max(focus, _flipBoost * 0.6f));

                _rimOutline.enabled = true;
                _rimOutline.effectColor = Color.Lerp(
                    rimBase,
                    rimActive,
                    Mathf.Max(Mathf.Max(focus * 0.86f, hover * 0.52f), _flipBoost * 0.48f));
                _rimOutline.effectDistance = new Vector2(Mathf.Lerp(0.65f, 1.22f, materialLift), Mathf.Lerp(-0.65f, -1.22f, materialLift));
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
                float baseHighlightAlpha = _isCardBack ? 0.012f : 0.004f;
                float activeHighlightAlpha = _isCardBack ? 0.032f : 0.018f;
                float alpha = Mathf.Lerp(baseHighlightAlpha, activeHighlightAlpha, Mathf.Max(Mathf.Max(hover * 0.36f, focus * 0.68f), _flipBoost * 0.52f));
                Color highlightColor = _isCardBack
                    ? UcgToolUiPalette.BrandPinkLight
                    : Color.Lerp(UcgToolUiPalette.SoftWhite, UcgToolUiPalette.BrandPinkLight, 0.34f);
                _highlightImage.color = UcgToolUiPalette.WithAlpha(highlightColor, alpha * (1f - disabled * 0.65f));
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
                _disabledWashImage.color = new Color(0.08f, 0.08f, 0.13f, _isDisabled ? 0.22f : 0f);
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
                    tint = Color.Lerp(tint, new Color(0.62f, 0.62f, 0.68f, tint.a), 0.38f);
                    tint.a = Mathf.Min(tint.a, 0.96f);
                }
                else
                {
                    tint = Color.Lerp(tint, Color.white, hover * 0.05f + focusLevel * 0.07f);
                }

                if (!_isDisabled) tint.a = 1f;
                _targetImage.color = tint;
            }

            ApplyPresentationMotion();
        }

        void ApplyPresentationMotion()
        {
            if (!_motionSeedInitialized) EnsureMotionSeed();

            float disabledFactor = _isDisabled ? 0.45f : 1f;
            float hover = _isHovered ? 1f : 0f;
            float focus = Mathf.Max(_isSelected ? 1f : 0f, _flipBoost);
            float time = Time.unscaledTime;
            float floatY = _allowPositionFloating
                ? Mathf.Sin(time * _motionSpeed + _motionPhase) * _floatAmplitude * disabledFactor
                : 0f;
            float breath = 0.5f + Mathf.Sin(time * (_motionSpeed * 0.78f) + _motionPhase * 1.37f) * 0.5f;
            float scale = Mathf.Min(1.02f, Mathf.Lerp(1f, MaxBreathScale, breath) + hover * HoverScaleBoost + focus * FocusScaleBoost);
            float shadowScale = Mathf.Min(1.015f, 1f + focus * 0.012f + hover * 0.004f);

            ApplyMotionTo(_shadowRect, new Vector3(0f, floatY * 0.22f, 0f), new Vector3(shadowScale, 1f + (shadowScale - 1f) * 0.5f, 1f));
            ApplyMotionTo(_rimRect, new Vector3(0f, floatY, 0f), Vector3.one * scale);
            ApplyMotionTo(_highlightRect, new Vector3(0f, floatY, 0f), Vector3.one * scale);
            ApplyMotionTo(_disabledWashRect, new Vector3(0f, floatY, 0f), Vector3.one * scale);
        }

        static void ApplyMotionTo(RectTransform rect, Vector3 localPosition, Vector3 localScale)
        {
            if (rect == null) return;

            rect.localPosition = localPosition;
            rect.localScale = localScale;
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
