using Unity.VisualScripting;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    [Header("Plant List")]
    public Plant plant;
    public PlantData[] allPlants;
    [SerializeField] private int currentPlantIndex = 0;
    public PlantData CurrentPlant { get; private set; }

    void Awake()
    {
        instance = this;
    }
    public void SetPlaints()
    {
        PlantData plantData = allPlants[currentPlantIndex];
        if (plantData != null && UnlockCheckUtility.CanUse(plantData))
            CurrentPlant = plantData;
        plant.Init(CurrentPlant);
    }
    public void ResetIndex()
    {
        currentPlantIndex = 0;
        SetPlaints();
    }
    public bool UpIndex()
    {
        Debug.Log(currentPlantIndex + 1 + "/" + allPlants.Length);
        if(currentPlantIndex + 1 >= allPlants.Length) return false;
        currentPlantIndex++;
        SetPlaints();
        return true;
    }

    public void PlayGrown()
    {
        //폭발 애니메이션 작동
    }
}