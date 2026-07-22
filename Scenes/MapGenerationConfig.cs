using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapConfig", menuName = "Levels/Map Generation Config")]
public class MapGenerationConfig : ScriptableObject
{
    [Header("Map Structure")]
    [Tooltip("Total number of floors/depths in the map")]
    public int totalDepth = 15;
    
    [Tooltip("Minimum number of nodes per floor")]
    public int minNodesPerFloor = 2;
    
    [Tooltip("Maximum number of nodes per floor")]
    public int maxNodesPerFloor = 4;

    [Header("Room Type Probabilities (Per Floor)")]
    [Range(0f, 1f)]
    [Tooltip("Chance of a room being a battle room")]
    public float battleRoomChance = 0.5f;
    
    [Range(0f, 1f)]
    [Tooltip("Chance of a room being an elite room")]
    public float eliteRoomChance = 0.15f;
    
    [Range(0f, 1f)]
    [Tooltip("Chance of a room being a treasure room")]
    public float treasureRoomChance = 0.2f;
    
    [Range(0f, 1f)]
    [Tooltip("Chance of a room being a shop")]
    public float shopRoomChance = 0.1f;
    
    [Range(0f, 1f)]
    [Tooltip("Chance of a room being a rest area")]
    public float restRoomChance = 0.05f;

    [Header("Special Floors")]
    [Tooltip("Floor indices where boss rooms must appear")]
    public List<int> bossFloors = new List<int> { 7, 15 };
    
    [Tooltip("Floor indices where treasure rooms are guaranteed")]
    public List<int> treasureFloors = new List<int> { 5, 10 };

    [Header("Enemy Configuration")]
    public AnimationCurve enemyDifficultyProgression;
    
    [Header("Loot Configuration")]
    public AnimationCurve lootQualityProgression;
}
