using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager instance;

    [Header("Target")]
    public Plant Target;

    [Header("Normal Spawners")]
    public EnemySpawner[] enemySpawners;

    [Header("Special Spawners")]
    [FormerlySerializedAs("middleBossSpanwer")]
    public EnemySpawner middleBossSpawner;

    [FormerlySerializedAs("bossSpanwer")]
    public EnemySpawner bossSpawner;

    [Header("Spawn Limit")]
    public int maxAliveEnemyCount = 50;

    [Tooltip("켜두면 미들보스/보스는 일반 적 최대 생존 수 제한을 무시하고 스폰됩니다.")]
    public bool specialSpawnerIgnoresMaxAliveCount = true;

    [Header("Debug")]
    public bool logSpawnTime = true;
    [SerializeField] private int currentAliveEnemyCount;

    private readonly List<Enemy> currentEnemies = new List<Enemy>();

    private float spawnStartTime;
    private bool allEnemiesActionDisabled = false;

    private const int MiddleBossSpawnerIndex = -100;
    private const int BossSpawnerIndex = -200;

    private void Awake()
    {
        instance = this;
    }

    public void Init(PlantData plantData)
    {
        StopAllSpawners();
        KillAllEnemies();

        allEnemiesActionDisabled = false;
        spawnStartTime = Time.time;

        if (Target == null)
            return;

        if (plantData == null)
            return;

        SetupNormalSpawners(plantData);
        SetupSpecialSpawner(middleBossSpawner, plantData.middleBossEnemies, MiddleBossSpawnerIndex);
        SetupSpecialSpawner(bossSpawner, plantData.bossEnemies, BossSpawnerIndex);

        if (logSpawnTime)
            Debug.Log("Enemy Spawn Init Time: 0.00초");

        EnableAllEnemiesAction();
        RefreshEnemyCount();
    }

    private void SetupNormalSpawners(PlantData plantData)
    {
        if (enemySpawners == null || enemySpawners.Length == 0)
            return;

        bool hasNormalEnemies = HasValidSpawnInfos(plantData.normalEnemies);

        int useSpawnerCount = hasNormalEnemies
            ? Mathf.Clamp(plantData.spawnNormalEnemyCount, 0, enemySpawners.Length)
            : 0;

        for (int i = 0; i < enemySpawners.Length; i++)
        {
            if (enemySpawners[i] == null)
                continue;

            enemySpawners[i].StopSpawning();

            bool useSpawner = i < useSpawnerCount;
            enemySpawners[i].gameObject.SetActive(useSpawner);
        }

        for (int i = 0; i < useSpawnerCount; i++)
        {
            if (enemySpawners[i] == null)
                continue;

            enemySpawners[i].gameObject.SetActive(true);
            enemySpawners[i].SetSpawner(
                plantData.normalEnemies,
                Target,
                i,
                spawnStartTime
            );
        }
    }

    private void SetupSpecialSpawner(EnemySpawner spawner, EnemySpawnInfo spawnInfo, int spawnerIndex)
    {
        if (spawner == null)
            return;

        spawner.StopSpawning();

        bool hasSpawnInfo = HasValidSpawnInfo(spawnInfo);
        spawner.gameObject.SetActive(hasSpawnInfo);

        if (!hasSpawnInfo)
            return;

        spawner.overSpawn = specialSpawnerIgnoresMaxAliveCount;

        spawner.SetSpawner(
            new EnemySpawnInfo[] { spawnInfo },
            Target,
            spawnerIndex,
            spawnStartTime
        );
    }

    private bool HasValidSpawnInfos(EnemySpawnInfo[] infos)
    {
        if (infos == null || infos.Length == 0)
            return false;

        for (int i = 0; i < infos.Length; i++)
        {
            if (HasValidSpawnInfo(infos[i]))
                return true;
        }

        return false;
    }

    private bool HasValidSpawnInfo(EnemySpawnInfo info)
    {
        return info != null && info.enemyPrefab != null;
    }

    public bool CanSpawnMoreEnemies()
    {
        RefreshEnemyCount();

        if (maxAliveEnemyCount <= 0)
            return false;

        return currentAliveEnemyCount < maxAliveEnemyCount;
    }

    public void RefreshEnemyCount()
    {
        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = currentEnemies[i];

            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                currentEnemies.RemoveAt(i);
        }

        currentAliveEnemyCount = currentEnemies.Count;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!currentEnemies.Contains(enemy))
            currentEnemies.Add(enemy);

        if (allEnemiesActionDisabled)
            enemy.DisableAction();

        RefreshEnemyCount();
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        currentEnemies.Remove(enemy);
        RefreshEnemyCount();
    }

    public void AllStop()
    {
        DisableAllEnemiesAction();
        StopAllSpawners();
    }

    public void AllStart()
    {
        EnableAllEnemiesAction();
        StartAllSpawners();
    }

    public void StopAllSpawners()
    {
        if (enemySpawners != null)
        {
            for (int i = 0; i < enemySpawners.Length; i++)
            {
                if (enemySpawners[i] == null)
                    continue;

                enemySpawners[i].StopSpawning();
            }
        }

        if (middleBossSpawner != null)
            middleBossSpawner.StopSpawning();

        if (bossSpawner != null)
            bossSpawner.StopSpawning();
    }

    public void StartAllSpawners()
    {
        if (enemySpawners != null)
        {
            for (int i = 0; i < enemySpawners.Length; i++)
            {
                StartSpawnerIfActive(enemySpawners[i]);
            }
        }

        StartSpawnerIfActive(middleBossSpawner);
        StartSpawnerIfActive(bossSpawner);
    }

    private void StartSpawnerIfActive(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        if (!spawner.gameObject.activeInHierarchy)
            return;

        spawner.StartSpawning();
    }

    public void DisableAllEnemiesAction()
    {
        allEnemiesActionDisabled = true;

        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = currentEnemies[i];

            if (enemy == null)
            {
                currentEnemies.RemoveAt(i);
                continue;
            }

            enemy.DisableAction();
        }

        RefreshEnemyCount();
    }

    public void EnableAllEnemiesAction()
    {
        allEnemiesActionDisabled = false;

        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = currentEnemies[i];

            if (enemy == null)
            {
                currentEnemies.RemoveAt(i);
                continue;
            }

            enemy.EnableAction();
        }

        RefreshEnemyCount();
    }

    public void KillAllEnemies()
    {
        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            if (currentEnemies[i] != null)
                Destroy(currentEnemies[i].gameObject);
        }

        currentEnemies.Clear();
        RefreshEnemyCount();
    }
}