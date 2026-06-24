using System.Collections.Generic;
using UnityEngine;

namespace UCG
{
    public static class UcgBattleJudge
    {
        public static bool debugBpBreakdown;

        public static UcgLaneResultType JudgeLane(UcgBattleLane lane, out string message)
        {
            if (lane == null)
            {
                message = "無結果";
                return UcgLaneResultType.None;
            }

            int playerStackCount = lane.playerPlayArea != null ? lane.playerPlayArea.GetStackCount() : 0;
            int opponentStackCount = lane.GetOpponentStackCount();
            UcgCardView playerTopCard = lane.playerPlayArea != null ? lane.playerPlayArea.GetTopCard() : lane.playerTopCard;

            return JudgeLane(
                playerTopCard,
                playerStackCount,
                lane.GetOpponentTopCard(),
                opponentStackCount,
                lane.playerTemporaryBpModifier + lane.playerSceneBpModifier + lane.playerConditionalBpModifier,
                lane.opponentTemporaryBpModifier + lane.opponentSceneBpModifier + lane.opponentConditionalBpModifier,
                lane.playerTemporaryBpModifiers,
                lane.playerSceneBpModifiers,
                lane.playerConditionalBpModifiers,
                lane.opponentTemporaryBpModifiers,
                lane.opponentSceneBpModifiers,
                lane.opponentConditionalBpModifiers,
                lane.laneIndex,
                out lane.playerBp,
                out lane.opponentBp,
                out message);
        }

        public static UcgLaneResultType JudgeLane(
            UcgCardView playerTopCard,
            int playerStackCount,
            UcgCardView opponentTopCard,
            int opponentStackCount,
            out int playerBp,
            out int opponentBp,
            out string message)
        {
            return JudgeLane(
                playerTopCard,
                playerStackCount,
                opponentTopCard,
                opponentStackCount,
                0,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                -1,
                out playerBp,
                out opponentBp,
                out message);
        }

        public static UcgLaneResultType JudgeLane(
            UcgCardView playerTopCard,
            int playerStackCount,
            UcgCardView opponentTopCard,
            int opponentStackCount,
            int playerBpModifier,
            int opponentBpModifier,
            out int playerBp,
            out int opponentBp,
            out string message)
        {
            return JudgeLane(
                playerTopCard,
                playerStackCount,
                opponentTopCard,
                opponentStackCount,
                playerBpModifier,
                opponentBpModifier,
                null,
                null,
                null,
                null,
                null,
                null,
                -1,
                out playerBp,
                out opponentBp,
                out message);
        }

        static UcgLaneResultType JudgeLane(
            UcgCardView playerTopCard,
            int playerStackCount,
            UcgCardView opponentTopCard,
            int opponentStackCount,
            int playerBpModifier,
            int opponentBpModifier,
            IReadOnlyList<UcgBpModifierInfo> playerTemporaryModifiers,
            IReadOnlyList<UcgBpModifierInfo> playerSceneModifiers,
            IReadOnlyList<UcgBpModifierInfo> playerConditionalModifiers,
            IReadOnlyList<UcgBpModifierInfo> opponentTemporaryModifiers,
            IReadOnlyList<UcgBpModifierInfo> opponentSceneModifiers,
            IReadOnlyList<UcgBpModifierInfo> opponentConditionalModifiers,
            int laneIndex,
            out int playerBp,
            out int opponentBp,
            out string message)
        {
            bool hasPlayer = playerTopCard != null && playerTopCard.CardData != null;
            bool hasOpponent = opponentTopCard != null && opponentTopCard.CardData != null;

            if (hasPlayer && playerTopCard.IsFaceDown)
            {
                Debug.LogWarning("UCG BattleJudgement found a face-down player card. BP will still use UcgCardData for this demo.");
            }

            if (hasOpponent && opponentTopCard.IsFaceDown)
            {
                Debug.LogWarning("UCG BattleJudgement found a face-down opponent card. BP will still use UcgCardData for this demo.");
            }

            UcgBpBreakdown playerBreakdown = CalculateLaneBpBreakdown(
                playerTopCard,
                playerStackCount,
                playerBpModifier,
                playerTemporaryModifiers,
                playerSceneModifiers,
                playerConditionalModifiers);
            UcgBpBreakdown opponentBreakdown = CalculateLaneBpBreakdown(
                opponentTopCard,
                opponentStackCount,
                opponentBpModifier,
                opponentTemporaryModifiers,
                opponentSceneModifiers,
                opponentConditionalModifiers);

            playerBp = playerBreakdown.finalBp;
            opponentBp = opponentBreakdown.finalBp;

            if (!hasPlayer && !hasOpponent)
            {
                message = "我方最終 BP：0\n對手最終 BP：0\n結果：無結果";
                return UcgLaneResultType.None;
            }

            if (hasPlayer && !hasOpponent)
            {
                message = $"{FormatBreakdownLine("我方", playerBreakdown)}\n對手：0\n結果：我方勝";
                DebugLogBreakdown("Player", laneIndex, playerBreakdown);
                return UcgLaneResultType.PlayerWin;
            }

            if (!hasPlayer)
            {
                message = $"我方：0\n{FormatBreakdownLine("對手", opponentBreakdown)}\n結果：對手勝";
                DebugLogBreakdown("Opponent", laneIndex, opponentBreakdown);
                return UcgLaneResultType.OpponentWin;
            }

            DebugLogBreakdown("Player", laneIndex, playerBreakdown);
            DebugLogBreakdown("Opponent", laneIndex, opponentBreakdown);

            if (playerBp > opponentBp)
            {
                message = $"{FormatBreakdownLine("我方", playerBreakdown)}\n{FormatBreakdownLine("對手", opponentBreakdown)}\n結果：我方勝";
                return UcgLaneResultType.PlayerWin;
            }

            if (opponentBp > playerBp)
            {
                message = $"{FormatBreakdownLine("我方", playerBreakdown)}\n{FormatBreakdownLine("對手", opponentBreakdown)}\n結果：對手勝";
                return UcgLaneResultType.OpponentWin;
            }

            message = $"{FormatBreakdownLine("我方", playerBreakdown)}\n{FormatBreakdownLine("對手", opponentBreakdown)}\n結果：平手";
            return UcgLaneResultType.Draw;
        }

