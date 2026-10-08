using System.Collections.Generic;
using UnityEngine;

public enum DamageApplyMode
{
    HitOnce,
    EveryEnter,
    Periodic
}

[BuffTargetGroups("DamageArea")]
public class DamageArea : AttackObject<DamageAreaAttackStat>, IBuffTarget
{
    private static readonly List<DamageArea> activeDamageAreas =
        new List<DamageArea>();

    [Header("Component")]
    public CircleCollider2D circleCollider;
    public Transform rangeVisual;

    [Header("Buff Target")]
    [BuffTargetGroupName]
    public string buffTargetGroup = "DamageArea";
    public Object BuffTargetObject => this;
    public string BuffTargetGroup => buffTargetGroup;
    public string BuffTargetDebugName => name;
    private BuffManager registeredBuffManager;

    [Header("Damage")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Header("Runtime Stat")]
    [SerializeField] private float damage = 10f;

    [Min(0.01f)]
    [SerializeField] private float damageInterval = 0.5f;

    [SerializeField] private float radius = 1f;
    [SerializeField] private float lifeTime = 0.2f;

    private float timer;

    private readonly HashSet<EnemyLifeKey> hitObjects = new HashSet<EnemyLifeKey>();
    private readonly Dictionary<EnemyLifeKey, float> periodicTimers = new Dictionary<EnemyLifeKey, float>();
    private readonly Dictionary<EnemyLifeKey, HashSet<Collider2D>> contacts = new Dictionary<EnemyLifeKey, HashSet<Collider2D>>();
    private readonly Dictionary<Collider2D, EnemyLifeKey> contactLives = new Dictionary<Collider2D, EnemyLifeKey>();

    private HitEffectDispatcher hitDispatcher;
    private ItemEffectContext hitSourceContext;

    protected virtual void Awake()
    {
        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (rangeVisual == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                rangeVisual = spriteRenderer.transform;
        }

        ApplyRadius();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!activeDamageAreas.Contains(this))
            activeDamageAreas.Add(this);

