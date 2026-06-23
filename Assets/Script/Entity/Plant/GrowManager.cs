using UnityEngine;

public class GrowManager : MonoBehaviour
{
    public static GrowManager instance;

    [Header("Growth Value")]
    [SerializeField] private float startGrowValue = 0f;
    [SerializeField] private float growValue;
    [SerializeField] private float maxGrowValue = 100f;

    [Header("UI")]
    public ImageFillUI growthFill;

    [Header("Managers")]
    public GameManager gameManager;

    private PlantUI plantUI;
    private PlantData plantData;
    private bool isGrowing = false;
    private int currentGrowingIndex = -1;

    public float CurrentGrowValue => growValue;
    public float MaxGrowValue => maxGrowValue;
    public float GrowProgress01 => maxGrowValue > 0f ? growValue / maxGrowValue : 0f;
    public float GrowProgressPercent => GrowProgress01 * 100f;
    public bool IsGrowing => isGrowing;

    private void Awake()
    {
        instance = this;
    }

    public void Init(PlantData data, PlantUI ui)
    {
        plantData = data;
        plantUI = ui;

        growValue = startGrowValue;
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
        UpdateGrowingSprite();
    }

    public void SetMaxGrowValue(float value)
    {
        maxGrowValue = Mathf.Max(0f, value);

        if (growValue > maxGrowValue)
            growValue = maxGrowValue;

        UpdateUI();
        UpdateGrowingSprite();
    }

    public void SetStartGrowValue(float value)
    {
        startGrowValue = Mathf.Max(0f, value);
    }

    public void StartGrowth()
    {
        growValue = startGrowValue;
        isGrowing = true;
        currentGrowingIndex = -1;

        if (maxGrowValue <= 0f)
        {
            growValue = 0f;
            UpdateUI();
            CompleteGrowth();
            return;
        }

        if (growValue >= maxGrowValue)
        {
            growValue = maxGrowValue;
            UpdateUI();
            CompleteGrowth();
            return;
        }

        UpdateUI();
        UpdateGrowingSprite();
    }

    public void AddGrowth(float amount)
    {
        if (!isGrowing)
            return;
        if (amount <= 0f)
            return;
        if (maxGrowValue <= 0f)
            return;

        growValue += amount;

        if (growValue >= maxGrowValue)
        {
            growValue = maxGrowValue;

            UpdateUI();
            UpdateGrowingSprite();
            CompleteGrowth();
            return;
        }

        UpdateUI();
        UpdateGrowingSprite();
    }

    public void SetGrowth(float value)
    {
        growValue = Mathf.Clamp(value, 0f, maxGrowValue);

        UpdateUI();
        UpdateGrowingSprite();

        if (isGrowing && growValue >= maxGrowValue)
            CompleteGrowth();
    }

    private void UpdateUI()
    {
        if (growthFill == null)
            return;

        if (maxGrowValue <= 0f)
        {
            growthFill.SetFill01(0f);
            return;
        }

        growthFill.SetFill(growValue, maxGrowValue);
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
        if (maxGrowValue <= 0f)
            return plantData.growing.Length - 1;

        float progress = growValue / maxGrowValue;
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
        growValue = startGrowValue;
        isGrowing = false;
        currentGrowingIndex = -1;

        UpdateUI();
        UpdateGrowingSprite();
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