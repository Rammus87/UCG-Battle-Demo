using UnityEngine;
using UnityEngine.EventSystems;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgTutorialFinishClickLayer : MonoBehaviour, IPointerClickHandler
    {
        public UcgHandDemo demo;

        public void OnPointerClick(PointerEventData eventData)
        {
            demo?.HandleTutorialFinishScreenClicked();
        }
    }
}
