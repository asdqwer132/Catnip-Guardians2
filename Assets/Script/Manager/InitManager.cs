using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public Plant plant;
    public ItemUseManager itemUseManager;
    public ShopManager shopManager;

    public void InitAll()
    {
        plant.Init();
        itemUseManager.Init();
        shopManager.InitShop();
        EnemyManager enemyManager = EnemyManager.instance;
        enemyManager.KillAllEnemies();
        enemyManager.Init(plant.plantData.enemies);
    }
}