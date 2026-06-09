using System.Collections.Generic;
using UnityEngine;

public enum SummonThrowTargetMode
{
    NearestEnemy,
    RandomEnemy,
    RandomPosition
}

public class SummonItemThrower : AttackObject<SummonStat>, IBuffTarget
{
    private static readonly List<SummonItemThrower> activeThrowers =
        new List<SummonItemThrower>();

    [Header("Component")]
    public CircleCollider2D rangeCollider;
    public Transform rangeVisual;

    [Header("Throw")]
    public ItemThrowExecutor itemThrowExecutor;
    public ItemData itemDatas;
    public SummonThrowTargetMode targetMode = SummonThrowTargetMode.NearestEnemy;

    [Header("Detect")]
    public LayerMask enemyLayerMask;

    [Header("Runtime Stat")]
    [SerializeField] private float summonAttackPower = 0f;
    [SerializeField] private float summonAttackRange = 5f;
    [SerializeField] private float summonThrowInterval = 1f;
    [SerializeField] private float lifeTime = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    [SerializeField] private bool showWarningLog = true;
    [SerializeField] private Enemy currentTarget;
    [SerializeField] private int detectedEnemyCount;
    [SerializeField] private List<Enemy> detectedEnemies = new List<Enemy>();

    private float timer;
    private float throwTimer;
    private bool isRegisteredBuffTarget;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Summon";
    public string BuffTargetDebugName => name;

    protected virtual void Awake()
    {
        if (itemThrowExecutor == null)
            itemThrowExecutor = GetComponent<ItemThrowExecutor>();

        if (rangeCollider == null)
            rangeCollider = GetComponent<CircleCollider2D>();

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

        if (!activeThrowers.Contains(this))
            activeThrowers.Add(this);

        timer = 0f;
        throwTimer = 0f;
        currentTarget = null;

        detectedEnemies.Clear();
        detectedEnemyCount = 0;

        ApplyRadius();

        if (buffManager != null)
            RegisterBuffTargetToManager();
    }

    protected override void OnDisable()
    {
        UnregisterBuffTargetFromManager();

        activeThrowers.Remove(this);

        currentTarget = null;
        detectedEnemies.Clear();
        detectedEnemyCount = 0;

        base.OnDisable();
    }

    private void Update()
    {
        UpdateLifeTime();
        UpdateThrow();

        detectedEnemyCount = detectedEnemies.Count;
    }

    private void UpdateLifeTime()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    private void UpdateThrow()
    {
        throwTimer += Time.deltaTime;

        if (throwTimer < summonThrowInterval)
            return;

        throwTimer = 0f;
        TryThrow();
    }

    public override void InitWithSnapshotAndDynamicBuff(
        SummonStat snapshotAttackStat,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        BuffManager buffManager,
        GameObject owner
    )
    {
        UnregisterBuffTargetFromManager();

        timer = 0f;
        throwTimer = 0f;
        currentTarget = null;

        detectedEnemies.Clear();
        detectedEnemyCount = 0;

        base.InitWithSnapshotAndDynamicBuff(
            snapshotAttackStat,
            sourceItemData,
            sourceBag,
            buffManager,
            owner
        );

        RegisterBuffTargetToManager();

        ApplyRadius();
        RefreshEnemiesInsideRange();
    }

    protected override void RefreshStatFromSnapshotAndDynamicBuff()
    {
        if (snapshotAttackStat == null)
            return;

        SummonStat currentStat = snapshotAttackStat;

        if (buffManager != null)
        {
            SummonStat itemBuffedStat = buffManager.GetBuffedStatForItem(
                snapshotAttackStat,
                sourceItemData,
                sourceBag,
                BuffCalculationMode.DynamicOnly
            );

            if (itemBuffedStat != null)
                currentStat = itemBuffedStat;

            SummonStat targetBuffedStat = buffManager.GetBuffedStatForTarget(
                currentStat,
                this
            );

            if (targetBuffedStat != null)
                currentStat = targetBuffedStat;
        }

        ApplyStat(currentStat);
    }

    public void RefreshBuffedStat()
    {
        RefreshStatFromSnapshotAndDynamicBuff();
    }

