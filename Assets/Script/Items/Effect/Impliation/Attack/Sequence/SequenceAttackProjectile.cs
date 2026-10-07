using System;
using UnityEngine;

// 분산 폭탄은 비행 후 대기한다. 범위 피해는 폭발하는 순간에만 생성한다.
public sealed class SequenceAttackProjectile : MonoBehaviour
{
    private ItemEffectContext context;
    private ItemEffectData[] effects;
    private ItemEffectLease lease;
    private SequenceAttackStat stat;
    private BombTriggerMode trigger;
    private ContactFilter2D filter;
    private readonly Collider2D[] hits = new Collider2D[32];
    private Action resolved;
    private float timer, queryTimer, armedAtTime;
    private bool armed, finished;

    public void Init(ItemThrowMover mover, ItemEffectContext execution, ItemEffectData[] effects,
        BombTriggerMode trigger, SequenceAttackStat stat, LayerMask mask, Action resolved)
    {
        context = execution;
        this.effects = effects;
        this.trigger = trigger;
        this.stat = stat;
        this.resolved = resolved;
        lease = context.RetainLifetime();
        filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useLayerMask = true;
        filter.useTriggers = true;
        mover.InitMove(execution.usePosition, execution.targetPosition, stat.flightTime, Arrive);
    }

    private void Arrive()
    {
        if (finished || !context.CanContinue) return;
        armed = true;
        timer = queryTimer = 0f;
        armedAtTime = Time.time;
        if (trigger == BombTriggerMode.OnArrival) Explode();
    }

    private void Update()
    {
        if (finished || context == null) return;
        if (!context.CanContinue) { Cancel(); return; }
        if (!armed) return;
        timer = Mathf.Max(0f, Time.time - armedAtTime);
        if (trigger == BombTriggerMode.AfterDelay && timer >= stat.bombDelay)
        { Explode(); return; }
        if (trigger != BombTriggerMode.EnemyNearbyOrTimeout) return;
        if (timer >= stat.bombLifetime) { Explode(); return; }
        if (timer < stat.bombDelay) return;
        queryTimer -= Time.deltaTime;
        if (queryTimer > 0f || stat.triggerRadius <= 0f) return;
        queryTimer = 0.05f;
        int count = Physics2D.OverlapCircle(context.targetPosition, stat.triggerRadius, filter, hits);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            Enemy enemy = hit != null ? hit.GetComponentInParent<Enemy>() : null;
            if (enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled) found = true;
            hits[i] = null;
        }
        if (found) Explode();
    }

    private void Explode()
    {
        if (finished) return;
        finished = true;
        try { SequenceAttackRunner.ExecuteEffectsAt(effects, context); }
        finally
        {
            if (lease != null) lease.Finish();
            lease = null;
            Action callback = resolved;
            resolved = null;
            if (callback != null) callback();
            Destroy(gameObject);
        }
    }

    private void Cancel()
    {
        finished = true;
        if (lease != null) lease.Cancel();
        lease = null;
        resolved = null;
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        if (lease != null) lease.Cancel();
        lease = null;
        resolved = null;
    }
}
