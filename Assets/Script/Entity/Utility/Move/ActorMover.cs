using UnityEngine;

public class ActorMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;

    [Header("Components")]
    public ActorVisual visual;

    void Awake()
    {
        if (visual == null)
            visual = GetComponent<ActorVisual>();
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    #region Move
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
            MoveDirection(Vector2.right);
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

        if (visual != null)
        {
            visual.PlayMove();
            visual.LookDirection(direction);
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }
    public void Stop()
    {
        if (visual != null)
            visual.StopMove();
    }
    #endregion
}