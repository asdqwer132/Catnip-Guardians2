using UnityEngine;

public class EnemyPatternRuntimeModifier
{
    public float remainingTime;
    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;
    public float attackCooldownMultiplier = 1f;
    public float attackRangeMultiplier = 1f;
    public float incomingDamageMultiplier = 1f;

    public bool IsExpired => remainingTime <= 0f;

    public void Tick(float deltaTime)
    {
        remainingTime -= deltaTime;
    }
}
