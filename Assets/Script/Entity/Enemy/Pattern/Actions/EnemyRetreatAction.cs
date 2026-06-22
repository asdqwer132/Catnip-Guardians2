using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRetreatAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Retreat")]
public class EnemyRetreatAction : EnemyPatternAction
{
    [Header("Move")]
    [Min(0f)] public float speed = 3f;
    [Min(0f)] public float duration = 0.4f;


    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        Transform target = context.GetTargetTransform();

        if (target == null)
            yield break;

        Vector2 direction = GetRetreatDirection(context, target, Vector2.left);
        float timer = 0f;

        while (timer < duration)
        {
            target = context.GetTargetTransform();

            if (target == null)
                break;

            //if (updateDirectionEveryFrame)
            //    direction = GetRetreatDirection(context, target, direction);

            context.MoveDirection(direction, speed);

            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }

    private Vector2 GetRetreatDirection(EnemyPatternContext context, Transform target, Vector2 fallback)
    {
        if (target == null)
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector2.left;

        Vector2 direction = context.Position - target.position;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = fallback;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.left;

        return direction.normalized;
    }
}