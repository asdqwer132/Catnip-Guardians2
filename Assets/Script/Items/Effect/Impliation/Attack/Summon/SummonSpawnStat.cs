using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class SummonSpawnStat : IGameStat<SummonSpawnStat>, IEffectScalableStat
{
    [FormerlySerializedAs("summonLifeTime")]
    [Min(0.01f)] public float lifetime = 5f;
    [Tooltip("버프 계산 후 소수점을 버려 0~128개로 결정합니다. 0이면 소환하지 않습니다.")]
    [Min(0f)] public float count = 1f;
    [Min(0f)] public float spreadRadius;
    [Range(0f, 360f)] public float spreadAngle = 360f;
    [Tooltip("Forward 배치에서 다음 소환물까지의 앞쪽 간격입니다.")]
    public float forwardSpacing = 1f;
    [Tooltip("Forward 배치에서 다음 소환물까지의 옆쪽 간격입니다.")]
    public float sideSpacing;

    public int SpawnCount => Mathf.FloorToInt(EffectStatUtility.Safe(count, 0f, 128f, 1f));
    public SummonSpawnStat Clone() => (SummonSpawnStat)MemberwiseClone();
    public void Clamp()
    {
        lifetime = EffectStatUtility.Safe(lifetime, 0.01f, 600f, 5f);
        count = EffectStatUtility.Safe(count, 0f, 128f, 1f);
        spreadRadius = EffectStatUtility.Safe(spreadRadius, 0f, 100f, 0f);
        spreadAngle = EffectStatUtility.Safe(spreadAngle, 0f, 360f, 360f);
        forwardSpacing = EffectStatUtility.Safe(forwardSpacing, -100f, 100f, 1f);
        sideSpacing = EffectStatUtility.Safe(sideSpacing, -100f, 100f, 0f);
    }

    public Vector3 GetPosition(AttackPlacementMode mode, Vector3 origin, Vector3 direction,
        int index, int total, AttackSpreadStartMode start = AttackSpreadStartMode.CenteredOnDirection,
        bool clockwise = false)
    {
        origin.z = direction.z = 0f;
        if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
        Vector3 position = AttackPlacement.Position(mode, origin, direction.normalized, index, total,
            forwardSpacing, sideSpacing, spreadRadius, spreadAngle,
            shotgunDistribution: AttackSpreadDistribution.Even, spreadStartMode: start, clockwiseSpread: clockwise);
        position.z = 0f;
        return position;
    }

    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        lifetime *= scale.Duration;
        spreadRadius *= scale.Range;
        forwardSpacing *= scale.Range;
        sideSpacing *= scale.Range;
    }
}
