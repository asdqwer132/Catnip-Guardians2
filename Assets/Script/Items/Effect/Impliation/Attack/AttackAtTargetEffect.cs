using UnityEngine;

[CreateAssetMenu(fileName = "AttackAtTargetEffect", menuName = "Game/Item Effect/Attack At Target")]
public class AttackAtTargetEffect : ItemEffectData, IItemEffectStatProvider
{
    [Header("Attack Stat")]
    public AttackStat attackStat;

    [Header("Runtime")]
    [Tooltip("켜면 이미 생성된 장판도 버프 만료/해제에 따라 실시간으로 약해짐")]
    public bool useDynamicRuntimeStat = true;

    [Header("Optional Override")]
    [Tooltip("비워두면 ItemData.prefab을 사용합니다.")]
    public GameObject overrideAttackPrefab;

    public EffectStat GetBaseEffectStat()
    {
        return attackStat;
    }

    public override void Execute(ItemEffectContext context)
    {
        if (context == null || context.itemData == null)
            return;

        GameObject attackPrefab = GetAttackPrefab(context);

        if (attackPrefab == null)
        {
            Debug.LogWarning(context.itemData.itemName + " 공격 프리팹이 없습니다.");
            return;
        }

        GameObject obj = Instantiate(
            attackPrefab,
            context.targetPosition,
            Quaternion.identity
        );

        ApplyDamageArea(obj, context);
    }

    private GameObject GetAttackPrefab(ItemEffectContext context)
    {
        if (overrideAttackPrefab != null)
            return overrideAttackPrefab;

        return context.itemData.prefab;
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

        AttackStat finalAttackStat = GetFinalAttackStat(context);

        if (finalAttackStat == null)
        {
            Debug.LogWarning("최종 AttackStat이 없습니다.");
            return;
        }

        if (useDynamicRuntimeStat)
        {
            EffectStat ownerStat = GetOwnerStat(context.owner);

            damageArea.InitDynamic(
                statSourceItem: context.inventoryItem,
                baseAttackStat: attackStat,
                owner: context.owner,
                ownerStat: ownerStat,
                currentCycleId: context.currentCycleId
            );
        }
        else
        {
            damageArea.Init(
                damage: finalAttackStat.GetAttackDamage(),
                radius: finalAttackStat.GetSafeRadius(),
                lifeTime: finalAttackStat.GetSafeLifeTime(),
                damageApplyMode: finalAttackStat.damageApplyMode,
                damageInterval: finalAttackStat.GetSafeDamageInterval(),
                owner: context.owner
            );
        }

        Debug.Log(
            "공격 실행 / 데미지: " +
            finalAttackStat.GetAttackDamage() +
            " / 범위: " +
            finalAttackStat.effectRadius +
            " / 생존시간: " +
            finalAttackStat.effectLifeTime +
            " / 실시간 스탯: " +
            useDynamicRuntimeStat
        );
    }

    private AttackStat GetFinalAttackStat(ItemEffectContext context)
    {
        if (context.effectStat is AttackStat contextAttackStat)
            return contextAttackStat;

        if (attackStat != null)
            return attackStat.CloneAttack();

        return null;
    }

    private EffectStat GetOwnerStat(GameObject owner)
    {
        if (owner == null)
            return null;

        PlayerStat playerStat = owner.GetComponent<PlayerStat>();

        if (playerStat == null)
            return null;

        return playerStat.TotalStat;
    }
}