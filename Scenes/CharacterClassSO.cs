using UnityEngine;

[CreateAssetMenu(fileName = "New Character Class", menuName = "Game/Character Class")]
public class CharacterClassSO : ScriptableObject
{
    [Header("Class Info")]
    public string className;
    [TextArea(3, 5)]
    public string classDescription;
    public Sprite classIcon;

    [Header("Base Stats")]
    [Tooltip("Base agility for this character class. Higher values act first in combat.")]
    public int baseAgility = 10;

    [Header("Starting Equipment")]
    public EquipmentItemSO startingWeapon;
    public EquipmentItemSO startingArmor;
    public EquipmentItemSO startingItem;
}
