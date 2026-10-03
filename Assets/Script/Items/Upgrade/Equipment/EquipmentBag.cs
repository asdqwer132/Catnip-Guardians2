using System.Collections.Generic;
using UnityEngine;

public class EquipmentBag : RefreshListener
{
    [Header("Bag Info")]
    public BagData bagData;

    [Header("Slot Settings")]
    public int maxSlotCount = 12;
    public int openSlotCount = 4;
    public int currentSlotCount = 0;

    [Header("UI")]
    public EquipmentBagUI bagUI;

    [Header("Lock")]
    public List<LockInfo> locks = new List<LockInfo>();

    [Header("Runtime")]
    public List<InventoryItem> equippedItems = new List<InventoryItem>();

    public void Init()
    {
        equippedItems.Clear();

        for (int i = 0; i < maxSlotCount; i++)
        {
            equippedItems.Add(CreateEmptyItem());
        }

        UpdateCurrentSlotCount();
        RefreshUI();
    }

    protected override void Refresh(RefreshType refreshType)
    {
        RefreshLocks();
        UpdateCurrentSlotCount();
        RefreshUI();
    }

    public bool EquipItem(InventoryItem item)
    {
        if (!CanEquipItem(item))
            return false;

        int emptyIndex = GetEmptySlotIndex();

        if (emptyIndex == -1)
            return false;

        equippedItems[emptyIndex] = new InventoryItem(item.itemData, 1);

        RefreshUI();
        return true;
    }

    public void UnequipItem(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return;

        if (!HasItem(slotIndex))
            return;

        equippedItems[slotIndex] = CreateEmptyItem();

        RefreshUI();
    }

    public void SwapItems(int fromIndex, int toIndex)
    {
        if (!IsValidCurrentSlotIndex(fromIndex))
            return;

        if (!IsValidCurrentSlotIndex(toIndex))
            return;

        if (fromIndex == toIndex)
            return;

        InventoryItem temp = equippedItems[fromIndex];
        equippedItems[fromIndex] = equippedItems[toIndex];
        equippedItems[toIndex] = temp;

        RefreshUI();
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            equippedItems[i] = CreateEmptyItem();
        }

        RefreshUI();
    }

    public bool HasItem(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        InventoryItem item = equippedItems[slotIndex];

        return item != null &&
               item.itemData != null &&
               item.amount > 0;
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return null;

        return equippedItems[slotIndex];
    }

    public int GetEquippedCount()
    {
        int count = 0;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (HasItem(i))
                count++;
        }

        return count;
    }

    public int GetCurrentEquippedCount()
    {
        int count = 0;

        int max = Mathf.Min(currentSlotCount, equippedItems.Count);

        for (int i = 0; i < max; i++)
        {
            if (HasItem(i))
                count++;
        }

        return count;
    }

    public int GetCurrentOpenSlotCount()
    {
        if (locks == null)
            return 0;

        int count = 0;

        foreach (LockInfo lockInfo in locks)
        {
            if (lockInfo == null)
                continue;

            if (!lockInfo.locked)
                count++;
        }

        return count;
    }

    public float GetCurrentWeight()
    {
        float totalWeight = 0f;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            InventoryItem item = equippedItems[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            totalWeight += item.itemData.weight * item.amount;
        }

        return totalWeight;
    }

    public float GetMaxWeight()
    {
        if (bagData == null)
            return 0f;

        return bagData.maxWeight;
    }

    public void RefreshUI()
    {
        if (bagUI != null)
            bagUI.Refresh(this);
    }

    private bool CanEquipItem(InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("������ �������� �����ϴ�.");
            return false;
        }

        if (item.amount <= 0)
        {
            Debug.LogWarning("������ ������ �����ϴ�.");
            return false;
        }

        if (bagData == null)
        {
            Debug.LogWarning("���� �����Ͱ� �����ϴ�.");
            return false;
        }

        if (GetCurrentEquippedCount() >= currentSlotCount)
        {
            Debug.Log("���� ������ ���� á���ϴ�.");
            return false;
        }

        if (!CanAddWeight(item))
        {
            Debug.Log("���� �ִ� ���Ը� �ʰ��մϴ�.");
            return false;
        }

        return true;
    }

    private bool CanAddWeight(InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return false;

        if (bagData == null)
            return false;

        float nextWeight = GetCurrentWeight() + item.itemData.weight;

        return nextWeight <= bagData.maxWeight;
    }

    private int GetEmptySlotIndex()
    {
        int max = Mathf.Min(currentSlotCount, equippedItems.Count);

        for (int i = 0; i < max; i++)
        {
            if (!HasItem(i))
                return i;
        }

        return -1;
    }

    private void RefreshLocks()
    {
        if (locks == null)
            return;

        foreach (LockInfo lockInfo in locks)
        {
            if (lockInfo == null)
                continue;

            if (UnlockCheckUtility.CanUse(lockInfo))
            {
                lockInfo.locked = false;
            }
        }
    }

    private void UpdateCurrentSlotCount()
    {
        int unlockedExtraSlotCount = GetCurrentOpenSlotCount();

        currentSlotCount = openSlotCount + unlockedExtraSlotCount;
        currentSlotCount = Mathf.Clamp(currentSlotCount, 0, maxSlotCount);
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < equippedItems.Count;
    }

    private bool IsValidCurrentSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < currentSlotCount &&
               slotIndex < equippedItems.Count;
    }


    public EquipmentBagSnapshot CreateSnapshot()
    {
        return new EquipmentBagSnapshot(this);
    }

    public void ApplySnapshot(EquipmentBagSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        if (snapshot.bagData != null)
            bagData = snapshot.bagData;

        equippedItems.Clear();

        for (int i = 0; i < maxSlotCount; i++)
        {
            if (snapshot.equippedItems != null &&
                i < snapshot.equippedItems.Count &&
                snapshot.equippedItems[i] != null)
            {
                InventoryItem savedItem = snapshot.equippedItems[i];
                equippedItems.Add(new InventoryItem(savedItem.itemData, savedItem.amount));
            }
            else
            {
                equippedItems.Add(CreateEmptyItem());
            }
        }

        RefreshLocks();
        UpdateCurrentSlotCount();
        RefreshUI();
    }

    private InventoryItem CreateEmptyItem()
    {
        return new InventoryItem(null, 0);
    }
}