using UnityEngine;

public class SelectedBagPreviewUI : MonoBehaviour
{
    [Header("References")]
    public BagSelectManager bagSelectManager;

    [Header("Preview Parts")]
    public CurrentBagPreviewUI currentBagPreviewUI;
    public NextItemPreviewUI nextItemPreviewUI;
    public CooldownFillPreviewUI cooldownFillPreviewUI;

    [Header("Linked UI")]
    public BagCooldownUIController bagCooldownUIController;

    private int lastBagIndex = -999;
    private int lastNextSlotIndex = -999;


    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        RefreshRuntimeUI();
    }

    public void Init()
    {
        Subscribe();

        if (bagCooldownUIController != null)
            bagCooldownUIController.Init(bagSelectManager);

        RefreshAllUI();
    }

    private void Subscribe()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected += HandleBagSelected;

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.OnCooldownReady += HandleCooldownReady;
    }

    private void Unsubscribe()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected -= HandleBagSelected;

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.OnCooldownReady -= HandleCooldownReady;
    }

    private void HandleBagSelected(int bagIndex)
    {
        RefreshAllUI();
        ForceRefreshLinkedBagSlotUI();
    }

    private void HandleCooldownReady()
    {
        RefreshRuntimeUI();
        ForceRefreshLinkedBagSlotUI();
    }

    private void RefreshAllUI()
    {
        BagItemUseManager manager = GetCurrentManager();

        if (currentBagPreviewUI != null)
            currentBagPreviewUI.RefreshInfo(manager);

        if (nextItemPreviewUI != null)
            nextItemPreviewUI.Refresh(manager);

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.Refresh(manager);

        RefreshLinkedBagSlotUIIfChanged(manager);
    }

    private void RefreshRuntimeUI()
    {
        BagItemUseManager manager = GetCurrentManager();

        if (manager == null)
        {
            ClearAllUI();
            return;
        }

        if (nextItemPreviewUI != null)
            nextItemPreviewUI.RefreshRuntimeState(manager);

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.Refresh(manager);

        RefreshLinkedBagSlotUIIfChanged(manager);
    }

    private void RefreshLinkedBagSlotUIIfChanged(BagItemUseManager manager)
    {
        if (bagCooldownUIController == null || bagSelectManager == null)
            return;

        int currentBagIndex = bagSelectManager.CurrentBagIndex;
        int nextSlotIndex = GetNextSlotIndexForPreview(manager);

        if (lastBagIndex == currentBagIndex && lastNextSlotIndex == nextSlotIndex)
            return;

        lastBagIndex = currentBagIndex;
        lastNextSlotIndex = nextSlotIndex;

        bagCooldownUIController.RefreshSelection();
    }

    private int GetNextSlotIndexForPreview(BagItemUseManager manager)
    {
        if (manager == null)
            return -1;

        if (manager.IsBagCoolingDown())
            return -1;

        int readyIndex = manager.GetNextReadyUsableSlotIndexForUI();

        if (readyIndex >= 0)
            return readyIndex;

        InventoryItem nextItem = manager.GetNextUsableInventoryItemForUI();

        if (nextItem == null || manager.bag == null || manager.bag.equippedItems == null)
            return -1;

        for (int i = 0; i < manager.bag.equippedItems.Count; i++)
        {
            if (manager.bag.equippedItems[i] == nextItem)
                return i;
        }

        return -1;
    }

    private void ForceRefreshLinkedBagSlotUI()
    {
        lastBagIndex = -999;
        lastNextSlotIndex = -999;

        RefreshLinkedBagSlotUIIfChanged(GetCurrentManager());
    }

    private BagItemUseManager GetCurrentManager()
    {
        if (bagSelectManager == null)
            return null;

        return bagSelectManager.CurrentBagUseManager;
    }

    private void ClearAllUI()
    {
        if (currentBagPreviewUI != null)
            currentBagPreviewUI.Clear();

        if (nextItemPreviewUI != null)
            nextItemPreviewUI.Clear();

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.Clear();
    }
}