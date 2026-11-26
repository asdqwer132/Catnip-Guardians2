using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    private bool isInited = false;
    private bool captureQueued;

    [Header("Managers")]
    public ItemInitManager itemInitManager;
    public UnlockManager unlockManager;
    public ShopManager shopManager;
    public BuffSkillManager buffSkillManager;
    public MergeBoxUpgradeManager mergeBoxUpgrades;
    public PotionEquipmentManager potionEquipment;

    [Header("UI")]
    public SkillTreeUI skillTreeUI;
    public InventoryUI[] inventoryUIs;
    [SerializeField] private BoardEquipmentPanelUI boardEquipment;

    [Header("DataCarrier")]
    public SceneMoveManager sceneMoveManager;
    [Tooltip("제너럴/아이템 씬에 전달할 재료 인벤토리. 비워두면 현재 InventoryManager를 사용합니다.")]
    public InventoryManager materialInventory;

    private void Start()
    {
        if (isInited)
            return;

        UpgradeInit();
        UIInit();
        if (mergeBoxUpgrades != null)
            RefreshBroadcaster.Instance?.Broadcast(RefreshType.SkillTree | RefreshType.Unlock | RefreshType.Shop | RefreshType.Inventory | RefreshType.Equipment);

        isInited = true;
        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        if (inventory != null) inventory.onInventoryChanged += QueueCapture;
        if (potionEquipment != null) potionEquipment.Changed += CaptureProgress;
        if (mergeBoxUpgrades != null && mergeBoxUpgrades.skillTree != null) mergeBoxUpgrades.skillTree.OnSkillTreeChanged += CaptureProgress;
        CaptureProgress();
    }

    private void UpgradeInit()
    {
        if (shopManager != null)
            shopManager.InitShop();


        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        bool restored = GameSession.Instance != null && GameSession.Instance.TryRestoreUpgradeInventory(inventory);
        if (!restored && itemInitManager != null)
            itemInitManager.ApplyDefaultInventoryItems();

        if (unlockManager != null)
            unlockManager.Init();

        // 스킬트리에서 등록한 아이템 복원
        if (GameSession.Instance != null &&
            GameSession.Instance.HasSkillItems &&
            buffSkillManager != null)
        {
            GameSession.Instance.LoadSkillItems(
                buffSkillManager
            );
        }
    }

    private void UIInit()
    {
        mergeBoxUpgrades?.Initialize();
        if (boardEquipment != null)
        {
            var unlockedNodes = mergeBoxUpgrades != null ? mergeBoxUpgrades.CaptureUnlockedNodes() : null;
            boardEquipment.InitializeLoadout(MergeBoxUpgradeManager.CaptureChestUnlocks(unlockedNodes));
        }
        if (potionEquipment != null && GameSession.Instance != null && GameSession.Instance.TryReadGeneralData(out var data))
            potionEquipment.RestoreSlots(data.potionSlots);
        if (skillTreeUI != null)
            skillTreeUI.Init();

        if (inventoryUIs == null)
            return;

        foreach (InventoryUI item in inventoryUIs)
        {
            if (item != null)
                item.Init();
        }
    }

    private void OnDestroy()
    {
        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        if (inventory != null) inventory.onInventoryChanged -= QueueCapture;
        if (potionEquipment != null) potionEquipment.Changed -= CaptureProgress;
        if (mergeBoxUpgrades != null && mergeBoxUpgrades.skillTree != null) mergeBoxUpgrades.skillTree.OnSkillTreeChanged -= CaptureProgress;
    }



    public void GoGame()
    {
        CaptureProgress();
        var loader = sceneMoveManager != null ? sceneMoveManager : SceneMoveManager.instance;
        if (loader != null) loader.GoGameScene();
    }

    private void QueueCapture() => captureQueued = true;

    private void LateUpdate()
    {
        if (captureQueued) CaptureProgress();
    }

    private void CaptureProgress()
    {
        captureQueued = false;
        GameSession.GetOrCreate().CaptureUpgradeProgress(
            mergeBoxUpgrades, potionEquipment, materialInventory != null ? materialInventory : InventoryManager.instance, buffSkillManager);
    }
}
