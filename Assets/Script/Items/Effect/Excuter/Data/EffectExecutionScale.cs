using UnityEngine;

// 배율을 적용할 의미가 있는 필드만 각 스탯에서 명시한다. 횟수/확률/간격은 자동으로 바꾸지 않는다.
public interface IEffectScalableStat
{
    void ApplyExecutionScale(EffectExecutionScale scale);
}

public struct EffectExecutionScale
{
    public float Damage, Healing, Range, Duration;
    public float? DamageOverride;
    public EffectExecutionScale(ItemEffectContext context)
    {
        Damage = Safe(context.damageMultiplier);
        Healing = Safe(context.healingMultiplier);
        Range = Safe(context.rangeMultiplier);
        Duration = Safe(context.durationMultiplier);
        DamageOverride = context.damageOverride.HasValue
            ? (float?)EffectStatUtility.Safe(context.damageOverride.Value, 0f, 1000000f, 0f) : null;
    }
    public static float Safe(float value) => EffectStatUtility.Safe(value, 0f, 100f, 1f);
    public float ScaleDamage(float value) => (DamageOverride ?? value) * Damage;
}

public static class EffectExecutionScaling
{
    public static T Apply<T>(T snapshot, ItemEffectContext context) where T : class, IGameStat<T>
    {
        if (snapshot == null) return null;
        T result = snapshot.Clone();
        IEffectScalableStat scalable = result as IEffectScalableStat;
        if (scalable != null) scalable.ApplyExecutionScale(new EffectExecutionScale(context));
        result.Clamp();
        return result;
    }
}
