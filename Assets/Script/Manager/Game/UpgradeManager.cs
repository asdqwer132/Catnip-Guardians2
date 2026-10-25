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
    public EquipmentBagManager equipmentBagManager;
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

        if (equipmentBagManager != null)
            equipmentBagManager.Init();

        if (itemInitManager != null)
            itemInitManager.ApplyDefaultInventoryItems();

        if (unlockManager != null)
            unlockManager.Init();

        // 가방 복원
        if (GameSession.Instance != null &&
            GameSession.Instance.HasEquipmentBagData &&
            equipmentBagManager != null)
        {
            GameSession.Instance.LoadEquipmentBags(
                equipmentBagManager.bags
            );
        }

        // 스킬 버프 복원
        if (GameSession.Instance != null &&
            GameSession.Instance.HasSkillBuffData &&
            buffSkillManager != null)
        {
            GameSession.Instance.LoadSkillBuff(
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
        if (GameSession.Instance != null)
        {
            // 가방 저장
            if (equipmentBagManager != null)
            {
                GameSession.Instance.SaveEquipmentBags(
                    equipmentBagManager.bags
                );
            }

            // 버프 스킬 저장
            if (buffSkillManager != null)
            {
                GameSession.Instance.SaveSkillBuff(
                    buffSkillManager.RegisteredBuffItems
                );
            }
        }

        var data = new UpgradeToGeneralData();
        var inventory = materialInventory != null ? materialInventory : InventoryManager.instance;
        if (inventory != null) data.materialItems = GameSessionItemCopy.Copy(inventory.items);
        if (buffSkillManager != null)
            foreach (var item in buffSkillManager.RegisteredBuffItems)
                if (item != null && item.itemData != null)
                    data.skillItems.Add(new SkillBuffSnapshot(item));
        session.SaveUpgradeToGeneral(data);

        var loader = sceneMoveManager != null ? sceneMoveManager : SceneMoveManager.instance;
        if (loader != null) loader.GoGameScene();
    }
}
