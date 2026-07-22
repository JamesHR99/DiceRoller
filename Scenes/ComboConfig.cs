using UnityEngine;
using System.Collections.Generic;

// Defines a single combo
[System.Serializable]
public class DiceCombo
{
    public string comboName;
    public List<DiceActionType> requiredSequence; // The sequence of dice actions for this combo
    public float damageMultiplier = 1.0f;        // Multiplier for attack combos
    public int bonusScore = 0;                   // Bonus score for any type of combo
    public DiceActionType bonusActionType = DiceActionType.None; // Optional: A bonus action to perform
    public int bonusActionValue = 0;             // Value for the bonus action
}

[CreateAssetMenu(fileName = "ComboConfig", menuName = "Game/Dice Combo Configuration")]
public class ComboConfig : ScriptableObject
{
    public List<DiceCombo> diceCombos;
}