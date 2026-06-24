using System.Collections.Generic;

namespace UCG
{
    public class UcgBpModifierInfo
    {
        public string sourceCardId;
        public string sourceCardName;
        public string reason;
        public string trigger;
        public string condition;
        public string effectCategory;
        public UcgEffectDuration duration;
        public int amount;
        public int requiredStackCount;
        public int currentStackCount;
        public bool requireExactStackCount;
        public bool stackRequirementMet = true;
        public string skippedReason;
        public bool isStepUp;
        public int stepFromBp;
        public int stepToBp;
    }

    public class UcgBpBreakdown
    {
        public int baseBp;
        public int finalBp;
        public readonly List<UcgBpModifierInfo> modifiers = new List<UcgBpModifierInfo>();

        public int TotalModifier
        {
            get
            {
                int total = 0;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    if (modifiers[i] == null) continue;
                    total += modifiers[i].amount;
                }

                return total;
            }
        }
    }
}
