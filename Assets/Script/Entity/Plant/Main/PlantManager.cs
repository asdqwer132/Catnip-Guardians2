using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    [Header("Plant List")]
    public PlantData[] allPlants;

    public PlantData CurrentPlant { get; private set; }

    void Awake()
    {
        instance = this;
        InitPlants();
    }

    void InitPlants()
    {
        CurrentPlant = null;

        foreach (PlantData plant in allPlants)
        {
            if (plant == null)
                continue;

            if (CurrentPlant == null && UnlockCheckUtility.CanUse(plant)) // 최종 언락 식물 연결
                CurrentPlant = plant;
        }
    }
}