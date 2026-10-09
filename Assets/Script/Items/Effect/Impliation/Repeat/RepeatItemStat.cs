using System;
using UnityEngine;

[Serializable]
public class RepeatItemStat : IGameStat<RepeatItemStat>, IEffectScalableStat
{
    [Header("Repeat")]
    [Tooltip("아이템 묶음을 사용할 총 횟수입니다. 목록보다 많으면 처음부터 다시 선택합니다.")]
    public float itemRepeatCount = 3f;
    [Tooltip("효과 시작부터 첫 아이템 묶음을 사용하기까지의 대기 시간입니다.")]
    public float itemRepeatInterval = 0.2f;
    [Header("Step Intervals")]
    [Tooltip("각 묶음의 Interval After에 곱하는 값입니다. 1 = 그대로, 0.5 = 절반, 0 = 대기 없음. 버프로 변경할 수 있습니다.")]
    public float itemRepeatStepIntervalMultiplier = 1f;
    [Header("Placement")]
    public float itemRepeatForwardOffset;
    public float itemRepeatSideOffset;
    public float itemRepeatRadius = 2f;
    [Min(0f)]
    [Tooltip("Shotgun의 착지 거리를 Radius ± 이 값에서 발사체마다 독립 추첨합니다. 0이면 기존 고정 반지름을 사용하며, 거리는 0 아래로 내려가지 않습니다.")]
    public float itemRepeatShotgunRadiusOffset;
    public float itemRepeatSpreadAngle = 360f;
    public float itemRepeatDirectionAngle;
    [Header("Projectiles")]
    public float itemRepeatProjectileCountMultiplier = 1f;
    public float itemRepeatFlightTime = 0.3f;
    public float itemRepeatArcHeight = 0.8f;
    [Header("Impact Trigger")]
    public float itemRepeatBombDelay;
    public float itemRepeatBombLifetime = 5f;
    public float itemRepeatTriggerRadius = 0.5f;
    public RepeatItemStat Clone() => (RepeatItemStat)MemberwiseClone();
    public void Clamp()
    {
        itemRepeatCount = EffectStatUtility.Safe(itemRepeatCount, 1f, 128f, 1f);
        itemRepeatInterval = EffectStatUtility.Safe(itemRepeatInterval, 0f, 60f, 0.2f);
        itemRepeatStepIntervalMultiplier = EffectStatUtility.Safe(itemRepeatStepIntervalMultiplier, 0f, 100f, 1f);
        itemRepeatForwardOffset = EffectStatUtility.Safe(itemRepeatForwardOffset, -100f, 100f, 0f);
        itemRepeatSideOffset = EffectStatUtility.Safe(itemRepeatSideOffset, -100f, 100f, 0f);
        itemRepeatRadius = EffectStatUtility.Safe(itemRepeatRadius, 0f, 100f, 2f);
        itemRepeatShotgunRadiusOffset = EffectStatUtility.Safe(itemRepeatShotgunRadiusOffset, 0f, 100f, 0f);
        itemRepeatSpreadAngle = EffectStatUtility.Safe(itemRepeatSpreadAngle, 0f, 360f, 360f);
        itemRepeatDirectionAngle = EffectStatUtility.Safe(itemRepeatDirectionAngle, -360f, 360f, 0f);
        itemRepeatProjectileCountMultiplier = EffectStatUtility.Safe(itemRepeatProjectileCountMultiplier, 0f, 128f, 1f);
        itemRepeatFlightTime = EffectStatUtility.Safe(itemRepeatFlightTime, 0.01f, 60f, 0.3f);
        itemRepeatArcHeight = EffectStatUtility.Safe(itemRepeatArcHeight, 0f, 100f, 0.8f);
        itemRepeatBombDelay = EffectStatUtility.Safe(itemRepeatBombDelay, 0f, 600f, 0f);
        itemRepeatBombLifetime = EffectStatUtility.Safe(itemRepeatBombLifetime, 0.01f, 600f, 5f);
        itemRepeatTriggerRadius = EffectStatUtility.Safe(itemRepeatTriggerRadius, 0f, 100f, 0.5f);
    }
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        itemRepeatForwardOffset *= scale.Range;
        itemRepeatSideOffset *= scale.Range;
        itemRepeatRadius *= scale.Range;
        itemRepeatShotgunRadiusOffset *= scale.Range;
        itemRepeatTriggerRadius *= scale.Range;
    }
}
