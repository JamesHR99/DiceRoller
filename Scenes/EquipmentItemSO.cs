using UnityEngine;
using System.Collections.Generic;

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Item
}

public enum ItemType
{
    Equipment,
    Special
}

public enum SpecialItemType
{
    None,
    Armoury
}

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Equipment/Equipment Item")]
public class EquipmentItemSO : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    [TextArea]
    public string description;
    public Sprite itemIcon;
    public EquipmentSlot equipmentSlot;

    [Header("Item Type")]
    public ItemType itemType = ItemType.Equipment;
    public SpecialItemType specialItemType = SpecialItemType.None;

    [Header("Stats")]
    [Tooltip("Agility bonus from this equipment. Adds to total agility.")]
    public int agilityBonus = 0;

    [Header("Dice Granted")]
    [Tooltip("The list of dice this item adds to the player's dice pool when equipped.")]
    public List<DiceDefinitionSO> diceGranted;
}