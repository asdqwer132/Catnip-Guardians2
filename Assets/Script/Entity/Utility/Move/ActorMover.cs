using UnityEngine;

public class ActorMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;
    public float stopDistance = 0.03f;

    [Header("Components")]
    public ActorVisual visual;
    public ActorAttack attack;

    [Header("Debug")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isMoveStopped;
    [SerializeField] private Vector2 currentMoveDirection;
    [SerializeField] private Vector2 lastMoveDirection = Vector2.right;

    public bool IsMoving => isMoving;
    public bool IsMoveStopped => isMoveStopped;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public Vector2 LastMoveDirection => lastMoveDirection;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponent<ActorVisual>();

        if (attack == null)
            attack = GetComponent<ActorAttack>();
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
        isMoving = false;
        currentMoveDirection = Vector2.zero;

        if (visual != null)
            visual.ForceIdle(lastMoveDirection, true, false);
    }

    private bool CanMove()
    {
        return !isMoveStopped;
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

        Vector2 delta = direction.normalized * moveSpeed * Time.deltaTime;
        MoveBy(delta);
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

        Vector2 direction = delta.normalized;

        isMoving = true;
        currentMoveDirection = direction;
        lastMoveDirection = direction;

        transform.position += (Vector3)delta;

        PlayMoveVisual(direction);
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

        if (lookDirection.sqrMagnitude > 0.0001f)
            FaceDirection(lookDirection);

        Stop();
    }

    #endregion

    #region Visual

    public void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector2 normalizedDirection = direction.normalized;
        lastMoveDirection = normalizedDirection;

        if (visual != null)
            visual.LookDirection(normalizedDirection);
    }

    private void PlayMoveVisual(Vector2 direction)
    {
        if (visual == null)
            return;

        if (attack != null && attack.IsAttacking)
            return;

        visual.PlayMove(direction);
    }

    public void Stop()
    {
        isMoving = false;
        currentMoveDirection = Vector2.zero;

        if (visual != null)
            visual.StopMove(lastMoveDirection);
    }

    #endregion
}