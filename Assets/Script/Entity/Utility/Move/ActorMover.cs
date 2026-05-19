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

    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Stop();
            return;
        }

        if (visual != null)
            visual.SetWalking(true);

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
    }

    public void Stop()
    {
        if (visual != null)
            visual.SetWalking(false);
    }
}