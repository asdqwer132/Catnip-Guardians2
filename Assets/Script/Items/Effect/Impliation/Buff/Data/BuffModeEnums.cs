public enum BuffApplyTiming
{
    Snapshot,
    Dynamic
}

public enum BuffStackMode
{
    Refresh,
    Stack
}

public enum BuffUseLimitType
{
    Time,
    UseCount
}

public enum BuffCalculationMode
{
    All,
    SnapshotOnly,
    DynamicOnly
}

public enum BuffTargetKind
{
    Item,
    Bag,
    ItemSeries,
    AllItems,
    Enemy,
    AllEnemiesIncludingFuture,
    EnemySpawner,
    AllEnemySpawners,
    Player,
    AllPlayers
}

public enum BuffNotifyScope
{
    All,
    Item,
    Enemy,
    EnemySpawner,
    DynamicOnly,
    Player
}
