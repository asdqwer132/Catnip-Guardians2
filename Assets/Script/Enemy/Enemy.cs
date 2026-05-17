using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;

    [Header("HP")]
    public float hp = 10f;
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Animation")]
    public Animator animator;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Reward")]
    public int goldReward = 10;

    [Header("HP UI")]
    public Slider hpSlider;

    private Plant targetPlant;
    private Transform target;

    private float attackTimer = 0f;

    private bool isInitialized = false;
    private bool isDead = false;
    private bool isAttacking = false;

    private Coroutine attackCoroutine;
    private Coroutine dieCoroutine;
    private DamagePopupSpawner damagePopupSpawner;

    void Awake()
    {
        damagePopupSpawner = GetComponent<DamagePopupSpawner>();
    }
    public void Init(Plant plant)
    {
        targetPlant = plant;

        if (targetPlant != null)
            target = targetPlant.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        maxHp = hp;

        InitHPBar();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (isDead)
            return;

        if (target == null || targetPlant == null)
        {
            SetWalking(false);
            return;
        }

        LookAtTarget();

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            MoveToTarget();
        }
        else
        {
            StopMove();
            AttackTarget();
        }
    }

    void LookAtTarget()
    {
        if (target == null || spriteRenderer == null)
            return;

        // 스프라이트 기본 방향이 오른쪽일 때 기준
        spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    void MoveToTarget()
    {
        if (isAttacking)
            return;

        SetWalking(true);

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
    }

    void StopMove()
    {
        SetWalking(false);
    }

    void AttackTarget()
    {
        if (isAttacking)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        attackCoroutine = StartCoroutine(AttackRoutine());

        attackTimer = attackCooldown;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        SetWalking(false);

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Die");
            animator.SetTrigger("Attack");

            // Animator가 Attack 상태로 전환될 시간을 1프레임 줌
            yield return null;

            yield return WaitCurrentAnimationEnd();
        }
        else
        {
            yield return null;
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    // 이 함수는 공격 애니메이션의 Animation Event에서 호출
    public void ApplyAttackDamage()
    {
        if (isDead)
            return;

        if (targetPlant == null || target == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > attackRange)
            return;

        targetPlant.TakeDamage(damage);
    }

    public void TakeDamage(float dmg)
    {
        if (isDead)
            return;

        hp -= dmg;

        UpdateHPBar();
        if (damagePopupSpawner != null)
        {
            damagePopupSpawner.ShowDamage(dmg);
        }
        if (hp <= 0)
        {
            Die();
            return;
        }

        PlayHitAnimation();
    }

    void PlayHitAnimation()
    {
        if (isDead)
            return;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Hit");
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;

        SetWalking(false);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Die");
        }

        if (CurrencyManager.instance != null)
        {
            CurrencyManager.instance.AddCurrency(
                CurrencyType.Gold,
                goldReward
            );
        }

        dieCoroutine = StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        if (animator != null)
        {
            // Animator가 Die 상태로 전환될 시간을 1프레임 줌
            yield return null;

            yield return WaitCurrentAnimationEnd();
        }

        DestroySelf();
    }

    IEnumerator WaitCurrentAnimationEnd()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        float waitTime = stateInfo.length;

        yield return new WaitForSeconds(waitTime);
    }

    void SetWalking(bool value)
    {
        if (animator != null)
            animator.SetBool("IsWalking", value);
    }

    void InitHPBar()
    {
        if (hpSlider == null)
            return;

        hpSlider.maxValue = maxHp;
        hpSlider.value = hp;
    }

    void UpdateHPBar()
    {
        if (hpSlider == null)
            return;

        hpSlider.value = hp;
    }

    void DestroySelf()
    {
        if (EnemyManager.instance != null)
        {
            EnemyManager.instance.RemoveEnemy(gameObject);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}