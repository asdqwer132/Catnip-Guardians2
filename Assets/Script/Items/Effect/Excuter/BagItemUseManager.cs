using UnityEngine;

public class BagItemUseManager : MonoBehaviour
{
    [Header("Bag")]
    public EquipmentBag bag;

    [Header("Cooldown")]
    public float bagCooldown = 3f;

    [Header("Throw")]
    public ItemThrowExecutor throwExecutor;

    private BagItemUseCycle useCycle = new BagItemUseCycle();
    private BagItemCooldownController cooldownController =
        new BagItemCooldownController();

    void Awake() { Init(); }

    void Update()
    {
        if (bag == null || bag.equippedItems == null)
            return;
        if (IsBagCoolingDown())
            return;

        StartPreparationForCurrentSlot();
    }
    public BagData GetBagData()
    {
        return bag.bagData;
    }
    public void Init()
    {
        if (throwExecutor == null)
            throwExecutor = GetComponent<ItemThrowExecutor>();

        int slotCount = GetSlotCount();

        cooldownController.SetBagCooldown(bagCooldown);
        cooldownController.Init(slotCount);

        useCycle.Init(slotCount);

        StartPreparationForCurrentSlot();

        //Debug.Log("BagItemUseManager 초기화 완료 / Cycle: " + useCycle.CurrentCycleId);
    }

    public bool TryUseNextItem(Vector3 startPosition, Vector3 targetPosition, GameObject owner)
    {
        if (!CanTryUse(owner))
            return false;

        SyncControllers();

        if (IsBagCoolingDown())
            return false;

        int slotIndex = useCycle.GetNextUsableSlotIndex(bag);
        if (slotIndex == -1)
            return false;

        InventoryItem inventoryItem = bag.equippedItems[slotIndex];
        if (!ItemThrowExecutor.CanExecuteItemEffect(inventoryItem))
            return false;

        cooldownController.StartPreparationCooldownIfNeeded(            slotIndex,            inventoryItem        );
        if (cooldownController.IsSlotCoolingDown(slotIndex))
            return false;

        startPosition.z = 0f;
        targetPosition.z = 0f;
        Vector3 direction = targetPosition - startPosition;
        if (direction.sqrMagnitude <= 0.0001f) //가깝
            return false;

        direction.Normalize();

        if (throwExecutor == null)
            return false;

        throwExecutor.Throw(
            inventoryItem,
            startPosition,
            targetPosition,
            direction,
            owner,
            bag,
            useCycle.CurrentCycleId
        );

        ApplyUseResult(slotIndex);

        return true;
    }

    private bool CanTryUse(GameObject owner)
    {
        if (bag == null || bag.equippedItems == null)
            return false;
        if (owner == null)
            return false;

        return true;
    }

    private void ApplyUseResult(int slotIndex)
    {
        useCycle.MarkSlotUsedAndMoveNext(           slotIndex,            GetSlotCount()        );

        if (useCycle.HasUsedAllUsableSlotsThisCycle(bag))
        {
            cooldownController.StartBagCooldown();
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

        SyncControllers();

        int slotIndex = useCycle.GetNextUsableSlotIndex(bag);

        if (slotIndex == -1)
            return;

        InventoryItem inventoryItem = bag.equippedItems[slotIndex];

        cooldownController.StartPreparationCooldownIfNeeded(
            slotIndex,
            inventoryItem
        );
    }

    private void SyncControllers()
    {
        int slotCount = GetSlotCount();

        useCycle.SyncSlotCount(slotCount);
        cooldownController.SyncSlotCount(slotCount);
        cooldownController.SetBagCooldown(bagCooldown);
    }

    private int GetSlotCount()
    {
        if (bag == null || bag.equippedItems == null)
            return 0;

        return bag.equippedItems.Count;
    }

    public void ResetUsePosition()
    {
        int slotCount = GetSlotCount();

        useCycle.ResetUsePosition(slotCount);
        cooldownController.ResetSlotPreparation(slotCount);

        StartPreparationForCurrentSlot();

        Debug.Log(
            "아이템 사용 위치가 초기화되고, 첫 아이템 준비가 시작되었습니다. Cycle: " +
            useCycle.CurrentCycleId
        );
    }

    public void ResetAllCooldowns()
    {
        int slotCount = GetSlotCount();

        useCycle.ResetUsePosition(slotCount);
        cooldownController.ResetAllCooldowns(slotCount);

        StartPreparationForCurrentSlot();

        // Debug.Log("가방 쿨타임과 아이템 사용 순서가 초기화되었습니다.");
    }

    public int GetNextReadyUsableSlotIndexForUI()
    {
        SyncControllers();
        return useCycle.GetNextUsableSlotIndex(bag);
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

    public bool IsBagCoolingDown()
    {
        return cooldownController.IsBagCoolingDown();
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
        return cooldownController.IsSlotCoolingDown(slotIndex);
    }

    public float GetBagCooldownRemain()
    {
        return cooldownController.GetBagCooldownRemain();
    }

    public float GetBagCooldownRatio()
    {
        return cooldownController.GetBagCooldownRatio();
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
        return cooldownController.GetSlotCooldownRemain(slotIndex);
    }

    public float GetSlotCooldownRatio(int slotIndex)
    {
        return cooldownController.GetSlotCooldownRatio(
            bag,
            slotIndex
        );
    }
}