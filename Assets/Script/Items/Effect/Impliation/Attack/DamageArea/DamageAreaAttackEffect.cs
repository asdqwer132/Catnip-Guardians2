using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageAreaAttackEffect",
    menuName = "Game/Item Effect/DamageAreaAttackEffect"
)]
public class DamageAreaAttackEffect : ItemEffectData
{
    [Header("Attack Stat")]
    public DamageAreaAttackStat attackStat;

    [Header("Optional Override")]
    public DamageArea attackPrefab;

    [Header("Damage Area")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

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

        DamageArea damageArea = Instantiate(
            attackPrefab,
            spawnPosition,
            Quaternion.identity
        );

        InitDamageArea(damageArea, context);
    }

    protected DamageAreaAttackStat GetCurrentAttackStat(ItemEffectContext context)
    {
        if (attackStat == null)
            return null;

        if (context == null || context.buffManager == null)
            return attackStat;

        DamageAreaAttackStat buffedStat = context.buffManager.GetBuffedStatForItem(
            attackStat,
            context.sourceItemData,
            context.sourceBag,
            BuffCalculationMode.All
        );

        if (buffedStat != null)
            return buffedStat;

        return attackStat;
    }

    private void InitDamageArea(DamageArea damageArea, ItemEffectContext context)
    {
        if (damageArea == null || context == null)
            return;

        damageArea.damageApplyMode = damageApplyMode;

        DamageAreaAttackStat snapshotStat = attackStat;

        if (context.buffManager != null)
        {
            DamageAreaAttackStat buffedSnapshotStat =
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
        DamageAreaAttackStat currentStat = GetCurrentAttackStat(context);

        if (currentStat == null)
            return 1f;

        return currentStat.damageAreaRange;
    }
}