using UnityEngine;
using UnityEngine.UI;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgTurnManager : MonoBehaviour
    {
        public int currentTurn = 1;
        public int maxLaneCount = 8;
        public Text turnInfoText;

        bool[] _upgradedThisTurn = new bool[8];

        public int ActiveNewLaneIndex => currentTurn <= maxLaneCount ? currentTurn - 1 : -1;

        public bool CanPlaceNewCardInLane(int laneIndex, out string message)
        {
            if (!IsLaneIndexValid(laneIndex))
            {
                message = "此路尚未開放";
                return false;
            }

            int openedLaneCount = Mathf.Min(currentTurn, maxLaneCount);
            if (laneIndex >= openedLaneCount)
            {
                message = "此路尚未開放";
                return false;
            }

            int activeNewLaneIndex = ActiveNewLaneIndex;
            if (activeNewLaneIndex < 0)
            {
                message = "本回合不再開放新的戰鬥區";
                return false;
            }

            if (laneIndex != activeNewLaneIndex)
            {
                message = $"本回合只能設置第 {activeNewLaneIndex + 1} 路";
                return false;
            }

            message = "";
            return true;
        }

        public bool CanUpgradeLaneThisTurn(int laneIndex, out string message)
        {
            if (!IsLaneIndexValid(laneIndex) || laneIndex >= Mathf.Min(currentTurn, maxLaneCount))
            {
                message = "此路尚未開放";
                return false;
            }

            if (_upgradedThisTurn[laneIndex])
            {
                message = "這一路本回合已經升級過了。";
                return false;
            }

            message = "";
            return true;
        }

        public void MarkLaneUpgraded(int laneIndex)
        {
            if (!IsLaneIndexValid(laneIndex)) return;
            _upgradedThisTurn[laneIndex] = true;
        }

        public void NextTurn()
        {
            currentTurn++;
            ResetLaneUpgradeFlags();
            UpdateTurnInfoText();
        }

        public void ResetTurns()
        {
            currentTurn = 1;
            ResetLaneUpgradeFlags();
            UpdateTurnInfoText();
        }

        public string GetTurnInfoText()
        {
            int activeNewLaneIndex = ActiveNewLaneIndex;
            if (activeNewLaneIndex < 0)
            {
                return $"第 {currentTurn} 回合｜不再開放新戰鬥區";
            }

            return $"第 {currentTurn} 回合｜可設置第 {activeNewLaneIndex + 1} 路";
        }

        public void UpdateTurnInfoText()
        {
            if (turnInfoText == null) return;
            turnInfoText.text = GetTurnInfoText();
        }

        void ResetLaneUpgradeFlags()
        {
            EnsureUpgradeFlags();
            for (int i = 0; i < _upgradedThisTurn.Length; i++)
            {
                _upgradedThisTurn[i] = false;
            }
        }

        void EnsureUpgradeFlags()
        {
            if (_upgradedThisTurn != null && _upgradedThisTurn.Length == maxLaneCount) return;
            _upgradedThisTurn = new bool[maxLaneCount];
        }

        bool IsLaneIndexValid(int laneIndex)
        {
            EnsureUpgradeFlags();
            return laneIndex >= 0 && laneIndex < maxLaneCount;
        }
    }
}
