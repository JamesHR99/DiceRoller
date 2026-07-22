using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Legacy Level Data (Deprecated)")]
    [Tooltip("Old system - use MapGenerator instead")]
    public List<LevelSO> availableLevels;

    [Header("UI Reference")]
    [Tooltip("Drag the 'LevelSelectionPanel' from your scene here.")]
    public LevelSelectionUI levelSelectionUI;

    [Header("Map System")]
    public bool useNewMapSystem = true;
    
    private RoomNode pendingRoom;

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

        if (levelSelectionUI == null)
        {
            levelSelectionUI = FindFirstObjectByType<LevelSelectionUI>(FindObjectsInactive.Include);
        }
    }

    public void OnVictory()
    {
        if (useNewMapSystem && MapGenerator.Instance != null)
        {
            MapGenerator.Instance.CompleteCurrentRoom();
            
            List<RoomNode> nextRooms = MapGenerator.Instance.GetNextAvailableRooms();

            if (nextRooms.Count > 0)
            {
                ShowMapSelection(nextRooms);
            }
            else
            {
                Debug.Log("No more rooms available - Map complete!");
            }
        }
        else
        {
            UseLegacyLevelSelection();
        }
    }

    private void ShowMapSelection(List<RoomNode> rooms)
    {
        if (MapUI.Instance != null)
        {
            MapUI.Instance.ShowMap();
            Debug.Log("Pausing game for map selection.");
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogWarning("MapUI not found! Falling back to legacy system.");
            UseLegacyLevelSelection();
        }
    }

    private void UseLegacyLevelSelection()
    {
        List<LevelSO> levelChoices = availableLevels
            .OrderBy(x => Random.value)
            .Take(3)
            .ToList();

        if (levelChoices.Count > 0 && levelSelectionUI != null)
        {
            levelSelectionUI.Show(levelChoices);
        }
        else
        {
            Debug.LogError("Not enough levels defined in LevelManager or LevelSelectionUI not found!");
        }
    }

    public void SelectRoom(RoomNode room)
    {
        if (room == null || !room.isAccessible)
        {
            Debug.LogWarning("Cannot select this room!");
            return;
        }

        pendingRoom = room;
        
        if (MapGenerator.Instance != null)
        {
            MapGenerator.Instance.MoveToNode(pendingRoom);
        }

        PrepareForNewRoom();
        SetupNewRoom(room);
    }

    public void LoadLevel(string sceneName)
    {
        Debug.Log("Resuming game time before loading new level.");
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty! Cannot load level.");
            return;
        }

        StopAllBattleActivity();

        if (levelSelectionUI != null)
        {
            levelSelectionUI.gameObject.SetActive(false);
        }

        Debug.Log($"Loading level: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void PrepareForNewRoom()
    {
        Debug.Log("Resuming game time and preparing for new room.");
        Time.timeScale = 1f;

        StopAllBattleActivity();

        if (levelSelectionUI != null)
        {
            levelSelectionUI.gameObject.SetActive(false);
        }

        if (MapUI.Instance != null)
        {
            MapUI.Instance.CloseMap();
        }

        if (LootSelectionUI.Instance != null)
        {
            LootSelectionUI.Instance.Hide();
        }
    }

    private void SetupNewRoom(RoomNode room)
    {
        if (RoomSetupManager.Instance != null)
        {
            RoomSetupManager.Instance.SetupRoom(room);
        }
        else
        {
            Debug.LogError("RoomSetupManager not found! Cannot setup room.");
        }
    }

    private void StopAllBattleActivity()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StopAllCoroutines();
            BattleManager.Instance.EndBattle();
        }

        if (BattleAnimator.Instance != null)
        {
            BattleAnimator.Instance.StopAllAnimations();
        }

        if (SlotMachine.Instance != null)
        {
            SlotMachine.Instance.StopAllCoroutines();
        }

        if (EnemyTurnManager.Instance != null)
        {
            EnemyTurnManager.Instance.StopAllCoroutines();
        }
    }

    public RoomNode GetCurrentRoom()
    {
        if (MapGenerator.Instance != null)
        {
            return MapGenerator.Instance.currentNode;
        }
        return null;
    }
}