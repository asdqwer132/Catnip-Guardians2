using NUnit.Framework.Interfaces;
using UnityEngine;

public class ItemCombinationManager : ItemRecipeManager
{
    public static ItemCombinationManager instance;
    public CraftSuccessEffectUI craftSuccessEffectUI;
    private void Awake()
    {
        instance = this;
    }

    public override void Combine()
    {
        if (GetCurrentMaterialCount() < 2)
        {
            Debug.Log("개수 부족 " + GetCurrentMaterialCount());
            return;
        }

        base.Combine();

        if (resultItem == null)
        {
            Debug.LogWarning("[ItemCombinationManager] 조합 결과 아이템이 없습니다.");
            return;
        }

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("[ItemCombinationManager] InventoryManager.instance가 없습니다.");
            return;
        }

        InventoryManager.instance.AddItem(resultItem, 1);

        ClearMaterials();
        craftSuccessEffectUI.Play(resultItem.icon);
    }
}