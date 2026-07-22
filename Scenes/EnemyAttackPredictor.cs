using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EnemyAttackPredictor : MonoBehaviour
{
    public static EnemyAttackPredictor Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI predictionText;
    
    [Header("Simulation Settings")]
    public int numberOfSimulations = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdatePredictions(List<Dice> enemyDice, EnemyManager enemyManager)
    {
        if (predictionText == null || enemyDice == null || enemyDice.Count == 0 || enemyManager == null)
        {
            if (predictionText != null)
            {
                predictionText.text = "";
            }
            return;
        }

        List<TurnOutcome> predictions = SimulateEnemyTurns(enemyDice, enemyManager);

        if (predictions.Count == 0)
        {
            predictionText.text = "<color=#FFAA00>Enemy Likely Actions: Unknown</color>";
            return;
        }

        bool isDefensive = enemyManager.ShouldBeDefensive();
        float healthPercent = enemyManager.GetHealthPercentage();
        
        string predictionDisplay = "<color=#FFAA00><b>Enemy Likely Moves:</b></color>";
        
        if (isDefensive)
        {
            predictionDisplay += $" <color=#FF4444>⚠️ DEFENSIVE ({healthPercent:P0} HP)</color>";
        }
        
        predictionDisplay += "\n";
        
        int count = Mathf.Min(3, predictions.Count);
        for (int i = 0; i < count; i++)
        {
            TurnOutcome outcome = predictions[i];
            predictionDisplay += $"{i + 1}. {outcome.GetDisplayText()} ";
            predictionDisplay += $"<color=#AAAAAA>({outcome.probability:F0}%)</color>\n";
        }

        predictionText.text = predictionDisplay;
    }

    private List<TurnOutcome> SimulateEnemyTurns(List<Dice> enemyDice, EnemyManager enemyManager)
    {
        Dictionary<string, TurnOutcome> outcomes = new Dictionary<string, TurnOutcome>();

        for (int i = 0; i < numberOfSimulations; i++)
        {
            TurnOutcome outcome = SimulateSingleTurn(enemyDice, enemyManager);
            string key = outcome.GetKey();

            if (outcomes.ContainsKey(key))
            {
                outcomes[key].occurrences++;
            }
            else
            {
                outcome.occurrences = 1;
                outcomes[key] = outcome;
            }
        }

        foreach (var outcome in outcomes.Values)
        {
            outcome.probability = (outcome.occurrences / (float)numberOfSimulations) * 100f;
        }

        return outcomes.Values.OrderByDescending(o => o.probability).ToList();
    }

    private TurnOutcome SimulateSingleTurn(List<Dice> enemyDice, EnemyManager enemyManager)
    {
        TurnOutcome outcome = new TurnOutcome();
        
        List<Dice> availableDice = new List<Dice>(enemyDice);
        
        int energyBudget = Random.Range(enemyManager.minEnergyToSpend, enemyManager.maxEnergyToSpend + 1);
        energyBudget = Mathf.Min(energyBudget, enemyManager.GetCurrentEnergy());

        if (enemyManager.availableCombos != null)
        {
            var sortedCombos = enemyManager.availableCombos.OrderByDescending(c => c.priority).ToList();

            foreach (var combo in sortedCombos)
            {
                if (energyBudget <= 0) break;
                if (combo.requiredActions == null || combo.requiredActions.Count == 0) continue;
                if (combo.requiredActions.Count > energyBudget) continue;

                bool canFormCombo = true;
                List<Dice> diceForCombo = new List<Dice>();

                foreach (var requiredAction in combo.requiredActions)
                {
                    Dice matchingDie = availableDice.FirstOrDefault(d => d != null && d.GetCurrentAction() == requiredAction);
                    if (matchingDie == null)
                    {
                        canFormCombo = false;
                        break;
                    }
                    diceForCombo.Add(matchingDie);
                }

                if (canFormCombo && Random.value < combo.aiUsageLikelihood)
                {
                    foreach (var die in diceForCombo)
                    {
                        availableDice.Remove(die);
                    }

                    energyBudget -= diceForCombo.Count;
                    outcome.AddCombo(combo);
                }
            }
        }

        bool isDefensiveMode = enemyManager.ShouldBeDefensive();

        while (energyBudget > 0 && availableDice.Count > 0)
        {
            Dice dieToPick = null;

            if (isDefensiveMode)
            {
                dieToPick =
                    FindBestDieOfType(availableDice, DiceActionType.Heal) ??
                    FindBestDieOfType(availableDice, DiceActionType.Defend) ??
                    FindBestDieOfType(availableDice, DiceActionType.Block);
            }

            if (dieToPick == null)
            {
                dieToPick =
                    FindBestDieOfType(availableDice, DiceActionType.Attack) ??
                    FindBestDieOfType(availableDice, DiceActionType.HeavyAttack) ??
                    FindBestDieOfType(availableDice, DiceActionType.SwiftStrike) ??
                    FindBestDieOfType(availableDice, DiceActionType.LightAttack);
            }

            if (dieToPick == null) break;

            outcome.AddIndividualAction(dieToPick.GetCurrentAction());
            availableDice.Remove(dieToPick);
            energyBudget--;
        }

        return outcome;
    }

    private Dice FindBestDieOfType(List<Dice> dicePool, DiceActionType type)
    {
        var matchingDice = dicePool.Where(d => d != null && d.GetCurrentAction() == type).ToList();
        if (matchingDice.Count == 0) return null;
        return matchingDice[Random.Range(0, matchingDice.Count)];
    }

    public void ClearPredictions()
    {
        if (predictionText != null)
        {
            predictionText.text = "";
        }
    }

    private class TurnOutcome
    {
        public List<ComboAction> combos = new List<ComboAction>();
        public Dictionary<DiceActionType, int> individualActions = new Dictionary<DiceActionType, int>();
        public int occurrences = 0;
        public float probability = 0f;

        public void AddCombo(DiceComboSO combo)
        {
            combos.Add(new ComboAction
            {
                name = combo.comboName,
                resultType = combo.resultActionType,
                value = combo.baseValue
            });
        }

        public void AddIndividualAction(DiceActionType action)
        {
            if (!individualActions.ContainsKey(action))
            {
                individualActions[action] = 0;
            }
            individualActions[action]++;
        }

        public string GetKey()
        {
            string key = "";
            
            foreach (var combo in combos.OrderBy(c => c.name))
            {
                key += $"COMBO:{combo.name};";
            }
            
            foreach (var kvp in individualActions.OrderBy(x => x.Key))
            {
                key += $"{kvp.Key}:{kvp.Value};";
            }
            
            return key;
        }

        public string GetDisplayText()
        {
            List<string> parts = new List<string>();

            foreach (var combo in combos)
            {
                string icon = GetIconForType(combo.resultType);
                string color = GetColorForType(combo.resultType);
                parts.Add($"<color={color}>{icon} {combo.name}</color>");
            }

            foreach (var kvp in individualActions.OrderByDescending(x => GetPriority(x.Key)))
            {
                string icon = GetIconForType(kvp.Key);
                string color = GetColorForType(kvp.Key);
                string name = GetActionName(kvp.Key);
                parts.Add($"<color={color}>{icon} {kvp.Value}x{name}</color>");
            }

            return string.Join(", ", parts);
        }

        private int GetPriority(DiceActionType type)
        {
            switch (type)
            {
                case DiceActionType.Attack:
                case DiceActionType.HeavyAttack:
                case DiceActionType.SwiftStrike:
                case DiceActionType.LightAttack:
                    return 10;
                case DiceActionType.Heal:
                    return 5;
                case DiceActionType.Defend:
                case DiceActionType.Block:
                    return 3;
                default:
                    return 0;
            }
        }

        private string GetIconForType(DiceActionType type)
        {
            switch (type)
            {
                case DiceActionType.Attack:
                case DiceActionType.HeavyAttack:
                case DiceActionType.SwiftStrike:
                case DiceActionType.LightAttack:
                    return "⚔️";
                case DiceActionType.Defend:
                case DiceActionType.Block:
                    return "🛡️";
                case DiceActionType.Heal:
                    return "❤️";
                default:
                    return "•";
            }
        }

        private string GetColorForType(DiceActionType type)
        {
            switch (type)
            {
                case DiceActionType.Attack:
                case DiceActionType.HeavyAttack:
                case DiceActionType.SwiftStrike:
                case DiceActionType.LightAttack:
                    return "#FF4444";
                case DiceActionType.Defend:
                case DiceActionType.Block:
                    return "#4444FF";
                case DiceActionType.Heal:
                    return "#44FF44";
                default:
                    return "#FFFFFF";
            }
        }

        private string GetActionName(DiceActionType type)
        {
            switch (type)
            {
                case DiceActionType.HeavyAttack:
                    return "Heavy";
                case DiceActionType.LightAttack:
                    return "Light";
                case DiceActionType.SwiftStrike:
                    return "Swift";
                case DiceActionType.Defend:
                    return "Shield";
                case DiceActionType.Block:
                    return "Block";
                default:
                    return type.ToString();
            }
        }
    }

    private class ComboAction
    {
        public string name;
        public DiceActionType resultType;
        public int value;
    }
}
