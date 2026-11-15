using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Consumes one set of materials, then rolls once per distinct result item.</summary>
public sealed class ItemCraftManager : MonoBehaviour
{
    // Compatibility for older UI scripts; resolves to the manager selected by GeneralCraftManager.
    public static ItemCraftManager instance =>
        GeneralCraftManager.instance != null ? GeneralCraftManager.instance.GetCurrentManager() : null;

    [Header("Craftable items")]
    [SerializeField] private List<CraftItemData> craftableItems = new List<CraftItemData>();

    [Header("Gauge")]
    [SerializeField] private CraftGaugeManager gaugeManager;

    [Header("Materials")]
    [SerializeField, Min(1)] private int minimumMaterialCount = 2;
    [SerializeField, Min(1)] private int maximumMaterialCount = 4;
    [SerializeField] private List<InventoryItem> currentMaterials = new List<InventoryItem>();

    [Header("Optional failed result")]
    [SerializeField] private bool giveFailedItem;
    [SerializeField] private ItemData failedItem;

    public Action onMaterialChanged;
    public event Action<CraftResult> CraftCompleted;

    public IReadOnlyList<CraftItemData> CraftableItems => craftableItems;
    public IReadOnlyList<InventoryItem> CurrentMaterials => currentMaterials;
    public int GetCurrentMaterialCount() => new CraftContext(currentMaterials).TotalMaterialCount;
    public CraftGaugeManager GaugeManager
    {
        get
        {
            if (gaugeManager == null)
                gaugeManager = GetComponent<CraftGaugeManager>();
            return gaugeManager;
        }
    }

    private void Awake()
    {
        MergeDuplicateMaterials();
        if (GaugeManager == null)
            Debug.LogWarning("[ItemCraftManager] CraftGaugeManager is missing on " + name);
    }

    private void OnValidate()
    {
        maximumMaterialCount = Mathf.Max(1, maximumMaterialCount);
        minimumMaterialCount = Mathf.Clamp(minimumMaterialCount, 1, maximumMaterialCount);
    }

    public CraftContext CreateContext()
    {
        CraftGaugeSnapshot gauges = GaugeManager != null
            ? GaugeManager.CalculateSnapshot(currentMaterials)
            : CraftGaugeSnapshot.Empty;
        return new CraftContext(currentMaterials, gauges);
    }

    public float GetCurrentGaugePercentage(CraftGaugeType gaugeType)
    {
        return CreateContext().Gauges.GetPercentage(gaugeType);
    }

    public void AddMaterial(ItemData item) => TryAddMaterial(item);

    public bool TryAddMaterial(ItemData item)
    {
        InventoryManager inventory = InventoryManager.instance;
        if (item == null || inventory == null || GetCurrentMaterialCount() >= maximumMaterialCount)
            return false;

        if (!inventory.RemoveItem(item, 1))
            return false;

        InventoryItem existing = currentMaterials.Find(entry => entry != null && entry.itemData == item);
        if (existing != null)
            existing.amount++;
        else
            currentMaterials.Add(new InventoryItem(item, 1));

        onMaterialChanged?.Invoke();
        return true;
    }

    // Merge any duplicate entries serialized in the scene before this version was installed.
    private void MergeDuplicateMaterials()
    {
        if (currentMaterials == null)
            currentMaterials = new List<InventoryItem>();

        Dictionary<ItemData, InventoryItem> firstByItem = new Dictionary<ItemData, InventoryItem>();
        for (int i = 0; i < currentMaterials.Count;)
        {
            InventoryItem entry = currentMaterials[i];
            if (entry == null || entry.itemData == null || entry.amount <= 0)
            {
                currentMaterials.RemoveAt(i);
                continue;
            }

            InventoryItem first;
            if (firstByItem.TryGetValue(entry.itemData, out first))
            {
                first.amount += entry.amount;
                currentMaterials.RemoveAt(i);
                continue;
            }

            firstByItem.Add(entry.itemData, entry);
            i++;
        }
    }

    public void ReturnMaterial(ItemData item, int amount = 1)
    {
        InventoryManager inventory = InventoryManager.instance;
        if (item == null || inventory == null || amount <= 0)
            return;

        int remaining = amount;
        for (int i = currentMaterials.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryItem entry = currentMaterials[i];
            if (entry == null || entry.itemData != item || entry.amount <= 0)
                continue;

            int returned = Mathf.Min(entry.amount, remaining);
            inventory.AddItem(item, returned);
            entry.amount -= returned;
            remaining -= returned;

            if (entry.amount == 0)
                currentMaterials.RemoveAt(i);
        }

        if (remaining != amount)
            onMaterialChanged?.Invoke();
    }

    public void ReturnMaterials()
    {
        InventoryManager inventory = InventoryManager.instance;
        if (inventory == null)
            return;

        foreach (InventoryItem entry in currentMaterials)
        {
            if (entry != null && entry.itemData != null && entry.amount > 0)
                inventory.AddItem(entry.itemData, entry.amount);
        }

        currentMaterials.Clear();
        onMaterialChanged?.Invoke();
    }

    public float GetCurrentRawChancePercent(CraftItemData item)
    {
        return item == null ? 0f : item.GetRawChancePercent(CreateContext());
    }

    // Can be connected directly to an existing crafting button.
    public void Combine() => TryCraft(out _);

    /// <returns>False when crafting cannot start; a completed attempt may yield zero items.</returns>
    public bool TryCraft(out CraftResult result)
    {
        result = null;
        InventoryManager inventory = InventoryManager.instance;
        if (inventory == null)
        {
            Debug.LogWarning("[ItemCraftManager] InventoryManager is missing.");
            return false;
        }

        CraftContext context = CreateContext();
        if (context.TotalMaterialCount < minimumMaterialCount)
            return false;

        CraftResult completed = new CraftResult();
        HashSet<ItemData> seenItems = new HashSet<ItemData>();

        if (craftableItems != null)
        {
            foreach (CraftItemData candidate in craftableItems)
            {
                if (candidate == null || candidate.resultItem == null)
                    continue;

                if (!seenItems.Add(candidate.resultItem))
                {
                    Debug.LogWarning("[ItemCraftManager] Duplicate result item: " + candidate.resultItem.name);
                    continue;
                }

                float chance = candidate.GetRawChancePercent(context);
                if (chance <= 0f)
                    continue;

                bool success = chance >= 100f || UnityEngine.Random.value < chance / 100f;
                completed.AddRoll(new CraftRollResult(candidate.resultItem, chance, success));
            }
        }

        // All candidates use the same snapshot. Materials are spent once for the whole attempt.
        currentMaterials.Clear();
        if (completed.CraftedItems.Count == 0) Debug.Log("조합 실패");
        foreach (ItemData item in completed.CraftedItems)
        {
            inventory.AddItem(item, 1);
            Debug.Log(item.GetDataName());
        }

        if (!completed.HasSuccess && giveFailedItem && failedItem != null)
        {
            inventory.AddItem(failedItem, 1);
            completed.FailureItem = failedItem;
        }

        result = completed;
        onMaterialChanged?.Invoke();
        CraftCompleted?.Invoke(completed);
        return true;
    }
}
