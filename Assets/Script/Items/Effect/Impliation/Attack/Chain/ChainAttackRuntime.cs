using System.Collections.Generic;
using UnityEngine;

public sealed class ChainAttackRuntime : AttackObject<ChainAttackStat>
{
    private ChainAttackEffect data;
    private ChainAttackStat stat;
    private ItemEffectContext context;
    private HitEffectDispatcher dispatcher;
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
    private readonly HashSet<EnemyLifeKey> hitTargets = new HashSet<EnemyLifeKey>();
    private Enemy pendingTarget;
    private int pendingLife, hitCount;
    private EnemyLifeKey? previous;
    private Vector3 origin;
    private float wait;
    private bool initialized, finished;

    public void Init(ChainAttackEffect effect, ItemEffectContext source)
    {
        data = effect; context = source.Copy(source.targetPosition, source.direction);
        origin = context.targetPosition;
        dispatcher = new HitEffectDispatcher(data.onHitEffects, data.hitEffectApplyMode, context);
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetUnscaledSnapshotStat(data, data.attackStat), context.sourceItemData,
            context.sourceBag, context.buffManager, context.owner);
        initialized = true;
        SelectNext();
        Advance();
    }
    protected override void ApplyStat(ChainAttackStat currentStat) { stat = currentStat; }

    private void Update()
    {
        if (!initialized || finished) return;
        if (!context.CanContinue) { Finish(false); return; }
        wait -= Time.deltaTime;
        int budget = 128;
        while (!finished && wait <= 0f && budget-- > 0) Advance();
    }

    private void SelectNext()
    {
        bool initial = hitCount == 0;
        TargetSelection selection = initial ? data.firstTarget : data.nextTarget;
        if (selection == null) { pendingTarget = null; return; }
        float range = initial ? EffectStatUtility.Safe(selection.range, 0f, 100000f, 8f) *
            EffectExecutionScale.Safe(context.rangeMultiplier) : stat.chainRange;
        query.Scan(origin, range, data.enemyLayerMask);
        pendingTarget = selection.Select(query.Enemies, origin,
            data.allowTargetRehit ? null : hitTargets,
            !data.allowImmediateSameTarget ? previous : null, range);
        pendingLife = pendingTarget != null ? pendingTarget.HitEffectLifeId : 0;
    }

    private void Advance()
    {
        if (!context.CanContinue || stat == null) { Finish(false); return; }
        if (hitCount >= EffectStatUtility.Count(stat.chainMaxHits)) { Finish(true); return; }
        if (!IsPendingValid())
        {
            if (!data.retargetWhenTargetLost) { Finish(true); return; }
            SelectNext();
        }
        if (pendingTarget == null) { Finish(true); return; }
        Enemy target = pendingTarget;
        EnemyLifeKey key = new EnemyLifeKey(target, pendingLife);
        Vector3 destination = target.transform.position;
        Vector3 direction = destination - origin;
        if (direction.sqrMagnitude < 0.000001f) direction = context.direction;
        if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
        hitTargets.Add(key);
        previous = key;
        origin = destination;
        transform.position = destination;
        dispatcher.Hit(target, stat.DamageAt(hitCount), direction.normalized);
        hitCount++;
        if (data.jumpVisualData != null && context.CanContinue)
            data.jumpVisualData.Play(new EffectVisualContext(destination, Quaternion.identity));
        if (!context.CanContinue) { Finish(false); return; }
        if (hitCount >= EffectStatUtility.Count(stat.chainMaxHits)) { Finish(true); return; }
        SelectNext();
        if (pendingTarget == null) { Finish(true); return; }
        wait += stat.chainJumpInterval;
    }

    private bool IsPendingValid()
    {
        if (pendingTarget == null || !pendingTarget.CanReceiveHitEffects || pendingTarget.HitEffectLifeId != pendingLife) return false;
        float range = hitCount == 0 ? EffectStatUtility.Safe(data.firstTarget != null ? data.firstTarget.range : 0f,
            0f, 100000f, 8f) * EffectExecutionScale.Safe(context.rangeMultiplier) : stat.chainRange;
        return (pendingTarget.transform.position - origin).sqrMagnitude <= range * range;
    }

    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        if (success) CompleteLifetime();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
