using System;
using UnityEngine;

namespace UCG
{
    [Serializable]
    public class UcgCardData
    {
        public string id;
        public string sku;
        public string cardName;
        public string characterName;
        public string cardCategory;
        public int level;
        public string type;
        public string seriesText;
        public string imageLocal;
        public string imageUrl;
        public string teamTag;
        public Sprite cardImage;
        public int singleBp;
        public int doubleBp;
        public int tripleBp;
        public int quadBp;
        public UcgEffectTiming effectTiming;
        public UcgDemoEffectId effectId;
        public string effectDescription;
        public int sceneTurnCost;
        public UcgEffectTiming sceneEffectTiming;
        public UcgDemoSceneEffectId sceneEffectId;
        public string sceneDescription;

        public int GetBpByStackCount(int stackCount)
        {
            if (stackCount <= 1) return singleBp;
            if (stackCount == 2) return doubleBp;
            if (stackCount == 3) return tripleBp;
            return quadBp;
        }

        public bool IsSceneCard()
        {
            return cardCategory == "場景";
        }

        public bool IsExternalCard()
        {
            return !string.IsNullOrWhiteSpace(imageLocal);
        }
    }
}
