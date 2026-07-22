using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ArmouryUI : MonoBehaviour
{
    public static ArmouryUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject armouryPanel;
    public Transform diceListContainer;
    public GameObject diceButtonPrefab;
    
    [Header("Dice Face Selection")]
    public Transform faceSelectionContainer;
    public GameObject faceButtonPrefab;
    
    [Header("Headers")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI instructionText;
    
    [Header("Font Sizes")]
    public float equipmentHeaderFontSize = 18f;
    public float diceButtonFontSize = 14f;
    public float faceButtonFontSize = 14f;
    public float actionTypeButtonFontSize = 14f;
    
    private DiceDefinitionSO selectedDice;
    private EquipmentItemSO selectedEquipmentItem;
    private int selectedDiceIndexInItem = -1;
    private int selectedFaceIndex = -1;
    private Dictionary<DiceActionType, DiceActionConfig> actionConfigMap;
    
    private enum UIState
    {
        SelectingDice,
        SelectingFace,
        SelectingActionType
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeActionConfigs();
    }

    private void InitializeActionConfigs()
    {
        actionConfigMap = new Dictionary<DiceActionType, DiceActionConfig>();
        
        if (SlotMachine.Instance != null && SlotMachine.Instance.allActionConfigs != null)
        {
            foreach (var config in SlotMachine.Instance.allActionConfigs)
            {
                if (!actionConfigMap.ContainsKey(config.actionType))
                {
                    actionConfigMap.Add(config.actionType, config);
                }
            }
            Debug.Log($"ArmouryUI loaded {actionConfigMap.Count} action configs from SlotMachine");
        }
        else
        {
            Debug.LogWarning("SlotMachine.Instance or allActionConfigs not available yet. Will retry when showing UI.");
        }
    }

    public void Show()
    {
        if (actionConfigMap == null || actionConfigMap.Count == 0)
        {
            InitializeActionConfigs();
        }
        
        if (armouryPanel != null)
        {
            armouryPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        ShowEquippedDice();
    }

    public void Hide()
    {
        if (armouryPanel != null)
        {
            armouryPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnVictory();
        }
    }

    private void UpdateHeader(string header, string instruction)
    {
        if (headerText != null)
        {
            headerText.text = header;
        }
        
        if (instructionText != null)
        {
            instructionText.text = instruction;
        }
    }

    private void ShowEquippedDice()
    {
        UpdateHeader("ARMOURY", "Select a dice to modify:");
        
        foreach (Transform child in diceListContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in faceSelectionContainer)
        {
            Destroy(child.gameObject);
        }

        EquipmentItemSO weapon = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Weapon);
        EquipmentItemSO armor = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Armor);
        EquipmentItemSO item = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Item);

        int totalDice = 0;

        if (weapon != null)
        {
            totalDice += ShowEquipmentSection(weapon, "WEAPON");
        }

        if (armor != null)
        {
            totalDice += ShowEquipmentSection(armor, "ARMOR");
        }

        if (item != null)
        {
            totalDice += ShowEquipmentSection(item, "ITEM");
        }

        Debug.Log($"Showing {totalDice} total equipped dice organized by equipment slot");
    }

    private int ShowEquipmentSection(EquipmentItemSO equipment, string slotLabel)
    {
        if (equipment.diceGranted == null || equipment.diceGranted.Count == 0)
        {
            return 0;
        }

        GameObject headerGO = Instantiate(diceButtonPrefab, diceListContainer);
        Button headerButton = headerGO.GetComponent<Button>();
        if (headerButton != null)
        {
            headerButton.interactable = false;
        }

        Image headerImage = headerGO.GetComponent<Image>();
        if (headerImage != null)
        {
            Color headerColor = headerImage.color;
            headerColor.a = 0.5f;
            headerImage.color = headerColor;
        }

        TextMeshProUGUI headerText = headerGO.GetComponentInChildren<TextMeshProUGUI>();
        if (headerText != null)
        {
            headerText.text = $"━━━ {slotLabel}: {equipment.itemName} ━━━";
            headerText.fontStyle = TMPro.FontStyles.Bold;
            headerText.fontSize = equipmentHeaderFontSize;
        }

        for (int i = 0; i < equipment.diceGranted.Count; i++)
        {
            DiceDefinitionSO dice = equipment.diceGranted[i];
            int diceIndex = i;

            GameObject buttonGO = Instantiate(diceButtonPrefab, diceListContainer);
            Button button = buttonGO.GetComponent<Button>();

            TextMeshProUGUI nameText = buttonGO.transform.Find("DiceNameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText == null)
            {
                nameText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (nameText != null)
            {
                string diceName = string.IsNullOrEmpty(dice.dieName) ? "Dice" : dice.dieName;
                nameText.text = $"  └─ {diceName} #{diceIndex + 1}";
                nameText.fontSize = diceButtonFontSize;
            }

            button.onClick.AddListener(() => {
                OnDiceSelected(dice, equipment, diceIndex);
            });
        }

        return equipment.diceGranted.Count;
    }

    private void OnDiceSelected(DiceDefinitionSO dice, EquipmentItemSO equipment, int diceIndexInItem)
    {
        selectedDice = dice;
        selectedEquipmentItem = equipment;
        selectedDiceIndexInItem = diceIndexInItem;
        
        string diceName = string.IsNullOrEmpty(dice.dieName) ? "Dice" : dice.dieName;
        Debug.Log($"Selected {equipment.itemName} - {diceName} #{diceIndexInItem + 1} with {dice.faces.Count} faces");
        ShowDiceFaces(dice, equipment, diceIndexInItem);
    }

    private void ShowDiceFaces(DiceDefinitionSO dice, EquipmentItemSO equipment, int diceIndexInItem)
    {
        string diceName = string.IsNullOrEmpty(dice.dieName) ? "Dice" : dice.dieName;
        UpdateHeader($"{equipment.itemName} - {diceName} #{diceIndexInItem + 1}", "Select a face to change:");
        
        foreach (Transform child in diceListContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in faceSelectionContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < dice.faces.Count; i++)
        {
            int faceIndex = i;
            DiceActionType faceType = dice.faces[i];

            GameObject buttonGO = Instantiate(faceButtonPrefab, faceSelectionContainer);
            Button button = buttonGO.GetComponent<Button>();

            TextMeshProUGUI faceText = buttonGO.transform.Find("FaceText")?.GetComponent<TextMeshProUGUI>();
            if (faceText == null)
            {
                faceText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (faceText != null)
            {
                faceText.text = $"Face {faceIndex + 1}: {faceType}";
                faceText.fontSize = faceButtonFontSize;
            }

            Image faceIcon = buttonGO.transform.Find("FaceIcon")?.GetComponent<Image>();
            if (faceIcon != null && actionConfigMap.ContainsKey(faceType))
            {
                faceIcon.sprite = actionConfigMap[faceType].actionSprite;
            }

            button.onClick.AddListener(() => {
                OnFaceSelected(faceIndex);
            });
        }
        
        GameObject backButtonGO = Instantiate(faceButtonPrefab, faceSelectionContainer);
        Button backButton = backButtonGO.GetComponent<Button>();
        TextMeshProUGUI backText = backButtonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = "← Back to Equipment List";
            backText.fontSize = faceButtonFontSize;
        }
        backButton.onClick.AddListener(() => {
            selectedDice = null;
            selectedEquipmentItem = null;
            selectedDiceIndexInItem = -1;
            ShowEquippedDice();
        });
    }

    private void OnFaceSelected(int faceIndex)
    {
        selectedFaceIndex = faceIndex;
        DiceActionType currentFaceType = selectedDice.faces[faceIndex];
        Debug.Log($"Selected face {faceIndex + 1} (currently: {currentFaceType})");
        ShowActionTypeSelection();
    }

    private void ShowActionTypeSelection()
    {
        UpdateHeader($"CHANGE FACE {selectedFaceIndex + 1}", "Choose new action type:");
        
        foreach (Transform child in diceListContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in faceSelectionContainer)
        {
            Destroy(child.gameObject);
        }

        DiceActionType[] availableTypes = System.Enum.GetValues(typeof(DiceActionType)) as DiceActionType[];

        foreach (DiceActionType actionType in availableTypes)
        {
            if (!actionConfigMap.ContainsKey(actionType))
            {
                continue;
            }

            GameObject buttonGO = Instantiate(faceButtonPrefab, faceSelectionContainer);
            Button button = buttonGO.GetComponent<Button>();

            TextMeshProUGUI actionText = buttonGO.transform.Find("FaceText")?.GetComponent<TextMeshProUGUI>();
            if (actionText == null)
            {
                actionText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (actionText != null)
            {
                actionText.text = actionType.ToString();
                actionText.fontSize = actionTypeButtonFontSize;
            }

            Image actionIcon = buttonGO.transform.Find("FaceIcon")?.GetComponent<Image>();
            if (actionIcon != null)
            {
                actionIcon.sprite = actionConfigMap[actionType].actionSprite;
            }

            button.onClick.AddListener(() => {
                OnActionTypeSelected(actionType);
            });
        }
        
        GameObject backButtonGO = Instantiate(faceButtonPrefab, faceSelectionContainer);
        Button backButton = backButtonGO.GetComponent<Button>();
        TextMeshProUGUI backText = backButtonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = "← Back to Face List";
            backText.fontSize = actionTypeButtonFontSize;
        }
        backButton.onClick.AddListener(() => {
            selectedFaceIndex = -1;
            ShowDiceFaces(selectedDice, selectedEquipmentItem, selectedDiceIndexInItem);
        });
    }

    private void OnActionTypeSelected(DiceActionType newActionType)
    {
        if (selectedDice != null && selectedFaceIndex >= 0 && selectedFaceIndex < selectedDice.faces.Count)
        {
            DiceActionType oldActionType = selectedDice.faces[selectedFaceIndex];
            selectedDice.faces[selectedFaceIndex] = newActionType;
            
            string diceName = string.IsNullOrEmpty(selectedDice.dieName) ? "Dice" : selectedDice.dieName;
            string equipmentName = selectedEquipmentItem != null ? selectedEquipmentItem.itemName : "Unknown";
            
            Debug.Log($"Modified {equipmentName} - {diceName} #{selectedDiceIndexInItem + 1} | Face {selectedFaceIndex + 1}: {oldActionType} → {newActionType}");

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.RefreshInventoryDisplay();
            }

            selectedDice = null;
            selectedEquipmentItem = null;
            selectedDiceIndexInItem = -1;
            selectedFaceIndex = -1;
            
            Hide();
        }
    }

    public void OnSkipButtonClicked()
    {
        Debug.Log("Player skipped Armoury modification");
        Hide();
    }
}
