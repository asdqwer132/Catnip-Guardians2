using UnityEngine;

public class Enemy : HealthActor
{
    [Header("Data")]
    public EnemyStatData statData;

    [Header("Components")]
    public ActorTarget actorTarget;
    public ActorMover mover;
    public ActorAttack attack;

    private bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake();

        if (actorTarget == null)
            actorTarget = GetComponent<ActorTarget>();

        if (mover == null)
            mover = GetComponent<ActorMover>();

        if (attack == null)
            attack = GetComponent<ActorAttack>();
    }

    public void Init(IDamageable target)
    {
        ApplyStat();

        if (actorTarget != null)
            actorTarget.SetTarget(target);

        isInitialized = true;
    }

    void ApplyStat()
    {
        if (statData == null)
        {
            Debug.LogWarning(name + " EnemyStatData가 없습니다.");
            return;
        }

        InitHealth(statData.maxHp, true);

        if (mover != null)
            mover.SetSpeed(statData.speed);

        if (attack != null)
        {
            attack.SetAttackStat(
                statData.damage,
                statData.attackRange,
                statData.attackCooldown
            );
        }
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (IsDead)
            return;

        if (actorTarget == null || !actorTarget.HasTarget)
        {
            if (mover != null)
                mover.Stop();

            return;
        }

        Transform targetTransform = actorTarget.TargetTransform;

        if (visual != null)
            visual.LookAt(targetTransform);

        if (attack != null && attack.IsTargetInRange())
        {
            if (mover != null)
                mover.Stop();

            attack.TickAttack();
        }
        else
        {
            if (attack == null || !attack.IsAttacking)
            {
                if (mover != null)
                    mover.MoveTo(targetTransform);
            }
        }
    }

    protected override void OnDamaged(float damage)
    {
        if (IsDead)
            return;

        if (visual != null)
            visual.PlayHit();
    }

    protected override void OnDeathStarted()
    {
        if (mover != null)
            mover.Stop();

        if (attack != null)
            attack.CancelAttack();

        GiveReward();
    }

    protected override void OnDeathFinished()
    {
        DestroySelf();
    }

    void GiveReward()
    {
        if (statData == null)
            return;

        if (CurrencyManager.instance != null)
        {
            CurrencyManager.instance.AddCurrency(statData.reward);
        }

        if (GrowManager.instance != null)
        {
            GrowManager.instance.AddGrowth(statData.growEx);
        }
    }

    void DestroySelf()
    {
        if (EnemyManager.instance != null)
            EnemyManager.instance.RemoveEnemy(gameObject);

        Destroy(gameObject);
    }
}