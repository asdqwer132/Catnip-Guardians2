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
    [Tooltip("비우면 프리팹의 종류 설정을 유지합니다.")]
    public SummonDefinition definition;
    public bool overrideThrownItemDamage;
    [Header("Behaviour Override")]
    [Tooltip("켜면 프리팹 대신 이 효과의 모듈 목록을 사용합니다. 빈 목록이면 행동하지 않습니다.")]
    public bool overrideModules;
    public SummonBehaviourModule[] modules;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, attackStat);
        SummonBehaviourModule[] configured = overrideModules ? modules : attackPrefab != null ? attackPrefab.modules : null;
        if (configured != null)
            foreach (SummonBehaviourModule module in configured)
                if (module != null) module.Prepare(context);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        TrySpawn(context, out _);
    }

    public bool TrySpawn(ItemEffectContext context, out SummonItemThrower summon)
    {
        summon = null;
        if (context == null || !context.CanContinue || context.sourceItemData == null ||
            attackStat == null || attackPrefab == null) return false;

        Vector3 spawnPosition = context.targetPosition;
        spawnPosition.z = 0f;

        summon = Instantiate(
            attackPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (definition != null) summon.ConfigureDefinition(definition);
        if (overrideModules) summon.ConfigureModules(modules);
        if (overrideThrownItemDamage) summon.overrideThrownItemDamage = true;
        summon.SetExecutionContext(context);
        InitDamageArea(summon, context);
        if (!summon.gameObject.activeSelf) summon.gameObject.SetActive(true);
        return true;
    }

    protected SummonStat GetCurrentAttackStat(ItemEffectContext context)
    {
        if (attackStat == null)
            return null;

        if (context == null)
            return attackStat;

        return context.GetCurrentStat(this, attackStat);
    }

    private void InitDamageArea(SummonItemThrower damageArea, ItemEffectContext context)
    {
        if (damageArea == null || context == null)
            return;

        SummonStat snapshotStat = context.GetUnscaledSnapshotStat(this, attackStat);
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
