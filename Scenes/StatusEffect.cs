using UnityEngine;

public enum StatusEffectType
{
    None,
    Burn,      // Deals damage over time
    Poison,    // Deals damage over time  
    Bleed,     // Deals 10 damage for 3 turns
    Stun,      // Causes enemy to miss next attack turn
    Weak,      // Reduces damage dealt
    Vulnerable, // Increases damage taken
    Freeze     // Reduces enemy stamina by 2
}

[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public int damagePerTurn;
    public int duration;
    public float multiplier = 1f;

    public StatusEffect(StatusEffectType type, int damage, int duration)
    {
        this.type = type;
        this.damagePerTurn = damage;
        this.duration = duration;
        this.multiplier = 1f;
    }

    public StatusEffect(StatusEffectType type, int damage, int duration, float multiplier)
    {
        this.type = type;
        this.damagePerTurn = damage;
        this.duration = duration;
        this.multiplier = multiplier;
    }

    public string GetDisplayName()
    {
        return type switch
        {
            StatusEffectType.Burn => "🔥 Burn",
            StatusEffectType.Poison => "☠ Poison",
            StatusEffectType.Bleed => "🩸 Bleed",
            StatusEffectType.Stun => "💫 Stun",
            StatusEffectType.Weak => "⬇ Weak",
            StatusEffectType.Vulnerable => "🎯 Vulnerable",
            StatusEffectType.Freeze => "❄ Freeze",
            _ => type.ToString()
        };
    }

    public string GetDescription()
    {
        return type switch
        {
            StatusEffectType.Burn => $"Takes {damagePerTurn} fire damage per turn",
            StatusEffectType.Poison => $"Takes {damagePerTurn} poison damage per turn",
            StatusEffectType.Bleed => $"Takes {damagePerTurn} damage for {duration} turns",
            StatusEffectType.Stun => "Misses next attack turn",
            StatusEffectType.Weak => $"Deals {multiplier * 100}% damage",
            StatusEffectType.Vulnerable => $"Takes {multiplier * 100}% more damage",
            StatusEffectType.Freeze => "Stamina reduced by 2",
            _ => ""
        };
    }
}