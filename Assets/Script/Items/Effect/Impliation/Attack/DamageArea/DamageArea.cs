using System.Collections.Generic;
using UnityEngine;

public enum DamageApplyMode
{
    HitOnce,
    EveryEnter,
    Periodic
}

public class DamageArea : AttackObject<DamageAreaAttackStat>
{
    private static readonly List<DamageArea> activeDamageAreas =
        new List<DamageArea>();

    [Header("Component")]
    public CircleCollider2D circleCollider;
    public Transform rangeVisual;

    [Header("Damage")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Header("Runtime Stat")]
    [SerializeField] private float damage = 10f;

    [Min(0.01f)]
    [SerializeField] private float damageInterval = 0.5f;

    [SerializeField] private float radius = 1f;
    [SerializeField] private float lifeTime = 0.2f;

    private float timer;

    private readonly HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> periodicTimers =
        new Dictionary<GameObject, float>();

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
    }

    protected override void OnDisable()
    {
        activeDamageAreas.Remove(this);

        hitObjects.Clear();
        periodicTimers.Clear();

        base.OnDisable();
    }

    protected virtual void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    public override void InitWithSnapshotAndDynamicBuff(
        DamageAreaAttackStat snapshotAttackStat,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        BuffManager buffManager,
        GameObject owner
    )
    {
        timer = 0f;
        hitObjects.Clear();
        periodicTimers.Clear();

        base.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat,
            sourceItemData,
            sourceBag,
            buffManager,
            owner
        );

        ApplyRadius();
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

    private void ApplyRadius()
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

    #region Trigger

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageApplyMode == DamageApplyMode.HitOnce)
        {
            TryHitOnce(other);
            return;
        }

        if (damageApplyMode == DamageApplyMode.EveryEnter)
        {
            TryHitAlways(other);
            return;
        }

        if (damageApplyMode == DamageApplyMode.Periodic)
            TryHitPeriodicEnter(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageApplyMode != DamageApplyMode.Periodic)
            return;

        TryHitPeriodicStay(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject targetObj = GetTargetObject(other);

        if (targetObj == null)
            return;

        if (periodicTimers.ContainsKey(targetObj))
            periodicTimers.Remove(targetObj);
    }

    #endregion

    #region Attack

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

    #endregion

    #region GetObject

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

    #endregion

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