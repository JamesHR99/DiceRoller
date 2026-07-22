using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MapUI : MonoBehaviour
{
    public static MapUI Instance { get; private set; }

    [Header("UI References")]
    public RectTransform mapContainer;
    public GameObject nodePrefab;

    [Header("Layout Settings - Left to Right")]
    public float horizontalSpacing = 250f;
    public float verticalSpacing = 120f;
    public float nodeSize = 100f;
    public float leftMargin = 100f;
    
    [Header("Text Settings")]
    public float fontSize = 16f;

    [Header("Colors")]
    public Color battleColor = new Color(0.8f, 0.2f, 0.2f);
    public Color eliteColor = new Color(1f, 0.5f, 0f);
    public Color bossColor = new Color(0.6f, 0f, 0.6f);
    public Color treasureColor = new Color(1f, 0.9f, 0.2f);
    public Color shopColor = new Color(0.2f, 0.8f, 0.2f);
    public Color restColor = new Color(0.2f, 0.8f, 0.9f);
    public Color eventColor = new Color(0.3f, 0.3f, 0.9f);
    public Color completedColor = new Color(0.4f, 0.4f, 0.4f);
    public Color currentRoomColor = Color.white;
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
    public Color notTakenColor = new Color(0.2f, 0.2f, 0.25f);

    private Dictionary<int, GameObject> nodeObjects = new Dictionary<int, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (mapContainer == null)
        {
            mapContainer = GetComponent<RectTransform>();
        }

        gameObject.SetActive(false);
    }

    public void ShowMap()
    {
        if (MapGenerator.Instance == null)
        {
            Debug.LogError("MapGenerator not found!");
            return;
        }

        ClearMap();
        
        List<RoomNode> visibleNodes = GetVisibleNodes();
        DrawMap(visibleNodes);
        
        gameObject.SetActive(true);
        
        Debug.Log($"Map UI shown with {visibleNodes.Count} visible nodes");
    }

    private List<RoomNode> GetVisibleNodes()
    {
        return MapGenerator.Instance.allNodes;
    }

    public void CloseMap()
    {
        gameObject.SetActive(false);
    }

    private void DrawMap(List<RoomNode> nodes)
    {
        if (nodes.Count == 0) return;

        RoomNode currentRoom = MapGenerator.Instance.currentNode;
        int currentDepth = currentRoom != null ? currentRoom.depth : 0;

        var nodesByDepth = nodes.GroupBy(n => n.depth).OrderBy(g => g.Key);

        int columnIndex = 0;
        foreach (var depthGroup in nodesByDepth)
        {
            int actualDepth = depthGroup.Key;
            var nodesAtDepth = depthGroup.ToList();

            float xPosition = leftMargin + (columnIndex * horizontalSpacing);

            int totalNodes = nodesAtDepth.Count();
            float totalHeight = (totalNodes - 1) * verticalSpacing;
            float startY = totalHeight / 2f;

            for (int i = 0; i < totalNodes; i++)
            {
                RoomNode node = nodesAtDepth[i];
                float yPosition = startY - (i * verticalSpacing);

                GameObject nodeObj = CreateNodeObject(node);
                nodeObjects[node.nodeId] = nodeObj;

                RectTransform rect = nodeObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(xPosition, yPosition);
                }
            }
            
            columnIndex++;
        }
        
        Debug.Log($"Drew {nodes.Count} nodes across {columnIndex} columns");
    }

    private GameObject CreateNodeObject(RoomNode node)
    {
        GameObject nodeObj;

        if (nodePrefab != null)
        {
            nodeObj = Instantiate(nodePrefab, mapContainer);
            
            Button btn = nodeObj.GetComponent<Button>();
            if (btn != null)
            {
                SetupNodeButton(btn, node);
            }

            Image img = nodeObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = GetNodeColor(node);
            }

            TextMeshProUGUI text = nodeObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = GetNodeText(node);
                text.fontSize = fontSize;
            }
        }
        else
        {
            nodeObj = new GameObject($"Node_{node.nodeId}_{node.roomType}");
            nodeObj.transform.SetParent(mapContainer, false);

            RectTransform rect = nodeObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(nodeSize, nodeSize);

            Image img = nodeObj.AddComponent<Image>();
            img.color = GetNodeColor(node);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(nodeObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = GetNodeText(node);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;

            Button btn = nodeObj.AddComponent<Button>();
            SetupNodeButton(btn, node);
        }

        return nodeObj;
    }

    private string GetNodeText(RoomNode node)
    {
        string roomName = node.roomType.ToString();
        
        if (node.isCurrentRoom)
        {
            return $"{roomName}\n★";
        }
        else if (node.isCompleted)
        {
            return $"{roomName}\n✓";
        }
        else if (node.isNotTaken)
        {
            return $"{roomName}\n✗";
        }

        return roomName;
    }

    private void SetupNodeButton(Button nodeButton, RoomNode node)
    {
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(() => OnNodeClicked(node));
        nodeButton.interactable = node.isAccessible && !node.isCompleted;
        
        if (!node.isAccessible || node.isCompleted)
        {
            nodeButton.interactable = false;
        }
    }

    private void OnNodeClicked(RoomNode node)
    {
        Debug.Log($"Clicked on room: {node.roomType} at depth {node.depth}");
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SelectRoom(node);
        }
    }

    private Color GetNodeColor(RoomNode node)
    {
        if (node.isCurrentRoom) return currentRoomColor;
        if (node.isCompleted) return completedColor;
        if (node.isNotTaken) return notTakenColor;
        if (!node.isAccessible) return lockedColor;

        return node.roomType switch
        {
            RoomType.Battle => battleColor,
            RoomType.Elite => eliteColor,
            RoomType.Boss => bossColor,
            RoomType.Treasure => treasureColor,
            RoomType.Shop => shopColor,
            RoomType.Rest => restColor,
            RoomType.Event => eventColor,
            _ => Color.white
        };
    }

    private void ClearMap()
    {
        foreach (var nodeObj in nodeObjects.Values)
        {
            if (nodeObj != null)
            {
                Destroy(nodeObj);
            }
        }
        nodeObjects.Clear();
    }
}