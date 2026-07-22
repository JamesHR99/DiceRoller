using UnityEngine;
using System.Collections.Generic;

public enum ComboClassRestriction
{
    None,
    Warrior,
    Rogue,
    Wizard,
    Archer
}

[CreateAssetMenu(fileName = "NewDiceCombo", menuName = "Dice/Dice Combo")]
public class DiceComboSO : ScriptableObject
{
    [Header("Combo Definition")]
    public string comboName = "New Combo";
    [Tooltip("The sequence of dice actions required to trigger this combo.")]
    public List<DiceActionType> requiredActions;
    
    [Header("Class Restriction")]
    [Tooltip("Which class can use this combo. Set to None for universal combos.")]
    public ComboClassRestriction classRestriction = ComboClassRestriction.None;

    [Header("Combo Outcome (Transformation)")]
    [Tooltip("The resulting action type of the combo (e.g., Attack, Heal).")]
    public DiceActionType resultActionType = DiceActionType.Attack;

    [Tooltip("The base value/score of the resulting combo action. This completely REPLACES the sum of the original dice values.")]
    public int baseValue = 25;

    [Tooltip("The base speed of the resulting combo action. This completely REPLACES the sum of the original dice speeds.")]
    public int baseSpeed = 10;

    [Header("Status Effect")]
    [Tooltip("Optional status effect applied when this combo is used")]
    public StatusEffectType statusEffectType = StatusEffectType.None;
    [Tooltip("Damage or strength of the status effect")]
    public int statusEffectDamage = 0;
    [Tooltip("Duration of the status effect in turns")]
    public int statusEffectDuration = 0;
    [Tooltip("Multiplier for Weak/Vulnerable effects")]
    public float statusEffectMultiplier = 1f;

    [Header("Priority & AI Behavior")]
    [Tooltip("Higher numbers are checked first. Use this to prioritize longer combos over shorter ones.")]
    public int priority = 0;

    [Tooltip("For enemies: The chance (0.0 to 1.0) that an AI will choose to perform this specific combo if it's available.")]
    [Range(0, 1)]
    public float aiUsageLikelihood = 0.75f;

    [TextArea(3, 5)]
    public string description = "A powerful combination!";
}