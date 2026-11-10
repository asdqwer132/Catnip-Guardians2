using System;
using UnityEngine;

// A stage is item-shaped display data; merely selecting it never starts a stage or uses an item.
[CreateAssetMenu(fileName = "Stage Codex", menuName = "Upgrade/Codex/Stage Item")]
public sealed class StageCodexData : ItemData
{
    [Header("Extra Data")]
    public MapCodexData map;
    [Min(1)] public int stageNumber = 1;
    public Sprite preview;
    public EnemyDataSet[] encounters = Array.Empty<EnemyDataSet>();
    public Cost[] entryCosts = Array.Empty<Cost>();
    public Cost[] currencyRewards = Array.Empty<Cost>();
    public InventoryItem[] itemRewards = Array.Empty<InventoryItem>();
}
