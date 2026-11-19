using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemySpawnShape { Circle, Rectangle, RectanglePerimeter }

[BuffTargetGroups("EnemySpawner")]
public class EnemySpawner : MonoBehaviour, IBuffTarget
{
    [Header("Setting")]
    public GameObject enemyPrefab;
    public EnemySpawnInfo[] enemyInfos;
    public SpecialEnemySpawnInfo[] specialEnemyInfos;
    [SerializeField] private bool useSpecialEntries;
    [SerializeField] private SpecialEnemySpawnSettings specialSpawnSettings = new SpecialEnemySpawnSettings();
    public EnemySpawnerStat baseStat = new EnemySpawnerStat();
    [Tooltip("켜면 스포너 시작 시 첫 번째 적은 대기시간 없이 바로 소환됩니다.")]
    public bool spawnImmediatelyOnStart = false;
    public bool overSpawn = false;
    public bool isBoss = false;
    [Tooltip("Random seconds added to each buffed spawn interval. Each spawner samples independently every cycle. Immediate first spawns skip this wait.")]
    public Vector2 spawnIntervalOffsetRange;

    [Header("Spawn Area")]
    [Tooltip("Circle: circular perimeter / Rectangle: filled rectangle / Rectangle Perimeter: rectangular border.")]
    public EnemySpawnShape spawnShape;
    [Tooltip("월드 좌표 기준 영역 중심입니다. 사각형 크기는 Base Stat의 Spawn Width / Height로 설정합니다.")]
    public Vector2 spawnCenter;
    [Tooltip("Optional center transform. Spawn Center becomes an offset from this transform.")]
    public Transform spawnCenterTarget;
    public Vector2 SpawnWorldCenter => spawnCenterTarget != null ? (Vector2)spawnCenterTarget.position + spawnCenter : spawnCenter;

    [Header("Runtime Stat")]
    [SerializeField] private EnemySpawnerStat currentStat = new EnemySpawnerStat();

    [Header("Spawn Gizmo")]
    [SerializeField] private bool drawSpawnGizmo = true;
    [SerializeField] private bool drawSpawnGizmoOnlySelected = true;
    [SerializeField] private Color spawnGizmoColor = new Color(1f, 0.35f, 0.1f, 0.9f);
    [SerializeField, Min(8)] private int spawnGizmoSegments = 64;
    [SerializeField] private bool drawSpawnCenterPoint = true;
    [SerializeField] private bool drawSpawnerToCenterLine = true;

    [Header("Spawn Debug Only")]
    [SerializeField] private bool enableSpawnDebug = true;
    [SerializeField] private string debugNextEnemyName;
    [SerializeField] private float debugSpawnInterval;
    [SerializeField] private float debugSpawnRemainingTime;
    [SerializeField] private float debugSpawnProgress01;
    [SerializeField] private float debugSelectedSpawnRate;
    [SerializeField] private float debugSelectedSpawnWeight;

    [Header("Managers")]
    public BuffManager buffManager;
    public GameStatisticsManager statisticsManager;

    private Plant targetPlant;
    private Coroutine spawnCoroutine;
    private Coroutine spawnDebugCoroutine;
    private bool isSpawning;
    private EnemySpawnInfo pendingImmediateSpawn;
    private int spawnerIndex = -1;
    private float spawnStartTime;
    private EnemyManager specialEnemySource;
    private GameStatisticsManager killSource;
    private int killRoundVersion = -1;
    private int killBaseline;
    public int SpecialKillsSinceLastSpawn => GetSpecialKillCount();

    private bool IsSpawnClockStopped => Time.timeScale <= 0f || TimeStopRuntime.IsStopped(TimeStopTargets.EnemySpawning);

    public EnemySpawnerStat CurrentStat => currentStat;

    public string DebugNextEnemyName => debugNextEnemyName;
    public float DebugSpawnInterval => debugSpawnInterval;
    public float DebugSpawnRemainingTime => debugSpawnRemainingTime;
    public float DebugSpawnProgress01 => debugSpawnProgress01;
    public float DebugSelectedSpawnRate => debugSelectedSpawnRate;
    public float DebugSelectedSpawnWeight => debugSelectedSpawnWeight;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "EnemySpawner";
    public string BuffTargetDebugName => name;

