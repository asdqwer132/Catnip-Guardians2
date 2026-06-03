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
        EnemyManager enemyManager = EnemyManager.instance;
        DamageArea.ClearAllActiveAreas();
        bagUIInitializer.InitAll();
        enemyManager.KillAllEnemies();
        enemyManager.Init(plantManager.CurrentPlant);
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