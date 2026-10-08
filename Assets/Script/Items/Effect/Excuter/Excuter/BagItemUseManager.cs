using UnityEngine;

public class BagItemUseManager : MonoBehaviour, IDynamicBuffReceiver
{
    [Header("Bag")]
    public EquipmentBag bag;

    [Header("Cooldown")]
    public float bagCooldown = 3f;
    public Player cooldownOwner;

    [Header("Throw")]
    public ItemThrowExecutor throwExecutor;

    private BagItemUseCycle useCycle = new BagItemUseCycle();
    private BagItemCooldownController cooldownController =
        new BagItemCooldownController();

    private bool canTickCooldown = false;
    private System.Func<int, float> slotRecoveryRate;
    private float playerRecoveryRate = 1f;
    public static readonly System.Collections.Generic.List<BagItemUseManager> ActiveManagers =
        new System.Collections.Generic.List<BagItemUseManager>();
    private void OnEnable() { if (!ActiveManagers.Contains(this)) ActiveManagers.Add(this); }
    private void OnDisable() { ActiveManagers.Remove(this); }

    public void ApplyCooldownControl(CooldownControlEffect effect)
    {
        if (effect == null || bag == null || bag.equippedItems == null) return;
        SyncControllers();
        if (effect.affectBagCooldown) cooldownController.ChangeBagCooldown(effect.operation, effect.amount);
        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            ItemData item = bag.equippedItems[i] != null ? bag.equippedItems[i].itemData : null;
            if (!effect.Matches(item)) continue;
            StartItemPreparation(i, item);
            cooldownController.ChangeSlotCooldown(i, effect.operation, effect.amount);
        }
    }

    private void Awake()
    {
        Init();
    }
    
    void Start()
    {
        BuffManager.instance.RegisterDynamicBuffReceiver(this);
    }

    public BagData GetBagData()
    {
        if (bag == null)
            return null;

        return bag.bagData;
    }

    public void SetCooldownTickEnabled(bool enabled)
    {
        canTickCooldown = enabled;
    }

    public void Init()
    {
        if (cooldownOwner == null) cooldownOwner = GetComponentInParent<Player>();
        if (throwExecutor == null)
            throwExecutor = GetComponent<ItemThrowExecutor>();

        int slotCount = GetSlotCount();

        cooldownController.SetBagCooldown(bagCooldown);
        cooldownController.Init(slotCount);

        useCycle.Init(slotCount);

        StartPreparationForCurrentSlot();
    }

    public void TickCooldown(float deltaTime)
    {
        if (!canTickCooldown)
            return;

        if (deltaTime <= 0f)
            return;

        if (bag == null || bag.equippedItems == null)
            return;

        SyncControllers();

        playerRecoveryRate = GetPlayerCooldownRecoveryRate();
        if (slotRecoveryRate == null)
            slotRecoveryRate = GetSlotCooldownRecoveryRate;
        float bagRate = BagCooldownStat.Resolve(bagCooldown, bag, GetCooldownBuffManager()).cooldownRecoveryRate;
        cooldownController.TickCooldown(deltaTime, slotRecoveryRate, bagRate);

        if (IsBagCoolingDown())
            return;

        StartPreparationForCurrentSlot();
    }

    public bool TryUseNextItem(
        Vector3 startPosition,
        Vector3 targetPosition,
        GameObject owner
    )
    {
        if (!CanTryUse(owner))
            return false;

        if (cooldownOwner == null)
            cooldownOwner = owner.GetComponentInParent<Player>();

        SyncControllers();

        if (IsBagCoolingDown())
            return false;

        int slotIndex = useCycle.GetNextUsableSlotIndex(bag);

        if (slotIndex == -1)
            return false;

        ItemData inventoryItem = bag.equippedItems[slotIndex].itemData;

        if (!ItemEffectExecutor.CanExecuteItemEffect(inventoryItem))
            return false;

        StartItemPreparation(slotIndex, inventoryItem);

        if (cooldownController.IsSlotCoolingDown(slotIndex))
            return false;

        if (throwExecutor == null)
            return false;

        throwExecutor.Throw(
            inventoryItem,
            startPosition,
            targetPosition,
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
        useCycle.MarkSlotUsedAndMoveNext(
            slotIndex,
            GetSlotCount()
        );

        if (useCycle.HasUsedAllUsableSlotsThisCycle(bag))
        {
            cooldownController.StartBagCooldown(GetBagCooldown());
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

        ItemData inventoryItem = bag.equippedItems[slotIndex].itemData;

        StartItemPreparation(slotIndex, inventoryItem);
    }

    private void StartItemPreparation(int slotIndex, ItemData item)
    {
        if (item == null || cooldownController.HasStartedPreparation(slotIndex))
            return;

        cooldownController.StartPreparationCooldownIfNeeded(slotIndex, GetItemCooldown(item));
    }

    public float GetItemCooldown(ItemData item)
        => ItemCooldownStat.Resolve(item, bag, GetCooldownBuffManager()).cooldown;

    public float GetBagCooldown()
        => BagCooldownStat.Resolve(bagCooldown, bag, GetCooldownBuffManager()).cooldown;

    private BuffManager GetCooldownBuffManager()
    {
        BuffManager manager = null;
        if (throwExecutor != null && throwExecutor.itemEffectExecutor != null)
            manager = throwExecutor.itemEffectExecutor.buffManager;
        if (manager == null && cooldownOwner != null)
            manager = cooldownOwner.buffManager;
        if (manager == null)
            manager = BuffManager.instance;
        return manager;
    }

    private float GetPlayerCooldownRecoveryRate()
    {
        if (cooldownOwner == null)
            return 1f;

        BuffManager manager = GetCooldownBuffManager();
        PlayerStat stat = cooldownOwner.baseStat;
        // Player의 초기 등록 순서와 관계없이 현재 버프를 조회한다. 재조회는 횟수를 소비하지 않는다.
        if (manager != null && stat != null)
            stat = manager.GetBuffedStatForTarget(stat, cooldownOwner, BuffCalculationMode.All, false);
        else
            stat = cooldownOwner.currentStat ?? stat;
        return stat != null ? EffectStatUtility.Safe(stat.cooldownRecoveryRate, 0f, 100f, 1f) : 1f;
    }

    private float GetSlotCooldownRecoveryRate(int slotIndex)
    {
        ItemData item = bag != null && bag.equippedItems != null && slotIndex >= 0 &&
            slotIndex < bag.equippedItems.Count && bag.equippedItems[slotIndex] != null
            ? bag.equippedItems[slotIndex].itemData : null;
        ItemCooldownStat stat = ItemCooldownStat.Resolve(item, bag, GetCooldownBuffManager());
        return stat.cooldownRecoveryRate * playerRecoveryRate;
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
    }

    public void ResetAllCooldowns()
    {
        int slotCount = GetSlotCount();

        useCycle.ResetUsePosition(slotCount);
        cooldownController.ResetAllCooldowns(slotCount);

        StartPreparationForCurrentSlot();
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

    public void OnDynamicBuffChanged()
    {
        if (bag == null || bag.equippedItems == null)
            return;

        SyncControllers();

        // 가방 쿨타임 재계산
        cooldownController.RecalculateBagCooldown(
            GetBagCooldown()
        );

        // 각 슬롯의 쿨타임 재계산
        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            ItemData item = bag.equippedItems[i]?.itemData;

            if (item == null)
                continue;

            cooldownController.RecalculateSlotCooldown(
                i,
                GetItemCooldown(item)
            );
        }
    }
}