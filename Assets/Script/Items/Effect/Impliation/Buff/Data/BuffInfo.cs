using System;
using UnityEngine;

[Serializable]
public class BuffInfo : IGameStat<BuffInfo>, IEffectScalableStat
{
    [Header("Stack")]
    public BuffStackMode stackMode = BuffStackMode.Refresh;
    [Min(1)] public int maxStack = 1;

    [Tooltip("Refresh: 시간/횟수 초기화 / AddRemaining: 남은 시간 또는 횟수에 이번 부여량을 더합니다. Stack Mode와 별개입니다.")]
    public BuffReapplyMode reapplyMode = BuffReapplyMode.Refresh;

    [Header("Timing")]
    [Tooltip("아이템 대상의 UseCount 버프는 마지막 사용 보호를 위해 Snapshot에 저장됩니다. Time/Infinite는 이 설정을 따릅니다.")]
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;

    [Header("Limit")]
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;
    [Tooltip("Time 버프를 부여할 때 계산하는 지속시간(초). BuffInfo.duration 모디파이어로 늘릴 수 있으며, 이미 부여된 버프의 남은 시간은 바뀌지 않습니다.")]
    [Min(0.01f)] public float duration = 1f;
    [Min(1)] public int maxUseCount = 1;

    [Header("Use Count Consumption")]
    [Tooltip("WhenBuffApplied: 버프 적용 / AnyItemUsed: 모든 아이템 / SpecificItemsUsed: 지정 아이템 사용 시 차감")]
    public BuffUseCountConsumeMode useCountConsumeMode = BuffUseCountConsumeMode.WhenBuffApplied;
    [Tooltip("SpecificItemsUsed일 때만 사용. 같은 ItemData를 여러 번 넣어도 사용 1회당 1만 차감합니다.")]
    public ItemData[] consumeItems;

    public BuffInfo Clone()
    {
        return new BuffInfo
        {
            stackMode = stackMode,
            maxStack = maxStack,
            reapplyMode = reapplyMode,
            applyTiming = applyTiming,
            useLimitType = useLimitType,
            duration = duration,
            maxUseCount = maxUseCount,
            useCountConsumeMode = useCountConsumeMode,
            consumeItems = consumeItems == null ? null : (ItemData[])consumeItems.Clone()
        };
    }

    public void Clamp()
    {
        duration = EffectStatUtility.Safe(duration, 0.01f, float.MaxValue, 1f);
        maxStack = Mathf.Max(1, maxStack);
        maxUseCount = Mathf.Max(1, maxUseCount);

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;
    }

    public void ApplyExecutionScale(EffectExecutionScale scale) => duration *= scale.Duration;
}
