using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class UcgCardView : MonoBehaviour, IPointerClickHandler
    {
        public event Action<UcgCardView> OnCardSelected;

        [Header("Data")]
        public UcgCardData cardData;

        [Header("Display")]
        public Image cardImage;
        public Text placeholderText;
        public Color placeholderColor = new Color(0.18f, 0.22f, 0.3f, 1f);
        public Color imageCardColor = Color.white;

        [Header("Selection")]
        public float selectedSizeMultiplier = 1.35f;
        public int selectedSortingOrder = 1200;

        RectTransform _rectTransform;
        Canvas _selectionCanvas;
        Vector2 _baseSize;
        bool _isSelected;
        bool _previousOverrideSorting;
        int _previousSortingOrder;

        public bool IsSelected => _isSelected;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (cardImage == null) cardImage = GetComponent<Image>();
            _baseSize = _rectTransform.sizeDelta;
        }

        void Start()
        {
            Refresh();
        }

        void LateUpdate()
        {
            if (!_isSelected || _selectionCanvas == null) return;

            _selectionCanvas.overrideSorting = true;
            _selectionCanvas.sortingOrder = selectedSortingOrder;
        }

        public void Initialize(UcgCardData data)
        {
            cardData = data;
            Refresh();
        }

        public void Refresh()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (cardImage == null) cardImage = GetComponent<Image>();
            if (_baseSize == Vector2.zero) _baseSize = _rectTransform.sizeDelta;

            Sprite sprite = cardData != null ? cardData.cardImage : null;
            string displayName = cardData != null && !string.IsNullOrWhiteSpace(cardData.cardName)
                ? cardData.cardName
                : "UCG Card";

            cardImage.sprite = sprite;
            cardImage.color = sprite != null ? imageCardColor : placeholderColor;
            cardImage.preserveAspect = sprite != null;

            if (placeholderText != null)
            {
                placeholderText.text = displayName;
                placeholderText.enabled = sprite == null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetSelected(!_isSelected);
            OnCardSelected?.Invoke(this);
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;

            if (selected)
            {
                _selectionCanvas = GetComponent<Canvas>();
                if (_selectionCanvas == null) _selectionCanvas = gameObject.AddComponent<Canvas>();

                _previousOverrideSorting = _selectionCanvas.overrideSorting;
                _previousSortingOrder = _selectionCanvas.sortingOrder;
                _selectionCanvas.overrideSorting = true;
                _selectionCanvas.sortingOrder = selectedSortingOrder;

                if (GetComponent<GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<GraphicRaycaster>();
                }

                _rectTransform.sizeDelta = _baseSize * selectedSizeMultiplier;
            }
            else
            {
                _rectTransform.sizeDelta = _baseSize;

                if (_selectionCanvas != null)
                {
                    _selectionCanvas.overrideSorting = _previousOverrideSorting;
                    _selectionCanvas.sortingOrder = _previousSortingOrder;
                }
            }
        }
    }
}
