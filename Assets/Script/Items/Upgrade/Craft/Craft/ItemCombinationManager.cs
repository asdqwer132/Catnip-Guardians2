using UnityEngine;

public class ItemCombinationManager : ItemRecipeManager
{
    public static ItemCombinationManager instance;

    private void Awake()
    {
        instance = this;
    }

    public override void Combine()
    {
        if (GetCurrentMaterialCount() < 2)
        {
            Debug.Log(" 개수 부족" + GetCurrentMaterialCount());
            return;
        }
        base.Combine();

        InventoryManager.instance.AddItem(resultItem, 1);

        ClearMaterials();
    }
}
