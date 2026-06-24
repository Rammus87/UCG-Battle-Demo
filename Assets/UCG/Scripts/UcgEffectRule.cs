using System.Collections.Generic;

namespace UCG
{
    public enum UcgEffectTrigger
    {
        None,
        OnRevealOrEnter,
        Activated,
        Continuous
    }

    public enum UcgEffectActionType
    {
        None,
        ModifyBp,
        DrawCards,
        DeckOperation,
        GrantTemporaryType,
        SwapOwnCharacters,
        StepDownThenStepUp,
        SwapTopWithDiscard,
        SceneUpgradeFromDeck
    }

    public enum UcgDeckOperationType
    {
        None,
        RevealTopSelectToHandRestTrash,
        DrawThenSelectBottom,
        DrawThenPutHandToBottom,
        SelectHandToBottomThenDrawSameCount
    }

    public enum UcgDeckSelectionFilter
    {
        Any,
        SceneCard,
        UltramanCard
    }

    public enum UcgDeckOperationDestination
    {
        None,
        Hand,
        BottomOfDeck,
        Trash,
        KeepOrder,
        ShuffleBack
    }

    public enum UcgEffectDuration
    {
        None,
        UntilEndOfTurn,
        WhileSceneActive
    }

    public enum UcgEffectConditionSide
    {
        None,
        Self,
        Opponent
    }

    public enum UcgEffectCategory
    {
        None,
        EnterEffect,
        BattleEffect,
        ContinuousEffect
    }

    public class UcgEffectRule
    {
        public bool supported;
        public UcgEffectCategory effectCategory;
        public UcgEffectTrigger trigger;
        public UcgEffectActionType actionType;
        public UcgEffectDuration duration;
        public int bpAmount;
        public bool bpStepUp;
        public int drawCount;
        public string grantedType;
        public UcgDeckOperationRule deckOperation;
        public UcgEffectConditionRule condition;
        public UcgDeckOperationSourceZone selectionSourceZone;
        public UcgDeckOperationSourceZone reorderSourceZone;
        public int reorderTopDeckCount;
        public int requiredStackCount;
        public bool requireExactStackCount;
        public List<int> allowedStackCounts;
        public string rawText;
        public string unsupportedReason;
    }

    public class UcgEffectConditionRule
    {
        public UcgEffectConditionSide side;
        public List<string> requiredTypes;
        public string characterNameContains;
        public string failureMessage;
    }

    public class UcgDeckOperationRule
    {
        public UcgDeckOperationType operationType;
        public int revealCount;
        public int drawCount;
        public int selectCount;
        public int minSelectCount;
        public int handSelectCount;
        public int minHandSelectCount;
        public UcgDeckSelectionFilter selectionFilter;
        public UcgDeckSelectionFilter handSelectionFilter;
        public UcgDeckOperationDestination selectedDestination;
        public UcgDeckOperationDestination restDestination;
        public UcgDeckOperationDestination selectedHandCardDestination;
        public bool sendAllToRestDestinationIfNoValidSelection;
        public bool requiresPlayerSelection;
        public string unsupportedReason;
    }
}
