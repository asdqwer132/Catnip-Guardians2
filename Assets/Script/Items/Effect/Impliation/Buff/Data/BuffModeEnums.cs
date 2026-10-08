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

public enum BuffUseCountConsumeMode
{
    // 기존 에셋의 기본값: 실제 아이템 실행 중 버프가 스탯에 적용되면 차감.
    WhenBuffApplied = 0,
    AnyItemUsed = 1,
    SpecificItemsUsed = 2,
    // 해당 소환물이 실제로 공격한 경우에만 명시적으로 차감합니다.
    SummonAttack = 3
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
