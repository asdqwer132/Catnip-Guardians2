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

    private HitEffectData[] onHitEffects;
    private HitEffectApplyMode hitEffectApplyMode;
    private ItemEffectContext hitSourceContext;
    private readonly Dictionary<Enemy, int> hitEffectLifeIds =
        new Dictionary<Enemy, int>();

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
        hitEffectLifeIds.Clear();
        onHitEffects = null;
        hitSourceContext = null;

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
        hitEffectLifeIds.Clear();

        base.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat,
            sourceItemData,
            sourceBag,
            buffManager,
            owner
        );

        ApplyRadius();
    }

    public void InitHitEffects(
        HitEffectData[] effects,
        HitEffectApplyMode applyMode,
        ItemEffectContext context
    )
    {
        onHitEffects = effects != null ? (HitEffectData[])effects.Clone() : null;
        hitEffectApplyMode = applyMode;
        hitEffectLifeIds.Clear();

        // Executor의 Context는 여러 효과가 공유하므로 생성 시점의 값을 복사한다.
        hitSourceContext = context == null ? null : new ItemEffectContext(
            context.owner, context.sourceItemData, context.usePosition,
            context.targetPosition, context.sourceBag, context.currentEffectData,
            context.buffManager
        );
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

    #region Trigger

    protected virtual void OnTriggerEnter2D(Collider2D other)
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

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (damageApplyMode != DamageApplyMode.Periodic)
            return;

        TryHitPeriodicStay(other);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
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

        hitObjects.Add(targetObj);
        ApplyHit(enemy);
    }

    private void TryHitAlways(Collider2D other)
    {
        if (!CanHit(other))
            return;

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        ApplyHit(enemy);
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

        ApplyHit(enemy);
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

        ApplyHit(enemy);
    }

    private void ApplyHit(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled)
            return;

        // 피해 처리 중 풀 반환/재생성/부활이 일어나도 새 생명에 효과를 옮기지 않는다.
        int lifeId = enemy.HitEffectLifeId;
        HitEffectData[] effects = onHitEffects;
        ItemEffectContext sourceContext = hitSourceContext;
        enemy.TakeDamage(damage);

        if (!isActiveAndEnabled || enemy == null || !enemy.CanReceiveHitEffects ||
            enemy.HitEffectLifeId != lifeId || effects == null || effects.Length == 0)
            return;

        if (hitEffectApplyMode == HitEffectApplyMode.FirstHitOnly)
        {
            int appliedLifeId;
            if (hitEffectLifeIds.TryGetValue(enemy, out appliedLifeId) &&
                appliedLifeId == lifeId)
                return;

            // 확률 실패도 첫 명중 시도에 포함한다. 다음 주기에 다시 굴리지 않는다.
            hitEffectLifeIds[enemy] = lifeId;
        }

        HitEffectContext context = new HitEffectContext(enemy, sourceContext);
        for (int i = 0; i < effects.Length; i++)
        {
            if (!context.IsTargetValid || !isActiveAndEnabled)
                break;

            HitEffectData effect = effects[i];
            if (effect != null)
                effect.TryExecute(context);
        }
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

        return !enemy.IsDead && enemy.isActiveAndEnabled;
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
