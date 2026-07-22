using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RerollManager : MonoBehaviour
{
    public static RerollManager Instance { get; private set; }

    [Header("Reroll Settings")]
    public int damageRequiredForReroll = 50;
    [Tooltip("Maximum number of reroll charges that can be stored.")]
    public int maxCharges = 3;

    [Header("UI References")]
    public Button rerollButton;
    [Tooltip("Slider showing progress toward earning the next charge.")]
    public Slider rerollChargeSlider;
    public TextMeshProUGUI rerollChargeText;
    [Tooltip("Text showing how many reroll holds are currently used (e.g. 1/3).")]
    public TextMeshProUGUI holdCountText;
    public GameObject rerollPanel;

    private int currentCharges = 0;
    private int currentDamageDealt = 0;
    private bool hasRolledThisTurn = false;
    private bool isDiceRolling = false;

    private const string ChargeFilledIcon  = "<color=#FFD700>●</color>";
    private const string ChargeEmptyIcon   = "<color=#555555>○</color>";
    private const string HoldFilledIcon    = "<color=#66CCFF>■</color>";
    private const string HoldEmptyIcon     = "<color=#333333>□</color>";
    private const int    MaxHolds          = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClicked);
        }

        UpdateRerollUI();
        UpdatePanelVisibility();
    }

    private void Update()
    {
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        if (rerollPanel == null) return;

        bool shouldShow = true;

        if (CharacterClassSelectionUI.Instance != null && CharacterClassSelectionUI.Instance.gameObject.activeInHierarchy)
            shouldShow = false;

        if (InventoryUI.Instance != null)
        {
            GameObject inventoryPanel = InventoryUI.Instance.gameObject;
            if (inventoryPanel != null && inventoryPanel.activeInHierarchy)
                shouldShow = false;
        }

        if (rerollPanel.activeSelf != shouldShow)
            rerollPanel.SetActive(shouldShow);
    }

    public void OnTurnStart()
    {
        hasRolledThisTurn = false;
        UpdateRerollUI();
    }

    public void OnDiceStartRolling()
    {
        isDiceRolling = true;
        UpdateRerollUI();
    }

    public void OnDiceStopRolling()
    {
        isDiceRolling = false;
        hasRolledThisTurn = true;
        UpdateRerollUI();
    }

    /// <summary>Called by BattleManager each time the player deals damage.</summary>
    public void AddDamageDealt(int damage)
    {
        if (damage <= 0) return;
        if (currentCharges >= maxCharges) return; // Already full — don't track progress

        currentDamageDealt += damage;

        // Award a charge for each full threshold crossed
        while (currentDamageDealt >= damageRequiredForReroll && currentCharges < maxCharges)
        {
            currentDamageDealt -= damageRequiredForReroll;
            currentCharges++;
            Debug.Log($"Reroll charge earned! Charges: {currentCharges}/{maxCharges}");
        }

        // Cap overflow when at max charges
        if (currentCharges >= maxCharges)
            currentDamageDealt = 0;

        UpdateRerollUI();
    }

    private void OnRerollButtonClicked()
    {
        if (!CanReroll())
        {
            Debug.LogWarning("Cannot reroll: conditions not met");
            return;
        }

        currentCharges--;
        hasRolledThisTurn = false;
        Debug.Log($"Reroll used! Charges remaining: {currentCharges}/{maxCharges}");

        if (SlotMachine.Instance != null)
        {
            SlotMachine.Instance.StartSpin(isReroll: true);
        }

        UpdateRerollUI();
    }

    /// <summary>Returns true when a reroll charge is available and conditions are met.</summary>
    public bool CanReroll()
    {
        return !isDiceRolling && hasRolledThisTurn && currentCharges > 0;
    }

    private void UpdateRerollUI()
    {
        bool canReroll = CanReroll();

        if (rerollButton != null)
            rerollButton.interactable = canReroll;

        // Slider shows progress toward the NEXT charge (or empty when full)
        if (rerollChargeSlider != null)
        {
            rerollChargeSlider.maxValue = damageRequiredForReroll;
            rerollChargeSlider.value = currentCharges >= maxCharges ? damageRequiredForReroll : currentDamageDealt;
        }

        if (rerollChargeText != null)
        {
            string pips = BuildChargePips();

            if (isDiceRolling)
            {
                rerollChargeText.text = $"{pips}\n<color=#FFAA00>Rolling...</color>";
            }
            else if (currentCharges >= maxCharges)
            {
                rerollChargeText.text = $"{pips}\n<color=#00FF00>Full!</color>";
            }
            else if (currentCharges > 0)
            {
                rerollChargeText.text = $"{pips}\n{currentDamageDealt}/{damageRequiredForReroll} dmg";
            }
            else
            {
                rerollChargeText.text = $"{pips}\n{currentDamageDealt}/{damageRequiredForReroll} dmg";
            }
        }
    }

    /// <summary>Builds a pip string like ●●○ based on current charges vs max.</summary>
    private string BuildChargePips()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < maxCharges; i++)
        {
            sb.Append(i < currentCharges ? ChargeFilledIcon : ChargeEmptyIcon);
            if (i < maxCharges - 1) sb.Append(" ");
        }
        return sb.ToString();
    }

    /// <summary>Updates the hold count display, e.g. ■ □ □  1/3 holds.</summary>
    public void UpdateHoldUI(int heldCount)
    {
        if (holdCountText == null) return;

        System.Text.StringBuilder pips = new System.Text.StringBuilder();
        for (int i = 0; i < MaxHolds; i++)
        {
            pips.Append(i < heldCount ? HoldFilledIcon : HoldEmptyIcon);
            if (i < MaxHolds - 1) pips.Append(" ");
        }
        holdCountText.text = $"{pips}  <color=#CCCCCC>{heldCount}/{MaxHolds} holds</color>";
    }

    public void ResetForNewBattle()
    {
        currentCharges = 0;
        currentDamageDealt = 0;
        hasRolledThisTurn = false;
        isDiceRolling = false;
        UpdateRerollUI();
    }
}