    private void OnDisable()
    {
        StopSpawning();

        if (buffManager != null)
            buffManager.UnregisterBuffTarget(this);
    }

    public void SetSpawner(EnemySpawnInfo[] infos, Plant plant)
    {
        SetSpawner(infos, plant, -1, Time.time, buffManager);
    }

    public void SetSpawner(EnemySpawnInfo[] infos, Plant plant, int index, float startTime)
    {
        SetSpawner(infos, plant, index, startTime, buffManager);
    }

    public void SetSpawner(EnemySpawnInfo[] infos, Plant plant, int index, float startTime, float interval)
    {
        if (baseStat == null) baseStat = new EnemySpawnerStat();
        baseStat.spawnInterval = EffectStatUtility.Safe(interval, .05f, 100000f, 1.5f);
        SetSpawner(infos, plant, index, startTime, buffManager);
    }

    public void SetSpawner(EnemySpawnInfo[] infos, Plant plant, int index, float startTime, BuffManager manager)
    {
        enemyInfos = infos;
        specialEnemyInfos = null;
        useSpecialEntries = false;
        Configure(plant, index, startTime, manager);
        pendingImmediateSpawn = spawnImmediatelyOnStart ? GetRandomEnemySpawnInfo() : null;
        StartSpawning();
    }

    public void SetSpecialSpawner(SpecialEnemySpawnInfo[] infos, Plant plant, int index, float startTime)
        => SetSpecialSpawner(infos, plant, index, startTime, new SpecialEnemySpawnSettings());

    public void SetSpecialSpawner(SpecialEnemySpawnInfo[] infos, Plant plant, int index, float startTime,
        SpecialEnemySpawnSettings settings, EnemyManager enemySource = null)
    {
        StopSpawning();
        specialSpawnSettings = settings ?? new SpecialEnemySpawnSettings();
        specialEnemySource = enemySource;
        killSource = null;
        killRoundVersion = -1;
        killBaseline = 0;
        specialEnemyInfos = infos;
        enemyInfos = null;
        useSpecialEntries = true;
        pendingImmediateSpawn = null;
        if (baseStat == null) baseStat = new EnemySpawnerStat();
        if (specialSpawnSettings.selection != SpecialEnemySpawnSelection.IndependentIntervals)
            baseStat.spawnInterval = EffectStatUtility.Safe(specialSpawnSettings.spawnInterval, .05f, 100000f, 1.5f);
        else if (infos != null)
            foreach (var info in infos)
                if (info != null && info.dataSet != null)
                {
                    baseStat.spawnInterval = EffectStatUtility.Safe(info.spawnRate, .05f, 100000f, 1.5f);
                    break;
                }
        Configure(plant, index, startTime, buffManager);
        StartSpawning();
    }

    private void Configure(Plant plant, int index, float startTime, BuffManager manager)
    {
        targetPlant = plant;
        spawnerIndex = index;
        spawnStartTime = startTime;
        buffManager = manager;

        if (buffManager != null)
            buffManager.RegisterBuffTarget(this);

        RefreshBuffedStat();
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        if (buffManager != null)
            currentStat = buffManager.GetBuffedStatForTarget(baseStat, this);
        else
            currentStat = baseStat.Clone();

        if (currentStat == null)
            currentStat = baseStat.Clone();

        currentStat.Clamp();
    }

