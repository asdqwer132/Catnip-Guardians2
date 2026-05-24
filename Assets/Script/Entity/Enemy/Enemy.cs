using Unity.VisualScripting;
using UnityEngine;

public class Enemy : HealthActor, IPoolable
{
    [Header("Data")]
    public EnemyStatData statData;

    [Header("Components")]
    public ActorTarget actorTarget;
    public ActorMover mover;
    public ActorAttack attack;

    [Header("Buff")]
    public BuffManager buffManager;
    public float statRefreshInterval = 0.1f;

    private EnemyStat baseStat;
    private EnemyStat currentStat;
    private float statRefreshTimer;
    private bool isInitialized = false;

    #region Control

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

    private void OnDestroy()
    {
        if (buffManager != null)
            buffManager.UnregisterEnemy(this);
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (IsDead)
            return;

        RefreshBuffedStatByTimer();

        if (actorTarget == null || !actorTarget.HasTarget)
        {
            StopMove();
            CancelAttack();
            return;
        }

        Transform targetTransform = actorTarget.TargetTransform;

        if (targetTransform == null)
        {
            StopMove();
            CancelAttack();
            return;
        }

        if (attack == null)
        {
            MoveToTarget(targetTransform);
            return;
        }

        bool isAtAttackDistance = attack.IsTargetAtAttackDistance();

        if (!isAtAttackDistance)
        {
            CancelAttack();
            MoveToAttackDistance(targetTransform);
            return;
        }

        StopMove();

        attack.TickAttack();
    }

    #endregion

    #region Pool

    public void OnSpawnedFromPool()
    {
        isInitialized = false;
        statRefreshTimer = 0f;
    }

    public void OnReturnedToPool()
    {
        isInitialized = false;

        StopMove();
        CancelAttack();

        if (actorTarget != null)
            actorTarget.SetTarget(null);

        if (buffManager != null)
        {
            buffManager.ClearEnemyBuffs(this);
            buffManager.UnregisterEnemy(this);
        }

        if (EnemyManager.instance != null)
            EnemyManager.instance.RemoveEnemy(gameObject);
    }

    #endregion

    #region Init

    public void Init(IDamageable target, BuffManager injectedBuffManager)
    {
        buffManager = injectedBuffManager;

        ApplyBaseStat();

        if (actorTarget != null)
            actorTarget.SetTarget(target);

        if (buffManager != null)
            buffManager.RegisterEnemy(this);

        isInitialized = true;
    }

    void ReturnSelfToPool()
    {
        if (EnemyManager.instance != null)
            EnemyManager.instance.RemoveEnemy(gameObject);

        if (ObjectPoolManager.instance != null)
        {
            ObjectPoolManager.instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Move and Attack

    void MoveToAttackDistance(Transform targetTransform)
    {
        if (mover == null || attack == null)
            return;

        mover.MoveToDistanceFromTarget(
            targetTransform,
            attack.attackRange,
            attack.attackDistanceTolerance
        );
    }

    void MoveToTarget(Transform targetTransform)
    {
        if (mover == null)
            return;

        mover.MoveTo(targetTransform);
    }

    void StopMove()
    {
        if (mover != null)
            mover.Stop();
    }

    void CancelAttack()
    {
        if (attack != null && attack.IsAttacking)
            attack.CancelAttack();
    }

    #endregion

    #region Stat

    void ApplyBaseStat()
    {
        if (statData == null)
            return;

        baseStat = statData.CreateStat();
        currentStat = baseStat.Clone();

        InitHealth(baseStat.maxHp, true);

        ApplyRuntimeStat(currentStat);
    }

    void RefreshBuffedStatByTimer()
    {
        statRefreshTimer += Time.deltaTime;

        if (statRefreshTimer < statRefreshInterval)
            return;

        statRefreshTimer = 0f;

        RefreshBuffedStat();
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        EnemyStat nextStat = null;

        if (buffManager != null)
            nextStat = buffManager.GetBuffedEnemyStat(baseStat, this);

        if (nextStat == null)
            nextStat = baseStat.Clone();

        currentStat = nextStat;

        ApplyRuntimeStat(currentStat);
    }

    void ApplyRuntimeStat(EnemyStat stat)
    {
        if (stat == null)
            return;

        stat.Clamp();

        if (mover != null)
            mover.SetSpeed(stat.speed);

        if (attack != null)
        {
            attack.SetAttackStat(
                stat.damage,
                stat.attackRange,
                stat.attackCooldown
            );
        }
    }

    #endregion

    #region OnEvent

    protected override void OnDamaged(float damage)
    {
        if (IsDead)
            return;

        if (visual != null)
            visual.PlayHit();
    }

    protected override void OnDeathStarted()
    {
        StopMove();
        CancelAttack();

        if (buffManager != null)
            buffManager.ClearEnemyBuffs(this);

        GiveReward();
    }

    protected override void OnDeathFinished()
    {
        ReturnSelfToPool();
    }

    #endregion

    void GiveReward()
    {
        if (statData == null)
            return;

        if (CurrencyManager.instance != null)
            CurrencyManager.instance.AddCurrency(statData.reward);

        if (GrowManager.instance != null)
            GrowManager.instance.AddGrowth(statData.growEx);
    }
}