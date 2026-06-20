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

    public bool IsAttacking { get; private set; }
    public bool IsAttackStopped => isAttackStopped;

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
        if (target == null)
            return false;

        if (!target.HasTarget)
            return false;

        float distance = GetDistanceToTarget();
        return Mathf.Abs(distance - attackRange) <= attackDistanceTolerance;
    }

    #endregion

    #region Attack

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

        attackCoroutine = StartCoroutine(AttackRoutine());
        attackTimer = attackCooldown;
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;

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

    public void FaceTarget()
    {
        if (visual == null)
            return;

        Vector2 attackDirection = GetAttackDirection();

        if (attackDirection.sqrMagnitude <= 0.0001f)
            return;

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

    #region Event

    public void ApplyAttackDamage()
    {
        if (isAttackStopped)
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

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    #endregion
}
