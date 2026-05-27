using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setting")]
    public EnemySpawnInfo[] enemyInfos;

    [Header("Base Stat")]
    public EnemySpawnerStat baseStat = new EnemySpawnerStat();

    [Header("Runtime Stat")]
    [SerializeField] private EnemySpawnerStat currentStat = new EnemySpawnerStat();

    [Header("Managers")]
    public BuffManager buffManager;

    private Plant targetPlant;
    private Coroutine spawnCoroutine;
    private bool isSpawning;
    private int spawnerIndex = -1;
    private float spawnStartTime;

    public EnemySpawnerStat CurrentStat => currentStat;

    private void OnDisable()
    {
        StopSpawning();

        if (buffManager != null)
            buffManager.UnregisterEnemySpawner(this);
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
            buffManager.RegisterEnemySpawner(this);

        RefreshBuffedStat();
        StartSpawning();
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        if (buffManager != null)
            currentStat = buffManager.GetBuffedEnemySpawnerStat(baseStat, this);
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

            yield return new WaitForSeconds(interval);

            SpawnEnemy(selectedInfo, interval);
        }
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
            EnemyManager.instance.RegisterEnemy(spawnedEnemy);

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
            elapsedTime.ToString("F2") + "초 경과 / " +
            enemyName + " 소환 / " +
            "사용된 SpawnRate: " + info.spawnRate.ToString("F2") + " / " +
            "실제 대기시간: " + usedInterval.ToString("F2")
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