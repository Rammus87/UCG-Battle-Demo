using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    public static class UcgCardImageApplier
    {
        public static void ApplySprite(Image targetImage, Sprite sprite)
        {
            if (targetImage == null) return;

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
            targetImage.color = Color.white;
            targetImage.preserveAspect = false;
            targetImage.raycastTarget = false;

            if (targetImage.transform != null)
            {
                targetImage.transform.SetAsLastSibling();
            }
        }

        public static bool ValidateVisibility(
            UcgCardData card,
            GameObject viewObject,
            Image targetImage,
            Text placeholderText,
            bool isFaceDown,
            out string reason)
        {
            var builder = new StringBuilder();
            bool visible = true;

            if (viewObject == null)
            {
                builder.Append("viewObject=null; ");
                reason = builder.ToString();
                return false;
            }

            if (!viewObject.activeInHierarchy)
            {
                visible = false;
                builder.Append("view inactiveInHierarchy; ");
            }

            if (targetImage == null)
            {
                visible = false;
                builder.Append("targetImage=null; ");
            }
            else
            {
                if (!targetImage.gameObject.activeInHierarchy)
                {
                    visible = false;
                    builder.Append("target inactiveInHierarchy; ");
                }

                if (!targetImage.enabled)
                {
                    visible = false;
                    builder.Append("target image disabled; ");
                }

                if (targetImage.sprite == null)
                {
                    visible = false;
                    builder.Append("target sprite null; ");
                }

                if (targetImage.color.a <= 0.01f)
                {
                    visible = false;
                    builder.Append($"target alpha={targetImage.color.a}; ");
                }

                RectTransform rect = targetImage.rectTransform;
                Vector2 size = rect != null ? rect.rect.size : Vector2.zero;
                if (Mathf.Abs(size.x) <= 0.01f || Mathf.Abs(size.y) <= 0.01f)
                {
                    visible = false;
                    builder.Append($"rectSize={size.x}x{size.y}; ");
                }
            }

            float canvasGroupAlpha = GetEffectiveCanvasGroupAlpha(viewObject);
            if (canvasGroupAlpha <= 0.01f)
            {
                visible = false;
                builder.Append($"canvasGroupAlpha={canvasGroupAlpha}; ");
            }

            if (!isFaceDown && placeholderText != null && placeholderText.enabled)
            {
                builder.Append("placeholder still enabled; ");
            }

            reason = builder.ToString();
            if (!visible)
            {
                Debug.LogWarning(
                    "Card image assigned but not visible:\n" +
                    $"card={FormatCard(card)}\n" +
                    $"view={viewObject.name}\n" +
                    $"target={(targetImage != null ? targetImage.name : "null")}\n" +
                    $"activeInHierarchy={viewObject.activeInHierarchy}\n" +
                    $"imageEnabled={(targetImage != null && targetImage.enabled)}\n" +
                    $"alpha={(targetImage != null ? targetImage.color.a : 0f)}\n" +
                    $"canvasGroupAlpha={canvasGroupAlpha}\n" +
                    $"rectSize={(targetImage != null ? targetImage.rectTransform.rect.size.ToString() : "null")}\n" +
                    $"placeholderActive={(placeholderText != null && placeholderText.enabled)}\n" +
                    $"faceDown={isFaceDown}\n" +
                    $"reason={reason}");
            }

            return visible;
        }

        static string FormatCard(UcgCardData card)
        {
            if (card == null) return "null";
            return $"{card.id} {card.cardName}";
        }

        static float GetEffectiveCanvasGroupAlpha(GameObject viewObject)
        {
            if (viewObject == null) return 0f;

            float alpha = 1f;
            CanvasGroup[] groups = viewObject.GetComponentsInParent<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null) continue;
                alpha *= groups[i].alpha;
            }

            return alpha;
        }
    }
}
