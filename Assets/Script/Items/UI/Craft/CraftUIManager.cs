using UnityEngine;

public class CraftUIManager : MonoBehaviour
{
    public CraftMaterialSlotUI[] slots;

    void Start()
    {
        RefreshUI();

        if (ItemCombinationManager.instance != null)
        {
            ItemCombinationManager.instance.onMaterialChanged += RefreshUI;
        }
    }

    void OnDestroy()
    {
        if (ItemCombinationManager.instance != null)
        {
            ItemCombinationManager.instance.onMaterialChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        var materials = ItemCombinationManager.instance.currentMaterials;

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