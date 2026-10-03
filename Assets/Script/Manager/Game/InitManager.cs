using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public PlantManager plantManager;
    public ItemUseManager itemUseManager;
    public BuffManager buffManager;
    public BuffSkillManager buffSkillManager;
    public SubPlantManager subPlantManager;
    public EnemyManager enemyManager;
    public GameStatisticsManager statisticsManager;

    [Header("DataCarrier")]
    public GameItemBagManager bagManager;

    [Header("UI")]
    public BagUIInitializer bagUIInitializer;
    public SelectedBagPreviewUI selectedBagPreviewUI;
    public BuffUIManager buffUIManager;

    public bool isInited = false;

    public void InitAll()
    {
        plantManager.SetPlants();
        enemyManager.Init(plantManager.CurrentPlant);
        bagManager.Init();
        if (GameSession.Instance != null)
        {
            GameSession.Instance.LoadSkillBuff(
                buffSkillManager
            );
        }
        if (!isInited)
        {
            RoundInit();
            isInited = true;    
        }
    }

    public void ResetEntity()
    {
        DamageArea.ClearAllActiveAreas();
        enemyManager.AllStop();
    }
    public void RoundInit()
    {
        //subPlantManager.ThrowAllItems();
        itemUseManager.Init();
        buffManager.ClearAllBuffs();
        buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject, 0);
        statisticsManager.ResetRound();
        if (ItemRuntimeObjectManager.Instance != null)
            ItemRuntimeObjectManager.Instance.ClearAll();

        UIInit();
    }
    private void UIInit()
    {
        bagUIInitializer.InitAll();
        selectedBagPreviewUI.Init();
        buffUIManager.Init();
        OffscreenTargetIndicatorManager.Instance.ClearAll();

    }
}