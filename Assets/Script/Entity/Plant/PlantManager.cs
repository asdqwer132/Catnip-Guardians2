using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    [Header("Plant List")]
    public Plant plant;
    public PlantData[] allPlants;
    [Tooltip("Index in All Plants selected before battle. A battle grows only this plant.")]
    [SerializeField, Min(0)] private int currentPlantIndex = 0;

    private int skillPlantIndex = -1;
    private int potionPlantIndex = -1;
    public int CurrentPlantIndex => potionPlantIndex >= 0 ? potionPlantIndex : skillPlantIndex >= 0 ? skillPlantIndex : currentPlantIndex;

    public void ResetSelectionOverrides() { skillPlantIndex = potionPlantIndex = -1; }
    public bool SetSkillPlantIndex(int index)
    {
        if (!IsValidIndex(index)) return false;
        skillPlantIndex = index;
        return true;
    }
    public bool SetPotionPlantIndex(int index)
    {
        if (!IsValidIndex(index)) return false;
        potionPlantIndex = index;
        return true;
    }
    private bool IsValidIndex(int index) => allPlants != null && index >= 0 && index < allPlants.Length && allPlants[index] != null;
    public PlantData CurrentPlant { get; private set; }

    void Awake() { instance = this; }

    private void OnDestroy() { if (instance == this) instance = null; }

    // Select before starting a battle. Retry keeps this selection.
    public void SetPlantIndex(int index)
    {
        if (allPlants == null || index < 0 || index >= allPlants.Length)
        {
            Debug.LogWarning("Plant index is outside All Plants.", this);
            return;
        }
        currentPlantIndex = index;
    }

    public void SetPlants()
    {
        CurrentPlant = null;
        int selectedIndex = CurrentPlantIndex;
        if (allPlants == null || selectedIndex < 0 || selectedIndex >= allPlants.Length)
        {
            Debug.LogWarning("Plant index is outside All Plants.", this);
            return;
        }

        var data = allPlants[selectedIndex];
        if (data == null || !UnlockCheckUtility.CanUse(data))
        {
            Debug.LogWarning("Selected plant is missing or locked.", this);
            return;
        }

        CurrentPlant = data;
        if (plant != null) plant.Init(data);
    }

    public void PlayGrown()
    {
        //폭발 애니메이션 작동
    }
}