using System.Collections;
using CardFanUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgDragLayerCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int draggingSortingOrder = 10000;

        Canvas _canvas;
        Transform _originalParent;
        bool _previousOverrideSorting;
        int _previousSortingOrder;
        UcgCardView _cardView;
        bool _isDragging;
        bool _dragAllowed = true;
        UcgHandDemo _demo;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
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
            if (_dragAllowed && _demo != null && _cardView != null && dragCard != null && cardRect != null)
            {
                _demo.TrySnapDraggedCardToNearestTarget(
                    _cardView,
                    dragCard,
                    cardRect,
                    eventData.position,
                    eventData.pressEventCamera);
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
