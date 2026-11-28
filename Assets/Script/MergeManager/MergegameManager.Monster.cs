using System.Collections.Generic;
using UnityEngine;


public partial class MergegameManager
{
    private MergeItem pendingMonster;
    private Vector3 pendingPosition;
    public bool TrySpawnMonster(WeightedMergeItem[] monsters, Collider2D area)
    {
        if (!CanOperate || area == null || !area.isActiveAndEnabled) return false;
        if (pendingMonster == null)
        {
            var definition = MergeGimmickUtility.ChooseItem(monsters);
            if (definition == null || definition.prefab == null || definition.prefab.GetComponent<MonsterMergeItemObject>() == null ||
                !MergeGimmickUtility.TryPoint(area, out pendingPosition)) return false;
            pendingMonster = definition;
        }
        if (!TrySpawnGimmickItem(pendingMonster, pendingPosition, out _)) return false;
        pendingMonster = null; return true;
    }
    public void ClearMonsters()
    { pendingMonster = null; foreach (var piece in new List<MergeItemObject>(ActiveItems)) if (piece is MonsterMergeItemObject && piece.IsLive) piece.ReturnToPool(); }
    public void TickMonsters(Collider2D quarantine)
    {
        // Eating/quarantine can return objects and alter ActiveItems during iteration.
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
            if (piece is MonsterMergeItemObject monster && monster.IsLive) monster.TickMonster(quarantine);
    }
}
