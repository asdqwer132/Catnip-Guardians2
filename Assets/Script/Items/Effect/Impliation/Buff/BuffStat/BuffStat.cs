using System;
using UnityEngine;

[Serializable]
public class BuffStat : EffectStat
{
    [Header("Attack Buff")]
    public AttackBuffStat attackBuff;

    [Header("Cooldown Buff")]
    public CooldownBuffStat cooldownBuff;

    [Header("Buff Info Buff")]
    public BuffInfoBuffStat buffInfoBuff;


    public override void Add(EffectStat other)
    {
        base.Add(other);

        BuffStat otherBuff = other as BuffStat;

        if (otherBuff == null)
            return;

        if (otherBuff.attackBuff != null)
        {
            if (attackBuff == null)
                attackBuff = new AttackBuffStat();

            attackBuff.Add(otherBuff.attackBuff);
        }

        if (otherBuff.cooldownBuff != null)
        {
            if (cooldownBuff == null)
                cooldownBuff = new CooldownBuffStat();

            cooldownBuff.Add(otherBuff.cooldownBuff);
        }

        if (otherBuff.buffInfoBuff != null)
        {
            if (buffInfoBuff == null)
                buffInfoBuff = new BuffInfoBuffStat();

            buffInfoBuff.Add(otherBuff.buffInfoBuff);
        }

    }

    public override void Reset()
    {
        base.Reset();

        if (attackBuff != null)
            attackBuff.Reset();

        if (cooldownBuff != null)
            cooldownBuff.Reset();

        if (buffInfoBuff != null)
            buffInfoBuff.Reset();

    }

    public override EffectStat Clone()
    {
        BuffStat clone = new BuffStat();

        clone.effectRadius = effectRadius;
        clone.effectCount = effectCount;
        clone.effectLifeTime = effectLifeTime;
        clone.effectDuration = effectDuration;

        if (attackBuff != null)
            clone.attackBuff = attackBuff.Clone();

        if (cooldownBuff != null)
            clone.cooldownBuff = cooldownBuff.Clone();

        if (buffInfoBuff != null)
            clone.buffInfoBuff = buffInfoBuff.Clone();


        return clone;
    }

    public BuffStat CloneBuff()
    {
        return Clone() as BuffStat;
    }

    public void ApplyAttackBuffTo(AttackStat attackStat)
    {
        if (attackBuff == null)
            return;

        attackBuff.ApplyTo(attackStat);
    }

    public void ApplyBuffInfoBuffTo(BuffInfoStat buffInfo)
    {
        if (buffInfoBuff == null)
            return;

        buffInfoBuff.ApplyTo(buffInfo);
    }

    public float ApplyCooldownBuff(float baseCooldown)
    {
        if (cooldownBuff == null)
            return Mathf.Max(0.01f, baseCooldown);

        return cooldownBuff.ApplyTo(baseCooldown);
    }
}