using UnityEngine;

public class GrowManager : MonoBehaviour
{
    public static GrowManager instance;

    [Header("UI")]
    public ImageFillUI growthFill;

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

        if (growthFill != null)
        {
            growthFill.gameObject.SetActive(true);
            growthFill.SetFill01(0f);
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
        if (growthFill == null)
            return;

        if (plantData == null)
        {
            growthFill.SetFill01(0f);
            return;
        }

        growthFill.SetFill(growValue, plantData.growTime);
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
    }

    private void CompleteGrowth()
    {
        if (!isGrowing)
            return;

        isGrowing = false;

        if (plantUI != null)
            plantUI.SetGrownUpSprite();

        if (gameManager != null)
            gameManager.Victory();
    }
}