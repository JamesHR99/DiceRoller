using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EnemyAttackPredictorNew : MonoBehaviour
{
    public static EnemyAttackPredictorNew Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI predictionText;

    private List<DiceActionDetails> plannedActions = new List<DiceActionDetails>();
    private List<Dice> plannedDiceToUse = new List<Dice>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdatePredictions(List<Dice> enemyDice, EnemyManager enemyManager)
    {
        if (predictionText == null || enemyDice == null || enemyDice.Count == 0)
        {
            ClearPredictions();
            return;
        }

        plannedActions.Clear();
        plannedDiceToUse.Clear();

        CalculateEnemyActions(enemyDice, enemyManager);
        DisplayPlannedActions();
    }

    private void CalculateEnemyActions(List<Dice> enemyDice, EnemyManager enemyManager)
    {
        List<Dice> availableDice = new List<Dice>(enemyDice);
        
        int energyBudget = Random.Range(enemyManager.minEnergyToSpend, enemyManager.maxEnergyToSpend + 1);
        energyBudget = Mathf.Min(energyBudget, enemyManager.GetCurrentEnergy());

        if (enemyManager.availableCombos != null && enemyManager.availableCombos.Any())
        {
            var bestComboInfo = FindBestAvailableCombo(availableDice, enemyManager.availableCombos);

            if (bestComboInfo.HasValue)
            {
                var (combo, usedDiceForCombo) = bestComboInfo.Value;

                if (usedDiceForCombo.Count <= energyBudget && Random.value < combo.aiUsageLikelihood)
                {
                    var sprites = usedDiceForCombo.Select(d => d.diceImage.sprite).ToList();
                    
                    int actionAgility = enemyManager.CalculateAgilityForAction(combo.resultActionType);

                    plannedActions.Add(new DiceActionDetails(
                        combo.resultActionType,
                        combo.baseValue,
                        combo.baseSpeed,
                        sprites,
                        false,
                        null,
                        actionAgility
                    ));

                    plannedDiceToUse.AddRange(usedDiceForCombo);
                    availableDice = availableDice.Except(usedDiceForCombo).ToList();
                    energyBudget -= usedDiceForCombo.Count;
                }
            }
        }

        bool isDefensiveMode = enemyManager.ShouldBeDefensive();

        while (energyBudget > 0 && availableDice.Count > 0)
        {
            Dice dieToPick = null;

            if (isDefensiveMode)
            {
                dieToPick = FindBestDieOfType(availableDice, DiceActionType.Heal) ??
                            FindBestDieOfType(availableDice, DiceActionType.Defend) ??
                            FindBestDieOfType(availableDice, DiceActionType.Block) ??
                            availableDice.OrderByDescending(d => d.GetActionDetails().Score).FirstOrDefault();
            }
            else
            {
                dieToPick = FindBestDieOfType(availableDice, DiceActionType.HeavyAttack) ??
                            FindBestDieOfType(availableDice, DiceActionType.Attack) ??
                            FindBestDieOfType(availableDice, DiceActionType.SwiftStrike) ??
                            FindBestDieOfType(availableDice, DiceActionType.LightAttack) ??
                            availableDice.OrderByDescending(d => d.GetActionDetails().Score).FirstOrDefault();
            }

            if (dieToPick == null) break;

            DiceActionDetails rawDetails = dieToPick.GetActionDetails();
            int actionAgility = enemyManager.CalculateAgilityForAction(rawDetails.Type);
            DiceActionDetails detailsWithAgility = new DiceActionDetails(
                rawDetails.Type,
                rawDetails.Score,
                rawDetails.Speed,
                rawDetails.ContributingDiceSprites,
                rawDetails.IsCritical,
                rawDetails.AppliedStatusEffect,
                actionAgility,
                rawDetails.BaseScore,
                rawDetails.CritBonusValue
            );

            plannedActions.Add(detailsWithAgility);
            plannedDiceToUse.Add(dieToPick);
            availableDice.Remove(dieToPick);
            energyBudget--;
        }
    }

    private (DiceComboSO combo, List<Dice> usedDice)? FindBestAvailableCombo(
        List<Dice> availableDice,
        List<DiceComboSO> enemyCombos)
    {
        var sortedCombos = enemyCombos.OrderByDescending(c => c.priority).ToList();

        foreach (var combo in sortedCombos)
        {
            if (combo.requiredActions == null || combo.requiredActions.Count > availableDice.Count)
                continue;

            var dicePoolCopy = new List<Dice>(availableDice);
            var usedDiceForCombo = new List<Dice>();
            bool comboPossible = true;

            foreach (var requiredAction in combo.requiredActions)
            {
                int dieIndex = dicePoolCopy.FindIndex(d => d.GetCurrentAction() == requiredAction);
                if (dieIndex != -1)
                {
                    usedDiceForCombo.Add(dicePoolCopy[dieIndex]);
                    dicePoolCopy.RemoveAt(dieIndex);
                }
                else
                {
                    comboPossible = false;
                    break;
                }
            }

            if (comboPossible)
            {
                return (combo, usedDiceForCombo);
            }
        }

        return null;
    }

    private Dice FindBestDieOfType(List<Dice> dicePool, DiceActionType type)
    {
        return dicePool
            .Where(d => d.GetCurrentAction() == type)
            .OrderByDescending(d => d.GetActionDetails().Score)
            .FirstOrDefault();
    }

    private void DisplayPlannedActions()
    {
        if (plannedActions.Count == 0)
        {
            predictionText.text = "<color=#FF4444>Enemy will take no actions this turn.</color>";
            return;
        }

        string displayText = "<color=#FFAA00><b>Enemy will:</b></color>\n";

        foreach (var action in plannedActions)
        {
            string actionColor = GetActionColor(action.Type);
            string actionText = GetActionText(action);
            displayText += $"<color={actionColor}>• {actionText}</color>\n";
        }

        predictionText.text = displayText;
    }

    private string GetActionColor(DiceActionType actionType)
    {
        switch (actionType)
        {
            case DiceActionType.Attack:
            case DiceActionType.HeavyAttack:
            case DiceActionType.LightAttack:
            case DiceActionType.SwiftStrike:
                return "#FF4444";
            case DiceActionType.Heal:
                return "#44FF44";
            case DiceActionType.Block:
            case DiceActionType.Defend:
                return "#4444FF";
            default:
                return "#FFFFFF";
        }
    }

    private string GetActionText(DiceActionDetails action)
    {
        switch (action.Type)
        {
            case DiceActionType.Attack:
            case DiceActionType.HeavyAttack:
            case DiceActionType.LightAttack:
            case DiceActionType.SwiftStrike:
                return $"{action.Type} ({action.Score} damage)";
            case DiceActionType.Heal:
                return $"Heal ({action.Score} HP)";
            case DiceActionType.Block:
                return "Block (15%)";
            case DiceActionType.Defend:
                return action.Score == 100 ? "Full Block (100%)" : $"Shield ({action.Score})";
            default:
                return action.Type.ToString();
        }
    }

    public List<DiceActionDetails> GetPlannedActions()
    {
        return new List<DiceActionDetails>(plannedActions);
    }

    public List<Dice> GetPlannedDice()
    {
        return new List<Dice>(plannedDiceToUse);
    }

    public void ClearPredictions()
    {
        if (predictionText != null)
        {
            predictionText.text = "";
        }
        plannedActions.Clear();
        plannedDiceToUse.Clear();
    }
}
