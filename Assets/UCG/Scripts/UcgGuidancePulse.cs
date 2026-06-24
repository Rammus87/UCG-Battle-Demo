using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgGuidancePulse : MonoBehaviour
    {
        public Image targetImage;
        public Text targetText;
        public Outline targetOutline;
        public RectTransform targetRect;
        public bool pulseScale;
        public bool pulseAlpha = true;
        public float scaleAmplitude = 0.02f;
        public float alphaAmplitude = 0.14f;
        public float bobAmplitude = 0f;
        public float rotateDegreesPerSecond = 0f;
        public float speed = 2.4f;

        Vector3 _baseScale;
        Vector2 _baseAnchoredPosition;
        Quaternion _baseRotation;
        Color _baseImageColor;
        Color _baseTextColor;
        Color _baseOutlineColor;
        Vector2 _baseOutlineDistance;
        bool _hasBase;

        void OnEnable()
        {
            CacheBaseState();
        }

        void OnDisable()
        {
            RestoreBaseState();
        }

        void Update()
        {
            if (!_hasBase) CacheBaseState();

            float pulse = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            float centeredPulse = pulse - 0.5f;

            if (pulseScale && targetRect != null)
            {
                float scale = 1f + centeredPulse * scaleAmplitude;
                targetRect.localScale = _baseScale * scale;
            }

            if (bobAmplitude > 0f && targetRect != null)
            {
                targetRect.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, centeredPulse * bobAmplitude);
            }

            if (pulseAlpha)
            {
                float alphaOffset = centeredPulse * alphaAmplitude;
                if (targetImage != null)
                {
                    Color color = _baseImageColor;
                    color.a = Mathf.Clamp01(_baseImageColor.a + alphaOffset);
                    targetImage.color = color;
                }

                if (targetText != null)
                {
                    Color color = _baseTextColor;
                    color.a = Mathf.Clamp01(_baseTextColor.a + alphaOffset);
                    targetText.color = color;
                }

                if (targetOutline != null)
                {
                    Color color = _baseOutlineColor;
                    color.a = Mathf.Clamp01(_baseOutlineColor.a + alphaOffset);
                    targetOutline.effectColor = color;
                }
            }

            if (targetOutline != null)
            {
                float distanceScale = 1f + centeredPulse * 0.22f;
                targetOutline.effectDistance = _baseOutlineDistance * distanceScale;
            }

            if (rotateDegreesPerSecond != 0f && targetRect != null)
            {
                targetRect.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, Time.unscaledTime * rotateDegreesPerSecond);
            }
        }

        public void CaptureBaseState()
        {
            _hasBase = false;
            CacheBaseState();
        }

        void CacheBaseState()
        {
            if (targetRect == null) targetRect = transform as RectTransform;
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetText == null) targetText = GetComponent<Text>();
            if (targetOutline == null) targetOutline = GetComponent<Outline>();

            if (targetRect != null)
            {
                _baseScale = targetRect.localScale;
                _baseAnchoredPosition = targetRect.anchoredPosition;
                _baseRotation = targetRect.localRotation;
            }

            if (targetImage != null) _baseImageColor = targetImage.color;
            if (targetText != null) _baseTextColor = targetText.color;
            if (targetOutline != null)
            {
                _baseOutlineColor = targetOutline.effectColor;
                _baseOutlineDistance = targetOutline.effectDistance;
            }

            _hasBase = true;
        }

        void RestoreBaseState()
        {
            if (!_hasBase) return;

            if (targetRect != null)
            {
                targetRect.localScale = _baseScale;
                targetRect.anchoredPosition = _baseAnchoredPosition;
                targetRect.localRotation = _baseRotation;
            }

            if (targetImage != null) targetImage.color = _baseImageColor;
            if (targetText != null) targetText.color = _baseTextColor;
            if (targetOutline != null)
            {
                targetOutline.effectColor = _baseOutlineColor;
                targetOutline.effectDistance = _baseOutlineDistance;
            }
        }
    }
}
