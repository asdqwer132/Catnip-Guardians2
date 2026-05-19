using System.Collections.Generic;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    [Header("Plant List")]
    public PlantData[] allPlants;

    private Dictionary<PlantData, bool> unlockedPlants =
        new Dictionary<PlantData, bool>();

    public PlantData CurrentPlant { get; private set; }

    void Awake()
    {
        instance = this;
        InitPlants();
    }

    void InitPlants()
    {
        unlockedPlants.Clear();
        CurrentPlant = null;

        foreach (PlantData plant in allPlants)
        {
            if (plant == null)
                continue;

            unlockedPlants[plant] = plant.unlockedByDefault;

            if (CurrentPlant == null && plant.unlockedByDefault)
            {
                CurrentPlant = plant;
            }
        }
    }

    public void SelectPlant(PlantData plant)
    {
        if (plant == null)
            return;

        if (!IsUnlocked(plant))
        {
            Debug.Log("아직 해금되지 않은 플랜트입니다: " + plant.plantName);
            return;
        }

        CurrentPlant = plant;
        Debug.Log("선택된 플랜트: " + plant.plantName);
    }

    public void UnlockPlant(PlantData plant)
    {
        if (plant == null)
            return;

        if (!unlockedPlants.ContainsKey(plant))
        {
            unlockedPlants.Add(plant, true);
        }
        else
        {
            unlockedPlants[plant] = true;
        }

        if (CurrentPlant == null)
        {
            CurrentPlant = plant;
        }
        SelectPlant(plant);
        Debug.Log("플랜트 해금 완료: " + plant.plantName);
    }

    public bool IsUnlocked(PlantData plant)
    {
        if (plant == null)
            return false;

        if (!unlockedPlants.ContainsKey(plant))
            return false;

        return unlockedPlants[plant];
    }
}