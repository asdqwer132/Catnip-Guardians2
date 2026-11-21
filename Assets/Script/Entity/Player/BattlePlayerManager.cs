using System;
using UnityEngine;

public sealed class BattlePlayerManager : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Plant mainPlant;
    [Tooltip("기존 스킬·포션의 식물 인덱스에 대응하는 목록입니다.")]
    [SerializeField] private PlantData[] indexedPlants = Array.Empty<PlantData>();

    private int skillPlantIndex = -1;
    private int potionPlantIndex = -1;

    public Plant MainPlant => mainPlant;
    public PlantData CurrentPlant { get; private set; }
    public PlayerData CurrentPlayer => player != null ? player.CurrentPlayerData : null;
    public bool IsBossBattle { get; private set; }

    public void Prepare(UpgradeToGeneralData data)
    {
        CurrentPlant = null;
        skillPlantIndex = potionPlantIndex = -1;
        IsBossBattle = data?.bossBattle ?? false;
        player?.ApplyPlayerData(data?.battlePlayer);
        if (data != null) SetSkillPlantIndex(data.mainPlantIndex);
    }

    // Apply after the starting effects have had a chance to set their overrides.
    public void ApplyPlant()
    {
        int index = potionPlantIndex >= 0 ? potionPlantIndex : skillPlantIndex;
        CurrentPlant = index >= 0 ? indexedPlants[index] : CurrentPlayer?.GetPlant(IsBossBattle);
        if (CurrentPlant == null && IsValidIndex(0)) CurrentPlant = indexedPlants[0];
        if (mainPlant != null && CurrentPlant != null) mainPlant.Init(CurrentPlant);
    }

    public void SetSkillPlantIndex(int index)
    {
        if (IsValidIndex(index)) skillPlantIndex = Mathf.Max(skillPlantIndex, index);
    }

    public void SetPotionPlantIndex(int index)
    {
        if (IsValidIndex(index)) potionPlantIndex = index;
    }

    private bool IsValidIndex(int index) =>
        indexedPlants != null && index >= 0 && index < indexedPlants.Length && indexedPlants[index] != null;
}
