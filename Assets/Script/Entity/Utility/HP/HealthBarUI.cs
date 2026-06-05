using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public Health health;
    public GameObject pannel;

    [Header("UI")]
    public ImageFillUI hpFill;

    [Header("Option")]
    public bool hideWhenFullHp = false;
    public bool hideWhenDead = true;

    private bool panelVisibleBySetting = true;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (hpFill == null)
            hpFill = GetComponentInChildren<ImageFillUI>();

        ApplyPanelVisible();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
        ApplyPanelVisible();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetPanelVisibleBySetting(bool visible)
    {
        panelVisibleBySetting = visible;
        ApplyPanelVisible();
        Refresh();
    }

    private void ApplyPanelVisible()
    {
        if (pannel != null)
            pannel.SetActive(panelVisibleBySetting);
        else
            gameObject.SetActive(panelVisibleBySetting);
    }

    public void SetTarget(Health targetHealth)
    {
        Unsubscribe();

        health = targetHealth;

        Subscribe();
        Refresh();
        ApplyPanelVisible();
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
            hpFill.SetVisible(!isFull && panelVisibleBySetting);
        }
        else
        {
            hpFill.SetVisible(panelVisibleBySetting);
        }
    }

    private void HandleDead()
    {
        if (!hideWhenDead)
            return;

        if (hpFill != null)
            hpFill.SetVisible(false);
    }
}