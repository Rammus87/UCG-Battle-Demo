using UnityEngine;
using UnityEngine.EventSystems;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgLaneClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public UcgHandDemo demo;
        public UcgBattleLane ownerLane;
        public UcgPlayerSide targetSide;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (demo == null || ownerLane == null) return;
            demo.HandleLaneClickedForEffect(ownerLane, targetSide);
        }
    }
}
