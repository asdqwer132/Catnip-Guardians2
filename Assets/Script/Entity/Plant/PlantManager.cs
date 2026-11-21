using System;
using UnityEngine;

public sealed class PlantManager : MonoBehaviour
{
    [SerializeField] private Plant mainPlant;
    [Tooltip("기존 스킬·포션의 식물 인덱스에 대응하는 목록입니다.")]
    [SerializeField] private PlantData[] indexedPlants = Array.Empty<PlantData>();

    private PlantData normalPlant, bossPlant;
    private int skillPlantIndex, potionPlantIndex;

    public Plant MainPlant => mainPlant;
    public PlantData CurrentPlant { get; private set; }
    public bool IsBossPhase { get; private set; }
    public bool CanAdvanceToBoss => !IsBossPhase && bossPlant != null;

    public void Prepare(PlantData normal, PlantData boss, bool startWithBoss, int skillIndex)
    {
        CurrentPlant = null;
        normalPlant = normal;
        bossPlant = boss;
        IsBossPhase = startWithBoss && bossPlant != null;
        skillPlantIndex = potionPlantIndex = -1;
        SetSkillPlantIndex(skillIndex);
    }

    public void ApplyStartingPlant()
    {
        int index = potionPlantIndex >= 0 ? potionPlantIndex : skillPlantIndex;
        PlantData selected = IsBossPhase ? bossPlant : index >= 0 ? indexedPlants[index] : normalPlant;
        Apply(selected != null ? selected : IsValidIndex(0) ? indexedPlants[0] : null);
    }

    public void StartBossPhase()
    {
        if (!CanAdvanceToBoss) return;
        IsBossPhase = true;
        Apply(bossPlant);
    }

    private void Apply(PlantData data)
    {
        CurrentPlant = data;
        if (mainPlant != null && data != null) mainPlant.Init(data);
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
