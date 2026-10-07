using System;
using UnityEngine;

// 비행/착지 대기/폭발은 스텝의 수명에 포함한다. 취소 시 완료 연출을 실행하지 않는다.
public sealed class RepeatItemProjectile : MonoBehaviour
{
    private ItemEffectContext context;
    private ItemEffectLease lease;
    private RepeatItemStat stat;
    private ItemImpactTrigger trigger;
    private LayerMask enemyMask;
    private TargetRangeIndicator indicator;
    private Action impact;
    private Action<bool> resolved;
    private float armedAt, nextQuery;
    private bool armed, finished;
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();

    public void Init(ItemThrowMover mover, ItemEffectContext context, ItemImpactTrigger trigger,
        RepeatItemStat stat, LayerMask enemyMask, float flightTime, TargetRangeIndicator indicator,
        Action impact, Action<bool> resolved)
    {
        this.context = context;
        this.trigger = trigger;
        this.stat = stat;
        this.enemyMask = enemyMask;
        this.indicator = indicator;
        this.impact = impact;
        this.resolved = resolved;
        lease = context.RetainLifetime();
        if (flightTime > 0f) mover.InitMove(context.usePosition, context.targetPosition, flightTime, Arrive);
        else { transform.position = context.targetPosition; Arrive(); }
    }

    private void Arrive()
    {
        if (finished || !context.CanContinue) return;
        armed = true;
        armedAt = Time.time;
        if (trigger == ItemImpactTrigger.OnArrival) Resolve(true);
    }

    private void Update()
    {
        if (finished || context == null) return;
        if (!context.CanContinue) { Resolve(false); return; }
        if (!armed) return;
        float elapsed = Time.time - armedAt;
        if (trigger == ItemImpactTrigger.AfterDelay)
        { if (elapsed >= stat.itemRepeatBombDelay) Resolve(true); return; }
        if (elapsed >= stat.itemRepeatBombLifetime) { Resolve(true); return; }
        if (elapsed < stat.itemRepeatBombDelay || Time.time < nextQuery) return;
        nextQuery = Time.time + 0.05f;
        query.Scan(context.targetPosition, stat.itemRepeatTriggerRadius, enemyMask);
        if (query.Enemies.Count > 0) Resolve(true);
    }

    private void Resolve(bool succeeded)
    {
        if (finished) return;
        finished = true;
        try { if (succeeded && context.CanContinue && impact != null) impact(); }
        catch { succeeded = false; throw; }
        finally
        {
            if (lease != null) lease.Finish(succeeded);
            lease = null;
            if (indicator != null) Destroy(indicator.gameObject);
            indicator = null;
            Action<bool> callback = resolved;
            resolved = null;
            impact = null;
            if (callback != null) callback(succeeded);
            Destroy(gameObject);
        }
    }

    private void OnDisable() => Resolve(false);
}
