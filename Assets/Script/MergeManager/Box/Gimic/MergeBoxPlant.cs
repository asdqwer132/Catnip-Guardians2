using UnityEngine;

public sealed class MergeBoxPlant : MergeBoxGimic
{
    public Collider2D groundArea;
    public MergePlantGrowthRule[] growthRules = System.Array.Empty<MergePlantGrowthRule>();
    // Contact/growth is continuous, so this passive has no periodic cooldown.
    protected override void EnsureSkills() => SetDefaultSkills(new GimicSkill { id = "Plant.RootAndGrow", type = GimicSkillType.Passive, defaultSkillCool = 0f });
    protected override void TickGimic(float deltaTime)
    { if (Board is PlantMergeGameManager plant) plant.TickPlants(groundArea, growthRules, deltaTime); }
    public override void GimicStop()
    { if (Board is PlantMergeGameManager plant) plant.ReleaseRoots(); base.GimicStop(); }
}
