using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Manager")]
    public EnemyManager enemyManager;
    public PlantManager plantManager;
    public GameEndManager endManager;

    public void Victory()
    {
        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager is null");
            return;
        }

        if (plantManager == null || plantManager.CurrentPlant == null)
        {
            Debug.LogWarning("CurrentPlant is null");
            GameOver();
            return;
        }

        CurrencyManager.instance.AddCurrency(plantManager.CurrentPlant.reward);

        if (enemyManager != null)
            enemyManager.AllStop();

        plantManager.PlayGrown();

        Invoke(nameof(Next), 2f);
    }

    private void Next()
    {
        if (plantManager == null)
        {
            GameOver();
            return;
        }

        if (plantManager.UpIndex())
        {
            if (enemyManager != null)
            {
                enemyManager.KillAllEnemies();
                enemyManager.Init(plantManager.CurrentPlant);
                enemyManager.AllStart();
            }
        }
        else
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        if (GrowManager.instance != null)
            GrowManager.instance.StopGrowth();

        if (enemyManager != null)
            enemyManager.AllStop();

        if (endManager != null)
            endManager.LoseGame();
    }
}