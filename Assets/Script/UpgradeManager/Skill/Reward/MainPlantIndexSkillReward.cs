using UnityEngine;

[CreateAssetMenu(fileName = "MainPlantIndexReward", menuName = "Upgrade/Skills/Rewards/Main Plant Index")]
public sealed class MainPlantIndexSkillReward : SkillRewardData
{
    [Min(0)] public int plantIndex;
    public override void Apply(SkillApplyContext context)
        => context?.mergeBoxUpgrades?.ApplyMainPlantIndex(plantIndex);
}
