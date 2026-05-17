using UnityEngine;

[CreateAssetMenu(fileName = "AttackAtTargetEffect", menuName = "Game/Item Effect/Attack At Target")]
public class AttackAtTargetEffect : ItemEffectData, IItemEffectStatProvider
{
    [Header("Attack Effect Stat")]
    public EffectStat effectStat;

    [Header("Damage Apply")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Header("Optional Override")]
    [Tooltip("비워두면 ItemData.prefab을 사용합니다.")]
    public GameObject overrideAttackPrefab;

    public EffectStat GetBaseEffectStat()
    {
        return effectStat;
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

        EffectStat stat = context.effectStat;

        if (stat == null)
            stat = effectStat;

        if (stat == null)
            return;

        damageArea.Init(
            damage: stat.GetAttackDamage(),
            radius: stat.GetSafeRadius(),
            lifeTime: stat.GetSafeLifeTime(),
            damageApplyMode: damageApplyMode,
            damageInterval: stat.GetSafeDamageInterval(),
            owner: context.owner
        );
    }
}