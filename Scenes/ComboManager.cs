using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ComboManager : MonoBehaviour
{
    public List<DiceComboSO> availableCombos;

    void Awake()
    {
        availableCombos = availableCombos.OrderByDescending(c => c.priority).ToList();
    }

    public (List<DiceActionDetails> comboActions, List<Dice> remainingDice) FindAndProcessCombos(List<Dice> heldDice)
    {
        var comboActions = new List<DiceActionDetails>();
        var remainingDice = new List<Dice>(heldDice);
        
        string playerClassName = GetPlayerClassName();

        foreach (var combo in availableCombos)
        {
            if (combo.requiredActions == null || combo.requiredActions.Count == 0 || remainingDice.Count < combo.requiredActions.Count) continue;
            
            if (!IsComboAvailableForClass(combo, playerClassName))
            {
                continue;
            }

            while (true)
            {
                var tempRemainingDice = new List<Dice>(remainingDice);
                bool comboFound = true;
                List<Dice> usedDiceForThisCombo = new List<Dice>();

                foreach (var requiredAction in combo.requiredActions)
                {
                    int foundIndex = tempRemainingDice.FindIndex(d => d.GetCurrentAction() == requiredAction);
                    if (foundIndex != -1)
                    {
                        usedDiceForThisCombo.Add(tempRemainingDice[foundIndex]);
                        tempRemainingDice.RemoveAt(foundIndex);
                    }
                    else
                    {
                        comboFound = false;
                        break;
                    }
                }

                if (comboFound)
                {
                    Debug.Log($"<color=cyan>Combo Found: {combo.comboName}!</color>");

                    remainingDice = tempRemainingDice;

                    int comboScore = combo.baseValue;
                    int comboSpeed = combo.baseSpeed;

                    int criticalDiceCount = usedDiceForThisCombo.Count(d => 
                    {
                        var details = d.GetActionDetails();
                        return details.IsCritical;
                    });
                    
                    bool isComboCritical = false;
                    if (criticalDiceCount > 0)
                    {
                        float comboCritChance = (float)criticalDiceCount / usedDiceForThisCombo.Count;
                        isComboCritical = Random.value < comboCritChance;
                        
                        if (isComboCritical)
                        {
                            comboScore = Mathf.RoundToInt(comboScore * 1.25f);
                            Debug.Log($"<color=yellow>Combo Critical! {criticalDiceCount}/{usedDiceForThisCombo.Count} dice were critical. Damage: {combo.baseValue} -> {comboScore}</color>");
                        }
                    }

                    List<Sprite> comboSprites = usedDiceForThisCombo
                        .Select(d => d.diceImage.sprite)
                        .Where(s => s != null)
                        .ToList();

                    int actionAgility = PlayerManager.Instance != null 
                        ? PlayerManager.Instance.CalculateAgilityForAction(combo.resultActionType) 
                        : 0;

                    StatusEffect comboStatusEffect = null;
                    if (combo.statusEffectType != StatusEffectType.None)
                    {
                        comboStatusEffect = new StatusEffect(
                            combo.statusEffectType,
                            combo.statusEffectDamage,
                            combo.statusEffectDuration,
                            combo.statusEffectMultiplier
                        );
                    }

                    var comboAction = new DiceActionDetails(combo.resultActionType, comboScore, comboSpeed, comboSprites, isComboCritical, comboStatusEffect, actionAgility, combo.baseValue);
                    comboActions.Add(comboAction);
                }
                else
                {
                    break;
                }
            }
        }
        return (comboActions, remainingDice);
    }
    
    private string GetPlayerClassName()
    {
        if (PlayerEquipment.Instance != null && PlayerEquipment.Instance.selectedCharacterClass != null)
        {
            return PlayerEquipment.Instance.selectedCharacterClass.className;
        }
        return "";
    }
    
    private bool IsComboAvailableForClass(DiceComboSO combo, string playerClassName)
    {
        if (combo.classRestriction == ComboClassRestriction.None)
        {
            return true;
        }
        
        string restrictionName = combo.classRestriction.ToString();
        return playerClassName.Equals(restrictionName, System.StringComparison.OrdinalIgnoreCase);
    }
}