        static string FormatBreakdownLine(string label, UcgBpBreakdown breakdown)
        {
            if (breakdown == null) return $"{label}：0";

            int totalModifier = breakdown.TotalModifier;
            if (totalModifier == 0) return $"{label}：{breakdown.baseBp} = {breakdown.finalBp}";

            string sign = totalModifier > 0 ? "+" : "";
            return $"{label}：{breakdown.baseBp} {sign}{totalModifier} = {breakdown.finalBp}";
        }

        public static int CalculateLaneBp(UcgCardView topCard, int stackCount, int modifier)
        {
            if (topCard == null || topCard.CardData == null) return 0;

            int baseBp = topCard.CardData.GetBpByStackCount(stackCount);
            return Mathf.Max(0, baseBp + modifier);
        }

        public static UcgBpBreakdown CalculateLaneBpBreakdown(
            UcgCardView topCard,
            int stackCount,
            int totalModifier,
            IReadOnlyList<UcgBpModifierInfo> temporaryModifiers,
            IReadOnlyList<UcgBpModifierInfo> sceneModifiers,
            IReadOnlyList<UcgBpModifierInfo> conditionalModifiers = null)
        {
            var breakdown = new UcgBpBreakdown();
            if (topCard == null || topCard.CardData == null)
            {
                breakdown.finalBp = 0;
                return breakdown;
            }

            breakdown.baseBp = topCard.CardData.GetBpByStackCount(stackCount);
            AddModifiers(breakdown, temporaryModifiers);
            AddModifiers(breakdown, sceneModifiers);
            AddModifiers(breakdown, conditionalModifiers);

            if (breakdown.modifiers.Count == 0 && totalModifier != 0)
            {
                breakdown.modifiers.Add(new UcgBpModifierInfo
                {
                    amount = totalModifier,
                    reason = "BP修正",
                    duration = UcgEffectDuration.None
                });
            }

            breakdown.finalBp = Mathf.Max(0, breakdown.baseBp + breakdown.TotalModifier);
            return breakdown;
        }

        public static int GetNextBpStep(UcgCardData card, int currentBp)
        {
            if (card == null)
            {
                Debug.LogWarning("BP step-up failed: card is null.");
                return currentBp;
            }

            var values = new List<int>();
            AddBpStep(values, card.singleBp);
            AddBpStep(values, card.doubleBp);
            AddBpStep(values, card.tripleBp);
            AddBpStep(values, card.quadBp);
            values.Sort();

            if (values.Count == 0)
            {
                Debug.LogWarning($"BP step-up failed: no BP steps on {card.id} {card.cardName}.");
                return currentBp;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] > currentBp) return values[i];
            }

