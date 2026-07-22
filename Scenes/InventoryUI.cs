using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeButton;

    [Header("Equipment Display")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponName;
    [SerializeField] private TextMeshProUGUI weaponDescription;
    [SerializeField] private Transform weaponDiceContainer;

    [SerializeField] private Image armorIcon;
    [SerializeField] private TextMeshProUGUI armorName;
    [SerializeField] private TextMeshProUGUI armorDescription;
    [SerializeField] private Transform armorDiceContainer;

    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Transform itemDiceContainer;

    [Header("Dice Display Prefab")]
    [SerializeField] private GameObject diceInfoPrefab;

    [Header("Empty Slot Settings")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color emptySlotColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

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
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = inventoryPanel.activeSelf;
            if (!isActive)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            RefreshInventoryDisplay();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    public void RefreshInventoryDisplay()
    {
        if (PlayerEquipment.Instance == null)
        {
            Debug.LogWarning("PlayerEquipment Instance not found!");
            return;
        }

        DisplayEquipmentSlot(EquipmentSlot.Weapon, weaponIcon, weaponName, weaponDescription, weaponDiceContainer);
        DisplayEquipmentSlot(EquipmentSlot.Armor, armorIcon, armorName, armorDescription, armorDiceContainer);
        DisplayEquipmentSlot(EquipmentSlot.Item, itemIcon, itemName, itemDescription, itemDiceContainer);
    }

    private void DisplayEquipmentSlot(EquipmentSlot slot, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI descText, Transform diceContainer)
    {
        EquipmentItemSO equipment = GetEquippedItem(slot);

        if (equipment != null)
        {
            if (icon != null)
            {
                icon.sprite = equipment.itemIcon;
                icon.color = Color.white;
            }

            if (nameText != null)
            {
                nameText.text = equipment.itemName;
            }

            if (descText != null)
            {
                descText.text = equipment.description;
            }

            DisplayDiceForEquipment(equipment, diceContainer);
        }
        else
        {
            if (icon != null)
            {
                icon.sprite = emptySlotSprite;
                icon.color = emptySlotColor;
            }

            if (nameText != null)
            {
                nameText.text = $"Empty {slot}";
            }

            if (descText != null)
            {
                descText.text = "No equipment in this slot.";
            }

            ClearDiceContainer(diceContainer);
        }
    }

    private EquipmentItemSO GetEquippedItem(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return PlayerEquipment.Instance.GetCurrentWeapon();
            case EquipmentSlot.Armor:
                return PlayerEquipment.Instance.GetCurrentArmor();
            case EquipmentSlot.Item:
                return PlayerEquipment.Instance.GetCurrentItem();
            default:
                return null;
        }
    }

    private void DisplayDiceForEquipment(EquipmentItemSO equipment, Transform diceContainer)
    {
        ClearDiceContainer(diceContainer);

        if (equipment.diceGranted == null || equipment.diceGranted.Count == 0)
        {
            GameObject noDiceObj = new GameObject("NoDiceText");
            noDiceObj.transform.SetParent(diceContainer, false);
            TextMeshProUGUI noDiceText = noDiceObj.AddComponent<TextMeshProUGUI>();
            noDiceText.text = "No dice granted";
            noDiceText.fontSize = 14;
            noDiceText.color = Color.gray;
            noDiceText.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (DiceDefinitionSO dice in equipment.diceGranted)
        {
            if (dice != null)
            {
                CreateDiceInfoDisplay(dice, diceContainer);
            }
        }
    }

    private void CreateDiceInfoDisplay(DiceDefinitionSO dice, Transform parent)
    {
        GameObject diceInfoObj;

        if (diceInfoPrefab != null)
        {
            diceInfoObj = Instantiate(diceInfoPrefab, parent);
        }
        else
        {
            diceInfoObj = new GameObject($"Dice_{dice.dieName}");
            diceInfoObj.transform.SetParent(parent, false);
        }

        TextMeshProUGUI[] texts = diceInfoObj.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (texts.Length >= 2)
        {
            texts[0].text = dice.dieName;
            texts[1].text = GetDiceFacesText(dice);
        }
        else
        {
            TextMeshProUGUI diceText = diceInfoObj.GetComponent<TextMeshProUGUI>();
            if (diceText == null)
            {
                diceText = diceInfoObj.AddComponent<TextMeshProUGUI>();
            }
            diceText.fontSize = 14;
            diceText.text = $"<b>{dice.dieName}</b>\n{GetDiceFacesText(dice)}";
        }
    }

    private string GetDiceFacesText(DiceDefinitionSO dice)
    {
        if (dice.faces == null || dice.faces.Count == 0)
        {
            return "No faces";
        }

        List<string> faceStrings = new List<string>();
        
        for (int i = 0; i < dice.faces.Count; i++)
        {
            DiceActionType face = dice.faces[i];
            if (face != DiceActionType.None)
            {
                string faceIcon = GetActionIcon(face);
                string faceColor = GetActionColor(face);
                faceStrings.Add($"<color={faceColor}>{faceIcon} {face}</color>");
            }
        }

        if (faceStrings.Count == 0)
        {
            return "No valid faces";
        }

        return string.Join("\n", faceStrings);
    }

    private string GetActionIcon(DiceActionType actionType)
    {
        switch (actionType)
        {
            case DiceActionType.Attack:
            case DiceActionType.HeavyAttack:
            case DiceActionType.SwiftStrike:
            case DiceActionType.LightAttack:
                return "⚔️";
            case DiceActionType.Defend:
            case DiceActionType.Block:
                return "🛡️";
            case DiceActionType.Heal:
                return "❤️";
            case DiceActionType.Gold:
                return "💰";
            case DiceActionType.Recharge:
            case DiceActionType.RegainStamina:
                return "⚡";
            case DiceActionType.Dodge:
                return "💨";
            default:
                return "•";
        }
    }

    private string GetActionColor(DiceActionType actionType)
    {
        switch (actionType)
        {
            case DiceActionType.Attack:
            case DiceActionType.HeavyAttack:
            case DiceActionType.SwiftStrike:
            case DiceActionType.LightAttack:
                return "#FF4444";
            case DiceActionType.Defend:
            case DiceActionType.Block:
                return "#4444FF";
            case DiceActionType.Heal:
                return "#44FF44";
            case DiceActionType.Gold:
                return "#FFD700";
            case DiceActionType.Recharge:
            case DiceActionType.RegainStamina:
                return "#FFFF44";
            case DiceActionType.Dodge:
                return "#AAAAFF";
            default:
                return "#FFFFFF";
        }
    }

    private void ClearDiceContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
