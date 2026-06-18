using UnityEngine;

public class CraftUIManager : MonoBehaviour
{
    public ItemCombinationManager ItemCombinationManager;
    public CraftMaterialSlotUI[] slots;

    void Start()
    {
        RefreshUI();

        if (ItemCombinationManager != null)
        {
            ItemCombinationManager.onMaterialChanged += RefreshUI;
        }
    }

    void OnDestroy()
    {
        if (ItemCombinationManager != null)
        {
            ItemCombinationManager.onMaterialChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        var materials = ItemCombinationManager.currentMaterials;

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
    }
}