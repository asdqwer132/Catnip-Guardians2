using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCircleMoveAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Circle Move Around Target")]
public class EnemyCircleMoveAction : EnemyPatternAction
{
    [Header("Move")]
    [Min(0f)] public float duration = 0.8f;
    [Min(0f)] public float angle = 180f;
    [Min(0f)] public float radius = 1.5f;
    public bool clockwise = true;

    [Header("Option")]
    public bool tickDefaultAttack = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        Transform target = context.GetTargetTransform();

        if (target == null)
            yield break;

        Vector2 startFromTarget = context.Position - target.position;

        if (startFromTarget.sqrMagnitude <= 0.0001f)
            startFromTarget = Vector2.right;

        float startRadius = startFromTarget.magnitude;
        float startAngle = Mathf.Atan2(startFromTarget.y, startFromTarget.x) * Mathf.Rad2Deg;
        float signedAngle = clockwise ? -angle : angle;
        float timer = 0f;

        while (timer < duration)
        {
            target = context.GetTargetTransform();

            if (target == null)
                break;

            float t = timer / Mathf.Max(0.0001f, duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentAngle = startAngle + signedAngle * smoothT;
            float currentRadius = Mathf.Lerp(startRadius, radius, smoothT);

            Vector2 offset = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            ) * currentRadius;

            Vector3 nextPosition = target.position + (Vector3)offset;
            Vector2 delta = nextPosition - context.Position;

            context.MoveBy(delta);

            if (tickDefaultAttack)
                context.TickDefaultAttack();

            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }
}