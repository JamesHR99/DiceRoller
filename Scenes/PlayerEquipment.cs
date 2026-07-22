using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance { get; private set; }

    [Header("Starting Equipment")]
    [Tooltip("The weapon the player will start the game with.")]
    public EquipmentItemSO startingWeapon;
    [Tooltip("The armor the player will start the game with.")]
    public EquipmentItemSO startingArmor;
    [Tooltip("The item the player will start the game with.")]
    public EquipmentItemSO startingItem;

    [Header("Character Class Selection")]
    [Tooltip("If true, waits for character class selection before equipping starting items.")]
    public bool useCharacterClassSelection = false;

    [HideInInspector]
    public CharacterClassSO selectedCharacterClass;

    // --- NEW: Fields to view current equipment in the Inspector ---
    [Header("Current Equipment (Read-Only)")]
    [SerializeField] private EquipmentItemSO _currentWeapon;
    [SerializeField] private EquipmentItemSO _currentArmor;
    [SerializeField] private EquipmentItemSO _currentItem;

    // This dictionary holds the actual equipped items data.
    private Dictionary<EquipmentSlot, EquipmentItemSO> equippedItems = new Dictionary<EquipmentSlot, EquipmentItemSO>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (!useCharacterClassSelection)
        {
            if (startingWeapon != null) EquipItem(startingWeapon);
            if (startingArmor != null) EquipItem(startingArmor);
            if (startingItem != null) EquipItem(startingItem);
        }
    }

    public void SetStartingEquipment(CharacterClassSO characterClass, EquipmentItemSO weapon, EquipmentItemSO armor, EquipmentItemSO item)
    {
        selectedCharacterClass = characterClass;
        Debug.Log($"Setting starting equipment from character class: {characterClass.className}");
        
        if (weapon != null) EquipItem(weapon);
        if (armor != null) EquipItem(armor);
        if (item != null) EquipItem(item);
    }

    public void EquipItem(EquipmentItemSO itemToEquip)
    {
        if (itemToEquip == null) return;

        // The core logic remains the same: update the dictionary
        equippedItems[itemToEquip.equipmentSlot] = itemToEquip;
        Debug.Log($"Equipped {itemToEquip.itemName} in the {itemToEquip.equipmentSlot} slot.");

        // --- NEW: Update the corresponding debug field for Inspector visibility ---
        switch (itemToEquip.equipmentSlot)
        {
            case EquipmentSlot.Weapon:
                _currentWeapon = itemToEquip;
                break;
            case EquipmentSlot.Armor:
                _currentArmor = itemToEquip;
                break;
            case EquipmentSlot.Item:
                _currentItem = itemToEquip;
                break;
        }
    }

    public List<DiceDefinitionSO> GetEquippedDice()
    {
        var allDice = new List<DiceDefinitionSO>();
        foreach (var item in equippedItems.Values)
        {
            if (item != null && item.diceGranted != null)
            {
                allDice.AddRange(item.diceGranted);
            }
        }
        return allDice;
    }

    public EquipmentItemSO GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            return equippedItems[slot];
        }
        return null;
    }

    public EquipmentItemSO GetCurrentWeapon() => _currentWeapon;
    public EquipmentItemSO GetCurrentArmor() => _currentArmor;
    public EquipmentItemSO GetCurrentItem() => _currentItem;
}