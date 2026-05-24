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

    public bool IsAttacking { get; private set; }

    private float attackTimer = 0f;
    private Coroutine attackCoroutine;

    void Awake()
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

    #region SetRange
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
        if (IsAttacking)
            return;

        if (target == null || !target.HasTarget)
            return;

        if (!IsTargetAtAttackDistance())
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        attackCoroutine = StartCoroutine(AttackRoutine());
        attackTimer = attackCooldown;
    }

    IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        if (visual != null)
        {
            visual.PlayAttack();
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

    #region Event
    // 애니메이션 이벤트에서 호출
    public void ApplyAttackDamage()
    {
        if (target == null)
            return;

        if (!target.HasTarget)
            return;

        if (!IsTargetAtAttackDistance())
            return;

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