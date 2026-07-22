using UnityEngine;
using System.Collections.Generic;

public class MagicIncantationHandler : MonoBehaviour
{
    public static MagicIncantationHandler Instance { get; private set; }

    [Header("Incantation Risk Settings")]
    [Tooltip("Chance for an invalid incantation to backfire and damage the player")]
    [Range(0, 1)]
    public float backfireChance = 0.5f;

    [Tooltip("Damage dealt to player on backfire")]
    public int backfireDamage = 25;

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

    public bool IsIncantation(DiceActionType actionType)
    {
        return actionType == DiceActionType.Zaa ||
               actionType == DiceActionType.Faa ||
               actionType == DiceActionType.Laa ||
               actionType == DiceActionType.Naa ||
               actionType == DiceActionType.Lee ||
               actionType == DiceActionType.Zoo;
    }

    public DiceActionDetails ProcessIncantationRisk(DiceActionType incantationType, List<Sprite> sprites)
    {
        float roll = Random.value;

        if (roll < backfireChance)
        {
            Debug.Log($"Invalid incantation {incantationType} backfired! Dealing {backfireDamage} damage to player.");
            
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.TakeDamage(backfireDamage);
            }

            if (BattleAnimator.Instance != null)
            {
                BattleAnimator.Instance.PlayStatusEffectAnimation($"Incantation backfired! Player takes {backfireDamage} damage!");
            }

            return new DiceActionDetails(
                DiceActionType.None,
                0,
                0,
                sprites,
                false,
                null,
                0
            );
        }

        return new DiceActionDetails(
            DiceActionType.None,
            0,
            0,
            sprites,
            false,
            null,
            0
        );
    }
}
