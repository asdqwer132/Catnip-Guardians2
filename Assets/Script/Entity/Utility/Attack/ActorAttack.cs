using System.Collections;
using UnityEngine;

public class ActorAttack : MonoBehaviour
{
    [Header("Attack Stat")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

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
        attackRange = newRange;
        attackCooldown = newCooldown;
    }

    public bool IsTargetInRange()
    {
        if (target == null)
            return false;

        float distance = target.GetDistanceFrom(transform);
        return distance <= attackRange;
    }

    public void TickAttack()
    {
        if (IsAttacking)
            return;

        if (target == null || !target.HasTarget)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        if (!IsTargetInRange())
            return;

        attackCoroutine = StartCoroutine(AttackRoutine());
        attackTimer = attackCooldown;
    }

    IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        if (visual != null)
        {
            visual.SetWalking(false);
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

    // 애니메이션 이벤트에서 호출
    public void ApplyAttackDamage()
    {
        if (target == null)
            return;

        if (!target.HasTarget)
            return;

        if (!IsTargetInRange())
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
}