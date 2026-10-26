using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Manager")]
    public EnemyManager enemyManager;
    public PlantManager plantManager;
    public GameEndManager endManager;

    public void Victory()
    {
        if (plantManager == null || plantManager.CurrentPlant == null)
        {
            Debug.LogWarning("CurrentPlant is null");
            GameOver();
            return;
        }

        if (plantManager.CurrentPlant.reward != null && GameStatisticsManager.Instance != null)
            foreach (var reward in plantManager.CurrentPlant.reward)
                if (reward != null)
                    GameStatisticsManager.Instance.AddCurrency(reward.currencyType, reward.amount);

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
            CompleteGame();
        }
    }

    public void GameOver() => FinishGame(GameEndType.Lose);
    public void CompleteGame() => FinishGame(GameEndType.Win);

    private void FinishGame(GameEndType endType)
    {
        if (GrowManager.instance != null)
            GrowManager.instance.StopGrowth();

        if (enemyManager != null)
            enemyManager.AllStop();

        var result = endManager != null ? endManager : GameEndManager.instance;
        if (result != null) result.EndGame(endType);
    }
}
