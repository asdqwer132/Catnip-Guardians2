using UnityEngine;

public class ActorMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;
    public float stopDistance = 0.03f;

    [Header("Acceleration")]
    public bool useAcceleration = true;
    public float acceleration = 25f;
    public float deceleration = 45f;

    [Header("External Move")]
    public float externalDamping = 10f;
    public float stopVelocityThreshold = 0.02f;
    [Tooltip("밀기/당기기 중에는 힘의 반대쪽을 바라보며 벗어나려는 모습을 보입니다.")]
    public bool faceAgainstMovementControl = true;

    [Header("Components")]
    public ActorVisual visual;
    public ActorAttack attack;

    [Header("Debug")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isMoveStopped;
    [SerializeField] private Vector2 currentMoveDirection;
    [SerializeField] private Vector2 lastMoveDirection = Vector2.right;
    [SerializeField] private Vector2 desiredBaseVelocity;
    [SerializeField] private Vector2 currentBaseVelocity;
    [SerializeField] private Vector2 externalVelocity;
    [SerializeField] private Vector2 finalVelocity;

    private Enemy enemyOwner;
    private ActorControlledMovement controlledMovement;
    private bool hasBaseMoveCommandThisFrame;
    private bool movedDirectlyThisFrame;
    private Vector2 intendedMoveDirection;
    private float remainingBaseMoveDistance = float.PositiveInfinity;
    private const float DirectionEpsilon = 0.0001f;
    private const float DeltaEpsilon = 0.0000001f;

    public bool IsMoving => isMoving || controlledMovement.IsActive;
    public bool IsMovementControlled => controlledMovement.IsActive;
    public bool IsBaseMovementBlocked => controlledMovement.SuppressBaseMovement;
    public bool IsMoveStopped => isMoveStopped;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public Vector2 CurrentBaseVelocity => currentBaseVelocity;
    public Vector2 ExternalVelocity => externalVelocity;
    public Vector2 FinalVelocity => finalVelocity;
    public bool HasExternalMovement => controlledMovement.IsActive ||
        externalVelocity.sqrMagnitude > StopVelocityThresholdSqr;
    public bool IsMovingOrTryingToMove => IsMoving ||
        desiredBaseVelocity.sqrMagnitude > StopVelocityThresholdSqr ||
        currentBaseVelocity.sqrMagnitude > StopVelocityThresholdSqr ||
        externalVelocity.sqrMagnitude > StopVelocityThresholdSqr;

    private float StopVelocityThresholdSqr => stopVelocityThreshold * stopVelocityThreshold;
    private bool IsBaseActionLocked => (enemyOwner != null && enemyOwner.IsHitReacting) ||
        (visual != null && (visual.IsHitPlaying || visual.IsDeathPlaying)) ||
        (attack != null && attack.IsAttacking);

    private void Awake()
    {
        if (visual == null) visual = GetComponent<ActorVisual>();
        if (attack == null) attack = GetComponent<ActorAttack>();
        enemyOwner = GetComponent<Enemy>();
    }

    private void LateUpdate() { TickMove(Time.deltaTime); }
    private void OnDisable() { ClearAllVelocity(); }

    private void TickMove(float deltaTime)
    {
        if (deltaTime <= 0f) { ResetFrameCommand(); return; }
        if (isMoveStopped && !controlledMovement.AllowWhileStopped)
        {
            ClearAllVelocity();
            SetIdleVisual();
            return;
        }

        // Tick이 마지막 프레임에 제어 상태를 해제해도 이번 변위는 강제 이동이다.
        bool wasControlled = controlledMovement.IsActive;
        bool wasForced = wasControlled || externalVelocity.sqrMagnitude > StopVelocityThresholdSqr;
        Vector2 resistanceDirection = controlledMovement.GetResistanceDirection(transform.position);
        bool suppressBase = isMoveStopped || controlledMovement.SuppressBaseMovement || IsBaseActionLocked;
        Vector2 controlledDelta = controlledMovement.Tick(transform.position, deltaTime);

        if (suppressBase) ClearBaseVelocity();
        else if (!hasBaseMoveCommandThisFrame) desiredBaseVelocity = Vector2.zero;

        if (useAcceleration)
        {
            float rate = desiredBaseVelocity.sqrMagnitude > DirectionEpsilon ? acceleration : deceleration;
            currentBaseVelocity = Vector2.MoveTowards(currentBaseVelocity, desiredBaseVelocity,
                Mathf.Max(0f, rate) * deltaTime);
        }
        else currentBaseVelocity = desiredBaseVelocity;

        Vector2 baseDelta = currentBaseVelocity * deltaTime;
        if (baseDelta.sqrMagnitude > remainingBaseMoveDistance * remainingBaseMoveDistance)
        {
            baseDelta = baseDelta.normalized * remainingBaseMoveDistance;
            currentBaseVelocity = Vector2.zero;
        }

        Vector2 externalDelta = externalVelocity.sqrMagnitude > StopVelocityThresholdSqr
            ? externalVelocity * deltaTime : Vector2.zero;
        Vector2 totalDelta = baseDelta + externalDelta + controlledDelta;
        finalVelocity = totalDelta / deltaTime;
        Vector2 visualDirection = wasForced
            ? GetForcedMoveFacing(resistanceDirection, externalVelocity) : totalDelta.normalized;

        if (totalDelta.sqrMagnitude > DeltaEpsilon)
        {
            transform.position += (Vector3)totalDelta;
            isMoving = true;
            currentMoveDirection = totalDelta.normalized;
            PlayMoveVisual(visualDirection);
        }
        else if (wasControlled)
        {
            // 중앙에 붙잡힌 동안에도 벗어나려는 이동 모습을 유지한다.
            isMoving = false;
            currentMoveDirection = Vector2.zero;
            PlayMoveVisual(visualDirection);
        }
        else if (!movedDirectlyThisFrame) SetIdleVisual();

        externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero,
            Mathf.Max(0f, externalDamping) * deltaTime);
        // 가속 중의 작은 속도를 지우면 낮은 가속도에서 영원히 가속하지 못한다.
        if (desiredBaseVelocity.sqrMagnitude <= DirectionEpsilon &&
            currentBaseVelocity.sqrMagnitude <= StopVelocityThresholdSqr) currentBaseVelocity = Vector2.zero;
        if (externalVelocity.sqrMagnitude <= StopVelocityThresholdSqr) externalVelocity = Vector2.zero;
        ResetFrameCommand();
    }

    private Vector2 GetForcedMoveFacing(Vector2 resistanceDirection, Vector2 forceVelocity)
    {
        if (faceAgainstMovementControl && resistanceDirection.sqrMagnitude > DirectionEpsilon)
            return resistanceDirection;
        if (intendedMoveDirection.sqrMagnitude > DirectionEpsilon) return intendedMoveDirection;
        if (forceVelocity.sqrMagnitude > DirectionEpsilon) return -forceVelocity.normalized;
        return lastMoveDirection;
    }

    private void ResetFrameCommand()
    {
        hasBaseMoveCommandThisFrame = false;
        movedDirectlyThisFrame = false;
        remainingBaseMoveDistance = float.PositiveInfinity;
    }

    public void SetSpeed(float newSpeed) { speed = Mathf.Max(0f, newSpeed); }

    #region Stop State
    public void SetMoveStopped(bool stopped)
    {
        if (isMoveStopped == stopped) return;
        isMoveStopped = stopped;
        if (stopped) ForceStop();
    }

    public void ForceStop()
    {
        ClearAllVelocity();
        if (visual != null) visual.ForceIdle(lastMoveDirection, true, false);
    }

    private bool CanMove()
    {
        return !isMoveStopped && !controlledMovement.SuppressBaseMovement && !IsBaseActionLocked;
    }
    #endregion

    #region Basic Move
    public void MoveTo(Transform target)
    {
        if (target == null) { Stop(); return; }
        MoveToPosition(target.position, stopDistance);
    }

    public void MoveToPosition(Vector3 targetPosition) { MoveToPosition(targetPosition, stopDistance); }
    public void MoveToPosition(Vector3 targetPosition, float customStopDistance)
    {
        MoveToPositionWithSpeed(targetPosition, speed, customStopDistance);
    }

    public void MoveToPositionWithSpeed(Vector3 targetPosition, float moveSpeed, float customStopDistance = 0.03f)
    {
        if (!CanMove()) return;
        Vector2 toTarget = targetPosition - transform.position;
        float remaining = toTarget.magnitude - Mathf.Max(0f, customStopDistance);
        if (remaining <= 0f) { Stop(); return; }
        QueueBaseVelocity(toTarget.normalized * Mathf.Max(0f, moveSpeed), toTarget.magnitude);
    }

    public void MoveToDistanceFromTarget(Transform target, float targetDistance, float tolerance)
    {
        if (!CanMove()) return;
        if (target == null) { Stop(); return; }
        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        float difference = distance - Mathf.Max(0f, targetDistance);
        if (Mathf.Abs(difference) <= Mathf.Max(0.0001f, tolerance)) { Stop(); return; }
        // 타깃과 정확히 겹쳤을 때도 물러나 공격 거리를 확보한다.
        Vector2 towardTarget = distance > 0.0001f ? toTarget / distance : -lastMoveDirection;
        Vector2 direction = difference > 0f ? towardTarget : -towardTarget;
        QueueBaseVelocity(direction * speed, Mathf.Abs(difference));
    }

    public void MoveDirection(Vector2 direction) { MoveDirection(direction, speed); }
    public void MoveDirection(Vector2 direction, float moveSpeed)
    {
        if (!CanMove()) return;
        if (direction.sqrMagnitude <= DirectionEpsilon || moveSpeed <= 0f) { Stop(); return; }
        QueueBaseVelocity(direction.normalized * moveSpeed, float.PositiveInfinity);
    }

    private void QueueBaseVelocity(Vector2 velocity, float maximumDistance)
    {
        hasBaseMoveCommandThisFrame = true;
        desiredBaseVelocity = velocity;
        remainingBaseMoveDistance = maximumDistance;
        if (velocity.sqrMagnitude > DirectionEpsilon) intendedMoveDirection = velocity.normalized;
        // 실제 이동이 적용되기 전에는 바라보는 방향을 바꾸지 않는다.
    }

    public void MoveBy(Vector2 delta)
    {
        if (!CanMove()) return;
        if (delta.sqrMagnitude <= DeltaEpsilon) { Stop(); return; }
        Stop();
        ApplyDirectPosition(transform.position + (Vector3)delta);
    }

    private void ApplyDirectPosition(Vector3 position)
    {
        Vector2 delta = position - transform.position;
        transform.position = position;
        if (delta.sqrMagnitude <= DeltaEpsilon) return;
        movedDirectlyThisFrame = true;
        isMoving = true;
        currentMoveDirection = delta.normalized;
        PlayMoveVisual(currentMoveDirection);
    }

    public void Stop()
    {
        hasBaseMoveCommandThisFrame = true;
        remainingBaseMoveDistance = 0f;
        ClearBaseVelocity();
        if (!HasExternalMovement) SetIdleVisual();
    }

    public void SmoothStop()
    {
        hasBaseMoveCommandThisFrame = true;
        desiredBaseVelocity = Vector2.zero;
        remainingBaseMoveDistance = float.PositiveInfinity;
    }
    #endregion

    #region External Move
    public bool TryStartMovementControl(ActorMovementControlRequest request)
    {
        bool stoppedByPattern = enemyOwner != null && !enemyOwner.IsFullyStopped &&
            enemyOwner.patternRunner != null && enemyOwner.patternRunner.IsExecuting;
        if (!isActiveAndEnabled || (isMoveStopped && !request.allowWhileStopped && !stoppedByPattern)) return false;
        if (stoppedByPattern) request.allowWhileStopped = true;
        if (!controlledMovement.TryStart(request, transform.position)) return false;
        if (request.suppressBaseMovement) ClearBaseVelocity();
        if (request.clearExternalVelocity) ClearExternalVelocity();
        return true;
    }

    public void CancelMovementControl() { controlledMovement.Clear(); }
    public bool IsMovementControlledBy(object source) { return controlledMovement.IsControlledBy(source); }
    public void CancelMovementControl(object source)
    {
        if (controlledMovement.IsControlledBy(source)) controlledMovement.Clear();
    }

    public void AddExternalVelocity(Vector2 velocity)
    {
        if (!isMoveStopped) externalVelocity += velocity;
    }
    public void AddExternalAcceleration(Vector2 accelerationValue)
    {
        if (!isMoveStopped) externalVelocity += accelerationValue * Time.deltaTime;
    }
    public void PullTo(Vector2 targetPosition, float pullAcceleration)
    {
        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        if (toTarget.sqrMagnitude > DirectionEpsilon)
            AddExternalAcceleration(toTarget.normalized * pullAcceleration);
    }
    public void KnockbackFrom(Vector2 sourcePosition, float knockbackPower)
    {
        Vector2 fromSource = (Vector2)transform.position - sourcePosition;
        if (fromSource.sqrMagnitude > DirectionEpsilon)
            AddExternalVelocity(fromSource.normalized * knockbackPower);
    }

    public void ClearExternalVelocity() { externalVelocity = Vector2.zero; }
    public void ClearBaseVelocity()
    {
        desiredBaseVelocity = Vector2.zero;
        currentBaseVelocity = Vector2.zero;
        finalVelocity = externalVelocity;
    }
    public void ClearAllVelocity()
    {
        CancelMovementControl();
        ClearBaseVelocity();
        externalVelocity = Vector2.zero;
        finalVelocity = Vector2.zero;
        isMoving = false;
        currentMoveDirection = Vector2.zero;
        intendedMoveDirection = Vector2.zero;
        ResetFrameCommand();
    }
    #endregion

    #region Position / Visual
    public void SetPosition(Vector3 position)
    {
        Stop();
        ApplyDirectPosition(position);
    }
    public void Teleport(Vector3 position, Vector2 lookDirection)
    {
        transform.position = position;
        ClearAllVelocity();
        FaceDirection(lookDirection);
        if (visual != null) visual.StopMove(lastMoveDirection);
    }
    public void FaceDirection(Vector2 direction)
    {
        if (IsBaseActionLocked || HasExternalMovement || direction.sqrMagnitude <= DirectionEpsilon) return;
        lastMoveDirection = direction.normalized;
        if (visual != null) visual.LookDirection(lastMoveDirection);
    }
    private void PlayMoveVisual(Vector2 direction)
    {
        if (IsBaseActionLocked || direction.sqrMagnitude <= DirectionEpsilon) return;
        lastMoveDirection = direction.normalized;
        if (visual != null) visual.PlayMove(lastMoveDirection);
    }
    private void SetIdleVisual()
    {
        isMoving = false;
        currentMoveDirection = Vector2.zero;
        if (visual != null) visual.StopMove();
    }
    #endregion
}
