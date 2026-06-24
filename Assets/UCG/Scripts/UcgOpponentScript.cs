using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgOpponentScript : MonoBehaviour
    {
        bool _digaTutorialLogged;

        public UcgCardData GetOpponentSetupCard(UcgTestMode mode, int turnNumber, int laneIndex)
        {
            if (turnNumber <= 0 || laneIndex < 0) return null;
            if (IsDigaTutorialMode(mode))
            {
                LogDigaTutorialLoaded();
                UcgCardData card = GetDigaTutorialSetupCard(turnNumber, laneIndex);
                Debug.Log($"DigaTutorial Opponent Setup: turn={turnNumber} lane={laneIndex + 1} card={(card != null ? card.cardName : "none")}");
                return card;
            }

            switch (mode)
            {
                case UcgTestMode.MonsterAlienTest:
                    return GetMonsterAlienSetupCard(turnNumber, laneIndex);
                case UcgTestMode.TeamTest:
                    return GetTeamSetupCard(turnNumber, laneIndex);
                default:
                    return GetUltramanSetupCard(turnNumber, laneIndex);
            }
        }

        public UcgCardData GetOpponentSceneCard(UcgTestMode mode, int turnNumber)
        {
            if (IsDigaTutorialMode(mode))
            {
                LogDigaTutorialLoaded();
                UcgCardData card = GetDigaTutorialSceneCard(turnNumber);
                Debug.Log($"DigaTutorial Opponent Scene: turn={turnNumber} card={(card != null ? card.cardName : "none")}");
                return card;
            }

            if (turnNumber == 1)
            {
                return CreateSceneCard(
                    "opponent-scene-space-battlefield",
                    "宇宙戰場",
                    1,
                    UcgDemoSceneEffectId.OpponentAllBpPlus500,
                    "持有者所有角色 BP +500");
            }

            if (turnNumber == 2)
            {
                return CreateSceneCard(
                    "opponent-scene-dark-zone",
                    "黑暗領域",
                    2,
                    UcgDemoSceneEffectId.OpponentAllBpPlus500,
                    "持有者所有角色 BP +500");
            }

            return null;
        }

        public bool ShouldOpponentUpgrade(UcgTestMode mode, int turnNumber, int laneIndex)
        {
            if (IsDigaTutorialMode(mode))
            {
                return turnNumber == 2 && laneIndex == 0;
            }

            if (turnNumber == 2) return laneIndex == 0;
            if (turnNumber == 3) return laneIndex == 0 || laneIndex == 1;
            return false;
        }

        public UcgCardData GetOpponentUpgradeCard(UcgTestMode mode, int turnNumber, int laneIndex, UcgCardData currentTopCard)
        {
            if (currentTopCard == null) return null;
            if (!ShouldOpponentUpgrade(mode, turnNumber, laneIndex)) return null;

            if (IsDigaTutorialMode(mode))
            {
                LogDigaTutorialLoaded();
                UcgCardData card = GetDigaTutorialUpgradeCard(turnNumber, laneIndex, currentTopCard);
                Debug.Log($"DigaTutorial Opponent Upgrade: turn={turnNumber} lane={laneIndex + 1} card={(card != null ? card.cardName : "none")}");
                return card;
            }

            int nextLevel = currentTopCard.level + 1;
            switch (mode)
            {
                case UcgTestMode.MonsterAlienTest:
                    if (nextLevel < 6 || nextLevel > 7) return null;
                    return CreateCard(
                        $"opponent-upgrade-{turnNumber}-{laneIndex + 1}",
                        $"{currentTopCard.characterName} Lv.{nextLevel}",
                        currentTopCard.characterName,
                        currentTopCard.cardCategory,
                        nextLevel,
                        currentTopCard.teamTag);
                default:
                    if (nextLevel < 2 || nextLevel > 3) return null;
                    return CreateCard(
                        $"opponent-upgrade-{turnNumber}-{laneIndex + 1}",
                        $"{currentTopCard.characterName} Lv.{nextLevel}",
                        currentTopCard.characterName,
                        currentTopCard.cardCategory,
                        nextLevel,
                        currentTopCard.teamTag);
            }
        }

        bool IsDigaTutorialMode(UcgTestMode mode)
        {
            return mode == UcgTestMode.UltramanTest;
        }

        void LogDigaTutorialLoaded()
        {
            if (_digaTutorialLogged) return;
            _digaTutorialLogged = true;
            Debug.Log("Diga Tutorial Match Script Loaded");
        }

        UcgCardData GetDigaTutorialSetupCard(int turnNumber, int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return CreateDigaOpponentCard(
                        "diga-opponent-training-lv1",
                        "對手訓練生 Lv.1",
                        "訓練生",
                        1,
                        3200,
                        6500,
                        8500,
                        10500,
                        UcgDemoEffectId.None,
                        UcgEffectTiming.None,
                        "");
                case 1:
                    return CreateDigaOpponentCard(
                        "diga-opponent-guard-lv1",
                        "對手警備隊員 Lv.1",
                        "警備隊員",
                        1,
                        5000,
                        7600,
                        9600,
                        11600,
                        UcgDemoEffectId.None,
                        UcgEffectTiming.None,
                        "");
                case 2:
                    return CreateDigaOpponentCard(
                        "diga-opponent-ace-lv1",
                        "對手精銳戰士 Lv.1",
                        "精銳戰士",
                        1,
                        6200,
                        8800,
                        10800,
                        12800,
                        UcgDemoEffectId.OnRevealOpponentBpMinus1000,
                        UcgEffectTiming.OnRevealOrEnter,
                        "藍色：登場時，本回合對手角色 BP -1000");
                default:
                    return CreateDigaOpponentCard(
                        $"diga-opponent-turn-{turnNumber}-lane-{laneIndex + 1}",
                        $"對手教學角色 Lv.1",
                        $"教學對手{laneIndex + 1}",
                        1,
                        5200,
                        7800,
                        9800,
                        11800,
                        UcgDemoEffectId.None,
                        UcgEffectTiming.None,
                        "");
            }
        }

        UcgCardData GetDigaTutorialUpgradeCard(int turnNumber, int laneIndex, UcgCardData currentTopCard)
        {
            if (turnNumber != 2 || laneIndex != 0 || currentTopCard == null) return null;

            return CreateDigaOpponentCard(
                "diga-opponent-training-lv2",
                $"{currentTopCard.characterName} Lv.2",
                currentTopCard.characterName,
                2,
                5200,
                7600,
                9600,
                11600,
                UcgDemoEffectId.None,
                UcgEffectTiming.None,
                "");
        }

        UcgCardData GetDigaTutorialSceneCard(int turnNumber)
        {
            if (turnNumber != 3) return null;

            return CreateSceneCard(
                "diga-opponent-scene-dark-zone",
                "黑暗領域",
                2,
                UcgDemoSceneEffectId.OpponentAllBpPlus500,
                "常駐：持有者所有角色 BP +500");
        }

        UcgCardData CreateDigaOpponentCard(
            string id,
            string cardName,
            string characterName,
            int level,
            int singleBp,
            int doubleBp,
            int tripleBp,
            int quadBp,
            UcgDemoEffectId effectId,
            UcgEffectTiming timing,
            string effectDescription)
        {
            var card = new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = characterName,
                cardCategory = "超人力霸王",
                level = level,
                teamTag = "",
                effectId = effectId,
                effectTiming = timing,
                effectDescription = effectDescription,
                sceneTurnCost = 0,
                sceneEffectId = UcgDemoSceneEffectId.None,
                sceneEffectTiming = UcgEffectTiming.None,
                sceneDescription = ""
            };

            SetBp(card, singleBp, doubleBp, tripleBp, quadBp);
            return card;
        }

        UcgCardData GetUltramanSetupCard(int turnNumber, int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return CreateCard("opponent-zoffy-lv1", "對手佐菲 Lv.1", "佐菲", "超人力霸王", 1, "");
                case 1:
                    return CreateCard("opponent-ultraman-lv1", "對手初代 Lv.1", "初代", "超人力霸王", 1, "");
                case 2:
                    return CreateCard("opponent-seven-lv1", "對手賽文 Lv.1", "賽文", "超人力霸王", 1, "");
                default:
                    return CreateCard($"opponent-ultra-turn-{turnNumber}", $"對手超人 Lv.1", $"對手角色{laneIndex + 1}", "超人力霸王", 1, "");
            }
        }

        UcgCardData GetMonsterAlienSetupCard(int turnNumber, int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return CreateCard("opponent-gomora-lv5", "對手哥莫拉 Lv.5", "哥莫拉", "怪獸", 5, "");
                case 1:
                    return CreateCard("opponent-eleking-lv5", "對手艾雷王 Lv.5", "艾雷王", "怪獸", 5, "");
                case 2:
                    return CreateCard("opponent-baltan-lv5", "對手巴爾坦星人 Lv.5", "巴爾坦星人", "宇宙人", 5, "");
                default:
                    return CreateCard($"opponent-monster-turn-{turnNumber}", $"對手怪獸 Lv.5", $"對手怪獸{laneIndex + 1}", "怪獸", 5, "");
            }
        }

        UcgCardData GetTeamSetupCard(int turnNumber, int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return CreateCard("opponent-team-x-lv1", "對手隊員X Lv.1", "隊員X", "超人力霸王", 1, "三人突擊隊");
                case 1:
                    return CreateCard("opponent-team-y-lv1", "對手隊員Y Lv.1", "隊員Y", "超人力霸王", 1, "三人突擊隊");
                case 2:
                    return CreateCard("opponent-team-z-lv1", "對手隊員Z Lv.1", "隊員Z", "超人力霸王", 1, "三人突擊隊");
                default:
                    return CreateCard($"opponent-team-turn-{turnNumber}", $"對手隊員{laneIndex + 1} Lv.1", $"隊員{laneIndex + 1}", "超人力霸王", 1, "三人突擊隊");
            }
        }

        UcgCardData CreateCard(string id, string cardName, string characterName, string category, int level, string teamTag)
        {
            var card = new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = characterName,
                cardCategory = category,
                level = level,
                teamTag = teamTag,
                effectId = UcgDemoEffectId.None,
                effectTiming = UcgEffectTiming.None,
                effectDescription = "",
                sceneTurnCost = 0,
                sceneEffectId = UcgDemoSceneEffectId.None,
                sceneEffectTiming = UcgEffectTiming.None,
                sceneDescription = ""
            };

            ApplyBp(card);
            ApplyDemoEffect(card);
            return card;
        }

        UcgCardData CreateSceneCard(string id, string cardName, int sceneTurnCost, UcgDemoSceneEffectId sceneEffectId, string description)
        {
            return new UcgCardData
            {
                id = id,
                cardName = cardName,
                characterName = "",
                cardCategory = "場景",
                level = 0,
                teamTag = "",
                sceneTurnCost = sceneTurnCost,
                sceneEffectTiming = GetSceneEffectTiming(sceneEffectId),
                sceneEffectId = sceneEffectId,
                sceneDescription = description,
            };
        }

        UcgEffectTiming GetSceneEffectTiming(UcgDemoSceneEffectId sceneEffectId)
        {
            switch (sceneEffectId)
            {
                case UcgDemoSceneEffectId.PlayerAllBpPlus500:
                case UcgDemoSceneEffectId.PlayerAllBpPlus1000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus2000:
                case UcgDemoSceneEffectId.PlayerAllBpPlus3000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus500:
                case UcgDemoSceneEffectId.OpponentAllBpPlus1000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus2000:
                case UcgDemoSceneEffectId.OpponentAllBpPlus3000:
                case UcgDemoSceneEffectId.ActiveLanePlayerBpPlus1000:
                    return UcgEffectTiming.Continuous;
                case UcgDemoSceneEffectId.ActivatedChooseOwnLaneBpPlus1000:
                    return UcgEffectTiming.Activated;
                case UcgDemoSceneEffectId.OnEnterDrawOne:
                    return UcgEffectTiming.OnRevealOrEnter;
                default:
                    return UcgEffectTiming.None;
            }
        }

        void ApplyDemoEffect(UcgCardData card)
        {
            if (card == null) return;

            card.effectId = UcgDemoEffectId.None;
            card.effectDescription = "";

            if (card.characterName == "佐菲" && card.level == 1)
            {
                card.effectId = UcgDemoEffectId.OnRevealOpponentBpMinus1000;
                card.effectTiming = UcgEffectTiming.OnRevealOrEnter;
                card.effectDescription = "翻開時，本回合此 Lane 對手角色 BP -1000";
            }
        }

        void ApplyBp(UcgCardData card)
        {
            if (card.cardCategory == "怪獸" || card.cardCategory == "宇宙人")
            {
                switch (card.level)
                {
                    case 6:
                        SetBp(card, 7000, 10000, 12000, 14000);
                        break;
                    case 7:
                        SetBp(card, 8000, 11000, 13000, 15000);
                        break;
                    default:
                        SetBp(card, 6000, 9000, 11000, 13000);
                        break;
                }
            }
            else
            {
                switch (card.level)
                {
                    case 2:
                        SetBp(card, 6000, 9000, 11000, 13000);
                        break;
                    case 3:
                        SetBp(card, 7000, 10000, 12000, 14000);
                        break;
                    default:
                        if (card.characterName == "初代")
                        {
                            SetBp(card, 6000, 9000, 11000, 13000);
                        }
                        else if (card.characterName == "賽文")
                        {
                            SetBp(card, 4000, 7000, 9000, 11000);
                        }
                        else
                        {
                            SetBp(card, 5000, 8000, 10000, 12000);
                        }
                        break;
                }
            }
        }

        void SetBp(UcgCardData card, int singleBp, int doubleBp, int tripleBp, int quadBp)
        {
            card.singleBp = singleBp;
            card.doubleBp = doubleBp;
            card.tripleBp = tripleBp;
            card.quadBp = quadBp;
        }
    }
}
