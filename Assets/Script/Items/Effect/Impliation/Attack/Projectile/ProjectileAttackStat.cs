using System;

[Serializable]
public sealed class ProjectileAttackStat : IGameStat<ProjectileAttackStat>, IEffectScalableStat
{
    public float projectileDamage = 8f;
    public float projectileSpeed = 8f;
    public float projectileDistance = 8f;
    public float projectileLifetime = 3f;
    public float projectileHitRadius = 0.2f;
    public float projectileRehitInterval = 0.3f;
    [UnityEngine.Tooltip("총 명중 수. 0이면 제한 없음, 1이면 첫 적에게 맞고 종료합니다.")]
    public float projectileMaxHits;
    public float projectileOrbitRadius = 2f;
    public ProjectileAttackStat Clone() => (ProjectileAttackStat)MemberwiseClone();
    public void Clamp()
    {
        projectileDamage = EffectStatUtility.Safe(projectileDamage, 0f, 1000000f, 8f);
        projectileSpeed = EffectStatUtility.Safe(projectileSpeed, 0.01f, 1000f, 8f);
        projectileDistance = EffectStatUtility.Safe(projectileDistance, 0.01f, 100000f, 8f);
        projectileLifetime = EffectStatUtility.Safe(projectileLifetime, 0.01f, 3600f, 3f);
        projectileHitRadius = EffectStatUtility.Safe(projectileHitRadius, 0.001f, 1000f, 0.2f);
        projectileRehitInterval = EffectStatUtility.Safe(projectileRehitInterval, 0.01f, 3600f, 0.3f);
        projectileMaxHits = EffectStatUtility.Safe(projectileMaxHits, 0f, 4096f, 0f);
        projectileOrbitRadius = EffectStatUtility.Safe(projectileOrbitRadius, 0.01f, 100000f, 2f);
    }
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        projectileDamage = scale.ScaleDamage(projectileDamage);
        projectileDistance *= scale.Range;
        projectileHitRadius *= scale.Range;
        projectileOrbitRadius *= scale.Range;
        projectileLifetime *= scale.Duration;
    }
}
