using UnityEngine;

public class Plant : HealthActor
{
    [Header("PlantData")]
    public PlantUI plantUI;
    public PlantData plantData;

    [Header("Manager")]
    public GameManager gameManager;
    public GrowManager growManager;

    public void Init()
    {
        if (PlantManager.instance != null && PlantManager.instance.CurrentPlant != null)
            plantData = PlantManager.instance.CurrentPlant;


        if (plantData == null)
            return;

        if (plantUI != null)
            plantUI.SetPlantData(plantData);

        Revive(plantData.maxHP, true);

        if (growManager != null)
        {
            if (growManager.gameManager == null)
                growManager.gameManager = gameManager;

            growManager.Init(plantData);
        }
    }

    #region OnEvent
    protected override void OnDeathStarted()
    {
        if (growManager != null)
            growManager.StopGrowth();

        if (gameManager != null)
            gameManager.GameOver();
    }

    protected override void OnDeathFinished() { }
    #endregion
}