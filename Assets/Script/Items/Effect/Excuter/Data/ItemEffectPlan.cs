using System.Collections.Generic;

// 공격 한 번의 스탯을 보관한다. 공유 에셋에는 실행 상태를 저장하지 않는다.
public sealed class ItemEffectPlan
{
    private readonly Dictionary<ItemEffectData, object> stats = new Dictionary<ItemEffectData, object>();
    private readonly HashSet<ItemEffectData> preparing = new HashSet<ItemEffectData>();

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
                context.sourceBag, BuffCalculationMode.SnapshotOnly, true)
            : baseStat.Clone();
        if (snapshot != null) snapshot.Clamp();
        stats.Add(effect, snapshot);
        return snapshot;
    }
}
