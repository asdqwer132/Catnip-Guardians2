using UnityEngine;

public enum EnemyBehaviorState
{
    Idle, Moving, PreparingAttack, Attacking, Pattern, MovementControlled, Hit, Stopped, DeathPattern, Dead
}

[BuffTargetGroups("Enemy")]
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

    [Header("Status")]
    public EnemyStatusController statusController;

    [Header("Hit Reaction")]
    [Tooltip("피격 시 기본행동을 차단하는 최소 시간입니다. 피격 애니메이션이 더 길면 끝까지 기다립니다.")]
    [Min(0.01f)] public float minimumHitReactionTime = 0.15f;
    [SerializeField] private EnemyBehaviorState behaviorState;
    private float hitReactionRemainingTime;

    [Header("Movement Control")]
    [Tooltip("이 적은 아이템의 밀어내기/끌어당기기에 면역입니다.")]
    public bool movementControlImmune;
    [Tooltip("0: 전부 적용, 1: 완전 저항. 강도만 감소시킵니다.")]
    [Range(0f, 1f)] public float movementControlResistance;

    [Header("Runtime Stat")]
    [SerializeField] private EnemyStat currentStat = new EnemyStat();

    private EnemyStat baseStat = new EnemyStat();

    private Animator cachedAnimator;
    private float previousAnimatorSpeed = 1f;
    private bool isInitialized = false;
    private bool isActionDisabled = false;

    [SerializeField] private bool isFullyStopped = false;
    [SerializeField] private bool isStunned = false;
    private int hitEffectLifeId;
    private bool isRooted;

    public bool IsRooted => isRooted;
    public bool IsTimeStopped => TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions);
    public void SetRooted(bool value)
    {
        isRooted = value;
        if (value && mover != null) mover.ClearAllVelocity();
    }

    public bool IsActionDisabled => isActionDisabled || isStunned;
    public bool IsFullyStopped => isFullyStopped || isActionDisabled || isStunned;
    public bool IsStunned => isStunned;
    public bool IsHitReacting => !IsDead && (hitReactionRemainingTime > 0f ||
        (visual != null && visual.IsHitPlaying));
    public EnemyBehaviorState BehaviorState => behaviorState;
    public bool CanRunDefaultActions => isInitialized && isActiveAndEnabled && !IsDead &&
        !IsFullyStopped && !IsTimeStopped && !IsHitReacting &&
        (patternRunner == null || (!patternRunner.IsExecuting && !patternRunner.IsHandlingLethalDamage)) &&
        (mover == null || !mover.IsBaseMovementBlocked);
    public int HitEffectLifeId => hitEffectLifeId;
    public bool CanReceiveHitEffects => isInitialized && !IsDead && isActiveAndEnabled;

    public UnityEngine.Object BuffTargetObject => this;

    private string buffTargetGroup = "Enemy";
    public string BuffTargetGroup => buffTargetGroup;
    public string BuffTargetDebugName => name;

    #region Unity

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

        EnsureRuntimeStatInstances();
        GetOrCreateStatusController();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!isInitialized || IsDead)
            return;

        if (IsFullyStopped)
            ApplyFullStopState();
        else
            TryReleaseControlStop();
    }

    protected override void OnDisable()
    {
        ClearHitReaction();
        if (patternRunner != null) patternRunner.StopPattern();
        if (attack != null) attack.CancelAttack();
        if (mover != null)
            mover.ClearAllVelocity();
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (buffManager != null)
            buffManager.UnregisterBuffTarget(this);
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (IsDead) { behaviorState = EnemyBehaviorState.Dead; return; }
        // 시간 정지는 수동 행동 정지/기절 상태를 변경하지 않고 실행 시계만 멈춘다.
        if (IsTimeStopped) { behaviorState = EnemyBehaviorState.Stopped; return; }
        hitReactionRemainingTime = Mathf.Max(0f, hitReactionRemainingTime - Time.deltaTime);

        // 사망 예약은 일반 정지·피격·이동제어보다 우선한다.
        if (patternRunner != null && patternRunner.IsHandlingLethalDamage)
        {
            behaviorState = EnemyBehaviorState.DeathPattern;
            ClearHitReaction();
            if (mover != null) { mover.CancelMovementControl(); mover.SetMoveStopped(false); }
            if (attack != null) attack.SetAttackStopped(false);
            patternRunner.TickPattern();
            return;
        }

        if (IsFullyStopped)
        {
            behaviorState = EnemyBehaviorState.Stopped;
            if (patternRunner != null) patternRunner.TickPattern(false);
            return;
        }

        if (IsHitReacting)
        {
            behaviorState = EnemyBehaviorState.Hit;
            StopMove();
            if (patternRunner != null) patternRunner.TickPattern(false);
            return;
        }

        bool blockedByMovementControl = mover != null && mover.IsBaseMovementBlocked;
        if (visual != null)
            visual.SetPatternAnimationPaused(blockedByMovementControl && patternRunner != null && patternRunner.IsExecuting);
        if (blockedByMovementControl)
        {
            behaviorState = EnemyBehaviorState.MovementControlled;
            StopMove();
            if (patternRunner != null) patternRunner.TickPattern(false);
            return;
        }

        if (patternRunner != null && patternRunner.TickPattern())
        {
            behaviorState = EnemyBehaviorState.Pattern;
            return;
        }

        // 타깃이 사라져도 자기 위치에서 실행할 패턴은 먼저 처리한다.
        Transform targetTransform = actorTarget != null && actorTarget.HasTarget ? actorTarget.TargetTransform : null;
        if (targetTransform == null)
        {
            behaviorState = EnemyBehaviorState.Idle;
            StopMove();
            CancelAttack();
            return;
        }
        TickDefaultAI(targetTransform);
    }

    #endregion

    #region AI

    private void TickDefaultAI(Transform targetTransform)
    {
        if (attack == null)
        {
            behaviorState = EnemyBehaviorState.Moving;
            MoveToTarget(targetTransform);
            return;
        }

        bool isAtAttackDistance = attack.IsTargetAtAttackDistance();

        if (!isAtAttackDistance)
        {
            behaviorState = EnemyBehaviorState.Moving;
            CancelAttack();
            MoveToAttackDistance(targetTransform);
            return;
        }

        // 사거리 안에서는 잔여 기본 속도까지 즉시 제거하고 공격 준비에 들어간다.
        StopMove();
        attack.TickAttack();
        behaviorState = attack.IsPreparing ? EnemyBehaviorState.PreparingAttack :
            attack.IsAttacking ? EnemyBehaviorState.Attacking : EnemyBehaviorState.Idle;
    }

    #endregion

    #region Control

    public bool TryApplyMovementControl(
        ActorMovementControlRequest request,
        bool interruptPattern = true,
        bool interruptAttack = true
    )
    {
        if (!CanReceiveHitEffects || IsRooted || IsTimeStopped || movementControlImmune || mover == null ||
            (patternRunner != null && patternRunner.IsHandlingLethalDamage))
            return false;

        if (float.IsNaN(movementControlResistance) || float.IsInfinity(movementControlResistance))
            return false;
        request.strength *= 1f - Mathf.Clamp01(movementControlResistance);
        if (!mover.TryStartMovementControl(request))
            return false;

        if (interruptPattern && patternRunner != null)
            patternRunner.ForceStopPattern();
        if (interruptAttack && attack != null)
            attack.CancelAttack();
        return true;
    }

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
        TryReleaseControlStop();
    }

    private void ApplyFullStopState()
    {
        if (patternRunner != null && patternRunner.IsHandlingLethalDamage) return;
        ClearHitReaction();
        if (patternRunner != null)
            patternRunner.ForceStopPattern();

        if (mover != null)
        {
            mover.SetMoveStopped(true);
            mover.ClearAllVelocity();
            mover.Stop();
        }

        if (attack != null)
        {
            attack.SetAttackStopped(true);
            attack.CancelAttack();
        }

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
        ApplyFullStopState();
    }

    public void EnableAction()
    {
        if (!isActionDisabled)
            return;

        isActionDisabled = false;
        TryReleaseControlStop();
    }

    public EnemyStatusController GetOrCreateStatusController()
    {
        if (statusController == null)
            statusController = GetComponent<EnemyStatusController>();

        if (statusController == null)
            statusController = gameObject.AddComponent<EnemyStatusController>();

        statusController.Bind(this);
        return statusController;
    }

    // 수동 전체 정지, 행동 비활성화, 기절을 독립적으로 유지한다.
    public bool SetStunned(bool value)
    {
        if (value && (!CanReceiveHitEffects ||
            (patternRunner != null && patternRunner.IsHandlingLethalDamage)))
            return false;

        if (isStunned == value)
            return true;

        isStunned = value;
        if (value)
            ApplyFullStopState();
        else
            TryReleaseControlStop();

        return true;
    }

    private void TryReleaseControlStop()
    {
        if (IsFullyStopped || IsDead || !isInitialized || !isActiveAndEnabled)
            return;

        if (mover != null)
            mover.SetMoveStopped(false);

        if (attack != null)
            attack.SetAttackStopped(false);

        ResumeAnimation();
    }

    private void ClearHitEffectStatuses()
    {
        SetRooted(false);
        MarkStatusController marks = GetComponent<MarkStatusController>();
        if (marks != null) marks.ClearMarks();
        if (actorTarget != null) actorTarget.ClearTargetOverrides();
        if (statusController != null)
            statusController.ClearAllStatuses();
        else
            SetStunned(false);
    }

    private void ClearHitReaction()
    {
        hitReactionRemainingTime = 0f;
        if (visual != null)
        {
            visual.SetPatternAnimationPaused(false);
            visual.CancelHitReaction();
        }
    }

    private void InvalidateHitEffectLife()
    {
        unchecked { hitEffectLifeId++; }
    }

    protected override void ResetActorStateForReuse()
    {
        ClearHitReaction();
        behaviorState = EnemyBehaviorState.Idle;
        if (attack != null) attack.ResetAttackState();
        if (mover != null) mover.ClearAllVelocity();
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
        if (buffManager != null)
            buffManager.ClearBuffsForTarget(this);
        base.ResetActorStateForReuse();
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

        if (IsTimeStopped && visual != null)
        {
            visual.SetTimeStopAnimationPaused(true);
            return;
        }
        if (visual != null && visual.IsAnimationPlaybackPaused) return;
        cachedAnimator.speed = previousAnimatorSpeed;
    }

    #endregion

    #region Pool

    public void OnSpawnedFromPool()
    {
        isInitialized = false;
        ClearHitReaction();
        behaviorState = EnemyBehaviorState.Idle;
        if (attack != null) attack.ResetAttackState();
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
        isActionDisabled = false;
        isFullyStopped = false;
        previousAnimatorSpeed = 1f;

        EnsureRuntimeStatInstances();

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
        ClearHitReaction();
        behaviorState = EnemyBehaviorState.Idle;
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
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
            attack.ResetAttackState();
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

    public void Init(IDamageable target, BuffManager injectedBuffManager, EnemyDataSet dataSet)
    {
        if (dataSet == null)
        {
            Debug.LogError($"[{name}] EnemyDataSet이 없습니다.", this);
            return;
        }
        isInitialized = false;
        ClearHitReaction();
        behaviorState = EnemyBehaviorState.Idle;
        if (attack != null) attack.ResetAttackState();
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
        buffManager = injectedBuffManager;

        isActionDisabled = false;
        isFullyStopped = false;
        previousAnimatorSpeed = 1f;

        EnsureRuntimeStatInstances();

        if (mover != null)
        {
            mover.SetMoveStopped(false);
            mover.ClearAllVelocity();
        }

        if (attack != null)
            attack.SetAttackStopped(false);

        statData = dataSet.statData;
        if (patternRunner != null)
        {
            patternRunner.patternData = dataSet.patternData;
            patternRunner.Init(this);
        }
        if (visual != null && visual.animator != null)
            visual.animator.runtimeAnimatorController = dataSet.animatorController;


        ResumeAnimation();

        ApplyBaseStat();

        if (statData != null && !string.IsNullOrEmpty(statData.enemyClass))
            buffTargetGroup = "Enemy/" + statData.enemyClass;
        else
            buffTargetGroup = "Enemy";

        if (actorTarget != null)
            actorTarget.SetTarget(target);

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

    private void EnsureRuntimeStatInstances()
    {
        if (baseStat == null)
            baseStat = new EnemyStat();

        if (currentStat == null)
            currentStat = new EnemyStat();
    }

    private void ApplyBaseStat()
    {
        if (statData == null)
            return;

        EnsureRuntimeStatInstances();

        statData.CreateStatTo(baseStat);
        currentStat.CopyFrom(baseStat);

        InitHealth(baseStat.maxHp, true);
        ApplyRuntimeStat(currentStat);
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        EnsureRuntimeStatInstances();

        currentStat.CopyFrom(baseStat);

        if (buffManager != null)
            buffManager.ApplyBuffsToStatForTarget(currentStat, this);

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

    public void TakeDamage(float damage, ItemEffectContext source)
    {
        if (health == null) return;
        using (health.UseDamageSource(source)) TakeDamage(damage);
    }

    public override void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (patternRunner != null)
            damage = patternRunner.ModifyIncomingDamage(damage);

        bool wasHandlingLethalDamage = patternRunner != null && patternRunner.IsHandlingLethalDamage;
        bool reachesZeroHp = health != null && health.PreviewDamageToHp(damage) >= health.Hp;
        if (patternRunner != null && (wasHandlingLethalDamage || reachesZeroHp) && patternRunner.TryHandleLethalDamage(damage))
        {
            if (!wasHandlingLethalDamage)
            {
                ClearHitReaction();
                CancelAttack();
                if (mover != null) mover.ClearAllVelocity();
            }
            return;
        }

        base.TakeDamage(damage);
    }

    public void ApplyDamageWithoutPattern(float damage, ItemEffectContext source = null)
    {
        if (health == null) return;
        using (health.UseDamageSource(source)) base.TakeDamage(damage);
    }

    #endregion

    #region Event

    protected override void OnDamaged(float damage)
    {
        if (IsDead || (health != null && health.Hp <= 0f) ||
            (patternRunner != null && patternRunner.IsHandlingLethalDamage))
            return;

        CancelAttack();
        StopMove();
        // 일반 패턴의 커스텀 연출보다 피격에 집중한다. 사망 패턴은 위에서 제외했다.
        if (patternRunner != null) patternRunner.StopPattern();
        hitReactionRemainingTime = Mathf.Max(0.01f, minimumHitReactionTime);
        behaviorState = EnemyBehaviorState.Hit;
        if (visual != null)
            visual.PlayHit();

        if (patternRunner != null)
            patternRunner.NotifyDamaged(damage);
    }

    protected override void OnDeathStarted()
    {
        ClearHitReaction();
        behaviorState = EnemyBehaviorState.Dead;
        InvalidateHitEffectLife();
        ClearHitEffectStatuses();
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

    #region Reward

    private void GiveReward()
    {
        if (statData == null)
            return;

        if (GameStatisticsManager.Instance != null && statData.reward != null)
        {
            for (int i = 0; i < statData.reward.Length; i++)
            {
                Cost reward = statData.reward[i];

                if (reward == null)
                    continue;

                GameStatisticsManager.Instance.AddCurrency(reward.currencyType, reward.amount);
            }
        }

        if (GrowManager.instance != null && baseStat != null)
            GrowManager.instance.AddGrowth(baseStat.growEx);
    }

    #endregion
}
