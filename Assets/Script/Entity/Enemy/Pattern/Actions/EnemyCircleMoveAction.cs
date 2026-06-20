using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCircleMoveAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Circle Move Around Target")]
public class EnemyCircleMoveAction : EnemyPatternAction
{
    [Min(0f)] public float duration = 0.8f;
    [Min(0f)] public float angle = 180f;
    [Min(0f)] public float radius = 1.5f;
    public bool clockwise = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context.Enemy == null)
            yield break;

        Transform target = context.GetTargetTransform();
        if (target == null)
            yield break;

        Vector2 fromTarget = context.Position - target.position;
        if (fromTarget.sqrMagnitude <= 0.0001f)
            fromTarget = Vector2.right;

        float startAngle = Mathf.Atan2(fromTarget.y, fromTarget.x) * Mathf.Rad2Deg;
        float signedAngle = clockwise ? -angle : angle;
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / Mathf.Max(0.0001f, duration);
            float currentAngle = startAngle + signedAngle * Mathf.SmoothStep(0f, 1f, t);
            Vector2 offset = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            ) * radius;

            Vector3 nextPosition = target.position + (Vector3)offset;
            Vector2 moveDirection = nextPosition - context.Enemy.transform.position;
            context.LookDirection(-offset);
            context.Enemy.transform.position = nextPosition;
            context.PlayMove(moveDirection);

            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }
}
