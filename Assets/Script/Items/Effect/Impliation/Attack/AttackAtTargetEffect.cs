using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackAtTargetEffect",
    menuName = "Game/Item Effect/Attack At Target"
)]
public class AttackAtTargetEffect : ItemEffectData
{
    [Header("Attack Stat")]
    public AttackStat attackStat;

    [Header("Optional Override")]
    public GameObject overrideAttackPrefab;

    [Header("Hit Option")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || context.sourceItemData == null)
            return;

        if (attackStat == null)
            return;
        
        GameObject attackPrefab = overrideAttackPrefab;

        if (attackPrefab == null)
            return;

        Vector3 spawnPosition = context.targetPosition;
        spawnPosition.z = 0f;

        GameObject obj = Instantiate(attackPrefab, spawnPosition, Quaternion.identity);

        ApplyDamageArea(obj, context);
    }
    private void ApplyDamageArea(GameObject obj, ItemEffectContext context)
    {
        if (obj == null || context == null)
            return;

        DamageArea damageArea = obj.GetComponent<DamageArea>();

        if (damageArea == null)
            damageArea = obj.GetComponentInChildren<DamageArea>();

        if (damageArea == null)
            return;

        damageArea.damageApplyMode = damageApplyMode;

        AttackStat snapshotStat = attackStat;

        if (context.buffManager != null)
        {
            AttackStat buffedSnapshotStat =
                context.buffManager.GetSnapshotAttackStatAndConsume(
                    attackStat,
                    context.sourceItemData,
                    context.sourceBag
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
        AttackStat currentStat = GetCurrentAttackStat(context);

        if (currentStat == null)
            return 1f;

        return currentStat.attackRange;
    }

    private AttackStat GetCurrentAttackStat(ItemEffectContext context)
    {
        if (attackStat == null)
            return null;

        if (context == null || context.buffManager == null)
            return attackStat;

        AttackStat buffedStat = context.buffManager.GetBuffedAttackStat(
            attackStat,
            context.sourceItemData,
            context.sourceBag
        );

        if (buffedStat != null)
            return buffedStat;

        return attackStat;
    }
}