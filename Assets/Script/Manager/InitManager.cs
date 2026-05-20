using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public Plant plant;
    public ItemUseManager itemUseManager;
    public ShopManager shopManager;
    public BuffManager buffManager;

    [Header("UI")]
    public SkillTreeUI skillTreeUI;

    public void InitAll()
    {
        plant.Init();
        itemUseManager.Init();
        shopManager.InitShop();
        buffManager.ClearAllBuffs();
        EnemyManager enemyManager = EnemyManager.instance;
        enemyManager.KillAllEnemies();
        enemyManager.Init(plant.plantData.enemies);
    }

    public void FirstInit()
    {
        skillTreeUI.Init();
        EquipmentBagManager.instance.Init();
    }
}