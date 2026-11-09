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
    [Header("Potion Effects")]
    public ItemEffectExecutor potionEffectExecutor;
    [Header("UI")]
    public BuffUIManager buffUIManager;
    public bool isInited;

    public void InitAll()
    {
        if (isInited) return;
        // Clear the old round before effects create any new runtime objects or buffs.
        ItemRuntimeObjectManager.Instance?.ClearAll();
        buffManager?.ClearAllBuffs();
        statisticsManager?.ResetRound();
        enemyManager?.PrepareRound();
        plantManager?.ResetSelectionOverrides();
        ApplyStartingEffects();

        plantManager?.SetPlants();
        Debug.Log($"[SkillTransfer][Battle APPLIED] index={plantManager?.CurrentPlantIndex}, plant={plantManager?.CurrentPlant?.dataId ?? "none"}", this);
        if (enemyManager != null && plantManager != null) enemyManager.Init(plantManager.CurrentPlant);
        itemUseManager?.Init();
        buffUIManager?.Init();
        OffscreenTargetIndicatorManager.Instance?.ClearAll();
        isInited = true;
    }
    private void ApplyStartingEffects()
    {
        UpgradeToGeneralData data = null;
        var session = GameSession.Instance;
        if (session != null)
        {
            session.LoadSkillItems(buffSkillManager);
            session.TryReadGeneralData(out data);
        }
        Debug.Log("[SkillTransfer][Battle RECEIVE] " + GameSessionSkillDiagnostics.Describe(data), this);
        if (data != null && data.mainPlantIndex >= 0)
            plantManager?.SetSkillPlantIndex(data.mainPlantIndex);
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

    public void RoundInit() { isInited = false; InitAll(); }
    public void ResetEntity() { DamageArea.ClearAllActiveAreas(); enemyManager?.AllStop(); }
}
