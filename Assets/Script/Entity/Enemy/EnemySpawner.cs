using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IBuffTarget
{
    [Header("Setting")]
    public EnemySpawnInfo[] enemyInfos;
    public EnemySpawnerStat baseStat = new EnemySpawnerStat();

    [Header("Runtime Stat")]
    [SerializeField] private EnemySpawnerStat currentStat = new EnemySpawnerStat();

    [Header("Spawn Debug Only")]
    [SerializeField] private bool enableSpawnDebug = true;
    [SerializeField] private string debugNextEnemyName;
    [SerializeField] private float debugSpawnInterval;
    [SerializeField] private float debugSpawnRemainingTime;
    [SerializeField] private float debugSpawnProgress01;
    [SerializeField] private float debugSelectedSpawnRate;
    [SerializeField] private float debugSelectedSpawnWeight;

    [Header("Spawn Debug Display")]
    [SerializeField] private string debugSpawnIntervalText;
    [SerializeField] private string debugSpawnRemainingTimeText;

    [Header("Managers")]
    public BuffManager buffManager;

    private Plant targetPlant;
    private Coroutine spawnCoroutine;
    private Coroutine spawnDebugCoroutine;
    private bool isSpawning;
    private int spawnerIndex = -1;
    private float spawnStartTime;

    public EnemySpawnerStat CurrentStat => currentStat;

    public string DebugNextEnemyName => debugNextEnemyName;
    public float DebugSpawnInterval => debugSpawnInterval;
    public float DebugSpawnRemainingTime => debugSpawnRemainingTime;
    public float DebugSpawnProgress01 => debugSpawnProgress01;
    public float DebugSelectedSpawnRate => debugSelectedSpawnRate;
    public float DebugSelectedSpawnWeight => debugSelectedSpawnWeight;
    public string DebugSpawnIntervalText => debugSpawnIntervalText;
    public string DebugSpawnRemainingTimeText => debugSpawnRemainingTimeText;

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

    public void SetSpawner(EnemySpawnInfo[] infos, Plant plant, int index, float startTime, BuffManager manager)
    {
        enemyInfos = infos;
        targetPlant = plant;
        spawnerIndex = index;
        spawnStartTime = startTime;
        buffManager = manager;

        if (buffManager != null)
            buffManager.RegisterBuffTarget(this);

        RefreshBuffedStat();
        StartSpawning();
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

        if (enemyInfos == null || enemyInfos.Length == 0)
            return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
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
        while (isSpawning)
        {
            EnemySpawnInfo selectedInfo = GetRandomEnemySpawnInfo();

            if (selectedInfo == null)
            {
                yield return null;
                continue;
            }

            baseStat.spawnInterval = Mathf.Max(0.01f, selectedInfo.spawnRate);
            RefreshBuffedStat();

            float interval = currentStat != null
                ? currentStat.spawnInterval
                : selectedInfo.spawnRate;

            interval = Mathf.Max(0.01f, interval);

            StartSpawnDebugTimer(selectedInfo, interval);

            yield return new WaitForSeconds(interval);

            StopSpawnDebugTimer();
            SetSpawnDebugCompleted();

            SpawnEnemy(selectedInfo, interval);
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
        float startTime = Time.time;

        while (isSpawning)
        {
            float elapsed = Time.time - startTime;
            float remaining = Mathf.Max(0f, safeInterval - elapsed);

            debugSpawnRemainingTime = RoundToOneDecimal(remaining);
            debugSpawnRemainingTimeText = debugSpawnRemainingTime.ToString("F1") + "초";
            debugSpawnProgress01 = Mathf.Clamp01(elapsed / safeInterval);

            if (remaining <= 0f)
                break;

            yield return new WaitForSeconds(0.1f);
        }

        debugSpawnRemainingTime = 0f;
        debugSpawnRemainingTimeText = "0.0초";
        debugSpawnProgress01 = 1f;
    }

    private void SetSpawnDebug(EnemySpawnInfo info, float interval)
    {
        debugNextEnemyName = info != null && info.enemyPrefab != null
            ? info.enemyPrefab.name
            : "None";

        debugSpawnInterval = RoundToOneDecimal(interval);
        debugSpawnRemainingTime = RoundToOneDecimal(interval);
        debugSpawnProgress01 = 0f;

        debugSpawnIntervalText = debugSpawnInterval.ToString("F1") + "초";
        debugSpawnRemainingTimeText = debugSpawnRemainingTime.ToString("F1") + "초";

        debugSelectedSpawnRate = info != null ? info.spawnRate : 0f;
        debugSelectedSpawnWeight = info != null ? info.spawnWeight : 0f;
    }

    private void SetSpawnDebugCompleted()
    {
        if (!enableSpawnDebug)
            return;

        debugSpawnRemainingTime = 0f;
        debugSpawnRemainingTimeText = "0.0초";
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

        debugSpawnIntervalText = "0.0초";
        debugSpawnRemainingTimeText = "0.0초";
    }

    private float RoundToOneDecimal(float value)
    {
        return Mathf.Round(Mathf.Max(0f, value) * 10f) / 10f;
    }

    private void SpawnEnemy(EnemySpawnInfo info, float usedInterval)
    {
        if (info == null)
            return;

        if (info.enemyPrefab == null)
            return;

        if (targetPlant == null)
            return;

        if (EnemyManager.instance != null && !EnemyManager.instance.CanSpawnMoreEnemies())
            return;

        float distance = currentStat != null ? currentStat.spawnDistance : 8f;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 spawnPos = dir * distance;

        GameObject spawnedEnemy = CreateEnemyObject(info.enemyPrefab, spawnPos);

        if (spawnedEnemy == null)
            return;

        Enemy enemy = spawnedEnemy.GetComponent<Enemy>();

        if (enemy != null)
            enemy.Init(targetPlant, buffManager);

        if (EnemyManager.instance != null)
            EnemyManager.instance.RegisterEnemy(enemy);

        LogSpawnTime(info, usedInterval);
    }

    private void LogSpawnTime(EnemySpawnInfo info, float usedInterval)
    {
        if (EnemyManager.instance == null)
            return;

        if (!EnemyManager.instance.logSpawnTime)
            return;

        float elapsedTime = Time.time - spawnStartTime;

        string enemyName = info.enemyPrefab != null
            ? info.enemyPrefab.name
            : "None";

        Debug.Log(
            "[Spawner " + spawnerIndex + "] " +
            elapsedTime.ToString("F1") + "초 경과 / " +
            enemyName + " 소환 / " +
            "사용된 SpawnRate: " + info.spawnRate.ToString("F1") + " / " +
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

            if (info.enemyPrefab == null)
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

            if (info.enemyPrefab == null)
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

            if (info.enemyPrefab == null)
                continue;

            return info;
        }

        return null;
    }
}