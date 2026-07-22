using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance { get; private set; }

    [Header("Loot Data")]
    [Tooltip("A master list of all possible items that can be offered as loot.")]
    public List<EquipmentItemSO> lootPool;

    [Header("Special Items")]
    [Tooltip("The Armoury item that allows dice face modification.")]
    public EquipmentItemSO armouryItem;
    [Range(0f, 1f)]
    [Tooltip("Chance (0-1) for the Armoury to appear in loot selection.")]
    public float armouryAppearanceChance = 0.15f;

    [Header("UI Reference")]
    public DiceRewardUI diceRewardUI;

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

        if (diceRewardUI == null)
        {
            diceRewardUI = FindFirstObjectByType<DiceRewardUI>(FindObjectsInactive.Include);
        }
    }

    public void PresentLootSelection()
    {
        if (diceRewardUI != null)
        {
            diceRewardUI.ShowRewardActions();
        }
        else
        {
            Debug.LogError("DiceRewardUI not found! Proceeding to level select.");
            LevelManager.Instance.OnVictory();
        }
    }

    public void GrantTreasureReward(LootQuality quality)
    {
        Debug.Log($"Granting treasure reward of quality: {quality}");

        if (diceRewardUI != null)
        {
            diceRewardUI.ShowRewardActions();
        }
        else
        {
            Debug.LogError("DiceRewardUI not found!");
            LevelManager.Instance.OnVictory();
        }
    }
}