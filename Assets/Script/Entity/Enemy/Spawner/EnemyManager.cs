using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager instance;

    [Header("Target")]
    public Plant Target;

    [Header("Spawners")]
    public EnemySpawner[] enemySpawners;
    public EnemySpawner middleBossSpanwer;
    public EnemySpawner bossSpanwer;
    public int maxAliveEnemyCount = 50;

    [Header("Debug")]
    public bool logSpawnTime = true;
    [SerializeField] private int currentAliveEnemyCount;

    private List<Enemy> currentEnemies = new List<Enemy>();

    private float spawnStartTime;
    private bool allEnemiesActionDisabled = false;

    private void Awake() { instance = this; }

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
        if (plantData.enemies == null || plantData.enemies.Length == 0)
            return;
        if (enemySpawners == null || enemySpawners.Length == 0)
            return;

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
            enemySpawners[i].SetSpawner(plantData.enemies, Target, i, spawnStartTime);
        }

        if (logSpawnTime)
            Debug.Log("Enemy Spawn Init Time: 0.00ÃÊ");

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

        RefreshEnemyCount();
    }
}