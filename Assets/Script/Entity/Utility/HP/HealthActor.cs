using System.Collections;
using UnityEngine;

public abstract class HealthActor : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public Health health;

    [Header("Health UI")]
    public HealthBarUI healthBarUI;

    [Header("Visual")]
    public ActorVisual visual;

    [Header("Damage Popup")]
    public DamagePopupSpawner damagePopupSpawner;
    public bool useDamagePopup = true;

    [Header("Death Option")]
    public bool hideHealthBarOnDeath = true;

    public Transform DamageTransform => transform;
    public bool IsDead => health != null && health.IsDead;

    private bool isDying = false;
    private Coroutine deathCoroutine;

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
    }

    protected virtual void OnEnable()
    {
        SubscribeHealth();
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

        ResetDeathState();

        health.Init(maxHp, fillHp);

        ConnectHealthUI();
        ShowHealthBar();
    }

    public virtual void Revive(float maxHp, bool fillHp = true)
    {
        if (health == null)
        {
            Debug.LogWarning(name + " Health가 없습니다.");
            return;
        }

        ResetDeathState();

        health.Init(maxHp, fillHp);

        ConnectHealthUI();
        ShowHealthBar();

        //if (visual != null)
        //    visual.PlayIdle();

        OnRevived();
    }

    public virtual void ResetDeathState()
    {
        StopDeathRoutine();
        isDying = false;
    }

    private void StopDeathRoutine()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }
    }

    void ConnectHealthUI()
    {
        if (healthBarUI == null)
            return;

        if (health == null)
            return;

        healthBarUI.SetTarget(health);
    }

    protected virtual void ShowHealthBar()
    {
        if (healthBarUI == null)
            return;
        healthBarUI.gameObject.SetActive(true);
        healthBarUI.SetTarget(health);
    }

    protected virtual void HideHealthBar()
    {
        if (healthBarUI == null)
            return;

        healthBarUI.gameObject.SetActive(false);
    }

    void SubscribeHealth()
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

    void UnsubscribeHealth()
    {
        if (health == null)
            return;

        health.OnDamaged -= HandleDamagedInternal;
        health.OnHealed -= HandleHealedInternal;
        health.OnDead -= HandleDeadInternal;
    }

    void HandleDamagedInternal(float damage)
    {
        if (isDying)
            return;

        ShowDamagePopup(damage);
        OnDamaged(damage);
    }

    void HandleHealedInternal(float amount)
    {
        if (isDying)
            return;

        OnHealed(amount);
    }

    void HandleDeadInternal()
    {
        if (isDying)
            return;

        isDying = true;

        StopDeathRoutine();
        deathCoroutine = StartCoroutine(DeathRoutine());
    }

    void ShowDamagePopup(float damage)
    {
        if (!useDamagePopup)
            return;

        if (damagePopupSpawner == null)
            return;

        damagePopupSpawner.ShowDamage(damage);
    }

    IEnumerator DeathRoutine()
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

    public virtual void TakeDamage(float damage)
    {
        if (health == null)
            return;

        if (isDying)
            return;

        health.TakeDamage(damage);
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