using UnityEngine;

public interface IDamageable
{
    Transform DamageTransform { get; }
    bool IsDead { get; }

    void TakeDamage(float damage);
}