    private void RegisterBuffTargetToManager()
    {
        if (isRegisteredBuffTarget)
            return;

        if (buffManager == null)
            return;

        buffManager.RegisterBuffTarget(this);
        isRegisteredBuffTarget = true;
    }

    private void UnregisterBuffTargetFromManager()
    {
        if (!isRegisteredBuffTarget)
            return;

        if (buffManager == null)
            return;

        buffManager.UnregisterBuffTarget(this);
        isRegisteredBuffTarget = false;
    }

    protected override void ApplyStat(SummonStat currentStat)
    {
        if (currentStat == null)
            return;

        summonAttackPower = currentStat.summonAttackPower;
        summonAttackRange = Mathf.Max(0.01f, currentStat.summonAttackRange);
        summonThrowInterval = Mathf.Max(0.01f, currentStat.summonThrowInterval);
        lifeTime = Mathf.Max(0.01f, currentStat.summonLifeTime);

        ApplyRadius();
    }

    private void ApplyRadius()
    {
        summonAttackRange = Mathf.Max(0.01f, summonAttackRange);

        if (rangeVisual != null)
        {
            rangeVisual.localScale = new Vector3(
                summonAttackRange * 2f,
                summonAttackRange * 2f,
                1f
            );
        }

        if (rangeCollider == null)
            rangeCollider = GetComponent<CircleCollider2D>();

        if (rangeCollider != null)
        {
            rangeCollider.radius = summonAttackRange;
            rangeCollider.isTrigger = true;
            rangeCollider.enabled = true;
        }

        transform.localScale = Vector3.one;
    }

    #region Trigger Detect

    private void OnTriggerEnter2D(Collider2D other)
    {
        Log(
            $"[SummonItemThrower] Trigger Enter: {other.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}"
        );

        Enemy enemy = GetEnemy(other);

        if (enemy == null)
        {
            Log($"[SummonItemThrower] Enemy component not found: {other.name}");
            return;
        }

        if (!IsEnemyLayer(other.gameObject) && !IsEnemyLayer(enemy.gameObject))
        {
            Log($"[SummonItemThrower] Layer blocked: {other.name}");
            return;
        }

        AddEnemy(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Enemy enemy = GetEnemy(other);

        if (enemy == null)
            return;

        RemoveEnemy(enemy);
    }

    private void AddEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (detectedEnemies.Contains(enemy))
            return;

        detectedEnemies.Add(enemy);
        detectedEnemyCount = detectedEnemies.Count;

        Log($"[SummonItemThrower] Enemy Added: {enemy.name} / Count: {detectedEnemies.Count}");
    }

