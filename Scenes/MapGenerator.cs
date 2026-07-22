using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("Configuration")]
    public MapGenerationConfig config;

    [Header("Current Map State")]
    public List<RoomNode> allNodes = new List<RoomNode>();
    public RoomNode currentNode;
    public int currentDepth = 0;

    private int nodeIdCounter = 0;

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

    void Start()
    {
        if (config == null)
        {
            Debug.LogError("MapGenerationConfig not assigned!");
            return;
        }

        GenerateNewMap();
    }

    public void GenerateNewMap()
    {
        allNodes.Clear();
        nodeIdCounter = 0;
        currentDepth = 0;

        GenerateMapStructure();
        SetupConnections();
        ConfigureRooms();
        
        if (allNodes.Count > 0)
        {
            currentNode = allNodes[0];
            currentNode.isCurrentRoom = true;
            currentNode.isAccessible = true;
            currentNode.isCompleted = false;
        }

        Debug.Log($"Generated map with {allNodes.Count} rooms across {config.totalDepth} floors.");
    }

    private void GenerateMapStructure()
    {
        for (int depth = 0; depth < config.totalDepth; depth++)
        {
            int nodesAtThisDepth;
            
            if (depth == 0)
            {
                nodesAtThisDepth = 1;
            }
            else if (depth == 1)
            {
                nodesAtThisDepth = 2;
            }
            else if (config.bossFloors.Contains(depth))
            {
                nodesAtThisDepth = 1;
            }
            else
            {
                nodesAtThisDepth = Random.Range(config.minNodesPerFloor, config.maxNodesPerFloor + 1);
            }

            int treasureNodeIndex = -1;
            if (config.treasureFloors.Contains(depth) && nodesAtThisDepth > 1)
            {
                treasureNodeIndex = Random.Range(0, nodesAtThisDepth);
            }

            float spacing = 2f / (nodesAtThisDepth + 1);
            
            for (int i = 0; i < nodesAtThisDepth; i++)
            {
                float xPos = -1f + spacing * (i + 1);
                float yPos = depth * 1.5f;
                
                xPos += Random.Range(-0.1f, 0.1f);

                RoomType roomType = DetermineRoomType(depth, nodesAtThisDepth, i, treasureNodeIndex);
                RoomNode node = new RoomNode(nodeIdCounter++, depth, roomType, new Vector2(xPos, yPos));
                
                allNodes.Add(node);
            }
        }
    }

    private RoomType DetermineRoomType(int depth, int nodesAtThisDepth, int nodeIndex, int treasureNodeIndex)
    {
        if (config.bossFloors.Contains(depth))
        {
            return RoomType.Boss;
        }

        if (config.treasureFloors.Contains(depth) && nodeIndex == treasureNodeIndex)
        {
            return RoomType.Treasure;
        }

        float roll = Random.value;
        float cumulative = 0f;

        cumulative += config.battleRoomChance;
        if (roll < cumulative) return RoomType.Battle;

        cumulative += config.eliteRoomChance;
        if (roll < cumulative) return RoomType.Elite;

        cumulative += config.treasureRoomChance;
        if (roll < cumulative) return RoomType.Treasure;

        cumulative += config.shopRoomChance;
        if (roll < cumulative) return RoomType.Shop;

        cumulative += config.restRoomChance;
        if (roll < cumulative) return RoomType.Rest;

        return RoomType.Battle;
    }

    private void SetupConnections()
    {
        var nodesByDepth = allNodes.GroupBy(n => n.depth).OrderBy(g => g.Key).ToList();

        for (int i = 0; i < nodesByDepth.Count - 1; i++)
        {
            var currentFloor = nodesByDepth[i].ToList();
            var nextFloor = nodesByDepth[i + 1].ToList();

            foreach (var node in currentFloor)
            {
                int connectionsToMake = Random.Range(1, Mathf.Min(3, nextFloor.Count + 1));
                
                var sortedByDistance = nextFloor
                    .OrderBy(n => Vector2.Distance(node.position, n.position))
                    .Take(connectionsToMake)
                    .ToList();

                foreach (var targetNode in sortedByDistance)
                {
                    if (!node.connectedNodeIds.Contains(targetNode.nodeId))
                    {
                        node.connectedNodeIds.Add(targetNode.nodeId);
                    }
                }
            }
        }
    }

    private void ConfigureRooms()
    {
        foreach (var node in allNodes)
        {
            float progressRatio = (float)node.depth / config.totalDepth;
            
            if (node.roomType == RoomType.Battle || node.roomType == RoomType.Elite || node.roomType == RoomType.Boss)
            {
                node.enemyDifficulty = GetEnemyDifficulty(progressRatio, node.roomType);
            }

            node.lootQuality = GetLootQuality(progressRatio, node.roomType);
            
            node.roomName = GetRoomName(node);
            node.roomDescription = GetRoomDescription(node);
        }
    }

    private EnemyDifficulty GetEnemyDifficulty(float progressRatio, RoomType roomType)
    {
        if (roomType == RoomType.Boss) return EnemyDifficulty.Boss;
        if (roomType == RoomType.Elite) return EnemyDifficulty.Elite;

        float difficultyValue = config.enemyDifficultyProgression != null 
            ? config.enemyDifficultyProgression.Evaluate(progressRatio) 
            : progressRatio;

        if (difficultyValue < 0.33f) return EnemyDifficulty.Easy;
        if (difficultyValue < 0.66f) return EnemyDifficulty.Medium;
        return EnemyDifficulty.Hard;
    }

    private LootQuality GetLootQuality(float progressRatio, RoomType roomType)
    {
        if (roomType == RoomType.Boss) return LootQuality.Epic;
        if (roomType == RoomType.Elite) return LootQuality.Rare;
        if (roomType == RoomType.Treasure) return LootQuality.Rare;

        float qualityValue = config.lootQualityProgression != null 
            ? config.lootQualityProgression.Evaluate(progressRatio) 
            : progressRatio;

        if (qualityValue < 0.4f) return LootQuality.Common;
        if (qualityValue < 0.7f) return LootQuality.Uncommon;
        if (qualityValue < 0.9f) return LootQuality.Rare;
        return LootQuality.Epic;
    }

    private string GetRoomName(RoomNode node)
    {
        return node.roomType switch
        {
            RoomType.Battle => $"Battle - Floor {node.depth + 1}",
            RoomType.Elite => $"Elite Battle - Floor {node.depth + 1}",
            RoomType.Boss => $"Boss - Floor {node.depth + 1}",
            RoomType.Treasure => $"Treasure Room - Floor {node.depth + 1}",
            RoomType.Shop => $"Shop - Floor {node.depth + 1}",
            RoomType.Rest => $"Rest Area - Floor {node.depth + 1}",
            _ => $"Room - Floor {node.depth + 1}"
        };
    }

    private string GetRoomDescription(RoomNode node)
    {
        return node.roomType switch
        {
            RoomType.Battle => $"Face enemies ({node.enemyDifficulty}). Rewards: {node.lootQuality} loot.",
            RoomType.Elite => $"Challenging elite battle! Rewards: {node.lootQuality} loot.",
            RoomType.Boss => "Epic boss battle awaits!",
            RoomType.Treasure => "Claim your rewards!",
            RoomType.Shop => "Purchase equipment and items.",
            RoomType.Rest => "Restore health and prepare.",
            _ => "Unknown room type."
        };
    }

    public void CompleteCurrentRoom()
    {
        if (currentNode != null)
        {
            currentNode.isCompleted = true;
            currentNode.isCurrentRoom = false;

            foreach (int connectedId in currentNode.connectedNodeIds)
            {
                RoomNode connectedNode = allNodes.FirstOrDefault(n => n.nodeId == connectedId);
                if (connectedNode != null)
                {
                    connectedNode.isAccessible = true;
                    Debug.Log($"Unlocked room: {connectedNode.roomName} at depth {connectedNode.depth}");
                }
            }
            
            Debug.Log($"Completed room at depth {currentNode.depth}. Unlocked {currentNode.connectedNodeIds.Count} rooms.");
        }
    }

    public void MoveToNode(RoomNode node)
    {
        if (node.isAccessible)
        {
            if (currentNode != null)
            {
                currentNode.isCurrentRoom = false;
                
                List<RoomNode> availableRooms = GetNextAvailableRooms();
                foreach (var availableRoom in availableRooms)
                {
                    if (availableRoom.nodeId != node.nodeId)
                    {
                        availableRoom.isNotTaken = true;
                        Debug.Log($"Marked {availableRoom.roomName} as not taken");
                    }
                }
            }

            currentNode = node;
            currentNode.isCurrentRoom = true;
            currentDepth = node.depth;

            Debug.Log($"Moving to: {node.roomName}");
        }
        else
        {
            Debug.LogWarning("Attempted to move to inaccessible room!");
        }
    }

    public List<RoomNode> GetNextAvailableRooms()
    {
        if (currentNode == null) return new List<RoomNode>();

        List<RoomNode> nextRooms = new List<RoomNode>();
        foreach (int connectedId in currentNode.connectedNodeIds)
        {
            RoomNode node = allNodes.FirstOrDefault(n => n.nodeId == connectedId);
            if (node != null && node.isAccessible)
            {
                nextRooms.Add(node);
            }
        }

        return nextRooms;
    }
}
