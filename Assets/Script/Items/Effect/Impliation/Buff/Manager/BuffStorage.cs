using System.Collections.Generic;

/// <summary>
/// BuffStorage
/// 
/// 역할:
/// - 현재 적용 중인 모든 ActiveBuff를 대상별로 저장한다.
/// - 계산, 등록, 틱 처리는 하지 않고 순수 저장소 역할만 한다.
/// 
/// 저장 그룹:
/// - globalBuffs: 모든 아이템 계산에 들어가는 전역 버프.
/// - futureEnemyBuffs: 현재/미래 적에게 모두 적용되는 적 버프.
/// - bagBuffs: 특정 가방 대상 버프.
/// - itemBuffs: 특정 아이템 대상 버프.
/// - itemSeriesBuffs: 특정 아이템 시리즈 대상 버프.
/// - enemyBuffs: 특정 적 대상 버프.
/// - enemySpawnerBuffs: 특정 스포너 대상 버프.
/// - globalEnemySpawnerBuffs: 모든 스포너 대상 버프.
/// </summary>
public class BuffStorage
{
    public readonly List<ActiveBuff> globalBuffs = new List<ActiveBuff>();
    public readonly List<ActiveBuff> futureEnemyBuffs = new List<ActiveBuff>();
    public readonly List<ActiveBuff> globalEnemySpawnerBuffs = new List<ActiveBuff>();

    public readonly Dictionary<EquipmentBag, List<ActiveBuff>> bagBuffs = new Dictionary<EquipmentBag, List<ActiveBuff>>();
    public readonly Dictionary<ItemData, List<ActiveBuff>> itemBuffs = new Dictionary<ItemData, List<ActiveBuff>>();
    public readonly Dictionary<ItemSeries, List<ActiveBuff>> itemSeriesBuffs = new Dictionary<ItemSeries, List<ActiveBuff>>();
    public readonly Dictionary<Enemy, List<ActiveBuff>> enemyBuffs = new Dictionary<Enemy, List<ActiveBuff>>();
    public readonly Dictionary<EnemySpawner, List<ActiveBuff>> enemySpawnerBuffs = new Dictionary<EnemySpawner, List<ActiveBuff>>();

    public readonly List<Enemy> registeredEnemies = new List<Enemy>();
    public readonly List<EnemySpawner> registeredEnemySpawners = new List<EnemySpawner>();

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

    public List<ActiveBuff> GetOrCreateEnemySpawnerBuffs(EnemySpawner spawner)
    {
        if (spawner == null)
            return null;

        if (!enemySpawnerBuffs.ContainsKey(spawner))
            enemySpawnerBuffs.Add(spawner, new List<ActiveBuff>());

        return enemySpawnerBuffs[spawner];
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

    public void RegisterEnemySpawner(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        if (registeredEnemySpawners.Contains(spawner))
            return;

        registeredEnemySpawners.Add(spawner);
    }

    public void UnregisterEnemySpawner(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        registeredEnemySpawners.Remove(spawner);
        enemySpawnerBuffs.Remove(spawner);
    }

    public void ClearAll()
    {
        globalBuffs.Clear();
        futureEnemyBuffs.Clear();
        globalEnemySpawnerBuffs.Clear();

        bagBuffs.Clear();
        itemBuffs.Clear();
        itemSeriesBuffs.Clear();
        enemyBuffs.Clear();
        enemySpawnerBuffs.Clear();
    }
}