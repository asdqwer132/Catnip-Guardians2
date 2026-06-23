using UnityEngine;

public class Plant : HealthActor
{
    [Header("PlantData")]
    public PlantUI plantUI;
    public PlantData plantData;

    [Header("Manager")]
    public GameManager gameManager;
    public GrowManager growManager;
    public BuffManager buffManager;

    public void Init(PlantData plantData)
    {
        if (plantData == null)
            return;
        this.plantData = plantData;
        if (plantUI != null)
            plantUI.SetPlantData(plantData);

        InitHealthOwner("PlantHealth");
        Revive(plantData.maxHP, true);

        if (growManager != null)
        {
            if (growManager.gameManager == null)
                growManager.gameManager = gameManager;

            growManager.Init(plantData, plantUI);
            growManager.StartGrowth();
        }

        if(buffManager != null)
        {
            buffManager.RegisterBuffTarget(health);
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