using System.Collections;
using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgDragLayerCard : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        enum UcgDragDropResult
        {
            DropSuccess,
            DropFailedInvalidTarget,
            DropCancelled,
            DropInterrupted
        }

        public int draggingSortingOrder = 10000;

        Canvas _canvas;
        Transform _originalParent;
        int _originalSiblingIndex;
        RectTransform _dragRect;
        CanvasGroup _dragCanvasGroup;
        Vector2 _originalAnchorMin;
        Vector2 _originalAnchorMax;
        Vector2 _originalPivot;
        Vector2 _originalSizeDelta;
        Vector2 _originalAnchoredPosition;
        Vector3 _originalLocalPosition;
        Vector3 _originalWorldPosition;
        Quaternion _originalLocalRotation;
        Quaternion _originalWorldRotation;
        Vector3 _originalLocalScale;
        float _originalCanvasAlpha;
        bool _originalCanvasBlocksRaycasts;
        bool _originalCanvasInteractable;
        bool _hasOriginalDragState;
        bool _previousOverrideSorting;
        int _previousSortingOrder;
        UcgCardView _cardView;
        bool _isDragging;
        bool _dragAllowed = true;
        UcgHandDemo _demo;

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            CaptureOriginalDragState();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_hasOriginalDragState)
            {
                CaptureOriginalDragState();
            }
            _cardView = GetComponent<UcgCardView>();
            _demo = FindFirstObjectByType<UcgHandDemo>();
            _dragAllowed = true;
            if (_demo != null && !_demo.CanPlayerDragHandCard(_cardView, out string reason))
            {
                _dragAllowed = false;
                _demo.ReportInteractionRejected(
                    "DragHandCard",
                    reason,
                    _cardView != null ? _cardView.CardData : null,
                    null);
                if (_cardView != null)
                {
                    _cardView.SetSelected(false);
                    _cardView.SetDragging(false);
                }
                return;
            }

            if (_cardView != null)
            {
                _cardView.SetSelected(false);
                _cardView.SetDragging(true);
            }
            if (_demo != null)
            {
                _demo.NotifyCardDragStarted(_cardView);
            }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _previousOverrideSorting = _canvas.overrideSorting;
            _previousSortingOrder = _canvas.sortingOrder;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = draggingSortingOrder;
            EnsureDragLayerIsOnTop();
            transform.SetAsLastSibling();
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var dragCard = GetComponent<UIDragCard>();
            var cardRect = transform as RectTransform;
            UcgDragDropResult dropResult = UcgDragDropResult.DropInterrupted;
            if (_dragAllowed && _demo != null && _cardView != null && dragCard != null && cardRect != null)
            {
                bool snapAccepted = _demo.TrySnapDraggedCardToNearestTarget(
                    _cardView,
                    dragCard,
                    cardRect,
                    eventData.position,
                    eventData.pressEventCamera);
                dropResult = snapAccepted || WasAcceptedByDropTarget()
                    ? UcgDragDropResult.DropSuccess
                    : UcgDragDropResult.DropFailedInvalidTarget;
            }
            else if (!_dragAllowed)
            {
                dropResult = UcgDragDropResult.DropCancelled;
            }

            if (dropResult != UcgDragDropResult.DropSuccess)
            {
                RestoreOriginalDragState(dropResult);
            }

            _isDragging = false;
            StartCoroutine(RestoreCanvasAfterDragEnd());
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragAllowed) return;
            if (_demo == null || _cardView == null) return;
            _demo.NotifyCardDragMoved(_cardView, eventData.position, eventData.pressEventCamera);
        }

        void LateUpdate()
        {
            if (!_dragAllowed || !_isDragging || _canvas == null) return;

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = draggingSortingOrder;
            EnsureDragLayerIsOnTop();
            transform.SetAsLastSibling();
        }

        IEnumerator RestoreCanvasAfterDragEnd()
        {
            yield return null;

            if (_canvas == null) yield break;

            if (transform.parent == _originalParent)
            {
                _canvas.overrideSorting = _previousOverrideSorting;
                _canvas.sortingOrder = _previousSortingOrder;
            }

            if (_cardView != null)
            {
                _cardView.SetDragging(false);
            }

            if (_demo != null)
            {
                _demo.NotifyCardDragEnded();
            }

            _hasOriginalDragState = false;
        }

        void CaptureOriginalDragState()
        {
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _dragRect = transform as RectTransform;
            _dragCanvasGroup = GetComponent<CanvasGroup>();
            _originalLocalPosition = transform.localPosition;
            _originalWorldPosition = transform.position;
            _originalLocalRotation = transform.localRotation;
            _originalWorldRotation = transform.rotation;
            _originalLocalScale = transform.localScale;

            if (_dragRect != null)
            {
                _originalAnchorMin = _dragRect.anchorMin;
                _originalAnchorMax = _dragRect.anchorMax;
                _originalPivot = _dragRect.pivot;
                _originalSizeDelta = _dragRect.sizeDelta;
                _originalAnchoredPosition = _dragRect.anchoredPosition;
            }

            if (_dragCanvasGroup != null)
            {
                _originalCanvasAlpha = _dragCanvasGroup.alpha;
                _originalCanvasBlocksRaycasts = _dragCanvasGroup.blocksRaycasts;
                _originalCanvasInteractable = _dragCanvasGroup.interactable;
            }

            _hasOriginalDragState = true;
        }

        void RestoreOriginalDragState(UcgDragDropResult result)
        {
            if (!_hasOriginalDragState) return;

            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false);
                int clampedSiblingIndex = Mathf.Clamp(_originalSiblingIndex, 0, _originalParent.childCount - 1);
                transform.SetSiblingIndex(clampedSiblingIndex);
            }
            else
            {
                transform.SetParent(null, true);
                transform.position = _originalWorldPosition;
                transform.rotation = _originalWorldRotation;
            }

            if (_dragRect != null)
            {
                _dragRect.anchorMin = _originalAnchorMin;
                _dragRect.anchorMax = _originalAnchorMax;
                _dragRect.pivot = _originalPivot;
                _dragRect.sizeDelta = _originalSizeDelta;
                _dragRect.anchoredPosition = _originalAnchoredPosition;
            }

            transform.localPosition = _originalLocalPosition;
            transform.localRotation = _originalLocalRotation;
            transform.localScale = _originalLocalScale;

            if (_dragCanvasGroup != null)
            {
                _dragCanvasGroup.alpha = 1f;
                _dragCanvasGroup.blocksRaycasts = true;
                _dragCanvasGroup.interactable = true;
            }

            var rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = true;
                rootImage.raycastTarget = true;
            }

            var dragCard = GetComponent<UIDragCard>();
            if (dragCard != null)
            {
                dragCard.enabled = true;
            }

            if (_canvas != null)
            {
                _canvas.overrideSorting = _previousOverrideSorting;
                _canvas.sortingOrder = _previousSortingOrder;
            }

            if (_cardView != null)
            {
                _cardView.SetSelected(false);
                _cardView.SetDragging(false);
            }
        }

        bool WasAcceptedByDropTarget()
        {
            if (!_hasOriginalDragState) return false;
            if (!gameObject.activeSelf) return true;

            Transform currentParent = transform.parent;
            if (currentParent == null) return false;
            if (currentParent == _originalParent) return false;

            var dragCard = GetComponent<UIDragCard>();
            Transform dragLayer = dragCard != null ? dragCard.dragLayerOverride : null;
            if (dragLayer != null && currentParent == dragLayer) return false;
            if (dragCard != null && dragCard.rootCanvas != null && currentParent == dragCard.rootCanvas.transform) return false;

            return true;
        }

        void EnsureDragLayerIsOnTop()
        {
            Transform parent = transform.parent;
            if (parent == null || parent.name != "DragLayer") return;

            parent.SetAsLastSibling();
            var parentCanvas = parent.GetComponent<Canvas>();
            if (parentCanvas == null) return;

            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = Mathf.Max(parentCanvas.sortingOrder, draggingSortingOrder - 1);
        }
    }
}
