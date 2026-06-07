using System;
using UnityEngine;

[Serializable]
public class PlayerStat : IGameStat<PlayerStat>
{
    [Header("Move")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float maxMoveSpeed = 10f;

    [Header("Range")]
    [Min(0f)] public float minRange = 1f;
    [Min(0f)] public float maxRange = 5f;

    public PlayerStat Clone()
    {
        return new PlayerStat
        {
            moveSpeed = moveSpeed,
            maxMoveSpeed = maxMoveSpeed,
            minRange = minRange,
            maxRange = maxRange
        };
    }

    public void Clamp()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxMoveSpeed = Mathf.Max(0f, maxMoveSpeed);

        minRange = Mathf.Max(0f, minRange);
        maxRange = Mathf.Max(minRange, maxRange);

        moveSpeed = Mathf.Min(moveSpeed, maxMoveSpeed);
    }
}