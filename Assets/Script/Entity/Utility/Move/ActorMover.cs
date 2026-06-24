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

    private bool hasBaseMoveCommandThisFrame;

    private const float FaceThreshold = 0.01f;

    public bool IsMoving => isMoving;
    public bool IsMoveStopped => isMoveStopped;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public Vector2 CurrentBaseVelocity => currentBaseVelocity;
    public Vector2 ExternalVelocity => externalVelocity;
    public Vector2 FinalVelocity => finalVelocity;

    public bool IsMovingOrTryingToMove
    {
        get
        {
            if (isMoving)
                return true;

            if (hasBaseMoveCommandThisFrame && desiredBaseVelocity.sqrMagnitude > StopVelocityThresholdSqr)
                return true;

            if (desiredBaseVelocity.sqrMagnitude > StopVelocityThresholdSqr)
                return true;

            if (currentBaseVelocity.sqrMagnitude > StopVelocityThresholdSqr)
                return true;

            if (externalVelocity.sqrMagnitude > StopVelocityThresholdSqr)
                return true;

            if (finalVelocity.sqrMagnitude > StopVelocityThresholdSqr)
                return true;

            return false;
        }
    }

    private float StopVelocityThresholdSqr => stopVelocityThreshold * stopVelocityThreshold;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponent<ActorVisual>();

        if (attack == null)
            attack = GetComponent<ActorAttack>();
    }

    private void LateUpdate()
    {
        TickMove(Time.deltaTime);
    }

    private void TickMove(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            ResetFrameCommand();
            return;
        }

        if (isMoveStopped)
        {
            ClearAllVelocity();
            SetIdleVisual();
            ResetFrameCommand();
            return;
        }

        if (!hasBaseMoveCommandThisFrame)
            desiredBaseVelocity = Vector2.zero;

        if (useAcceleration)
        {
            float rate = desiredBaseVelocity.sqrMagnitude > 0.0001f
                ? acceleration
                : deceleration;

            currentBaseVelocity = Vector2.MoveTowards(
                currentBaseVelocity,
                desiredBaseVelocity,
                rate * deltaTime
            );
        }
        else
        {
            currentBaseVelocity = desiredBaseVelocity;
        }

        finalVelocity = currentBaseVelocity + externalVelocity;

        if (finalVelocity.sqrMagnitude > StopVelocityThresholdSqr)
        {
            Vector2 delta = finalVelocity * deltaTime;
            ApplyPositionDelta(delta);
        }
        else
        {
            finalVelocity = Vector2.zero;
            SetIdleVisual();
        }

        externalVelocity = Vector2.MoveTowards(
            externalVelocity,
            Vector2.zero,
            externalDamping * deltaTime
        );

        if (currentBaseVelocity.sqrMagnitude <= StopVelocityThresholdSqr)
            currentBaseVelocity = Vector2.zero;

        if (externalVelocity.sqrMagnitude <= StopVelocityThresholdSqr)
            externalVelocity = Vector2.zero;

        ResetFrameCommand();
    }

    private void ResetFrameCommand()
    {
        hasBaseMoveCommandThisFrame = false;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    #region Stop State

    public void SetMoveStopped(bool stopped)
    {
        if (isMoveStopped == stopped)
            return;

        isMoveStopped = stopped;

        if (isMoveStopped)
            ForceStop();
    }

    public void ForceStop()
    {
        ClearAllVelocity();

        if (visual != null)
            visual.ForceIdle(lastMoveDirection, true, false);
    }

    private bool CanMove()
    {
        return !isMoveStopped;
    }

    private bool CanFaceByMovement()
    {
        if (attack != null && attack.IsAttacking)
            return false;

        return true;
    }

    #endregion

    #region Basic Move

    public void MoveTo(Transform target)
    {
        if (!CanMove())
            return;

        if (target == null)
        {
            Stop();
            return;
        }

        MoveToPosition(target.position, stopDistance);
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        MoveToPosition(targetPosition, stopDistance);
    }

    public void MoveToPosition(Vector3 targetPosition, float customStopDistance)
    {
        if (!CanMove())
            return;

        Vector2 toTarget = targetPosition - transform.position;

        if (toTarget.magnitude <= customStopDistance)
        {
            Stop();
            return;
        }

        MoveDirection(toTarget, speed);
    }

    public void MoveToPositionWithSpeed(Vector3 targetPosition, float moveSpeed, float customStopDistance = 0.03f)
    {
        if (!CanMove())
            return;

        Vector2 toTarget = targetPosition - transform.position;

        if (toTarget.magnitude <= customStopDistance)
        {
            Stop();
            return;
        }

        MoveDirection(toTarget, moveSpeed);
    }

    public void MoveToDistanceFromTarget(Transform target, float targetDistance, float tolerance)
    {
        if (!CanMove())
            return;

        if (target == null)
        {
            Stop();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float currentDistance = toTarget.magnitude;

        if (currentDistance <= 0.0001f)
        {
            Stop();
            return;
        }

        float distanceDifference = currentDistance - targetDistance;

        if (Mathf.Abs(distanceDifference) <= tolerance)
        {
            Stop();
            return;
        }

        Vector2 directionToTarget = toTarget.normalized;

        if (distanceDifference > 0f)
            MoveDirection(directionToTarget, speed);
        else
            MoveDirection(-directionToTarget, speed);
    }

    public void MoveDirection(Vector2 direction)
    {
        MoveDirection(direction, speed);
    }

    public void MoveDirection(Vector2 direction, float moveSpeed)
    {
        if (!CanMove())
            return;

        if (direction.sqrMagnitude <= 0.0001f || moveSpeed <= 0f)
        {
            Stop();
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        QueueBaseVelocity(normalizedDirection * moveSpeed);
    }

    private void QueueBaseVelocity(Vector2 velocity)
    {
        hasBaseMoveCommandThisFrame = true;
        desiredBaseVelocity = velocity;

        if (velocity.sqrMagnitude <= 0.0001f)
            return;

        Vector2 direction = velocity.normalized;
        currentMoveDirection = direction;

        if (!CanFaceByMovement())
            return;

        if (Mathf.Abs(direction.x) > FaceThreshold)
            lastMoveDirection = direction.x < 0f ? Vector2.left : Vector2.right;
    }

    public void MoveBy(Vector2 delta)
    {
        if (!CanMove())
            return;

        if (delta.sqrMagnitude <= 0.0000001f)
        {
            Stop();
            return;
        }

        ApplyPositionDelta(delta);
    }

    private void ApplyPositionDelta(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0.0000001f)
            return;

        Vector2 direction = delta.normalized;

        transform.position += (Vector3)delta;

        isMoving = true;

        currentMoveDirection = direction;

        if (CanFaceByMovement() && Mathf.Abs(direction.x) > FaceThreshold)
            lastMoveDirection = direction.x < 0f ? Vector2.left : Vector2.right;

        PlayMoveVisual(direction);
    }

    public void Stop()
    {
        hasBaseMoveCommandThisFrame = true;
        desiredBaseVelocity = Vector2.zero;
        currentBaseVelocity = Vector2.zero;

        if (externalVelocity.sqrMagnitude <= StopVelocityThresholdSqr)
            SetIdleVisual();
    }

    public void SmoothStop()
    {
        hasBaseMoveCommandThisFrame = true;
        desiredBaseVelocity = Vector2.zero;
    }

    #endregion

    #region External Move

    public void AddExternalVelocity(Vector2 velocity)
    {
        if (!CanMove())
            return;

        externalVelocity += velocity;
    }

    public void AddExternalAcceleration(Vector2 accelerationValue)
    {
        if (!CanMove())
            return;

        externalVelocity += accelerationValue * Time.deltaTime;
    }

    public void PullTo(Vector2 targetPosition, float pullAcceleration)
    {
        if (!CanMove())
            return;

        Vector2 toTarget = targetPosition - (Vector2)transform.position;

        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        AddExternalAcceleration(toTarget.normalized * pullAcceleration);
    }

    public void KnockbackFrom(Vector2 sourcePosition, float knockbackPower)
    {
        if (!CanMove())
            return;

        Vector2 fromSource = (Vector2)transform.position - sourcePosition;

        if (fromSource.sqrMagnitude <= 0.0001f)
            return;

        AddExternalVelocity(fromSource.normalized * knockbackPower);
    }

    public void ClearExternalVelocity()
    {
        externalVelocity = Vector2.zero;
    }

    public void ClearBaseVelocity()
    {
        desiredBaseVelocity = Vector2.zero;
        currentBaseVelocity = Vector2.zero;
        finalVelocity = externalVelocity;
    }

    public void ClearAllVelocity()
    {
        desiredBaseVelocity = Vector2.zero;
        currentBaseVelocity = Vector2.zero;
        externalVelocity = Vector2.zero;
        finalVelocity = Vector2.zero;

        isMoving = false;
        currentMoveDirection = Vector2.zero;
    }

    #endregion

    #region Position

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void Teleport(Vector3 position, Vector2 lookDirection)
    {
        transform.position = position;
        ClearAllVelocity();

        if (lookDirection.sqrMagnitude > 0.0001f)
            FaceDirection(lookDirection);

        SetIdleVisual();
    }

    #endregion

    #region Visual

    public void FaceDirection(Vector2 direction)
    {
        if (!CanFaceByMovement())
            return;

        if (Mathf.Abs(direction.x) <= FaceThreshold)
            return;

        Vector2 horizontalDirection = direction.x < 0f
            ? Vector2.left
            : Vector2.right;

        lastMoveDirection = horizontalDirection;

        if (visual != null)
            visual.LookDirection(horizontalDirection);
    }

    private void PlayMoveVisual(Vector2 direction)
    {
        if (visual == null)
            return;

        if (!CanFaceByMovement())
            return;

        visual.PlayMove(direction);
    }

    private void SetIdleVisual()
    {
        bool wasMoving = isMoving;

        isMoving = false;
        currentMoveDirection = Vector2.zero;

        if (!wasMoving)
            return;

        if (visual != null)
            visual.StopMove();
    }

    #endregion
}