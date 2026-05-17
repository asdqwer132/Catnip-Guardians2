using System.Collections.Generic;
using UnityEngine;

public enum DamageApplyMode
{
    HitOnce,
    EveryEnter,
    Periodic
}

public class DamageArea : MonoBehaviour
{
    [Header("Area")]
    public CircleCollider2D circleCollider;
    public Transform rangeVisual;

    [Header("Life Time")]
    public float lifeTime = 0.2f;

    [Header("Hit Option")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Tooltip("Periodic일 때 데미지가 들어가는 간격")]
    [Min(0.01f)]
    public float damageInterval = 0.5f;

    public GameObject owner;

    private float damage = 10f;
    private float radius = 1f;
    private float timer;

    private readonly HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> periodicTimers = new Dictionary<GameObject, float>();

    private bool useDynamicStat;
    private InventoryItem statSourceItem;
    private AttackStat baseAttackStat;
    private EffectStat ownerStat;
    private int currentCycleId;

    void Awake()
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

    public void Init(
        float damage,
        float radius,
        float lifeTime,
        DamageApplyMode damageApplyMode,
        float damageInterval,
        GameObject owner
    )
    {
        useDynamicStat = false;

        this.damage = damage;
        this.radius = Mathf.Max(0.01f, radius);
        this.lifeTime = Mathf.Max(0.01f, lifeTime);
        this.damageApplyMode = damageApplyMode;
        this.damageInterval = Mathf.Max(0.01f, damageInterval);
        this.owner = owner;

        timer = 0f;
        hitObjects.Clear();
        periodicTimers.Clear();

        ApplyRadius();
    }

    public void InitDynamic(
        InventoryItem statSourceItem,
        AttackStat baseAttackStat,
        GameObject owner,
        EffectStat ownerStat,
        int currentCycleId
    )
    {
        useDynamicStat = true;

        this.statSourceItem = statSourceItem;
        this.baseAttackStat = baseAttackStat != null ? baseAttackStat.CloneAttack() : null;
        this.owner = owner;
        this.ownerStat = ownerStat;
        this.currentCycleId = currentCycleId;

        timer = 0f;
        hitObjects.Clear();
        periodicTimers.Clear();

        RefreshDynamicStat();
        ApplyRadius();
    }

    void Update()
    {
        if (useDynamicStat)
        {
            RefreshDynamicStat();
        }

        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void RefreshDynamicStat()
    {
        if (baseAttackStat == null)
            return;

        EffectStat finalStat;

        if (statSourceItem != null)
        {
            finalStat = statSourceItem.GetFinalEffectStat(
                baseAttackStat,
                ownerStat,
                currentCycleId
            );
        }
        else
        {
            finalStat = baseAttackStat.Clone();

            if (ownerStat != null)
                finalStat.Add(ownerStat);
        }

        AttackStat attackStat = finalStat as AttackStat;

        if (attackStat == null)
            return;

        damage = attackStat.GetAttackDamage();
        radius = attackStat.GetSafeRadius();
        lifeTime = attackStat.GetSafeLifeTime();
        damageApplyMode = attackStat.damageApplyMode;
        damageInterval = attackStat.GetSafeDamageInterval();

        ApplyRadius();
    }

    private void ApplyRadius()
    {
        radius = Mathf.Max(0.01f, radius);

        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.radius = radius;
            circleCollider.isTrigger = true;
        }

        if (rangeVisual != null)
        {
            rangeVisual.localScale = new Vector3(
                radius * 2f,
                radius * 2f,
                1f
            );
        }

        transform.localScale = Vector3.one;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        RefreshDynamicStatIfNeeded();

        if (damageApplyMode == DamageApplyMode.HitOnce)
        {
            TryHitOnce(other);
        }
        else if (damageApplyMode == DamageApplyMode.EveryEnter)
        {
            TryHitAlways(other);
        }
        else if (damageApplyMode == DamageApplyMode.Periodic)
        {
            TryHitPeriodicEnter(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        RefreshDynamicStatIfNeeded();

        if (damageApplyMode != DamageApplyMode.Periodic)
            return;

        TryHitPeriodicStay(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        GameObject targetObj = GetTargetObject(other);

        if (targetObj == null)
            return;

        if (periodicTimers.ContainsKey(targetObj))
            periodicTimers.Remove(targetObj);
    }

    private void RefreshDynamicStatIfNeeded()
    {
        if (!useDynamicStat)
            return;

        RefreshDynamicStat();
    }

    private void TryHitOnce(Collider2D other)
    {
        if (!CanHit(other))
            return;

        GameObject targetObj = GetTargetObject(other);

        if (targetObj == null)
            return;

        if (hitObjects.Contains(targetObj))
            return;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);
        hitObjects.Add(targetObj);
    }

    private void TryHitAlways(Collider2D other)
    {
        if (!CanHit(other))
            return;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);
    }

    private void TryHitPeriodicEnter(Collider2D other)
    {
        if (!CanHit(other))
            return;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        GameObject targetObj = GetTargetObject(other);

        if (targetObj == null)
            return;

        if (!periodicTimers.ContainsKey(targetObj))
            periodicTimers.Add(targetObj, 0f);

        enemy.TakeDamage(damage);
    }

    private void TryHitPeriodicStay(Collider2D other)
    {
        if (!CanHit(other))
            return;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        GameObject targetObj = GetTargetObject(other);

        if (targetObj == null)
            return;

        if (!periodicTimers.ContainsKey(targetObj))
            periodicTimers.Add(targetObj, 0f);

        periodicTimers[targetObj] += Time.deltaTime;

        if (periodicTimers[targetObj] < damageInterval)
            return;

        periodicTimers[targetObj] = 0f;

        enemy.TakeDamage(damage);
    }

    private bool CanHit(Collider2D other)
    {
        if (other == null)
            return false;

        if (owner != null && other.gameObject == owner)
            return false;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return false;

        return true;
    }

    private Enemy GetEnemy(Collider2D other)
    {
        if (other == null)
            return null;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            enemy = other.GetComponentInParent<Enemy>();

        return enemy;
    }

    private GameObject GetTargetObject(Collider2D other)
    {
        Enemy enemy = GetEnemy(other);

        if (enemy != null)
            return enemy.gameObject;

        return other.gameObject;
    }
}