        timer = 0f;
        if (useSnapshotAndDynamicBuff)
        {
            RegisterDynamicBuffReceiver();
            RegisterBuffTarget();
            RefreshBuffedStat();
        }
    }

    protected override void OnDisable()
    {
        activeDamageAreas.Remove(this);

        hitObjects.Clear();
        periodicTimers.Clear();
        contacts.Clear();
        contactLives.Clear();
        hitDispatcher = null;
        hitSourceContext = null;

        base.OnDisable();
        UnregisterBuffTarget();
    }

    protected virtual void Update()
    {
        if (hitSourceContext != null && !hitSourceContext.CanContinue)
        { gameObject.SetActive(false); Destroy(gameObject); return; }
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            CompleteLifetime();
            Destroy(gameObject);
        }
    }

    public override void InitWithSnapshotAndDynamicBuff(
        DamageAreaAttackStat snapshotAttackStat,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        BuffManager buffManager,
        GameObject owner
    )
    {
        UnregisterBuffTarget();
        timer = 0f;
        hitObjects.Clear();
        periodicTimers.Clear();
        contacts.Clear();
        contactLives.Clear();

        base.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat,
            sourceItemData,
            sourceBag,
            buffManager,
            owner
        );

        RegisterBuffTarget();
        ApplyRadius();
    }

    public void RefreshBuffedStat() => OnDynamicBuffChanged();

    protected override DamageAreaAttackStat ApplyTargetBuffs(DamageAreaAttackStat currentStat)
        => buffManager != null
            ? buffManager.GetBuffedStatForTarget(currentStat, this, BuffCalculationMode.All)
            : currentStat;

    private void RegisterBuffTarget()
    {
        if (!isActiveAndEnabled || buffManager == null || registeredBuffManager == buffManager) return;
        registeredBuffManager = buffManager;
        registeredBuffManager.RegisterBuffTarget(this);
    }

    private void UnregisterBuffTarget()
    {
        BuffManager previous = registeredBuffManager;
        registeredBuffManager = null;
        if (previous != null) previous.UnregisterBuffTarget(this);
    }

    public void InitHitEffects(
        HitEffectData[] effects,
        HitEffectApplyMode applyMode,
        ItemEffectContext context
    )
    {
        contacts.Clear();
        contactLives.Clear();
        hitSourceContext = context != null ? context.Copy(context.targetPosition, context.direction) : null;
        hitDispatcher = new HitEffectDispatcher(effects, applyMode, hitSourceContext);
    }

    protected override void ApplyStat(DamageAreaAttackStat currentStat)
    {
        if (currentStat == null)
            return;

        damage = currentStat.damageAreaPower;
        damageInterval = Mathf.Max(0.01f, currentStat.damageAreaInterval);
        radius = Mathf.Max(0.01f, currentStat.damageAreaRange);
        lifeTime = Mathf.Max(0.01f, currentStat.damageAreaLifeTime);

        ApplyRadius();
    }

    protected virtual void ApplyRadius()
    {
        radius = Mathf.Max(0.01f, radius);

        if (rangeVisual != null)
        {
            rangeVisual.localScale = new Vector3(
                radius * 2f,
                radius * 2f,
                1f
            );
        }

        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.radius = radius;
            circleCollider.isTrigger = true;
        }

        transform.localScale = Vector3.one;
    }

    // Derived shapes filter this bounding collider without duplicating damage policy.
    protected virtual bool IsInsideAttack(Enemy enemy) => true;

    protected virtual void OnTriggerEnter2D(Collider2D other) => TryContact(other);
    protected virtual void OnTriggerStay2D(Collider2D other) => TryContact(other);
    protected virtual void OnTriggerExit2D(Collider2D other) => RemoveContact(other);

    private void RemoveContact(Collider2D other)
    {
        if (other == null) return;
        EnemyLifeKey key;
        if (!contactLives.TryGetValue(other, out key)) return;
        contactLives.Remove(other);
        HashSet<Collider2D> colliders;
        if (!contacts.TryGetValue(key, out colliders)) return;
        colliders.Remove(other);
        if (colliders.Count != 0) return;
        contacts.Remove(key);
        periodicTimers.Remove(key);
    }

    private void TryContact(Collider2D other)
    {
        Enemy enemy = other != null ? other.GetComponentInParent<Enemy>() : null;
        if (enemy == null || !enemy.CanReceiveHitEffects || enemy.gameObject == owner || !IsInsideAttack(enemy))
        { RemoveContact(other); return; }
        EnemyLifeKey key = new EnemyLifeKey(enemy);
        EnemyLifeKey old;
        if (contactLives.TryGetValue(other, out old) && !old.Equals(key)) RemoveContact(other);
        bool entered = !contacts.ContainsKey(key);
        HashSet<Collider2D> colliders;
        if (!contacts.TryGetValue(key, out colliders))
        { colliders = new HashSet<Collider2D>(); contacts.Add(key, colliders); }
        colliders.Add(other);
        contactLives[other] = key;

        if (damageApplyMode == DamageApplyMode.HitOnce)
        { if (hitObjects.Add(key)) ApplyHit(enemy); return; }
        if (damageApplyMode == DamageApplyMode.EveryEnter)
        { if (entered) ApplyHit(enemy); return; }
        float next;
        if (periodicTimers.TryGetValue(key, out next) && Time.time < next) return;
        // Absolute time means an enemy with several colliders never gains extra ticks.
        periodicTimers[key] = Time.time + damageInterval;
        ApplyHit(enemy);
    }

    private void ApplyHit(Enemy enemy)
    {
        if (hitDispatcher != null) hitDispatcher.Hit(enemy, damage,
            hitSourceContext != null ? hitSourceContext.direction : Vector3.zero);
        else if (enemy != null && enemy.CanReceiveHitEffects) enemy.TakeDamage(damage);
    }

    #region Clear

    public static void ClearAllActiveDamageAreas()
    {
        for (int i = activeDamageAreas.Count - 1; i >= 0; i--)
        {
            DamageArea area = activeDamageAreas[i];

            if (area == null)
            {
                activeDamageAreas.RemoveAt(i);
                continue;
            }

            if (area.circleCollider != null)
                area.circleCollider.enabled = false;

            area.gameObject.SetActive(false);
            Destroy(area.gameObject);
        }

        activeDamageAreas.Clear();
    }

    #endregion
}
