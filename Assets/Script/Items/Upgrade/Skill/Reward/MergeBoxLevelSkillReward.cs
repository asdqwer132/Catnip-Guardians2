using UnityEngine;

[CreateAssetMenu(fileName = "MergeBoxLevelReward", menuName = "03.Upgrade/Skills/Rewards/Merge Box Level")]
public sealed class MergeBoxLevelSkillReward : SkillRewardData
{
    public ItemSeries series = ItemSeries.Weapon;
    [Min(1)] public int level = 2;

    public override void Apply(SkillApplyContext context)
    {
        if (context?.mergeBoxUpgrades != null) context.mergeBoxUpgrades.ApplyReward(series, level);
    }
}
