using UnityEngine;

public class InitManager : MonoBehaviour
{
    public Plant plant;
    public ItemUseManager itemUseManager;
    public void InitAll()
    {
        //ÇÃ·»Æ®¿¡ µ¥ÀÌÅÍ »ğÀÔ
        plant.Init();
        itemUseManager.Init();
        //MissileManager.instance.DestroyAllMissiles();
        EnemyManager enemyManager = EnemyManager.instance;
        enemyManager.KillAllEnemies();
        enemyManager.Init(plant.plantData.enemies);

    }
}
