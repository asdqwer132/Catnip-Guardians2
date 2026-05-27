using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager instance;

    [Header("Target")]
    public Plant plant;

    [Header("Spawners")]
    public EnemySpawner[] enemySpawners;

    private List<GameObject> currentEnemies = new List<GameObject>();

    private bool allEnemiesActionDisabled = false;

    public bool AllEnemiesActionDisabled => allEnemiesActionDisabled;

    void Awake()
    {
        instance = this;
    }

    public void Init(EnemySpawnInfo[] infos)
    {
        StopAllSpawners();
        KillAllEnemies();

        allEnemiesActionDisabled = false;

        if (plant == null)
        {
            Debug.LogWarning("EnemyManager에 Plant가 연결되지 않았습니다.");
            return;
        }

        if (infos == null)
            return;

        for (int i = 0; i < infos.Length; i++)
        {
            if (i >= enemySpawners.Length)
            {
                Debug.LogWarning("EnemySpawner 수가 부족합니다.");
                return;
            }

            if (enemySpawners[i] == null)
                continue;

            enemySpawners[i].SetSpawner(infos[i], plant);
        }
    }

    #region Action Control

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
    }

    #endregion

    #region Clear

    public void StopAllSpawners()
    {
        if (enemySpawners == null)
            return;

        for (int i = 0; i < enemySpawners.Length; i++)
        {
            if (enemySpawners[i] != null)
            {
                enemySpawners[i].StopSpawning();
            }
        }
    }

    public void KillAllEnemies()
    {
        for (int i = currentEnemies.Count - 1; i >= 0; i--)
        {
            if (currentEnemies[i] != null)
            {
                Destroy(currentEnemies[i]);
            }
        }

        currentEnemies.Clear();
    }

    #endregion

    #region Register

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
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        if (currentEnemies.Contains(enemy))
            currentEnemies.Remove(enemy);
    }

    #endregion
}