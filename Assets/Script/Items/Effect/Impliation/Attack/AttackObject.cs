using UnityEngine;

public class AttackObject<TStat> : AttackObjectBase, IDynamicBuffReceiver
    where TStat : class, IGameStat<TStat>
{
    [Header("Reference")]
    public GameObject owner;

    protected bool useSnapshotAndDynamicBuff;

    protected TStat snapshotAttackStat;
    protected ItemData sourceItemData;
    protected EquipmentBag sourceBag;
    protected BuffManager buffManager;

    #region Init

    public virtual void InitWithSnapshotAndDynamicBuff(
        TStat snapshotAttackStat,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        BuffManager buffManager,
        GameObject owner
    )
    {
        UnregisterDynamicBuffReceiverInternal();

        useSnapshotAndDynamicBuff = true;

        this.snapshotAttackStat = snapshotAttackStat;
        this.sourceItemData = sourceItemData;
        this.sourceBag = sourceBag;
        this.buffManager = buffManager;
        this.owner = owner;

        RegisterDynamicBuffReceiver();

        OnDynamicBuffChanged();
    }

    protected void RegisterDynamicBuffReceiver()
    {
        if (buffManager == null)
            return;

        buffManager.RegisterDynamicBuffReceiver(this);
    }

    protected override void UnregisterDynamicBuffReceiverInternal()
    {
        if (buffManager == null)
            return;

        buffManager.UnregisterDynamicBuffReceiver(this);
    }

    public virtual void OnDynamicBuffChanged()
    {
        if (!useSnapshotAndDynamicBuff)
            return;

        RefreshStatFromSnapshotAndDynamicBuff();
    }

    protected virtual void RefreshStatFromSnapshotAndDynamicBuff()
    {
        if (snapshotAttackStat == null)
            return;

        TStat currentStat = snapshotAttackStat;

        if (buffManager != null)
        {
            TStat dynamicBuffedStat = buffManager.GetBuffedStatForItem(
                snapshotAttackStat,
                sourceItemData,
                sourceBag,
                BuffCalculationMode.DynamicOnly
            );

            if (dynamicBuffedStat != null)
                currentStat = dynamicBuffedStat;
        }

        ApplyStat(currentStat);
    }

    protected virtual void ApplyStat(TStat currentStat) { }

    #endregion
}