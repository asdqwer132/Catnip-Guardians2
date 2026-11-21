using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Manager")]
    public EnemyManager enemyManager;
    public BattlePlayerManager battlePlayerManager;
    public GameEndManager endManager;

    public void Victory()
    {
        var result = endManager != null ? endManager : GameEndManager.instance;
        if (result != null && result.IsGameEnded) return;

        if (battlePlayerManager == null || battlePlayerManager.CurrentPlant == null)
        {
            Debug.LogWarning("CurrentPlant is null");
            GameOver();
            return;
        }

        if (battlePlayerManager.CurrentPlant.reward != null && GameStatisticsManager.Instance != null)
            foreach (var reward in battlePlayerManager.CurrentPlant.reward)
                if (reward != null)
                    GameStatisticsManager.Instance.AddCurrency(reward.currencyType, reward.amount);

        CompleteGame();
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
