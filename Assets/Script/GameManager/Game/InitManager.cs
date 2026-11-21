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

    public void InitAll(BattlePlayerManager battlePlayerManager, PlantManager plantManager)
    {
        if (isInited) return;
        // Clear the old round before effects create any new runtime objects or buffs.
        ItemRuntimeObjectManager.Instance?.ClearAll();
        buffManager?.ClearAllBuffs();
        statisticsManager?.ResetRound();
        enemyManager?.PrepareRound();
        UpgradeToGeneralData data = null;
        GameSession.Instance?.TryReadGeneralData(out data);
        battlePlayerManager?.Prepare(data?.battlePlayer);
        var player = battlePlayerManager?.CurrentPlayer;
        plantManager?.Prepare(player?.normalPlant, player?.bossPlant, data?.bossBattle ?? false,
            data?.mainPlantIndex ?? -1);
        ApplyStartingEffects(data);

        plantManager?.ApplyStartingPlant();
        if (enemyManager != null && plantManager != null) enemyManager.Init(plantManager.CurrentPlant);
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

    public void RoundInit() { isInited = false; InitAll(GameManager.instance?.battlePlayerManager, GameManager.instance?.plantManager); }
    public void StartBossPhase(PlantManager plantManager)
    {
        DamageArea.ClearAllActiveAreas();
        enemyManager?.PrepareRound();
        plantManager.StartBossPhase();
        enemyManager?.Init(plantManager.CurrentPlant);
    }

    public void ResetEntity() { DamageArea.ClearAllActiveAreas(); enemyManager?.AllStop(); }
}
