using System.Collections.Generic;

public class BuffQuery
{
    private BuffStorage storage;

    public BuffQuery(BuffStorage storage)
    {
        this.storage = storage;
    }

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        AddBuffsToList(result, storage.globalBuffs);
        AddBuffsToList(result, storage.futureEnemyBuffs);
        AddBuffsToList(result, storage.globalEnemySpawnerBuffs);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
            AddBuffsToList(result, pair.Value);
        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
            AddBuffsToList(result, pair.Value);
        foreach (KeyValuePair<ItemSeries, List<ActiveBuff>> pair in storage.itemSeriesBuffs)
            AddBuffsToList(result, pair.Value);
        foreach (KeyValuePair<Enemy, List<ActiveBuff>> pair in storage.enemyBuffs)
            AddBuffsToList(result, pair.Value);
        foreach (KeyValuePair<EnemySpawner, List<ActiveBuff>> pair in storage.enemySpawnerBuffs)
            AddBuffsToList(result, pair.Value);

        return result;
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        AddUniqueBuffsToList(result, storage.globalBuffs, true);
        AddUniqueBuffsToList(result, storage.futureEnemyBuffs, true);
        AddUniqueBuffsToList(result, storage.globalEnemySpawnerBuffs, true);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
            AddUniqueBuffsToList(result, pair.Value, true);
        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
            AddUniqueBuffsToList(result, pair.Value, true);
        foreach (KeyValuePair<ItemSeries, List<ActiveBuff>> pair in storage.itemSeriesBuffs)
            AddUniqueBuffsToList(result, pair.Value, true);
        foreach (KeyValuePair<EnemySpawner, List<ActiveBuff>> pair in storage.enemySpawnerBuffs)
            AddUniqueBuffsToList(result, pair.Value, true);
        foreach (KeyValuePair<Enemy, List<ActiveBuff>> pair in storage.enemyBuffs)
            AddUniqueBuffsToList(result, pair.Value, true);

        return result;
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        if (bag == null)
            return result;
        if (storage.bagBuffs.TryGetValue(bag, out List<ActiveBuff> buffs))
            AddBuffsToList(result, buffs, visibleOnly);
        return result;
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        if (itemData == null)
            return result;
        if (storage.itemBuffs.TryGetValue(itemData, out List<ActiveBuff> buffs))
            AddBuffsToList(result, buffs, visibleOnly);
        return result;
    }

    public List<ActiveBuff> GetItemSeriesBuffsAsList(ItemSeries series, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        if (series == ItemSeries.None)
            return result;
        if (storage.itemSeriesBuffs.TryGetValue(series, out List<ActiveBuff> buffs))
            AddBuffsToList(result, buffs, visibleOnly);
        return result;
    }

    public List<ActiveBuff> GetEnemyBuffsAsList(Enemy enemy, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        if (enemy == null)
            return result;

        AddBuffsToList(result, storage.futureEnemyBuffs, visibleOnly);

        if (storage.enemyBuffs.TryGetValue(enemy, out List<ActiveBuff> buffs))
            AddBuffsToList(result, buffs, visibleOnly);

        return result;
    }

    private void AddBuffsToList(List<ActiveBuff> target, List<ActiveBuff> source, bool visibleOnly = false)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];
            if (!CanAddBuff(buff, visibleOnly))
                continue;

            target.Add(buff);
        }
    }

    private void AddUniqueBuffsToList(List<ActiveBuff> target, List<ActiveBuff> source, bool visibleOnly = false)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];
            if (!CanAddBuff(buff, visibleOnly))
                continue;
            if (ContainsSameVisibleBuff(target, buff))
                continue;

            target.Add(buff);
        }
    }

    private bool CanAddBuff(ActiveBuff buff, bool visibleOnly)
    {
        if (buff == null)
            return false;
        if (buff.IsExpired)
            return false;
        if (visibleOnly && !buff.showInUI)
            return false;

        return true;
    }

    private bool ContainsSameVisibleBuff(List<ActiveBuff> list, ActiveBuff buff)
    {
        for (int i = 0; i < list.Count; i++)
        {
            ActiveBuff other = list[i];
            if (other == null)
                continue;
            if (other.sourceItemData == buff.sourceItemData && other.sourceBag == buff.sourceBag && other.sourceEffectData == buff.sourceEffectData)
                return true;
        }

        return false;
    }
}