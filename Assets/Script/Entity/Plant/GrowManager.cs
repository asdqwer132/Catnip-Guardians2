using System;
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

    private bool isGrowing = false;
    private float nutrientsPerSecond;

    public float CurrentGrowValue => growValue;
    public float MaxGrowValue => maxGrowValue;
    public float GrowProgress01 => maxGrowValue > 0f ? growValue / maxGrowValue : 0f;
    public float GrowProgressPercent => GrowProgress01 * 100f;
    public bool IsGrowing => isGrowing;
    public event Action<float, float> GrowthChanged;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    public void Init(PlantData data, PlantUI ui)
    {
        maxGrowValue = data != null ? Mathf.Max(0f, data.nutrientsRequired) : 0f;
        nutrientsPerSecond = data != null ? Mathf.Max(0f, data.nutrientsPerSecond) : 0f;

        growValue = Mathf.Clamp(startGrowValue, 0f, maxGrowValue);
        isGrowing = false;

        if (ui != null) ui.SetPlantData(data);

        if (growthFill != null)
        {
            growthFill.gameObject.SetActive(true);
            growthFill.SetFill01(0f);
        }

        UpdateUI();
    }

    public void SetMaxGrowValue(float value)
    {
        maxGrowValue = Mathf.Max(0f, value);

        if (growValue > maxGrowValue)
            growValue = maxGrowValue;

        UpdateUI();
    }

    public void SetStartGrowValue(float value)
    {
        startGrowValue = Mathf.Max(0f, value);
    }

    public void StartGrowth()
    {
        growValue = Mathf.Clamp(startGrowValue, 0f, maxGrowValue);
        isGrowing = true;

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

            CompleteGrowth();
            return;
        }

        UpdateUI();
    }

    public void SetGrowth(float value)
    {
        growValue = Mathf.Clamp(value, 0f, maxGrowValue);

        UpdateUI();

        if (isGrowing && growValue >= maxGrowValue)
            CompleteGrowth();
    }

    private void UpdateUI()
    {
        GrowthChanged?.Invoke(growValue, maxGrowValue);
        if (growthFill == null)
            return;

        if (maxGrowValue <= 0f)
        {
            growthFill.SetFill01(0f);
            return;
        }

        growthFill.SetFill(growValue, maxGrowValue);
    }

    private void Update()
    {
        if (isGrowing) AddGrowth(nutrientsPerSecond * Time.deltaTime);
    }

    public void StopGrowth()
    {
        isGrowing = false;
    }

    public void ResetGrowth()
    {
        growValue = Mathf.Clamp(startGrowValue, 0f, maxGrowValue);
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
}
