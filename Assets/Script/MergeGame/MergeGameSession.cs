using System.Collections.Generic;
using UnityEngine;

// 씬 이동 중에는 재료 보유량과 구매한 통 스킬을 유지합니다. 디스크 저장과는 별개입니다.
public sealed class MergeGameSession : MonoBehaviour
{
    public static MergeGameSession Instance { get; private set; }
    public MergeContainerUpgradeLedger upgrades = new MergeContainerUpgradeLedger();
    public List<InventoryItem> inventory = new List<InventoryItem>();
    public bool hasInventory;
    public List<InventoryItem> subPlantMaterials = new List<InventoryItem>();
    public ItemData subPlantResult;
    public bool hasSubPlantState;
    public static MergeGameSession Ensure()
    {
        if (Instance == null) new GameObject("Merge Game Session").AddComponent<MergeGameSession>();
        return Instance;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void SaveInventory(InventoryManager source)
    {
        if (source == null) return;
        inventory.Clear();
        foreach (var entry in source.items)
            if (entry != null && entry.itemData != null && entry.amount > 0) inventory.Add(new InventoryItem(entry.itemData, entry.amount));
        hasInventory = true;
    }
    public void RestoreInventory(InventoryManager target)
    {
        if (target == null || !hasInventory) return;
        target.items.Clear();
        foreach (var entry in inventory)
            if (entry != null && entry.itemData != null && entry.amount > 0) target.items.Add(new InventoryItem(entry.itemData, entry.amount));
        target.onInventoryChanged?.Invoke();
    }
    public void SaveSubPlant(IEnumerable<InventoryItem> materials, ItemData result)
    {
        subPlantMaterials.Clear();
        if (materials != null) foreach (var entry in materials)
            if (entry != null && entry.itemData != null && entry.amount > 0) subPlantMaterials.Add(new InventoryItem(entry.itemData, entry.amount));
        subPlantResult = result; hasSubPlantState = true;
    }
}
