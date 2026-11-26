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
    [SerializeField] private Button clearButton;
    [SerializeField] private Button inlineResultButton;
    private ItemData inlineResult;
    private bool resultAwaitingConfirmation;

    public PotionCraftManager Manager => manager;

    private void OnEnable()
    {
        if (clearButton != null) clearButton.onClick.AddListener(ReturnMaterials);
        if (inlineResultButton != null) inlineResultButton.onClick.AddListener(ConfirmInlineResult);
        if (manager != null)
        {
            manager.onMaterialChanged += OnMaterialsChanged;
            if (manager.recipeHistory != null) manager.recipeHistory.Changed += RefreshUI;
        }
        RefreshUI();
        if (combineButton != null)
        {
            bool wired = false;
            for (int i = 0; i < combineButton.onClick.GetPersistentEventCount(); i++)
                if ((combineButton.onClick.GetPersistentTarget(i) == this ||
                     (manager != null && combineButton.onClick.GetPersistentTarget(i) == manager)) &&
                    combineButton.onClick.GetPersistentMethodName(i) == nameof(Combine)) wired = true;
            combineButton.onClick.RemoveListener(Combine);
            if (!wired) combineButton.onClick.AddListener(Combine);
        }
    }

    private void OnDisable()
    {
        if (clearButton != null) clearButton.onClick.RemoveListener(ReturnMaterials);
        if (inlineResultButton != null) inlineResultButton.onClick.RemoveListener(ConfirmInlineResult);
        if (combineButton != null) combineButton.onClick.RemoveListener(Combine);
        if (manager != null)
        {
            manager.onMaterialChanged -= OnMaterialsChanged;
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
        if (manager == null || !manager.TryCombine(out ItemData result)) return;
        if (resultPanel != null) resultPanel.ShowResult(result);
        if (inlineResultButton != null)
        {
            inlineResult = result;
            resultAwaitingConfirmation = true;
            RefreshUI();
        }
    }

    private void ConfirmInlineResult()
    {
        if (!resultAwaitingConfirmation) return;
        resultAwaitingConfirmation = false;
        manager?.ConfirmCraftResult();
    }

    private void OnMaterialsChanged()
    {
        inlineResult = null;
        resultAwaitingConfirmation = false;
        RefreshUI();
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
        ItemData preview = inlineResult != null ? inlineResult : manager.GetPreviewResult();
        if (inlineResultButton != null) inlineResultButton.interactable = preview != null;
        if (resultSlot != null)
            resultSlot.SetSlot(preview != null ? new InventoryItem(preview, 1) : null);
        if (combineButton != null)
            combineButton.interactable = manager.CanCraft();
        if (resultTooltip != null && inlineResultButton == null)
        {
            if (preview != null)
                resultTooltip.ShowTooltip();
            else
                resultTooltip.HideTooltip();
        }
    }
}
