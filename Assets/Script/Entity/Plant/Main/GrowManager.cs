using UnityEngine;
using UnityEngine.UI;

public class GrowManager : MonoBehaviour
{
    public static GrowManager instance;

    [Header("UI")]
    public Slider growthSlider;

    [Header("Managers")]
    public GameManager gameManager;

    [Header("Debug")]
    public bool debugLog = false;

    private PlantData plantData;
    private float growValue;
    private bool isGrowing = false;

    public float GrowValue => growValue;
    public float GrowMaxValue => plantData != null ? plantData.growTime : 0f;
    public bool IsGrowing => isGrowing;

    private void Awake()
    {
        instance = this;
    }

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
            Debug.LogWarning(name + " GrowManager에 PlantData가 없습니다.");
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

        if (debugLog)
            Debug.Log("성장 시작 / 목표 성장치: " + plantData.growTime);
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

        if (growValue > plantData.growTime)
            growValue = plantData.growTime;

        UpdateUI();

        if (debugLog)
            Debug.Log("성장 증가: +" + amount + " / 현재: " + growValue);

        if (growValue >= plantData.growTime)
        {
            CompleteGrowth();
        }
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

        if (debugLog)
            Debug.Log("성장 완료");

        if (gameManager != null)
            gameManager.Victory();
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
}