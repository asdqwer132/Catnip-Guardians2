using System;
using UnityEngine;

public sealed class MergeBoxPlant : MergeBoxGimic
{
    public Collider2D groundArea;
    [Tooltip("즉시 성장 스킬이 적용되는 구역입니다. 비워두면 Ground Area를 사용합니다.")]
    public Collider2D instantGrowthArea;
    public MergePlantGrowthRule[] growthRules = Array.Empty<MergePlantGrowthRule>();
    public Collider2D InstantGrowthArea => instantGrowthArea != null ? instantGrowthArea : groundArea;

    private static GimicSkill InstantGrowthSkill() => new GimicSkill
    {
        id = "Plant.InstantGrowth", type = GimicSkillType.Active, defaultSkillCool = 10f
    };

    protected override void EnsureSkills()
    {
        SetDefaultSkills(
            new GimicSkill { id = "Plant.RootAndGrow", type = GimicSkillType.Passive, defaultSkillCool = 0f },
            InstantGrowthSkill());
        // Preserve authored passive settings when upgrading an existing one-skill box.
        if (!Array.Exists(skills, skill => skill != null && skill.type == GimicSkillType.Active))
        {
            Array.Resize(ref skills, skills.Length + 1);
            skills[skills.Length - 1] = InstantGrowthSkill();
        }
    }

    protected override void TickGimic(float deltaTime)
    { if (Board is { } plant) plant.TickPlants(groundArea, growthRules, deltaTime); }
    protected override bool CanUseActiveSkill(int index)
        => Board != null && InstantGrowthArea != null && InstantGrowthArea.isActiveAndEnabled;
    protected override void OnActiveSkill(int index)
    { if (Board is { } plant) plant.InstantGrowInArea(InstantGrowthArea, growthRules); }

    public override void GimicStop()
    { if (Board is { } plant) plant.ReleaseRoots(); base.GimicStop(); }
}
