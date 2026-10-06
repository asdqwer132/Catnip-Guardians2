using UnityEngine;

public class ActorTarget : MonoBehaviour
{
    private IDamageable target;
    [Header("Debug")]
    [SerializeField] private string targetName = "None";

    // 인터페이스 참조는 Unity의 파괴된 Object를 자동으로 null 취급하지 않는다.
    private bool IsTargetAlive => target != null &&
        (!(target is UnityEngine.Object) || (UnityEngine.Object)target != null);
    public Transform TargetTransform => IsTargetAlive ? target.DamageTransform : null;
    public bool HasTarget => IsTargetAlive && !target.IsDead && target.DamageTransform != null;
    public IDamageable TargetDamageable => HasTarget ? target : null;

    public void SetTarget(IDamageable newTarget)
    {
        target = newTarget;
        RefreshTargetName();
    }

    public float GetDistanceFrom(Transform origin)
    {
        if (!HasTarget || origin == null) return float.MaxValue;
        return Mathf.Sqrt(GetSqrDistanceFrom(origin));
    }

    public float GetSqrDistanceFrom(Transform origin)
    {
        Transform destination = TargetTransform;
        if (!HasTarget || origin == null || destination == null) return float.MaxValue;
        Vector2 difference = destination.position - origin.position;
        return difference.sqrMagnitude;
    }

    public void DamageTarget(float damage)
    {
        if (!HasTarget) { RefreshTargetName(); return; }
        target.TakeDamage(damage);
    }

    private void RefreshTargetName()
    {
        Transform destination = TargetTransform;
        targetName = destination != null ? destination.name : "None";
    }

#if UNITY_EDITOR
    private void OnValidate() { RefreshTargetName(); }
#endif
}
