using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardFanUI
{
    [AddComponentMenu("CardFanUI/Drop Handler")]
    [DisallowMultipleComponent]
    public class UIDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Feedback (optional)")]
        public Image highlightImage;
        public Color highlightColor = new Color(1f, 1f, 1f, 0.15f);

        [Header("Behavior")]
        public bool destroyNoteOnDrop = true;

        [Tooltip("Accept drops only from objects with UIDragNote component")] 
        public bool requireUIDragNote = true;

        [Tooltip("(Optional) Accept only drag objects with this tag. Leave empty to ignore tag check")] 
        public string acceptDragTag = "";  

        private Color _defaultColor;

        private int currentPage;            
        private int pageEntryCount;         

        private void Awake()
        {
            if (highlightImage) _defaultColor = highlightImage.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlightImage) highlightImage.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlightImage) highlightImage.color = _defaultColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (highlightImage) highlightImage.color = _defaultColor;
            if (eventData.pointerDrag == null) return;

            var dragged = eventData.pointerDrag.transform;

            var dragNote = dragged.GetComponent<UIDragCard>();
            if (requireUIDragNote && dragNote == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(acceptDragTag) && !dragged.CompareTag(acceptDragTag))
            {
                return;
            }
            
            if (dragNote != null) dragNote.MarkDropped();

            if (destroyNoteOnDrop && dragNote != null)
            {
                Object.Destroy(dragged.gameObject);
            }
        }
    }
}