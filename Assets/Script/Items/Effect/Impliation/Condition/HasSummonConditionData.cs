using UnityEngine;

[CreateAssetMenu(fileName = "HasSummonCondition", menuName = "GameData/Items/Conditions/Has Summon")]
public sealed class HasSummonConditionData : ItemEffectConditionData
{
    public SummonSelection selection = new SummonSelection();
    [Range(1, 512)] public int minimumCount = 1;
    public override bool IsSatisfied(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || selection == null) return false;
        int count = 0, minimum = Mathf.Clamp(minimumCount, 1, 512);
        foreach (SummonItemThrower summon in SummonRegistry.Active)
            if (selection.Matches(summon, context) && ++count >= minimum) return true;
        return false;
    }
}
