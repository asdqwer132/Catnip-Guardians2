using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffTargetAreaResolver", menuName = "GameData/Buff/Buff Target/Area")]
public class BuffTargetAreaResolver : BuffTargetResolver
{
    public float radius = 3f;
    public LayerMask layerMask = ~0;
    public string requiredGroup;
    public bool includeTriggers = true;

    private readonly Collider2D[] hits = new Collider2D[128];
    private readonly HashSet<Object> addedTargets = new HashSet<Object>();

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || results == null)
            return;

        addedTargets.Clear();

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useLayerMask = true;
        filter.useTriggers = includeTriggers;

        int count = Physics2D.OverlapCircle(context.targetPosition, radius, filter, hits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            IBuffTarget target = hit.GetComponentInParent<IBuffTarget>();

            if (target == null)
                continue;

            if (!string.IsNullOrEmpty(requiredGroup) && target.BuffTargetGroup != requiredGroup)
                continue;

            Object targetObject = target.BuffTargetObject;

            if (targetObject == null)
                continue;

            if (!addedTargets.Add(targetObject))
                continue;

            BuffTargetHandle handle = BuffTargetHandle.Target(target);

            if (handle != null)
                results.Add(handle);
        }

        addedTargets.Clear();
    }
}