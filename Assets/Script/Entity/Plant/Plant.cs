using UnityEngine;

public class Plant : HealthActor
{
    [Header("PlantData")]
    public PlantUI plantUI;
    public PlantData plantData;

    [Header("Manager")]
    public GameManager gameManager;
    public GrowManager growManager;
    public TimerManager timerManager;
    public BuffManager buffManager;

    [Tooltip("끄면 식물 피해 및 식물 체력에 의한 패배를 사용하지 않습니다.")]
    public bool useHealthDefeat = true;

    public void Init(PlantData plantData)
    {
        if (plantData == null)
            return;
        this.plantData = plantData;
        if (plantUI != null)
            plantUI.SetPlantData(plantData);

        InitHealthOwner("PlantHealth");
        if (health != null)
        {
            if (!useHealthDefeat) health.damageImmune = true;
            health.SetBuffManager(buffManager != null ? buffManager : BuffManager.instance);
        }
        Revive(plantData.maxHP, true);

        if(timerManager != null)
            timerManager.RestartTimer();

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
    public override void TakeDamage(float damage)
    {
        if (useHealthDefeat) base.TakeDamage(damage);
    }

    protected override void OnDeathStarted()
    {
        if (!useHealthDefeat) return;
        if (growManager != null)
            growManager.StopGrowth();
        if (gameManager != null)
            gameManager.GameOver();
    }

    protected override void OnDeathFinished() { }
    #endregion
}
