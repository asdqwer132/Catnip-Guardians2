using System;
using UnityEngine;

public enum SessionThrowSpreadDistribution { RandomArea, EvenArc }

[Serializable]
public sealed class SessionItemThrowSpread
{
    public SessionThrowSpreadDistribution distribution;
    [Min(0f)] public float baseRadius = 0.3f;
    [Range(0f, 360f)] public float spreadAngle = 360f;
    [Tooltip("투척 시작점에서 목표를 향하는 방향에 더하는 회전각입니다.")]
    public float directionAngle;

    [Header("수량에 따른 퍼짐")]
    public bool scaleByItemCount = true;
    [Min(0f)] public float radiusPerAdditionalItem = 0.15f;
    [Min(0f)] public float maximumRadius = 3f;

    public float ResolveRadius(int itemCount)
    {
        float radius = EffectStatUtility.Safe(baseRadius, 0f, 10000f, 0f);
        if (scaleByItemCount)
        {
            float increase = EffectStatUtility.Safe(radiusPerAdditionalItem, 0f, 10000f, 0f);
            radius += Mathf.Max(0, itemCount - 1) * increase;
        }

        return Mathf.Min(radius, EffectStatUtility.Safe(maximumRadius, 0f, 10000f, 3f));
    }

    public Vector3 GetPosition(Vector3 center, Vector3 forward, int index, int count, float radius)
    {
        center.z = forward.z = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        forward = Quaternion.Euler(0f, 0f, EffectStatUtility.Safe(directionAngle, -360f, 360f, 0f)) * forward.normalized;
        var placement = distribution == SessionThrowSpreadDistribution.EvenArc
            ? AttackPlacementMode.CircleEven : AttackPlacementMode.CircleRandom;
        return AttackPlacement.Position(placement, center, forward, index, count, 0f, 0f, radius, spreadAngle);
    }
}
