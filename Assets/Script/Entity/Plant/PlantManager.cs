using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    [Header("Plant List")]
    public Plant plant;
    public PlantData[] allPlants;
    [Tooltip("Index in All Plants selected before battle. A battle grows only this plant.")]
    [SerializeField, Min(0)] private int currentPlantIndex = 0;

    public int CurrentPlantIndex => currentPlantIndex;
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
        if (allPlants == null || currentPlantIndex < 0 || currentPlantIndex >= allPlants.Length)
        {
            Debug.LogWarning("Plant index is outside All Plants.", this);
            return;
        }

        var data = allPlants[currentPlantIndex];
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