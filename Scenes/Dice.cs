using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public enum DiceActionType { None, Attack, Defend, Heal, Gold, SwiftStrike, Recharge, Zaa, Faa, Laa, Naa, Lee, Zoo, HeavyAttack, LightAttack, Block, RegainStamina, Dodge, Shot_Level1, Shot_Level2, Shot_Level3 }
[System.Serializable]
public struct DiceActionConfig
{
    public DiceActionType actionType;
    public Sprite actionSprite;
    public int baseValue;
    public int attackSpeed;

    [Header("Agility")]
    [Tooltip("Agility modifier for this action. Higher values execute first in combat.")]
    public int agility;

    [Header("Critical Hit Properties")]
    [Range(0, 1)]
    public float critChance;
    [Tooltip("Critical damage multiplier (e.g., 1.25 for 125% damage). Used for attack dice.")]
    public float critDamageMultiplier;
    [Tooltip("Bonus value on a critical hit. For Heal: extra heal as a fraction (e.g. 0.2 = +20%). For Block/Defend: extra block % (e.g. 10 = +10%). For Recharge/RegainStamina: extra energy (e.g. 1 = +1).")]
    public float critBonusValue;

    [Header("Status Effect Properties")]
    [Tooltip("Type of status effect this action can apply")]
    public StatusEffectType statusEffectType;
    [Range(0, 1)]
    [Tooltip("Independent % chance to apply status effect (separate from crit)")]
    public float statusEffectChance;
    [Tooltip("Damage or strength of the status effect per turn")]
    public int statusEffectDamage;
    [Tooltip("How many turns the status effect lasts")]
    public int statusEffectDuration;
    [Tooltip("Multiplier for Weak/Vulnerable effects (e.g., 0.5 for 50% damage)")]
    public float statusEffectMultiplier;
}
public struct DiceActionDetails
{
    public DiceActionType Type { get; private set; }
    public int Score { get; private set; }
    public int BaseScore { get; private set; }
    public int Speed { get; private set; }
    public int Agility { get; private set; }
    public float CritBonusValue { get; private set; }
    public List<Sprite> ContributingDiceSprites { get; private set; }
    public bool IsCritical { get; private set; }
    public StatusEffect AppliedStatusEffect { get; private set; }

    public DiceActionDetails(DiceActionType type, int score, int speed, List<Sprite> sprites, bool isCritical, StatusEffect statusEffect, int agility = 0, int baseScore = -1, float critBonusValue = 0f)
    {
        Type = type;
        Score = score;
        BaseScore = baseScore < 0 ? score : baseScore;
        Speed = speed;
        Agility = agility;
        CritBonusValue = critBonusValue;
        ContributingDiceSprites = sprites ?? new List<Sprite>();
        IsCritical = isCritical;
        AppliedStatusEffect = statusEffect;
    }
}

