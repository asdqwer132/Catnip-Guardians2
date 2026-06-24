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

    [SerializeField] private bool isFullyStopped = false;

    public bool IsActionDisabled => isActionDisabled;
    public bool IsFullyStopped => isFullyStopped;

    public UnityEngine.Object BuffTargetObject => this;
    private string buffTargetGroup = "Enemy";
    public string BuffTargetGroup => buffTargetGroup;
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

        if (visual == null)
            visual = GetComponent<ActorVisual>();

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

        if (isFullyStopped)
            return;

        if (isActionDisabled)
        {
            FullStop();
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

        TickDefaultAI(targetTransform);
    }

    private void TickDefaultAI(Transform targetTransform)
    {
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

    public void FullStop()
    {
        if (isFullyStopped)
            return;

        isFullyStopped = true;
        ApplyFullStopState();
    }

    public void ReleaseFullStop()
    {
        if (!isFullyStopped)
            return;

        isFullyStopped = false;

        if (mover != null)
            mover.SetMoveStopped(false);

        if (attack != null)
            attack.SetAttackStopped(false);

        ResumeAnimation();
    }

    private void ApplyFullStopState()
    {
        if (patternRunner != null)
            patternRunner.ForceStopPattern();

        if (mover != null)
            mover.SetMoveStopped(true);

        if (attack != null)
            attack.SetAttackStopped(true);

        if (visual != null)
        {
            Vector2 lookDirection = mover != null ? mover.LastMoveDirection : Vector2.zero;
            visual.ForceIdle(lookDirection, true, false);
        }

        ResumeAnimation();
    }

    public void DisableAction()
    {
        if (isActionDisabled)
            return;

        isActionDisabled = true;
        FullStop();
    }

    public void EnableAction()
    {
        if (!isActionDisabled)
            return;

        isActionDisabled = false;
        ReleaseFullStop();
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
        isFullyStopped = false;
        previousAnimatorSpeed = 1f;

        if (mover != null)
        {
            mover.SetMoveStopped(false);
            mover.ClearAllVelocity();
        }

        if (attack != null)
            attack.SetAttackStopped(false);

        if (patternRunner != null)
            patternRunner.ResetRunner();

        ResumeAnimation();
    }

    public void OnReturnedToPool()
    {
        isInitialized = false;
        isActionDisabled = false;
        isFullyStopped = false;

        ResumeAnimation();

        if (mover != null)
        {
            mover.SetMoveStopped(false);
            mover.ClearAllVelocity();
            mover.Stop();
        }

        if (attack != null)
        {
            attack.SetAttackStopped(false);
            attack.CancelAttack();
        }

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
        isFullyStopped = false;
        previousAnimatorSpeed = 1f;

        if (mover != null)
        {
            mover.SetMoveStopped(false);
            mover.ClearAllVelocity();
        }

        if (attack != null)
            attack.SetAttackStopped(false);

        ResumeAnimation();

        ApplyBaseStat();

        if (statData != null)
            buffTargetGroup = "Enemy/" + statData.enemyClass;
        else
            buffTargetGroup = "Enemy";

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
            attack.SetAttackStat(damage, attackRange, attackCooldown);
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
        StopMove();
        CancelAttack();

        if (mover != null)
            mover.ClearAllVelocity();

        if (patternRunner != null)
            patternRunner.StopPattern();

        isActionDisabled = false;
        isFullyStopped = false;

        if (mover != null)
            mover.SetMoveStopped(false);

        if (attack != null)
            attack.SetAttackStopped(false);

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

        if (GameStatisticsManager.Instance != null)
        {
            foreach(var reward in statData.reward)
            {
                GameStatisticsManager.Instance.AddCurrency(reward.currencyType, reward.amount);
            }
        }

        if (GrowManager.instance != null && baseStat != null)
            GrowManager.instance.AddGrowth(baseStat.growEx);
    }
}