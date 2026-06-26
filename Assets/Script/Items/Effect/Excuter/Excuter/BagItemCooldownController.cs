using UnityEngine;

[System.Serializable]
public class BagItemCooldownController
{
    private float bagCooldown = 3f;
    private float bagCooldownRemain = 0f;
    private float[] slotCooldownRemains;
    private bool[] slotPreparationStarted;

    public void Init(int slotCount)
    {
        bagCooldownRemain = 0f;

        SyncSlotCount(slotCount);
        ClearSlotCooldowns();
        ClearSlotPreparation();
    }

    public void SetBagCooldown(float value)
    {
        bagCooldown = Mathf.Max(0f, value);
    }

    public void SyncSlotCount(int slotCount)
    {
        if (slotCount < 0)
            slotCount = 0;

        if (slotCooldownRemains == null)
            slotCooldownRemains = new float[slotCount];

        if (slotPreparationStarted == null)
            slotPreparationStarted = new bool[slotCount];

        if (slotCooldownRemains.Length != slotCount)
        {
            float[] newSlotCooldownRemains = new float[slotCount];
            int copyCount = Mathf.Min(slotCooldownRemains.Length, newSlotCooldownRemains.Length);

            for (int i = 0; i < copyCount; i++)
                newSlotCooldownRemains[i] = slotCooldownRemains[i];

            slotCooldownRemains = newSlotCooldownRemains;
        }

        if (slotPreparationStarted.Length != slotCount)
        {
            bool[] newSlotPreparationStarted = new bool[slotCount];
            int copyCount = Mathf.Min(slotPreparationStarted.Length, newSlotPreparationStarted.Length);

            for (int i = 0; i < copyCount; i++)
                newSlotPreparationStarted[i] = slotPreparationStarted[i];

            slotPreparationStarted = newSlotPreparationStarted;
        }
    }

    public void TickCooldown(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (bagCooldownRemain > 0f)
        {
            bagCooldownRemain -= deltaTime;

            if (bagCooldownRemain < 0f)
                bagCooldownRemain = 0f;
        }

        if (slotCooldownRemains == null)
            return;

        for (int i = 0; i < slotCooldownRemains.Length; i++)
        {
            if (slotCooldownRemains[i] <= 0f)
                continue;

            slotCooldownRemains[i] -= deltaTime;

            if (slotCooldownRemains[i] < 0f)
                slotCooldownRemains[i] = 0f;
        }
    }

    public void ResetAllCooldowns(int slotCount)
    {
        bagCooldownRemain = 0f;

        SyncSlotCount(slotCount);
        ClearSlotCooldowns();
        ClearSlotPreparation();
    }

    public void ResetSlotPreparation(int slotCount)
    {
        SyncSlotCount(slotCount);
        ClearSlotCooldowns();
        ClearSlotPreparation();
    }

    public void StartPreparationCooldownIfNeeded(int slotIndex, ItemData item)
    {
        if (item == null)
            return;
        if (slotPreparationStarted == null)
            return;
        if (slotIndex < 0 || slotIndex >= slotPreparationStarted.Length)
            return;
        if (slotPreparationStarted[slotIndex])
            return;

        float cooldown = Mathf.Max(0f, item.Cooldown);

        if (slotCooldownRemains != null && slotIndex >= 0 && slotIndex < slotCooldownRemains.Length)
            slotCooldownRemains[slotIndex] = cooldown;

        slotPreparationStarted[slotIndex] = true;
    }

    public void StartBagCooldown()
    {
        bagCooldownRemain = bagCooldown;
    }

    public bool IsBagCoolingDown()
    {
        return bagCooldownRemain > 0f;
    }

    public bool IsSlotCoolingDown(int slotIndex)
    {
        if (slotCooldownRemains == null)
            return false;
        if (slotIndex < 0 || slotIndex >= slotCooldownRemains.Length)
            return false;

        return slotCooldownRemains[slotIndex] > 0f;
    }

    public float GetBagCooldownRemain()
    {
        return Mathf.Max(0f, bagCooldownRemain);
    }

    public float GetBagCooldownRatio()
    {
        if (bagCooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetBagCooldownRemain() / bagCooldown);
    }

    public float GetSlotCooldownRemain(int slotIndex)
    {
        if (slotCooldownRemains == null)
            return 0f;
        if (slotIndex < 0 || slotIndex >= slotCooldownRemains.Length)
            return 0f;

        return Mathf.Max(0f, slotCooldownRemains[slotIndex]);
    }

    public float GetSlotCooldownRatio(EquipmentBag bag, int slotIndex)
    {
        if (bag == null || bag.equippedItems == null)
            return 0f;
        if (slotIndex < 0 || slotIndex >= bag.equippedItems.Count)
            return 0f;

        InventoryItem item = bag.equippedItems[slotIndex];
        if (item == null || item.itemData == null)
            return 0f;

        float cooldown = Mathf.Max(0f, item.itemData.Cooldown);
        if (cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetSlotCooldownRemain(slotIndex) / cooldown);
    }

    private void ClearSlotCooldowns()
    {
        if (slotCooldownRemains == null)
            return;

        for (int i = 0; i < slotCooldownRemains.Length; i++)
            slotCooldownRemains[i] = 0f;
    }

    private void ClearSlotPreparation()
    {
        if (slotPreparationStarted == null)
            return;

        for (int i = 0; i < slotPreparationStarted.Length; i++)
            slotPreparationStarted[i] = false;
    }
}
