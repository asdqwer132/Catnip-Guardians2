using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageAreaAttackEffect",
    menuName = "GameData/Items/Effects/Attack/Damage Area"
)]
public class DamageAreaAttackEffect : ItemEffectData
{
    [Header("Attack Stat")]
    public DamageAreaAttackStat attackStat;

    [Header("Optional Override")]
    public DamageArea attackPrefab;

    [Header("Damage Area")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Header("On Hit Effects")]
    public HitEffectData[] onHitEffects;
    public HitEffectApplyMode hitEffectApplyMode = HitEffectApplyMode.FirstHitOnly;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, attackStat);
        if (onHitEffects == null) return;
        for (int i = 0; i < onHitEffects.Length; i++)
        {
            BuffHitEffectData hitBuff = onHitEffects[i] as BuffHitEffectData;
            if (hitBuff != null && hitBuff.buffEffect != null)
                context.plan.Prepare(hitBuff.buffEffect, context);
        }
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

        return context.GetCurrentStat(this, attackStat);
    }

    protected void InitDamageArea(DamageArea damageArea, ItemEffectContext context)
    {
        if (damageArea == null || context == null)
            return;

        damageArea.damageApplyMode = damageApplyMode;

        DamageAreaAttackStat snapshotStat = context.GetSnapshotStat(this, attackStat);
        damageArea.BindLifetime(context);

        damageArea.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat: snapshotStat,
            sourceItemData: context.sourceItemData,
            sourceBag: context.sourceBag,
            buffManager: context.buffManager,
            owner: context.owner
        );

        damageArea.InitHitEffects(onHitEffects, hitEffectApplyMode, context);
    }

    protected override float GetImpactRadius(ItemEffectContext context)
    {
        DamageAreaAttackStat currentStat = GetCurrentAttackStat(context);

        if (currentStat == null)
            return 1f;

        return currentStat.damageAreaRange;
    }
}
