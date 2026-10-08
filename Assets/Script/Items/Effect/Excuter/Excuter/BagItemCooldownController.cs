using UnityEngine;

[System.Serializable]
public class BagItemCooldownController
{
    private float bagCooldown = 3f;
    private float bagCooldownRemain = 0f;
    private float bagCooldownDuration;
    private float[] slotCooldownRemains;
    private float[] slotCooldownDurations;
    private bool[] slotPreparationStarted;

    // 준비 상태를 지우는 Reset과 다르다. 게임 효과는 다음 사용을 준비 완료로 만든다.
    public void MakeSlotReady(int slotIndex)
    {
        if (slotCooldownRemains == null || slotIndex < 0 || slotIndex >= slotCooldownRemains.Length) return;
        slotCooldownRemains[slotIndex] = 0f;
        slotPreparationStarted[slotIndex] = true;
    }

    public void ChangeSlotCooldown(int slotIndex, CooldownOperation operation, float amount)
    {
        if (slotCooldownRemains == null || slotIndex < 0 || slotIndex >= slotCooldownRemains.Length) return;
        if (operation == CooldownOperation.Ready) { MakeSlotReady(slotIndex); return; }
        slotCooldownRemains[slotIndex] = ChangeRemaining(slotCooldownRemains[slotIndex], operation, amount);
    }

    public void ChangeBagCooldown(CooldownOperation operation, float amount)
        => bagCooldownRemain = ChangeRemaining(bagCooldownRemain, operation, amount);

    public static float ChangeRemaining(float remaining, CooldownOperation operation, float amount)
    {
        remaining = EffectStatUtility.Safe(remaining, 0f, 1000000f, 0f);
        amount = EffectStatUtility.Safe(amount, 0f, 1000000f, 0f);
        switch (operation)
        {
            case CooldownOperation.ReduceSeconds: return Mathf.Max(0f, remaining - amount);
            case CooldownOperation.ReduceFraction: return remaining * (1f - Mathf.Clamp01(amount));
            case CooldownOperation.Ready: return 0f;
            default: return remaining;
        }
    }

    public void Init(int slotCount)
    {
        bagCooldownRemain = 0f;
        bagCooldownDuration = 0f;

        SyncSlotCount(slotCount);
        ClearSlotCooldowns();
        ClearSlotPreparation();
    }

    public void SetBagCooldown(float value)
    {
        bagCooldown = EffectStatUtility.Safe(value, 0f, 1000000f, 0f);
    }

    public void SyncSlotCount(int slotCount)
    {
        if (slotCount < 0)
            slotCount = 0;

        if (slotCooldownRemains == null)
            slotCooldownRemains = new float[slotCount];

        if (slotCooldownDurations == null)
            slotCooldownDurations = new float[slotCount];

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

        if (slotCooldownDurations.Length != slotCount)
        {
            float[] newSlotCooldownDurations = new float[slotCount];
            int copyCount = Mathf.Min(slotCooldownDurations.Length, newSlotCooldownDurations.Length);
            for (int i = 0; i < copyCount; i++)
                newSlotCooldownDurations[i] = slotCooldownDurations[i];
            slotCooldownDurations = newSlotCooldownDurations;
        }
    }

    public void TickCooldown(float deltaTime, float itemRecoveryRate = 1f, float bagRecoveryRate = 1f)
        => TickCooldownInternal(deltaTime, itemRecoveryRate, null, bagRecoveryRate);

    public void TickCooldown(float deltaTime, System.Func<int, float> getItemRecoveryRate, float bagRecoveryRate = 1f)
        => TickCooldownInternal(deltaTime, 1f, getItemRecoveryRate, bagRecoveryRate);

    private void TickCooldownInternal(float deltaTime, float itemRecoveryRate,
        System.Func<int, float> getItemRecoveryRate, float bagRecoveryRate)
    {
        deltaTime = EffectStatUtility.Safe(deltaTime, 0f, 1000000f, 0f);
        if (deltaTime <= 0f)
            return;

        if (bagCooldownRemain > 0f)
        {
            bagRecoveryRate = EffectStatUtility.Safe(bagRecoveryRate, 0f, 100f, 1f);
            bagCooldownRemain -= deltaTime * bagRecoveryRate;

            if (bagCooldownRemain < 0f)
                bagCooldownRemain = 0f;
        }

        if (slotCooldownRemains == null)
            return;

        for (int i = 0; i < slotCooldownRemains.Length; i++)
        {
            if (slotCooldownRemains[i] <= 0f)
                continue;

            float rate = getItemRecoveryRate != null ? getItemRecoveryRate(i) : itemRecoveryRate;
            rate = EffectStatUtility.Safe(rate, 0f, 100f, 1f);
            slotCooldownRemains[i] -= deltaTime * rate;

            if (slotCooldownRemains[i] < 0f)
                slotCooldownRemains[i] = 0f;
        }
    }

