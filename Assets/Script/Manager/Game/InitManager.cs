using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public PlantManager plantManager;
    public ItemUseManager itemUseManager;
    public ShopManager shopManager;
    public BuffManager buffManager;
    public BuffSkillManager buffSkillManager;
    public ItemInitManager itemInitManager;
    public UnlockManager unlockManager;
    public EnemyManager enemyManager;

    [Header("UI")]
    public SkillTreeUI skillTreeUI;
    public BagUIInitializer bagUIInitializer;
    public SelectedBagPreviewUI selectedBagPreviewUI;
    public BuffUIManager buffUIManager;
    public InventoryUI[] inventoryUIs;

    public void InitAll()
    {
        plantManager.SetPlants();
        itemUseManager.Init();
        shopManager.InitShop();
        buffManager.ClearAllBuffs();
        buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject, 0);
        bagUIInitializer.InitAll();
        enemyManager.Init(plantManager.CurrentPlant);
    }

    public void ResetEntity()
    {
        DamageArea.ClearAllActiveAreas();
        enemyManager.AllStop();
    }
    public void FirstInit()
    {
        unlockManager.Init();
        EquipmentBagManager.instance.Init();
       
        itemInitManager.ApplyDefaultInventoryItems();

        UIInit();
    }
    private void UIInit()
    {
        selectedBagPreviewUI.Init();
        skillTreeUI.Init();
        buffUIManager.Init();

        foreach (var item in inventoryUIs)
        {
            item.Init();
        }
    }
}