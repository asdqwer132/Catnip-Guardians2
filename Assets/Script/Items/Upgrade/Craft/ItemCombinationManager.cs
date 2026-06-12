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
        base.Combine();

        InventoryManager.instance.AddItem(resultItem, 1);

        ClearMaterials();
    }
}
