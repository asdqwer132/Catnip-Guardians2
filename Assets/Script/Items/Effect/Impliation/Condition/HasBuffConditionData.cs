using UnityEngine;

[CreateAssetMenu(fileName = "HasBuffCondition", menuName = "04.Items/Conditions/Has Buff")]
public class HasBuffConditionData : ItemEffectConditionData
{
    public PlayerStatusList targetStatus;
    public override bool IsSatisfied(ItemEffectContext context)
    {
        return StatusManager.Instance != null && StatusManager.Instance.HasStatus(targetStatus);
    }
}
