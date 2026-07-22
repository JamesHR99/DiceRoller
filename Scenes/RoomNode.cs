using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoomNode
{
    public int nodeId;
    public int depth;
    public RoomType roomType;
    public Vector2 position;
    public List<int> connectedNodeIds = new List<int>();
    
    public EnemyDifficulty enemyDifficulty;
    public LootQuality lootQuality;
    public string sceneName;
    public string roomName;
    public string roomDescription;
    public Sprite roomIcon;
    
    public bool isCompleted;
    public bool isAccessible;
    public bool isCurrentRoom;
    public bool isNotTaken;

    public RoomNode(int id, int depth, RoomType type, Vector2 pos)
    {
        this.nodeId = id;
        this.depth = depth;
        this.roomType = type;
        this.position = pos;
        this.isCompleted = false;
        this.isAccessible = false;
        this.isCurrentRoom = false;
        this.isNotTaken = false;
    }
}
