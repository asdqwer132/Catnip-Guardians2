using UnityEngine;

public class BagItemUseManager : MonoBehaviour
{
    [Header("Bag")]
    public EquipmentBag bag;

    [Header("Cooldown")]
    public float bagCooldown = 3f;

    [Header("Throw")]
    public ItemThrowExecutor throwExecutor;

    private float bagCooldownEndTime = 0f;

    private float nextItemUseTime = 0f;
    private float currentItemCooldownDuration = 0f;

    private float[] slotCooldownEndTimes;
    private bool[] slotPreparationStarted;

    private int currentSlotIndex = 0;
    private bool[] usedSlotInCycle;

    private int currentCycleId = 0;

    void Awake()
    {
        Init();
    }

    void Update()
    {
        if (bag == null || bag.equippedItems == null)
            return;

        if (IsBagCoolingDown())
            return;

        StartPreparationForCurrentSlot();
    }

    public void Init()
    {
        if (throwExecutor == null)
            throwExecutor = GetComponent<ItemThrowExecutor>();

        bagCooldownEndTime = 0f;

        SyncSlotArrays();

        currentSlotIndex = 0;
        currentCycleId++;

        if (usedSlotInCycle != null)
        {
            for (int i = 0; i < usedSlotInCycle.Length; i++)
                usedSlotInCycle[i] = false;
        }

        if (slotPreparationStarted != null)
        {
            for (int i = 0; i < slotPreparationStarted.Length; i++)
                slotPreparationStarted[i] = false;
        }

        if (slotCooldownEndTimes != null)
        {
            for (int i = 0; i < slotCooldownEndTimes.Length; i++)
                slotCooldownEndTimes[i] = 0f;
        }

        nextItemUseTime = 0f;
        currentItemCooldownDuration = 0f;

        StartPreparationForCurrentSlot();

        Debug.Log("BagItemUseManager 초기화 완료 / 아이템 사용 순서 초기화 / Cycle: " + currentCycleId);
    }

    public void SetBag(EquipmentBag newBag)
    {
        bag = newBag;

        Init();
    }

    public bool TryUseNextItem(
        Vector3 startPosition,
        Vector3 targetPosition,
        GameObject owner
    )
    {
        if (!CanTryUse(owner))
            return false;

        SyncSlotArrays();

        if (IsBagCoolingDown())
        {
            Debug.Log("가방 쿨타임 중입니다. 남은 시간: " + GetBagCooldownRemain().ToString("F1"));
            return false;
        }

        int slotIndex = GetNextUsableSlotIndex();

        if (slotIndex == -1)
        {
            Debug.LogWarning("사용 가능한 아이템이 없습니다.");
            return false;
        }

        InventoryItem inventoryItem = bag.equippedItems[slotIndex];

        if (!ItemThrowExecutor.CanExecuteItemEffect(inventoryItem))
        {
            Debug.LogWarning("아이템 효과가 없어서 사용할 수 없습니다.");
            return false;
        }

        StartPreparationCooldownIfNeeded(
            slotIndex,
            inventoryItem
        );

        if (IsSlotCoolingDown(slotIndex))
        {
            Debug.Log(
                inventoryItem.itemData.itemName +
                " 준비 중입니다. 남은 시간: " +
                GetSlotCooldownRemain(slotIndex).ToString("F1")
            );

            return false;
        }

        startPosition.z = 0f;
        targetPosition.z = 0f;

        Vector3 direction = targetPosition - startPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning("시작 위치와 목표 위치가 너무 가깝습니다.");
            return false;
        }

        direction.Normalize();

        if (throwExecutor == null)
        {
            Debug.LogWarning("ItemThrowExecutor가 없습니다.");
            return false;
        }

        throwExecutor.Throw(
            inventoryItem,
            startPosition,
            targetPosition,
            direction,
            owner,
            bag,
            currentCycleId
        );

        Debug.Log(
            bag.bagName +
            " 슬롯 " +
            slotIndex +
            " 아이템 사용: " +
            inventoryItem.itemData.itemName
        );

        ApplyUseResult(slotIndex);

