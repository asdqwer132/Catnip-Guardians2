using System;

public enum SummonPeriodicSpawnOrder { InOrder, Random, All }

// 타이머와 성공한 발동 횟수는 각 모듈 Runtime에만 저장합니다.
public sealed class SummonPeriodicSpawnSchedule
{
    private float elapsed;
    private int next;
    public int ActivationCount { get; private set; }
    public bool Advance(float deltaTime, float interval, bool limited, int maximum, int effectCount,
        SummonPeriodicSpawnOrder order, Func<int, int> randomIndex, Func<int, bool> execute,
        Func<bool> canContinue)
    {
        if (effectCount <= 0 || execute == null || canContinue == null || !canContinue() ||
            (limited && ActivationCount >= Math.Max(1, maximum))) return false;
        elapsed += EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
        if (elapsed < EffectStatUtility.Safe(interval, 0.01f, 600f, 1f)) return false;
        elapsed = 0f; // 긴 프레임에도 한꺼번에 과도한 소환을 하지 않습니다.
        int start = order == SummonPeriodicSpawnOrder.Random
            ? Math.Max(0, Math.Min(effectCount - 1, randomIndex(effectCount))) : next % effectCount;
        bool activated = false;
        for (int i = 0; i < (order == SummonPeriodicSpawnOrder.All ? effectCount : 1); i++)
        {
            if (!canContinue()) break;
            activated |= execute((start + i) % effectCount);
        }
        next = (start + 1) % effectCount;
        if (activated) ActivationCount++;
        return activated;
    }
}
