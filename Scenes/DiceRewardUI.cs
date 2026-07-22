using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DiceRewardUI : MonoBehaviour
{
    public static DiceRewardUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject rewardPanel;
    public Transform actionChoicesContainer;
    public GameObject actionChoiceButtonPrefab;

    [Header("Dice Selection UI")]
    public Transform diceListContainer;
    public GameObject diceButtonPrefab;
    public Transform faceListContainer;
    public GameObject faceButtonPrefab;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI instructionText;
    public Button skipButton;

    [Header("Font Sizes")]
    public float actionButtonFontSize = 20f;
    public float diceButtonFontSize = 14f;
    public float faceButtonFontSize = 14f;

    private Dictionary<DiceActionType, DiceActionConfig> actionConfigMap;
    private DiceActionType selectedRewardAction;
    private DiceDefinitionSO selectedDice;
    private EquipmentItemSO selectedEquipment;
    private int selectedDiceIndexInItem;
    private int selectedFaceIndex;

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

        EnsureReferencesAreSet();
        InitializeActionConfigs();

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipPressed);
        }
    }

    private void EnsureReferencesAreSet()
    {
        if (rewardPanel == null)
        {
            rewardPanel = gameObject;
        }

        if (actionChoicesContainer == null)
        {
            actionChoicesContainer = transform.Find("ActionChoicesContainer");
        }

        if (diceListContainer == null)
        {
            diceListContainer = transform.Find("DiceSelectionContainer");
        }

        if (faceListContainer == null)
        {
            faceListContainer = transform.Find("FaceSelectionContainer");
        }

        if (headerText == null)
        {
            Transform titleTransform = transform.Find("Title");
            if (titleTransform != null)
            {
                headerText = titleTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (instructionText == null)
        {
            Transform instructionTransform = transform.Find("InstructionText");
            if (instructionTransform != null)
            {
                instructionText = instructionTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (skipButton == null)
        {
            Transform skipButtonTransform = transform.Find("SkipButton");
            if (skipButtonTransform != null)
            {
                skipButton = skipButtonTransform.GetComponent<Button>();
            }
        }
    }

    private void InitializeActionConfigs()
    {
        actionConfigMap = new Dictionary<DiceActionType, DiceActionConfig>();
        if (SlotMachine.Instance != null && SlotMachine.Instance.allActionConfigs != null)
        {
            foreach (var config in SlotMachine.Instance.allActionConfigs)
            {
                if (!actionConfigMap.ContainsKey(config.actionType))
                    actionConfigMap.Add(config.actionType, config);
            }
            Debug.Log($"DiceRewardUI loaded {actionConfigMap.Count} action configs from SlotMachine");
        }
    }

    public void ShowRewardActions()
    {
        if (actionChoicesContainer == null)
        {
            Debug.LogError("DiceRewardUI: actionChoicesContainer is null! Please assign it in the Inspector.");
            return;
        }

        if (actionChoiceButtonPrefab == null)
        {
            Debug.LogError("DiceRewardUI: actionChoiceButtonPrefab is null! Please assign it in the Inspector.");
            return;
        }

        if (actionConfigMap == null || actionConfigMap.Count == 0)
        {
            InitializeActionConfigs();
        }

        if (actionConfigMap.Count == 0)
        {
            Debug.LogError("DiceRewardUI: No action configs available! Make sure SlotMachine has action configs assigned.");
            return;
        }

        List<DiceActionType> allActions = actionConfigMap.Keys.ToList();
        List<DiceActionType> rewardActions = allActions.OrderBy(x => Random.value).Take(3).ToList();

        foreach (Transform child in actionChoicesContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (DiceActionType actionType in rewardActions)
        {
            GameObject buttonGO = Instantiate(actionChoiceButtonPrefab, actionChoicesContainer);
            Button button = buttonGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = actionType.ToString();
                buttonText.fontSize = actionButtonFontSize;
            }

            button.onClick.AddListener(() => OnRewardActionSelected(actionType));
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        Debug.Log($"Showing {rewardActions.Count} reward action choices");
    }

    private void OnRewardActionSelected(DiceActionType actionType)
    {
        selectedRewardAction = actionType;
        Debug.Log($"Player selected reward action: {actionType}");

        ShowEquippedDice();
    }

    private void ShowEquippedDice()
    {
        ClearContainers();

        if (headerText != null)
        {
            headerText.text = $"Select a Dice to Modify";
        }

        if (instructionText != null)
        {
            instructionText.text = "Choose which dice to add the new action to";
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        EquipmentItemSO weapon = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Weapon);
        EquipmentItemSO armor = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Armor);
        EquipmentItemSO item = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Item);

        if (weapon != null) ShowEquipmentSection(weapon, "Weapon");
        if (armor != null) ShowEquipmentSection(armor, "Armor");
        if (item != null) ShowEquipmentSection(item, "Item");
    }

    private int ShowEquipmentSection(EquipmentItemSO equipment, string slotLabel)
    {
        GameObject headerGO = Instantiate(diceButtonPrefab, diceListContainer);
        Button headerButton = headerGO.GetComponent<Button>();
        if (headerButton != null)
        {
            headerButton.interactable = false;
        }
        TextMeshProUGUI headerText = headerGO.GetComponentInChildren<TextMeshProUGUI>();
        if (headerText != null)
        {
            headerText.text = $"━━━ {slotLabel}: {equipment.itemName} ━━━";
            headerText.fontStyle = TMPro.FontStyles.Bold;
            headerText.fontSize = diceButtonFontSize + 2;
        }

        for (int i = 0; i < equipment.diceGranted.Count; i++)
        {
            DiceDefinitionSO dice = equipment.diceGranted[i];
            GameObject buttonGO = Instantiate(diceButtonPrefab, diceListContainer);
            var nameText = buttonGO.transform.Find("DiceNameText")?.GetComponent<TextMeshProUGUI>() ?? buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string diceName = string.IsNullOrEmpty(dice.dieName) ? "Dice" : dice.dieName;
                nameText.text = $"  └─ {diceName} #{i + 1}";
                nameText.fontSize = diceButtonFontSize;
            }
            buttonGO.GetComponent<Button>().onClick.AddListener(() => OnDiceSelected(dice, equipment, i));
        }
        return equipment.diceGranted.Count;
    }

    private void OnDiceSelected(DiceDefinitionSO dice, EquipmentItemSO equipment, int diceIndex)
    {
        selectedDice = dice;
        selectedEquipment = equipment;
        selectedDiceIndexInItem = diceIndex;

        ShowDiceFaces();
    }

    private void ShowDiceFaces()
    {
        ClearContainers();

        if (headerText != null)
        {
            string diceName = string.IsNullOrEmpty(selectedDice.dieName) ? "Dice" : selectedDice.dieName;
            headerText.text = $"Select Face to Replace on {diceName} #{selectedDiceIndexInItem + 1}";
        }

        if (instructionText != null)
        {
            instructionText.text = $"This face will become: {selectedRewardAction}";
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < selectedDice.faces.Count; i++)
        {
            int faceIndex = i;
            DiceActionType faceType = selectedDice.faces[i];

            GameObject buttonGO = Instantiate(faceButtonPrefab, faceListContainer);
            Button button = buttonGO.GetComponent<Button>();
            TextMeshProUGUI faceText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (faceText != null)
            {
                faceText.text = $"Face {faceIndex + 1}: {faceType}";
                faceText.fontSize = faceButtonFontSize;
            }

            button.onClick.AddListener(() => OnFaceSelected(faceIndex));
        }
    }

    private void OnFaceSelected(int faceIndex)
    {
        selectedFaceIndex = faceIndex;
        DiceActionType oldAction = selectedDice.faces[faceIndex];

        selectedDice.faces[faceIndex] = selectedRewardAction;

        Debug.Log($"Changed face {faceIndex + 1} from {oldAction} to {selectedRewardAction}");

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.RefreshInventoryDisplay();
        }

        Hide();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnVictory();
        }
    }

    private void OnSkipPressed()
    {
        Debug.Log("Player skipped dice reward");
        Hide();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnVictory();
        }
    }

    private void ClearContainers()
    {
        if (diceListContainer != null)
        {
            foreach (Transform child in diceListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (faceListContainer != null)
        {
            foreach (Transform child in faceListContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void Hide()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        selectedRewardAction = DiceActionType.Attack;
        selectedDice = null;
        selectedEquipment = null;
        selectedDiceIndexInItem = -1;
        selectedFaceIndex = -1;

        Time.timeScale = 1f;
    }
}
