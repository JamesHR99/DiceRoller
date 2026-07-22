using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StatusEffectManager : MonoBehaviour
{
    public static StatusEffectManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void ApplyStatusEffect(Component target, StatusEffect effect)
    {
        if (effect == null || effect.type == StatusEffectType.None) return;

        List<StatusEffect> targetList = GetTargetList(target);
        if (targetList != null)
        {
            StatusEffect existingEffect = targetList.FirstOrDefault(e => e.type == effect.type);
            
            if (existingEffect != null)
            {
                if (effect.type == StatusEffectType.Bleed)
                {
                    existingEffect.duration = effect.duration;
                    Debug.Log($"Refreshed {effect.type} on {target.name} - Duration reset to: {existingEffect.duration}");
                }
                else
                {
                    existingEffect.duration = effect.duration;
                    existingEffect.damagePerTurn = effect.damagePerTurn;
                    Debug.Log($"Refreshed {effect.type} on {target.name} - Duration reset to: {existingEffect.duration}");
                }
            }
            else
            {
                targetList.Add(new StatusEffect(effect.type, effect.damagePerTurn, effect.duration, effect.multiplier));
                Debug.Log($"Applied {effect.GetDisplayName()} to {target.name} for {effect.duration} turns");
            }
        }
    }

    public void ProcessTurnEffects()
    {
        Debug.Log("=== ProcessTurnEffects called ===");
        if (PlayerManager.Instance != null) ProcessEffectsForTarget(PlayerManager.Instance);
        if (EnemyManager.Instance != null) ProcessEffectsForTarget(EnemyManager.Instance);
    }

    private void ProcessEffectsForTarget(Component target)
    {
        List<StatusEffect> targetList = GetTargetList(target);
        if (targetList == null || targetList.Count == 0) return;

        Debug.Log($"Processing {targetList.Count} effects for {target.name}");
        int totalDamageThisTurn = 0;
        bool isStunned = false;
        List<string> effectMessages = new List<string>();

        for (int i = targetList.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = targetList[i];
            Debug.Log($"  - {effect.type}: {effect.damagePerTurn} dmg, {effect.duration} turns left");

            switch (effect.type)
            {
                case StatusEffectType.Burn:
                case StatusEffectType.Poison:
                    totalDamageThisTurn += effect.damagePerTurn;
                    effectMessages.Add($"{effect.GetDisplayName()} ({effect.damagePerTurn} damage)");
                    break;

                case StatusEffectType.Bleed:
                    totalDamageThisTurn += effect.damagePerTurn;
                    effectMessages.Add($"{effect.GetDisplayName()} ({effect.damagePerTurn} damage)");
                    Debug.Log($"{target.name} bleeds for {effect.damagePerTurn} damage");
                    break;

                case StatusEffectType.Stun:
                    isStunned = true;
                    effectMessages.Add($"{effect.GetDisplayName()}");
                    Debug.Log($"{target.name} is stunned!");
                    break;

                case StatusEffectType.Freeze:
                    if (target is EnemyManager enemy)
                    {
                        enemy.ReduceStamina(2);
                        effectMessages.Add($"{effect.GetDisplayName()} (stamina -2)");
                        Debug.Log($"{target.name} loses 2 stamina from freeze");
                    }
                    break;
            }

            effect.duration--;

            if (effect.duration <= 0)
            {
                Debug.Log($"{effect.GetDisplayName()} wore off from {target.name}");
                targetList.RemoveAt(i);
            }
        }

        if (totalDamageThisTurn > 0)
        {
            if (target is PlayerManager player) player.TakeDamage(totalDamageThisTurn);
            else if (target is EnemyManager enemy) enemy.TakeDamage(totalDamageThisTurn);
            Debug.Log($"{target.name} takes {totalDamageThisTurn} damage from status effects");
        }

        if (effectMessages.Count > 0 && BattleAnimator.Instance != null)
        {
            string targetName = target is PlayerManager ? "Player" : "Enemy";
            string message = totalDamageThisTurn > 0 
                ? $"{targetName} takes {totalDamageThisTurn} from {string.Join(", ", effectMessages)}"
                : $"{targetName} affected by {string.Join(", ", effectMessages)}";
            BattleAnimator.Instance.PlayStatusEffectAnimation(message);
        }

        if (isStunned && target is EnemyManager)
        {
            Debug.Log("Enemy turn skipped due to stun!");
        }
    }

    public bool IsStunned(Component target)
    {
        List<StatusEffect> targetList = GetTargetList(target);
        if (targetList == null) return false;
        return targetList.Any(e => e.type == StatusEffectType.Stun && e.duration > 0);
    }

    public float GetDamageMultiplier(Component target, bool isDealingDamage)
    {
        List<StatusEffect> targetList = GetTargetList(target);
        if (targetList == null) return 1f;

        float multiplier = 1f;

        if (isDealingDamage)
        {
            StatusEffect weakEffect = targetList.FirstOrDefault(e => e.type == StatusEffectType.Weak);
            if (weakEffect != null) multiplier *= weakEffect.multiplier;
        }
        else
        {
            StatusEffect vulnEffect = targetList.FirstOrDefault(e => e.type == StatusEffectType.Vulnerable);
            if (vulnEffect != null) multiplier *= vulnEffect.multiplier;
        }

        return multiplier;
    }

    public List<StatusEffect> GetActiveEffects(Component target)
    {
        return GetTargetList(target) ?? new List<StatusEffect>();
    }

    private List<StatusEffect> GetTargetList(Component target)
    {
        if (target is PlayerManager player) return player.activeStatusEffects;
        if (target is EnemyManager enemy) return enemy.activeStatusEffects;
        return null;
    }
}