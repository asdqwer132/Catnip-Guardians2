using System.Collections.Generic;
using UnityEngine;

public class EquipmentBagPresetManager : MonoBehaviour
{
    public static EquipmentBagPresetManager instance;

    [Header("Reference")]
    public EquipmentBagManager bagManager;
    public EquipmentBagPresetPanelUI presetPanelUI;

    [Header("Option")]
    public int maxPresetCount = 20;
    public bool allowEmptyPreset = false;
    public bool allowApplyToDifferentBag = false;

    [Header("Runtime")]
    public List<EquipmentBagPreset> presets = new List<EquipmentBagPreset>();

    public int PresetCount => presets != null ? presets.Count : 0;

    private void Awake()
    {
        instance = this;
        ResolveReferences();

        if (presetPanelUI != null)
            presetPanelUI.Init(this);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void AddCurrentBagPreset()
    {
        ResolveReferences();

        if (bagManager == null || bagManager.CurrentBag == null)
        {
            Debug.LogWarning("현재 선택된 가방이 없습니다.");
            return;
        }

        if (presets == null)
            presets = new List<EquipmentBagPreset>();

        if (maxPresetCount > 0 && presets.Count >= maxPresetCount)
        {
            Debug.LogWarning("프리셋 개수가 최대치입니다.");
            return;
        }

        EquipmentBag currentBag = bagManager.CurrentBag;
        EquipmentBagPreset preset = new EquipmentBagPreset(CreatePresetName(currentBag), currentBag);

        if (!allowEmptyPreset && !preset.HasAnyItem())
        {
            //ErrorMessageManager.Show("아이템이 부족합니다.");
            ErrorMessageManager.ShowNews("No Equip.", 180f);
            //ErrorMessageManager.ShowFade("No Equip.", 1.5f, 0.4f);
            Debug.LogWarning("장착된 아이템이 없어 프리셋을 만들 수 없습니다.");
            return;
        }

        presets.Add(preset);
        RefreshUI();
    }

    public bool ApplyPreset(int presetIndex)
    {
        ResolveReferences();

        if (!IsValidPresetIndex(presetIndex))
            return false;

        if (bagManager == null || bagManager.CurrentBag == null)
            return false;

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return false;
        }

        EquipmentBag targetBag = bagManager.CurrentBag;
        EquipmentBagPreset preset = presets[presetIndex];

        if (!CanApplyPresetToBag(preset, targetBag))
            return false;

        List<ItemData> originalSlotItems = CaptureAllSlotItems(targetBag);

        ReturnBagItemsToInventory(targetBag);
        ClearBagSlotsDirect(targetBag);

        Dictionary<ItemData, int> requiredItems = preset.GetItemCountMap();

        if (!HasRequiredItems(requiredItems))
        {
            Debug.LogWarning("프리셋에 필요한 아이템이 인벤토리에 부족합니다.");
            RestoreOriginalBag(targetBag, originalSlotItems);
            return false;
        }

        Dictionary<ItemData, int> removedItems = new Dictionary<ItemData, int>();

        if (!RemoveRequiredItems(requiredItems, removedItems))
        {
            RollbackRemovedItems(removedItems);
            RestoreOriginalBag(targetBag, originalSlotItems);
            return false;
        }

        SetBagSlotsDirect(targetBag, preset.slotItems, true);
        targetBag.RefreshUI();
        RefreshUI();

        return true;
    }

    public void DeletePreset(int presetIndex)
    {
        if (!IsValidPresetIndex(presetIndex))
            return;

        presets.RemoveAt(presetIndex);
        RefreshUI();
    }

    // 기존 호환용: 특정 슬롯 index 앞으로 이동한다.
    public void MovePreset(int fromIndex, int toIndex)
    {
        MovePresetToInsertIndex(fromIndex, toIndex);
    }

    // insertIndex는 0 ~ PresetCount까지 허용된다.
    // PresetCount로 넣으면 마지막 뒤로 이동한다.
    public void MovePresetToInsertIndex(int fromIndex, int insertIndex)
    {
        if (!IsValidPresetIndex(fromIndex))
            return;

        if (presets == null)
            return;

        insertIndex = Mathf.Clamp(insertIndex, 0, presets.Count);

        // 자기 자신 앞, 자기 자신 바로 뒤는 실제 순서 변화가 없다.
        if (insertIndex == fromIndex || insertIndex == fromIndex + 1)
            return;

        EquipmentBagPreset preset = presets[fromIndex];
        presets.RemoveAt(fromIndex);

        if (insertIndex > fromIndex)
            insertIndex--;

        insertIndex = Mathf.Clamp(insertIndex, 0, presets.Count);
        presets.Insert(insertIndex, preset);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (presetPanelUI != null)
            presetPanelUI.Refresh(presets);
    }

    private void ResolveReferences()
    {
        if (bagManager == null)
            bagManager = EquipmentBagManager.instance;
    }

    private string CreatePresetName(EquipmentBag bag)
    {
        string bagName = "Bag";

        if (bag != null && bag.bagData != null)
            bagName = bag.bagData.name;

        int number = presets != null ? presets.Count + 1 : 1;
        return bagName + " Preset " + number.ToString("00");
    }

