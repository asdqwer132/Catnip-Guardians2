using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public PlantManager plantManager;
    public ItemUseManager itemUseManager;
    public ShopManager shopManager;
    public BuffManager buffManager;
    public BuffSkillManager buffSkillManager;
    public SubPlantManager subPlantManager;
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

        //?
        shopManager.InitShop();

        plantManager.SetPlants();
        enemyManager.Init(plantManager.CurrentPlant);


        itemUseManager.Init();
        buffManager.ClearAllBuffs();
        itemInitManager.ApplyDefaultInventoryItems();
        buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject, 0);

        subPlantManager.ThrowAllItems();
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

        UIInit();
    }
    private void UIInit()
    {
        bagUIInitializer.InitAll();
        selectedBagPreviewUI.Init();
        skillTreeUI.Init();
        buffUIManager.Init();

        foreach (var item in inventoryUIs)
        {
            item.Init();
        }
    }
}