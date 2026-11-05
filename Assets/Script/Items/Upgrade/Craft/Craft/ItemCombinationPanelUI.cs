using UnityEngine;
using UnityEngine.UI;

// Connects the authored potion slots to deterministic recipe crafting.
public sealed class ItemCombinationPanelUI : MonoBehaviour
{
    public PotionCraftManager manager;
    public SubPlantMaterialSlotUI[] materialSlots;
    public InventorySlotUI resultSlot;
    public ItemTooltipTrigger resultTooltip;
    public Button combineButton;
    public ResultPanelUI resultPanel;

    public PotionCraftManager Manager => manager;

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.onMaterialChanged += RefreshUI;
            if (manager.recipeHistory != null) manager.recipeHistory.Changed += RefreshUI;
        }
        RefreshUI();
        if (combineButton != null)
            combineButton.onClick.AddListener(manager.Combine);
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.onMaterialChanged -= RefreshUI;
            if (manager.recipeHistory != null) manager.recipeHistory.Changed -= RefreshUI;
        }
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
                {
                    materialSlots[i].Bind(manager, i);
                    if (materialSlots[i].IsLocked || i >= manager.maxMaterialCount)
                        materialSlots[i].LockSlot();
                    else materialSlots[i].SetSlot(manager.GetMaterialAt(i));
                }
        ItemData preview = manager.GetPreviewResult();
        if (resultSlot != null)
            resultSlot.SetSlot(preview != null ? new InventoryItem(preview, 1) : null);
        if (combineButton != null)
            combineButton.interactable = manager.CanCraft();
        if (resultTooltip != null)
        {
            if (preview != null)
                resultTooltip.ShowTooltip();
            else
                resultTooltip.HideTooltip();
        }
    }
}
