using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    public struct UcgCardMoveAnimationOptions
    {
        public float duration;
        public float staggerDelay;
        public bool startFaceDown;
        public bool endFaceUp;
        public float flipAtProgress;
        public float arcHeight;
        public Vector3 scaleFrom;
        public Vector3 scaleTo;
        public Vector3 eulerFrom;
        public Vector3 eulerTo;
        public bool followTarget;
        public bool useDissolveOnDiscard;
        public Action onComplete;

        public static UcgCardMoveAnimationOptions Default
        {
            get
            {
                return new UcgCardMoveAnimationOptions
                {
                    duration = 0.5f,
                    staggerDelay = 0f,
                    startFaceDown = false,
                    endFaceUp = false,
                    flipAtProgress = -1f,
                    arcHeight = 120f,
                    scaleFrom = Vector3.one,
                    scaleTo = Vector3.one,
                    eulerFrom = Vector3.zero,
                    eulerTo = Vector3.zero,
                    followTarget = false,
                    useDissolveOnDiscard = false,
                    onComplete = null
                };
            }
        }
    }

    public sealed class UcgCardMoveAnimationSystem : MonoBehaviour
    {
        public Coroutine MoveCardArc(RectTransform card, RectTransform source, RectTransform target, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(MoveCardArcRoutine(card, source, target, options));
        }

        public Coroutine MoveCardArcToTarget(RectTransform card, RectTransform target, UcgCardMoveAnimationOptions options)
        {
            options.followTarget = true;
            return StartCoroutine(MoveCardArcRoutine(card, null, target, options));
        }

        public Coroutine PlaySelectedFeedback(RectTransform card, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(SelectedFeedbackRoutine(card, options));
        }

        public Coroutine MoveCardArc(RectTransform card, Vector2 sourceAnchoredPosition, Vector2 targetAnchoredPosition, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(MoveCardArcAnchoredRoutine(card, sourceAnchoredPosition, targetAnchoredPosition, options));
        }

        public Coroutine DrawCardToHand(RectTransform card, RectTransform sourceDeck, RectTransform targetHandSlot, UcgCardMoveAnimationOptions options)
        {
            options.startFaceDown = true;
            options.endFaceUp = true;
            if (options.flipAtProgress < 0f) options.flipAtProgress = 0.72f;
            return MoveCardArc(card, sourceDeck, targetHandSlot, options);
        }

        public Coroutine MoveCardToDiscard(RectTransform card, RectTransform source, RectTransform discardPile, UcgCardMoveAnimationOptions options)
        {
            return MoveCardArc(card, source, discardPile, options);
        }

        public Coroutine MoveCardToDiscardWithDissolve(RectTransform card, RectTransform discardPile, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(MoveCardToDiscardWithDissolveRoutine(card, discardPile, options));
        }

        public Coroutine BounceTarget(RectTransform target, float distance, float duration)
        {
            return StartCoroutine(BounceTargetRoutine(target, distance, duration));
        }

        public Coroutine ReturnCardToDeckBottom(RectTransform card, RectTransform sourceHand, RectTransform deckPile, UcgCardMoveAnimationOptions options)
        {
            options.endFaceUp = false;
            return MoveCardArc(card, sourceHand, deckPile, options);
        }

        public Coroutine FlipCard(UcgCardView card, bool faceUp, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(FlipCardRoutine(card, faceUp, options));
        }

        public Coroutine AnimateHandInsert(RectTransform card, int targetIndex, UcgCardMoveAnimationOptions options)
        {
            return StartCoroutine(AnimateHandInsertRoutine(card, targetIndex, options));
        }

        IEnumerator MoveCardArcRoutine(RectTransform card, RectTransform source, RectTransform target, UcgCardMoveAnimationOptions options)
        {
            if (card == null)
            {
                options.onComplete?.Invoke();
                yield break;
            }

            if (options.staggerDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(options.staggerDelay);
            }

            UcgCardView cardView = card.GetComponent<UcgCardView>();
            if (cardView != null && options.startFaceDown)
            {
                cardView.SetFaceDown(true);
            }

            Vector3 startWorld = source != null ? source.TransformPoint(source.rect.center) : card.position;
            Vector3 endWorld = target != null ? target.TransformPoint(target.rect.center) : card.position;
            if (source != null)
            {
                card.position = startWorld;
            }

            Vector3 scaleFrom = options.scaleFrom == Vector3.zero ? card.localScale : options.scaleFrom;
            Vector3 scaleTo = options.scaleTo == Vector3.zero ? card.localScale : options.scaleTo;
            bool animateRotation = options.eulerFrom != Vector3.zero || options.eulerTo != Vector3.zero;
            float duration = Mathf.Max(0.01f, options.duration);
            float arcHeight = options.arcHeight;
            bool flipped = false;
            float elapsed = 0f;

            while (elapsed < duration && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                if (options.followTarget && target != null)
                {
                    endWorld = target.TransformPoint(target.rect.center);
                }
                Vector3 position = Vector3.LerpUnclamped(startWorld, endWorld, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                card.position = position;
                card.localScale = GetArcScale(scaleFrom, scaleTo, eased, t, options.flipAtProgress);
                if (animateRotation)
                {
                    card.localEulerAngles = Vector3.LerpUnclamped(options.eulerFrom, options.eulerTo, eased);
                }

                if (!flipped && cardView != null && options.flipAtProgress >= 0f && t >= options.flipAtProgress)
                {
                    flipped = true;
                    cardView.SetFaceDown(!options.endFaceUp);
                }

                yield return null;
            }

            if (card != null)
            {
                card.position = endWorld;
                card.localScale = scaleTo;
                if (animateRotation)
                {
                    card.localEulerAngles = options.eulerTo;
                }
            }

            if (cardView != null && options.endFaceUp)
            {
                cardView.SetFaceDown(false);
            }

            options.onComplete?.Invoke();
        }

        IEnumerator MoveCardArcAnchoredRoutine(RectTransform card, Vector2 sourceAnchoredPosition, Vector2 targetAnchoredPosition, UcgCardMoveAnimationOptions options)
        {
            if (card == null)
            {
                options.onComplete?.Invoke();
                yield break;
            }

            if (options.staggerDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(options.staggerDelay);
            }

            UcgCardView cardView = card.GetComponent<UcgCardView>();
            if (cardView != null && options.startFaceDown)
            {
                cardView.SetFaceDown(true);
            }

            card.anchoredPosition = sourceAnchoredPosition;
            Vector3 scaleFrom = options.scaleFrom == Vector3.zero ? card.localScale : options.scaleFrom;
            Vector3 scaleTo = options.scaleTo == Vector3.zero ? card.localScale : options.scaleTo;
            bool animateRotation = options.eulerFrom != Vector3.zero || options.eulerTo != Vector3.zero;
            float duration = Mathf.Max(0.01f, options.duration);
            float arcHeight = options.arcHeight;
            bool flipped = false;
            float elapsed = 0f;

            while (elapsed < duration && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                Vector2 position = Vector2.LerpUnclamped(sourceAnchoredPosition, targetAnchoredPosition, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                card.anchoredPosition = position;
                card.localScale = GetArcScale(scaleFrom, scaleTo, eased, t, options.flipAtProgress);
                if (animateRotation)
                {
                    card.localEulerAngles = Vector3.LerpUnclamped(options.eulerFrom, options.eulerTo, eased);
                }

                if (!flipped && cardView != null && options.flipAtProgress >= 0f && t >= options.flipAtProgress)
                {
                    flipped = true;
                    cardView.SetFaceDown(!options.endFaceUp);
                }

                yield return null;
            }

            if (card != null)
            {
                card.anchoredPosition = targetAnchoredPosition;
                card.localScale = scaleTo;
                if (animateRotation)
                {
                    card.localEulerAngles = options.eulerTo;
                }
            }

            if (cardView != null && options.endFaceUp)
            {
                cardView.SetFaceDown(false);
            }

            options.onComplete?.Invoke();
        }

        IEnumerator FlipCardRoutine(UcgCardView card, bool faceUp, UcgCardMoveAnimationOptions options)
        {
            if (card == null)
            {
                options.onComplete?.Invoke();
                yield break;
            }

            float duration = Mathf.Max(0.01f, options.duration <= 0f ? 0.22f : options.duration);
            RectTransform rect = card.transform as RectTransform;
            Vector3 baseScale = rect != null ? rect.localScale : Vector3.one;
            bool applied = false;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float x = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
                if (!applied && t >= 0.5f)
                {
                    applied = true;
                    card.SetFaceDown(!faceUp);
                }

                if (rect != null)
                {
                    rect.localScale = new Vector3(Mathf.Max(0.08f, baseScale.x * x), baseScale.y, baseScale.z);
                }

                yield return null;
            }

            if (rect != null) rect.localScale = baseScale;
            card.SetFaceDown(!faceUp);
            options.onComplete?.Invoke();
        }

        IEnumerator AnimateHandInsertRoutine(RectTransform card, int targetIndex, UcgCardMoveAnimationOptions options)
        {
            if (card == null)
            {
                options.onComplete?.Invoke();
                yield break;
            }

            if (options.staggerDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(options.staggerDelay);
            }

            card.SetSiblingIndex(Mathf.Max(0, targetIndex));
            options.onComplete?.Invoke();
        }

        IEnumerator MoveCardToDiscardWithDissolveRoutine(RectTransform card, RectTransform discardPile, UcgCardMoveAnimationOptions options)
        {
            Action onComplete = options.onComplete;
            options.onComplete = null;

            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            options.endFaceUp = true;
            options.flipAtProgress = -1f;
            if (options.duration <= 0f) options.duration = 0.46f;
            if (options.arcHeight <= 0f) options.arcHeight = 82f;
            if (options.scaleTo == Vector3.zero) options.scaleTo = Vector3.one * 0.52f;

            yield return MoveCardArcRoutine(card, null, discardPile, options);

            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            yield return DiscardDissolveRoutine(card, card.localScale, 0.32f);
            onComplete?.Invoke();
        }

        IEnumerator DiscardDissolveRoutine(RectTransform card, Vector3 settledScale, float duration)
        {
            if (card == null) yield break;

            Transform particleParent = card.parent;
            Vector3 center = card.position;
            Vector3 startScale = settledScale == Vector3.zero ? card.localScale : settledScale;
            card.localScale = startScale;
            SpawnDiscardParticles(particleParent, center, duration);

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = EaseOutCubic(t);
                float dissolveScale = Mathf.Lerp(1f, 0.04f, eased);
                card.localScale = startScale * dissolveScale;
                yield return null;
            }

            if (card != null)
            {
                card.localScale = Vector3.zero;
            }
        }

        void SpawnDiscardParticles(Transform parent, Vector3 centerWorld, float duration)
        {
            if (parent == null) return;

            const int particleCount = 12;
            for (int i = 0; i < particleCount; i++)
            {
                var particleObject = new GameObject("Discard Energy Particle", typeof(RectTransform), typeof(Image));
                particleObject.transform.SetParent(parent, false);
                var rect = particleObject.GetComponent<RectTransform>();
                var image = particleObject.GetComponent<Image>();
                rect.position = centerWorld;
                rect.sizeDelta = Vector2.one * UnityEngine.Random.Range(3.5f, 7.5f);
                rect.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-18f, 18f));
                image.raycastTarget = false;
                image.color = UnityEngine.Random.value > 0.46f
                    ? UcgToolUiPalette.WithAlpha(UcgToolUiPalette.BrandPinkLight, 0.42f)
                    : new Color(0.58f, 0.62f, 0.72f, 0.38f);

                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-32f, 32f),
                    UnityEngine.Random.Range(-22f, 28f),
                    0f);
                StartCoroutine(DiscardParticleRoutine(rect, image, centerWorld, centerWorld + offset, duration * UnityEngine.Random.Range(0.75f, 1.15f)));
            }
        }

        IEnumerator DiscardParticleRoutine(RectTransform particle, Image image, Vector3 startWorld, Vector3 endWorld, float duration)
        {
            if (particle == null || image == null) yield break;

            Color startColor = image.color;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration && particle != null && image != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = EaseOutCubic(t);
                particle.position = Vector3.LerpUnclamped(startWorld, endWorld, eased);
                particle.localScale = Vector3.one * Mathf.Lerp(1f, 0.18f, eased);
                image.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, eased));
                yield return null;
            }

            if (particle != null)
            {
                Destroy(particle.gameObject);
            }
        }

        IEnumerator BounceTargetRoutine(RectTransform target, float distance, float duration)
        {
            if (target == null) yield break;

            Vector2 basePosition = target.anchoredPosition;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float offset = Mathf.Sin(t * Mathf.PI) * distance;
                target.anchoredPosition = basePosition + new Vector2(0f, offset);
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = basePosition;
            }
        }

        IEnumerator SelectedFeedbackRoutine(RectTransform card, UcgCardMoveAnimationOptions options)
        {
            if (card == null)
            {
                options.onComplete?.Invoke();
                yield break;
            }

            float duration = Mathf.Max(0.01f, options.duration <= 0f ? 0.12f : options.duration);
            Vector3 startScale = options.scaleFrom == Vector3.zero ? card.localScale : options.scaleFrom;
            Vector3 targetScale = options.scaleTo == Vector3.zero ? startScale * 1.05f : options.scaleTo;
            float elapsed = 0f;

            while (elapsed < duration && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                card.localScale = Vector3.LerpUnclamped(startScale, targetScale, pulse);
                yield return null;
            }

            if (card != null)
            {
                card.localScale = startScale;
            }

            options.onComplete?.Invoke();
        }

        static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        static Vector3 GetArcScale(Vector3 scaleFrom, Vector3 scaleTo, float eased, float progress, float flipAtProgress)
        {
            Vector3 scale = Vector3.LerpUnclamped(scaleFrom, scaleTo, eased);
            if (flipAtProgress < 0f) return scale;

            const float halfWindow = 0.11f;
            float start = Mathf.Clamp01(flipAtProgress - halfWindow);
            float end = Mathf.Clamp01(flipAtProgress + halfWindow);
            if (progress <= start || progress >= end) return scale;

            float flipT = Mathf.InverseLerp(start, end, progress);
            float flipScale = Mathf.Abs(Mathf.Cos(flipT * Mathf.PI));
            scale.x = Mathf.Max(0.08f, scale.x * flipScale);
            return scale;
        }
    }
}
