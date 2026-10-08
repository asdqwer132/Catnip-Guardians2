using System.Collections.Generic;
using UnityEngine;

public enum AreaQueryPosition { ImpactPosition, OwnerPosition, HitPosition, Anywhere }
public enum AreaOwnerFilter { Own, Allied, Any }

public static class AreaRegistry
{
    private static readonly List<ReactiveGroundArea> areas = new List<ReactiveGroundArea>();
    internal static void Register(ReactiveGroundArea area) { if (!areas.Contains(area)) areas.Add(area); }
    internal static void Unregister(ReactiveGroundArea area) => areas.Remove(area);

    public static void Find(AreaDefinition definition, ItemEffectContext context,
        AreaQueryPosition position, AreaOwnerFilter ownerFilter, List<ReactiveGroundArea> results)
    {
        if (results == null) return;
        results.Clear();
        if (definition == null || context == null || !context.CanContinue) return;
        if (position == AreaQueryPosition.OwnerPosition && context.owner == null) return;
        Vector3 point = position == AreaQueryPosition.OwnerPosition ? context.owner.transform.position : context.targetPosition;
        for (int i = areas.Count - 1; i >= 0; i--)
        {
            ReactiveGroundArea area = areas[i];
            if (area == null) { areas.RemoveAt(i); continue; }
            if (!area.IsRegistered || area.Definition != definition) continue;
            if (ownerFilter == AreaOwnerFilter.Own && (context.owner == null || area.owner != context.owner)) continue;
            if (ownerFilter == AreaOwnerFilter.Allied && !AreaOwnerIdentity.AreAllied(area.owner, context.owner)) continue;
            if (position != AreaQueryPosition.Anywhere && !area.Contains(point)) continue;
            results.Add(area);
        }
        // 등록 순서대로 조회한 첫 장판을 고릅니다.
        results.Reverse();
    }
}
