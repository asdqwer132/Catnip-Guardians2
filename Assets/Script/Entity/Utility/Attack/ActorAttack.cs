using System.Collections;
using UnityEngine;

public class ActorAttack : MonoBehaviour
{
    [Header("Attack Stat")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float attackDistanceTolerance = 0.15f;

    [Header("Attack Preparation")]
    [Tooltip("멈춰서 타깃을 바라본 뒤 공격하기까지의 시간입니다. 0이면 즉시 공격합니다.")]
    [Min(0f)] public float attackPreparationTime = 0.1f;

    [Header("Components")]
    public ActorTarget target;
    public ActorVisual visual;
    public ActorMover mover;

    [Header("Debug")]
    [SerializeField] private bool isAttackStopped;
    [SerializeField] private bool isActionAttackPlaying;

    private enum DefaultAttackPhase { None, Preparing, Animating }
    private DefaultAttackPhase defaultPhase;
    private Enemy enemyOwner;
    private float preparationElapsed;
    private float animationElapsed;
    private float nextAttackTime;
    private bool defaultDamageApplied;
    private int attackVersion;

    public bool IsAttacking { get; private set; }
    public bool IsPreparing => defaultPhase == DefaultAttackPhase.Preparing;
    public bool IsAttackStopped => isAttackStopped;
    public bool IsActionAttackPlaying => isActionAttackPlaying;

    private void Awake()
    {
        if (target == null) target = GetComponent<ActorTarget>();
        if (visual == null) visual = GetComponent<ActorVisual>();
        if (mover == null) mover = GetComponent<ActorMover>();
        enemyOwner = GetComponent<Enemy>();
    }

    private void OnDisable() { ResetAttackState(); }

    // 기본 공격은 코루틴을 만들지 않고 작은 단계 상태로 진행한다.
    private void Update()
    {
        if (defaultPhase == DefaultAttackPhase.None) return;
        // interruptAttack=false로 남겨 둔 기본 공격은 강제 이동이 끝날 때까지 보존한다.
        bool pauseForMovementControl = mover != null && mover.IsMovementControlled && !isAttackStopped &&
            (enemyOwner == null || (!enemyOwner.IsFullyStopped && !enemyOwner.IsHitReacting && !enemyOwner.IsDead));
        if (visual != null) visual.SetDefaultAttackAnimationPaused(pauseForMovementControl);
        if (pauseForMovementControl) return;
        if (!CanUseDefaultAttack() || !IsTargetAtAttackDistance())
        {
            CancelAttack();
            return;
        }
        if (mover != null) mover.Stop();

        if (defaultPhase == DefaultAttackPhase.Preparing)
        {
            FaceTarget();
            preparationElapsed += Time.deltaTime;
            if (preparationElapsed < Mathf.Max(0f, attackPreparationTime)) return;
            defaultPhase = DefaultAttackPhase.Animating;
            animationElapsed = 0f;
            if (visual != null) visual.PlayAttack();
            return;
        }

        animationElapsed += Time.deltaTime;
        bool finished = visual != null ? !visual.IsAttackPlaying : animationElapsed > 0f;
        if (!finished) return;
        ApplyAttackDamage();
        CancelAttack();
    }

    public void SetAttackStat(float newDamage, float newRange, float newCooldown)
    {
        damage = newDamage;
        attackRange = Mathf.Max(0.01f, newRange);
        attackCooldown = Mathf.Max(0.01f, newCooldown);
    }

    public void SetAttackStopped(bool stopped)
    {
        if (isAttackStopped == stopped) return;
        isAttackStopped = stopped;
        if (stopped) ForceStop();
    }

    public void ForceStop() { CancelAttack(); }
    public void ResetAttackState()
    {
        CancelAttack();
        nextAttackTime = 0f;
    }

    #region Range
    public float GetDistanceToTarget()
    {
        return target != null ? target.GetDistanceFrom(transform) : float.MaxValue;
    }

    public bool IsTargetAtAttackDistance()
    {
        return IsTargetAtAttackDistance(attackRange, attackDistanceTolerance);
    }

    public bool IsTargetAtAttackDistance(float checkRange, float checkTolerance)
    {
        if (target == null || !target.HasTarget) return false;
        // 기존의 원형 띠 사거리 규칙을 유지하면서 제곱근 계산을 피한다.
        float range = Mathf.Max(0f, checkRange);
        float tolerance = Mathf.Max(0.0001f, checkTolerance);
        float minimum = Mathf.Max(0f, range - tolerance);
        float maximum = range + tolerance;
        float distanceSqr = target.GetSqrDistanceFrom(transform);
        return distanceSqr >= minimum * minimum && distanceSqr <= maximum * maximum;
    }
    #endregion

    #region Default Attack
    private bool CanUseDefaultAttack()
    {
        if (isAttackStopped || !isActiveAndEnabled) return false;
        if (enemyOwner != null && !enemyOwner.CanRunDefaultActions) return false;
        if (visual != null && (visual.IsHitPlaying || visual.IsDeathPlaying || visual.IsCustomAnimationLocked))
            return false;
        return mover == null || (!mover.IsMoveStopped && !mover.HasExternalMovement);
    }

    public void TickAttack()
    {
        if (Time.deltaTime <= 0f || IsAttacking || !CanUseDefaultAttack() || Time.time < nextAttackTime ||
            !IsTargetAtAttackDistance()) return;
        if (mover != null) mover.Stop();
        IsAttacking = true;
        isActionAttackPlaying = false;
        defaultPhase = DefaultAttackPhase.Preparing;
        preparationElapsed = 0f;
        defaultDamageApplied = false;
        FaceTarget();
    }
    #endregion

    #region Pattern Action Attack
    public IEnumerator PlayActionAttack(
        float actionDamage, float attackDelay, bool useCustomRange, float customRange,
        float customTolerance, bool requireRangeBeforeStart, bool checkRangeBeforeDamage,
        bool waitAnimationEnd, bool faceTargetBeforeStart, bool faceTargetBeforeActionDamage,
        float afterDamageDelay)
    {
        if (isAttackStopped || target == null || !target.HasTarget) yield break;
        float checkRange = useCustomRange ? customRange : attackRange;
        float tolerance = useCustomRange ? customTolerance : attackDistanceTolerance;
        if (requireRangeBeforeStart && !IsTargetAtAttackDistance(checkRange, tolerance)) yield break;

        CancelAttack();
        int version = attackVersion;
        IsAttacking = true;
        isActionAttackPlaying = true;
        if (mover != null) mover.Stop();

        // 직접 실행하거나 패턴 러너가 중단해도 이전 공격이 나중에 피해를 주지 않는다.
        try
        {
            float elapsed = 0f;
            while (elapsed < Mathf.Max(0f, attackPreparationTime))
            {
                if (!CanContinueActionAttack(version)) yield break;
                if (faceTargetBeforeStart) FaceTarget();
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (!CanContinueActionAttack(version)) yield break;
            if (faceTargetBeforeStart) FaceTarget();
            if (visual != null) visual.PlayAttack();

            elapsed = 0f;
            bool damaged = false;
            float delay = Mathf.Max(0f, attackDelay);
            do
            {
                if (!CanContinueActionAttack(version)) yield break;
                if (!damaged && elapsed >= delay)
                {
                    // 표시를 먼저 설정해서 피해 이벤트의 재진입에도 한 번만 적용한다.
                    damaged = true;
                    if (faceTargetBeforeActionDamage) FaceTarget();
                    ApplyActionDamage(actionDamage, checkRangeBeforeDamage, checkRange, tolerance);
                    if (!CanContinueActionAttack(version)) yield break;
                }
                bool animationPlaying = waitAnimationEnd && visual != null &&
                    (visual.IsAttackPlaying || visual.IsCustomAnimationLocked);
                if (damaged && !animationPlaying) break;
                yield return null;
                elapsed += Time.deltaTime;
            } while (true);

            elapsed = 0f;
            while (elapsed < Mathf.Max(0f, afterDamageDelay))
            {
                if (!CanContinueActionAttack(version)) yield break;
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            if (version == attackVersion) CancelAttack();
        }
    }

    private bool CanContinueActionAttack(int version)
    {
        return version == attackVersion && IsAttacking && !isAttackStopped &&
            isActiveAndEnabled && target != null && target.HasTarget &&
            (enemyOwner == null || (!enemyOwner.IsDead && (!enemyOwner.IsFullyStopped ||
                (enemyOwner.patternRunner != null && enemyOwner.patternRunner.IsHandlingLethalDamage)) && !enemyOwner.IsHitReacting));
    }

    private void ApplyActionDamage(float actionDamage, bool checkRange, float range, float tolerance)
    {
        if (isAttackStopped || target == null || !target.HasTarget) return;
        if (checkRange && !IsTargetAtAttackDistance(range, tolerance)) return;
        target.DamageTarget(Mathf.Max(0f, actionDamage));
    }
    #endregion

    #region Direction / Animation Event
    public void FaceTarget()
    {
        if (isAttackStopped || target == null || !target.HasTarget || target.TargetTransform == null) return;
        if (enemyOwner != null && (enemyOwner.IsHitReacting || enemyOwner.IsDead)) return;
        if (mover != null && (mover.IsMovingOrTryingToMove || mover.HasExternalMovement)) return;
        Vector2 direction = target.TargetTransform.position - transform.position;
        if (visual != null && direction.sqrMagnitude > 0.0001f) visual.LookDirection(direction.normalized);
    }

    public void ApplyAttackDamage()
    {
        // 애니메이션 이벤트와 종료 시점 대체 처리가 함께 있어도 한 공격당 한 번이다.
        if (defaultPhase != DefaultAttackPhase.Animating || isActionAttackPlaying ||
            defaultDamageApplied || !CanUseDefaultAttack() || !IsTargetAtAttackDistance()) return;
        defaultDamageApplied = true;
        target.DamageTarget(Mathf.Max(0f, damage));
    }

    public void CancelAttack()
    {
        bool wasDefaultAttack = defaultPhase != DefaultAttackPhase.None;
        unchecked { attackVersion++; }
        defaultPhase = DefaultAttackPhase.None;
        IsAttacking = false;
        isActionAttackPlaying = false;
        defaultDamageApplied = false;
        if (wasDefaultAttack) nextAttackTime = Time.time + Mathf.Max(0.01f, attackCooldown);
        if (visual != null)
        {
            visual.SetDefaultAttackAnimationPaused(false);
            visual.CancelAttackAnimation();
        }
    }
    #endregion
}
