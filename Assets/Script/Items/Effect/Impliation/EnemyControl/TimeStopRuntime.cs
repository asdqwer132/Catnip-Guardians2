using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum TimeStopTargets
{
    None = 0,
    EnemyActions = 1,
    EnemyProjectiles = 2,
    EnemySpawning = 4,
    EnemyStatusTimers = 8
}

// 정지 상태 대신 각 실행의 정지 이유를 보관한다. 중첩 효과의 종료는 다른 이유를 풀지 않는다.
public static class TimeStopRuntime
{
    private static readonly List<TimeStopHandle> handles = new List<TimeStopHandle>();
    public static TimeStopHandle Acquire(TimeStopTargets targets, ItemEffectContext context = null)
    {
        TimeStopHandle handle = new TimeStopHandle(targets, context);
        handles.Add(handle);
        return handle;
    }
    public static bool IsStopped(TimeStopTargets target)
    {
        for (int i = handles.Count - 1; i >= 0; i--)
        {
            TimeStopHandle handle = handles[i];
            if (handle.IsValid && (handle.Targets & target) != 0) return true;
        }
        return false;
    }
    internal static void Release(TimeStopHandle handle) { handles.Remove(handle); }
}

public sealed class TimeStopHandle : IDisposable
{
    private bool disposed;
    private readonly int generation = ItemEffectRuntime.Generation;
    private readonly ItemEffectContext context;
    public TimeStopTargets Targets { get; private set; }
    public bool IsValid => !disposed && generation == ItemEffectRuntime.Generation &&
        (context == null || context.CanContinue);
    internal TimeStopHandle(TimeStopTargets targets, ItemEffectContext context)
    {
        Targets = targets;
        this.context = context;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        TimeStopRuntime.Release(this);
    }
}
