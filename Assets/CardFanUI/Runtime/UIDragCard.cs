using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardFanUI
{

    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("CardFanUI/Drag Card")]
    [DisallowMultipleComponent]
    public class UIDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("References (auto if null)")]
    public Canvas rootCanvas; // Usually the parent canvas
    public Transform dragLayerOverride; // Temporary parent during drag (e.g. a separate layer)

    private RectTransform _rt;
    private CanvasGroup _cg;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private bool _droppedSomewhere;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _droppedSomewhere = false;
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();

        // Allow drop zones beneath this card to receive raycasts while dragging:
        _cg.blocksRaycasts = false;
        _cg.alpha = 0.8f;

        // Temporarily reparent under a drag layer or root canvas for clean z-sorting
        if (dragLayerOverride != null)
            transform.SetParent(dragLayerOverride, true);
        else
            transform.SetParent(rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Works correctly with both Screen Space - Overlay and Camera, scaled by canvas factor
        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;

        // If a dropzone accepted it, parent was already changed in OnDrop.
        // Otherwise, return to original parent and sibling index.
        if (!_droppedSomewhere)
        {
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }
    }

    /// <summary>
    /// Called when dropped successfully onto a drop zone (e.g. by UIDropToBook).
    /// </summary>
    public void MarkDropped()
    {
        _droppedSomewhere = true;
    }

}
}