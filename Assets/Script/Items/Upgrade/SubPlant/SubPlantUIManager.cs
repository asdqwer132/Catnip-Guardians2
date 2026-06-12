using UnityEngine;

public class SubPlantUIManager : MonoBehaviour
{
    public SubPlantManager subplantmanager;
    public SubPlantMaterialSlotUI[] slots;
    public InventorySlotUI resultSlot;

    void Start()
    {
        RefreshUI();

        if (subplantmanager != null)
        {
            subplantmanager.onMaterialChanged += RefreshUI;
        }
    }

    void OnDestroy()
    {
        if (subplantmanager != null)
        {
            subplantmanager.onMaterialChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        var materials = subplantmanager.currentMaterials;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < materials.Count)
            {
                slots[i].SetSlot(materials[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
        subplantmanager.Combine();
        resultSlot.SetSlot(new InventoryItem(subplantmanager.resultItem, 1));
    }
}