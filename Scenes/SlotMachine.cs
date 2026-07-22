using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SlotMachine : MonoBehaviour
{
    public static SlotMachine Instance { get; private set; }

    [Header("Dynamic Dice Setup")]
    public GameObject dicePrefab;
    public Transform playerDiceContainer;

    private List<Dice> activeDice = new List<Dice>();

    [Header("Enemy Dice References")]
    public EnemyTurnManager enemyTurnManager;

    [Header("Dice Configuration")]
    public DiceActionConfig[] allActionConfigs;

    [Header("Spin Settings")]
    public float spinSpeed = 0.1f;
    public float spinDuration = 2f;

    [Header("UI References")]
    public Button spinButton;
    public Button nextTurnButton;
    public TextMeshProUGUI scoreText;

    [Header("System References")]
    public BattleManager battleManager;
    public ComboManager comboManager;

    [Header("Dice Tuning")]
    [Range(0f, 1f)]
    public float globalCritChanceOverride = -1f;  // -1 means use the default from DiceDefinitionSO

    public float stopDelay = 0.3f;
    private int totalScore = 0;
    private Dictionary<DiceActionType, DiceActionConfig> _masterActionConfigMap;

    void Awake()
    {
        if (battleManager == null) battleManager = FindObjectOfType<BattleManager>();
        if (enemyTurnManager == null) enemyTurnManager = FindObjectOfType<EnemyTurnManager>();
        if (comboManager == null) comboManager = FindObjectOfType<ComboManager>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReacquireSceneReferences();
        ClearAllDice();
    }

    private void ReacquireSceneReferences()
    {
        if (playerDiceContainer == null)
        {
            var allTransforms = FindObjectsOfType<Transform>();
            playerDiceContainer = allTransforms.FirstOrDefault(t => t.name == "PlayerDiceContainer");
            if (playerDiceContainer != null)
            {
                Debug.Log("Found PlayerDiceContainer in new scene.");
            }
            else
            {
                Debug.LogWarning("PlayerDiceContainer not found in scene!");
            }
        }

        if (spinButton == null || nextTurnButton == null)
        {
            var buttons = FindObjectsOfType<Button>();
            if (spinButton == null)
            {
                spinButton = buttons.FirstOrDefault(b => b.name == "SpinButton");
                if (spinButton != null)
                {
                    spinButton.onClick.RemoveAllListeners();
                    spinButton.onClick.AddListener(() => StartSpin());
                    Debug.Log("Reconnected SpinButton.");
                }
            }
            if (nextTurnButton == null)
            {
                nextTurnButton = buttons.FirstOrDefault(b => b.name == "NextTurnButton");
                if (nextTurnButton != null)
                {
                    nextTurnButton.onClick.RemoveAllListeners();
                    nextTurnButton.onClick.AddListener(OnNextTurnButtonClicked);
                    Debug.Log("Reconnected NextTurnButton.");
                }
            }
        }

        if (scoreText == null)
        {
            var allTexts = FindObjectsOfType<TextMeshProUGUI>();
            scoreText = allTexts.FirstOrDefault(t => t.name == "ScoreText");
        }
    }

    private void ClearAllDice()
    {
        foreach (Dice die in activeDice)
        {
            if (die != null) Destroy(die.gameObject);
        }
        activeDice.Clear();
        totalScore = 0;
        UpdateScoreUI();
    }

    void Start()
    {
        _masterActionConfigMap = new Dictionary<DiceActionType, DiceActionConfig>();
        foreach (var config in allActionConfigs)
        {
            if (!_masterActionConfigMap.ContainsKey(config.actionType))
                _masterActionConfigMap.Add(config.actionType, config);
        }

        if (spinButton != null) spinButton.onClick.AddListener(() => StartSpin());
        if (nextTurnButton != null) { nextTurnButton.onClick.AddListener(OnNextTurnButtonClicked); nextTurnButton.interactable = false; }

        if (enemyTurnManager != null)
        {
            enemyTurnManager.Initialize(_masterActionConfigMap);
        }
        UpdateScoreUI();
    }

    public void StartSpin(bool isReroll = false)
    {
        if (PlayerEnergy.Instance != null && !isReroll)
        {
            PlayerEnergy.Instance.AddEnergyForNewTurn();
            Debug.Log("Energy updated at start of turn before dice roll");
        }

        if (!isReroll)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.ResetShield();
                PlayerManager.Instance.ResetDodgeChance();
            }

            ConfigurePlayerDice();
        }
        if (SelectedDiceUI.Instance != null)
        {
            SelectedDiceUI.Instance.ClearAllDice();
            SelectedDiceUI.Instance.Show();
        }

        spinButton.interactable = false;
        nextTurnButton.interactable = false;

        if (RerollManager.Instance != null)
        {
            RerollManager.Instance.OnDiceStartRolling();
        }

        foreach (Dice die in activeDice)
        {
            if (die != null && !die.isHeld && !die.isRerollHeld && die.gameObject.activeInHierarchy)
            {
                die.StartRolling();
            }
        }

        StartCoroutine(StopDiceRoutine(isReroll));
    }

    IEnumerator StopDiceRoutine(bool isReroll = false)
    {
        yield return new WaitForSeconds(spinDuration);

        foreach (Dice die in activeDice.OrderBy(d => Random.value))
        {
            if (die != null && !die.isHeld && !die.isRerollHeld && die.gameObject.activeInHierarchy)
            {
                die.StopRolling();
            }
            yield return new WaitForSeconds(stopDelay);
        }

        yield return new WaitForSeconds(0.5f);

        if (RerollManager.Instance != null)
        {
            RerollManager.Instance.OnDiceStopRolling();
        }

        CalculateScore();
        UpdateScoreUI();
        
        // Only re-roll enemy dice on a fresh turn spin, not on a player reroll
        if (!isReroll && enemyTurnManager != null)
        {
            enemyTurnManager.RollEnemyDiceForPreview();
        }
        
        nextTurnButton.interactable = true;
        spinButton.interactable = false;
    }

    private void ConfigurePlayerDice()
    {
        List<DiceDefinitionSO> equippedDiceDefs = PlayerEquipment.Instance.GetEquippedDice();

        // Destroy only the dice that are not reroll-held, keeping held ones alive.
        List<Dice> snapshot = new List<Dice>(activeDice);
        for (int i = 0; i < snapshot.Count; i++)
        {
            if (snapshot[i] != null && !snapshot[i].isRerollHeld)
                Destroy(snapshot[i].gameObject);
        }
        activeDice.Clear();

        for (int i = 0; i < equippedDiceDefs.Count; i++)
        {
            // Re-use the held die that was sitting in this slot.
            if (i < snapshot.Count && snapshot[i] != null && snapshot[i].isRerollHeld)
            {
                snapshot[i].gameObject.SetActive(true);
                activeDice.Add(snapshot[i]);
                continue;
            }

            if (dicePrefab == null || playerDiceContainer == null)
            {
                Debug.LogError("Dice Prefab or Dice Container not set in SlotMachine!");
                return;
            }

            GameObject diceGO = Instantiate(dicePrefab, playerDiceContainer);
            Dice newDie = diceGO.GetComponent<Dice>();
            if (newDie != null)
            {
                float? critOverride = (globalCritChanceOverride >= 0f && globalCritChanceOverride <= 1f)
                    ? globalCritChanceOverride
                    : (float?)null;

                newDie.ConfigureDie(equippedDiceDefs[i], _masterActionConfigMap, critOverride);
                newDie.OnScoreUpdated += UpdateScore;
                activeDice.Add(newDie);
            }
        }

        // Enforce visual order — held dice may have kept their old sibling index
        // while new dice were appended at the end. Reorder to match slot positions.
        for (int i = 0; i < activeDice.Count; i++)
        {
            if (activeDice[i] != null)
                activeDice[i].transform.SetSiblingIndex(i);
        }
    }

    public void OnNextTurnButtonClicked()
    {
        if (SelectedDiceUI.Instance != null)
        {
            SelectedDiceUI.Instance.ClearAllDice();
            SelectedDiceUI.Instance.Hide();
        }

        List<DiceActionDetails> playerAttackActions = new List<DiceActionDetails>();
        List<DiceActionDetails> playerImmediateActions = new List<DiceActionDetails>();

        List<Dice> heldDice = activeDice.Where(d => d != null && d.isHeld).ToList();

        if (comboManager != null && heldDice.Count > 0)
        {
            var comboResult = comboManager.FindAndProcessCombos(heldDice);
            foreach (var comboAction in comboResult.comboActions)
            {
                if (comboAction.Type == DiceActionType.Attack || 
                    comboAction.Type == DiceActionType.SwiftStrike ||
                    comboAction.Type == DiceActionType.HeavyAttack ||
                    comboAction.Type == DiceActionType.LightAttack)
                    playerAttackActions.Add(comboAction);
                else
                    playerImmediateActions.Add(comboAction);
            }
            heldDice = comboResult.remainingDice;
        }

        foreach (Dice die in heldDice)
        {
            DiceActionDetails details = die.GetActionDetails();
            
            if (MagicIncantationHandler.Instance != null && MagicIncantationHandler.Instance.IsIncantation(details.Type))
            {
                MagicIncantationHandler.Instance.ProcessIncantationRisk(details.Type, details.ContributingDiceSprites);
                continue;
            }
            
            int actionAgility = PlayerManager.Instance != null 
                ? PlayerManager.Instance.CalculateAgilityForAction(details.Type) 
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
            
            if (details.Type == DiceActionType.Attack || 
                details.Type == DiceActionType.SwiftStrike || 
                details.Type == DiceActionType.HeavyAttack || 
                details.Type == DiceActionType.LightAttack ||
                details.Type == DiceActionType.Shot_Level1 ||
                details.Type == DiceActionType.Shot_Level2 ||
                details.Type == DiceActionType.Shot_Level3)
                playerAttackActions.Add(detailsWithAgility);
            else
                playerImmediateActions.Add(detailsWithAgility);
        }

        // Hide dice — reroll-held dice are reactivated on the next spin.
        foreach (Dice die in activeDice)
        {
            if (die != null)
            {
                if (die.isHeld)
                    die.SetHeldState(false);

                die.gameObject.SetActive(false);
            }
        }

        StartCoroutine(ProcessPlayerImmediateActionsCoroutine(playerImmediateActions, playerAttackActions));
    }

    private IEnumerator ProcessPlayerImmediateActionsCoroutine(List<DiceActionDetails> playerImmediateActions, List<DiceActionDetails> playerAttackActions)
    {
        yield return StartCoroutine(ProcessPlayerImmediateActionsWithDelay(playerImmediateActions));

        nextTurnButton.interactable = false;
        spinButton.interactable = false;
        
        if (RerollManager.Instance != null)
        {
            RerollManager.Instance.OnTurnStart();
        }

        if (enemyTurnManager != null)
        {
            StartCoroutine(enemyTurnManager.TakeEnemyTurn(playerAttackActions));
        }
    }

    private IEnumerator ProcessPlayerImmediateActionsWithDelay(List<DiceActionDetails> actionsToProcess)
    {
        int totalDodge = 0;
        int totalHeal = 0;
        int totalBlock = 0;
        int totalShield = 0;
        int healCritBonus = 0;
        int blockCritBonus = 0;
        int rechargeAmount = 0;
        int rechargeCritBonus = 0;
        List<Sprite> healSprites = new List<Sprite>();
        List<Sprite> blockSprites = new List<Sprite>();
        List<Sprite> shieldSprites = new List<Sprite>();
        List<Sprite> rechargeSprites = new List<Sprite>();

        foreach (var details in actionsToProcess)
        {
            switch (details.Type)
            {
                case DiceActionType.Defend:
                    if (details.Score == 100)
                    {
                        int blockPct = 100 + (details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0);
                        PlayerManager.Instance?.AddBlockWithSprites(blockPct, details.ContributingDiceSprites);
                        totalBlock += 100;
                        if (details.IsCritical) blockCritBonus += Mathf.RoundToInt(details.CritBonusValue);
                        if (details.ContributingDiceSprites != null)
                            blockSprites.AddRange(details.ContributingDiceSprites);
                    }
                    else
                    {
                        PlayerManager.Instance?.AddShield(details.Score);
                        totalShield += details.Score;
                        if (details.ContributingDiceSprites != null)
                            shieldSprites.AddRange(details.ContributingDiceSprites);
                    }
                    break;

                case DiceActionType.Block:
                    int critBlockBonus = details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0;
                    int pct = 15 + critBlockBonus;
                    PlayerManager.Instance?.AddBlockWithSprites(pct, details.ContributingDiceSprites);
                    totalBlock += 15;
                    if (details.IsCritical) blockCritBonus += critBlockBonus;
                    if (details.ContributingDiceSprites != null)
                        blockSprites.AddRange(details.ContributingDiceSprites);
                    break;

                case DiceActionType.Heal:
                    int healAmount = details.Score;
                    int critHealExtra = 0;
                    if (details.IsCritical)
                    {
                        critHealExtra = Mathf.RoundToInt(details.Score * details.CritBonusValue);
                        healAmount += critHealExtra;
                        healCritBonus += critHealExtra;
                    }
                    PlayerManager.Instance?.Heal(healAmount);
                    totalHeal += details.Score;
                    if (details.ContributingDiceSprites != null)
                        healSprites.AddRange(details.ContributingDiceSprites);
                    break;

                case DiceActionType.Gold:
                    GameManager.Instance?.AddGold(details.Score);
                    break;

                case DiceActionType.Recharge:
                case DiceActionType.RegainStamina:
                    int critRechargeBonus = details.IsCritical ? Mathf.RoundToInt(details.CritBonusValue) : 0;
                    int energy = 2 + critRechargeBonus;
                    PlayerEnergy.Instance?.AddTemporaryEnergy(energy);
                    rechargeAmount += 2;
                    if (details.IsCritical) rechargeCritBonus += critRechargeBonus;
                    if (details.ContributingDiceSprites != null)
                        rechargeSprites.AddRange(details.ContributingDiceSprites);
                    break;

                case DiceActionType.Dodge:
                    totalDodge += details.Score;
                    break;
            }
        }

        if (totalHeal > 0 && BattleAnimator.Instance != null)
        {
            int totalHealWithCrit = totalHeal + healCritBonus;
            string healText = healCritBonus > 0
                ? $"Player Healed +{totalHeal} HP <color=#FFD700>CRITICAL! +{healCritBonus} bonus!</color> ({totalHealWithCrit} total)"
                : $"Player Healed +{totalHeal} HP";
            BattleAnimator.Instance.PlayHealAnimation(healText, true, healSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (totalBlock > 0 && BattleAnimator.Instance != null)
        {
            int totalBlockWithCrit = totalBlock + blockCritBonus;
            string blockText;
            if (blockCritBonus > 0)
                blockText = totalBlockWithCrit >= 100
                    ? $"Player Block: {totalBlock}% <color=#FFD700>CRITICAL! +{blockCritBonus}%!</color> (100% total)"
                    : $"Player Block: {totalBlock}% <color=#FFD700>CRITICAL! +{blockCritBonus}%!</color> ({totalBlockWithCrit}% total)";
            else
                blockText = totalBlock >= 100 ? "Player Block: 100% (Full Block!)" : $"Player Block: {totalBlock}%";
            BattleAnimator.Instance.PlayBlockAnimation(blockText, true, blockSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (totalShield > 0 && BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.PlayBlockAnimation($"Player Shield +{totalShield}", true, shieldSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (rechargeAmount > 0 && BattleAnimator.Instance != null)
        {
            string rechargeText = rechargeCritBonus > 0
                ? $"Player Recharging +{rechargeAmount} energy <color=#FFD700>CRITICAL! +{rechargeCritBonus} bonus!</color> ({rechargeAmount + rechargeCritBonus} total)"
                : $"Player Recharging +{rechargeAmount} energy next turn";
            BattleAnimator.Instance.PlayHealAnimation(rechargeText, true, rechargeSprites);
            yield return new WaitForSeconds(1.5f);
        }

        if (totalDodge > 0 && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddDodgeChance(totalDodge);
        }
    }

    void CalculateScore()
    {
        totalScore = 0;
        foreach (Dice die in activeDice)
        {
            if (die != null && die.isHeld)
            {
                totalScore += die.score;
            }
        }
    }

    void UpdateScore()
    {
        CalculateScore();
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }
    }
}