        return true;
    }

    public bool TryUseNextItem(
        Vector3 targetPosition,
        GameObject owner
    )
    {
        if (owner == null)
            return false;

        Vector3 startPosition = owner.transform.position;
        startPosition.z = 0f;

        targetPosition.z = 0f;

        return TryUseNextItem(
            startPosition,
            targetPosition,
            owner
        );
    }

    private bool CanTryUse(GameObject owner)
    {
        if (bag == null || bag.equippedItems == null)
        {
            Debug.LogWarning("가방이 없습니다.");
            return false;
        }

        if (owner == null)
        {
            Debug.LogWarning("아이템 사용자 오브젝트가 없습니다.");
            return false;
        }

        return true;
    }

    private void ApplyUseResult(int slotIndex)
    {
        usedSlotInCycle[slotIndex] = true;

        currentSlotIndex = slotIndex + 1;

        if (currentSlotIndex >= bag.equippedItems.Count)
            currentSlotIndex = 0;

        if (HasUsedAllUsableSlotsThisCycle())
        {
            Debug.Log(bag.bagName + "의 아이템을 한 바퀴 전부 사용했습니다. 가방 쿨타임 시작.");

            StartBagCooldown();
            ResetUsePosition();

            return;
        }

        StartPreparationForCurrentSlot();
    }

    private void StartPreparationForCurrentSlot()
    {
        if (bag == null || bag.equippedItems == null)
            return;

        if (IsBagCoolingDown())
            return;

        SyncSlotArrays();

        int slotIndex = GetNextUsableSlotIndex();

        if (slotIndex == -1)
            return;

        InventoryItem inventoryItem = bag.equippedItems[slotIndex];

        StartPreparationCooldownIfNeeded(
            slotIndex,
            inventoryItem
        );
    }

    private void StartPreparationCooldownIfNeeded(
        int slotIndex,
        InventoryItem item
    )
    {
        if (item == null || item.itemData == null)
            return;

        if (slotPreparationStarted == null)
            return;

        if (slotIndex < 0 || slotIndex >= slotPreparationStarted.Length)
            return;

        if (slotPreparationStarted[slotIndex])
            return;

        float cooldown = Mathf.Max(
            0f,
            item.itemData.cooldown
        );

        currentItemCooldownDuration = cooldown;
        nextItemUseTime = Time.time + cooldown;

        if (slotCooldownEndTimes != null &&
            slotIndex >= 0 &&
            slotIndex < slotCooldownEndTimes.Length)
        {
            slotCooldownEndTimes[slotIndex] = Time.time + cooldown;
        }

        slotPreparationStarted[slotIndex] = true;

        Debug.Log(item.itemData.itemName + " 준비 시작 / 준비 시간: " + cooldown);
    }

    private void SyncSlotArrays()
    {
        if (bag == null || bag.equippedItems == null)
            return;

        int slotCount = bag.equippedItems.Count;

        if (slotCooldownEndTimes == null || slotCooldownEndTimes.Length != slotCount)
            slotCooldownEndTimes = new float[slotCount];

        if (slotPreparationStarted == null || slotPreparationStarted.Length != slotCount)
            slotPreparationStarted = new bool[slotCount];

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

    public void ResetUsePosition()
    {
        currentCycleId++;

        currentSlotIndex = 0;

        if (usedSlotInCycle != null)
        {
            for (int i = 0; i < usedSlotInCycle.Length; i++)
                usedSlotInCycle[i] = false;
        }

        if (slotPreparationStarted != null)
        {
            for (int i = 0; i < slotPreparationStarted.Length; i++)
                slotPreparationStarted[i] = false;
        }

        if (slotCooldownEndTimes != null)
        {
            for (int i = 0; i < slotCooldownEndTimes.Length; i++)
                slotCooldownEndTimes[i] = 0f;
        }

        nextItemUseTime = 0f;
        currentItemCooldownDuration = 0f;

        StartPreparationForCurrentSlot();

        Debug.Log("아이템 사용 위치가 초기화되고, 첫 아이템 준비가 시작되었습니다. Cycle: " + currentCycleId);
    }

    public void ResetAllCooldowns()
    {
        bagCooldownEndTime = 0f;

        nextItemUseTime = 0f;
        currentItemCooldownDuration = 0f;

        currentSlotIndex = 0;
        currentCycleId++;

        if (usedSlotInCycle != null)
        {
            for (int i = 0; i < usedSlotInCycle.Length; i++)
                usedSlotInCycle[i] = false;
        }

        if (slotCooldownEndTimes != null)
        {
            for (int i = 0; i < slotCooldownEndTimes.Length; i++)
                slotCooldownEndTimes[i] = 0f;
        }

        if (slotPreparationStarted != null)
        {
            for (int i = 0; i < slotPreparationStarted.Length; i++)
                slotPreparationStarted[i] = false;
        }

        StartPreparationForCurrentSlot();

        Debug.Log("가방 쿨타임과 아이템 사용 순서가 초기화되고, 첫 아이템 준비가 시작되었습니다.");
    }

    private int GetNextUsableSlotIndex()
    {
        if (bag == null || bag.equippedItems == null)
            return -1;

        SyncSlotArrays();

        int slotCount = bag.equippedItems.Count;

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

    public int GetNextReadyUsableSlotIndexForUI()
    {
        return GetNextUsableSlotIndex();
    }

    public InventoryItem GetNextUsableInventoryItemForUI()
    {
        int index = GetNextReadyUsableSlotIndexForUI();

        if (index == -1)
            return null;

        if (bag == null || bag.equippedItems == null)
            return null;

        if (index < 0 || index >= bag.equippedItems.Count)
            return null;

        return bag.equippedItems[index];
    }

    private bool HasUsedAllUsableSlotsThisCycle()
    {
        if (bag == null || bag.equippedItems == null)
            return false;

        if (usedSlotInCycle == null || usedSlotInCycle.Length != bag.equippedItems.Count)
            return false;

        bool hasUsableItem = false;

        for (int i = 0; i < bag.equippedItems.Count; i++)
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

    private bool IsUsableItem(InventoryItem item)
    {
        return item != null &&
               item.itemData != null &&
               item.amount > 0;
    }

    private void StartBagCooldown()
    {
        bagCooldownEndTime = Time.time + bagCooldown;
    }

    public bool IsBagCoolingDown()
    {
        return Time.time < bagCooldownEndTime;
    }

    public bool IsNextItemUseCoolingDown()
    {
        int index = GetNextReadyUsableSlotIndexForUI();

        if (index == -1)
            return false;

        return IsSlotCoolingDown(index);
    }

    public bool IsSlotCoolingDown(int slotIndex)
    {
        if (slotCooldownEndTimes == null)
            return false;

        if (slotIndex < 0 || slotIndex >= slotCooldownEndTimes.Length)
            return false;

        return Time.time < slotCooldownEndTimes[slotIndex];
    }

    public float GetBagCooldownRemain()
    {
        return Mathf.Max(
            0f,
            bagCooldownEndTime - Time.time
        );
    }

    public float GetBagCooldownRatio()
    {
        if (bagCooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(
            GetBagCooldownRemain() / bagCooldown
        );
    }

    public float GetNextItemUseCooldownRemain()
    {
        int index = GetNextReadyUsableSlotIndexForUI();

        if (index == -1)
            return 0f;

        return GetSlotCooldownRemain(index);
    }

    public float GetNextItemUseCooldownRatio()
    {
        int index = GetNextReadyUsableSlotIndexForUI();

        if (index == -1)
            return 0f;

        return GetSlotCooldownRatio(index);
    }

    public float GetSlotCooldownRemain(int slotIndex)
    {
        if (slotCooldownEndTimes == null)
            return 0f;

        if (slotIndex < 0 || slotIndex >= slotCooldownEndTimes.Length)
            return 0f;

        return Mathf.Max(
            0f,
            slotCooldownEndTimes[slotIndex] - Time.time
        );
    }

    public float GetSlotCooldownRatio(int slotIndex)
    {
        if (bag == null || bag.equippedItems == null)
            return 0f;

        if (slotIndex < 0 || slotIndex >= bag.equippedItems.Count)
            return 0f;

        InventoryItem item = bag.equippedItems[slotIndex];

        if (item == null || item.itemData == null)
            return 0f;

        float cooldown = Mathf.Max(
            0f,
            item.itemData.cooldown
        );

        if (cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(
            GetSlotCooldownRemain(slotIndex) / cooldown
        );
    }

    public int GetCurrentCycleId()
    {
        return currentCycleId;
    }
}