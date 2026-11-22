using UnityEngine;

public class InitManager : MonoBehaviour
{
    [Header("Managers")]
    public ItemUseManager itemUseManager;
    public BuffManager buffManager;
    public BuffSkillManager buffSkillManager;
    public SubPlantManager subPlantManager;
    public EnemyManager enemyManager;
    public GameStatisticsManager statisticsManager;
    [Header("Potion Effects")]
    public ItemEffectExecutor potionEffectExecutor;
    [Header("UI")]
    public BuffUIManager buffUIManager;
    public bool isInited;

    public void InitAll(GameManager game)
    {
        if (isInited) return;
        // Clear the old round before effects create any new runtime objects or buffs.
        ItemRuntimeObjectManager.Instance?.ClearAll();
        buffManager?.ClearAllBuffs();
        statisticsManager?.ResetRound();
        enemyManager?.PrepareRound();
        UpgradeToGeneralData data = null;
        GameSession.Instance?.TryReadGeneralData(out data);
        PlantData selectedPlant = game != null ? game.PrepareBattle(data) : null;
        ApplyStartingEffects(data);
        if (game != null) InitializePlant(game.MainPlant, selectedPlant);
        itemUseManager?.Init();
        buffUIManager?.Init();
        OffscreenTargetIndicatorManager.Instance?.ClearAll();
        isInited = true;
    }
    private void ApplyStartingEffects(UpgradeToGeneralData data)
    {
        GameSession.Instance?.LoadSkillItems(buffSkillManager);
        Debug.Log("[SkillTransfer][Battle RECEIVE] " + GameSessionSkillDiagnostics.Describe(data), this);
        if (buffSkillManager != null)
            buffSkillManager.ExecuteAllRegisteredBuffItems(buffSkillManager.gameObject);
        if (data?.potionSlots == null) return;
        var executor = potionEffectExecutor;
        if (executor == null && buffSkillManager != null) executor = buffSkillManager.itemEffectExecutor;
        if (executor == null) return;
        foreach (var entry in data.potionSlots)
            if (entry != null && !entry.locked && entry.potion != null)
                executor.ExecuteItemEffect(entry.potion, Vector3.zero, Vector3.zero, Vector3.zero,
                    executor.gameObject, ItemEffectOrigin.Potion);
    }

    public void RoundInit() { isInited = false; InitAll(GameManager.instance); }

    private void InitializePlant(Plant plant, PlantData data)
    {
        if (plant != null) plant.Init(data);
        enemyManager?.Init(data);
    }

    public void ResetEntity() { DamageArea.ClearAllActiveAreas(); enemyManager?.AllStop(); }
}
