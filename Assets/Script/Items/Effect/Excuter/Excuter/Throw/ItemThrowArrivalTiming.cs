using System;
using UnityEngine;

public enum ItemThrowArrivalMode { Fixed, RandomRange, WeightAndRandomOffset }
public enum ItemThrowArrivalDistribution { Uniform, PreferShort, PreferLong, PreferMiddle, PreferEdges, CustomCurve }

[Serializable]
public sealed class ItemThrowArrivalTiming
{
    [Tooltip("Fixed: 기존 Arrive Time / RandomRange: 최소~최대 랜덤 / WeightAndRandomOffset: 무게 기준 시간 ± 오프셋 범위에서 랜덤")]
    public ItemThrowArrivalMode mode = ItemThrowArrivalMode.Fixed;
    [Min(0.01f)] public float minArriveTime = 0.6f;
    [Min(0.01f)] public float maxArriveTime = 1.8f;
    [Min(0f)]
    [Tooltip("무게 모드에서 기준 시간 앞뒤로 허용하는 랜덤 편차(초). 최종 추첨 범위는 최소·최대 도착 시간 안으로 제한합니다.")]
    public float randomOffset = 0.15f;
    [Min(0f)]
    [Tooltip("무게 모드의 기준 시간 = Arrive Time + ItemData.weight × 이 값. 가방 총 무게는 사용하지 않습니다.")]
    public float secondsPerWeight = 0.2f;
    [Tooltip("도착 시간 추첨 함수. Short는 빠른 쪽, Long은 느린 쪽, Middle은 중앙, Edges는 양 끝을 선호합니다.")]
    public ItemThrowArrivalDistribution distribution = ItemThrowArrivalDistribution.PreferMiddle;
    [Tooltip("CustomCurve일 때 사용. X=균등 난수 0~1, Y=추첨 구간 내 위치 0~1. Y=0은 가장 빠른 시간, Y=1은 가장 느린 시간입니다. 확률 밀도 곡선이 아닌 난수 변환 함수입니다.")]
    public AnimationCurve customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // The caller supplies one random sample so a launch can capture a stable duration.
    public float Resolve(float baseArriveTime, float maxMoveTime, float itemWeight, float sample)
    {
        if (mode == ItemThrowArrivalMode.Fixed)
            return ClampDuration(baseArriveTime, maxMoveTime);

        float cap = Safe(maxMoveTime, 0.01f, 600f, 3f);
        float a = Safe(minArriveTime, 0.01f, 600f, 0.6f);
        float b = Safe(maxArriveTime, 0.01f, 600f, 1.8f);
        float low = Mathf.Min(Mathf.Min(a, b), cap);
        float high = Mathf.Min(Mathf.Max(a, b), cap);

        if (mode == ItemThrowArrivalMode.WeightAndRandomOffset)
        {
            float weight = Safe(itemWeight, 0f, 100000000f, 0f);
            float perWeight = Safe(secondsPerWeight, 0f, 600f, 0f);
            float basis = Safe(baseArriveTime, 0.01f, 600f, 0.6f);
            float center = Mathf.Clamp(basis + weight * perWeight, low, high);
            float offset = Safe(randomOffset, 0f, 600f, 0f);
            // Intersect before sampling to avoid piling out-of-range samples at a limit.
            low = Mathf.Max(low, center - offset);
            high = Mathf.Min(high, center + offset);
        }

        return Mathf.Lerp(low, high, TransformSample(sample));
    }

    public float TransformSample(float sample)
    {
        float u = Safe(sample, 0f, 1f, 0.5f);
        switch (distribution)
        {
            case ItemThrowArrivalDistribution.PreferShort: return u * u;
            case ItemThrowArrivalDistribution.PreferLong: return 1f - (1f - u) * (1f - u);
            case ItemThrowArrivalDistribution.PreferMiddle:
                return u <= 0.5f ? Mathf.Sqrt(u * 0.5f) : 1f - Mathf.Sqrt((1f - u) * 0.5f);
            case ItemThrowArrivalDistribution.PreferEdges: return u * u * (3f - 2f * u);
            case ItemThrowArrivalDistribution.CustomCurve:
                return customCurve != null && customCurve.length > 0
                    ? Safe(customCurve.Evaluate(u), 0f, 1f, u) : u;
            default: return u;
        }
    }

    public static float ClampDuration(float duration, float maxMoveTime)
        => Mathf.Min(Safe(duration, 0.01f, 600f, 0.6f), Safe(maxMoveTime, 0.01f, 600f, 3f));

    private static float Safe(float value, float minimum, float maximum, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
}
