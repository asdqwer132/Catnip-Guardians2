using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStatModifierAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Stat Modifier")]
public class EnemyStatModifierAction : EnemyPatternAction
{
    [Min(0f)] public float duration = 2f;

    [Header("Outgoing Stat")]
    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;
    public float attackCooldownMultiplier = 1f;
    public float attackRangeMultiplier = 1f;

    [Header("Incoming Damage")]
    public float incomingDamageMultiplier = 1f;

    [Header("Flow")]
    public bool waitUntilEnd = false;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        EnemyPatternRuntimeModifier modifier = new EnemyPatternRuntimeModifier
        {
            remainingTime = duration,
            moveSpeedMultiplier = moveSpeedMultiplier,
            attackDamageMultiplier = attackDamageMultiplier,
            attackCooldownMultiplier = attackCooldownMultiplier,
            attackRangeMultiplier = attackRangeMultiplier,
            incomingDamageMultiplier = incomingDamageMultiplier
        };

        context.AddRuntimeModifier(modifier);

        if (waitUntilEnd && duration > 0f)
            yield return new WaitForSeconds(duration);
    }
}
