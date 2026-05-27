using UnityEngine;
using UnityEngine.UI;

public class GrowManager : MonoBehaviour
{
    public static GrowManager instance;

    [Header("UI")]
    public Slider growthSlider;

    [Header("Plant UI")]
    public PlantUI plantUI;

    [Header("Managers")]
    public GameManager gameManager;

    private PlantData plantData;
    private float growValue;
    private bool isGrowing = false;

    private int currentGrowingIndex = -1;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (!isGrowing)
            return;

        AddGrowth(Time.deltaTime);
    }

    public void Init(PlantData data, PlantUI ui)
    {
        plantData = data;
        plantUI = ui;

        growValue = 0f;
        isGrowing = false;
        currentGrowingIndex = -1;

        if (plantUI != null)
            plantUI.SetPlantData(plantData);

        if (growthSlider != null)
        {
            growthSlider.gameObject.SetActive(true);
            growthSlider.minValue = 0f;
            growthSlider.value = 0f;

            if (plantData != null)
                growthSlider.maxValue = plantData.growTime;
        }

        UpdateUI();
    }

    public void StartGrowth()
    {
        if (plantData == null)
            return;

        growValue = 0f;
        isGrowing = true;
        currentGrowingIndex = -1;

        UpdateUI();

        // 성장 시작 시 seed가 아니라 growing[0]부터 표시
        UpdateGrowingSprite();
    }

    public void AddGrowth(float amount)
    {
        if (!isGrowing)
            return;

        if (plantData == null)
            return;

        if (amount <= 0f)
            return;

        growValue += amount;

        if (growValue >= plantData.growTime)
        {
            growValue = plantData.growTime;

            UpdateUI();
            CompleteGrowth();
            return;
        }

        UpdateUI();
        UpdateGrowingSprite();
    }

    private void UpdateUI()
    {
        if (growthSlider == null)
            return;

        growthSlider.value = growValue;
    }

    private void UpdateGrowingSprite()
    {
        if (plantUI == null)
            return;

        if (plantData == null)
            return;

        if (plantData.growing == null || plantData.growing.Length == 0)
            return;

        int nextIndex = GetGrowingIndex();

        if (currentGrowingIndex == nextIndex)
            return;

        currentGrowingIndex = nextIndex;
        plantUI.SetGrowingSprite(nextIndex);
    }

    private int GetGrowingIndex()
    {
        if (plantData == null)
            return 0;

        if (plantData.growing == null || plantData.growing.Length == 0)
            return 0;

        if (plantData.growTime <= 0f)
            return plantData.growing.Length - 1;

        float progress = growValue / plantData.growTime;

        int index = Mathf.FloorToInt(progress * plantData.growing.Length);

        if (index < 0)
            index = 0;

        if (index >= plantData.growing.Length)
            index = plantData.growing.Length - 1;

        return index;
    }

    public void StopGrowth()
    {
        isGrowing = false;
    }

    public void ResetGrowth()
    {
        growValue = 0f;
        isGrowing = false;
        currentGrowingIndex = -1;

        UpdateUI();

        // 여기서도 seed 표시 안 함
        // 필요하면 외부 애니메이션에서 따로 처리
    }

    private void CompleteGrowth()
    {
        if (!isGrowing)
            return;

        isGrowing = false;

        // 완료 시점에만 grownUp으로 변경
        if (plantUI != null)
            plantUI.SetGrownUpSprite();

        if (gameManager != null)
            gameManager.Victory();
    }
}