public class Dice : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    public Image diceImage;
    public Button diceButton;
    public Color normalColor = Color.white;
    public Color heldColor = Color.gray;
    public Color critColor = Color.red;
    public Color statusEffectColor = new Color(0.6f, 0.2f, 0.8f);

    [Header("Reroll Hold")]
    [Tooltip("Colour tint applied when this die is held for reroll (right-click).")]
    public Color rerollHeldColor = new Color(0.4f, 0.8f, 1f);

    private const int MaxRerollHolds = 3;
    private static int rerollHeldCount = 0;

    [HideInInspector] public bool isHeld { get; private set; }
    [HideInInspector] public bool isRerollHeld { get; private set; }
    [HideInInspector] public int score { get; private set; }

    private bool isRolling = false;
    private float rollInterval = 0.05f;
    private Coroutine rollCoroutine;
    public event System.Action OnScoreUpdated;
    private DiceActionType currentAction;
    private bool isCritical = false;
    private bool hasStatusEffect = false;

    private List<DiceActionType> _faces;
    private float _critChance;
    private Dictionary<DiceActionType, DiceActionConfig> _masterConfigMap;

    void Awake()
    {
        if (diceButton == null) diceButton = GetComponent<Button>();
        if (diceButton != null)
        {
            diceButton.onClick.AddListener(ToggleHold);
            Debug.Log($"Dice button listener added for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Dice {gameObject.name} has no Button component!");
        }
    }

    public void ConfigureDie(DiceDefinitionSO definition, Dictionary<DiceActionType, DiceActionConfig> masterConfig, float? overrideCritChance = null)
    {
        _faces = new List<DiceActionType>(definition.faces);
        _masterConfigMap = masterConfig;
    }

    public void Roll()
    {
        if (isHeld || isRerollHeld || _faces == null || _faces.Count == 0) return;

        isCritical = false;        hasStatusEffect = false;

        currentAction = _faces[Random.Range(0, _faces.Count)];

        if (_masterConfigMap.TryGetValue(currentAction, out DiceActionConfig config))
        {
            isCritical = Random.value < config.critChance;
            hasStatusEffect = Random.value < config.statusEffectChance;
        }

        UpdateFaceWithAction(currentAction);
    }

    private void UpdateFaceWithAction(DiceActionType action)
    {
        currentAction = action;
        if (_masterConfigMap != null && _masterConfigMap.TryGetValue(action, out DiceActionConfig config))
        {
            if (diceImage != null)
            {
                diceImage.sprite = config.actionSprite;
                UpdateFaceColor();
            }
            score = config.baseValue;
        }
    }

    public void UpdateFaceColor()
    {
        if (diceImage == null) return;

        if (isCritical)
        {
            diceImage.color = critColor;
        }
        else if (hasStatusEffect)
        {
            diceImage.color = statusEffectColor;
        }
        else
        {
            if (isRerollHeld)
                diceImage.color = rerollHeldColor;
            else
                diceImage.color = isHeld ? heldColor : normalColor;
        }
    }

    // -------------------------------------------------------------------------
    // Right-click: reroll hold
    // -------------------------------------------------------------------------

    /// <summary>Handles pointer clicks — right-click toggles reroll hold.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            ToggleRerollHold();
    }

    /// <summary>Toggles whether this die is protected from rerolling (max 3 across all dice).</summary>
    public void ToggleRerollHold()
    {
        if (isRerollHeld)
        {
            isRerollHeld = false;
            rerollHeldCount--;
        }
        else
        {
            if (rerollHeldCount >= MaxRerollHolds)
            {
                Debug.Log($"[Dice] Max reroll holds ({MaxRerollHolds}) reached.");
                return;
            }
            isRerollHeld = true;
            rerollHeldCount++;
        }
        UpdateFaceColor();
        RerollManager.Instance?.UpdateHoldUI(rerollHeldCount);
    }

    /// <summary>Clears this die's reroll hold without affecting others.</summary>
    public void ClearRerollHold()
    {
        if (!isRerollHeld) return;
        isRerollHeld = false;
        rerollHeldCount = Mathf.Max(0, rerollHeldCount - 1);
        UpdateFaceColor();
        RerollManager.Instance?.UpdateHoldUI(rerollHeldCount);
    }

    /// <summary>
    /// Resets all reroll holds across every die and updates the hold UI.
    /// Call this at the start of every normal (non-reroll) spin.
    /// </summary>
    public static void ResetAllHolds(IEnumerable<Dice> dice)
    {
        foreach (Dice die in dice)
        {
            if (die == null) continue;
            die.isRerollHeld = false;
            die.UpdateFaceColor();
        }
        rerollHeldCount = 0;
        RerollManager.Instance?.UpdateHoldUI(0);
    }

    /// <summary>
    /// Restores the reroll-held visual on a freshly created die without touching the static counter.
    /// Only call this as part of a batch restore followed by SetRerollHeldCount.
    /// </summary>
    internal void RestoreRerollHeld()
    {
        isRerollHeld = true;
        UpdateFaceColor();
    }

    /// <summary>
    /// Directly sets the static held counter and refreshes the UI.
    /// Call this once after batch-restoring held state via RestoreRerollHeld.
    /// </summary>
    internal static void SetRerollHeldCount(int count)
    {
        rerollHeldCount = Mathf.Clamp(count, 0, MaxRerollHolds);
        RerollManager.Instance?.UpdateHoldUI(rerollHeldCount);
    }

    public void SetButtonInteractable(bool interactable)
    {
        if (diceButton != null)
        {
            diceButton.interactable = interactable;
            Debug.Log($"Dice {gameObject.name} button interactable set to {interactable}");
        }
        else
        {
            Debug.LogWarning($"Dice {gameObject.name} has no button to set interactable!");
        }
    }

    public DiceActionDetails GetActionDetails()
    {
        int scoreValue = 0;
        int baseScoreValue = 0;
        int speedValue = 0;
        StatusEffect effectToApply = null;

        if (_masterConfigMap != null && _masterConfigMap.TryGetValue(currentAction, out DiceActionConfig config))
        {
            baseScoreValue = config.baseValue;
            scoreValue = config.baseValue;
            speedValue = config.attackSpeed;

            if (isCritical)
            {
                scoreValue = Mathf.RoundToInt(scoreValue * config.critDamageMultiplier);
            }

            if (hasStatusEffect && config.statusEffectType != StatusEffectType.None)
            {
                float multiplier = config.statusEffectMultiplier > 0 ? config.statusEffectMultiplier : 1f;
                effectToApply = new StatusEffect(
                    config.statusEffectType,
                    config.statusEffectDamage,
                    config.statusEffectDuration,
                    multiplier
                );
            }
        }

        var sprites = new List<Sprite> { diceImage.sprite };
        float critBonus = (_masterConfigMap != null && _masterConfigMap.TryGetValue(currentAction, out DiceActionConfig cfg)) ? cfg.critBonusValue : 0f;
        return new DiceActionDetails(currentAction, scoreValue, speedValue, sprites, isCritical, effectToApply, 0, baseScoreValue, critBonus);
    }
    public void ToggleHold() 
    { 
        if (!isHeld) 
        { 
            bool costsEnergy = GetCurrentAction() != DiceActionType.Recharge && GetCurrentAction() != DiceActionType.RegainStamina; 
            if (PlayerEnergy.Instance != null && (PlayerEnergy.Instance.CanSelectDie() || !costsEnergy)) 
            { 
                SetHeldState(true); 
                if (costsEnergy) 
                { 
                    PlayerEnergy.Instance.OnDieSelected(); 
                } 
                if (SelectedDiceUI.Instance != null) 
                    SelectedDiceUI.Instance.AddDie(this); 
            } 
            else 
            { 
                return; 
            } 
        } 
        else 
        { 
            bool wasFree = GetCurrentAction() == DiceActionType.Recharge || GetCurrentAction() == DiceActionType.RegainStamina; 
            SetHeldState(false); 
            if (!wasFree) 
            { 
                PlayerEnergy.Instance.OnDieDeselected(); 
            } 
            if (SelectedDiceUI.Instance != null) 
                SelectedDiceUI.Instance.RemoveDie(this); 
        } 
        OnScoreUpdated?.Invoke(); 
    }
    public void SetHeldState(bool held) { isHeld = held; UpdateFaceColor(); }
    public DiceActionType GetCurrentAction() { return currentAction; }
    public void StartRolling() { if (isRolling || isHeld) return; isRolling = true; SetButtonInteractable(false); rollCoroutine = StartCoroutine(RollingAnimation()); }
    public void StopRolling() { if (rollCoroutine != null) StopCoroutine(rollCoroutine); isRolling = false; SetButtonInteractable(true); Roll(); OnScoreUpdated?.Invoke(); }
    IEnumerator RollingAnimation() { while (isRolling) { if (_masterConfigMap == null || _masterConfigMap.Count == 0 || _faces == null || _faces.Count == 0) yield break; var randomAction = _faces[Random.Range(0, _faces.Count)]; if (_masterConfigMap.ContainsKey(randomAction)) { diceImage.sprite = _masterConfigMap[randomAction].actionSprite; } yield return new WaitForSeconds(rollInterval); } }
}