    public void StartSpawning()
    {
        StopSpawning();

        if (useSpecialEntries ? specialEnemyInfos == null || specialEnemyInfos.Length == 0
            : enemyInfos == null || enemyInfos.Length == 0)
            return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(useSpecialEntries
            ? specialSpawnSettings.selection == SpecialEnemySpawnSelection.IndependentIntervals ? SpecialSpawnRoutine() : SpecialGroupRoutine()
            : SpawnRoutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;

        StopSpawnDebugTimer();
        ClearSpawnDebug();

        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        if (pendingImmediateSpawn != null)
        {
            while (isSpawning && IsSpawnClockStopped) yield return null;
            if (!isSpawning) yield break;
            EnemySpawnInfo immediate = pendingImmediateSpawn;
            pendingImmediateSpawn = null;
            SpawnEnemy(immediate, 0f);
        }
        while (isSpawning)
        {
            EnemySpawnInfo selectedInfo = GetRandomEnemySpawnInfo();

            if (selectedInfo == null)
            {
                yield return null;
                continue;
            }

            float interval = GetNextSpawnInterval();

            StartSpawnDebugTimer(selectedInfo, interval);

            float remaining = interval;
            while (remaining > 0f || IsSpawnClockStopped)
            {
                yield return null;
                if (!IsSpawnClockStopped)
                    remaining -= Time.deltaTime;
            }

            StopSpawnDebugTimer();
            SetSpawnDebugCompleted();

            SpawnEnemy(selectedInfo, interval);
        }
    }

    public float GetNextSpawnInterval()
    {
        RefreshBuffedStat();
        return ApplyIntervalOffset(currentStat != null ? currentStat.spawnInterval : 1.5f);
    }

    private float GetSpecialInterval(SpecialEnemySpawnInfo info)
        => GetSpecialInterval(info.spawnRate);

    private float GetSpecialInterval(float baseInterval)
    {
        var stat = baseStat != null ? baseStat.Clone() : new EnemySpawnerStat();
        stat.spawnInterval = EffectStatUtility.Safe(baseInterval, .05f, 100000f, 1.5f);
        var buffed = buffManager != null ? buffManager.GetBuffedStatForTarget(stat, this) : stat;
        if (buffed == null) buffed = stat;
        buffed.Clamp();
        return ApplyIntervalOffset(buffed.spawnInterval);
    }

    private float ApplyIntervalOffset(float interval)
    {
        float first = EffectStatUtility.Safe(spawnIntervalOffsetRange.x, -100000f, 100000f, 0f);
        float second = EffectStatUtility.Safe(spawnIntervalOffsetRange.y, -100000f, 100000f, 0f);
        float offset = Random.Range(Mathf.Min(first, second), Mathf.Max(first, second));
        return Mathf.Max(.05f, interval + offset);
    }

    private sealed class SpecialTimer
    {
        public SpecialEnemySpawnInfo info;
        public float interval;
        public float remaining;
    }

    private IEnumerator SpecialSpawnRoutine()
    {
        var timers = new List<SpecialTimer>();
        foreach (var info in specialEnemyInfos)
        {
            if (info == null || info.dataSet == null) continue;
            float interval = spawnImmediatelyOnStart ? 0f : GetSpecialInterval(info);
            timers.Add(new SpecialTimer { info = info, interval = interval, remaining = interval });
        }
        while (isSpawning && timers.Count > 0)
        {
            if (!IsSpawnClockStopped)
            {
                foreach (var timer in timers)
                {
                    if (timer.remaining > 0f || !SpecialConditionsMet()) continue;
                    bool spawned = SpawnEnemy(timer.info, timer.interval);
                    if (spawned) ResetSpecialKillProgress();
                    if (!isSpawning) yield break;
                    if (!spawned) continue;
                    timer.interval = GetSpecialInterval(timer.info);
                    timer.remaining = timer.interval;
                }
            }
            if (enableSpawnDebug)
            {
                var next = timers[0];
                foreach (var timer in timers) if (timer.remaining < next.remaining) next = timer;
                SetSpawnDebug(next.info, next.interval);
                debugSelectedSpawnRate = next.info.spawnRate;
                debugSpawnRemainingTime = RoundToOneDecimal(Mathf.Max(0f, next.remaining));
                debugSpawnProgress01 = next.interval > 0f ? Mathf.Clamp01(1f - next.remaining / next.interval) : 1f;
            }
            yield return null;
            if (!IsSpawnClockStopped)
                foreach (var timer in timers) timer.remaining -= Time.deltaTime;
        }
    }

    private GameStatisticsManager SpecialStatistics => statisticsManager != null ? statisticsManager : GameStatisticsManager.Instance;

    private int GetSpecialKillCount()
    {
        var source = SpecialStatistics;
        if (source == null)
        {
            killSource = null; killRoundVersion = -1; killBaseline = 0;
            return 0;
        }
        if (killSource != source || killRoundVersion != source.RoundVersion || source.RoundKillCount < killBaseline)
        {
            killSource = source;
            killRoundVersion = source.RoundVersion;
            killBaseline = 0;
        }
        return Mathf.Max(0, source.RoundKillCount - killBaseline);
    }

    private void ResetSpecialKillProgress()
    {
        GetSpecialKillCount();
        if (killSource != null) killBaseline = killSource.RoundKillCount;
    }

    private bool SpecialConditionsMet()
    {
        var enemies = specialEnemySource != null ? specialEnemySource : EnemyManager.instance;
        return specialSpawnSettings.ConditionsMet(enemies != null ? enemies.CurrentAliveEnemyCount : 0,
            GetSpecialKillCount(), enemies != null, SpecialStatistics != null);
    }

    private SpecialEnemySpawnInfo PickWeightedSpecial()
    {
        double total = 0;
        foreach (var info in specialEnemyInfos)
            if (info != null && info.dataSet != null)
                total += EffectStatUtility.Safe(info.spawnWeight, 0f, 100000000f, 0f);
        if (total <= 0) return null;
        double roll = Random.value * total;
        SpecialEnemySpawnInfo last = null;
        foreach (var info in specialEnemyInfos)
        {
            if (info == null || info.dataSet == null) continue;
            float weight = EffectStatUtility.Safe(info.spawnWeight, 0f, 100000000f, 0f);
            if (weight <= 0) continue;
            last = info;
            roll -= weight;
            if (roll < 0) return info;
        }
        return last;
    }

    private IEnumerator SpecialGroupRoutine()
    {
        float interval = spawnImmediatelyOnStart ? 0f : GetSpecialInterval(specialSpawnSettings.spawnInterval);
        float remaining = interval;
        while (isSpawning)
        {
            if (!IsSpawnClockStopped && remaining <= 0f && SpecialConditionsMet())
            {
                bool spawned = false;
                if (specialSpawnSettings.selection == SpecialEnemySpawnSelection.WeightedOne)
                {
                    var selected = PickWeightedSpecial();
                    if (selected != null) spawned = SpawnEnemy(selected, interval);
                }
                else
                {
                    // Test the group's conditions once: its own summons must not break an all-at-once batch.
                    foreach (var info in specialEnemyInfos)
                    {
                        if (info == null || info.dataSet == null) continue;
                        spawned |= SpawnEnemy(info, interval);
                        if (!isSpawning) break;
                    }
                }
                if (spawned) ResetSpecialKillProgress();
                if (!isSpawning) yield break;
                if (spawned)
                {
                    interval = GetSpecialInterval(specialSpawnSettings.spawnInterval);
                    remaining = interval;
                }
            }
            if (enableSpawnDebug)
            {
                debugNextEnemyName = specialSpawnSettings.selection == SpecialEnemySpawnSelection.AllTogether ? "All Together" : "Weighted One";
                debugSpawnInterval = RoundToOneDecimal(interval);
                debugSpawnRemainingTime = RoundToOneDecimal(Mathf.Max(0f, remaining));
                debugSpawnProgress01 = interval > 0f ? Mathf.Clamp01(1f - remaining / interval) : 1f;
                debugSelectedSpawnRate = specialSpawnSettings.spawnInterval;
                debugSelectedSpawnWeight = 0;
            }
            yield return null;
            if (!IsSpawnClockStopped) remaining -= Time.deltaTime;
        }
    }

    private void StartSpawnDebugTimer(EnemySpawnInfo info, float interval)
    {
        if (!enableSpawnDebug)
            return;

        StopSpawnDebugTimer();

        SetSpawnDebug(info, interval);
        spawnDebugCoroutine = StartCoroutine(SpawnDebugTimerRoutine(interval));
    }

    private void StopSpawnDebugTimer()
    {
        if (spawnDebugCoroutine == null)
            return;

        StopCoroutine(spawnDebugCoroutine);
        spawnDebugCoroutine = null;
    }

    private IEnumerator SpawnDebugTimerRoutine(float interval)
    {
        float safeInterval = Mathf.Max(0.01f, interval);
        float elapsed = 0f;

        while (isSpawning)
        {
            float remaining = Mathf.Max(0f, safeInterval - elapsed);

            debugSpawnRemainingTime = RoundToOneDecimal(remaining);
            debugSpawnProgress01 = Mathf.Clamp01(elapsed / safeInterval);

            if (remaining <= 0f)
                break;

            yield return null;
            if (!IsSpawnClockStopped)
                elapsed += Time.deltaTime;
        }

        debugSpawnRemainingTime = 0f;
        debugSpawnProgress01 = 1f;
    }

    private void SetSpawnDebug(EnemySpawnData info, float interval)
    {
        debugNextEnemyName = info != null && info.dataSet != null
            ? info.dataSet.name
            : "None";

        debugSpawnInterval = RoundToOneDecimal(interval);
        debugSpawnRemainingTime = RoundToOneDecimal(interval);
        debugSpawnProgress01 = 0f;


        debugSelectedSpawnRate = baseStat != null ? baseStat.spawnInterval : 0f;
        debugSelectedSpawnWeight = info is EnemySpawnInfo normal ? normal.spawnWeight : 0f;
    }

    private void SetSpawnDebugCompleted()
    {
        if (!enableSpawnDebug)
            return;

        debugSpawnRemainingTime = 0f;
        debugSpawnProgress01 = 1f;
    }

    private void ClearSpawnDebug()
    {
        debugNextEnemyName = "";
        debugSpawnInterval = 0f;
        debugSpawnRemainingTime = 0f;
        debugSpawnProgress01 = 0f;
        debugSelectedSpawnRate = 0f;
        debugSelectedSpawnWeight = 0f;

    }

    private float RoundToOneDecimal(float value)
    {
        return Mathf.Round(Mathf.Max(0f, value) * 10f) / 10f;
    }

    private bool SpawnEnemy(EnemySpawnData info, float usedInterval)
    {
        if (EnemyManager.instance != null && EnemyManager.instance.FieldLimitReached) return false;
        if (IsSpawnClockStopped || info == null || enemyPrefab == null)
            return false;

        if (info.dataSet == null)
            return false;

        if (targetPlant == null)
            return false;

        if (EnemyManager.instance != null && !EnemyManager.instance.CanSpawnMoreEnemies() && !overSpawn)
            return false;

        Vector2 spawnPos = GetRandomSpawnPosition();

        GameObject spawnedEnemy = CreateEnemyObject(enemyPrefab, spawnPos);

        if (spawnedEnemy == null)
            return false;

        Enemy enemy = spawnedEnemy.GetComponent<Enemy>();

        if (enemy != null)
            enemy.Init(targetPlant, buffManager, info.dataSet);

        if (EnemyManager.instance != null)
            EnemyManager.instance.RegisterEnemy(enemy);

        if (isBoss && OffscreenTargetIndicatorManager.Instance != null)
            OffscreenTargetIndicatorManager.Instance.ShowIndicator(spawnedEnemy);
        LogSpawnTime(info, usedInterval);
        return true;
    }

    private EnemySpawnerStat SpawnAreaStat => Application.isPlaying && currentStat != null ? currentStat : baseStat;
    public Vector2 SpawnRectangleSize => new Vector2(
        EffectStatUtility.Safe(SpawnAreaStat?.spawnWidth ?? 16f, 0.01f, 100000f, 16f),
        EffectStatUtility.Safe(SpawnAreaStat?.spawnHeight ?? 2f, 0.01f, 100000f, 2f));

    public Vector2 GetRandomSpawnPosition()
    {
        if (spawnShape == EnemySpawnShape.Circle)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return SpawnWorldCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * GetSpawnDistance();
        }

        Vector2 size = SpawnRectangleSize;
        if (spawnShape == EnemySpawnShape.RectanglePerimeter)
            return SpawnWorldCenter + EnemyRegionGeometry.RectangleBorderPoint(size, SpawnAreaStat?.spawnBorderThickness ?? 0f);
        Vector2 min = SpawnWorldCenter - size * .5f;
        Vector2 max = SpawnWorldCenter + size * .5f;
        return new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
    }

