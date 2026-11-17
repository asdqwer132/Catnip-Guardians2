using UnityEngine;

public sealed class MergeBoxMineral : MergeBoxGimic
{
    private bool spawnPending;
    private float retryAt;
    public Collider2D oreSpawnArea;
    public WeightedMergeItem[] ores = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int maximumOres = 5;
    protected override void EnsureSkills() => SetDefaultSkills(new GimicSkill { id = "Mineral.SpawnOre", type = GimicSkillType.Passive, defaultSkillCool = 8f });
    protected override void OnPassiveSkill(int index)
    { if (!spawnPending && Board is MineralMergeGameManager mineral) { spawnPending = !mineral.TrySpawnOre(ores, oreSpawnArea, maximumOres); retryAt = Time.time + Mathf.Max(.01f, mineral.spawnRetryInterval); } }
    protected override void TickGimic(float deltaTime)
    {
        if (spawnPending && Time.time >= retryAt && Board is MineralMergeGameManager mineral)
        { spawnPending = !mineral.TrySpawnOre(ores, oreSpawnArea, maximumOres); retryAt = Time.time + Mathf.Max(.01f, mineral.spawnRetryInterval); }
    }
    public override void GimicStop()
    { spawnPending = false; if (Board is MineralMergeGameManager mineral) mineral.ClearOres(); base.GimicStop(); }
}
