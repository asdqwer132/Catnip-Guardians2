using UnityEngine;

public class ActorMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;

    [Header("Components")]
    public ActorVisual visual;

    private Vector2 lastMoveDirection;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponent<ActorVisual>();
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Stop();
            return;
        }

        Vector2 direction = target.position - transform.position;
        MoveDirection(direction);
    }

    public void MoveToPosition(Vector3 targetPosition, float stopDistance = 0.03f)
    {
        Vector2 toTarget = targetPosition - transform.position;

        if (toTarget.magnitude <= stopDistance)
        {
            Stop();
            return;
        }

        MoveDirection(toTarget);
    }

    public void MoveToDistanceFromTarget(Transform target, float targetDistance, float tolerance)
    {
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
            MoveDirection(directionToTarget);
        else
            MoveDirection(-directionToTarget);
    }

    public void MoveDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            Stop();
            return;
        }

        direction.Normalize();
        lastMoveDirection = direction;

        if (visual != null)
            visual.PlayMove(direction);

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void Stop()
    {
        if (visual != null)
            visual.StopMove(lastMoveDirection);
    }
}