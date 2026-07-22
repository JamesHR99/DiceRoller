using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class SelectedDiceUI : MonoBehaviour
{
    public static SelectedDiceUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Parent object where the cards will be placed.")]
    public Transform selectedDiceContainer;
    [Tooltip("Prefab for an individual die card.")]
    public GameObject selectedDiceIconPrefab;

    [Header("Colors")]
    public Color attackCardColor  = new Color(1f,   0.35f, 0.35f, 0.9f);
    public Color comboCardColor   = new Color(1f,   0.65f, 0f,    0.9f);
    public Color defenseCardColor = new Color(0.3f, 0.55f, 1f,    0.9f);
    public Color healCardColor    = new Color(0.2f, 0.9f,  0.3f,  0.9f);
    public Color otherCardColor   = new Color(0.7f, 0.7f,  0.7f,  0.9f);

    // All dice the player has currently selected (in selection order)
    private readonly List<Dice> selectedDice = new List<Dice>();
    // Instantiated card GameObjects
    private readonly List<GameObject> displayedCards = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllDice();
        ReacquireUIReferences();
    }

    private void ReacquireUIReferences()
    {
        if (selectedDiceContainer == null)
        {
            var allTransforms = FindObjectsOfType<Transform>();
            selectedDiceContainer = allTransforms.FirstOrDefault(t => t.name == "SelectedDiceContainer");
        }
    }

    /// <summary>Adds a die to the selection and refreshes the panel.</summary>
    public void AddDie(Dice die)
    {
        if (!selectedDice.Contains(die))
        {
            selectedDice.Add(die);
            Rebuild();
        }
    }

    /// <summary>Removes a die from the selection and refreshes the panel.</summary>
    public void RemoveDie(Dice die)
    {
        if (selectedDice.Remove(die))
            Rebuild();
    }

    /// <summary>Clears all selections and cards.</summary>
    public void ClearAllDice()
    {
        selectedDice.Clear();
        DestroyAllCards();
    }

    /// <summary>Hides the panel during battle animation.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Shows the panel when the player is choosing dice.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Core rebuild — resolves combos, sorts by agility, draws cards
    // -------------------------------------------------------------------------
    private void Rebuild()
    {
        DestroyAllCards();

        if (selectedDice.Count == 0 || selectedDiceContainer == null) return;

        // Resolve combos using the same logic as SlotMachine
        ComboManager comboManager = FindObjectOfType<ComboManager>();
        List<ResolvedEntry> entries = new List<ResolvedEntry>();

        List<Dice> remainingDice = new List<Dice>(selectedDice);

        if (comboManager != null)
        {
            // Detect combos without consuming/rolling dice — mirror FindAndProcessCombos logic
            var (comboActions, afterComboDice) = comboManager.FindAndProcessCombos(new List<Dice>(selectedDice));

            // Map which dice were consumed into combos
            List<Dice> consumedDice = new List<Dice>(selectedDice);
            foreach (Dice d in afterComboDice) consumedDice.Remove(d);

            // Add combo entries
            foreach (var comboAction in comboActions)
            {
                int agility = PlayerManager.Instance != null
                    ? PlayerManager.Instance.CalculateAgilityForAction(comboAction.Type)
                    : comboAction.Agility;
                entries.Add(new ResolvedEntry
                {
                    label       = comboAction.Type.ToString(),
                    agility     = agility,
                    sprites     = comboAction.ContributingDiceSprites,
                    isCombo     = true,
                    actionType  = comboAction.Type
                });
            }

            remainingDice = afterComboDice;
        }

        // Add individual die entries
        foreach (Dice die in remainingDice)
        {
            DiceActionDetails details = die.GetActionDetails();
            int agility = PlayerManager.Instance != null
                ? PlayerManager.Instance.CalculateAgilityForAction(details.Type)
                : details.Agility;

            entries.Add(new ResolvedEntry
            {
                label      = details.Type.ToString(),
                agility    = agility,
                sprites    = details.ContributingDiceSprites,
                isCombo    = false,
                actionType = details.Type
            });
        }

        // Sort highest agility first (player wins ties — same as BattleManager)
        entries = entries.OrderByDescending(e => e.agility).ToList();

        // Draw cards in order
        for (int i = 0; i < entries.Count; i++)
        {
            CreateCard(entries[i]);
        }
    }

    private void CreateCard(ResolvedEntry entry)
    {
        if (selectedDiceIconPrefab == null || selectedDiceContainer == null) return;

        GameObject card = Instantiate(selectedDiceIconPrefab, selectedDiceContainer);
        displayedCards.Add(card);

        // Enable the label — it is disabled by default on the prefab so it
        // doesn't appear on dice icons used elsewhere (e.g. battle animation).
        Transform labelTransform = card.transform.Find("Label");
        if (labelTransform != null) labelTransform.gameObject.SetActive(true);

        // --- Background color ---
        Image bg = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        if (bg != null)
            bg.color = GetCardColor(entry.actionType, entry.isCombo);

        // --- Die sprite(s) ---
        Image spriteImage = card.GetComponentInChildren<Image>();
        if (spriteImage != null && entry.sprites != null && entry.sprites.Count > 0)
            spriteImage.sprite = entry.sprites[0];

        // --- Text label (rendered below the dice image) ---
        TextMeshProUGUI label = card.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            string comboTag   = entry.isCombo ? "<color=#FFD700><b>COMBO</b></color>\n" : "";
            string actionName = $"<b>{FormatActionName(entry.label)}</b>";
            string agilityTag = $"\nSPD <color=#00FFFF><b>{entry.agility}</b></color>";
            label.text = $"{comboTag}{actionName}{agilityTag}";
        }
    }

    private Color GetCardColor(DiceActionType type, bool isCombo)
    {
        if (isCombo) return comboCardColor;
        switch (type)
        {
            case DiceActionType.Attack:
            case DiceActionType.HeavyAttack:
            case DiceActionType.LightAttack:
            case DiceActionType.SwiftStrike:
                return attackCardColor;
            case DiceActionType.Block:
            case DiceActionType.Defend:
                return defenseCardColor;
            case DiceActionType.Heal:
                return healCardColor;
            default:
                return otherCardColor;
        }
    }

    private static string FormatActionName(string raw)
    {
        // Insert spaces before capital letters: "HeavyAttack" → "Heavy Attack"
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i])) sb.Append(' ');
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }

    private void DestroyAllCards()
    {
        foreach (var card in displayedCards)
            if (card != null) Destroy(card);
        displayedCards.Clear();
    }

    // Simple data container for a resolved action entry
    private struct ResolvedEntry
    {
        public string          label;
        public int             agility;
        public List<Sprite>    sprites;
        public bool            isCombo;
        public DiceActionType  actionType;
    }
}
