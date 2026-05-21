using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager")]
    public InitManager initManager;
    public Plant plant;

    [Header("UI")]
    public GameObject upgradePanel;

    private bool isGameOver = false;
    private bool isUpgradeMode = false;

    public bool IsUpgradeMode => isUpgradeMode;

    private void Start()
    {
        initManager.FirstInit();
        StartNextRound();
    }

    public void Victory()
    {
        if (isGameOver) return;

        CurrencyManager.instance.AddCurrency(plant.plantData.reward);

        ReadyUpgrade();
    }

    public void GameOver()
    {
        if (isGameOver) return;

        ReadyUpgrade();
    }

    void ReadyUpgrade()
    {
        isGameOver = true;
        isUpgradeMode = true;

        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        CursorChanger.instance.SetCursor(CursorType.Default);

        if (EnemyManager.instance != null)
        {
            EnemyManager.instance.StopAllSpawners();
            EnemyManager.instance.KillAllEnemies();
        }

    }

    public void StartNextRound()
    {
        isGameOver = false;
        isUpgradeMode = false;

        initManager.InitAll();

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        CursorChanger.instance.SetCursor(CursorType.Attack);
    }
}