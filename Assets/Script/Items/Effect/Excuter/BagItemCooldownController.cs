using UnityEngine;

[System.Serializable]
public class BagItemCooldownController
{
    private float bagCooldown = 3f;
    private float bagCooldownEndTime = 0f;
    private float[] slotCooldownEndTimes;
    private bool[] slotPreparationStarted;

    public void SetBagCooldown(float value) { bagCooldown = Mathf.Max(0f, value); }

    public void Init(int slotCount)
    {
        bagCooldownEndTime = 0f;

        SyncSlotCount(slotCount);
        ClearSlotCooldowns();
        ClearSlotPreparation();
    }

    public void SyncSlotCount(int slotCount)
    {
        if (slotCount < 0)
            slotCount = 0;

        if (slotCooldownEndTimes == null || slotCooldownEndTimes.Length != slotCount)
            slotCooldownEndTimes = new float[slotCount];

        if (slotPreparationStarted == null || slotPreparationStarted.Length != slotCount)
            slotPreparationStarted = new bool[slotCount];
    }

    public void ResetAllCooldowns(int slotCount)
    {
        bagCooldownEndTime = 0f;

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

    public void StartPreparationCooldownIfNeeded(int slotIndex, InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return;
        if (slotPreparationStarted == null)
            return;
        if (slotIndex < 0 || slotIndex >= slotPreparationStarted.Length)
            return;
        if (slotPreparationStarted[slotIndex])
            return;

        float cooldown = Mathf.Max(0f, item.itemData.cooldown);

        if (slotCooldownEndTimes != null && slotIndex >= 0 && slotIndex < slotCooldownEndTimes.Length)
            slotCooldownEndTimes[slotIndex] = Time.time + cooldown;

        slotPreparationStarted[slotIndex] = true;

        //Debug.Log(item.itemData.dataName + " 준비 시작 / 준비 시간: " + cooldown);
    }

    public void StartBagCooldown() { bagCooldownEndTime = Time.time + bagCooldown; }

    public bool IsBagCoolingDown() { return Time.time < bagCooldownEndTime; }

    public bool IsSlotCoolingDown(int slotIndex)
    {
        if (slotCooldownEndTimes == null)
            return false;
        if (slotIndex < 0 || slotIndex >= slotCooldownEndTimes.Length)
            return false;

        return Time.time < slotCooldownEndTimes[slotIndex];
    }

    public float GetBagCooldownRemain() { return Mathf.Max(0f, bagCooldownEndTime - Time.time); }

    public float GetBagCooldownRatio()
    {
        if (bagCooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetBagCooldownRemain() / bagCooldown);
    }

    public float GetSlotCooldownRemain(int slotIndex)
    {
        if (slotCooldownEndTimes == null)
            return 0f;
        if (slotIndex < 0 || slotIndex >= slotCooldownEndTimes.Length)
            return 0f;

        return Mathf.Max(0f, slotCooldownEndTimes[slotIndex] - Time.time);
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

        float cooldown = Mathf.Max(0f, item.itemData.cooldown);
        if (cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetSlotCooldownRemain(slotIndex) / cooldown);
    }

    private void ClearSlotCooldowns()
    {
        if (slotCooldownEndTimes == null)
            return;

        for (int i = 0; i < slotCooldownEndTimes.Length; i++)
            slotCooldownEndTimes[i] = 0f;
    }

    private void ClearSlotPreparation()
    {
        if (slotPreparationStarted == null)
            return;

        for (int i = 0; i < slotPreparationStarted.Length; i++)
            slotPreparationStarted[i] = false;
    }
}