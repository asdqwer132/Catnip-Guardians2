using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChargeAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Charge")]
public class EnemyChargeAction : EnemyPatternAction
{
    [Header("Move")]
    [Min(0f)] public float speed = 7f;
    [Min(0f)] public float duration = 0.6f;
    public EnemyPatternMoveCurve moveCurve = EnemyPatternMoveCurve.Linear;

    [Header("Hit")]
    [Min(0f)] public float hitRadius = 0.7f;
    public float damage = 0f;
    public float damageMultiplier = 1.2f;
    public bool damageOnce = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        Vector2 direction = context.DirectionToTarget;
        if (direction.sqrMagnitude <= 0.0001f)
            yield break;

        context.LookDirection(direction);

        float timer = 0f;
        bool damaged = false;

        while (timer < duration)
        {
            float curveMultiplier = GetCurveMultiplier(timer / Mathf.Max(0.0001f, duration));
            context.MoveBy(direction.normalized * speed * curveMultiplier * Time.deltaTime, true);

            if ((!damageOnce || !damaged) && hitRadius > 0f && context.IsTargetInRadius(hitRadius))
            {
                float finalDamage = damage > 0f ? damage : context.GetAttackDamage() * damageMultiplier;
                context.DamageTarget(finalDamage);
                damaged = true;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }

    private float GetCurveMultiplier(float t)
    {
        switch (moveCurve)
        {
            case EnemyPatternMoveCurve.EaseInOut:
                return Mathf.SmoothStep(0.3f, 1f, t);
            case EnemyPatternMoveCurve.Linear:
            default:
                return 1f;
        }
    }
}
