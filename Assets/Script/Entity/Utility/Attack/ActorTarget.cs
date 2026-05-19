using UnityEngine;

public class ActorTarget : MonoBehaviour
{
    private IDamageable target;

    public IDamageable Target => target;

    public Transform TargetTransform
    {
        get
        {
            if (target == null)
                return null;

            return target.DamageTransform;
        }
    }

    public bool HasTarget
    {
        get
        {
            return target != null &&
                   target.DamageTransform != null &&
                   !target.IsDead;
        }
    }

    public void SetTarget(IDamageable newTarget)
    {
        target = newTarget;
    }

    public void SetTargetFromComponent(Component component)
    {
        if (component == null)
        {
            target = null;
            return;
        }

        target = component.GetComponent<IDamageable>();
    }

    public void ClearTarget()
    {
        target = null;
    }

    public float GetDistanceFrom(Transform origin)
    {
        if (!HasTarget || origin == null)
            return float.MaxValue;

        return Vector2.Distance(
            origin.position,
            TargetTransform.position
        );
    }

    public void DamageTarget(float damage)
    {
        if (!HasTarget)
            return;

        target.TakeDamage(damage);
    }
}