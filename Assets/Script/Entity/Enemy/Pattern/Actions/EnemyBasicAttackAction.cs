using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBasicAttackAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Basic Attack")]
public class EnemyBasicAttackAction : EnemyPatternAction
{
    [Header("Damage")]
    [Tooltip("이 액션에서 사용할 전용 공격력입니다. ActorAttack.damage와 별개입니다.")]
    [Min(0f)] public float actionDamage = 10f;

    [Tooltip("켜면 기본 공격력에 액션 공격력을 더합니다. 끄면 액션 공격력만 사용합니다.")]
    public bool addBaseAttackDamage = false;

    [Tooltip("addBaseAttackDamage가 켜져 있을 때 기본 공격력에 곱할 값입니다.")]
    [Min(0f)] public float baseAttackDamageMultiplier = 1f;

    [Header("Timing")]
    [Tooltip("공격 애니메이션을 시작한 뒤 실제 데미지가 들어가기까지 걸리는 시간입니다.")]
    [Min(0f)] public float attackDelay = 0.25f;

    [Tooltip("데미지가 들어간 뒤 액션이 끝나기 전 추가로 기다리는 시간입니다.")]
    [Min(0f)] public float afterDamageDelay = 0f;

    [Tooltip("켜면 공격 애니메이션 길이만큼 액션을 유지합니다.")]
    public bool waitAnimationEnd = true;

    [Header("Range")]
    [Tooltip("켜면 ActorAttack의 공격 사거리 대신 아래 customAttackRange를 사용합니다.")]
    public bool useCustomAttackRange = false;

    [Min(0.01f)] public float customAttackRange = 1.5f;
    [Min(0f)] public float customAttackDistanceTolerance = 0.15f;

    [Tooltip("켜면 공격 시작 전에 사거리 체크를 합니다.")]
    public bool requireRangeBeforeStart = true;

    [Tooltip("켜면 데미지 적용 순간에도 사거리 체크를 합니다.")]
    public bool checkRangeBeforeDamage = true;

    [Header("Facing")]
    public bool faceTargetBeforeStart = true;
    public bool faceTargetBeforeDamage = true;

    [Header("Default AI")]
    public bool forceBlockDefaultAI = true;

    public override bool ForceBlockDefaultAI => forceBlockDefaultAI;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null || !context.HasEnemy || !context.HasTarget)
            yield break;

        float finalDamage = ResolveDamage(context);

        if (context.Attack != null)
        {
            yield return context.Attack.PlayActionAttack(
                finalDamage,
                attackDelay,
                useCustomAttackRange,
                customAttackRange,
                customAttackDistanceTolerance,
                requireRangeBeforeStart,
                checkRangeBeforeDamage,
                waitAnimationEnd,
                faceTargetBeforeStart,
                faceTargetBeforeDamage,
                afterDamageDelay
            );

            yield break;
        }

        yield return FallbackAttack(context, finalDamage);
    }

    private float ResolveDamage(EnemyPatternContext context)
    {
        float finalDamage = actionDamage;

        if (addBaseAttackDamage && context != null)
            finalDamage += context.GetAttackDamage() * baseAttackDamageMultiplier;

        return Mathf.Max(0f, finalDamage);
    }

    private IEnumerator FallbackAttack(EnemyPatternContext context, float finalDamage)
    {
        Vector2 direction = context.DirectionToTarget;

        if (direction.sqrMagnitude > 0.0001f)
            context.FaceDirection(direction);

        context.PlayAttack(direction);

        if (attackDelay > 0f)
            yield return new WaitForSeconds(attackDelay);
        else
            yield return null;

        if (!checkRangeBeforeDamage || IsTargetInRange(context))
            context.DamageTarget(finalDamage);

        if (afterDamageDelay > 0f)
            yield return new WaitForSeconds(afterDamageDelay);
    }

    private bool IsTargetInRange(EnemyPatternContext context)
    {
        if (context == null || !context.HasTarget)
            return false;

        float checkRange = useCustomAttackRange ? customAttackRange : context.Attack != null ? context.Attack.attackRange : customAttackRange;
        float checkTolerance = useCustomAttackRange ? customAttackDistanceTolerance : context.Attack != null ? context.Attack.attackDistanceTolerance : customAttackDistanceTolerance;

        float distance = context.GetDistanceToTarget();
        return Mathf.Abs(distance - checkRange) <= checkTolerance;
    }
}
