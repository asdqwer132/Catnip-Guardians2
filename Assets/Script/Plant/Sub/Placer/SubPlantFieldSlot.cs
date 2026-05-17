using UnityEngine;

[System.Serializable]
public class SubPlantFieldSlot
{
    public int slotIndex;
    public Transform spawnPoint;

    public bool isOccupied;
    public ItemData plantedItem;
    public GameObject spawnedObject;
}