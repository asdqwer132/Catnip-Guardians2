using UnityEngine;

public sealed class MergeBoxMineral : MergeBoxGimic
{
    public Collider2D oreSpawnArea;
    public WeightedMergeItem[] ores = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int maximumOres = 5;
    protected override void EnsureSkills() => SetDefaultSkills(new GimicSkill { id = "Mineral.SpawnOre", type = GimicSkillType.Passive, defaultSkillCool = 8f });
    protected override void OnPassiveSkill(int index)
    { if (Board is MineralMergeGameManager mineral) mineral.TrySpawnOre(ores, oreSpawnArea, maximumOres); }
    public override void GimicStop()
    { if (Board is MineralMergeGameManager mineral) mineral.ClearOres(); base.GimicStop(); }
}
