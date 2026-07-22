using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int gold = 0;
    public TextMeshProUGUI goldText;

    [Header("Game Over Settings")]
    public string gameOverSceneName = "GameOver";

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); }
        UpdateGoldUI();
    }

    private void OnEnable()
    {
        EnemyManager.OnEnemyDied += HandleVictory;
        PlayerManager.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        EnemyManager.OnEnemyDied -= HandleVictory;
        PlayerManager.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandleVictory()
    {
        Debug.Log("GameManager received VICTORY signal!");

        int goldEarned = 0;
        if (EnemyManager.Instance != null)
        {
            goldEarned = EnemyManager.Instance.GetGoldReward();
            AddGold(goldEarned);
            Debug.Log($"Enemy defeated! Earned {goldEarned} gold. Total gold: {gold}");
        }

        BattleManager.Instance.EndBattle();

        if (BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.StopAllAnimations();
        }

        SlotMachine slotMachine = FindFirstObjectByType<SlotMachine>();
        if (slotMachine != null)
        {
            slotMachine.spinButton.interactable = false;
            slotMachine.nextTurnButton.interactable = false;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnVictory();
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("GameManager received PLAYER DEATH signal!");

        BattleManager.Instance.EndBattle();

        if (BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.StopAllAnimations();
        }

        SlotMachine slotMachine = FindFirstObjectByType<SlotMachine>();
        if (slotMachine != null)
        {
            slotMachine.spinButton.interactable = false;
            slotMachine.nextTurnButton.interactable = false;
        }

        Time.timeScale = 1f;

        Debug.Log($"Loading Game Over scene: {gameOverSceneName}");
        SceneManager.LoadScene(gameOverSceneName);
    }

    public void ProcessStartOfTurnEffects()
    {
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.ProcessTurnEffects();
        }
    }

    // (Rest of the script is unchanged)
    public void AddGold(int amount) { if (amount > 0) { gold += amount; UpdateGoldUI(); } }
    public bool SpendGold(int amount) { if (gold >= amount) { gold -= amount; UpdateGoldUI(); return true; } return false; }
    private void UpdateGoldUI() { if (goldText != null) { goldText.text = $"Gold: {gold}"; } }
}