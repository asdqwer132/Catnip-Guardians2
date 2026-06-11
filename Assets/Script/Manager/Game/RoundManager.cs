using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Manager")]
    public EnemyManager enemyManager;
    public PlantManager plantManager;


    [Header("UI")]
    public GameObject upgradePanel;

    public void Victory()
    {
        if ((CurrencyManager.instance == null)) Debug.LogWarning("null1");
        if ((plantManager.CurrentPlant == null)) Debug.LogWarning("null2");
        CurrencyManager.instance.AddCurrency(plantManager.CurrentPlant.reward);

        //º∫¿Â ±‚πÕ
        enemyManager.AllStop();

        plantManager.PlayGrown();

        Invoke(nameof(Next), 2);
        
    }
    public void Next()
    {
        if (plantManager.UpIndex())
        {
            enemyManager.KillAllEnemies();
            enemyManager.Init(plantManager.CurrentPlant);
            enemyManager.AllStart();
        }
        else
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        GrowManager.instance.StopGrowth();
        enemyManager.AllStop();
        ReadyUpgrade();
    }


    void ReadyUpgrade()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        CursorChanger.instance.SetCursor(CursorType.Default);
    }

    public void StartNextRound()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        CursorChanger.instance.SetCursor(CursorType.Attack);
    }
}
