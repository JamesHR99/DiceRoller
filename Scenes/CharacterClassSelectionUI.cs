using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterClassSelectionUI : MonoBehaviour
{
    public static CharacterClassSelectionUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject selectionPanel;
    public Transform classButtonContainer;
    public GameObject classButtonPrefab;

    [Header("Class Preview")]
    public Image classIcon;
    public TextMeshProUGUI className;
    public TextMeshProUGUI classDescription;
    public Button confirmButton;

    [Header("Dice Preview")]
    public Transform dicePreviewContainer;
    public GameObject diceInfoPrefab;

    [Header("Available Classes")]
    public CharacterClassSO[] availableClasses;

    [Header("Font Sizes")]
    public float classButtonFontSize = 16f;
    public float dicePreviewFontSize = 14f;

    private CharacterClassSO selectedClass;

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
    }

    void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmSelection);
            confirmButton.interactable = false;
        }

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }

        Time.timeScale = 0f;

        PopulateClassButtons();
    }

    private void PopulateClassButtons()
    {
        foreach (Transform child in classButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (CharacterClassSO characterClass in availableClasses)
        {
            if (characterClass == null) continue;

            GameObject buttonGO = Instantiate(classButtonPrefab, classButtonContainer);
            Button button = buttonGO.GetComponent<Button>();

            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = characterClass.className;
                buttonText.fontSize = classButtonFontSize;
            }

            button.onClick.AddListener(() => {
                OnClassSelected(characterClass);
            });
        }

        if (availableClasses.Length > 0)
        {
            OnClassSelected(availableClasses[0]);
        }
    }

    private void OnClassSelected(CharacterClassSO characterClass)
    {
        selectedClass = characterClass;

        if (classIcon != null)
        {
            classIcon.sprite = characterClass.classIcon;
        }

        if (className != null)
        {
            className.text = characterClass.className;
        }

        if (classDescription != null)
        {
            classDescription.text = characterClass.classDescription;
        }

        RefreshDicePreview(characterClass);

        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }

        Debug.Log($"Selected class: {characterClass.className}");
    }

    private void RefreshDicePreview(CharacterClassSO characterClass)
    {
        if (dicePreviewContainer == null) return;

        foreach (Transform child in dicePreviewContainer)
        {
            Destroy(child.gameObject);
        }

        List<EquipmentItemSO> allEquipment = new List<EquipmentItemSO>();
        if (characterClass.startingWeapon != null) allEquipment.Add(characterClass.startingWeapon);
        if (characterClass.startingArmor != null) allEquipment.Add(characterClass.startingArmor);
        if (characterClass.startingItem != null) allEquipment.Add(characterClass.startingItem);

        foreach (EquipmentItemSO equipment in allEquipment)
        {
            if (equipment == null || equipment.diceGranted == null || equipment.diceGranted.Count == 0)
                continue;

            CreateEquipmentHeader(equipment);

            for (int i = 0; i < equipment.diceGranted.Count; i++)
            {
                DiceDefinitionSO dice = equipment.diceGranted[i];
                if (dice != null)
                {
                    CreateDiceInfo(dice, i + 1);
                }
            }
        }
    }

    private void CreateEquipmentHeader(EquipmentItemSO equipment)
    {
        if (diceInfoPrefab == null) return;

        GameObject headerGO = Instantiate(diceInfoPrefab, dicePreviewContainer);
        TextMeshProUGUI headerText = headerGO.GetComponentInChildren<TextMeshProUGUI>();
        
        if (headerText != null)
        {
            headerText.text = $"━━━ {equipment.itemName} ━━━";
            headerText.fontSize = dicePreviewFontSize + 2;
            headerText.fontStyle = TMPro.FontStyles.Bold;
            headerText.color = new Color(1f, 0.9f, 0.5f);
        }

        LayoutElement layoutElement = headerGO.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = headerGO.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 28;
        layoutElement.preferredHeight = 28;

        Button button = headerGO.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
        }
    }

    private void CreateDiceInfo(DiceDefinitionSO dice, int diceNumber)
    {
        if (diceInfoPrefab == null) return;

        GameObject diceGO = Instantiate(diceInfoPrefab, dicePreviewContainer);
        TextMeshProUGUI diceText = diceGO.GetComponentInChildren<TextMeshProUGUI>();
        
        if (diceText != null)
        {
            string diceName = string.IsNullOrEmpty(dice.dieName) ? "Dice" : dice.dieName;
            string facesText = GetFacesSummary(dice.faces);
            
            diceText.text = $"  Dice #{diceNumber}: {facesText}";
            diceText.fontSize = dicePreviewFontSize;
            diceText.alignment = TMPro.TextAlignmentOptions.Left;
        }

        LayoutElement layoutElement = diceGO.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = diceGO.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 35;
        layoutElement.preferredHeight = 35;

        Button button = diceGO.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
        }
    }

    private string GetFacesSummary(List<DiceActionType> faces)
    {
        if (faces == null || faces.Count == 0) return "Empty";

        Dictionary<DiceActionType, int> faceCounts = new Dictionary<DiceActionType, int>();
        foreach (DiceActionType face in faces)
        {
            if (faceCounts.ContainsKey(face))
                faceCounts[face]++;
            else
                faceCounts[face] = 1;
        }

        List<string> parts = new List<string>();
        foreach (var kvp in faceCounts)
        {
            parts.Add($"{kvp.Value}× {kvp.Key}");
        }

        return string.Join(", ", parts);
    }

    private void OnConfirmSelection()
    {
        if (selectedClass == null)
        {
            Debug.LogWarning("No class selected!");
            return;
        }

        Debug.Log($"Confirmed class: {selectedClass.className}");

        if (PlayerEquipment.Instance != null)
        {
            PlayerEquipment.Instance.SetStartingEquipment(
                selectedClass,
                selectedClass.startingWeapon,
                selectedClass.startingArmor,
                selectedClass.startingItem
            );
        }

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        Debug.Log("Character class selection complete. Game started!");
    }
}
