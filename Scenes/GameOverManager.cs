using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Restart Settings")]
    public string startSceneName = "SampleScene";

    void Start()
    {
        DestroyPersistentObjects();
    }

    public void RestartGame()
    {
        DestroyPersistentObjects();
        
        Time.timeScale = 1f;
        
        SceneManager.LoadScene(startSceneName);
    }

    private void DestroyPersistentObjects()
    {
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        
        if (PlayerManager.Instance != null)
        {
            Destroy(PlayerManager.Instance.gameObject);
        }
        
        if (EnemyManager.Instance != null)
        {
            Destroy(EnemyManager.Instance.gameObject);
        }
        
        if (BattleManager.Instance != null)
        {
            Destroy(BattleManager.Instance.gameObject);
        }
        
        if (SlotMachine.Instance != null)
        {
            Destroy(SlotMachine.Instance.gameObject);
        }
        
        if (EnemyTurnManager.Instance != null)
        {
            Destroy(EnemyTurnManager.Instance.gameObject);
        }
        
        if (LevelManager.Instance != null)
        {
            Destroy(LevelManager.Instance.gameObject);
        }
    }
}
