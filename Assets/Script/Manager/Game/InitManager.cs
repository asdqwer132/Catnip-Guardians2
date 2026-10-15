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

    [Header("Tester")]
    public ItemTester itemTester;

    public bool isInited = false;
    public MergeGameSceneController mergeGame;

    public void InitAll()
    {
        plantManager.SetPlants();
        enemyManager.Init(plantManager.CurrentPlant);
        if (mergeGame == null && bagManager != null) bagManager.Init();

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

        if (mergeGame == null && itemTester != null) itemTester.Init();
    }

    public void ResetEntity()
    {
        DamageArea.ClearAllActiveAreas();
        enemyManager.AllStop();
    }
    public void RoundInit()
    {
        //subPlantManager.ThrowAllItems();
        if (mergeGame == null && itemUseManager != null) itemUseManager.Init();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.ClearAll();
        buffManager.ClearAllBuffs();
        buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject, 0);
        statisticsManager.ResetRound();
        if (mergeGame != null) mergeGame.InitializeRound();

        UIInit();
    }
    private void UIInit()
    {
        if (mergeGame == null)
        {
            if (bagUIInitializer != null) bagUIInitializer.InitAll();
            if (selectedBagPreviewUI != null) selectedBagPreviewUI.Init();
        }
        buffUIManager.Init();
        OffscreenTargetIndicatorManager.Instance.ClearAll();

    }
}