            return values[values.Count - 1];
        }

        public static int GetPreviousBpStep(UcgCardData card, int currentBp)
        {
            if (card == null)
            {
                Debug.LogWarning("BP step-down failed: card is null.");
                return currentBp;
            }

            var values = new List<int>();
            AddBpStep(values, card.singleBp);
            AddBpStep(values, card.doubleBp);
            AddBpStep(values, card.tripleBp);
            AddBpStep(values, card.quadBp);
            values.Sort();

            if (values.Count == 0)
            {
                Debug.LogWarning($"BP step-down failed: no BP steps on {card.id} {card.cardName}.");
                return currentBp;
            }

            for (int i = values.Count - 1; i >= 0; i--)
            {
                if (values[i] < currentBp) return values[i];
            }

            return values[0];
        }

        static void AddBpStep(List<int> values, int value)
        {
            if (value <= 0 || values.Contains(value)) return;
            values.Add(value);
        }

        static void AddModifiers(UcgBpBreakdown breakdown, IReadOnlyList<UcgBpModifierInfo> modifiers)
        {
            if (breakdown == null || modifiers == null) return;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == null) continue;
                breakdown.modifiers.Add(modifiers[i]);
            }
        }

        static void DebugLogBreakdown(string sideLabel, int laneIndex, UcgBpBreakdown breakdown)
        {
            if (!debugBpBreakdown) return;
            if (breakdown == null) return;

            var builder = new System.Text.StringBuilder();
            string laneText = laneIndex >= 0 ? $" Lane {laneIndex + 1}" : "";
            builder.AppendLine($"BP Breakdown {sideLabel}{laneText}:");
            builder.AppendLine($"Base BP = {breakdown.baseBp}");
            for (int i = 0; i < breakdown.modifiers.Count; i++)
            {
                UcgBpModifierInfo modifier = breakdown.modifiers[i];
                if (modifier == null) continue;
                string sign = modifier.amount > 0 ? "+" : "";
                string source = string.IsNullOrWhiteSpace(modifier.sourceCardName)
                    ? modifier.sourceCardId
                    : $"{modifier.sourceCardId} {modifier.sourceCardName}";
                string stepText = modifier.isStepUp ? $" step={modifier.stepFromBp}->{modifier.stepToBp}" : "";
                string stackText = modifier.requiredStackCount > 0
                    ? $" requiredStackCount={(modifier.requireExactStackCount ? "==" : ">=")}{modifier.requiredStackCount} currentStackCount={modifier.currentStackCount} applied={modifier.stackRequirementMet}"
                    : "";
                builder.AppendLine($"Modifier: {sign}{modifier.amount} from {source} category={modifier.effectCategory} reason={modifier.reason} trigger={modifier.trigger} condition={modifier.condition} duration={modifier.duration}{stackText}{stepText}");
            }

            builder.AppendLine($"Final BP = {breakdown.finalBp}");
            Debug.Log(builder.ToString());
        }

        public static UcgGameResultType JudgeGameResult(
            IReadOnlyList<UcgBattleLane> lanes,
            out int playerWinCount,
            out int opponentWinCount,
            out string message)
        {
            playerWinCount = 0;
            opponentWinCount = 0;

            if (lanes != null)
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    UcgBattleLane lane = lanes[i];
                    if (lane == null) continue;

                    if (lane.laneResult == UcgLaneResultType.PlayerWin)
                    {
                        playerWinCount++;
                    }
                    else if (lane.laneResult == UcgLaneResultType.OpponentWin)
                    {
                        opponentWinCount++;
                    }
                }
            }

            if (playerWinCount >= 3 && opponentWinCount < 3)
            {
                message = "我方獲勝";
                return UcgGameResultType.PlayerWin;
            }

            if (opponentWinCount >= 3 && playerWinCount < 3)
            {
                message = "對手獲勝";
                return UcgGameResultType.OpponentWin;
            }

            if (playerWinCount >= 3 && opponentWinCount >= 3)
            {
                if (playerWinCount > opponentWinCount)
                {
                    message = "我方獲勝";
                    return UcgGameResultType.PlayerWin;
                }

                if (opponentWinCount > playerWinCount)
                {
                    message = "對手獲勝";
                    return UcgGameResultType.OpponentWin;
                }

                message = "雙方勝利數相同，繼續遊戲";
                return UcgGameResultType.ContinueGame;
            }

            message = "尚未達成 3 處勝利，繼續遊戲";
            return UcgGameResultType.ContinueGame;
        }
    }
}
