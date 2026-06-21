using System.Collections;
using UnityEngine;

public class ActorAttack : MonoBehaviour
{
    [Header("Attack Stat")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float attackDistanceTolerance = 0.15f;

    [Header("Facing")]
    public bool faceTargetWhileInAttackRange = true;
    public bool faceTargetBeforeDamage = true;

    [Header("Components")]
    public ActorTarget target;
    public ActorVisual visual;

    [Header("Debug")]
    [SerializeField] private bool isAttackStopped;
    [SerializeField] private bool isActionAttackPlaying;

    public bool IsAttacking { get; private set; }
    public bool IsAttackStopped => isAttackStopped;
    public bool IsActionAttackPlaying => isActionAttackPlaying;

    private float attackTimer = 0f;
    private Coroutine attackCoroutine;
    private Vector2 currentAttackDirection = Vector2.right;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<ActorTarget>();

        if (visual == null)
            visual = GetComponent<ActorVisual>();
    }

    public void SetAttackStat(float newDamage, float newRange, float newCooldown)
    {
        damage = newDamage;
        attackRange = Mathf.Max(0.01f, newRange);
        attackCooldown = Mathf.Max(0.01f, newCooldown);
    }

    #region Stop State

    public void SetAttackStopped(bool stopped)
    {
        if (isAttackStopped == stopped)
            return;

        isAttackStopped = stopped;

        if (isAttackStopped)
            ForceStop();
    }

    public void ForceStop()
    {
        CancelAttack();

        if (visual != null)
            visual.ForceIdle(GetAttackDirection(), true, false);
    }

    #endregion

    #region Range

    public float GetDistanceToTarget()
    {
        if (target == null)
            return float.MaxValue;

        return target.GetDistanceFrom(transform);
    }

    public bool IsTargetAtAttackDistance()
    {
        return IsTargetAtAttackDistance(attackRange, attackDistanceTolerance);
    }

    public bool IsTargetAtAttackDistance(float checkRange, float checkTolerance)
    {
        if (target == null)
            return false;

        if (!target.HasTarget)
            return false;

        float distance = GetDistanceToTarget();
        return Mathf.Abs(distance - checkRange) <= checkTolerance;
    }

    #endregion

    #region Default Attack

    public void TickAttack()
    {
        if (isAttackStopped)
            return;

        if (IsAttacking)
            return;

        if (target == null || !target.HasTarget)
            return;

        if (!IsTargetAtAttackDistance())
            return;

        if (faceTargetWhileInAttackRange)
            FaceTarget();

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        attackCoroutine = StartCoroutine(DefaultAttackRoutine());
        attackTimer = attackCooldown;
    }

    private IEnumerator DefaultAttackRoutine()
    {
        IsAttacking = true;
        isActionAttackPlaying = false;

        currentAttackDirection = GetAttackDirection();

        if (visual != null)
        {
            visual.PlayAttack(currentAttackDirection);
            yield return visual.WaitCurrentAnimationEnd();
        }
        else
        {
            yield return null;
        }

        IsAttacking = false;
        attackCoroutine = null;
    }

    #endregion

    #region Action Attack

    public IEnumerator PlayActionAttack(
        float actionDamage,
        float attackDelay,
        bool useCustomRange,
        float customRange,
        float customTolerance,
        bool requireRangeBeforeStart,
        bool checkRangeBeforeDamage,
        bool waitAnimationEnd,
        bool faceTargetBeforeStart,
        bool faceTargetBeforeActionDamage,
        float afterDamageDelay)
    {
        if (isAttackStopped)
            yield break;

        if (target == null || !target.HasTarget)
            yield break;

        float checkRange = useCustomRange ? customRange : attackRange;
        float checkTolerance = useCustomRange ? customTolerance : attackDistanceTolerance;

        if (requireRangeBeforeStart && !IsTargetAtAttackDistance(checkRange, checkTolerance))
            yield break;

        CancelAttack();

        IsAttacking = true;
        isActionAttackPlaying = true;
        attackCoroutine = null;

        currentAttackDirection = GetAttackDirection();

        if (faceTargetBeforeStart)
            FaceTarget();

        if (visual != null)
            visual.PlayAttack(currentAttackDirection);

        float safeAttackDelay = Mathf.Max(0f, attackDelay);
        float safeAfterDamageDelay = Mathf.Max(0f, afterDamageDelay);
        bool damaged = false;

        if (waitAnimationEnd && visual != null && visual.animator != null)
        {
            yield return null;

            AnimatorStateInfo stateInfo = visual.animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = Mathf.Max(0f, stateInfo.length);
            float elapsed = 0f;

            while (elapsed < animationLength)
            {
                if (isAttackStopped)
                    break;

                if (!damaged && elapsed >= safeAttackDelay)
                {
                    ApplyActionDamage(actionDamage, checkRangeBeforeDamage, checkRange, checkTolerance, faceTargetBeforeActionDamage);
                    damaged = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (!damaged)
        {
            if (safeAttackDelay > 0f)
                yield return new WaitForSeconds(safeAttackDelay);

            if (!isAttackStopped)
                ApplyActionDamage(actionDamage, checkRangeBeforeDamage, checkRange, checkTolerance, faceTargetBeforeActionDamage);
        }

        if (safeAfterDamageDelay > 0f)
            yield return new WaitForSeconds(safeAfterDamageDelay);

        IsAttacking = false;
        isActionAttackPlaying = false;
    }

    private void ApplyActionDamage(float actionDamage, bool checkRange, float checkRangeValue, float checkTolerance, bool faceBeforeDamage)
    {
        if (isAttackStopped)
            return;

        if (target == null || !target.HasTarget)
            return;

        if (checkRange && !IsTargetAtAttackDistance(checkRangeValue, checkTolerance))
            return;

        if (faceBeforeDamage)
            FaceTarget();

        target.DamageTarget(Mathf.Max(0f, actionDamage));
    }

    #endregion

    #region Direction

    public void FaceTarget()
    {
        Vector2 attackDirection = GetAttackDirection();

        if (attackDirection.sqrMagnitude <= 0.0001f)
            return;

        currentAttackDirection = attackDirection;

        if (visual != null)
            visual.LookDirection(attackDirection);
    }

    private Vector2 GetAttackDirection()
    {
        if (target == null || !target.HasTarget || target.TargetTransform == null)
            return currentAttackDirection.sqrMagnitude > 0.0001f ? currentAttackDirection : Vector2.right;

        Vector2 direction = target.TargetTransform.position - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
            return currentAttackDirection.sqrMagnitude > 0.0001f ? currentAttackDirection : Vector2.right;

        return direction.normalized;
    }

    #endregion

    #region Animation Event

    public void ApplyAttackDamage()
    {
        if (isAttackStopped)
            return;

        // 패턴 액션 공격은 액션이 지정한 시간에 직접 데미지를 넣는다.
        // 그래서 기존 공격 애니메이션 이벤트 데미지는 막아야 중복 데미지가 안 들어간다.
        if (isActionAttackPlaying)
            return;

        if (target == null)
            return;

        if (!target.HasTarget)
            return;

        if (!IsTargetAtAttackDistance())
            return;

        if (faceTargetBeforeDamage)
            FaceTarget();

        target.DamageTarget(damage);
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        isActionAttackPlaying = false;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    #endregion
}
