using System;
using System.Collections.Generic;
using UnityEngine;

public enum SummonOwnerScope { SameOwner, AllOwners }
public enum SummonQueryOrigin { TargetPosition, UsePosition }

[Serializable]
public sealed class SummonSelection
{
    public SummonOwnerScope ownerScope = SummonOwnerScope.SameOwner;
    [Tooltip("비우면 모든 종류. 여러 종류는 OR로 조회합니다.")]
    public SummonDefinition[] definitions;
    [Tooltip("지정한 태그를 모두 가진 소환물만 선택합니다.")]
    public string[] requiredTags;
    public bool limitRadius;
    [Min(0f)] public float radius = 5f;
    public SummonQueryOrigin origin;
    [Range(1, 512)] public int maximumTargets = 128;

    public bool Matches(SummonItemThrower summon, ItemEffectContext context)
    {
        if (summon == null || !summon.CanAct || context == null) return false;
        if (ownerScope == SummonOwnerScope.SameOwner && summon.Owner != context.owner) return false;
        if (definitions != null && definitions.Length > 0)
        {
            bool found = false;
            foreach (SummonDefinition definition in definitions)
                if (definition != null && summon.Definition == definition) { found = true; break; }
            if (!found) return false;
        }
        if (requiredTags != null)
            foreach (string tag in requiredTags)
                if (!string.IsNullOrWhiteSpace(tag) && (summon.Definition == null || !summon.Definition.HasTag(tag))) return false;
        Vector3 center = origin == SummonQueryOrigin.UsePosition ? context.usePosition : context.targetPosition;
        float safeRadius = EffectStatUtility.Safe(radius, 0f, 1000f, 5f);
        return !limitRadius || ((Vector2)(summon.transform.position - center)).sqrMagnitude <= safeRadius * safeRadius;
    }
}

// 활성 소환물만 추적하며 풀에서 새로 초기화된 소환물은 LifeId가 달라진다.
public static class SummonRegistry
{
    private static readonly List<SummonItemThrower> active = new List<SummonItemThrower>();
    public static IReadOnlyList<SummonItemThrower> Active => active;
    internal static void Register(SummonItemThrower summon)
    {
        if (summon != null && !active.Contains(summon)) active.Add(summon);
    }
    internal static void Unregister(SummonItemThrower summon) => active.Remove(summon);

    public static void Collect(SummonSelection selection, ItemEffectContext context, List<SummonItemThrower> results)
    {
        results.Clear();
        if (selection == null || context == null || !context.CanContinue) return;
        int maximum = Mathf.Clamp(selection.maximumTargets, 1, 512);
        for (int i = 0; i < active.Count && results.Count < maximum; i++)
            if (selection.Matches(active[i], context)) results.Add(active[i]);
    }
}
