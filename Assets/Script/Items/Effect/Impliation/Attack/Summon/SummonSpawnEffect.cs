using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

// 생성 조건/배치만 소유합니다. 공격력과 행동 모듈은 프리팹의 SummonActor가 소유합니다.
[MovedFrom(true, null, "Assembly-CSharp", "SummonAttackEffect")]
[CreateAssetMenu(fileName = "SummonSpawnEffect", menuName = "04.Items/Effects/Direct Effects/Summon/Spawn")]
public sealed class SummonSpawnEffect : ItemEffectData
{
    [Header("Summon Creation")]
    [FormerlySerializedAs("attackStat")]
    public SummonSpawnStat spawnStat = new SummonSpawnStat();
    [FormerlySerializedAs("attackPrefab")]
    public SummonActor summonPrefab;
    [Header("Spawn Layout")]
    public AttackPlacementMode placementMode = AttackPlacementMode.CircleEven;
    public AttackSpreadStartMode spreadStartMode = AttackSpreadStartMode.CenteredOnDirection;
    public bool clockwiseSpread;

    protected override bool CanStart(ItemEffectContext context)
        => base.CanStart(context) && summonPrefab != null && spawnStat != null;

    protected override bool TryExecuteWithConditions(ItemEffectContext context)
    {
        if (!AreConditionsSatisfied(context) || context.GetCurrentStat(this, spawnStat).SpawnCount == 0) return false;
        PlayImpactVfx(context);
        return Spawn(context) > 0;
    }

    public override void Prepare(ItemEffectContext context)
        => context.GetUnscaledSnapshotStat(this, spawnStat);

    public override void ExecuteEffect(ItemEffectContext context) => Spawn(context);

    // 목록을 받으면 합체/후속 코드에서도 이번에 생성된 전체 소환물을 확인할 수 있습니다.
    public int Spawn(ItemEffectContext context, List<SummonActor> results = null)
    {
        if (context == null || !context.CanContinue ||
            spawnStat == null || summonPrefab == null) return 0;

        SummonSpawnStat current = context.GetCurrentStat(this, spawnStat);
        Vector3 direction;
        if (!context.TryGetDirection(out direction)) direction = Vector3.right;
        int count = current.SpawnCount;
        int spawned = 0;
        for (int i = 0; i < count && context.CanContinue; i++)
        {
            Vector3 position = current.GetPosition(placementMode, context.targetPosition, direction,
                i, count, spreadStartMode, clockwiseSpread);
            SummonActor actor = Instantiate(summonPrefab, position, Quaternion.identity);
            actor.Initialize(context, current.lifetime);
            if (!actor.gameObject.activeSelf) actor.gameObject.SetActive(true);
            results?.Add(actor);
            context.onSummonSpawned?.Invoke(actor);
            spawned++;
        }
        return spawned;
    }

    public bool TrySpawn(ItemEffectContext context, out SummonActor first)
    {
        var results = new List<SummonActor>();
        Spawn(context, results);
        first = results.Count > 0 ? results[0] : null;
        return first != null;
    }

    protected override float GetImpactRadius(ItemEffectContext context)
        => context != null && spawnStat != null ? context.GetCurrentStat(this, spawnStat).spreadRadius : 0f;
}
