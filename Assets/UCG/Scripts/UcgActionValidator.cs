namespace UCG
{
    public enum UcgPlayActionType
    {
        PlayToEmptyArea,
        Upgrade,
        Reject
    }

    public static class UcgActionValidator
    {
        const string CategoryUltra = "超人力霸王";
        const string CategoryMecha = "機甲";
        const string CategoryMonster = "怪獸";
        const string CategoryAlien = "宇宙人";
        const string TeamTriSquad = "三人突擊隊";

        public static bool CanPlayToEmptyArea(UcgCardData cardData, bool playAreaOccupied, out string message)
        {
            if (playAreaOccupied)
            {
                message = "角色區已有卡牌";
                return false;
            }

            return CanPlayOrUpgrade(cardData, null, out message, out _);
        }

        public static bool CanPlayOrUpgrade(
            UcgCardData incomingCard,
            UcgCardData topCard,
            out string message,
            out UcgPlayActionType actionType)
        {
            actionType = UcgPlayActionType.Reject;

            if (incomingCard == null)
            {
                message = "沒有可登場的卡牌";
                return false;
            }

            if (topCard == null)
            {
                if (UcgTutorialCardEffectMap.ForbidsSingleState(incomingCard, out message))
                {
                    return false;
                }

                message = "登場成功";
                actionType = UcgPlayActionType.PlayToEmptyArea;
                return true;
            }

            if (!CanUseUpgradeLine(incomingCard, topCard))
            {
                message = "這張卡不能升級到目前角色之上";
                return false;
            }

            int nextLevel = GetNextLevel(topCard);
            if (nextLevel < 0)
            {
                message = "目前已是最高測試等級";
                return false;
            }

            bool sameCharacter = IsSameCharacter(incomingCard, topCard);
            bool sameTeamUpgrade = IsSameTeamUpgrade(incomingCard, topCard);
            if (!sameCharacter && !sameTeamUpgrade)
            {
                message = "不同角色不能升級";
                return false;
            }

            if (incomingCard.level == nextLevel)
            {
                message = sameTeamUpgrade && !sameCharacter ? "三人突擊隊升級成功" : "升級成功";
                actionType = UcgPlayActionType.Upgrade;
                return true;
            }

            if (incomingCard.level > nextLevel)
            {
                message = sameTeamUpgrade ? "三人突擊隊也不能跳級" : "不能跳級";
                return false;
            }

            message = "這張卡不能升級到目前角色之上";
            return false;
        }

        static int GetNextLevel(UcgCardData topCard)
        {
            if (topCard == null) return -1;

            if (IsMonsterOrAlien(topCard))
            {
                if (topCard.level >= 7) return -1;
                return topCard.level + 1;
            }

            if (topCard.level >= 3) return -1;
            return topCard.level + 1;
        }

        static bool IsSameCharacter(UcgCardData incomingCard, UcgCardData topCard)
        {
            if (incomingCard == null || topCard == null) return false;
            if (string.IsNullOrWhiteSpace(incomingCard.characterName)) return false;
            if (string.IsNullOrWhiteSpace(topCard.characterName)) return false;

            return incomingCard.characterName == topCard.characterName;
        }

        static bool IsSameTeamUpgrade(UcgCardData incomingCard, UcgCardData topCard)
        {
            if (incomingCard == null || topCard == null) return false;
            return incomingCard.teamTag == TeamTriSquad && topCard.teamTag == TeamTriSquad;
        }

        static bool CanUseUpgradeLine(UcgCardData incomingCard, UcgCardData topCard)
        {
            if (incomingCard == null || topCard == null) return false;
            return IsMonsterOrAlien(incomingCard) == IsMonsterOrAlien(topCard);
        }

        static bool IsMonsterOrAlien(UcgCardData card)
        {
            if (card == null) return false;
            return card.cardCategory == CategoryMonster || card.cardCategory == CategoryAlien;
        }
    }
}
