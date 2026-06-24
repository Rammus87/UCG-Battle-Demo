namespace UCG
{
    public class UcgEffectInstance
    {
        public UcgDemoEffectId effectId;
        public UcgCardData cardData;
        public UcgCardView sourceCard;
        public UcgSceneCardView sourceSceneCard;
        public UcgBattleLane lane;
        public UcgPlayerSide ownerSide;
        public UcgEffectTiming timing;
        public bool isSceneEffect;
        public string effectKey;

        public int LaneIndex => lane != null ? lane.laneIndex : -1;
    }
}
