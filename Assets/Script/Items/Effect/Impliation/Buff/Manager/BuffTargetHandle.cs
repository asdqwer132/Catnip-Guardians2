using System;
using UnityEngine;

[Serializable]
public class BuffTargetHandle
{
    public BuffTargetKind kind;
    public ItemData itemData;
    public EquipmentBag bag;
    public ItemSeries itemSeries = ItemSeries.None;
    public Enemy enemy;
    public EnemySpawner enemySpawner;

    public static BuffTargetHandle Item(ItemData itemData)
    {
        return new BuffTargetHandle { kind = BuffTargetKind.Item, itemData = itemData };
    }

    public static BuffTargetHandle Bag(EquipmentBag bag)
    {
        return new BuffTargetHandle { kind = BuffTargetKind.Bag, bag = bag };
    }

    public static BuffTargetHandle GetItemSeries(ItemSeries itemSeries)
    {
        return new BuffTargetHandle { kind = BuffTargetKind.ItemSeries, itemSeries = itemSeries };
    }

    public static BuffTargetHandle AllItems()
    {
        return new BuffTargetHandle { kind = BuffTargetKind.AllItems };
    }

    public static BuffTargetHandle Enemy(Enemy enemy)
    {
        return new BuffTargetHandle { kind = BuffTargetKind.Enemy, enemy = enemy };
    }

    public static BuffTargetHandle AllEnemiesIncludingFuture()
    {
        return new BuffTargetHandle { kind = BuffTargetKind.AllEnemiesIncludingFuture };
    }

    public static BuffTargetHandle EnemySpawner(EnemySpawner enemySpawner)
    {
        return new BuffTargetHandle { kind = BuffTargetKind.EnemySpawner, enemySpawner = enemySpawner };
    }

    public static BuffTargetHandle AllEnemySpawners()
    {
        return new BuffTargetHandle { kind = BuffTargetKind.AllEnemySpawners };
    }

    public bool Matches(BuffQueryContext query)
    {
        if (query == null)
            return false;

        if (query.enemy != null)
            return MatchesEnemy(query.enemy);

        if (query.enemySpawner != null)
            return MatchesEnemySpawner(query.enemySpawner);

        return MatchesItem(query.itemData, query.bag);
    }

    public bool MatchesItem(ItemData targetItemData, EquipmentBag targetBag)
    {
        if (kind == BuffTargetKind.AllItems)
            return true;

        if (kind == BuffTargetKind.Bag)
            return bag != null && bag == targetBag;

        if (targetItemData == null)
            return false;

        if (kind == BuffTargetKind.Item)
            return itemData != null && itemData == targetItemData;

        if (kind == BuffTargetKind.ItemSeries)
            return itemSeries != ItemSeries.None && targetItemData.series == itemSeries;

        return false;
    }

    public bool MatchesEnemy(Enemy targetEnemy)
    {
        if (targetEnemy == null)
            return false;

        if (kind == BuffTargetKind.AllEnemiesIncludingFuture)
            return true;

        if (kind == BuffTargetKind.Enemy)
            return enemy != null && enemy == targetEnemy;

        return false;
    }

    public bool MatchesEnemySpawner(EnemySpawner targetEnemySpawner)
    {
        if (targetEnemySpawner == null)
            return false;

        if (kind == BuffTargetKind.AllEnemySpawners)
            return true;

        if (kind == BuffTargetKind.EnemySpawner)
            return enemySpawner != null && enemySpawner == targetEnemySpawner;

        return false;
    }

    public bool SameTarget(BuffTargetHandle other)
    {
        if (other == null)
            return false;

        if (kind != other.kind)
            return false;

        return itemData == other.itemData
            && bag == other.bag
            && itemSeries == other.itemSeries
            && enemy == other.enemy
            && enemySpawner == other.enemySpawner;
    }

    public string GetDebugName()
    {
        if (kind == BuffTargetKind.Item)
            return itemData != null ? itemData.GetDataName() : "Null Item";

        if (kind == BuffTargetKind.Bag)
            return bag != null ? bag.name : "Null Bag";

        if (kind == BuffTargetKind.ItemSeries)
            return itemSeries.ToString();

        if (kind == BuffTargetKind.Enemy)
            return enemy != null ? enemy.name : "Null Enemy";

        if (kind == BuffTargetKind.EnemySpawner)
            return enemySpawner != null ? enemySpawner.name : "Null EnemySpawner";

        return kind.ToString();
    }
}
