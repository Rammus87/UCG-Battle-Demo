using System;
using System.Collections.Generic;

namespace UCG
{
    [Serializable]
    public class UcgDeckDefinitionEntry
    {
        public string id;
        public int count;

        public UcgDeckDefinitionEntry(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }

    [Serializable]
    public class UcgDeckDefinition
    {
        public string deckId;
        public string name;
        public List<UcgDeckDefinitionEntry> cards = new List<UcgDeckDefinitionEntry>();

        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] == null) continue;
                    total += Math.Max(0, cards[i].count);
                }

                return total;
            }
        }
    }
}
