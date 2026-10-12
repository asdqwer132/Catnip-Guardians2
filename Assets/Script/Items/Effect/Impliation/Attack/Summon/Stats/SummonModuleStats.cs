using System;
using UnityEngine;

// 기본 수치는 모듈의 프로필에 저장하고, 버프는 복제한 런타임 수치에만 적용합니다.
public abstract class SummonModuleStat<T> : IGameStat<T>, IEffectScalableStat where T : class, IGameStat<T>
{
    public T Clone() => (T)(object)MemberwiseClone();
    public abstract void Clamp();
    public virtual void ApplyExecutionScale(EffectExecutionScale scale) { }
}

[Serializable]
public sealed class SummonContactDamageStat : SummonModuleStat<SummonContactDamageStat>
{
    [Header("피해")]
    public float damage = 10f;
    [Header("접촉 범위")]
    public float radius = 0.3f;
    [Header("피해 주기")]
    public float interval = 0.5f;
    public override void Clamp()
    {
        damage = EffectStatUtility.Safe(damage, 0f, 1000000f, 10f);
        radius = EffectStatUtility.Safe(radius, 0.01f, 100f, 0.3f);
        interval = EffectStatUtility.Safe(interval, 0.01f, 60f, 0.5f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        radius *= scale.Range;
    }
}

[Serializable]
public sealed class SummonContactEffectStat : SummonModuleStat<SummonContactEffectStat>
{
    [Header("접촉 범위")]
    public float radius = 0.3f;
    [Header("반복 주기")]
    public float repeatInterval = 0.5f;
    [Header("횟수 제한")]
    public float activationCount = 1f;
    public override void Clamp()
    {
        radius = EffectStatUtility.Safe(radius, 0.01f, 100f, 0.3f);
        repeatInterval = EffectStatUtility.Safe(repeatInterval, 0.01f, 60f, 0.5f);
        activationCount = EffectStatUtility.Safe(activationCount, 1f, 100000f, 1f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        radius *= scale.Range;
    }
}

[Serializable]
public sealed class SummonTurretStat : SummonModuleStat<SummonTurretStat>
{
    [Header("피해")]
    public float damage = 10f;
    [Header("탐색 범위")]
    public float range = 5f;
    [Header("발사 주기")]
    public float interval = 0.5f;
    [Header("탄환 이동")]
    public float projectileSpeed = 10f;
    [Header("탄환 수명")]
    public float projectileLifetime = 3f;
    [Header("탄환 충돌 범위")]
    public float projectileRadius = 0.1f;
    public override void Clamp()
    {
        damage = EffectStatUtility.Safe(damage, 0f, 1000000f, 10f);
        range = EffectStatUtility.Safe(range, 0.01f, 100f, 5f);
        interval = EffectStatUtility.Safe(interval, 0.01f, 60f, 0.5f);
        projectileSpeed = EffectStatUtility.Safe(projectileSpeed, 0.01f, 1000f, 10f);
        projectileLifetime = EffectStatUtility.Safe(projectileLifetime, 0.01f, 60f, 3f);
        projectileRadius = EffectStatUtility.Safe(projectileRadius, 0.01f, 100f, 0.1f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        range *= scale.Range;
        projectileRadius *= scale.Range;
        projectileLifetime *= scale.Duration;
    }
}

[Serializable]
public sealed class SummonItemThrowStat : SummonModuleStat<SummonItemThrowStat>
{
    [Header("피해 덮어쓰기")]
    public float damage = 10f;
    [Header("피해 배율")]
    public float damageMultiplier = 1f;
    [Header("회복 배율")]
    public float healingMultiplier = 1f;
    [Header("탐색 범위")]
    public float range = 5f;
    [Header("투척 주기")]
    public float interval = 1f;
    [Header("투척 개수")]
    public float shotCount = 1f;
    [Header("투척 배치")]
    public float spreadRadius = 1f;
    [Header("투척 각도")]
    public float spreadAngle = 30f;
    [Header("직선 배치 간격")]
    public float forwardSpacing = 1f;
    public float sideSpacing = 1f;
    public override void Clamp()
    {
        damage = EffectStatUtility.Safe(damage, 0f, 1000000f, 10f);
        damageMultiplier = EffectStatUtility.Safe(damageMultiplier, 0f, 1000f, 1f);
        healingMultiplier = EffectStatUtility.Safe(healingMultiplier, 0f, 1000f, 1f);
        range = EffectStatUtility.Safe(range, 0.01f, 100f, 5f);
        interval = EffectStatUtility.Safe(interval, 0.01f, 60f, 1f);
        shotCount = EffectStatUtility.Safe(shotCount, 1f, 128f, 1f);
        spreadRadius = EffectStatUtility.Safe(spreadRadius, 0f, 100f, 1f);
        spreadAngle = EffectStatUtility.Safe(spreadAngle, 0f, 360f, 30f);
        forwardSpacing = EffectStatUtility.Safe(forwardSpacing, 0f, 100f, 1f);
        sideSpacing = EffectStatUtility.Safe(sideSpacing, 0f, 100f, 1f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        range *= scale.Range;
        spreadRadius *= scale.Range;
        forwardSpacing *= scale.Range;
        sideSpacing *= scale.Range;
    }
}

[Serializable]
public sealed class SummonOrbitStat : SummonModuleStat<SummonOrbitStat>
{
    [Header("공전 반경")]
    public float radius = 2f;
    [Header("공전 속도")]
    public float angularSpeed = 90f;
    [Header("시작 각도")]
    public float initialAngle = 0f;
    public override void Clamp()
    {
        radius = EffectStatUtility.Safe(radius, 0f, 100f, 2f);
        angularSpeed = EffectStatUtility.Safe(angularSpeed, -3600f, 3600f, 90f);
        initialAngle = EffectStatUtility.Safe(initialAngle, -360f, 360f, 0f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        radius *= scale.Range;
    }
}

[Serializable]
public sealed class SummonPullStat : SummonModuleStat<SummonPullStat>
{
    [Header("적 탐색 범위")]
    public float range = 5f;
    [Header("당김 속도")]
    public float speed = 5f;
    [Header("갱신 주기")]
    public float tickInterval = 0.05f;
    [Header("중심 정지 거리")]
    public float stopDistance = 0.1f;
    public override void Clamp()
    {
        range = EffectStatUtility.Safe(range, 0.01f, 100f, 5f);
        speed = EffectStatUtility.Safe(speed, 0f, 1000f, 5f);
        tickInterval = EffectStatUtility.Safe(tickInterval, 0.01f, 1f, 0.05f);
        stopDistance = EffectStatUtility.Safe(stopDistance, 0f, 100f, 0.1f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        range *= scale.Range;
        stopDistance *= scale.Range;
    }
}

[Serializable]
public sealed class SummonAuraStat : SummonModuleStat<SummonAuraStat>
{
    [Header("오라 범위")]
    public float range = 5f;
    [Header("대상 갱신 주기")]
    public float scanInterval = 0.1f;
    [Header("최대 대상 수")]
    public float maximumTargets = 128f;
    public override void Clamp()
    {
        range = EffectStatUtility.Safe(range, 0.01f, 100f, 5f);
        scanInterval = EffectStatUtility.Safe(scanInterval, 0.01f, 10f, 0.1f);
        maximumTargets = EffectStatUtility.Safe(maximumTargets, 1f, 512f, 128f);
    }
    public override void ApplyExecutionScale(EffectExecutionScale scale)
    {
        range *= scale.Range;
    }
}

[Serializable]
public sealed class SummonPeriodicSpawnStat : SummonModuleStat<SummonPeriodicSpawnStat>
{
    [Header("소환 주기")]
    public float interval = 1f;
    [Header("횟수 제한")]
    public float activationCount = 1f;
    public override void Clamp()
    {
        interval = EffectStatUtility.Safe(interval, 0.01f, 600f, 1f);
        activationCount = EffectStatUtility.Safe(activationCount, 1f, 100000f, 1f);
    }
}

[Serializable]
public sealed class SummonHealthStat : SummonModuleStat<SummonHealthStat>
{
    [Header("최대 체력")]
    public float maxHealth = 10f;
    [Header("방어력")]
    public float defense = 0f;
    public override void Clamp()
    {
        maxHealth = EffectStatUtility.Safe(maxHealth, 1f, 1000000f, 10f);
        defense = EffectStatUtility.Safe(defense, 0f, 1000000f, 0f);
    }
}
