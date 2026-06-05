using System.Collections;
using UnityEngine;

public abstract class HealthActor : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public Health health;

    [Header("Visual")]
    public HealthBarUI healthBarUI;
    public ActorVisual visual;
    public DamagePopupSpawner damagePopupSpawner;
    public bool useDamagePopup = true;
    public bool hideHealthBarOnDeath = true;

    public Transform DamageTransform => transform;
    public bool IsDead => health != null && health.IsDead;

    private bool isDying = false;
    private Coroutine deathCoroutine;
    private bool healthBarVisibleBySetting = true;

    protected virtual void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (healthBarUI == null)
            healthBarUI = GetComponentInChildren<HealthBarUI>(true);

        if (visual == null)
            visual = GetComponent<ActorVisual>();

        if (damagePopupSpawner == null)
            damagePopupSpawner = GetComponent<DamagePopupSpawner>();

        ConnectHealthUI();
        ApplyHealthBarVisibleBySetting();
    }

    protected virtual void OnEnable()
    {
        SubscribeHealth();
        ApplyHealthBarVisibleBySetting();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeHealth();
        StopDeathRoutine();
    }

    protected void InitHealth(float maxHp, bool fillHp = true)
    {
        if (health == null)
            return;

        ResetActorStateForReuse();

        health.Init(maxHp, fillHp);

        ConnectHealthUI();
        ApplyHealthBarVisibleBySetting();
    }

    protected virtual void ResetActorStateForReuse()
    {
        ResetDeathState();

        ApplyHealthBarVisibleBySetting();

        if (visual != null)
            visual.ResetVisual();
    }

    public virtual void Revive(float maxHp, bool fillHp = true)
    {
        if (health == null)
        {
            Debug.LogWarning(name + " Health가 없습니다.");
            return;
        }

        ResetActorStateForReuse();

        health.Init(maxHp, fillHp);

        ConnectHealthUI();
        ApplyHealthBarVisibleBySetting();

        OnRevived();
    }

    public virtual void ResetDeathState()
    {
        StopDeathRoutine();
        isDying = false;
    }

    private IEnumerator DeathRoutine()
    {
        OnDeathStarted();

        if (hideHealthBarOnDeath)
            HideHealthBar();

        if (visual != null)
        {
            visual.PlayDie();
            yield return visual.WaitCurrentAnimationEnd();
        }

        deathCoroutine = null;

        OnDeathFinished();
    }

    private void StopDeathRoutine()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }
    }

    private void ConnectHealthUI()
    {
        if (healthBarUI == null)
            return;

        if (health == null)
            return;

        healthBarUI.SetTarget(health);
    }

    public void SetHealthBarVisibleBySetting(bool visible)
    {
        healthBarVisibleBySetting = visible;
        ApplyHealthBarVisibleBySetting();
    }

    private void ApplyHealthBarVisibleBySetting()
    {
        if (healthBarUI == null)
            return;

        if (IsDead && hideHealthBarOnDeath)
        {
            healthBarUI.SetPanelVisibleBySetting(false);
            return;
        }

        healthBarUI.SetPanelVisibleBySetting(healthBarVisibleBySetting);
        healthBarUI.SetTarget(health);
    }

    protected virtual void ShowHealthBar()
    {
        if (healthBarUI == null)
            return;

        healthBarUI.SetTarget(health);
        ApplyHealthBarVisibleBySetting();
    }

    protected virtual void HideHealthBar()
    {
        if (healthBarUI == null)
            return;

        healthBarUI.SetPanelVisibleBySetting(false);
    }

    private void ShowDamagePopup(float damage)
    {
        if (!useDamagePopup)
            return;

        if (damagePopupSpawner == null)
            return;

        if (SettingManager.instance != null &&
            SettingManager.instance.setting.showDamagePopup)
        {
            damagePopupSpawner.ShowDamage(damage);
        }
    }

    private void SubscribeHealth()
    {
        if (health == null)
            return;

        health.OnDamaged -= HandleDamagedInternal;
        health.OnDamaged += HandleDamagedInternal;

        health.OnHealed -= HandleHealedInternal;
        health.OnHealed += HandleHealedInternal;

        health.OnDead -= HandleDeadInternal;
        health.OnDead += HandleDeadInternal;
    }

    private void UnsubscribeHealth()
    {
        if (health == null)
            return;

        health.OnDamaged -= HandleDamagedInternal;
        health.OnHealed -= HandleHealedInternal;
        health.OnDead -= HandleDeadInternal;
    }

    private void HandleDamagedInternal(float damage)
    {
        if (isDying)
            return;

        ShowDamagePopup(damage);
        OnDamaged(damage);
    }

    private void HandleHealedInternal(float amount)
    {
        if (isDying)
            return;

        OnHealed(amount);
    }

    private void HandleDeadInternal()
    {
        if (isDying)
            return;

        isDying = true;

        StopDeathRoutine();
        deathCoroutine = StartCoroutine(DeathRoutine());
    }

    public virtual void TakeDamage(float damage)
    {
        if (health == null)
            return;

        if (isDying)
            return;

        health.TakeDamage(damage);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx("Effect", "pyro");
    }

    public virtual void Heal(float amount)
    {
        if (health == null)
            return;

        if (isDying)
            return;

        health.Heal(amount);
    }

    protected virtual void OnDamaged(float damage) { }
    protected virtual void OnHealed(float amount) { }
    protected virtual void OnDeathStarted() { }
    protected virtual void OnDeathFinished() { }
    protected virtual void OnRevived() { }
}