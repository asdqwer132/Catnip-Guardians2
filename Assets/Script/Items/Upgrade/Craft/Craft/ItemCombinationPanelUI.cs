using UnityEngine;
using UnityEngine.UI;

// Uses the existing potion slots. The combination manager grants inventory items only.
public sealed class ItemCombinationPanelUI : MonoBehaviour
{
    public ItemCombinationManager manager;
    public SubPlantMaterialSlotUI[] materialSlots;
    public InventorySlotUI resultSlot;
    public ItemTooltipTrigger resultTooltip;
    public Button combineButton;
    public ShopPurchaseResultPanelUI resultPanel;

    public ItemCombinationManager Manager => manager;

    private void OnEnable()
    {
        if (manager != null)
            manager.onMaterialChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.onMaterialChanged -= RefreshUI;
        if (resultTooltip != null)
            resultTooltip.HideTooltip();
    }

    public void AddMaterial(ItemData item)
    {
        if (manager != null)
            manager.AddMaterial(item);
    }

    public void ReturnMaterials()
    {
        if (manager != null)
            manager.ReturnMaterials();
    }

    public void Combine()
    {
        if (manager != null && manager.TryCombine(out ItemData result) && resultPanel != null)
            resultPanel.ShowResult(result);
    }

    public void RefreshUI()
    {
        if (manager == null)
            return;
        if (materialSlots != null)
            for (int i = 0; i < materialSlots.Length; i++)
                if (materialSlots[i] != null)
                    materialSlots[i].SetSlot(i < manager.currentMaterials.Count ? manager.currentMaterials[i] : null);
        ItemData preview = manager.GetPreviewResult();
        if (resultSlot != null)
            resultSlot.SetSlot(preview != null ? new InventoryItem(preview, 1) : null);
        if (combineButton != null)
            combineButton.interactable = preview != null && manager.GetCurrentMaterialCount() >= 2;
        if (resultTooltip != null)
        {
            if (preview != null)
                resultTooltip.ShowTooltip();
            else
                resultTooltip.HideTooltip();
        }
    }
}
