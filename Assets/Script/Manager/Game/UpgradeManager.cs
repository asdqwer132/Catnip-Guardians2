using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    private bool isInited = false;

    [Header("Managers")]
    public ItemInitManager itemInitManager;
    public UnlockManager unlockManager;
    public ShopManager shopManager;
    public BuffSkillManager buffSkillManager;

    [Header("UI")]
    public SkillTreeUI skillTreeUI;
    public InventoryUI[] inventoryUIs;

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

        isInited = true;
    }

    private void UpgradeInit()
    {
        if (shopManager != null)
            shopManager.InitShop();


        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        bool restored = GameSession.Instance != null && GameSession.Instance.RestoreMaterialInventory(inventory);
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

    public void GoGame()
    {
        var session = GameSession.GetOrCreate();
        var data = new UpgradeToGeneralData();
        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        if (inventory != null)
            data.materialItems = GameSessionItemCopy.Copy(inventory.items);

        if (buffSkillManager != null)
        {
            foreach (RegisteredBuffSkillItem item in buffSkillManager.RegisteredBuffItems)
            {
                if (item != null && item.itemData != null)
                    data.skillItems.Add(new SkillItemSnapshot(item));
            }
        }
        session.SaveUpgradeToGeneral(data);

        var loader = sceneMoveManager != null ? sceneMoveManager : SceneMoveManager.instance;
        if (loader != null)
            loader.GoGameScene();
    }
}
