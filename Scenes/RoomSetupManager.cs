using UnityEngine;
using UnityEngine.Events;

public class RoomSetupManager : MonoBehaviour
{
    public static RoomSetupManager Instance { get; private set; }

    [Header("Events")]
    public UnityEvent<RoomNode> onRoomSetupStarted;
    public UnityEvent<RoomNode> onRoomSetupCompleted;

    [Header("Current Room Info")]
    private RoomNode currentRoom;

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

    public void SetupRoom(RoomNode room)
    {
        if (room == null)
        {
            Debug.LogError("Cannot setup null room!");
            return;
        }

        currentRoom = room;

        Debug.Log($"Setting up room: {room.roomName} (Type: {room.roomType}, Depth: {room.depth})");

        onRoomSetupStarted?.Invoke(room);

        switch (room.roomType)
        {
            case RoomType.Battle:
                SetupBattleRoom(room);
                break;
            case RoomType.Elite:
                SetupEliteRoom(room);
                break;
            case RoomType.Boss:
                SetupBossRoom(room);
                break;
            case RoomType.Treasure:
                SetupTreasureRoom(room);
                break;
            case RoomType.Shop:
                SetupShopRoom(room);
                break;
            case RoomType.Rest:
                SetupRestRoom(room);
                break;
            case RoomType.Event:
                SetupEventRoom(room);
                break;
        }

        onRoomSetupCompleted?.Invoke(room);

        Debug.Log($"Room setup complete: Enemy Difficulty = {room.enemyDifficulty}, Loot Quality = {room.lootQuality}");
    }

    private void SetupBattleRoom(RoomNode room)
    {
        Debug.Log($"Setting up Battle room with {room.enemyDifficulty} difficulty");
        
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.LoadEnemyByDifficulty(room.enemyDifficulty, room.depth);
        }

        InitializeBattle();
        Debug.Log("Battle room ready - waiting for player to start combat");
    }

    private void SetupEliteRoom(RoomNode room)
    {
        Debug.Log($"Setting up Elite room");
        
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.LoadEnemyByDifficulty(EnemyDifficulty.Elite, room.depth);
        }

        InitializeBattle();
        Debug.Log("Elite battle room ready");
    }

    private void SetupBossRoom(RoomNode room)
    {
        Debug.Log($"Setting up Boss room");
        
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.LoadEnemyByDifficulty(EnemyDifficulty.Boss, room.depth);
        }

        InitializeBattle();
        Debug.Log("Boss battle room ready");
    }

    private void InitializeBattle()
    {
        if (SlotMachine.Instance != null)
        {
            SlotMachine.Instance.spinButton.interactable = true;
            Debug.Log("Spin button enabled for new battle");
        }
    }

    private void SetupTreasureRoom(RoomNode room)
    {
        Debug.Log($"Setting up Treasure room with {room.lootQuality} quality loot");
        
        if (LootManager.Instance != null)
        {
            LootManager.Instance.GrantTreasureReward(room.lootQuality);
        }
    }

    private void SetupShopRoom(RoomNode room)
    {
        Debug.Log("Setting up Shop room");
        
    }

    private void SetupRestRoom(RoomNode room)
    {
        Debug.Log("Setting up Rest room");
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ShowRestOptions();
        }
    }

    private void SetupEventRoom(RoomNode room)
    {
        Debug.Log("Setting up Event room");
        
    }

    public RoomNode GetCurrentRoom()
    {
        return currentRoom;
    }
}