    public void ResetAllCooldowns(int slotCount)
    {
        bagCooldownRemain = 0f;
        bagCooldownDuration = 0f;

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
        if (item != null)
            StartPreparationCooldownIfNeeded(slotIndex, item.Cooldown);
    }

    public bool HasStartedPreparation(int slotIndex)
        => slotPreparationStarted != null && slotIndex >= 0 &&
           slotIndex < slotPreparationStarted.Length && slotPreparationStarted[slotIndex];

    public void StartPreparationCooldownIfNeeded(int slotIndex, float cooldown)
    {
        if (slotPreparationStarted == null)
            return;
        if (slotIndex < 0 || slotIndex >= slotPreparationStarted.Length)
            return;
        if (slotPreparationStarted[slotIndex])
            return;

        cooldown = EffectStatUtility.Safe(cooldown, 0f, 1000000f, 0f);

        if (slotCooldownRemains != null && slotIndex >= 0 && slotIndex < slotCooldownRemains.Length)
        {
            slotCooldownRemains[slotIndex] = cooldown;
            slotCooldownDurations[slotIndex] = cooldown;
        }

        slotPreparationStarted[slotIndex] = true;
    }

    public void StartBagCooldown()
        => StartBagCooldown(bagCooldown);

    public void StartBagCooldown(float cooldown)
    {
        bagCooldownDuration = EffectStatUtility.Safe(cooldown, 0f, 1000000f, 0f);
        bagCooldownRemain = bagCooldownDuration;
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
        if (bagCooldownDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(GetBagCooldownRemain() / bagCooldownDuration);
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

        return GetSlotCooldownRatio(slotIndex);
    }

    public float GetSlotCooldownRatio(int slotIndex)
    {
        if (slotCooldownDurations == null || slotIndex < 0 || slotIndex >= slotCooldownDurations.Length)
            return 0f;

        // 버프 만료 후에도 진행 중인 시계의 분모는 시작할 때의 시간으로 유지한다.
        float cooldown = slotCooldownDurations[slotIndex];
        if (cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetSlotCooldownRemain(slotIndex) / cooldown);
    }

    public void RecalculateBagCooldown(float newCooldown)
    {
        newCooldown = EffectStatUtility.Safe(
            newCooldown, 0f, 1000000f, 0f);

        if (bagCooldownRemain <= 0f)
            return;

        // 기존 전체 쿨타임과 새로운 쿨타임의 차이만 적용
        float difference = newCooldown - bagCooldownDuration;

        bagCooldownRemain = Mathf.Max(
            0f,
            bagCooldownRemain + difference
        );

        // bagCooldownDuration은 변경하지 않음
    }

    public void RecalculateSlotCooldown(int slotIndex, float newCooldown)
    {
        if (slotCooldownRemains == null ||
            slotCooldownDurations == null ||
            slotPreparationStarted == null)
            return;

        if (slotIndex < 0 || slotIndex >= slotCooldownRemains.Length)
            return;

        if (!slotPreparationStarted[slotIndex])
            return;

        if (slotCooldownRemains[slotIndex] <= 0f)
            return;

        newCooldown = EffectStatUtility.Safe(
            newCooldown, 0f, 1000000f, 0f);

        float difference =
            newCooldown - slotCooldownDurations[slotIndex];

        slotCooldownRemains[slotIndex] = Mathf.Max(
            0f,
            slotCooldownRemains[slotIndex] + difference
        );

        // slotCooldownDurations은 변경하지 않음
    }
    private void ClearSlotCooldowns()
    {
        if (slotCooldownRemains == null)
            return;

        for (int i = 0; i < slotCooldownRemains.Length; i++)
        {
            slotCooldownRemains[i] = 0f;
            slotCooldownDurations[i] = 0f;
        }
    }

    private void ClearSlotPreparation()
    {
        if (slotPreparationStarted == null)
            return;

        for (int i = 0; i < slotPreparationStarted.Length; i++)
            slotPreparationStarted[i] = false;
    }
}
