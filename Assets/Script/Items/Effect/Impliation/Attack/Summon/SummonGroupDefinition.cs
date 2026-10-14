using System;
using UnityEngine;

public enum SummonGroupRequirementMode { DistinctKinds, TotalInstances, Both }
public enum SummonGroupCountComparison { AtLeast, Exactly }
public enum SummonGroupTriggerMode { OnConditionReached, EveryMatchingSummon }

[Serializable]
public struct SummonGroupPopulation
{
    public int kindCount;
    public int instanceCount;
}

[Serializable]
public sealed class SummonGroupItemRule
{
    [Header("필드 조건")]
    public SummonGroupRequirementMode requirementMode = SummonGroupRequirementMode.Both;
    [Min(1)] public int requiredKinds = 3;
    [Min(1)] public int requiredInstances = 3;
    public SummonGroupCountComparison comparison;
    [Tooltip("기본은 조건 달성 시 한 번. 조건이 깨지면 다시 준비됩니다. Every Matching Summon은 조건을 만족한 각 새 소환에 반응합니다.")]
    public SummonGroupTriggerMode triggerMode;
    [Header("발동할 아이템")]
    [Tooltip("추가 몬스터를 만들려면 SummonSpawnEffect를 가진 ItemData를 연결합니다. 공격·버프·회복 아이템도 사용할 수 있습니다.")]
    public ItemData item;
    public ItemUseDeliverySettings itemUse = new ItemUseDeliverySettings();
    public bool consumeUseBuffs;

    public bool Matches(SummonGroupPopulation population)
    {
        bool kinds = Compare(population.kindCount, Mathf.Max(1, requiredKinds));
        bool instances = Compare(population.instanceCount, Mathf.Max(1, requiredInstances));
        return requirementMode == SummonGroupRequirementMode.DistinctKinds ? kinds :
            requirementMode == SummonGroupRequirementMode.TotalInstances ? instances : kinds && instances;
    }
    private bool Compare(int actual, int required)
        => comparison == SummonGroupCountComparison.Exactly ? actual == required : actual >= required;
}

[CreateAssetMenu(fileName = "SummonGroup", menuName = "04.Items/Effects/Direct Effects/Summons/Group")]
public sealed class SummonGroupDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("기본은 소유자마다 따로 셉니다. All Owners는 필드 전체의 같은 무리를 합산합니다.")]
    public SummonOwnerScope ownerScope = SummonOwnerScope.SameOwner;
    public SummonGroupItemRule[] itemRules;
}
