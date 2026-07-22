using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDiceDefinition", menuName = "Equipment/Dice Definition")]
public class DiceDefinitionSO : ScriptableObject
{
    public string dieName;
    public List<DiceActionType> faces = new List<DiceActionType>(6);

    [Header("Critical Hit Chance")]
    [Range(0, 1)]
    [Tooltip("The chance (0-1) that a roll from this die will be a critical hit.")]
    public float critChance = 0.1f;
}