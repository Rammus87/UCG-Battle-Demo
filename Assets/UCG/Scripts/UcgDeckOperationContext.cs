using System.Collections.Generic;

namespace UCG
{
    public enum UcgDeckOperationSourceZone
    {
        RevealedCards,
        Hand,
        DiscardPile,
        SceneRevealCards,
        TopDeckReorder
    }

    public class UcgRevealResult
    {
        public readonly List<UcgCardData> revealedCards = new List<UcgCardData>();
        public int drawPileBefore;
        public int drawPileAfterReveal;
    }

    public class UcgPendingCardSelection
    {
        public UcgEffectInstance sourceEffect;
        public UcgDeckOperationRule rule;
        public UcgPlayerSide owner;
        public UcgDeckOperationSourceZone sourceZone = UcgDeckOperationSourceZone.RevealedCards;
        public readonly List<UcgCardData> revealedCards = new List<UcgCardData>();
        public int handBefore;
        public int handAfterDraw;
        public int deckBefore;
        public bool resolved;
    }

    public class UcgCardSelectionContext : UcgPendingCardSelection
    {
        public UcgCardData selectedCard;
        public readonly List<UcgCardData> selectedCards = new List<UcgCardData>();
        public readonly List<UcgCardData> drawnCards = new List<UcgCardData>();
    }
}