    private bool CanApplyPresetToBag(EquipmentBagPreset preset, EquipmentBag targetBag)
    {
        if (preset == null || targetBag == null)
            return false;

        if (!allowApplyToDifferentBag && preset.sourceBagData != null && targetBag.bagData != preset.sourceBagData)
        {
            Debug.LogWarning("다른 가방에서 저장한 프리셋입니다.");
            return false;
        }

        int requiredSlotCount = preset.GetRequiredSlotCount();

        if (requiredSlotCount > targetBag.currentSlotCount)
        {
            Debug.LogWarning("현재 가방의 열린 슬롯 수가 부족합니다.");
            return false;
        }

        if (targetBag.bagData != null && preset.GetTotalWeight() > targetBag.bagData.maxWeight)
        {
            Debug.LogWarning("현재 가방의 최대 무게를 초과합니다.");
            return false;
        }

        return true;
    }

    private bool IsValidPresetIndex(int presetIndex)
    {
        return presets != null && presetIndex >= 0 && presetIndex < presets.Count;
    }

    private List<ItemData> CaptureAllSlotItems(EquipmentBag bag)
    {
        List<ItemData> result = new List<ItemData>();

        if (bag == null || bag.equippedItems == null)
            return result;

        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            InventoryItem item = bag.equippedItems[i];
            result.Add(item != null && item.itemData != null && item.amount > 0 ? item.itemData : null);
        }

        return result;
    }

    private void ReturnBagItemsToInventory(EquipmentBag bag)
    {
        if (bag == null || bag.equippedItems == null || InventoryManager.instance == null)
            return;

        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            InventoryItem item = bag.equippedItems[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            InventoryManager.instance.AddItem(item.itemData, item.amount);
        }
    }

    private bool HasRequiredItems(Dictionary<ItemData, int> requiredItems)
    {
        if (requiredItems == null)
            return true;

        foreach (KeyValuePair<ItemData, int> pair in requiredItems)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            if (!InventoryManager.instance.HasItem(pair.Key, pair.Value))
                return false;
        }

        return true;
    }

    private bool RemoveRequiredItems(Dictionary<ItemData, int> requiredItems, Dictionary<ItemData, int> removedItems)
    {
        if (requiredItems == null)
            return true;

        foreach (KeyValuePair<ItemData, int> pair in requiredItems)
        {
            ItemData itemData = pair.Key;
            int amount = pair.Value;

            if (itemData == null || amount <= 0)
                continue;

            bool removed = InventoryManager.instance.RemoveItem(itemData, amount);

            if (!removed)
                return false;

            if (removedItems != null)
                removedItems[itemData] = amount;
        }

        return true;
    }

    private void RollbackRemovedItems(Dictionary<ItemData, int> removedItems)
    {
        if (removedItems == null || InventoryManager.instance == null)
            return;

        foreach (KeyValuePair<ItemData, int> pair in removedItems)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            InventoryManager.instance.AddItem(pair.Key, pair.Value);
        }
    }

    private void RestoreOriginalBag(EquipmentBag bag, List<ItemData> originalSlotItems)
    {
        if (bag == null || InventoryManager.instance == null)
            return;

        Dictionary<ItemData, int> originalCounts = BuildCountMap(originalSlotItems);

        foreach (KeyValuePair<ItemData, int> pair in originalCounts)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            InventoryManager.instance.RemoveItem(pair.Key, pair.Value);
        }

        SetBagSlotsDirect(bag, originalSlotItems, false);
        bag.RefreshUI();
    }

    private Dictionary<ItemData, int> BuildCountMap(List<ItemData> slotItems)
    {
        Dictionary<ItemData, int> result = new Dictionary<ItemData, int>();

        if (slotItems == null)
            return result;

        for (int i = 0; i < slotItems.Count; i++)
        {
            ItemData itemData = slotItems[i];

            if (itemData == null)
                continue;

            if (!result.ContainsKey(itemData))
                result.Add(itemData, 0);

            result[itemData]++;
        }

        return result;
    }

    private void ClearBagSlotsDirect(EquipmentBag bag)
    {
        if (bag == null || bag.equippedItems == null)
            return;

        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            bag.equippedItems[i] = new InventoryItem(null, 0);
        }

        bag.RefreshUI();
    }

    private void SetBagSlotsDirect(EquipmentBag bag, List<ItemData> slotItems, bool respectCurrentSlotCount)
    {
        if (bag == null || bag.equippedItems == null)
            return;

        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            bag.equippedItems[i] = new InventoryItem(null, 0);
        }

        if (slotItems == null)
            return;

        int max = Mathf.Min(slotItems.Count, bag.equippedItems.Count);

        if (respectCurrentSlotCount)
            max = Mathf.Min(max, bag.currentSlotCount);

        for (int i = 0; i < max; i++)
        {
            ItemData itemData = slotItems[i];

            if (itemData == null)
                continue;

            bag.equippedItems[i] = new InventoryItem(itemData, 1);
        }
    }
}
