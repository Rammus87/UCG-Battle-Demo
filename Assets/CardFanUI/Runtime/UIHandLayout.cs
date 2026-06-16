using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardFanUI
{
    [AddComponentMenu("CardFanUI/Card Hand Layout")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIHandLayout : MonoBehaviour
    {
        [Header("Fan Layout")]
        [Tooltip("Radius from arc center to children (px)")]
        public float radius = 500f;

        [Tooltip("Total arc angle (deg). 0 = straight line, 90 = quarter, 180 = half")]
        [Range(0f, 180f)] public float totalAngle = 80f;

        [Tooltip("Rotate cards with the arc (bank)")]
        public bool rotateWithArc = true;

        [Tooltip("Invert card rotation (bend outward)")]
        public bool invertRotation = true;

        [Tooltip("Open the fan downward (invert) or upward")]
        public bool invertY = false;

        [Header("Ordering / Spacing")]
        [Tooltip("Child ordering mode")]
        public bool useSiblingOrder = true;

        [Tooltip("Extra angle per card (deg). 0 = equal share")]
        public float perItemExtraAngle = 0f;

        [Header("Adaptive Fan")]
        [Tooltip("Gradually widen the fan as card count increases")] 
        public bool adaptiveSpread = true;

        [Tooltip("Approx. number of cards to reach the full fan angle (min 2)")]
        public int cardsForFullSpread = 5;

        [Tooltip("Minimum fan angle (deg) when few cards")]
        [Range(0f, 180f)] public float minAngle = 0f;

        [Header("Positioning")]
        [Tooltip("Align the fan to the panel's bottom baseline")] 
        public bool useBottomBaseline = true;

        [Tooltip("Padding in px upward from the bottom baseline")] 
        public float baselinePadding = 200f;

        [Header("Animation")]
        public bool smooth = true;
        public float smoothSpeed = 12f;

        RectTransform _rt;
        readonly List<RectTransform> _items = new List<RectTransform>();

        void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            RebuildList();
            ApplyLayout(true);
        }

        void OnTransformChildrenChanged()
        {
            RebuildList();
            ApplyLayout(false);
        }

        void Update()
        {
            ApplyLayout(false);
        }

        public void RebuildList()
        {
            _items.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null) continue;

                // Skip arranging if item is currently being dragged
                var drag = child.GetComponent<UIDragCard>();
                if (drag != null && IsDragging(drag)) continue;

                // Only include active and raycast-enabled children; optionally add more filters
                _items.Add(child);
            }

            if (!useSiblingOrder)
            {
                // Custom sorting can be added here (by name/tag etc.)
            }
        }

        bool IsDragging(UIDragCard drag)
        {
            // Use CanvasGroup.blocksRaycasts=false as a signal for dragging
            var cg = drag.GetComponent<CanvasGroup>();
            return cg != null && cg.blocksRaycasts == false;
        }

        void ApplyLayout(bool instant)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_items.Count == 0) return;

            int n = _items.Count;

            // Toplam açı hesapla (ek açı ekle)
            float baseAngle = totalAngle + perItemExtraAngle * Mathf.Max(0, n - 1);
            float spreadAngle;
            if (adaptiveSpread)
            {
                if (n <= 1)
                {
                    spreadAngle = 0f; // tek kart: düz ve merkezde
                }
                else
                {
                    // (n-1) kart aralığı -> hedef karta yaklaşma
                    int denom = Mathf.Max(1, cardsForFullSpread - 1);
                    float t = Mathf.Clamp01((n - 1) / (float)denom);
                    spreadAngle = Mathf.Lerp(minAngle, baseAngle, t);
                }
            }
            else
            {
                spreadAngle = baseAngle;
            }

            float startAngle = -spreadAngle * 0.5f;   // merkezden simetrik
            float step = (n > 1) ? spreadAngle / (n - 1) : 0f;

            // Arc center (local)
            Vector2 center;
            if (useBottomBaseline)
            {
                // Bottom edge in local space (respecting pivot), then apply padding
                float bottomY = -_rt.rect.height * _rt.pivot.y + baselinePadding;
                float radiusSign = invertY ? -1f : 1f;
                center = new Vector2(0f, bottomY - radiusSign * radius);
            }
            else
            {
                // Centered at (0,0) when baseline is not used
                center = Vector2.zero;
            }

            for (int i = 0; i < n; i++)
            {
                float ang = startAngle + step * i; // degrees
                float rad = ang * Mathf.Deg2Rad;
                float yComp = invertY ? -Mathf.Cos(rad) : Mathf.Cos(rad);
                Vector2 targetPos = center + new Vector2(Mathf.Sin(rad), yComp) * radius;

                float targetRot = 0f;
                if (rotateWithArc)
                {
                    targetRot = invertRotation ? -ang : ang;
                }

                var item = _items[i];

                // Hover lift varsa ona ekleyelim
                var hover = item.GetComponent<UIHandCardHover>();
                Vector2 hoverOffset = Vector2.zero;
                float hoverRotAdd = 0f;
                float hoverScale = 1f;
                if (hover != null)
                {
                    hoverOffset = hover.CurrentOffset;
                    hoverRotAdd = hover.CurrentRotAdd;
                    hoverScale = hover.CurrentScale;
                }

                float straighten = (hover != null) ? hover.CurrentStraighten : 0f;

                Vector3 finalPos = new Vector3(targetPos.x + hoverOffset.x, targetPos.y + hoverOffset.y, 0f);
                float finalRot = Mathf.Lerp(targetRot + hoverRotAdd, 0f, straighten);

                if (smooth && Application.isPlaying && !instant)
                {
                    item.anchoredPosition = Vector2.Lerp(item.anchoredPosition, finalPos, Time.deltaTime * smoothSpeed);
                    var z = Mathf.LerpAngle(item.localEulerAngles.z, finalRot, Time.deltaTime * smoothSpeed);
                    item.localEulerAngles = new Vector3(0, 0, z);
                    item.localScale = Vector3.Lerp(item.localScale, Vector3.one * hoverScale, Time.deltaTime * smoothSpeed);
                }
                else
                {
                    item.anchoredPosition = finalPos;
                    item.localEulerAngles = new Vector3(0, 0, finalRot);
                    item.localScale = Vector3.one * hoverScale;
                }
            }
        }

        // Can be called externally when drag begins/ends
        public void NotifyLayoutChanged(bool instant = false)
        {
            RebuildList();
            ApplyLayout(instant);
        }
    }
}