using System.Collections.Generic;

namespace UCG
{
    public class UcgDeckProfile
    {
        public string deckId;
        public string deckName;
        public string source;
        public List<UcgCardData> cards = new List<UcgCardData>();
        public UcgCardData guaranteedBaseCard;
        public UcgCardData guaranteedUpgradeCard;
        public UcgCardData guaranteedSceneCard;
    }
}
