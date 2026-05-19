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

    // 체력, 성장 타이머 리셋
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

        InitHealth(plantData.maxHP, true);

        if (growthSlider != null)
        {
            growthSlider.maxValue = plantData.growTime;
            growthSlider.value = 0f;
        }

        growTimer = 0f;
        isInitialized = true;
    }

    protected override void OnDamaged(float damage)
    {
        // 식물 피격 연출이 필요하면 여기서 처리
        // 예: visual.PlayHit();
    }

    protected override void OnDeathStarted()
    {
        if (gameManager != null)
            gameManager.GameOver();

        isInitialized = false;
    }

    protected override void OnDeathFinished()
    {
        // 식물은 죽어도 Destroy하지 않을 거면 비워둠
        // Destroy(gameObject)를 원하면 여기서 처리
    }
}