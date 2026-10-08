using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SummonTransformRequirement
{
    public SummonDefinition definition;
    [Range(1, 128)] public int count = 1;
}

[CreateAssetMenu(fileName = "SummonTransformEffect", menuName = "GameData/Items/Effects/Summon/Transform")]
public sealed class SummonTransformEffect : ItemEffectData
{
    public SummonSelection selection = new SummonSelection();
    public SummonTransformRequirement[] requirements;
    public SummonAttackEffect result;
    [Tooltip("기획에서 소비가 확정된 경우에만 켭니다.")]
    public bool consumeParticipants;
    [Tooltip("소비하지 않는 재료도 같은 합체에 두 번 쓰지 않습니다. 반복 생성이 필요한 경우 끕니다.")]
    public bool requireFreshParticipants = true;

    public override void Prepare(ItemEffectContext context)
    {
        if (result != null) result.Prepare(context);
    }

    public override void ExecuteEffect(ItemEffectContext context) { TryTransform(context, out _); }

    public bool TryTransform(ItemEffectContext context, out SummonItemThrower created)
    {
        created = null;
        if (context == null || !context.CanContinue || result == null || !result.CanExecute(context) ||
            requirements == null || requirements.Length == 0 || requirements.Length > 64) return false;
        var candidates = new List<SummonItemThrower>();
        var selected = new List<SummonItemThrower>();
        SummonRegistry.Collect(selection, context, candidates);
        foreach (SummonTransformRequirement requirement in requirements)
        {
            if (requirement == null || requirement.definition == null) return false;
            int needed = Mathf.Clamp(requirement.count, 1, 128);
            foreach (SummonItemThrower summon in candidates)
            {
                if (summon.Definition != requirement.definition || selected.Contains(summon) || summon.IsTransformReserved ||
                    (requireFreshParticipants && summon.HasTransformed(this))) continue;
                selected.Add(summon);
                if (--needed == 0) break;
            }
            if (needed != 0 || selected.Count > 512) return false;
        }
        foreach (SummonItemThrower summon in selected) summon.IsTransformReserved = true;
        try
        {
            if (!result.TrySpawn(context, out created)) return false;
            foreach (SummonItemThrower summon in selected)
            {
                summon.RecordTransform(this);
                if (consumeParticipants) summon.Despawn(true);
            }
            return true;
        }
        finally
        {
            foreach (SummonItemThrower summon in selected)
                if (summon != null) summon.IsTransformReserved = false;
        }
    }
}
