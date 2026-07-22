using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    [Header("References")]
    public PlayerManager playerManager;
    public EnemyManager enemyManager;
    public SlotMachine slotMachine;
    
    [Header("Battle Animation Timing")]
    [Tooltip("Delay before the first attack animation starts")]
    public float battleStartDelay = 1f;
    [Tooltip("Delay after showing attack text before applying damage")]
    public float attackAnimationDelay = 1.2f;
    [Tooltip("Delay after applying damage before next attack")]
    public float afterDamageDelay = 1.2f;
    [Tooltip("Delay after all attacks finish before status effects")]
    public float statusEffectDelay = 2f;
    
    private bool battleIsOver = false;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); }
        if (playerManager == null) playerManager = FindObjectOfType<PlayerManager>();
        if (enemyManager == null) enemyManager = FindObjectOfType<EnemyManager>();
        if (slotMachine == null) slotMachine = FindObjectOfType<SlotMachine>();
    }

    public void StartBattle(List<DiceActionDetails> playerAttackActions, List<DiceActionDetails> enemyAttackActions)
    {
        StopAllCoroutines();
        battleIsOver = false;
        StartCoroutine(BattleSequence(playerAttackActions, enemyAttackActions));
    }

    public void EndBattle()
    {
        battleIsOver = true;
        StopAllCoroutines();
    }

    IEnumerator BattleSequence(List<DiceActionDetails> playerAttackActions, List<DiceActionDetails> enemyAttackActions)
    {
        List<(DiceActionDetails action, bool isPlayer)> allAttacks = new List<(DiceActionDetails, bool)>();
        foreach (var action in playerAttackActions) { allAttacks.Add((action, true)); }
        foreach (var action in enemyAttackActions) { allAttacks.Add((action, false)); }
        
        allAttacks = allAttacks.OrderByDescending(a => a.action.Agility).ThenByDescending(a => a.isPlayer).ToList();

        Debug.Log("=== Battle Order ===");
        foreach (var attack in allAttacks)
        {
            string attacker = attack.isPlayer ? "Player" : "Enemy";
            Debug.Log($"{attacker} - {attack.action.Type} (Agility: {attack.action.Agility})");
        }

        yield return new WaitForSeconds(battleStartDelay);

        foreach (var attackEntry in allAttacks)
        {
            if (battleIsOver) { yield break; }

            DiceActionDetails action = attackEntry.action;
            bool isPlayerAttack = attackEntry.isPlayer;
            int baseDamage = action.Score;

            float damageMultiplier = 1f;
            if (StatusEffectManager.Instance != null)
            {
                Component attacker = isPlayerAttack ? (Component)playerManager : enemyManager;
                Component defender = isPlayerAttack ? (Component)enemyManager : playerManager;
                
                float weakMultiplier = StatusEffectManager.Instance.GetDamageMultiplier(attacker, true);
                float vulnMultiplier = StatusEffectManager.Instance.GetDamageMultiplier(defender, false);
                damageMultiplier = weakMultiplier * vulnMultiplier;
            }

            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
            
            // Check for block and calculate reduced damage
            float defenderBlockPercentage = 0f;
            int actualDamage = finalDamage;
            int blockedAmount = 0;
            List<Sprite> defenderBlockSprites = null;
            
            if (isPlayerAttack && enemyManager != null)
            {
                defenderBlockPercentage = enemyManager.GetBlockPercentage();
                if (defenderBlockPercentage > 0f)
                {
                    actualDamage = enemyManager.ApplyBlockReduction(finalDamage);
                    blockedAmount = finalDamage - actualDamage;
                    defenderBlockSprites = enemyManager.GetBlockSprites();
                }
            }
            else if (!isPlayerAttack && playerManager != null)
            {
                defenderBlockPercentage = playerManager.GetBlockPercentage();
                if (defenderBlockPercentage > 0f)
                {
                    actualDamage = playerManager.ApplyBlockReduction(finalDamage);
                    blockedAmount = finalDamage - actualDamage;
                    defenderBlockSprites = playerManager.GetBlockSprites();
                }
            }
            
            // Show attack animation with block info if applicable
            if (BattleAnimator.Instance != null)
            {
                string attacker = isPlayerAttack ? "Player" : "Enemy";
                string multiplierText = damageMultiplier != 1f ? $" (x{damageMultiplier:F1})" : "";
                string statusText = action.AppliedStatusEffect != null ? $" + {action.AppliedStatusEffect.GetDisplayName()}" : "";

                string critText = "";
                if (action.IsCritical)
                {
                    int critBonus = action.Score - action.BaseScore;
                    critText = $" <color=#FFD700>CRITICAL! +{critBonus} bonus damage!</color>";
                }
                
                string animationText;
                if (defenderBlockPercentage > 0f)
                {
                    animationText = $"{attacker} uses {action.Type} for {finalDamage} damage!{critText}{multiplierText} <color=#4444FF>-{blockedAmount} BLOCKED!</color> ({actualDamage} damage)";
                }
                else
                {
                    animationText = $"{attacker} uses {action.Type} for {finalDamage} damage!{critText}{multiplierText}{statusText}";
                }
                
                BattleAnimator.Instance.PlayAttackAnimation(animationText, isPlayerAttack, action.ContributingDiceSprites, action.Agility);
            }

            yield return new WaitForSeconds(attackAnimationDelay);
            
            // Show block negation card if block was used
            if (defenderBlockPercentage > 0f)
            {
                if (isPlayerAttack && enemyManager != null)
                {
                    enemyManager.ConsumeBlock(finalDamage, blockedAmount);
                }
                else if (!isPlayerAttack && playerManager != null)
                {
                    playerManager.ConsumeBlock(finalDamage, blockedAmount);
                }
                
                yield return new WaitForSeconds(attackAnimationDelay);
            }
            
            // Apply damage
            if (isPlayerAttack)
            {
                if (enemyManager != null)
                {
                    enemyManager.TakeDamage(actualDamage);
                    if (action.AppliedStatusEffect != null)
                    {
                        StatusEffectManager.Instance.ApplyStatusEffect(enemyManager, action.AppliedStatusEffect);
                    }
                    
                    if (RerollManager.Instance != null)
                    {
                        RerollManager.Instance.AddDamageDealt(actualDamage);
                    }
                }
            }
            else
            {
                if (playerManager != null)
                {
                    playerManager.TakeDamage(actualDamage);
                    if (action.AppliedStatusEffect != null)
                    {
                        StatusEffectManager.Instance.ApplyStatusEffect(playerManager, action.AppliedStatusEffect);
                    }
                }
            }

            yield return new WaitForSeconds(afterDamageDelay);
        }

        Debug.Log("--- Battle Phase Ended ---");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProcessStartOfTurnEffects();
        }

        yield return new WaitForSeconds(statusEffectDelay);

        if (enemyManager != null && enemyManager.gameObject.activeInHierarchy)
        {
            if (slotMachine != null) { slotMachine.spinButton.interactable = true; }
        }
    }
}