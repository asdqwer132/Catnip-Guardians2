using System;
using System.Collections.Generic;
using UnityEngine;

// 호출 완료와 실제 효과 종료를 구분한다. 자식 효과/투사체/버프가 모두 끝나야 완료한다.
public sealed class ItemEffectLifetime
{
    private int pending = 1;
    private bool cancelled;
    private bool closed;
    private readonly int generation = ItemEffectRuntime.Generation;
    private readonly ItemEffectLease parent;
    private Action onCompleted;

    public bool IsCancelled => cancelled || generation != ItemEffectRuntime.Generation;
    public bool IsFinished => pending == 0;
    public bool TracksCompletion { get; private set; }

    public ItemEffectLifetime(ItemEffectLifetime parentLifetime = null, Action onCompleted = null, bool trackCompletion = true)
    {
        TracksCompletion = trackCompletion;
        parent = parentLifetime != null && parentLifetime.TracksCompletion ? parentLifetime.Retain() : null;
        this.onCompleted = onCompleted;
    }

    public ItemEffectLease Retain()
    {
        if (pending == 0)
            return null;
        pending++;
        return new ItemEffectLease(this);
    }

    public void Close(bool succeeded = true)
    {
        if (closed)
            return;
        closed = true;
        Release(succeeded);
    }

    internal void Release(bool succeeded)
    {
        if (pending == 0)
            return;
        cancelled |= !succeeded;
        pending--;
        if (pending != 0)
            return;
        Action callback = onCompleted;
        onCompleted = null;
        try
        {
            if (!IsCancelled && callback != null)
                callback();
        }
        finally
        {
            if (parent != null)
                // 적 사망/면역 등 한 하위 효과의 종료가 전체 연속 공격을 취소하지 않는다.
                // 전투 초기화는 세대 번호로 모든 실행에 함께 전달한다.
                parent.Finish(generation == ItemEffectRuntime.Generation);
        }
    }
}

// 중복 종료를 방지한다. 자연 종료는 Finish, 강제 정리는 Cancel을 호출한다.
public sealed class ItemEffectLease
{
    private ItemEffectLifetime lifetime;
    internal ItemEffectLease(ItemEffectLifetime lifetime) { this.lifetime = lifetime; }
    public void Finish(bool succeeded = true)
    {
        ItemEffectLifetime current = lifetime;
        lifetime = null;
        if (current != null)
            current.Release(succeeded);
    }
    public void Cancel() { Finish(false); }
}

// 같은 버프가 갱신되어도 활성 버프 하나의 종료 연출은 한 번만 재생한다.
public sealed class ItemEffectCompletionGroup
{
    private List<ItemEffectLease> leases;
    private EffectVisualData visual;
    private Transform target;
    private Vector3 position;
    private int generation;

    public void Track(ItemEffectContext context, EffectVisualData endVisual, Transform followTarget = null)
    {
        if (context == null)
            return;
        // 종료 연출을 기다리는 상위 실행이 없으면 갱신 버프에 완료 핸들을 쌓지 않는다.
        ItemEffectLease lease = context.RetainLifetime(completionOnly: true);
        if (lease != null)
        {
            if (leases == null) leases = new List<ItemEffectLease>();
            leases.Add(lease);
        }
        visual = endVisual;
        target = followTarget;
        position = context.targetPosition;
        generation = ItemEffectRuntime.Generation;
    }

    public void Finish(bool completed)
    {
        EffectVisualData endVisual = visual;
        Vector3 endPosition = target != null ? target.position : position;
        List<ItemEffectLease> finished = leases;
        visual = null;
        target = null;
        leases = null;
        try
        {
            if (completed && generation == ItemEffectRuntime.Generation && endVisual != null)
                endVisual.Play(new EffectVisualContext(endPosition, Quaternion.identity));
        }
        finally
        {
            if (finished != null)
                for (int i = 0; i < finished.Count; i++)
                    finished[i].Finish(completed);
        }
    }
}

public static class ItemEffectRuntime
{
    public static int Generation { get; private set; }
    public static void CancelAll() { unchecked { Generation++; } }
}
