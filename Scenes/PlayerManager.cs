using UnityEngine;
using TMPro;
using System.Collections.Generic; // <-- ADD THIS LINE
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public static event Action OnPlayerDied;

    [Header("Base Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int shield = 0;
    public TextMeshProUGUI healthText;
    public int Initiative = 10;
    
    [Header("Dodge System")]
    private float dodgeChance = 0f;
    
    [Header("Block System")]
    private float blockPercentage = 0f;
    private List<Sprite> blockDiceSprites = new List<Sprite>();

    [HideInInspector]
    public List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

    [Header("System References")]
    public PlayerEnergy playerEnergy;
    public PlayerEquipment playerEquipment;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (playerEnergy == null) playerEnergy = FindObjectOfType<PlayerEnergy>();
        if (playerEquipment == null) playerEquipment = FindObjectOfType<PlayerEquipment>();

        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void AddShield(int amount) { if (amount > 0) { shield += amount; UpdateHealthUI(); } }
    
    public void AddBlock(int percentage)
    {
        blockPercentage += percentage / 100f;
        blockPercentage = Mathf.Min(blockPercentage, 1f);
        Debug.Log($"Player block increased by {percentage}%. Total block: {blockPercentage * 100}%");
    }
    
    public void AddBlockWithSprites(int percentage, List<Sprite> sprites)
    {
        blockPercentage += percentage / 100f;
        blockPercentage = Mathf.Min(blockPercentage, 1f);
        if (sprites != null)
        {
            blockDiceSprites.AddRange(sprites);
        }
        Debug.Log($"Player block increased by {percentage}%. Total block: {blockPercentage * 100}%");
    }
    
    public float GetBlockPercentage()
    {
        return blockPercentage;
    }
    
    public List<Sprite> GetBlockSprites()
    {
        return blockDiceSprites;
    }
    
    public int ApplyBlockReduction(int incomingDamage)
    {
        if (blockPercentage <= 0f) return incomingDamage;
        
        int blockedDamage = Mathf.RoundToInt(incomingDamage * blockPercentage);
        int reducedDamage = incomingDamage - blockedDamage;
        
        Debug.Log($"Block reduced {blockedDamage} damage ({blockPercentage * 100}% reduction)! Damage: {incomingDamage} → {reducedDamage}");
        
        return reducedDamage;
    }
    
    public void ConsumeBlock(int originalDamage, int blockedAmount)
    {
        if (blockPercentage <= 0f) return;
        
        if (BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.PlayBlockAnimation($"Player's Block negated {blockedAmount} damage! ({blockPercentage * 100:F0}%)", true, blockDiceSprites);
        }
        
        blockPercentage = 0f;
        blockDiceSprites.Clear();
    }
    
    public void ResetShield() { shield = 0; blockPercentage = 0f; blockDiceSprites.Clear(); UpdateHealthUI(); }
    
    public void AddDodgeChance(int percentage)
    {
        dodgeChance += percentage / 100f;
        Debug.Log($"Player dodge chance increased by {percentage}%. Total: {dodgeChance * 100}%");
    }
    
    public void ResetDodgeChance()
    {
        dodgeChance = 0f;
    }
    
    public bool RollDodge()
    {
        if (dodgeChance <= 0) return false;
        bool dodged = UnityEngine.Random.value < dodgeChance;
        if (dodged)
        {
            Debug.Log($"Player dodged! (Dodge chance was {dodgeChance * 100}%)");
        }
        return dodged;
    }
    
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        
        if (RollDodge())
        {
            if (BattleAnimator.Instance != null)
            {
                BattleAnimator.Instance.PlayStatusEffectAnimation("Player dodged the attack!");
            }
            return;
        }
        
        int damageAbsorbed = Mathf.Min(shield, amount);
        shield -= damageAbsorbed;
        int remainingDamage = amount - damageAbsorbed;
        if (remainingDamage > 0) { currentHealth -= remainingDamage; }
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnPlayerDied?.Invoke();
        }
        UpdateHealthUI();
    }
    public void Heal(int amount) { if (amount > 0) { currentHealth += amount; if (currentHealth > maxHealth) { currentHealth = maxHealth; } UpdateHealthUI(); } }
    
    public void ShowRestOptions()
    {
        Debug.Log("Showing rest options - player can heal or prepare");
        
        int healAmount = Mathf.RoundToInt(maxHealth * 0.3f);
        Heal(healAmount);
        
        Debug.Log($"Rested and healed {healAmount} HP");
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnVictory();
        }
    }

    public int CalculateTotalAgility()
    {
        int totalAgility = 0;
        if (playerEquipment != null && playerEquipment.selectedCharacterClass != null)
            totalAgility += playerEquipment.selectedCharacterClass.baseAgility;
        return totalAgility;
    }

    private static bool IsWeaponAction(DiceActionType type)
    {
        return type == DiceActionType.Attack ||
               type == DiceActionType.HeavyAttack ||
               type == DiceActionType.LightAttack ||
               type == DiceActionType.SwiftStrike;
    }

    private static bool IsArmorAction(DiceActionType type)
    {
        return type == DiceActionType.Heal ||
               type == DiceActionType.RegainStamina ||
               type == DiceActionType.Block;
    }

    public int CalculateAgilityForAction(DiceActionType actionType)
    {
        int agility = CalculateTotalAgility();

        if (playerEquipment != null)
        {
            EquipmentItemSO weapon = playerEquipment.GetEquippedItem(EquipmentSlot.Weapon);
            EquipmentItemSO armor  = playerEquipment.GetEquippedItem(EquipmentSlot.Armor);
            EquipmentItemSO item   = playerEquipment.GetEquippedItem(EquipmentSlot.Item);

            if (weapon != null && IsWeaponAction(actionType)) agility += weapon.agilityBonus;
            if (armor  != null && IsArmorAction(actionType))  agility += armor.agilityBonus;
            if (item   != null) agility += item.agilityBonus;
        }

        if (SlotMachine.Instance != null && SlotMachine.Instance.allActionConfigs != null)
        {
            foreach (var config in SlotMachine.Instance.allActionConfigs)
            {
                if (config.actionType == actionType)
                {
                    agility += config.agility;
                    break;
                }
            }
        }

        return agility;
    }
    
    private void UpdateHealthUI() { if (healthText != null) { string shieldText = shield > 0 ? $" (+{shield})" : ""; healthText.text = $"Player HP: {currentHealth}/{maxHealth}{shieldText}"; } }
}