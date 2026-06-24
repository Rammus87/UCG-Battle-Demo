using CardFanUI;
using UnityEngine;

namespace UCG
{
    public enum UcgPendingActionType
    {
        None,
        CharacterSetup,
        CharacterUpgrade,
        SceneSetup
    }

    public class UcgPendingAction
    {
        public UcgPendingActionType actionType;
        public UcgCardData cardData;
        public UcgCardView cardView;
        public UIDragCard dragCard;
        public UcgBattleLane targetLane;
        public UcgPlayerSide targetSide;
        public UcgPlayActionType playActionType;
        public Transform previousParent;
        public int previousSiblingIndex;
        public UcgCardData previousSceneCard;
        public UcgPlayerSide previousSceneOwner;
        public string confirmMessage;
        public string successMessage;
    }
}
