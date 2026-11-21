using System;
using UnityEngine;

// Stage information has no item effects, rarity or crafting behaviour.
[CreateAssetMenu(fileName = "Stage Codex", menuName = "Upgrade/Codex/Stage")]
public sealed class StageCodexData : UnlockableDefaultData
{
    [Space(10)]
    public MapCodexData map;
    [Min(1)] public int stageNumber = 1;
    public Sprite preview;
    public EnemyDataSet[] encounters = Array.Empty<EnemyDataSet>();
    public Cost[] entryCosts = Array.Empty<Cost>();
    public Cost[] currencyRewards = Array.Empty<Cost>();
    public InventoryItem[] itemRewards = Array.Empty<InventoryItem>();

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(dataId))
            dataId = "Codex/Stage." + dataNumbering + "." + name;
    }
}
