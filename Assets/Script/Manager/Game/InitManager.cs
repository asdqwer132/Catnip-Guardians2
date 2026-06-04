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

    public void InitAll()
    {
        plantManager.SetPlaints();
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
        selectedBagPreviewUI.Init();
        skillTreeUI.Init();
        EquipmentBagManager.instance.Init();
        itemInitManager.ApplyDefaultInventoryItems();
    }
}