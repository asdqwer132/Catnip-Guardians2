using UnityEngine;

public enum HealthBarValueSource { Health, EnemyPopulation }

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public HealthBarValueSource valueSource;
    public Health health;
    [Tooltip("Enemy Population일 때 사용할 같은 배틀 씬의 EnemyManager입니다.")]
    public EnemyManager enemyManager;
    public GameObject pannel;

    [Header("UI")]
    public ImageFillUI hpFill;

    [Header("Option")]
    public bool hideWhenFullHp = false;
    public bool hideWhenDead = true;

    private bool panelVisibleBySetting = true;
    private Health subscribedHealth;
    private EnemyManager subscribedEnemyManager;

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

    public void SetEnemyManager(EnemyManager target)
    {
        valueSource = HealthBarValueSource.EnemyPopulation;
        enemyManager = target;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (!isActiveAndEnabled) return;
        if (valueSource == HealthBarValueSource.EnemyPopulation)
        {
            subscribedEnemyManager = enemyManager;
            if (subscribedEnemyManager != null) subscribedEnemyManager.OnEnemyCountChanged += RefreshEnemyCount;
        }
        else if (health != null)
        {
            subscribedHealth = health;
            subscribedHealth.OnHpChanged += Refresh;
            subscribedHealth.OnDead += HandleDead;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedHealth != null)
        {
            subscribedHealth.OnHpChanged -= Refresh;
            subscribedHealth.OnDead -= HandleDead;
        }
        if (subscribedEnemyManager != null) subscribedEnemyManager.OnEnemyCountChanged -= RefreshEnemyCount;
        subscribedHealth = null;
        subscribedEnemyManager = null;
    }

    private void Refresh()
    {
        if (valueSource == HealthBarValueSource.EnemyPopulation)
        {
            RefreshEnemyCount(enemyManager != null ? enemyManager.CurrentAliveEnemyCount : 0,
                enemyManager != null ? enemyManager.maxAliveEnemyCount : 0);
            return;
        }
        if (health == null)
            return;

        Refresh(health.Hp, health.MaxHp);
    }

    private void Refresh(float hp, float maxHp)
    {
        if (hpFill == null)
            return;

        hpFill.SetFill(hp, maxHp);

        if (valueSource == HealthBarValueSource.Health && hideWhenFullHp)
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
        if (valueSource != HealthBarValueSource.Health || !hideWhenDead)
            return;

        if (hpFill != null)
            hpFill.SetVisible(false);
    }

    private void RefreshEnemyCount(int count, int maximum) => Refresh(count, maximum);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        Unsubscribe();
        Subscribe();
        Refresh();
    }
#endif
}
