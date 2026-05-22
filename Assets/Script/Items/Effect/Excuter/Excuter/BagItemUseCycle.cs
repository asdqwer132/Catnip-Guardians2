[System.Serializable]
public class BagItemUseCycle
{
    private int currentSlotIndex = 0;
    private bool[] usedSlotInCycle;
    private int currentCycleId = 0;

    public int CurrentCycleId { get { return currentCycleId; } }

    public void Init(int slotCount)
    {
        currentSlotIndex = 0;
        currentCycleId++;

        SyncSlotCount(slotCount);
        ClearUsedSlots();
    }

    public void SyncSlotCount(int slotCount)
    {
        if (slotCount < 0)
            slotCount = 0;
        if (usedSlotInCycle == null || usedSlotInCycle.Length != slotCount)
            usedSlotInCycle = new bool[slotCount];
        if (slotCount == 0)
        {
            currentSlotIndex = 0;
            return;
        }
        if (currentSlotIndex < 0 || currentSlotIndex >= slotCount)
            currentSlotIndex = 0;
    }

    public void ResetUsePosition(int slotCount)
    {
        currentCycleId++;
        currentSlotIndex = 0;

        SyncSlotCount(slotCount);
        ClearUsedSlots();
    }

    public void MarkSlotUsedAndMoveNext(int slotIndex, int slotCount)
    {
        SyncSlotCount(slotCount);

        if (usedSlotInCycle == null)
            return;
        if (slotIndex < 0 || slotIndex >= usedSlotInCycle.Length)
            return;

        usedSlotInCycle[slotIndex] = true;
        currentSlotIndex = slotIndex + 1;

        if (slotCount <= 0)
        {
            currentSlotIndex = 0;
            return;
        }
        if (currentSlotIndex >= slotCount)
            currentSlotIndex = 0;
    }

    public int GetNextUsableSlotIndex(EquipmentBag bag)
    {
        if (bag == null || bag.equippedItems == null)
            return -1;

        int slotCount = bag.equippedItems.Count;
        SyncSlotCount(slotCount);

        if (slotCount <= 0)
            return -1;
        if (usedSlotInCycle == null || usedSlotInCycle.Length != slotCount)
            return -1;
        if (currentSlotIndex < 0 || currentSlotIndex >= slotCount)
            currentSlotIndex = 0;
        for (int i = 0; i < slotCount; i++)
        {
            int index = (currentSlotIndex + i) % slotCount;
            if (index < 0 || index >= usedSlotInCycle.Length)
                continue;
            if (usedSlotInCycle[index])
                continue;

            InventoryItem item = bag.equippedItems[index];
            if (!IsUsableItem(item))
                continue;

            return index;
        }

        return -1;
    }

    public bool HasUsedAllUsableSlotsThisCycle(EquipmentBag bag)
    {
        if (bag == null || bag.equippedItems == null)
            return false;

        int slotCount = bag.equippedItems.Count;
        SyncSlotCount(slotCount);

        if (usedSlotInCycle == null || usedSlotInCycle.Length != slotCount)
            return false;

        bool hasUsableItem = false;

        for (int i = 0; i < slotCount; i++)
        {
            InventoryItem item = bag.equippedItems[i];

            if (!IsUsableItem(item))
                continue;

            hasUsableItem = true;

            if (!usedSlotInCycle[i])
                return false;
        }

        return hasUsableItem;
    }

    private void ClearUsedSlots()
    {
        if (usedSlotInCycle == null)
            return;

        for (int i = 0; i < usedSlotInCycle.Length; i++)
            usedSlotInCycle[i] = false;
    }

    private bool IsUsableItem(InventoryItem item) { return item != null && item.itemData != null && item.amount > 0; }
}