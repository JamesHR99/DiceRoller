using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public static event Action OnEnemyDied;

    [Header("Base Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int shield = 0;
    public TextMeshProUGUI healthText;
    public int Initiative = 5;
    [Tooltip("Base agility for this enemy. Higher values act first in combat.")]
    public int baseAgility = 10;
    [Tooltip("Agility bonus range loaded from enemy template.")]
    public int agilityBonusMin = 0;
    public int agilityBonusMax = 0;

    [HideInInspector]
    public List<StatusEffect> activeStatusEffects = new List<StatusEffect>();
    
    [Header("Block System")]
    private float blockPercentage = 0f;
    private List<Sprite> blockDiceSprites = new List<Sprite>();

    [Header("Enemy Energy System")]
    public int maxEnergy = 6;
    public int energyPerTurn = 2;
    private int currentEnergy;

    [Header("Enemy Equipment")]
    public EquipmentItemSO weapon;
    public EquipmentItemSO armor;
    public EquipmentItemSO item;

    [Header("AI & Combo Settings")]
    public List<DiceComboSO> availableCombos;
    public int maxDiceSelection = 5;
    public int minEnergyToSpend = 2;
    public int maxEnergyToSpend = 4;
    [Range(0, 1)]
    public float defensiveTendency = 0.75f;
    [Range(0, 1)]
    public float healthThreshold = 0.5f;

    [Header("Enemy Templates by Difficulty")]
    public List<EnemyTemplate> easyEnemies = new List<EnemyTemplate>();
    public List<EnemyTemplate> mediumEnemies = new List<EnemyTemplate>();
    public List<EnemyTemplate> hardEnemies = new List<EnemyTemplate>();
    public List<EnemyTemplate> eliteEnemies = new List<EnemyTemplate>();
    public List<EnemyTemplate> bossEnemies = new List<EnemyTemplate>();

    [Header("Current Enemy Info")]
    private EnemyDifficulty currentDifficulty;
    private int currentDepth;
    private int goldReward;

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        InitializeEnemy();
    }

    private void InitializeEnemy()
    {
        InitializeEnergy();
        currentHealth = maxHealth;
        
        if (goldReward == 0)
        {
            int defaultBaseGold = 20;
            CalculateGoldReward(defaultBaseGold, currentDepth);
        }
        
        UpdateHealthUI();
    }

    public void LoadEnemyByDifficulty(EnemyDifficulty difficulty, int depth = 0)
    {
        currentDifficulty = difficulty;
        currentDepth = depth;

        List<EnemyTemplate> templatePool = GetTemplatePoolByDifficulty(difficulty);

        if (templatePool.Count == 0)
        {
            Debug.LogWarning($"No enemy templates defined for difficulty: {difficulty}. Using defaults.");
            SetDefaultStats(difficulty);
            return;
        }

        EnemyTemplate template = templatePool[UnityEngine.Random.Range(0, templatePool.Count)];
        ApplyTemplate(template, depth);
    }

    private List<EnemyTemplate> GetTemplatePoolByDifficulty(EnemyDifficulty difficulty)
    {
        return difficulty switch
        {
            EnemyDifficulty.Easy => easyEnemies,
            EnemyDifficulty.Medium => mediumEnemies,
            EnemyDifficulty.Hard => hardEnemies,
            EnemyDifficulty.Elite => eliteEnemies,
            EnemyDifficulty.Boss => bossEnemies,
            _ => easyEnemies
        };
    }

    private void ApplyTemplate(EnemyTemplate template, int depth)
    {
        gameObject.SetActive(true);
        
        maxHealth = Mathf.RoundToInt(template.baseHealth * (1f + depth * 0.1f));
        currentHealth = maxHealth;
        shield = 0;

        baseAgility = template.baseAgility;
        agilityBonusMin = template.agilityBonusMin;
        agilityBonusMax = template.agilityBonusMax;

        maxEnergy = template.maxEnergy;
        energyPerTurn = template.energyPerTurn;
        currentEnergy = maxEnergy;

        weapon = template.weapon;
        armor = template.armor;
        item = template.item;

        availableCombos = template.availableCombos != null ? new List<DiceComboSO>(template.availableCombos) : new List<DiceComboSO>();
        
        minEnergyToSpend = template.minEnergyToSpend;
        maxEnergyToSpend = template.maxEnergyToSpend;
        defensiveTendency = template.defensiveTendency;
        healthThreshold = template.healthThreshold;

        CalculateGoldReward(template.baseGoldReward, depth);

        activeStatusEffects.Clear();

        if (template.enemySprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = template.enemySprite;
                Debug.Log($"Applied sprite for {template.enemyName}");
            }
            else
            {
                Debug.LogWarning($"No SpriteRenderer found on {gameObject.name} to apply enemy sprite!");
            }
        }

        Debug.Log($"Loaded enemy template: {template.enemyName} | HP: {maxHealth} | Energy: {maxEnergy} | Gold: {goldReward}");
        
        UpdateHealthUI();
    }

    private void SetDefaultStats(EnemyDifficulty difficulty)
    {
        gameObject.SetActive(true);
        
        maxHealth = difficulty switch
        {
            EnemyDifficulty.Easy => 50,
            EnemyDifficulty.Medium => 80,
            EnemyDifficulty.Hard => 120,
            EnemyDifficulty.Elite => 150,
            EnemyDifficulty.Boss => 250,
            _ => 100
        };

        int baseGold = difficulty switch
        {
            EnemyDifficulty.Easy => 15,
            EnemyDifficulty.Medium => 25,
            EnemyDifficulty.Hard => 40,
            EnemyDifficulty.Elite => 60,
            EnemyDifficulty.Boss => 100,
            _ => 20
        };

        currentHealth = maxHealth;
        shield = 0;
        InitializeEnergy();
        CalculateGoldReward(baseGold, currentDepth);
        UpdateHealthUI();
    }

    public List<DiceDefinitionSO> GetEquippedDice()
    {
        var allDice = new List<DiceDefinitionSO>();
        if (weapon != null) allDice.AddRange(weapon.diceGranted);
        if (armor != null) allDice.AddRange(armor.diceGranted);
        if (item != null) allDice.AddRange(item.diceGranted);
        return allDice;
    }

    private void CalculateGoldReward(int baseGold, int depth)
    {
        float depthMultiplier = 1f + (depth * UnityEngine.Random.Range(0.10f, 0.15f));
        float variationMultiplier = UnityEngine.Random.Range(0.85f, 1.15f);
        goldReward = Mathf.RoundToInt(baseGold * depthMultiplier * variationMultiplier);
        Debug.Log($"Gold reward calculated: {goldReward} (Base: {baseGold}, Depth: {depth}, DepthMult: {depthMultiplier:F2}, Variation: {variationMultiplier:F2})");
    }

    public int GetGoldReward()
    {
        return goldReward;
    }

    public void InitializeEnergy()
    {
        currentEnergy = maxEnergy;
    }

    public void AddEnergyForNewTurn()
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyPerTurn);
    }

    /// <summary>Adds a bonus amount of energy immediately (e.g. from a critical recharge die).</summary>
    public void AddBonusEnergy(int amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }

    public bool SpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }

    public void ReduceStamina(int amount)
    {
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
        Debug.Log($"Enemy stamina reduced by {amount}. Current stamina: {currentEnergy}");
    }

    public int GetCurrentEnergy()
    {
        return currentEnergy;
    }
    
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
    
    public bool ShouldBeDefensive()
    {
        float healthPercent = GetHealthPercentage();
        
        if (healthPercent > healthThreshold)
        {
            return false;
        }
        
        // Calculate how far below threshold we are (0.0 = at threshold, 1.0 = at 0% health)
        float belowThresholdPercent = 1f - (healthPercent / healthThreshold);
        
        // Scale defensive tendency from base value up to 100% as health decreases
        // Example: if defensiveTendency = 0.75 and enemy is at 0% health:
        //   scaledTendency = 0.75 + (1.0 - 0.75) * 1.0 = 1.0 (100%)
        // Example: if defensiveTendency = 0.75 and enemy is at threshold (50%):
        //   scaledTendency = 0.75 + (1.0 - 0.75) * 0.0 = 0.75 (75%)
        float scaledTendency = defensiveTendency + (1f - defensiveTendency) * belowThresholdPercent;
        
        bool isDefensive = UnityEngine.Random.value < scaledTendency;
        
        if (isDefensive)
        {
            Debug.Log($"Enemy defensive! HP: {healthPercent:P0}, Threshold: {healthThreshold:P0}, Scaled Tendency: {scaledTendency:P0} (base: {defensiveTendency:P0})");
        }
        
        return isDefensive;
    }

    public void AddShield(int amount)
    {
        if (amount > 0)
        {
            shield += amount;
            UpdateHealthUI();
        }
    }

    public void ResetShield()
    {
        shield = 0;
        blockPercentage = 0f;
        blockDiceSprites.Clear();
        UpdateHealthUI();
    }
    
    public void AddBlockWithSprites(int percentage, List<Sprite> sprites)
    {
        blockPercentage += percentage / 100f;
        blockPercentage = Mathf.Min(blockPercentage, 1f);
        if (sprites != null)
        {
            blockDiceSprites.AddRange(sprites);
        }
        Debug.Log($"Enemy block increased by {percentage}%. Total block: {blockPercentage * 100}%");
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
        
        Debug.Log($"Enemy block reduced {blockedDamage} damage ({blockPercentage * 100}% reduction)! Damage: {incomingDamage} → {reducedDamage}");
        
        return reducedDamage;
    }
    
    public void ConsumeBlock(int originalDamage, int blockedAmount)
    {
        if (blockPercentage <= 0f) return;
        
        if (BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.PlayBlockAnimation($"Enemy's Block negated {blockedAmount} damage! ({blockPercentage * 100:F0}%)", false, blockDiceSprites);
        }
        
        blockPercentage = 0f;
        blockDiceSprites.Clear();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        int damageAbsorbed = Mathf.Min(shield, amount);
        shield -= damageAbsorbed;
        int remainingDamage = amount - damageAbsorbed;

        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnEnemyDied?.Invoke();
            gameObject.SetActive(false);
        }

        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        if (amount > 0)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            UpdateHealthUI();
        }
    }

    public void RefreshUI()
    {
        UpdateHealthUI();
    }

    public int CalculateTotalAgility()
    {
        return baseAgility + UnityEngine.Random.Range(agilityBonusMin, agilityBonusMax + 1);
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

        if (weapon != null && IsWeaponAction(actionType)) agility += weapon.agilityBonus;
        if (armor  != null && IsArmorAction(actionType))  agility += armor.agilityBonus;
        if (item   != null) agility += item.agilityBonus;

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

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            string shieldText = shield > 0 ? $" (+{shield})" : "";
            healthText.text = $"Enemy HP: {currentHealth}/{maxHealth}{shieldText}";
        }
    }
}

