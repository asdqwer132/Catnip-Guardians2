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

    public override void Execute(ItemEffectContext context)
    {
        if (context == null || context.sourceItemData == null)
            return;

        if (attackStat == null)
        {
            Debug.LogWarning(context.sourceItemData.itemName + " 공격 스탯이 없습니다.");
            return;
        }

        GameObject attackPrefab = overrideAttackPrefab;

        if (attackPrefab == null)
        {
            Debug.LogWarning(context.sourceItemData.itemName + " 공격 프리팹이 없습니다.");
            return;
        }

        Vector3 spawnPosition = context.targetPosition;
        spawnPosition.z = 0f;

        GameObject obj = Instantiate(
            attackPrefab,
            spawnPosition,
            Quaternion.identity
        );

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

        damageArea.InitDynamic(
            baseAttackStat: attackStat,
            sourceItemData: context.sourceItemData,
            sourceBag: context.sourceBag,
            buffManager: context.buffManager,
            owner: context.owner
        );
    }
}