using UnityEngine;

[CreateAssetMenu(fileName = "HasBuffCondition", menuName = "GameData/Item/Condition/Has Buff")]
public class HasBuffConditionData : ItemEffectConditionData
{
    public PlayerStatusList targetStatus;
    public override bool IsSatisfied(ItemEffectContext context)
    {
        return StatusManager.Instance.HasStatus(targetStatus);
    }
}