using UnityEngine;

[CreateAssetMenu(
    fileName = "SummonAttackEffect",
    menuName = "GameData/Items/Effects/Attack/Summon"
)]
public class SummonAttackEffect : ItemEffectData
{
    [Header("Attack Stat")]
    public SummonStat attackStat;

    [Header("Optional Override")]
    public SummonItemThrower attackPrefab;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, attackStat);
    }

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

        damageArea.SetExecutionContext(context);
        InitDamageArea(damageArea, context);
    }

    protected SummonStat GetCurrentAttackStat(ItemEffectContext context)
    {
        if (attackStat == null)
            return null;

        if (context == null || context.buffManager == null)
            return attackStat;

        return context.GetCurrentStat(this, attackStat);
    }

    private void InitDamageArea(SummonItemThrower damageArea, ItemEffectContext context)
    {
        if (damageArea == null || context == null)
            return;

        SummonStat snapshotStat = context.GetSnapshotStat(this, attackStat);
        damageArea.BindLifetime(context);

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