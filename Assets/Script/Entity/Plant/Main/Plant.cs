using UnityEngine;
using UnityEngine.UI;

public class Plant : HealthActor
{
    public PlantUI plantUI;

    public GameManager gameManager;
    public PlantData plantData;

    [Header("Growth UI")]
    public Slider growthSlider;

    private float growTimer;
    private bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (IsDead)
            return;

        UpdateGrowth();
    }

    void UpdateGrowth()
    {
        if (plantData == null)
            return;

        growTimer += Time.deltaTime;

        if (growthSlider != null)
            growthSlider.value = growTimer;

        if (growTimer >= plantData.growTime)
        {
            if (gameManager != null)
                gameManager.Victory();

            isInitialized = false;
        }
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

        if (growthSlider != null)
        {
            growthSlider.gameObject.SetActive(true);
            growthSlider.maxValue = plantData.growTime;
            growthSlider.value = 0f;
        }

        growTimer = 0f;
        isInitialized = true;
    }

    protected override void OnDamaged(float damage)
    {
        // 식물 피격 연출 필요하면 여기
    }

    protected override void OnDeathStarted()
    {
        if (gameManager != null)
            gameManager.GameOver();

        isInitialized = false;
    }

    protected override void OnDeathFinished()
    {
        // 식물은 Destroy하지 않음
    }

    protected override void OnRevived()
    {
        isInitialized = true;
    }
}