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

    private List<GameObject> currentEnemies = new List<GameObject>();
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
            GameObject enemyObject = currentEnemies[i];

            if (enemyObject == null || !enemyObject.activeInHierarchy)
                currentEnemies.RemoveAt(i);
        }

        currentAliveEnemyCount = currentEnemies.Count;

        if (!loggedThousandEnemies && currentAliveEnemyCount >= 1000)
        {
            loggedThousandEnemies = true;
            Debug.Log("현재 적 수가 1000마리에 도달했습니다.");
        }
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        if (!currentEnemies.Contains(enemy))
            currentEnemies.Add(enemy);

        if (allEnemiesActionDisabled)
        {
            Enemy enemyComponent = enemy.GetComponent<Enemy>();

            if (enemyComponent != null)
                enemyComponent.DisableAction();
        }

        RefreshEnemyCount();
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        currentEnemies.Remove(enemy);
        RefreshEnemyCount();
    }

    public void DisableAllEnemiesAction()
    {
        allEnemiesActionDisabled = true;

        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObject = currentEnemies[i];

            if (enemyObject == null)
            {
                currentEnemies.RemoveAt(i);
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null)
                continue;

            enemy.DisableAction();
        }

        RefreshEnemyCount();
    }

    public void EnableAllEnemiesAction()
    {
        allEnemiesActionDisabled = false;

        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObject = currentEnemies[i];

            if (enemyObject == null)
            {
                currentEnemies.RemoveAt(i);
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null)
                continue;

            enemy.EnableAction();
        }

        RefreshEnemyCount();
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

    public void KillAllEnemies()
    {
        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            if (currentEnemies[i] != null)
                Destroy(currentEnemies[i]);
        }

        currentEnemies.Clear();
        RefreshEnemyCount();
    }
}