using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgTurnOrderManager : MonoBehaviour
    {
        public UcgPlayerSide firstPlayer = UcgPlayerSide.Player;
        public UcgPlayerSide currentFirstPlayer = UcgPlayerSide.Player;
        public UcgPlayerSide nextFirstPlayer = UcgPlayerSide.Player;
        public UcgPlayerSide currentActingPlayer = UcgPlayerSide.Player;
        public bool isPlayerFirst = true;
        public bool isOpponentFirst;
        public bool openingCoinIsHeads = true;

        public void ResetTurnOrder()
        {
            DecideOpeningFirstPlayer();
        }

        public UcgPlayerSide DecideOpeningFirstPlayer()
        {
            openingCoinIsHeads = Random.value >= 0.5f;
            firstPlayer = openingCoinIsHeads ? UcgPlayerSide.Player : UcgPlayerSide.Opponent;
            currentFirstPlayer = firstPlayer;
            nextFirstPlayer = firstPlayer;
            currentActingPlayer = firstPlayer;
            isPlayerFirst = firstPlayer == UcgPlayerSide.Player;
            isOpponentFirst = firstPlayer == UcgPlayerSide.Opponent;
            Debug.Log($"Opening first player decided: coin={(openingCoinIsHeads ? "Heads" : "Tails")}, firstPlayer={firstPlayer}");
            return firstPlayer;
        }

        public UcgPlayerSide GetCurrentFirstPlayer()
        {
            return currentFirstPlayer;
        }

        public void SetNextFirstPlayer(UcgPlayerSide side)
        {
            nextFirstPlayer = side;
        }

        public void SetCurrentActingPlayer(UcgPlayerSide side)
        {
            currentActingPlayer = side;
        }

        public void ApplyNextFirstPlayer()
        {
            currentFirstPlayer = nextFirstPlayer;
            currentActingPlayer = currentFirstPlayer;
        }

        public UcgPlayerSide DecideNextFirstPlayerFromLatestLane(UcgBattleLane latestLane, UcgPlayerSide currentFirstSide)
        {
            if (latestLane == null)
            {
                nextFirstPlayer = currentFirstSide;
                return nextFirstPlayer;
            }

            switch (latestLane.laneResult)
            {
                case UcgLaneResultType.PlayerWin:
                    nextFirstPlayer = UcgPlayerSide.Player;
                    break;
                case UcgLaneResultType.OpponentWin:
                    nextFirstPlayer = UcgPlayerSide.Opponent;
                    break;
                default:
                    nextFirstPlayer = currentFirstSide;
                    break;
            }

            return nextFirstPlayer;
        }

        public string GetSideDisplayName(UcgPlayerSide side)
        {
            return side == UcgPlayerSide.Player ? "我方" : "對手";
        }

        public string GetCurrentFirstPlayerText()
        {
            return $"本回合先攻：{GetSideDisplayName(currentFirstPlayer)}";
        }

        public string GetOpeningFirstPlayerText()
        {
            string coinText = openingCoinIsHeads ? "正面" : "反面";
            return $"{coinText}，{GetSideDisplayName(firstPlayer)}先攻！";
        }

        public string GetNextFirstPlayerText()
        {
            return $"下一回合先攻：{GetSideDisplayName(nextFirstPlayer)}";
        }
    }
}
