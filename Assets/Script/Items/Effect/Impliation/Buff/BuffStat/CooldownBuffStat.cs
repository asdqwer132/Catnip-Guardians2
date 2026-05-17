using System;
using UnityEngine;

[Serializable]
public class CooldownBuffStat
{
    [Header("Cooldown Bonus")]
    [Tooltip("쿨타임에 더해지는 값. -0.2면 0.2초 감소")]
    public float cooldownAdd = 0f;

    [Tooltip("쿨타임 배율. 0.8이면 쿨타임 20% 감소")]
    public float cooldownMultiplier = 1f;

    public void Add(CooldownBuffStat other)
    {
        if (other == null)
            return;

        cooldownAdd += other.cooldownAdd;
        cooldownMultiplier *= other.cooldownMultiplier;
    }

    public void Reset()
    {
        cooldownAdd = 0f;
        cooldownMultiplier = 1f;
    }

    public CooldownBuffStat Clone()
    {
        CooldownBuffStat clone = new CooldownBuffStat();

        clone.cooldownAdd = cooldownAdd;
        clone.cooldownMultiplier = cooldownMultiplier;

        return clone;
    }

    public float ApplyTo(float baseCooldown)
    {
        float result = baseCooldown;

        result += cooldownAdd;
        result *= cooldownMultiplier;

        return Mathf.Max(0.01f, result);
    }
}