[System.Serializable]
public class EnemyTemplate
{
    public string enemyName = "Enemy";
    public Sprite enemySprite;
    public int baseHealth = 100;
    public int baseGoldReward = 20;
    [Tooltip("Base agility for this enemy template. Higher values act first in combat.")]
    public int baseAgility = 10;
    [Tooltip("Minimum agility bonus added to this enemy's total agility.")]
    public int agilityBonusMin = 0;
    [Tooltip("Maximum agility bonus added to this enemy's total agility. Higher difficulty enemies should have higher ranges.")]
    public int agilityBonusMax = 0;
    public int maxEnergy = 6;
    public int energyPerTurn = 2;
    public EquipmentItemSO weapon;
    public EquipmentItemSO armor;
    public EquipmentItemSO item;
    public List<DiceComboSO> availableCombos;
    public int minEnergyToSpend = 2;
    public int maxEnergyToSpend = 4;
    [Range(0, 1)]
    [Tooltip("Chance to switch to defensive behavior when below health threshold. 0 = never defensive, 1 = always defensive when injured.")]
    public float defensiveTendency = 0.75f;
    [Range(0, 1)]
    [Tooltip("Health percentage threshold to trigger defensive behavior. 0.5 = 50% health.")]
    public float healthThreshold = 0.5f;
}