    private float GetSpawnDistance()
        => EffectStatUtility.Safe(SpawnAreaStat?.spawnDistance ?? 8f, 0.01f, 100000f, 8f);

    private void LogSpawnTime(EnemySpawnData info, float usedInterval)
    {
        if (EnemyManager.instance == null)
            return;

        if (!EnemyManager.instance.logSpawnTime)
            return;

        float elapsedTime = Time.time - spawnStartTime;

        string enemyName = info.dataSet != null
            ? info.dataSet.name
            : "None";

        Debug.Log(
            "[Spawner " + spawnerIndex + "] " +
            elapsedTime.ToString("F1") + "초 경과 / " +
            enemyName + " 소환 / " +
            "기본 Spawn Interval: " + (info is SpecialEnemySpawnInfo special ? special.spawnRate : baseStat != null ? baseStat.spawnInterval : 0f).ToString("F1") + " / " +
            "실제 대기시간: " + usedInterval.ToString("F1")
        );
    }

    private GameObject CreateEnemyObject(GameObject prefab, Vector2 spawnPos)
    {
        if (ObjectPoolManager.instance != null)
            return ObjectPoolManager.instance.Spawn(prefab, spawnPos, Quaternion.identity);

        return Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private EnemySpawnInfo GetRandomEnemySpawnInfo()
    {
        if (enemyInfos == null || enemyInfos.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < enemyInfos.Length; i++)
        {
            EnemySpawnInfo info = enemyInfos[i];

            if (info == null)
                continue;

            if (info.dataSet == null)
                continue;

            totalWeight += Mathf.Max(0f, info.spawnWeight);
        }

        if (totalWeight <= 0f)
            return GetFirstValidEnemySpawnInfo();

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < enemyInfos.Length; i++)
        {
            EnemySpawnInfo info = enemyInfos[i];

            if (info == null)
                continue;

            if (info.dataSet == null)
                continue;

            currentWeight += Mathf.Max(0f, info.spawnWeight);

            if (randomValue <= currentWeight)
                return info;
        }

        return GetFirstValidEnemySpawnInfo();
    }

