using UnityEngine;

public class ActorTarget : MonoBehaviour
{
    private IDamageable target;

    [Header("Debug")]
    [SerializeField] private string targetName = "None";

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
            return target != null && target.DamageTransform != null && !target.IsDead;
        }
    }

    public void SetTarget(IDamageable newTarget)
    {
        target = newTarget;
        RefreshTargetName();
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
        {
            RefreshTargetName();
            return;
        }

        target.TakeDamage(damage);
    }

    private void RefreshTargetName()
    {
        if (target == null || target.DamageTransform == null)
        {
            targetName = "None";
            return;
        }

        targetName = target.DamageTransform.name;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshTargetName();
    }
#endif
}