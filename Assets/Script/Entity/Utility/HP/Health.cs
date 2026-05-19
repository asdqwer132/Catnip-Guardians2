using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Runtime HP")]
    [SerializeField] private float hp;
    [SerializeField] private float maxHp;

    public float Hp => hp;
    public float MaxHp => maxHp;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDead;

    public void Init(float startMaxHp, bool fillHp = true)
    {
        if (startMaxHp <= 0f)
            startMaxHp = 1f;

        maxHp = startMaxHp;

        if (fillHp)
            hp = maxHp;
        else
            hp = Mathf.Clamp(hp, 0f, maxHp);

        IsDead = false;

        BroadcastHpChanged();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        hp -= damage;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        OnDamaged?.Invoke(damage);
        BroadcastHpChanged();

        if (hp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (amount <= 0f)
            return;

        hp += amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        OnHealed?.Invoke(amount);
        BroadcastHpChanged();
    }

    public void SetMaxHp(float newMaxHp, bool fillHp = false)
    {
        if (newMaxHp <= 0f)
            newMaxHp = 1f;

        maxHp = newMaxHp;

        if (fillHp)
            hp = maxHp;
        else
            hp = Mathf.Clamp(hp, 0f, maxHp);

        BroadcastHpChanged();

        if (hp <= 0f)
            Die();
    }

    public void Kill()
    {
        if (IsDead)
            return;

        hp = 0f;
        BroadcastHpChanged();
        Die();
    }

    void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        hp = 0f;

        BroadcastHpChanged();
        OnDead?.Invoke();
    }

    void BroadcastHpChanged()
    {
        OnHpChanged?.Invoke(hp, maxHp);
    }
}