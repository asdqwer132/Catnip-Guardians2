using System;
using System.Reflection;
using UnityEngine;

public abstract class BuffModifier : ScriptableObject
{
    [Header("Optional Filter")]
    [Tooltip("비워두면 모든 스탯 타입에 적용 시도. 예: AttackStat, EnemyStat, EnemySpawnerStat, HealStat")]
    public string targetStatTypeName;

    public virtual bool CanApplyTo(object stat, BuffQueryContext query)
    {
        if (stat == null)
            return false;

        if (string.IsNullOrEmpty(targetStatTypeName))
            return true;

        Type type = stat.GetType();
        return type.Name == targetStatTypeName || type.FullName == targetStatTypeName;
    }

    public abstract void ApplyTo(object stat, int stack, BuffQueryContext query);

    // 기본 Modifier들은 기존 방식 그대로 더하기 단계에서 처리
    public virtual void ApplyAdditiveTo(object stat, int stack, BuffQueryContext query)
    {
        ApplyTo(stat, stack, query);
    }

    // 곱하기 단계는 필요한 Modifier만 override
    public virtual void ApplyMultiplicativeTo(object stat, int stack, BuffQueryContext query)
    {
    }

    protected void ClampIfPossible(object stat)
    {
        if (stat == null)
            return;

        MethodInfo method = stat.GetType().GetMethod("Clamp", BindingFlags.Public | BindingFlags.Instance);
        if (method != null)
            method.Invoke(stat, null);
    }
}