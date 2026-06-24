using System.Collections;
using UnityEngine;

public class ActorAttack : MonoBehaviour
{
    [Header("Attack Stat")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float attackDistanceTolerance = 0.15f;

    [Header("Components")]
    public ActorTarget target;
    public ActorVisual visual;
    public ActorMover mover;

    [Header("Debug")]
    [SerializeField] private bool isAttackStopped;
    [SerializeField] private bool isActionAttackPlaying;

    public bool IsAttacking { get; private set; }
    public bool IsAttackStopped => isAttackStopped;
    public bool IsActionAttackPlaying => isActionAttackPlaying;

    private float attackTimer = 0f;
    private Coroutine attackCoroutine;

    private const float FaceThreshold = 0.01f;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<ActorTarget>();

        if (visual == null)
            visual = GetComponent<ActorVisual>();

        if (mover == null)
            mover = GetComponent<ActorMover>();
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
            visual.ForceIdle(Vector2.zero, false, false);
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
        FaceTarget();
        if (visual != null)
        {
            visual.PlayAttack();
            yield return visual.WaitCurrentAnimationEnd();
            ApplyAttackDamage();
        }
        else
        {
            yield return null;
            ApplyAttackDamage();
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

        if (faceTargetBeforeStart)
            FaceTarget();

        if (visual != null)
            visual.PlayAttack();

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
                    if (faceTargetBeforeActionDamage)
                        FaceTarget();

                    ApplyActionDamage(
                        actionDamage,
                        checkRangeBeforeDamage,
                        checkRange,
                        checkTolerance
                    );

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
            {
                if (faceTargetBeforeActionDamage)
                    FaceTarget();

                ApplyActionDamage(
                    actionDamage,
                    checkRangeBeforeDamage,
                    checkRange,
                    checkTolerance
                );
            }
        }

        if (safeAfterDamageDelay > 0f)
            yield return new WaitForSeconds(safeAfterDamageDelay);

        IsAttacking = false;
        isActionAttackPlaying = false;
    }

    private void ApplyActionDamage(
        float actionDamage,
        bool checkRange,
        float checkRangeValue,
        float checkTolerance)
    {
        if (isAttackStopped)
            return;

        if (target == null || !target.HasTarget)
            return;

        if (checkRange && !IsTargetAtAttackDistance(checkRangeValue, checkTolerance))
            return;

        target.DamageTarget(Mathf.Max(0f, actionDamage));
    }

    #endregion

    #region Direction

    public void FaceTarget()
    {
        if (!CanFaceByAttack())
            return;

        if (target == null || !target.HasTarget)
            return;

        if (target.TargetTransform == null)
            return;

        Vector2 direction = target.TargetTransform.position - transform.position;

        if (Mathf.Abs(direction.x) <= FaceThreshold)
            return;

        Vector2 horizontalDirection = direction.x < 0f
            ? Vector2.left
            : Vector2.right;

        if (visual != null)
            visual.LookDirection(horizontalDirection);
    }

    private bool CanFaceByAttack()
    {
        if (isAttackStopped)
            return false;

        if (mover != null && mover.IsMovingOrTryingToMove)
            return false;

        return true;
    }

    #endregion

    #region Animation Event

    public void ApplyAttackDamage()
    {
        if (isAttackStopped)
            return;

        if (isActionAttackPlaying)
            return;

        if (target == null)
            return;

        if (!target.HasTarget)
            return;

        if (!IsTargetAtAttackDistance())
            return;

        //FaceTarget();

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