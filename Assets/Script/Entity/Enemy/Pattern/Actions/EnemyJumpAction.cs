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
        if (context == null || !context.HasEnemy)
            yield break;

        ActorMover mover = context.Enemy.mover;
        if (mover == null)
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

        mover.Stop();
        context.FaceDirection((Vector2)(endPosition - startPosition));

        while (timer < duration)
        {
            float deltaTime = Time.deltaTime;
            float t = timer / Mathf.Max(0.0001f, duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, smoothT);
            position.y += Mathf.Sin(t * Mathf.PI) * visualArcHeight;

            mover.SetPosition(position);

            timer += deltaTime;
            yield return null;
        }

        mover.SetPosition(endPosition);
        mover.Stop();

        if (damageOnLanding && hitRadius > 0f && context.IsTargetInRadius(hitRadius))
        {
            float finalDamage = damage > 0f
                ? damage
                : context.GetAttackDamage() * damageMultiplier;

            context.DamageTarget(finalDamage);
        }
    }
}