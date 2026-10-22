using UnityEngine;

public class ItemCombinationManager : ItemRecipeManager
{
    public static ItemCombinationManager instance;
    public CraftSuccessEffectUI craftSuccessEffectUI;

    private void Awake() => instance = this;

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public override void Combine() => TryCombine(out _);

    public bool TryCombine(out ItemData craftedItem)
    {
        craftedItem = null;
        if (GetCurrentMaterialCount() < 2 || InventoryManager.instance == null)
            return false;

        base.Combine();
        if (resultItem == null)
            return false;

        craftedItem = resultItem;
        // Grant the result without executing its item effects or throwing it.
        InventoryManager.instance.AddItem(craftedItem, 1);
        ClearMaterials();
        if (craftSuccessEffectUI != null)
            craftSuccessEffectUI.Play(craftedItem.icon);
        return true;
    }
}