    private EnemySpawnInfo GetFirstValidEnemySpawnInfo()
    {
        if (enemyInfos == null)
            return null;

        for (int i = 0; i < enemyInfos.Length; i++)
        {
            EnemySpawnInfo info = enemyInfos[i];

            if (info == null)
                continue;

            if (info.dataSet == null)
                continue;

            return info;
        }

        return null;
    }
    private void OnDrawGizmos()
    {
        if (drawSpawnGizmoOnlySelected)
            return;

        DrawSpawnGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnGizmoOnlySelected)
            return;

        DrawSpawnGizmo();
    }

    private void DrawSpawnGizmo()
    {
        if (!drawSpawnGizmo)
            return;

        Vector3 center = SpawnWorldCenter;
        float extent;

        Gizmos.color = spawnGizmoColor;

        if (spawnShape != EnemySpawnShape.Circle)
        {
            Vector2 size = SpawnRectangleSize;
            if (spawnShape == EnemySpawnShape.RectanglePerimeter)
            {
                EnemyRegionGeometry.RectangleHalfSizes(size, SpawnAreaStat?.spawnBorderThickness ?? 0f, out var inner, out var outer);
                Gizmos.DrawWireCube(center, inner * 2f);
                Gizmos.DrawWireCube(center, outer * 2f);
            }
            else Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0f));
            extent = Mathf.Max(size.x, size.y) * .5f;
        }
        else
        {
            extent = GetSpawnDistance();
            DrawWireCircle(center, extent, spawnGizmoSegments);
        }

        if (drawSpawnCenterPoint)
        {
            float centerSize = Mathf.Max(0.1f, extent * 0.03f);
            Gizmos.DrawSphere(center, centerSize);
        }

        if (drawSpawnerToCenterLine)
        {
            Gizmos.DrawLine(transform.position, center);
        }
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        segments = Mathf.Max(8, segments);

        float angleStep = Mathf.PI * 2f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;

            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
