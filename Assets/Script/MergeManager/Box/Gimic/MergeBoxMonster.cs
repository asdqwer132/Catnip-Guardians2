using UnityEngine;

public sealed class MergeBoxMonster : MergeBoxGimic
{
    public Collider2D monsterSpawnArea;
    public Collider2D quarantineArea;
    public WeightedMergeItem[] monsters = System.Array.Empty<WeightedMergeItem>();
    protected override void EnsureSkills() => SetDefaultSkills(new GimicSkill { id = "Monster.Spawn", type = GimicSkillType.Passive, defaultSkillCool = 10f });
    protected override void OnPassiveSkill(int index)
    { if (Board is MonsterMergeGameManager monster) monster.TrySpawnMonster(monsters, monsterSpawnArea); }
    public override void GimicStop()
    { if (Board is MonsterMergeGameManager monster) monster.ClearMonsters(); base.GimicStop(); }
    protected override void TickGimic(float deltaTime)
    { if (Board is MonsterMergeGameManager monster) monster.TickMonsters(quarantineArea); }
}
