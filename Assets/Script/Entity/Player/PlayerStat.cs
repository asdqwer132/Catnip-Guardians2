using System;
using UnityEngine;

[Serializable]
public class PlayerStat : IGameStat<PlayerStat>
{

    [Header("Move")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float maxMoveSpeed = 10f;
    [Tooltip("전체 아이템의 쿨다운 회복 속도 배율. 1 = 기본, 1.2 = 20% 빠르게 회복합니다. 가방 공통 쿨다운에는 적용하지 않습니다.")]
    [Min(0f)] public float cooldownRecoveryRate = 1f;

    [Header("Range")]
    [Min(0f)] public float minRange = 1f;
    [Min(0f)] public float maxRange = 5f;

    public PlayerStat Clone()
    {
        return new PlayerStat
        {
            moveSpeed = moveSpeed,
            maxMoveSpeed = maxMoveSpeed,
            cooldownRecoveryRate = cooldownRecoveryRate,
            minRange = minRange,
            maxRange = maxRange
        };
    }

    public void Clamp()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxMoveSpeed = Mathf.Max(0f, maxMoveSpeed);
        cooldownRecoveryRate = EffectStatUtility.Safe(cooldownRecoveryRate, 0f, 100f, 1f);

        minRange = Mathf.Max(0f, minRange);
        maxRange = Mathf.Max(minRange, maxRange);

        moveSpeed = Mathf.Min(moveSpeed, maxMoveSpeed);
    }
}
