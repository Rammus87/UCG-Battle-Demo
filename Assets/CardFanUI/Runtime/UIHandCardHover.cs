using UnityEngine;
using UnityEngine.EventSystems;

namespace CardFanUI
{

    [AddComponentMenu("CardFanUI/Card Hand Hover")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIHandCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Effect")]
    public float lift = 80f;         // px
    public float rotAdd = 0f;        // deg (additional rotation while hovering)
    public float scale = 1.08f;

    [Header("Smoothing")]
    public float speed = 12f;

    [Header("Behavior on Hover")]
    public bool bringToFrontOnHover = true;
    public bool straightenOnHover = true;
    [Tooltip("Smoothing speed when straightening rotation to 0")] public float straightenSpeed = 12f;

    [Header("Overlay Sorting (Bring to front without sibling change)")]
    public bool useOverlaySorting = true;
    [Tooltip("Sorting order to use during hover (when Canvas.overrideSorting=true)")]
    public int hoverSortingOrder = 1000;

    Vector2 _offset; float _rot; float _scale = 1f;
    Vector2 _tOffset; float _tRot; float _tScale = 1f;

    int _originalSibling = -1;
    float _straightenT;         // 0..1 arası
    float _targetStraightenT;   // hedef 0..1

    // Overlay Canvas/runtime state
    Canvas _canvas;
    UnityEngine.UI.GraphicRaycaster _raycaster;
    bool _hadCanvas;
    bool _prevOverride;
    int _prevOrder;

    public Vector2 CurrentOffset => _offset;
    public float CurrentRotAdd => _rot;
    public float CurrentScale => _scale;
    public float CurrentStraighten => _straightenT;

    void Update()
    {
        _offset = Vector2.Lerp(_offset, _tOffset, Time.deltaTime * speed);
        _rot = Mathf.Lerp(_rot, _tRot, Time.deltaTime * speed);
        _scale = Mathf.Lerp(_scale, _tScale, Time.deltaTime * speed);
        _straightenT = Mathf.Lerp(_straightenT, _targetStraightenT, Time.deltaTime * straightenSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _tOffset = new Vector2(0, lift);
        _tRot = rotAdd;
        _tScale = scale;

        if (straightenOnHover) _targetStraightenT = 1f; else _targetStraightenT = 0f;

        if (useOverlaySorting)
        {
            // Ensure a Canvas on this card so we can raise sorting order without changing siblings
            _canvas = GetComponent<Canvas>();
            _hadCanvas = (_canvas != null);
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

            // Ensure a raycaster so events still work when on its own canvas
            _raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (_raycaster == null) _raycaster = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Cache previous values and raise order
            _prevOverride = _canvas.overrideSorting;
            _prevOrder = _canvas.sortingOrder;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = hoverSortingOrder;
        }
        else if (bringToFrontOnHover)
        {
            if (_originalSibling < 0) _originalSibling = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tOffset = Vector2.zero;
        _tRot = 0f;
        _tScale = 1f;

        _targetStraightenT = 0f;

        if (useOverlaySorting)
        {
            if (_canvas != null)
            {
                _canvas.overrideSorting = _prevOverride;
                _canvas.sortingOrder = _prevOrder;
                // If we created the Canvas just for hover, we can keep it; it's harmless. If you prefer, you could destroy it.
            }
            _raycaster = null;
        }
        else if (bringToFrontOnHover && _originalSibling >= 0 && transform.parent != null)
        {
            int max = transform.parent.childCount - 1;
            transform.SetSiblingIndex(Mathf.Clamp(_originalSibling, 0, max));
            _originalSibling = -1;
        }
    }
}
}