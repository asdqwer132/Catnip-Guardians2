using UnityEngine;

[CreateAssetMenu(fileName = "ExecuteHitEffect", menuName = "GameData/Items/Hit Effects/Execute")]
public class ExecuteHitEffectData : HitEffectData
{
    [Range(0f, 1f)] public float threshold = 0.1f;
    public bool respectExecutionImmunity = true;
    public bool allowRestrictedTargets;
    [Tooltip("처형 피해의 방어/보호막/피해 면역 정책입니다. 처치 허용은 항상 켜집니다.")]
    public HealthDamagePolicy damagePolicy = new HealthDamagePolicy { bypassDefense = true, bypassShield = true };

    protected override bool ApplyEffect(HitEffectContext context)
    {
        Health health = context.target != null ? context.target.health : null;
        if (health == null || health.IsDead || health.MaxHp <= 0f ||
            (respectExecutionImmunity && health.executionImmune) ||
            (!allowRestrictedTargets && health.executionRestricted)) return false;
        float limit = EffectStatUtility.Safe(threshold, 0f, 1f, 0.1f);
        if (health.Hp / health.MaxHp > limit) return false;
        HealthDamagePolicy policy = damagePolicy != null ? damagePolicy.Clone() : new HealthDamagePolicy();
        policy.canKill = true;
        policy.sourceContext = context.CreateItemContext();
        health.ApplyDamage(health.Hp, policy);
        return health.IsDead;
    }
}
