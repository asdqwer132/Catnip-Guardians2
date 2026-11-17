using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class MonsterMergeGameManager : MergegameManager
{
    public bool TrySpawnMonster(WeightedMergeItem[] monsters, Collider2D area)
    {
        var definition = MergeGimmickUtility.ChooseItem(monsters);
        if (definition == null || definition.prefab == null || definition.prefab.GetComponent<MonsterMergeItemObject>() == null ||
            !MergeGimmickUtility.TryPoint(area, out var position)) return false;
        return TrySpawnGimmickItem(definition, position, out _);
    }
    public void ClearMonsters()
    { foreach (var piece in new List<MergeItemObject>(ActiveItems)) if (piece is MonsterMergeItemObject && piece.IsLive) piece.ReturnToPool(); }
    public void TickMonsters(Collider2D quarantine)
    {
        // Eating/quarantine can return objects and alter ActiveItems during iteration.
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
            if (piece is MonsterMergeItemObject monster && monster.IsLive) monster.TickMonster(quarantine);
    }
}
