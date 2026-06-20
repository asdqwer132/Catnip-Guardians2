using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTeleportAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Teleport")]
public class EnemyTeleportAction : EnemyPatternAction
{
    [Min(0f)] public float distanceFromTarget = 1f;
    public bool behindTargetFromEnemy = true;
    public GameObject beforeEffectPrefab;
    public GameObject afterEffectPrefab;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context.Enemy == null)
            yield break;

        Transform target = context.GetTargetTransform();
        if (target == null)
            yield break;

        if (beforeEffectPrefab != null)
            Instantiate(beforeEffectPrefab, context.Position, Quaternion.identity);

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
        context.Enemy.transform.position = teleportPosition;
        context.LookDirection(-direction);

        if (afterEffectPrefab != null)
            Instantiate(afterEffectPrefab, teleportPosition, Quaternion.identity);

        yield return null;
    }
}
