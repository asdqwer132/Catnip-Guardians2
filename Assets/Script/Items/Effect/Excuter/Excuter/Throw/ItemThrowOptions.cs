using System;
using UnityEngine;

// 호출마다 전달하는 연출 설정입니다. 실행기나 프리팹 원본의 설정을 바꾸지 않습니다.
[Serializable]
public sealed class ItemThrowOptions
{
    [Tooltip("이 호출의 투척 프리팹. 비우면 실행기의 기본 프리팹을 사용합니다.")]
    public ItemThrowMover projectilePrefab;
    [Tooltip("비우면 추가 사용하는 아이템의 아이콘을 사용합니다.")]
    public Sprite projectileSprite;
    [Tooltip("시작과 목표가 같아도 지정한 비행 시간 동안 제자리 아크/스핀을 재생합니다.")]
    public bool allowStationaryThrow = true;
    [Tooltip("켜면 아래 모션을 런타임 투척물에 적용합니다. 끄면 선택한 프리팹의 모션을 사용합니다.")]
    public bool overrideMotion;
    public ItemThrowMotionSettings motion = new ItemThrowMotionSettings();

    public void ApplyTo(ItemThrowMover mover)
    {
        if (overrideMotion && motion != null && mover != null) motion.ApplyTo(mover);
    }
}

[Serializable]
public sealed class ItemThrowMotionSettings
{
    [Header("비행 시간")]
    [Min(0.01f)] public float arriveTime = 0.6f;
    [Min(0.01f)] public float maxMoveTime = 3f;
    public ItemThrowArrivalTiming arrivalTiming = new ItemThrowArrivalTiming();
    [Header("아크 · 모두 0이면 직선")]
    [Min(0f)] public float arcHeight = 1.5f;
    public bool autoArcHeightByDistance = true;
    [Min(0f)] public float minArcHeight = 0.6f;
    [Min(0f)] public float maxArcHeight = 3f;
    [Min(0f)] public float arcHeightDistanceMultiplier = 0.25f;
    [Range(0.1f, 0.9f)] public float arcPeakProgress = 0.45f;
    [Header("회전")]
    public bool spinWhileMoving = true;
    public float spinSpeed = 540f;
    [Tooltip("켜면 현재 이동 방향을 바라보며 스핀보다 우선합니다.")]
    public bool faceMoveDirection;
    public float directionAngleOffset = -90f;

    public void ApplyTo(ItemThrowMover mover)
    {
        mover.maxMoveTime = EffectStatUtility.Safe(maxMoveTime, 0.01f, 600f, 3f);
        mover.arriveTime = ItemThrowArrivalTiming.ClampDuration(arriveTime, mover.maxMoveTime);
        mover.arrivalTiming = arrivalTiming != null ? arrivalTiming.Clone() : new ItemThrowArrivalTiming();
        mover.arcHeight = EffectStatUtility.Safe(arcHeight, 0f, 100f, 1.5f);
        mover.autoArcHeightByDistance = autoArcHeightByDistance;
        float minimum = EffectStatUtility.Safe(minArcHeight, 0f, 100f, 0.6f);
        float maximum = EffectStatUtility.Safe(maxArcHeight, 0f, 100f, 3f);
        mover.minArcHeight = Mathf.Min(minimum, maximum); mover.maxArcHeight = Mathf.Max(minimum, maximum);
        mover.arcHeightDistanceMultiplier = EffectStatUtility.Safe(arcHeightDistanceMultiplier, 0f, 100f, 0.25f);
        mover.arcPeakProgress = EffectStatUtility.Safe(arcPeakProgress, 0.1f, 0.9f, 0.45f);
        mover.spinWhileMoving = spinWhileMoving;
        mover.spinSpeed = EffectStatUtility.Safe(spinSpeed, -36000f, 36000f, 540f);
        mover.faceMoveDirection = faceMoveDirection;
        mover.directionAngleOffset = EffectStatUtility.Safe(directionAngleOffset, -3600f, 3600f, -90f);
    }
}
