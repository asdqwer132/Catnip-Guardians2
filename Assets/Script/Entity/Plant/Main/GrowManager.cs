using UnityEngine;
using UnityEngine.UI;

public class GrowManager : MonoBehaviour
{
    public static GrowManager instance;

    [Header("UI")]
    public Slider growthSlider;

    [Header("Managers")]
    public GameManager gameManager;

    private PlantData plantData;
    private float growValue;
    private bool isGrowing = false;

    private void Awake() { instance = this; }

    private void Update()
    {
        if (!isGrowing)
            return;

        if (plantData == null)
            return;

        AddGrowth(Time.deltaTime);
    }

    public void Init(PlantData data)
    {
        plantData = data;
        growValue = 0f;
        isGrowing = false;

        if (plantData == null)
        {
            UpdateUI();
            return;
        }

        if (growthSlider != null)
        {
            growthSlider.gameObject.SetActive(true);
            growthSlider.minValue = 0f;
            growthSlider.maxValue = plantData.growTime;
            growthSlider.value = 0f;
        }

        isGrowing = true;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (growthSlider == null)
            return;

        if (plantData == null)
        {
            growthSlider.value = 0f;
            return;
        }

        growthSlider.maxValue = plantData.growTime;
        growthSlider.value = growValue;
    }

    #region Modify Growth
    public void AddGrowth(float amount)
    {
        if (!isGrowing)
            return;

        if (plantData == null)
            return;

        if (amount <= 0f)
            return;

        growValue += amount;

        if (growValue > plantData.growTime)
            growValue = plantData.growTime;

        UpdateUI();

        if (growValue >= plantData.growTime)
            CompleteGrowth();
    }

    public void StopGrowth()
    {
        isGrowing = false;
    }

    public void ResetGrowth()
    {
        growValue = 0f;
        isGrowing = false;
        UpdateUI();
    }

    private void CompleteGrowth()
    {
        if (!isGrowing)
            return;

        isGrowing = false;

        if (gameManager != null)
            gameManager.Victory();
    }
    #endregion
}