using System;
using System.Collections.Generic;
using UnityEngine;

public enum HealthTargetMode { Plant, Owner, HitTarget, AlliedSummons, Enemies, AllAllies }
public enum HealthTargetCenter { TargetPosition, UsePosition, Owner }
public enum HealthTargetShape { Circle, DirectionalRectangle }

[Serializable]
public class HealthTargetSettings
{
    public HealthTargetMode target = HealthTargetMode.Plant;
    public bool useRadius;
    [Min(0f)] public float radius = 5f;
    public HealthTargetCenter center;
    public HealthTargetShape shape;
    [Min(0f)] public float width = 0.4f;
    [Min(0f)] public float length = 5f;
    public bool sameOwnerOnly = true;
    [Range(1, 512)] public int maximumTargets = 128;

    public void Resolve(ItemEffectContext context, List<Health> results)
    {
        results.Clear();
        if (context == null || !context.CanContinue) return;
        if (target == HealthTargetMode.Plant)
        {
            Plant plant = PlantManager.instance != null ? PlantManager.instance.plant : null;
            Add(plant != null ? plant.health : null, context, results);
        }
        else if (target == HealthTargetMode.Owner)
            Add(context.owner != null ? context.owner.GetComponentInChildren<Health>() : null, context, results);
        else if (target == HealthTargetMode.HitTarget)
        {
            Enemy enemy = context.hitTarget;
            if (enemy != null && enemy.HitEffectLifeId == context.hitTargetLifeId)
                Add(enemy.health, context, results);
        }
        else if (target == HealthTargetMode.AlliedSummons)
        {
            IReadOnlyList<SummonItemThrower> summons = SummonRegistry.Active;
            for (int i = 0; i < summons.Count && results.Count < Mathf.Clamp(maximumTargets, 1, 512); i++)
            {
                SummonItemThrower summon = summons[i];
                if (summon == null || !summon.CanAct || (sameOwnerOnly && summon.Owner != context.owner)) continue;
                Add(summon.Health, context, results);
            }
        }
        else
        {
            IReadOnlyList<Health> active = Health.Active;
            for (int i = 0; i < active.Count && results.Count < Mathf.Clamp(maximumTargets, 1, 512); i++)
            {
                Health health = active[i];
                if (health == null) continue;
                bool enemy = health.team == HealthTeam.Enemy || health.GetComponentInParent<Enemy>() != null;
                bool ally = health.team == HealthTeam.Ally || health.GetComponentInParent<Plant>() != null;
                if ((target == HealthTargetMode.Enemies && enemy) || (target == HealthTargetMode.AllAllies && ally))
                    Add(health, context, results);
            }
        }
    }

    private void Add(Health health, ItemEffectContext context, List<Health> results)
    {
        if (health == null || health.IsDead || !health.isActiveAndEnabled || results.Contains(health) ||
            results.Count >= Mathf.Clamp(maximumTargets, 1, 512)) return;
        if (useRadius)
        {
            Vector3 origin = center == HealthTargetCenter.UsePosition ? context.usePosition : context.targetPosition;
            if (center == HealthTargetCenter.Owner && context.owner != null) origin = context.owner.transform.position;
            Vector3 offset = health.transform.position - origin;
            offset.z = 0f;
            float scale = EffectExecutionScale.Safe(context.rangeMultiplier);
            if (shape == HealthTargetShape.DirectionalRectangle)
            {
                Vector3 forward;
                if (!context.TryGetDirection(out forward)) return;
                float along = Vector3.Dot(offset, forward);
                Vector3 side = new Vector3(-forward.y, forward.x, 0f);
                if (along < 0f || along > EffectStatUtility.Safe(length, 0f, 10000f, 5f) * scale ||
                    Mathf.Abs(Vector3.Dot(offset, side)) > EffectStatUtility.Safe(width, 0f, 10000f, 0.4f) * scale * 0.5f) return;
            }
            else
            {
                float range = EffectStatUtility.Safe(radius, 0f, 10000f, 5f) * scale;
                if (offset.sqrMagnitude > range * range) return;
            }
        }
        results.Add(health);
    }
}

public enum HealthAmountMode { Fixed, CurrentHpPercent, MaxHpPercent }
public enum HealthChangeKind { Heal, Damage, Cost }

public static class HealthAmountUtility
{
    public static float Resolve(float amount, HealthAmountMode mode, Health health)
    {
        amount = EffectStatUtility.Safe(amount, 0f, 100000000f, 0f);
        if (health == null) return 0f;
        if (mode == HealthAmountMode.CurrentHpPercent) return health.Hp * amount / 100f;
        if (mode == HealthAmountMode.MaxHpPercent) return health.MaxHp * amount / 100f;
        return amount;
    }
}
