using System.Collections.Generic;

/// <summary>
/// BuffTicker
/// 
/// 역할:
/// - 시간 제한 버프의 남은 시간을 감소시킨다.
/// - 만료된 버프를 제거한다.
/// - 비어 있는 딕셔너리 항목을 정리한다.
/// 
/// 주의:
/// - 스탯 계산은 하지 않는다.
/// - UI 갱신도 하지 않는다.
/// - 변경 여부만 반환하고, 실제 알림은 BuffManager가 담당한다.
/// </summary>
public class BuffTicker
{
    private BuffStorage storage;

    public BuffTicker(BuffStorage storage)
    {
        this.storage = storage;
    }

    public bool Tick(float deltaTime)
    {
        bool changed = false;

        if (TickList(storage.globalBuffs, deltaTime))
            changed = true;

        if (TickList(storage.futureEnemyBuffs, deltaTime))
            changed = true;

        if (TickList(storage.globalEnemySpawnerBuffs, deltaTime))
            changed = true;

        if (TickDictionary(storage.bagBuffs, deltaTime))
            changed = true;

        if (TickDictionary(storage.itemBuffs, deltaTime))
            changed = true;

        if (TickDictionary(storage.itemSeriesBuffs, deltaTime))
            changed = true;

        if (TickDictionary(storage.enemyBuffs, deltaTime))
            changed = true;

        if (TickDictionary(storage.enemySpawnerBuffs, deltaTime))
            changed = true;

        RemoveNullEnemies();
        RemoveNullEnemySpawners();

        return changed;
    }

    private bool TickList(List<ActiveBuff> buffs, float deltaTime)
    {
        bool changed = false;

        if (buffs == null)
            return false;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
            {
                buffs.RemoveAt(i);
                changed = true;
                continue;
            }

            buff.Tick(deltaTime);

            if (buff.IsExpired)
            {
                buffs.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    private bool TickDictionary<TKey>(Dictionary<TKey, List<ActiveBuff>> dictionary, float deltaTime)
    {
        bool changed = false;

        if (dictionary == null)
            return false;

        List<TKey> removeKeys = null;

        foreach (KeyValuePair<TKey, List<ActiveBuff>> pair in dictionary)
        {
            List<ActiveBuff> list = pair.Value;

            if (TickList(list, deltaTime))
                changed = true;

            if (list == null || list.Count == 0)
            {
                if (removeKeys == null)
                    removeKeys = new List<TKey>();

                removeKeys.Add(pair.Key);
            }
        }

        if (removeKeys == null)
            return changed;

        for (int i = 0; i < removeKeys.Count; i++)
        {
            dictionary.Remove(removeKeys[i]);
            changed = true;
        }

        return changed;
    }

    private void RemoveNullEnemies()
    {
        for (int i = storage.registeredEnemies.Count - 1; i >= 0; i--)
        {
            if (storage.registeredEnemies[i] == null)
                storage.registeredEnemies.RemoveAt(i);
        }
    }

    private void RemoveNullEnemySpawners()
    {
        for (int i = storage.registeredEnemySpawners.Count - 1; i >= 0; i--)
        {
            if (storage.registeredEnemySpawners[i] == null)
                storage.registeredEnemySpawners.RemoveAt(i);
        }
    }
}