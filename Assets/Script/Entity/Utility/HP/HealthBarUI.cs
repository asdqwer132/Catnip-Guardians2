using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public Health health;

    [Header("UI")]
    public Slider hpSlider;

    [Header("Option")]
    public bool hideWhenFullHp = false;
    public bool hideWhenDead = true;

    void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (hpSlider == null)
            hpSlider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void SetTarget(Health targetHealth)
    {
        Unsubscribe();

        health = targetHealth;

        Subscribe();
        Refresh();
    }

    void Subscribe()
    {
        if (health == null)
            return;

        health.OnHpChanged -= Refresh;
        health.OnHpChanged += Refresh;

        health.OnDead -= HandleDead;
        health.OnDead += HandleDead;
    }

    void Unsubscribe()
    {
        if (health == null)
            return;

        health.OnHpChanged -= Refresh;
        health.OnDead -= HandleDead;
    }

    void Refresh()
    {
        if (health == null)
            return;

        Refresh(health.Hp, health.MaxHp);
    }

    void Refresh(float hp, float maxHp)
    {
        if (hpSlider == null)
            return;

        hpSlider.maxValue = maxHp;
        hpSlider.value = hp;

        if (hideWhenFullHp)
        {
            bool isFull = hp >= maxHp;
            hpSlider.gameObject.SetActive(!isFull);
        }
    }

    void HandleDead()
    {
        if (!hideWhenDead)
            return;

        if (hpSlider != null)
            hpSlider.gameObject.SetActive(false);
    }
}