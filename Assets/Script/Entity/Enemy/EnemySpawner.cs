using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setting")]
    public GameObject enemyPrefab;
    public float spawnRate = 1.5f;
    public float spawnDistance = 8f;

    [Header("Managers")]
    public BuffManager buffManager;

    private Plant targetPlant;

    public void SetSpawner(EnemySpawnInfo info, Plant plant)
    {
        enemyPrefab = info.enemyPrefab;
        targetPlant = plant;

        StopSpawning();

        InvokeRepeating(nameof(SpawnEnemy), 1f, info.spawnWeight);
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnEnemy));
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
            return;

        if (targetPlant == null)
            return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 spawnPos = dir * spawnDistance;

        GameObject spawnedEnemy = Instantiate(
            enemyPrefab,
            spawnPos,
            Quaternion.identity
        );

        Enemy enemy = spawnedEnemy.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.Init(
                targetPlant,
                buffManager
            );
        }

        if (EnemyManager.instance != null)
            EnemyManager.instance.RegisterEnemy(spawnedEnemy);
    }
}