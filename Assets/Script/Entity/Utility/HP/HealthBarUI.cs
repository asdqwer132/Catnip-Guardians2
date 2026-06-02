using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public Health health;

    [Header("UI")]
    public ImageFillUI hpFill;

    [Header("Option")]
    public bool hideWhenFullHp = false;
    public bool hideWhenDead = true;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (hpFill == null)
            hpFill = GetComponentInChildren<ImageFillUI>();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    #region Setting

    public void SetTarget(Health targetHealth)
    {
        Unsubscribe();

        health = targetHealth;

        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (health == null)
            return;

        health.OnHpChanged -= Refresh;
        health.OnHpChanged += Refresh;

        health.OnDead -= HandleDead;
        health.OnDead += HandleDead;
    }

    private void Unsubscribe()
    {
        if (health == null)
            return;

        health.OnHpChanged -= Refresh;
        health.OnDead -= HandleDead;
    }

    #endregion

    #region Refresh

    private void Refresh()
    {
        if (health == null)
            return;

        Refresh(health.Hp, health.MaxHp);
    }

    private void Refresh(float hp, float maxHp)
    {
        if (hpFill == null)
            return;

        hpFill.SetFill(hp, maxHp);

        if (hideWhenFullHp)
        {
            bool isFull = hp >= maxHp;
            hpFill.SetVisible(!isFull);
        }
    }

    #endregion

    private void HandleDead()
    {
        if (!hideWhenDead)
            return;

        if (hpFill != null)
            hpFill.SetVisible(false);
    }
}