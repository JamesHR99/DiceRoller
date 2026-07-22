using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ActionAgilityTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Action Info")]
    public DiceActionType actionType;

    private Canvas canvas;
    private RectTransform tooltipRect;

    void Start()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }

        canvas = GetComponentInParent<Canvas>();
    }

    public void SetActionType(DiceActionType type)
    {
        actionType = type;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (tooltipPanel == null || tooltipText == null) return;

        DiceActionConfig? config = GetActionConfig(actionType);
        if (config == null) return;

        DiceActionConfig actionConfig = config.Value;

        string tooltipContent = $"<b>{actionType}</b>\n";
        tooltipContent += $"Agility: {actionConfig.agility}\n";
        tooltipContent += $"Base Value: {actionConfig.baseValue}";

        if (actionConfig.critChance > 0)
        {
            tooltipContent += $"\nCrit Chance: {actionConfig.critChance * 100}%";
        }

        tooltipText.text = tooltipContent;
        tooltipPanel.SetActive(true);

        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out mousePosition
        );

        if (tooltipRect != null)
        {
            tooltipRect.anchoredPosition = mousePosition + new Vector2(10, 10);
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private DiceActionConfig? GetActionConfig(DiceActionType type)
    {
        if (SlotMachine.Instance == null || SlotMachine.Instance.allActionConfigs == null)
        {
            return null;
        }

        foreach (var config in SlotMachine.Instance.allActionConfigs)
        {
            if (config.actionType == type)
            {
                return config;
            }
        }

        return null;
    }
}
