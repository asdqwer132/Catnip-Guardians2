using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CraftGaugeWeight
{
    public CraftGaugeType gaugeType;
    [Min(0f)] public float multiplier = 1f;
}

/// <summary>Weighted gauge totals normalized so their percentages sum to 100.</summary>
public sealed class CraftGaugeSnapshot
{
    public static readonly CraftGaugeSnapshot Empty =
        new CraftGaugeSnapshot(new Dictionary<CraftGaugeType, double>());

    private readonly Dictionary<CraftGaugeType, double> weightedAmounts;
    public double TotalWeightedAmount { get; private set; }
    public IEnumerable<CraftGaugeType> GaugeTypes => weightedAmounts.Keys;

    internal CraftGaugeSnapshot(Dictionary<CraftGaugeType, double> amounts)
    {
        weightedAmounts = new Dictionary<CraftGaugeType, double>(amounts);
        foreach (double amount in weightedAmounts.Values)
            TotalWeightedAmount += amount;
    }

    public double GetWeightedAmount(CraftGaugeType gaugeType)
    {
        if (gaugeType == null)
            return 0d;

        double amount;
        return weightedAmounts.TryGetValue(gaugeType, out amount) ? amount : 0d;
    }

    public float GetPercentage(CraftGaugeType gaugeType)
    {
        return TotalWeightedAmount > 0d
            ? (float)(GetWeightedAmount(gaugeType) / TotalWeightedAmount * 100d)
            : 0f;
    }
}

/// <summary>One per ItemCraftManager. Converts its materials to weighted gauge ratios.</summary>
public sealed class CraftGaugeManager : MonoBehaviour
{
    [SerializeField] private ItemCraftManager craftManager;
    [SerializeField] private CraftMaterialGaugeData[] materialGaugeData = new CraftMaterialGaugeData[0];
    [SerializeField] private CraftGaugeWeight[] gaugeWeights = new CraftGaugeWeight[0];

    private readonly Dictionary<ItemData, CraftMaterialGaugeData> dataByItem =
        new Dictionary<ItemData, CraftMaterialGaugeData>();
    private readonly Dictionary<CraftGaugeType, float> weightByGauge =
        new Dictionary<CraftGaugeType, float>();
    private ItemCraftManager subscribedManager;
    private bool lookupsReady;

    public CraftGaugeSnapshot CurrentSnapshot { get; private set; } = CraftGaugeSnapshot.Empty;
    public event Action<CraftGaugeSnapshot> GaugeChanged;

    private void Awake()
    {
        ResolveCraftManager();
        RebuildLookups();
    }

    private void OnEnable()
    {
        RebuildLookups();
        Subscribe();
        RefreshGauge();
    }

    // Also covers scene objects whose Awake order left the manager unresolved in OnEnable.
    private void Start()
    {
        Subscribe();
        RefreshGauge();
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.onMaterialChanged -= RefreshGauge;
            subscribedManager = null;
        }
    }

    private void OnValidate()
    {
        lookupsReady = false;
        if (Application.isPlaying)
        {
            RebuildLookups();
            RefreshGauge();
        }
    }

    private ItemCraftManager ResolveCraftManager()
    {
        if (craftManager == null)
            craftManager = GetComponent<ItemCraftManager>();

        return craftManager;
    }

    private void Subscribe()
    {
        ItemCraftManager manager = ResolveCraftManager();
        if (!isActiveAndEnabled || subscribedManager == manager)
            return;

        if (subscribedManager != null)
            subscribedManager.onMaterialChanged -= RefreshGauge;

        subscribedManager = manager;
        if (subscribedManager != null)
            subscribedManager.onMaterialChanged += RefreshGauge;
    }

    private void RebuildLookups()
    {
        dataByItem.Clear();
        weightByGauge.Clear();

        if (materialGaugeData != null)
        {
            foreach (CraftMaterialGaugeData data in materialGaugeData)
            {
                if (data != null && data.itemData != null && !dataByItem.ContainsKey(data.itemData))
                    dataByItem.Add(data.itemData, data);
            }
        }

        if (gaugeWeights != null)
        {
            foreach (CraftGaugeWeight weight in gaugeWeights)
            {
                if (weight == null || weight.gaugeType == null || weightByGauge.ContainsKey(weight.gaugeType))
                    continue;

                weightByGauge.Add(weight.gaugeType, IsFinitePositive(weight.multiplier) ? weight.multiplier : 0f);
            }
        }

        lookupsReady = true;
    }

    private static bool IsFinitePositive(float number)
    {
        return number > 0f && !float.IsNaN(number) && !float.IsInfinity(number);
    }

    public CraftGaugeSnapshot CalculateSnapshot(IEnumerable<InventoryItem> materials)
    {
        if (!lookupsReady)
            RebuildLookups();

        Dictionary<CraftGaugeType, double> totals = new Dictionary<CraftGaugeType, double>();
        if (materials == null)
            return CraftGaugeSnapshot.Empty;

        foreach (InventoryItem material in materials)
        {
            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            CraftMaterialGaugeData data;
            if (!dataByItem.TryGetValue(material.itemData, out data) || data == null || data.gauges == null)
                continue;

            foreach (CraftGaugeAmount gauge in data.gauges)
            {
                if (gauge == null || gauge.gaugeType == null || !IsFinitePositive(gauge.amount))
                    continue;

                float multiplier;
                if (!weightByGauge.TryGetValue(gauge.gaugeType, out multiplier))
                    multiplier = 1f;

                double added = (double)gauge.amount * material.amount * multiplier;
                if (added <= 0d || double.IsInfinity(added) || double.IsNaN(added))
                    continue;

                double existing;
                totals.TryGetValue(gauge.gaugeType, out existing);
                totals[gauge.gaugeType] = existing + added;
            }
        }

        return new CraftGaugeSnapshot(totals);
    }

    public float GetCurrentPercentage(CraftGaugeType gaugeType)
    {
        return CurrentSnapshot.GetPercentage(gaugeType);
    }

    public void RefreshGauge()
    {
        ItemCraftManager manager = ResolveCraftManager();
        CurrentSnapshot = manager != null
            ? CalculateSnapshot(manager.CurrentMaterials)
            : CraftGaugeSnapshot.Empty;

        GaugeChanged?.Invoke(CurrentSnapshot);
    }
}