    private void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!detectedEnemies.Contains(enemy))
            return;

        detectedEnemies.Remove(enemy);
        detectedEnemyCount = detectedEnemies.Count;

        Log($"[SummonItemThrower] Enemy Removed: {enemy.name} / Count: {detectedEnemies.Count}");

        if (currentTarget == enemy)
            currentTarget = null;
    }

    private bool IsEnemyLayer(GameObject obj)
    {
        if (obj == null)
            return false;

        return (enemyLayerMask.value & (1 << obj.layer)) != 0;
    }

    #endregion

    #region Initial Scan

    private void RefreshEnemiesInsideRange()
    {
        detectedEnemies.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            summonAttackRange,
            enemyLayerMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = GetEnemy(hits[i]);

            if (enemy == null)
                continue;

            if (detectedEnemies.Contains(enemy))
                continue;

            detectedEnemies.Add(enemy);
        }

        detectedEnemyCount = detectedEnemies.Count;
        currentTarget = null;

        Log($"[SummonItemThrower] Initial Scan / Count: {detectedEnemies.Count}");
    }

    #endregion

    #region Target / Throw Decision

    private void TryThrow()
    {
        CleanInvalidEnemies();

        if (targetMode == SummonThrowTargetMode.RandomPosition)
        {
            ThrowToRandomPosition();
            return;
        }

        currentTarget = SelectTargetEnemy();

        string targetName = currentTarget != null ? currentTarget.name : "null";
        Log($"[SummonItemThrower] TryThrow / Count: {detectedEnemies.Count} / Target: {targetName}");

        if (currentTarget == null)
            return;

        ThrowToEnemy(currentTarget);
    }

    private Enemy SelectTargetEnemy()
    {
        CleanInvalidEnemies();

        if (detectedEnemies.Count <= 0)
            return null;

        if (targetMode == SummonThrowTargetMode.NearestEnemy)
            return GetNearestEnemy();

        if (targetMode == SummonThrowTargetMode.RandomEnemy)
            return GetRandomEnemy();

        return null;
    }

    private Enemy GetNearestEnemy()
    {
        Enemy nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            Enemy enemy = detectedEnemies[i];

            if (enemy == null)
                continue;

            float distanceSqr =
                (enemy.transform.position - currentPosition).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearestEnemy = enemy;
        }

        return nearestEnemy;
    }

    private Enemy GetRandomEnemy()
    {
        CleanInvalidEnemies();

        if (detectedEnemies.Count <= 0)
            return null;

        int index = Random.Range(0, detectedEnemies.Count);
        return detectedEnemies[index];
    }

    private Vector3 GetRandomPositionInRange()
    {
        Vector2 randomCircle = Random.insideUnitCircle * summonAttackRange;

        Vector3 randomPosition = transform.position + new Vector3(
            randomCircle.x,
            randomCircle.y,
            0f
        );

        randomPosition.z = transform.position.z;
        return randomPosition;
    }

    private void CleanInvalidEnemies()
    {
        for (int i = detectedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = detectedEnemies[i];

            if (enemy == null)
            {
                detectedEnemies.RemoveAt(i);
                continue;
            }

            if (!enemy.gameObject.activeInHierarchy)
            {
                detectedEnemies.RemoveAt(i);
                continue;
            }
        }

        if (currentTarget != null && !detectedEnemies.Contains(currentTarget))
            currentTarget = null;

        detectedEnemyCount = detectedEnemies.Count;
    }

    #endregion

    #region Throw

    private void ThrowToEnemy(Enemy targetEnemy)
    {
        if (targetEnemy == null)
            return;

        Vector3 targetPosition = targetEnemy.transform.position;
        ThrowToPosition(targetPosition, targetEnemy.name);
    }

    private void ThrowToRandomPosition()
    {
        Vector3 targetPosition = GetRandomPositionInRange();
        ThrowToPosition(targetPosition, "Random Position");
    }

    private void ThrowToPosition(Vector3 targetPosition, string debugTargetName)
    {
        if (itemThrowExecutor == null)
        {
            Warning("[SummonItemThrower] itemThrowExecutor is null.");
            return;
        }

        if (itemDatas == null)
        {
            Warning("[SummonItemThrower] itemDatas is null.");
            return;
        }

        Vector3 startPosition = transform.position;
        targetPosition.z = startPosition.z;

        Log($"[SummonItemThrower] Throw: {itemDatas.name} -> {debugTargetName}");

        itemThrowExecutor.Throw(
            itemDatas,
            startPosition,
            targetPosition,
            owner,
            null,
            0
        );
    }

    #endregion

    #region GetObject

    private Enemy GetEnemy(Collider2D col)
    {
        if (col == null)
            return null;

        Enemy enemy = col.GetComponent<Enemy>();

        if (enemy == null)
            enemy = col.GetComponentInParent<Enemy>();

        return enemy;
    }

    #endregion

    #region Debug

    private void Log(string message)
    {
        if (!showDebugLog)
            return;

        Debug.Log(message);
    }

    private void Warning(string message)
    {
        if (!showWarningLog)
            return;

        Debug.LogWarning(message);
    }

    #endregion

    #region Clear

    public static void ClearAllActiveThrowers()
    {
        for (int i = activeThrowers.Count - 1; i >= 0; i--)
        {
            SummonItemThrower thrower = activeThrowers[i];

            if (thrower == null)
            {
                activeThrowers.RemoveAt(i);
                continue;
            }

            thrower.Clear();
        }

        activeThrowers.Clear();
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, summonAttackRange);
    }
#endif
}