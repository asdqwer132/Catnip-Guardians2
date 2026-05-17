using UnityEngine;

[CreateAssetMenu(fileName = "AttackAtTargetEffect", menuName = "Game/Item Effect/Attack At Target")]
public class AttackAtTargetEffect : ItemEffectData
{
    [Header("Optional Override")]
    [Tooltip("비워두면 ItemData.prefab을 사용합니다.")]
    public GameObject overrideAttackPrefab;

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

        Debug.Log(
            context.itemData.itemName +
            " 공격 효과 실행 / 데미지: " +
            GetItemDamage(context) +
            " / 범위: " +
            GetItemRadius(context)
        );
    }

    private GameObject GetAttackPrefab(ItemEffectContext context)
    {
        if (overrideAttackPrefab != null)
            return overrideAttackPrefab;

        return context.itemData.prefab;
    }

    private void ApplyDamageArea(GameObject obj, ItemEffectContext context)
    {
        DamageArea damageArea = obj.GetComponent<DamageArea>();

        if (damageArea == null)
            damageArea = obj.GetComponentInChildren<DamageArea>();

        if (damageArea == null)
            return;

        damageArea.Init(
            damage: GetItemDamage(context),
            radius: GetItemRadius(context),
            lifeTime: context.itemData.effectStat.effectRadius,
            hitOnce: context.itemData.effectStat.effectCount == 1 ? true : false,
            owner: context.owner
        );
    }

    private float GetItemDamage(ItemEffectContext context)
    {
        if (context.effectStat == null)
            return 0f;

        return context.effectStat.attackPower *
               context.effectStat.attackMultiplier;
    }

    private float GetItemRadius(ItemEffectContext context)
    {
        return Mathf.Max(0.01f, context.effectRadius);
    }
}