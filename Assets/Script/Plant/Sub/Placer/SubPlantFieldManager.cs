using UnityEngine;

public class SubPlantFieldManager : MonoBehaviour
{
    public static SubPlantFieldManager instance;

    [Header("심기 위치 데이터")]
    public SubPlantFieldSlot[] slots;

    void Awake()
    {
        instance = this;
    }

    public bool Plant(int slotIndex, ItemData itemData)
    {
        SubPlantFieldSlot slot = GetSlot(slotIndex);

        if (slot == null)
        {
            Debug.Log("없는 슬롯 번호입니다.");
            return false;
        }

        if (slot.isOccupied)
        {
            Debug.Log("이미 심어진 위치입니다.");
            return false;
        }

        if (itemData == null || itemData.prefab == null)
        {
            Debug.Log("심을 수 없는 아이템입니다.");
            return false;
        }

        bool removed = InventoryManager.instance.RemoveItem(itemData, 1);

        if (removed == false)
        {
            Debug.Log("인벤토리에 아이템이 없습니다.");
            return false;
        }

        slot.isOccupied = true;
        slot.plantedItem = itemData;

        GameObject plantObj = Instantiate(
            itemData.prefab,
            slot.spawnPoint.position,
            Quaternion.identity
        );

        slot.spawnedObject = plantObj;

        Debug.Log(slotIndex + "번 위치에 " + itemData.itemName + " 심음");

        return true;
    }

    public SubPlantFieldSlot GetSlot(int slotIndex)
    {
        foreach (SubPlantFieldSlot slot in slots)
        {
            if (slot.slotIndex == slotIndex)
                return slot;
        }

        return null;
    }
}