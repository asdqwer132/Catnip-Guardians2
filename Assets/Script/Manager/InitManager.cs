using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public Plant plant;
    public ItemUseManager itemUseManager;
    public ShopManager shopManager;
    public BuffManager buffManager;
    public BuffSkillManager buffSkillManager;

    [Header("UI")]
    public SkillTreeUI skillTreeUI;
    public SelectedBagPreviewUI selectedBagPreviewUI;

    public void InitAll()
    {
        plant.Init();
        itemUseManager.Init();
        shopManager.InitShop();
        buffManager.ClearAllBuffs();
        buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject, 0);
        EnemyManager enemyManager = EnemyManager.instance;
        DamageArea.ClearAllActiveAreas();
        enemyManager.KillAllEnemies();
        enemyManager.Init(plant.plantData.enemies);
    }

    public void FirstInit()
    {
        selectedBagPreviewUI.Init();
        skillTreeUI.Init();
        EquipmentBagManager.instance.Init();
    }
}