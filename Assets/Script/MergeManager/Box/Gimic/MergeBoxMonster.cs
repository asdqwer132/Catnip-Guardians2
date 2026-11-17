using UnityEngine;

public sealed class MergeBoxMonster : MergeBoxGimic
{
    private bool spawnPending;
    private float retryAt;
    public Collider2D monsterSpawnArea;
    public Collider2D quarantineArea;
    public WeightedMergeItem[] monsters = System.Array.Empty<WeightedMergeItem>();
    protected override void EnsureSkills() => SetDefaultSkills(new GimicSkill { id = "Monster.Spawn", type = GimicSkillType.Passive, defaultSkillCool = 10f });
    protected override void OnPassiveSkill(int index)
    { if (!spawnPending && Board is MonsterMergeGameManager monster) { spawnPending = !monster.TrySpawnMonster(monsters, monsterSpawnArea); retryAt = Time.time + Mathf.Max(.01f, monster.spawnRetryInterval); } }
    public override void GimicStop()
    { spawnPending = false; if (Board is MonsterMergeGameManager monster) monster.ClearMonsters(); base.GimicStop(); }
    protected override void TickGimic(float deltaTime)
    {
        if (!(Board is MonsterMergeGameManager monster)) return;
        monster.TickMonsters(quarantineArea);
        if (spawnPending && Time.time >= retryAt)
        { spawnPending = !monster.TrySpawnMonster(monsters, monsterSpawnArea); retryAt = Time.time + Mathf.Max(.01f, monster.spawnRetryInterval); }
    }
}
