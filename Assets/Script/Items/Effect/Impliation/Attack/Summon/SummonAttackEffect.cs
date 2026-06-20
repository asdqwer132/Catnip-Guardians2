using UnityEngine;

[CreateAssetMenu(
    fileName = "SummonAttackEffect",
    menuName = "GameData/Item/Item Effect/SummonAttackEffect"
)]
public class SummonAttackEffect : ItemEffectData
{
    [Header("Attack Stat")]
    public SummonStat attackStat;

    [Header("Optional Override")]
    public SummonItemThrower attackPrefab;

    public override void ExecuteEffect(ItemEffectContext context)
    {


        if (context == null || context.sourceItemData == null)
            return;

        if (attackStat == null)
            return;

        if (attackPrefab == null)
            return;

        Vector3 spawnPosition = context.targetPosition;
        spawnPosition.z = 0f;

        SummonItemThrower damageArea = Instantiate(
            attackPrefab,
            spawnPosition,
            Quaternion.identity
        );

        InitDamageArea(damageArea, context);
    }

    protected SummonStat GetCurrentAttackStat(ItemEffectContext context)
    {
        if (attackStat == null)
            return null;

        if (context == null || context.buffManager == null)
            return attackStat;

        SummonStat buffedStat = context.buffManager.GetBuffedStatForItem(
            attackStat,
            context.sourceItemData,
            context.sourceBag,
            BuffCalculationMode.All
        );

        if (buffedStat != null)
            return buffedStat;

        return attackStat;
    }

    private void InitDamageArea(SummonItemThrower damageArea, ItemEffectContext context)
    {
        if (damageArea == null || context == null)
            return;

        SummonStat snapshotStat = attackStat;

        if (context.buffManager != null)
        {
            SummonStat buffedSnapshotStat =
                context.buffManager.GetBuffedStatForItem(
                    attackStat,
                    context.sourceItemData,
                    context.sourceBag,
                    BuffCalculationMode.SnapshotOnly,
                    true
                );

            if (buffedSnapshotStat != null)
                snapshotStat = buffedSnapshotStat;
        }

        damageArea.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat: snapshotStat,
            sourceItemData: context.sourceItemData,
            sourceBag: context.sourceBag,
            buffManager: context.buffManager,
            owner: context.owner
        );
    }

    protected override float GetImpactRadius(ItemEffectContext context)
    {
        SummonStat currentStat = GetCurrentAttackStat(context);

        if (currentStat == null)
            return 1f;

        return currentStat.summonAttackRange;
    }
}