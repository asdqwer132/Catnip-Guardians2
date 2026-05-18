using System;
using UnityEngine;
[Serializable]
public class BuffStat
{
    [Header("Attack Buff")]
    public AttackBuffStat attackBuffStat = new AttackBuffStat();

    [Header("Buff Info Buff")]
    public BuffInfoBuffStat buffInfoBuffStat = new BuffInfoBuffStat();

    public void ApplyToAttackStat(AttackStat target)
    {
        ApplyBuff(attackBuffStat, target);
    }

    public void ApplyToBuffInfo(BuffInfo target)
    {
        ApplyBuff(buffInfoBuffStat, target);
    }

    private void ApplyBuff<T>(
        IBuffStat<T> buffStat,
        T target
    )
    {
        if (buffStat == null)
            return;

        if (target == null)
            return;

        buffStat.ApplyTo(target);
    }

    public string GetSummaryText()
    {
        string result = "";

        AppendSummary(ref result, attackBuffStat);
        AppendSummary(ref result, buffInfoBuffStat);

        if (string.IsNullOrEmpty(result))
            result = "스탯 변화 없음";

        return result;
    }

    private void AppendSummary<T>(
        ref string result,
        IBuffStat<T> buffStat
    )
    {
        if (buffStat == null)
            return;

        string text = buffStat.GetSummaryText();

        if (string.IsNullOrEmpty(text))
            return;

        if (!string.IsNullOrEmpty(result))
            result += " / ";

        result += text;
    }
}