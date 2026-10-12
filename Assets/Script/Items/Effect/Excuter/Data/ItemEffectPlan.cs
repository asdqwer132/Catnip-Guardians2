using System.Collections.Generic;

// 공격 한 번의 스탯을 보관한다. 공유 에셋에는 실행 상태를 저장하지 않는다.
public sealed class ItemEffectPlan
{
    private readonly Dictionary<ItemEffectData, object> stats = new Dictionary<ItemEffectData, object>();
    private readonly Dictionary<object, object> ownedStats = new Dictionary<object, object>();
    private readonly HashSet<ItemEffectData> preparing = new HashSet<ItemEffectData>();
    private readonly Dictionary<ItemEffectData, ItemEffectData> branches = new Dictionary<ItemEffectData, ItemEffectData>();

    public bool TryGetBranch(ItemEffectData effect, out ItemEffectData selected)
        => branches.TryGetValue(effect, out selected);
    public void SetBranch(ItemEffectData effect, ItemEffectData selected) => branches[effect] = selected;

    public void Prepare(ItemEffectData effect, ItemEffectContext context)
    {
        if (effect == null || !preparing.Add(effect))
            return;
        try { effect.Prepare(context); }
        finally { preparing.Remove(effect); }
    }

    public T GetSnapshot<T>(ItemEffectData effect, T baseStat, ItemEffectContext context)
        where T : class, IGameStat<T>
    {
        if (effect == null || baseStat == null)
            return null;
        object cached;
        if (stats.TryGetValue(effect, out cached))
            return cached as T;
        T snapshot = context.buffManager != null
            ? context.buffManager.GetBuffedStatForItem(baseStat, context.sourceItemData,
                context.sourceBag, BuffCalculationMode.SnapshotOnly, context.consumeUseBuffs)
            : BuffStatCloneUtility.Clone(baseStat);
        if (snapshot != null) snapshot.Clamp();
        stats.Add(effect, snapshot);
        return snapshot;
    }

    // 소환물 프리팹처럼 기능 스탯을 직접 소유하는 객체의 실행별 Snapshot입니다.
    // 생성 효과의 SpawnStat 캐시와 별개이며, 같은 실행에서 생성한 여러 인스턴스가
    // 마지막 횟수제 버프를 동일하게 캡처하도록 원본 소유자별로 한 번 계산합니다.
    public T GetOwnedSnapshot<T>(object statOwner, T baseStat, ItemEffectContext context)
        where T : class, IGameStat<T>
    {
        if (statOwner == null || baseStat == null || context == null) return null;
        object cached;
        if (ownedStats.TryGetValue(statOwner, out cached)) return cached as T;
        T snapshot = context.buffManager != null && (context.sourceItemData != null || context.sourceBag != null)
            ? context.buffManager.GetBuffedStatForItem(baseStat, context.sourceItemData,
                context.sourceBag, BuffCalculationMode.SnapshotOnly, context.consumeUseBuffs)
            : BuffStatCloneUtility.Clone(baseStat);
        if (snapshot != null) snapshot.Clamp();
        ownedStats.Add(statOwner, snapshot);
        return snapshot;
    }
}
