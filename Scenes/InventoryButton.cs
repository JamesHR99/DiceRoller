using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventoryButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnInventoryButtonClicked);
    }

    private void OnInventoryButtonClicked()
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("InventoryUI Instance not found!");
        }
    }
}
