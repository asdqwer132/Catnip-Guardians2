using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyJumpAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Jump To Target")]
public class EnemyJumpAction : EnemyPatternAction
{
    public EnemyPatternPointType targetPointType = EnemyPatternPointType.Target;

    [Header("Jump")]
    [Min(0.05f)] public float duration = 0.5f;
    [Min(0f)] public float endDistanceFromTarget = 0.6f;
    [Min(0f)] public float visualArcHeight = 0.4f;

    [Header("Hit On Landing")]
    public bool damageOnLanding = true;
    [Min(0f)] public float hitRadius = 1f;
    public float damage = 0f;
    public float damageMultiplier = 1.3f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context.Enemy == null)
            yield break;

        Vector3 startPosition = context.Position;
        Vector3 targetPosition = context.ResolvePoint(targetPointType, 0f, 0f);

        Vector2 directionFromTarget = startPosition - targetPosition;
        if (directionFromTarget.sqrMagnitude <= 0.0001f)
            directionFromTarget = -context.DirectionToTarget;
        if (directionFromTarget.sqrMagnitude <= 0.0001f)
            directionFromTarget = Vector2.left;

        Vector3 endPosition = targetPosition + (Vector3)(directionFromTarget.normalized * endDistanceFromTarget);
        float timer = 0f;

        context.LookDirection((Vector2)(endPosition - startPosition));

        while (timer < duration)
        {
            float t = timer / Mathf.Max(0.0001f, duration);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Sin(t * Mathf.PI) * visualArcHeight;

            context.Enemy.transform.position = position;

            timer += Time.deltaTime;
            yield return null;
        }

        context.Enemy.transform.position = endPosition;
        context.StopMove();

        if (damageOnLanding && hitRadius > 0f && context.IsTargetInRadius(hitRadius))
        {
            float finalDamage = damage > 0f ? damage : context.GetAttackDamage() * damageMultiplier;
            context.DamageTarget(finalDamage);
        }
    }
}
