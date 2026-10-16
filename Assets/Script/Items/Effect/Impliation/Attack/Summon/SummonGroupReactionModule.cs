using System;
using UnityEngine;

// 공유 에셋은 설정만 보관하고 실제 실행 상태는 무리/소유자마다 따로 만듭니다.
public abstract class SummonGroupReactionModule : ScriptableObject
{
    public SummonGroupPopulationCondition condition = new SummonGroupPopulationCondition();
    public abstract SummonGroupReactionRuntime CreateRuntime();
}

public abstract class SummonGroupReactionRuntime : IDisposable
{
    // 소환/회수가 다시 무리 변화를 알리면 같은 프레임에 여러 번 호출될 수 있습니다.
    // 호출 횟수 대신 조건의 진입/이탈과 현재 실행 상태를 기준으로 처리하세요.
    public abstract void Refresh(SummonGroupReactionContext context);
    public virtual void Tick(float deltaTime) { }
    public abstract void Dispose();
}

public sealed class SummonGroupReactionContext
{
    public SummonGroupPopulation population;
    public Vector3 lastSummonPosition;
    public ItemEffectContext source;

    // 특정 소환물 하나의 수명이 아닌 무리 조건에 귀속됩니다. 마지막 소환물이 죽어도
    // 나머지 무리가 조건을 만족한다면 특수 소환물을 계속 유지할 수 있습니다.
    public ItemEffectContext CreateExecution(Vector3 position, ItemEffectLifetime lifetime)
    {
        if (source == null) return null;
        return new ItemEffectContext(source.owner, source.sourceItemData, position, position,
            source.sourceBag, buffManager: source.buffManager, direction: source.direction)
        {
            lifetime = lifetime,
            damageMultiplier = source.damageMultiplier,
            healingMultiplier = source.healingMultiplier,
            rangeMultiplier = source.rangeMultiplier,
            durationMultiplier = source.durationMultiplier,
            damageOverride = source.damageOverride,
            consumeUseBuffs = false,
            allowItemUseReactions = false,
            allowSummonGroupReactions = false,
            countsForSummonGroupPopulation = false
        };
    }
}
