using UnityEngine;

public class Plant : HealthActor
{
    public PlantUI plantUI;

    public GameManager gameManager;
    public PlantData plantData;

    [Header("Growth")]
    public GrowManager growManager;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Init()
    {
        if (PlantManager.instance != null &&
            PlantManager.instance.CurrentPlant != null)
        {
            plantData = PlantManager.instance.CurrentPlant;
        }

        if (plantData == null)
        {
            Debug.LogWarning(name + " PlantData가 없습니다.");
            return;
        }

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

    protected override void OnDamaged(float damage)
    {
        // 식물 피격 연출 필요하면 여기
    }

    protected override void OnDeathStarted()
    {
        if (growManager != null)
            growManager.StopGrowth();

        if (gameManager != null)
            gameManager.GameOver();
    }

    protected override void OnDeathFinished()
    {
        // 식물은 Destroy하지 않음
    }

    protected override void OnRevived()
    {
    }
}