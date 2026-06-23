using System;
using UnityEngine;

[Serializable]
public class HealthStat : IGameStat<HealthStat>
{
    [Header("Spawn")]
    public float hp = 1.5f;
    public float maxHp = 8f;

    public HealthStat Clone()
    {
        return new HealthStat
        {
            hp = hp,
            maxHp = maxHp
        };
    }

    public void Clamp() { }
}
public class Health : MonoBehaviour, IBuffTarget
{

    [Header("Runtime HP")]
    public HealthStat currentHealthStat;

    public float Hp => currentHealthStat.hp;
    public float MaxHp => currentHealthStat.maxHp;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDead;

    public UnityEngine.Object BuffTargetObject => this;
    public string buffTargetGroup = "Health";
    public string BuffTargetGroup => buffTargetGroup;
    public string BuffTargetDebugName => name;

    public void RefreshBuffedStat()
    {
        currentHealthStat = BuffManager.instance.GetBuffedStatForTarget(currentHealthStat, this);
    }
    public void Init(float startMaxHp, bool fillHp = true)
    {
        if (startMaxHp <= 0f)
            startMaxHp = 1f;

        currentHealthStat.maxHp = startMaxHp;

        if (fillHp)
            currentHealthStat.hp = currentHealthStat.maxHp;
        else
            currentHealthStat.hp = Mathf.Clamp(currentHealthStat.hp, 0f, currentHealthStat.maxHp);

        IsDead = false;

        BroadcastHpChanged();
    }

    #region Event
    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        currentHealthStat.hp -= damage;
        currentHealthStat.hp = Mathf.Clamp(currentHealthStat.hp, 0f, currentHealthStat.maxHp);

        OnDamaged?.Invoke(damage);
        BroadcastHpChanged();

        if (currentHealthStat.hp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (amount <= 0f)
            return;

        currentHealthStat.hp += amount;
        currentHealthStat.hp = Mathf.Clamp(currentHealthStat.hp, 0f, currentHealthStat.maxHp);

        OnHealed?.Invoke(amount);
        BroadcastHpChanged();
    }
    void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        currentHealthStat.hp = 0f;

        BroadcastHpChanged();
        OnDead?.Invoke();
    }
    #endregion

    void BroadcastHpChanged()
    {
        OnHpChanged?.Invoke(currentHealthStat.hp, currentHealthStat.maxHp);
    }
}