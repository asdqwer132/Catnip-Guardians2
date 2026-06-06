using System.Collections.Generic;

public class BuffQuery
{
    private readonly BuffStorage storage;

    public BuffQuery(BuffStorage storage)
    {
        this.storage = storage;
    }

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        AddBuffsToList(result, storage.activeBuffs, false);
        return result;
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        AddBuffsToList(result, storage.activeBuffs, true);
        return result;
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag, bool visibleOnly = false)
    {
        return GetBuffsByPredicate(buff => buff.target != null && buff.target.kind == BuffTargetKind.Bag && buff.target.bag == bag, visibleOnly);
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData, bool visibleOnly = false)
    {
        return GetBuffsByPredicate(buff => buff.target != null && buff.target.kind == BuffTargetKind.Item && buff.target.itemData == itemData, visibleOnly);
    }

    public List<ActiveBuff> GetItemSeriesBuffsAsList(ItemSeries series, bool visibleOnly = false)
    {
        return GetBuffsByPredicate(buff => buff.target != null && buff.target.kind == BuffTargetKind.ItemSeries && buff.target.itemSeries == series, visibleOnly);
    }

    public List<ActiveBuff> GetEnemyBuffsAsList(Enemy enemy, bool visibleOnly = false)
    {
        return GetBuffsByPredicate(buff => buff.target != null && buff.target.MatchesEnemy(enemy), visibleOnly);
    }

    public List<ActiveBuff> GetEnemySpawnerBuffsAsList(EnemySpawner spawner, bool visibleOnly = false)
    {
        return GetBuffsByPredicate(buff => buff.target != null && buff.target.MatchesEnemySpawner(spawner), visibleOnly);
    }

    private List<ActiveBuff> GetBuffsByPredicate(System.Predicate<ActiveBuff> predicate, bool visibleOnly)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (storage == null || predicate == null)
            return result;

        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];
            if (!CanAdd(buff, visibleOnly))
                continue;

            if (predicate(buff))
                result.Add(buff);
        }

        return result;
    }

    private void AddBuffsToList(List<ActiveBuff> result, List<ActiveBuff> source, bool visibleOnly)
    {
        if (result == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];
            if (CanAdd(buff, visibleOnly))
                result.Add(buff);
        }
    }

    private bool CanAdd(ActiveBuff buff, bool visibleOnly)
    {
        if (buff == null || buff.IsExpired)
            return false;

        if (visibleOnly && !buff.showInUI)
            return false;

        return true;
    }
}
