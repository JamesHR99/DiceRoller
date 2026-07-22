using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class EnemyTurnManager : MonoBehaviour
{
    // ===========================================================
    //  Singleton + Persistence
    // ===========================================================
    public static EnemyTurnManager Instance { get; private set; }

    // ===========================================================
    //  Inspector Fields
    // ===========================================================
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Vector3 enemySpawnPosition = Vector3.zero;

    [Header("Dynamic Dice Setup")]
    public GameObject dicePrefab;
    public Transform enemyDiceContainer;

    private List<Dice> activeDice = new List<Dice>();

    [Header("References")]
    public PlayerManager playerManager;
    public EnemyManager enemyManager;
    public SlotMachine slotMachine;

    private Dictionary<DiceActionType, DiceActionConfig> _masterActionConfigMap;


    // ===========================================================
    //  Awake (Singleton + DontDestroyOnLoad)
    // ===========================================================
    void Awake()
    {
        // Ensure there is only ONE instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Reacquire references whenever scenes change
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initial scene reference linking
        ReacquireSceneReferences();
    }


    // ===========================================================
    //  Scene Loaded Hook
    // ===========================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReacquireSceneReferences();
    }


    // ===========================================================
    //  Find scene objects again after scene transitions
    // ===========================================================
    private void ReacquireSceneReferences()
    {
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerManager>();

        if (enemyManager == null)
        {
            enemyManager = FindObjectOfType<EnemyManager>();

            if (enemyManager == null && enemyPrefab != null)
            {
                Debug.Log("No Enemy found in scene. Spawning new Enemy from prefab.");
                GameObject newEnemy = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
                newEnemy.name = "Enemy";
                enemyManager = newEnemy.GetComponent<EnemyManager>();

                if (enemyManager == null)
                {
                    Debug.LogError("Spawned Enemy prefab does not have an EnemyManager component!");
                }
                else
                {
                    ConnectEnemyToUI();
                    ResetEnemyState();
                }
            }
            else if (enemyManager == null)
            {
                Debug.LogWarning("No Enemy found in scene and no Enemy prefab assigned to EnemyTurnManager!");
            }
            else
            {
                ConnectEnemyToUI();
                ResetEnemyState();
            }
        }

        if (slotMachine == null)
            slotMachine = FindObjectOfType<SlotMachine>();

        if (enemyDiceContainer == null)
        {
            var allTransforms = FindObjectsOfType<Transform>();
            enemyDiceContainer = allTransforms.FirstOrDefault(t => t.name == "EnemyDiceContainer");
        }

        UpdateBattleManagerReferences();
        ResetGameState();
    }

    private void ConnectEnemyToUI()
    {
        if (enemyManager != null && enemyManager.healthText == null)
        {
            TMPro.TextMeshProUGUI enemyHealthText = GameObject.Find("Enemy Health")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (enemyHealthText != null)
            {
                enemyManager.healthText = enemyHealthText;
                Debug.Log("Connected Enemy to UI health text.");
            }
            else
            {
                Debug.LogWarning("Could not find 'Enemy Health' UI text in scene!");
            }
        }
    }

    private void ResetEnemyState()
    {
        if (enemyManager != null)
        {
            enemyManager.currentHealth = enemyManager.maxHealth;
            enemyManager.shield = 0;
            enemyManager.InitializeEnergy();
            enemyManager.gameObject.SetActive(true);

            if (enemyManager.activeStatusEffects != null)
            {
                enemyManager.activeStatusEffects.Clear();
            }

            enemyManager.RefreshUI();

            Debug.Log("Enemy state reset.");
        }
    }

    private void UpdateBattleManagerReferences()
    {
        if (BattleManager.Instance != null)
        {
            var battleManagerType = BattleManager.Instance.GetType();
            var enemyManagerField = battleManagerType.GetField("enemyManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (enemyManagerField != null && enemyManager != null)
            {
                enemyManagerField.SetValue(BattleManager.Instance, enemyManager);
                Debug.Log("Updated BattleManager enemy reference.");
            }
        }
    }

    private void ResetGameState()
    {
        if (BattleManager.Instance != null)
        {
            var battleIsOverField = BattleManager.Instance.GetType()
                .GetField("battleIsOver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (battleIsOverField != null)
            {
                battleIsOverField.SetValue(BattleManager.Instance, false);
            }
        }

        if (slotMachine != null)
        {
            if (slotMachine.spinButton != null)
            {
                slotMachine.spinButton.interactable = true;
            }
            if (slotMachine.nextTurnButton != null)
            {
                slotMachine.nextTurnButton.interactable = false;
            }
            Debug.Log("Game state reset - spin button enabled.");
        }
    }


    // ===========================================================
    //  Public API
    // ===========================================================
    public void Initialize(Dictionary<DiceActionType, DiceActionConfig> masterConfigMap)
    {
        _masterActionConfigMap = masterConfigMap;
    }


    // ===========================================================
    //  Enemy Dice Setup + Rolling
    // ===========================================================
    private void ConfigureAndRollEnemyDice()
    {
        // Clear old dice
        foreach (Dice die in activeDice)
        {
            if (die != null) Destroy(die.gameObject);
        }
        activeDice.Clear();

        // Get enemy's equipped dice from EnemyManager
        List<DiceDefinitionSO> equippedDiceDefs = enemyManager.GetEquippedDice();

        foreach (DiceDefinitionSO def in equippedDiceDefs)
        {
            GameObject diceGO = Instantiate(dicePrefab, enemyDiceContainer);
            Dice newDie = diceGO.GetComponent<Dice>();
            if (newDie != null)
            {
                newDie.ConfigureDie(def, _masterActionConfigMap);
                newDie.Roll();
                activeDice.Add(newDie);
                
                diceGO.SetActive(false);
            }
        }
        
        if (EnemyAttackPredictor.Instance != null)
        {
            EnemyAttackPredictor.Instance.UpdatePredictions(activeDice, enemyManager);
        }
        
        if (EnemyAttackPredictorNew.Instance != null)
        {
            EnemyAttackPredictorNew.Instance.UpdatePredictions(activeDice, enemyManager);
        }
    }
    
    public void RollEnemyDiceForPreview()
    {
        if (StatusEffectManager.Instance != null && StatusEffectManager.Instance.IsStunned(enemyManager))
        {
            if (EnemyAttackPredictor.Instance != null)
            {
                EnemyAttackPredictor.Instance.ClearPredictions();
            }
            Debug.Log("Enemy is stunned - no dice to preview.");
            return;
        }
        
        ConfigureAndRollEnemyDice();
        Debug.Log("Enemy dice rolled for preview. Player can now see likely enemy actions.");
    }
    
    public void ClearEnemyDicePreview()
    {
        foreach (Dice die in activeDice)
        {
            if (die != null) Destroy(die.gameObject);
        }
        activeDice.Clear();
        
        if (EnemyAttackPredictor.Instance != null)
        {
            EnemyAttackPredictor.Instance.ClearPredictions();
        }
        
        if (EnemyAttackPredictorNew.Instance != null)
        {
            EnemyAttackPredictorNew.Instance.ClearPredictions();
        }
    }


    // ===========================================================
    //  Main Enemy Turn Coroutine
    // ===========================================================
    public IEnumerator TakeEnemyTurn(List<DiceActionDetails> playerAttackActions)
    {
        if (StatusEffectManager.Instance != null && StatusEffectManager.Instance.IsStunned(enemyManager))
        {
            Debug.Log("Enemy is stunned and skips their turn!");
            yield return new WaitForSeconds(2f);
            
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartBattle(playerAttackActions, new List<DiceActionDetails>());
            }
            yield break;
        }

        enemyManager.AddEnergyForNewTurn();
        enemyManager.ResetShield();

        Debug.Log("--- Enemy is thinking... ---");

        List<DiceActionDetails> chosenActions = new List<DiceActionDetails>();
        List<Dice> diceToUse = new List<Dice>();

        // Use pre-calculated actions from predictor if available
        if (EnemyAttackPredictorNew.Instance != null)
        {
            chosenActions = EnemyAttackPredictorNew.Instance.GetPlannedActions();
            diceToUse = EnemyAttackPredictorNew.Instance.GetPlannedDice();
            Debug.Log($"Enemy using {chosenActions.Count} pre-calculated actions from predictor");
        }
        else
        {
            // Fallback: Calculate actions manually if no predictor
            List<Dice> availableDice = new List<Dice>(activeDice);
            
            Debug.Log($"Enemy rolled {availableDice.Count} dice:");
            foreach (var die in availableDice)
            {
                if (die != null)
                {
                    Debug.Log($"  - {die.GetCurrentAction()}");
                }
            }

            int energyBudget = Random.Range(enemyManager.minEnergyToSpend, enemyManager.maxEnergyToSpend + 1);
            energyBudget = Mathf.Min(energyBudget, enemyManager.GetCurrentEnergy());
        
        Debug.Log($"Energy budget: {energyBudget} (min: {enemyManager.minEnergyToSpend}, max: {enemyManager.maxEnergyToSpend}, available: {enemyManager.GetCurrentEnergy()})");

        // =======================================================
        // Try using enemy combos first
        // =======================================================
        if (enemyManager.availableCombos.Any())
        {
            var bestComboInfo = FindBestAvailableCombo(availableDice, enemyManager.availableCombos);

            if (bestComboInfo.HasValue)
            {
                var (combo, usedDiceForCombo) = bestComboInfo.Value;

                if (usedDiceForCombo.Count <= energyBudget && Random.value < combo.aiUsageLikelihood)
                {
                    var sprites = usedDiceForCombo.Select(d => d.diceImage.sprite).ToList();
                    
                    int actionAgility = EnemyManager.Instance != null 
                        ? EnemyManager.Instance.CalculateAgilityForAction(combo.resultActionType) 
                        : 0;

                    chosenActions.Add(new DiceActionDetails(
                        combo.resultActionType,
                        combo.baseValue,
                        combo.baseSpeed,
                        sprites,
                        false,
                        null,
                        actionAgility
                    ));

                    diceToUse.AddRange(usedDiceForCombo);
                    availableDice = availableDice.Except(usedDiceForCombo).ToList();
                    energyBudget -= usedDiceForCombo.Count;
                }
            }
        }

        // =======================================================
        // Pick best individual dice using remaining energy
        // =======================================================
        bool isDefensiveMode = enemyManager.ShouldBeDefensive();
        
        Debug.Log($"Remaining energy budget after combos: {energyBudget}");
        Debug.Log($"Remaining available dice: {availableDice.Count}");
        
        if (isDefensiveMode)
        {
            Debug.Log($"Enemy is in DEFENSIVE mode (Health: {enemyManager.GetHealthPercentage():P0}, Threshold: {enemyManager.healthThreshold:P0})");
        }
        else
        {
            Debug.Log($"Enemy is in OFFENSIVE mode");
        }
        
        int dicePickedCount = 0;
        while (energyBudget > 0 && availableDice.Any())
        {
            Dice dieToPick = null;

            if (isDefensiveMode)
            {
                Debug.Log($"  Looking for defensive dice...");
                dieToPick =
                    FindBestDieOfType(availableDice, DiceActionType.Heal) ??
                    FindBestDieOfType(availableDice, DiceActionType.Defend) ??
                    FindBestDieOfType(availableDice, DiceActionType.Block);
                
                if (dieToPick != null)
                {
                    Debug.Log($"  Found defensive die: {dieToPick.GetCurrentAction()}");
                }
            }

            if (dieToPick == null)
            {
                Debug.Log($"  Looking for offensive dice...");
                dieToPick =
                    FindBestDieOfType(availableDice, DiceActionType.Attack) ??
                    FindBestDieOfType(availableDice, DiceActionType.HeavyAttack) ??
                    FindBestDieOfType(availableDice, DiceActionType.SwiftStrike) ??
                    FindBestDieOfType(availableDice, DiceActionType.LightAttack);
                
                if (dieToPick != null)
                {
                    Debug.Log($"  Found offensive die: {dieToPick.GetCurrentAction()}");
                }
                else
                {
                    Debug.Log($"  No offensive dice found!");
                }
            }

            if (dieToPick == null)
            {
                Debug.Log($"  No dice found at all! Breaking.");
                break;
            }

            DiceActionDetails details = dieToPick.GetActionDetails();
            int actionAgility = EnemyManager.Instance != null 
                ? EnemyManager.Instance.CalculateAgilityForAction(details.Type) 
                : 0;
            
            DiceActionDetails detailsWithAgility = new DiceActionDetails(
                details.Type, 
                details.Score, 
                details.Speed, 
                details.ContributingDiceSprites, 
                details.IsCritical, 
                details.AppliedStatusEffect,
                actionAgility,
                details.BaseScore,
                details.CritBonusValue
            );
            
            chosenActions.Add(detailsWithAgility);
            diceToUse.Add(dieToPick);
            availableDice.Remove(dieToPick);

            energyBudget--;
            dicePickedCount++;
        }
        
        Debug.Log($"Enemy picked {dicePickedCount} individual dice. Total actions: {chosenActions.Count}");
        }

        enemyManager.SpendEnergy(diceToUse.Count);

        yield return new WaitForSeconds(1.0f);

        // =======================================================
        // Execute actions
        // =======================================================
        var enemyAttackActions = new List<DiceActionDetails>();
        var enemyImmediateActions = new List<DiceActionDetails>();

        foreach (var action in chosenActions)
        {
            if (action.Type == DiceActionType.Attack || 
                action.Type == DiceActionType.SwiftStrike ||
                action.Type == DiceActionType.HeavyAttack ||
                action.Type == DiceActionType.LightAttack)
            {
                enemyAttackActions.Add(action);
                Debug.Log($"Enemy attack action: {action.Type} ({action.Score} damage)");
            }
            else
            {
                enemyImmediateActions.Add(action);
                Debug.Log($"Enemy immediate action: {action.Type} ({action.Score})");
            }
        }
        
        Debug.Log($"Total enemy attack actions: {enemyAttackActions.Count}, immediate actions: {enemyImmediateActions.Count}");

        yield return StartCoroutine(ProcessEnemyImmediateActionsWithDelay(enemyImmediateActions));

        if (enemyDiceContainer != null)
            enemyDiceContainer.gameObject.SetActive(false);
        
        ClearEnemyDicePreview();

        slotMachine.battleManager.StartBattle(playerAttackActions, enemyAttackActions);
    }


    // ===========================================================
    //  Utility Methods
    // ===========================================================
    private Dice FindBestDieOfType(List<Dice> dicePool, DiceActionType type)
    {
        return dicePool
            .Where(d => d.GetCurrentAction() == type)
            .OrderByDescending(d => d.GetActionDetails().Score)
            .FirstOrDefault();
    }

    private (DiceComboSO combo, List<Dice> usedDice)? FindBestAvailableCombo(
        List<Dice> availableDice,
        List<DiceComboSO> enemyCombos)
    {
        var sortedCombos = enemyCombos.OrderByDescending(c => c.priority).ToList();

        Debug.Log($"Enemy checking {sortedCombos.Count} combos. Available dice: {availableDice.Count}");
        foreach (var d in availableDice)
        {
            Debug.Log($"  - Available die: {d.GetCurrentAction()}");
        }

        foreach (var combo in sortedCombos)
        {
            Debug.Log($"Checking combo: {combo.comboName} (Priority: {combo.priority}, Likelihood: {combo.aiUsageLikelihood})");
            Debug.Log($"  Required: {string.Join(", ", combo.requiredActions)}");
            
            if (combo.requiredActions == null || combo.requiredActions.Count > availableDice.Count)
            {
                Debug.Log($"  SKIPPED: Required actions null or too many ({combo.requiredActions?.Count} > {availableDice.Count})");
                continue;
            }

            var dicePoolCopy = new List<Dice>(availableDice);
            var usedDiceForCombo = new List<Dice>();
            bool comboPossible = true;

            foreach (var requiredAction in combo.requiredActions)
            {
                int dieIndex = dicePoolCopy.FindIndex(d => d.GetCurrentAction() == requiredAction);
                if (dieIndex != -1)
                {
                    usedDiceForCombo.Add(dicePoolCopy[dieIndex]);
                    dicePoolCopy.RemoveAt(dieIndex);
                }
                else
                {
                    comboPossible = false;
                    Debug.Log($"  FAILED: Missing {requiredAction}");
                    break;
                }
            }

            if (comboPossible)
            {
                Debug.Log($"  SUCCESS: Combo is possible! Found all {usedDiceForCombo.Count} required dice.");
                return (combo, usedDiceForCombo);
            }
        }

        Debug.Log("No combos available.");
        return null;
    }

    private IEnumerator ProcessEnemyImmediateActionsWithDelay(List<DiceActionDetails> actionsToProcess)
    {
        int totalHeal = 0;
        int totalShield = 0;
        int totalBlock = 0;
        int healCritBonus = 0;
        int blockCritBonus = 0;
        int rechargeAmount = 0;
        int rechargeCritBonus = 0;
        List<Sprite> healSprites = new List<Sprite>();
        List<Sprite> shieldSprites = new List<Sprite>();
        List<Sprite> blockSprites = new List<Sprite>();
        List<Sprite> rechargeSprites = new List<Sprite>();

        foreach (DiceActionDetails details in actionsToProcess)
        {
            switch (details.Type)
            {
                case DiceActionType.Defend:
                    if (details.Score == 100)
                    {
                        int critBonus = details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0;
                        enemyManager?.AddBlockWithSprites(100 + critBonus, details.ContributingDiceSprites);
                        totalBlock += 100;
                        if (details.IsCritical) blockCritBonus += critBonus;
                        if (details.ContributingDiceSprites != null)
                            blockSprites.AddRange(details.ContributingDiceSprites);
                    }
                    else
                    {
                        enemyManager?.AddShield(details.Score);
                        totalShield += details.Score;
                        if (details.ContributingDiceSprites != null)
                            shieldSprites.AddRange(details.ContributingDiceSprites);
                    }
                    break;

                case DiceActionType.Block:
                    int critBlockBonus = details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0;
                    int pct = 15 + critBlockBonus;
                    enemyManager?.AddBlockWithSprites(pct, details.ContributingDiceSprites);
                    totalBlock += 15;
                    if (details.IsCritical) blockCritBonus += critBlockBonus;
                    if (details.ContributingDiceSprites != null)
                        blockSprites.AddRange(details.ContributingDiceSprites);
                    break;

                case DiceActionType.Heal:
                    float healBonusPct = details.CritBonusValue;
                    int healAmount = details.Score;
                    int critHealExtra = 0;
                    if (details.IsCritical)
                    {
                        critHealExtra = Mathf.RoundToInt(details.Score * healBonusPct);
                        healAmount += critHealExtra;
                        healCritBonus += critHealExtra;
                    }
                    enemyManager?.Heal(healAmount);
                    totalHeal += details.Score;
                    if (details.ContributingDiceSprites != null)
                        healSprites.AddRange(details.ContributingDiceSprites);
                    break;

                case DiceActionType.Recharge:
                case DiceActionType.RegainStamina:
                    int critRechargeBonus = details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0;
                    int energy = 2 + critRechargeBonus;
                    enemyManager?.AddBonusEnergy(energy);
                    rechargeAmount += 2;
                    if (details.IsCritical) rechargeCritBonus += critRechargeBonus;
                    if (details.ContributingDiceSprites != null)
                        rechargeSprites.AddRange(details.ContributingDiceSprites);
                    break;
            }
        }

        if (totalHeal > 0 && BattleAnimator.Instance != null)
        {
            int totalHealWithCrit = totalHeal + healCritBonus;
            string healText = healCritBonus > 0
                ? $"Enemy Healed +{totalHeal} HP <color=#FFD700>CRITICAL! +{healCritBonus} bonus!</color> ({totalHealWithCrit} total)"
                : $"Enemy Healed +{totalHeal} HP";
            BattleAnimator.Instance.PlayHealAnimation(healText, false, healSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (totalBlock > 0 && BattleAnimator.Instance != null)
        {
            int totalBlockWithCrit = totalBlock + blockCritBonus;
            string blockText;
            if (blockCritBonus > 0)
                blockText = totalBlockWithCrit >= 100
                    ? $"Enemy Block: {totalBlock}% <color=#FFD700>CRITICAL! +{blockCritBonus}%!</color> (100% total)"
                    : $"Enemy Block: {totalBlock}% <color=#FFD700>CRITICAL! +{blockCritBonus}%!</color> ({totalBlockWithCrit}% total)";
            else
                blockText = totalBlock >= 100 ? "Enemy Block: 100% (Full Block!)" : $"Enemy Block: {totalBlock}%";
            BattleAnimator.Instance.PlayBlockAnimation(blockText, false, blockSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (totalShield > 0 && BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.PlayBlockAnimation($"Enemy Shield +{totalShield}", false, shieldSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (rechargeAmount > 0 && BattleAnimator.Instance != null)
        {
            string rechargeText = rechargeCritBonus > 0
                ? $"Enemy Recharging +{rechargeAmount} energy <color=#FFD700>CRITICAL! +{rechargeCritBonus} bonus!</color> ({rechargeAmount + rechargeCritBonus} total)"
                : $"Enemy Recharging +{rechargeAmount} energy next turn";
            BattleAnimator.Instance.PlayHealAnimation(rechargeText, false, rechargeSprites);
            yield return new WaitForSeconds(1.5f);
        }
    }
}
