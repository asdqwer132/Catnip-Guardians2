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
    Infinite = -1,
    Time= 0,
    UseCount = 1,
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
    Target,
    Group
}

public enum BuffNotifyScope
{
    All,
    Item,
    Target,
    DynamicOnly
}
