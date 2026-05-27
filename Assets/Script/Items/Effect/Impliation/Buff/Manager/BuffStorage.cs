using System.Collections.Generic;

public class BuffStorage
{
    public readonly List<ActiveBuff> globalBuffs =
        new List<ActiveBuff>();

    public readonly List<ActiveBuff> futureEnemyBuffs =
        new List<ActiveBuff>();

    public readonly Dictionary<EquipmentBag, List<ActiveBuff>> bagBuffs =
        new Dictionary<EquipmentBag, List<ActiveBuff>>();

    public readonly Dictionary<ItemData, List<ActiveBuff>> itemBuffs =
        new Dictionary<ItemData, List<ActiveBuff>>();

    public readonly Dictionary<ItemSeries, List<ActiveBuff>> itemSeriesBuffs =
        new Dictionary<ItemSeries, List<ActiveBuff>>();

    public readonly Dictionary<Enemy, List<ActiveBuff>> enemyBuffs =
        new Dictionary<Enemy, List<ActiveBuff>>();

    public readonly List<Enemy> registeredEnemies =
        new List<Enemy>();

    public List<ActiveBuff> GetOrCreateBagBuffs(EquipmentBag bag)
    {
        if (bag == null)
            return null;

        if (!bagBuffs.ContainsKey(bag))
            bagBuffs.Add(bag, new List<ActiveBuff>());

        return bagBuffs[bag];
    }

    public List<ActiveBuff> GetOrCreateItemBuffs(ItemData itemData)
    {
        if (itemData == null)
            return null;

        if (!itemBuffs.ContainsKey(itemData))
            itemBuffs.Add(itemData, new List<ActiveBuff>());

        return itemBuffs[itemData];
    }

    public List<ActiveBuff> GetOrCreateItemSeriesBuffs(ItemSeries series)
    {
        if (series == ItemSeries.None)
            return null;

        if (!itemSeriesBuffs.ContainsKey(series))
            itemSeriesBuffs.Add(series, new List<ActiveBuff>());

        return itemSeriesBuffs[series];
    }

    public List<ActiveBuff> GetOrCreateEnemyBuffs(Enemy enemy)
    {
        if (enemy == null)
            return null;

        if (!enemyBuffs.ContainsKey(enemy))
            enemyBuffs.Add(enemy, new List<ActiveBuff>());

        return enemyBuffs[enemy];
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (registeredEnemies.Contains(enemy))
            return;

        registeredEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        registeredEnemies.Remove(enemy);
        enemyBuffs.Remove(enemy);
    }

    public void ClearAll()
    {
        globalBuffs.Clear();
        futureEnemyBuffs.Clear();
        bagBuffs.Clear();
        itemBuffs.Clear();
        itemSeriesBuffs.Clear();
        enemyBuffs.Clear();
        registeredEnemies.Clear();
    }
}