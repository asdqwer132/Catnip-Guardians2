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

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
            AddBuffsToList(result, pair.Value);

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
            AddBuffsToList(result, pair.Value);

        foreach (KeyValuePair<Enemy, List<ActiveBuff>> pair in storage.enemyBuffs)
            AddBuffsToList(result, pair.Value);

        return result;
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        AddBuffsToList(result, storage.globalBuffs, true);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
            AddBuffsToList(result, pair.Value, true);

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
            AddBuffsToList(result, pair.Value, true);

        foreach (KeyValuePair<Enemy, List<ActiveBuff>> pair in storage.enemyBuffs)
            AddBuffsToList(result, pair.Value, true);

        return result;
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (bag == null)
            return result;

        if (!storage.bagBuffs.ContainsKey(bag))
            return result;

        AddBuffsToList(result, storage.bagBuffs[bag], visibleOnly);

        return result;
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (itemData == null)
            return result;

        if (!storage.itemBuffs.ContainsKey(itemData))
            return result;

        AddBuffsToList(result, storage.itemBuffs[itemData], visibleOnly);

        return result;
    }

    public List<ActiveBuff> GetEnemyBuffsAsList(Enemy enemy, bool visibleOnly = false)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (enemy == null)
            return result;

        if (!storage.enemyBuffs.ContainsKey(enemy))
            return result;

        AddBuffsToList(result, storage.enemyBuffs[enemy], visibleOnly);

        return result;
    }

    private void AddBuffsToList(
        List<ActiveBuff> target,
        List<ActiveBuff> source,
        bool visibleOnly = false
    )
    {
        if (target == null)
            return;

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            if (visibleOnly && !buff.showInUI)
                continue;

            target.Add(buff);
        }
    }
}