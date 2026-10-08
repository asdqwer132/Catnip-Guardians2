using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HealthChangeStat : IGameStat<HealthChangeStat>
{
    [Min(0f)] public float healthChangeAmount = 1f;
    public HealthChangeStat Clone() => (HealthChangeStat)MemberwiseClone();
    public void Clamp() { healthChangeAmount = EffectStatUtility.Safe(healthChangeAmount, 0f, 100000000f, 1f); }
}

[CreateAssetMenu(fileName = "HealthChange", menuName = "GameData/Items/Effects/Health/Health Change")]
public class HealthChangeEffect : ItemEffectData
{
    public HealthTargetSettings targets = new HealthTargetSettings();
    public HealthChangeKind kind;
    public HealthAmountMode amountMode;
    public HealthChangeStat healthStat = new HealthChangeStat();
    [Tooltip("피해와 비용의 처치 허용, 방어/보호막/면역 무시 정책을 명시합니다.")]
    public HealthDamagePolicy damagePolicy = new HealthDamagePolicy();
    [Tooltip("체력 비용에도 실행의 피해 배율을 적용할 때만 켭니다.")]
    public bool scaleCostsWithDamage;

    public override void Prepare(ItemEffectContext context) { context.GetSnapshotStat(this, healthStat); }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || targets == null) return;
        HealthChangeStat current = context.GetCurrentStat(this, healthStat);
        if (current == null) return;
        current.Clamp();
        List<Health> resolved = new List<Health>();
        targets.Resolve(context, resolved);
        HealthDamagePolicy policy = damagePolicy != null ? damagePolicy.Clone() : new HealthDamagePolicy();
        policy.sourceContext = context.Copy(context.targetPosition, context.direction);
        policy.sourceContext = context;
        for (int i = 0; i < resolved.Count && context.CanContinue; i++)
        {
            Health health = resolved[i];
            float amount = HealthAmountUtility.Resolve(current.healthChangeAmount, amountMode, health);
            if (kind == HealthChangeKind.Heal)
                health.ApplyHealing(amount * EffectExecutionScale.Safe(context.healingMultiplier));
            else
            {
                if (kind == HealthChangeKind.Damage || scaleCostsWithDamage)
                {
                    if (kind == HealthChangeKind.Damage && amountMode == HealthAmountMode.Fixed && context.damageOverride.HasValue)
                        amount = EffectStatUtility.Safe(context.damageOverride.Value, 0f, 100000000f, 0f);
                    amount *= EffectExecutionScale.Safe(context.damageMultiplier);
                }
                health.ApplyDamage(amount, policy);
            }
        }
    }
}
