using System.Collections.Generic;
using UnityEngine;

public enum DamageApplyMode
{
    HitOnce,
    EveryEnter,
    Periodic
}

public class DamageArea : MonoBehaviour, IDynamicBuffReceiver
{
    private static readonly List<DamageArea> activeAreas =
        new List<DamageArea>();

    [Header("Componnet")]
    public CircleCollider2D circleCollider;
    public Transform rangeVisual;

    [Header("Refference")]
    public float lifeTime = 0.2f;
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Min(0.01f)]
    public float damageInterval = 0.5f;

    public GameObject owner;

    private float damage = 10f;
    private float radius = 1f;
    private float timer;
    private bool useSnapshotAndDynamicBuff;

    private readonly HashSet<GameObject> hitObjects =
        new HashSet<GameObject>();

    private readonly Dictionary<GameObject, float> periodicTimers =
        new Dictionary<GameObject, float>();

    private AttackStat snapshotAttackStat;
    private ItemData sourceItemData;
    private EquipmentBag sourceBag;
    private BuffManager buffManager;

    private void Awake()
    {
        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (rangeVisual == null)
        {
            SpriteRenderer spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                rangeVisual = spriteRenderer.transform;
        }

        ApplyRadius();
    }

    private void OnEnable()
    {
        if (!activeAreas.Contains(this))
            activeAreas.Add(this);
    }

    private void OnDisable()
    {
        activeAreas.Remove(this);
        UnregisterDynamicBuffReceiver();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    #region Init

    public void InitWithSnapshotAndDynamicBuff(
        AttackStat snapshotAttackStat,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        BuffManager buffManager,
        GameObject owner
    )
    {
        UnregisterDynamicBuffReceiver();

        useSnapshotAndDynamicBuff = true;

        this.snapshotAttackStat = snapshotAttackStat;
        this.sourceItemData = sourceItemData;
        this.sourceBag = sourceBag;
        this.buffManager = buffManager;
        this.owner = owner;

        timer = 0f;
        hitObjects.Clear();
        periodicTimers.Clear();

        RegisterDynamicBuffReceiver();

        OnDynamicBuffChanged();
        ApplyRadius();
    }

    private void RegisterDynamicBuffReceiver()
    {
        if (buffManager == null)
            return;

        buffManager.RegisterDynamicBuffReceiver(this);
    }

    private void UnregisterDynamicBuffReceiver()
    {
        if (buffManager == null)
            return;

        buffManager.UnregisterDynamicBuffReceiver(this);
    }

    public void OnDynamicBuffChanged()
    {
        if (!useSnapshotAndDynamicBuff)
            return;

        RefreshStatFromSnapshotAndDynamicBuff();
    }

    private void RefreshStatFromSnapshotAndDynamicBuff()
    {
        if (snapshotAttackStat == null)
            return;

        AttackStat currentStat = snapshotAttackStat;

        if (buffManager != null)
        {
            AttackStat dynamicBuffedStat =
                buffManager.GetDynamicAttackStat(
                    snapshotAttackStat,
                    sourceItemData,
                    sourceBag
                );

            if (dynamicBuffedStat != null)
                currentStat = dynamicBuffedStat;
        }

        damage = currentStat.attackPower;
        radius = Mathf.Max(0.01f, currentStat.attackRange);
        lifeTime = Mathf.Max(0.01f, currentStat.attackLifeTime);
        damageInterval = Mathf.Max(0.01f, currentStat.damageInterval);

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

    public static void ClearAllActiveAreas()
    {
        for (int i = activeAreas.Count - 1; i >= 0; i--)
        {
            DamageArea area = activeAreas[i];

            if (area == null)
            {
                activeAreas.RemoveAt(i);
                continue;
            }

            if (area.circleCollider != null)
                area.circleCollider.enabled = false;

            area.UnregisterDynamicBuffReceiver();

            area.gameObject.SetActive(false);
            Destroy(area.gameObject);
        }

        activeAreas.Clear();
    }

    #endregion

    #region SetAttackMode

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageApplyMode == DamageApplyMode.HitOnce)
            TryHitOnce(other);
        else if (damageApplyMode == DamageApplyMode.EveryEnter)
            TryHitAlways(other);
        else if (damageApplyMode == DamageApplyMode.Periodic)
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
}