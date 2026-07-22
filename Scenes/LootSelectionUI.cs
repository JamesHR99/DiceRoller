using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LootSelectionUI : MonoBehaviour
{
    public static LootSelectionUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject lootChoiceButtonPrefab;
    public Transform optionsContainer;

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

    public void Show(List<EquipmentItemSO> lootChoices)
    {
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (EquipmentItemSO item in lootChoices)
        {
            GameObject buttonGO = Instantiate(lootChoiceButtonPrefab, optionsContainer);
            Button button = buttonGO.GetComponent<Button>();

            buttonGO.transform.Find("LevelNameText").GetComponent<TextMeshProUGUI>().text = item.itemName;
            buttonGO.transform.Find("LevelDescriptionText").GetComponent<TextMeshProUGUI>().text = item.description;
            buttonGO.transform.Find("LevelIcon").GetComponent<Image>().sprite = item.itemIcon;

            button.onClick.AddListener(() => {
                OnLootSelected(item);
            });
        }

        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnLootSelected(EquipmentItemSO chosenItem)
    {
        if (chosenItem.itemType == ItemType.Special)
        {
            HandleSpecialItem(chosenItem);
        }
        else
        {
            if (PlayerEquipment.Instance != null)
            {
                PlayerEquipment.Instance.EquipItem(chosenItem);
                Debug.Log($"Player chose and equipped loot: {chosenItem.itemName}");
            }

            Hide();

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnVictory();
            }
        }
    }

    private void HandleSpecialItem(EquipmentItemSO specialItem)
    {
        Hide();

        if (specialItem.specialItemType == SpecialItemType.Armoury)
        {
            ArmouryUI armouryUI = ArmouryUI.Instance;
            if (armouryUI == null)
            {
                armouryUI = FindFirstObjectByType<ArmouryUI>(FindObjectsInactive.Include);
            }

            if (armouryUI != null)
            {
                armouryUI.Show();
            }
            else
            {
                Debug.LogError("ArmouryUI not found! Make sure ArmouryPanel exists in the scene with ArmouryUI component.");
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.OnVictory();
                }
            }
        }
    }
}