using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorTarget : MonoBehaviour
{
    private IDamageable target;
    private readonly List<TargetOverrideHandle> overrides = new List<TargetOverrideHandle>();
    private long nextOrder;
    [Header("Debug")]
    [SerializeField] private string targetName = "None";

    private IDamageable CurrentTarget
    {
        get
        {
            TargetOverrideHandle best = null;
            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                TargetOverrideHandle candidate = overrides[i];
                if (!candidate.IsValid) { overrides.RemoveAt(i); continue; }
                if (best == null || candidate.Priority > best.Priority ||
                    (candidate.Priority == best.Priority && candidate.Order > best.Order)) best = candidate;
            }
            return best != null ? best.Target : IsAlive(target) ? target : null;
        }
    }
    public Transform TargetTransform => CurrentTarget != null ? CurrentTarget.DamageTransform : null;
    public bool HasTarget => CurrentTarget != null;
    public IDamageable TargetDamageable => CurrentTarget;

    public void SetTarget(IDamageable newTarget)
    {
        target = newTarget;
        RefreshTargetName();
    }

    // 임시 타깃은 기본 타깃을 덮어쓰지 않는다. 각 효과는 자신의 핸들만 제거한다.
    public TargetOverrideHandle AddTargetOverride(IDamageable newTarget, int priority = 0)
    {
        if (!IsAlive(newTarget)) return null;
        TargetOverrideHandle handle = new TargetOverrideHandle(this, newTarget, priority, ++nextOrder);
        overrides.Add(handle);
        RefreshTargetName();
        return handle;
    }

    public void ClearTargetOverrides()
    {
        TargetOverrideHandle[] active = overrides.ToArray();
        overrides.Clear();
        for (int i = 0; i < active.Length; i++) active[i].Dispose();
        RefreshTargetName();
    }

    internal void RemoveTargetOverride(TargetOverrideHandle handle)
    {
        overrides.Remove(handle);
        RefreshTargetName();
    }

    internal static bool IsAlive(IDamageable candidate) => candidate != null &&
        (!(candidate is UnityEngine.Object) || (UnityEngine.Object)candidate != null) &&
        !candidate.IsDead && candidate.DamageTransform != null && candidate.DamageTransform.gameObject.activeInHierarchy;

    public float GetDistanceFrom(Transform origin)
    {
        if (!HasTarget || origin == null) return float.MaxValue;
        return Mathf.Sqrt(GetSqrDistanceFrom(origin));
    }

    public float GetSqrDistanceFrom(Transform origin)
    {
        Transform destination = TargetTransform;
        if (!HasTarget || origin == null || destination == null) return float.MaxValue;
        Vector2 difference = destination.position - origin.position;
        return difference.sqrMagnitude;
    }

    public void DamageTarget(float damage)
    {
        IDamageable current = CurrentTarget;
        if (current == null) { RefreshTargetName(); return; }
        current.TakeDamage(damage);
    }

    private void RefreshTargetName()
    {
        Transform destination = TargetTransform;
        targetName = destination != null ? destination.name : "None";
    }

    private void OnDisable() { ClearTargetOverrides(); }
#if UNITY_EDITOR
    private void OnValidate() { RefreshTargetName(); }
#endif
}

public sealed class TargetOverrideHandle : IDisposable
{
    private ActorTarget owner;
    private readonly Enemy enemyTarget;
    private readonly int enemyLifeId;
    private readonly Health healthTarget;
    private readonly int healthLifeId;
    private readonly SummonItemThrower summonTarget;
    private readonly int summonLifeId;
    internal IDamageable Target { get; private set; }
    internal int Priority { get; private set; }
    internal long Order { get; private set; }
    public bool IsValid => owner != null && ActorTarget.IsAlive(Target) &&
        (enemyTarget == null || enemyTarget.HitEffectLifeId == enemyLifeId) &&
        (healthTarget == null || healthTarget.LifeId == healthLifeId) &&
        (summonTarget == null || summonTarget.LifeId == summonLifeId);

    internal TargetOverrideHandle(ActorTarget owner, IDamageable target, int priority, long order)
    {
        this.owner = owner;
        Target = target;
        Priority = priority;
        Order = order;
        enemyTarget = target as Enemy;
        enemyLifeId = enemyTarget != null ? enemyTarget.HitEffectLifeId : 0;
        HealthActor actor = target as HealthActor;
        summonTarget = target as SummonItemThrower;
        summonLifeId = summonTarget != null ? summonTarget.LifeId : 0;
        healthTarget = actor != null ? actor.health : summonTarget != null ? summonTarget.Health : null;
        healthLifeId = healthTarget != null ? healthTarget.LifeId : 0;
    }

    public void Dispose()
    {
        ActorTarget current = owner;
        owner = null;
        if (current != null) current.RemoveTargetOverride(this);
    }
}
