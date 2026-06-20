using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyPatternPickGroup
{
    None,
    Random1,
    Random2,
    Reactive,
    Death
}

public enum EnemyPatternConditionType
{
    Always,
    HpRatioBelow,
    HpRatioAbove,
    DistanceToTargetLess,
    DistanceToTargetGreater,
    AfterDamaged,
    OnLethalDamage
}

public enum EnemyPatternPointType
{
    Self,
    Target,
    InFrontOfSelf,
    RandomAroundTarget,
    RandomAroundSelf
}

public enum EnemyPatternMoveCurve
{
    Linear,
    EaseInOut
}

[Serializable]
public class EnemyPatternEntry
{
    [Header("Basic")]
    public string patternName;
    [TextArea] public string memo;
    public bool enabled = true;
    public EnemyPatternPickGroup pickGroup = EnemyPatternPickGroup.Random1;
    public bool blockDefaultAI = true;

    [Header("Pick")]
    [Min(0f)] public float weight = 1f;
    [Range(0f, 1f)] public float chance = 1f;
    [Min(0f)] public float cooldown = 0f;
    public bool consumeOnce = false;

    [Header("Conditions")]
    public List<EnemyPatternCondition> conditions = new List<EnemyPatternCondition>();

    [Header("Actions")]
    public List<EnemyPatternAction> actions = new List<EnemyPatternAction>();

    public bool HasCondition(EnemyPatternConditionType conditionType)
    {
        if (conditions == null)
            return false;

        for (int i = 0; i < conditions.Count; i++)
        {
            if (conditions[i] != null && conditions[i].conditionType == conditionType)
                return true;
        }

        return false;
    }
}

[Serializable]
public class EnemyPatternCondition
{
    public EnemyPatternConditionType conditionType = EnemyPatternConditionType.Always;

    [Header("Value")]
    [Range(0f, 1f)] public float hpRatio = 0.5f;
    [Min(0f)] public float distance = 3f;

    public bool Check(EnemyPatternContext context)
    {
        if (context == null)
            return false;

        switch (conditionType)
        {
            case EnemyPatternConditionType.Always:
                return true;

            case EnemyPatternConditionType.HpRatioBelow:
                return context.GetHpRatio() <= hpRatio;

            case EnemyPatternConditionType.HpRatioAbove:
                return context.GetHpRatio() >= hpRatio;

            case EnemyPatternConditionType.DistanceToTargetLess:
                return context.GetDistanceToTarget() <= distance;

            case EnemyPatternConditionType.DistanceToTargetGreater:
                return context.GetDistanceToTarget() >= distance;

            case EnemyPatternConditionType.AfterDamaged:
                return true;

            case EnemyPatternConditionType.OnLethalDamage:
                return true;
        }

        return false;
    }
}
