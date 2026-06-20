using UnityEngine;

public class Enemy : HealthActor, IPoolable, IBuffTarget
{
    [Header("Data")]
    public EnemyStatData statData;

    [Header("Components")]
    public ActorTarget actorTarget;
    public ActorMover mover;
    public ActorAttack attack;
    public EnemyPatternRunner patternRunner;

    [Header("Buff")]
    public BuffManager buffManager;
    public float statRefreshInterval = 0.1f;

    private EnemyStat baseStat;
    [SerializeField] private EnemyStat currentStat;

    private Animator cachedAnimator;
    private float statRefreshTimer;
    private float previousAnimatorSpeed = 1f;

    private bool isInitialized = false;
    private bool isActionDisabled = false;

    public bool IsActionDisabled => isActionDisabled;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Enemy";
    public string BuffTargetDebugName => name;

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

        if (patternRunner == null)
            patternRunner = GetComponent<EnemyPatternRunner>();

        cachedAnimator = GetComponentInChildren<Animator>();
    }

    private void OnDestroy()
    {
        if (buffManager != null)
            buffManager.UnregisterBuffTarget(this);
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (IsDead)
            return;

        if (isActionDisabled)
        {
            StopMove();
            CancelAttack();
            return;
        }

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

        if (patternRunner != null && patternRunner.TickPattern())
            return;

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

    #region Action Control

    public void DisableAction()
    {
        if (isActionDisabled)
            return;

        isActionDisabled = true;

        LockMove();
        CancelAttack();
        PauseAnimation();
    }

    public void EnableAction()
    {
        if (!isActionDisabled)
            return;

        isActionDisabled = false;

        UnlockMove();
        ResumeAnimation();
    }

    private void PauseAnimation()
    {
        if (cachedAnimator == null)
            cachedAnimator = GetComponentInChildren<Animator>();

        if (cachedAnimator == null)
            return;

        previousAnimatorSpeed = cachedAnimator.speed;
        cachedAnimator.speed = 0f;
    }

    private void ResumeAnimation()
    {
        if (cachedAnimator == null)
            cachedAnimator = GetComponentInChildren<Animator>();

        if (cachedAnimator == null)
            return;

        if (previousAnimatorSpeed <= 0f)
            previousAnimatorSpeed = 1f;

        cachedAnimator.speed = previousAnimatorSpeed;
    }

    #endregion

    #region Pool

    public void OnSpawnedFromPool()
    {
        isInitialized = false;
        statRefreshTimer = 0f;
        isActionDisabled = false;
        previousAnimatorSpeed = 1f;

        UnlockMove();
        StopMove();

        if (patternRunner != null)
            patternRunner.ResetRunner();

        ResumeAnimation();
    }

    public void OnReturnedToPool()
    {
        isInitialized = false;
        isActionDisabled = false;

        UnlockMove();
        ResumeAnimation();
        StopMove();
        CancelAttack();

        if (patternRunner != null)
            patternRunner.ResetRunner();

        if (actorTarget != null)
            actorTarget.SetTarget(null);

        if (buffManager != null)
        {
            buffManager.ClearBuffsForTarget(this);
            buffManager.UnregisterBuffTarget(this);
        }

        if (EnemyManager.instance != null)
            EnemyManager.instance.RemoveEnemy(this);
    }

    #endregion

    #region Init

    public void Init(IDamageable target, BuffManager injectedBuffManager)
    {
        buffManager = injectedBuffManager;

        isActionDisabled = false;
        previousAnimatorSpeed = 1f;

        UnlockMove();
        ResumeAnimation();

        ApplyBaseStat();

        if (actorTarget != null)
            actorTarget.SetTarget(target);

        if (patternRunner != null)
            patternRunner.Init(this);

        if (buffManager != null)
            buffManager.RegisterBuffTarget(this);

        isInitialized = true;
    }

    private void ReturnSelfToPool()
    {
        if (EnemyManager.instance != null)
            EnemyManager.instance.RemoveEnemy(this);

        if (ObjectPoolManager.instance != null)
            ObjectPoolManager.instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    #endregion

    #region Move and Attack
    private void MoveToAttackDistance(Transform targetTransform)
    {
        if (mover == null || attack == null)
            return;

        mover.MoveToDistanceFromTarget(
            targetTransform,
            attack.attackRange,
            attack.attackDistanceTolerance
        );
    }

    private void MoveToTarget(Transform targetTransform)
    {
        if (mover == null)
            return;

        mover.MoveTo(targetTransform);
    }

    private void StopMove()
    {
        if (mover != null)
            mover.Stop();
    }

    private void LockMove()
    {
        if (mover != null)
            mover.LockMove();
    }

    private void UnlockMove()
    {
        if (mover != null)
            mover.UnlockMove();
    }

    private void CancelAttack()
    {
        if (attack != null && attack.IsAttacking)
            attack.CancelAttack();
    }

    public float GetAttackPower()
    {
        if (attack != null)
            return attack.damage;

        if (currentStat != null)
            return currentStat.damage;

        if (baseStat != null)
            return baseStat.damage;

        return 1f;
    }

    #endregion

    #region Stat

    private void ApplyBaseStat()
    {
        if (statData == null)
            return;

        baseStat = statData.CreateStat();
        currentStat = baseStat.Clone();

        InitHealth(baseStat.maxHp, true);
        ApplyRuntimeStat(currentStat);
    }

    private void RefreshBuffedStatByTimer()
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
            nextStat = buffManager.GetBuffedStatForTarget(baseStat, this);

        if (nextStat == null)
            nextStat = baseStat.Clone();

        currentStat = nextStat;
        ApplyRuntimeStat(currentStat);
    }

    private void ApplyRuntimeStat(EnemyStat stat)
    {
        if (stat == null)
            return;

        stat.Clamp();

        float speed = stat.speed;
        float damage = stat.damage;
        float attackRange = stat.attackRange;
        float attackCooldown = stat.attackCooldown;

        if (patternRunner != null)
        {
            speed = patternRunner.ModifyMoveSpeed(speed);
            damage = patternRunner.ModifyAttackDamage(damage);
            attackRange = patternRunner.ModifyAttackRange(attackRange);
            attackCooldown = patternRunner.ModifyAttackCooldown(attackCooldown);
        }

        if (mover != null)
            mover.SetSpeed(speed);

        if (attack != null)
        {
            attack.SetAttackStat(
                damage,
                attackRange,
                attackCooldown
            );
        }
    }

    #endregion

    #region Damage

    public override void TakeDamage(float damage)
    {
        if (patternRunner != null)
            damage = patternRunner.ModifyIncomingDamage(damage);

        if (patternRunner != null && patternRunner.TryHandleLethalDamage(damage))
            return;

        base.TakeDamage(damage);
    }

    public void ApplyDamageWithoutPattern(float damage)
    {
        base.TakeDamage(damage);
    }

    #endregion

    #region OnEvent

    protected override void OnDamaged(float damage)
    {
        if (IsDead)
            return;

        if (visual != null)
            visual.PlayHit();

        if (patternRunner != null)
            patternRunner.NotifyDamaged(damage);
    }

    protected override void OnDeathStarted()
    {
        LockMove();
        CancelAttack();

        if (patternRunner != null)
            patternRunner.StopPattern();

        isActionDisabled = false;
        ResumeAnimation();

        if (buffManager != null)
            buffManager.ClearBuffsForTarget(this);

        GiveReward();
    }

    protected override void OnDeathFinished()
    {
        ReturnSelfToPool();
    }

    #endregion

    private void GiveReward()
    {
        if (statData == null)
            return;

        if (CurrencyManager.instance != null)
            CurrencyManager.instance.AddCurrency(statData.reward);

        if (GrowManager.instance != null)
            GrowManager.instance.AddGrowth(statData.growEx);
    }
}