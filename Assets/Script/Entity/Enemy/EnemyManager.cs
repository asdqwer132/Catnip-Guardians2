using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager instance;

    [Header("Target")]
    public Plant plant;

    [Header("Spawners")]
    public EnemySpawner[] enemySpawners;

    [Header("Enemy Limit")]
    public int maxAliveEnemyCount = 50;

    [Header("Debug")]
    public bool logSpawnTime = true;
    [SerializeField] private int currentAliveEnemyCount;

    private List<Enemy> currentEnemies = new List<Enemy>();

    private bool allEnemiesActionDisabled = false;
    private bool loggedThousandEnemies = false;
    private float spawnStartTime;

    public bool AllEnemiesActionDisabled => allEnemiesActionDisabled;
    public int CurrentAliveEnemyCount => currentAliveEnemyCount;
    public int MaxAliveEnemyCount => maxAliveEnemyCount;
    public float SpawnStartTime => spawnStartTime;

    private void Awake()
    {
        instance = this;
    }

    public void Init(PlantData plantData)
    {
        StopAllSpawners();
        KillAllEnemies();

        allEnemiesActionDisabled = false;
        loggedThousandEnemies = false;
        spawnStartTime = Time.time;

        if (plant == null)
        {
            Debug.LogWarning("EnemyManager에 Plant가 연결되지 않았습니다.");
            return;
        }

        if (plantData == null)
            return;

        if (plantData.enemies == null || plantData.enemies.Length == 0)
            return;

        if (enemySpawners == null || enemySpawners.Length == 0)
        {
            Debug.LogWarning("EnemySpawner가 없습니다.");
            return;
        }

        int useSpawnerCount = Mathf.Clamp(plantData.spawnCount, 0, enemySpawners.Length);

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
            enemySpawners[i].SetSpawner(plantData.enemies, plant, i, spawnStartTime);
        }

        if (logSpawnTime)
            Debug.Log("Enemy Spawn Init Time: 0.00초");

        AllStart();
        RefreshEnemyCount();
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
            {
                currentEnemies.RemoveAt(i);

                if (EnemyStatusManager.instance != null && enemy != null)
                    EnemyStatusManager.instance.RemoveEnemy(enemy);
            }
        }

        currentAliveEnemyCount = currentEnemies.Count;

        if (!loggedThousandEnemies && currentAliveEnemyCount >= 1000)
        {
            loggedThousandEnemies = true;
            Debug.Log("현재 적 수가 1000마리에 도달했습니다.");
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!currentEnemies.Contains(enemy))
            currentEnemies.Add(enemy);

        if (EnemyStatusManager.instance != null)
            EnemyStatusManager.instance.RegisterEnemy(enemy);

        if (allEnemiesActionDisabled)
            enemy.DisableAction();

        RefreshEnemyCount();
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        currentEnemies.Remove(enemy);

        if (EnemyStatusManager.instance != null)
            EnemyStatusManager.instance.RemoveEnemy(enemy);

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
        if (enemySpawners == null)
            return;

        for (int i = 0; i < enemySpawners.Length; i++)
        {
            if (enemySpawners[i] == null)
                continue;

            enemySpawners[i].StopSpawning();
        }
    }

    public void StartAllSpawners()
    {
        if (enemySpawners == null)
            return;

        for (int i = 0; i < enemySpawners.Length; i++)
        {
            if (enemySpawners[i] == null)
                continue;

            if (!enemySpawners[i].gameObject.activeInHierarchy)
                continue;

            enemySpawners[i].StartSpawning();
        }
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

        if (EnemyStatusManager.instance != null)
            EnemyStatusManager.instance.Clear();

        RefreshEnemyCount();
    }
}