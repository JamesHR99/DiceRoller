using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Levels/Level Definition")]
public class LevelSO : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    [TextArea]
    public string levelDescription;
    public Sprite levelIcon;

    [Header("Scene")]
    [Tooltip("The exact name of the scene to load for this level.")]
    public string sceneName;
}