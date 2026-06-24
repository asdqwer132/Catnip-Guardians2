using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTeleportAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Teleport")]
public class EnemyTeleportAction : EnemyPatternAction
{
    [Min(0f)] public float distanceFromTarget = 1f;
    public bool behindTargetFromEnemy = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null || !context.HasEnemy)
            yield break;

        ActorMover mover = context.Enemy.mover;
        if (mover == null)
            yield break;

        Transform target = context.GetTargetTransform();

        if (target == null)
            yield break;

        Vector2 direction;

        if (behindTargetFromEnemy)
        {
            direction = target.position - context.Enemy.transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;
        }
        else
        {
            direction = context.DirectionToTarget;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            direction = -direction;
        }

        Vector3 teleportPosition = target.position + (Vector3)(direction.normalized * distanceFromTarget);
        Vector2 lookDirection = -direction;

        mover.Teleport(teleportPosition, lookDirection);

        yield return null;
    }
}