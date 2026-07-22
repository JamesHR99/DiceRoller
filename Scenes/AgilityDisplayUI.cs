using UnityEngine;
using TMPro;

public class AgilityDisplayUI : MonoBehaviour
{
    public static AgilityDisplayUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI playerAgilityText;
    public TextMeshProUGUI enemyAgilityText;

    [Header("Display Settings")]
    public bool showBreakdown = true;

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

    void Start()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        UpdatePlayerAgility();
        UpdateEnemyAgility();
    }

    private void UpdatePlayerAgility()
    {
        if (playerAgilityText == null) return;

        if (PlayerManager.Instance == null || PlayerEquipment.Instance == null)
        {
            playerAgilityText.text = "Player Agility: --";
            return;
        }

        int totalAgility = PlayerManager.Instance.CalculateTotalAgility();

        if (showBreakdown)
        {
            string breakdown = GetPlayerAgilityBreakdown();
            playerAgilityText.text = $"Player Agility: {totalAgility}\n{breakdown}";
        }
        else
        {
            playerAgilityText.text = $"Player Agility: {totalAgility}";
        }
    }

    private void UpdateEnemyAgility()
    {
        if (enemyAgilityText == null) return;

        if (EnemyManager.Instance == null)
        {
            enemyAgilityText.text = "Enemy Agility: --";
            return;
        }

        int totalAgility = EnemyManager.Instance.CalculateTotalAgility();

        if (showBreakdown)
        {
            string breakdown = GetEnemyAgilityBreakdown();
            enemyAgilityText.text = $"Enemy Agility: {totalAgility}\n{breakdown}";
        }
        else
        {
            enemyAgilityText.text = $"Enemy Agility: {totalAgility}";
        }
    }

    private string GetPlayerAgilityBreakdown()
    {
        string breakdown = "";
        int classAgility = 0;

        if (PlayerEquipment.Instance != null && PlayerEquipment.Instance.selectedCharacterClass != null)
        {
            classAgility = PlayerEquipment.Instance.selectedCharacterClass.baseAgility;
            breakdown += $"  Class: +{classAgility}";
        }

        if (PlayerEquipment.Instance != null)
        {
            EquipmentItemSO weapon = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Weapon);
            EquipmentItemSO armor = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Armor);
            EquipmentItemSO item = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Item);

            if (weapon != null && weapon.agilityBonus != 0)
            {
                breakdown += $"\n  {weapon.itemName}: {(weapon.agilityBonus > 0 ? "+" : "")}{weapon.agilityBonus}";
            }
            if (armor != null && armor.agilityBonus != 0)
            {
                breakdown += $"\n  {armor.itemName}: {(armor.agilityBonus > 0 ? "+" : "")}{armor.agilityBonus}";
            }
            if (item != null && item.agilityBonus != 0)
            {
                breakdown += $"\n  {item.itemName}: {(item.agilityBonus > 0 ? "+" : "")}{item.agilityBonus}";
            }
        }

        return breakdown;
    }

    private string GetEnemyAgilityBreakdown()
    {
        string breakdown = "";

        if (EnemyManager.Instance != null)
        {
            int baseAgility = EnemyManager.Instance.baseAgility;
            breakdown += $"  Base: +{baseAgility}";

            EquipmentItemSO weapon = EnemyManager.Instance.weapon;
            EquipmentItemSO armor = EnemyManager.Instance.armor;
            EquipmentItemSO item = EnemyManager.Instance.item;

            if (weapon != null && weapon.agilityBonus != 0)
            {
                breakdown += $"\n  {weapon.itemName}: {(weapon.agilityBonus > 0 ? "+" : "")}{weapon.agilityBonus}";
            }
            if (armor != null && armor.agilityBonus != 0)
            {
                breakdown += $"\n  {armor.itemName}: {(armor.agilityBonus > 0 ? "+" : "")}{armor.agilityBonus}";
            }
            if (item != null && item.agilityBonus != 0)
            {
                breakdown += $"\n  {item.itemName}: {(item.agilityBonus > 0 ? "+" : "")}{item.agilityBonus}";
            }
        }

        return breakdown;
    }
}
