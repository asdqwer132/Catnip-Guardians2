using UnityEngine;
using UnityEngine.UI;

public class NextItemPreviewUI : MonoBehaviour
{
    [Header("Next Item UI")]
    public Image nextItemIcon;

    [Header("Ready UI")]
    public GameObject itemReadyImage;

    private ItemData lastItemData;
    private bool lastBagCoolingDown;

    public void Refresh(BagItemUseManager manager)
    {
        lastItemData = null;
        lastBagCoolingDown = false;

        RefreshItemIfChanged(manager, true);
        RefreshRuntimeState(manager);
    }

    public void RefreshRuntimeState(BagItemUseManager manager)
    {
        RefreshItemIfChanged(manager, false);
        RefreshReadyImage(manager);
    }

    private void RefreshItemIfChanged(BagItemUseManager manager, bool force)
    {
        ItemData currentItemData = GetCurrentNextItemData(manager);
        bool isBagCoolingDown = manager != null && manager.IsBagCoolingDown();

        if (!force && currentItemData == lastItemData && isBagCoolingDown == lastBagCoolingDown)
            return;

        lastItemData = currentItemData;
        lastBagCoolingDown = isBagCoolingDown;

        RefreshItem(manager, currentItemData);
    }

    private ItemData GetCurrentNextItemData(BagItemUseManager manager)
    {
        if (manager == null)
            return null;

        if (manager.IsBagCoolingDown())
            return null;

        InventoryItem nextItem = manager.GetNextUsableInventoryItemForUI();

        if (nextItem == null)
            return null;

        return nextItem.itemData;
    }

    private void RefreshItem(BagItemUseManager manager, ItemData itemData)
    {
        if (manager == null)
        {
            Clear();
            return;
        }

        if (manager.IsBagCoolingDown())
        {
            ClearIconOnly();
            return;
        }

        if (itemData == null)
        {
            ClearIconOnly();
            return;
        }

        if (nextItemIcon == null)
            return;

        if (itemData.icon != null)
        {
            nextItemIcon.enabled = true;
            nextItemIcon.sprite = itemData.icon;
        }
        else
        {
            nextItemIcon.enabled = false;
            nextItemIcon.sprite = null;
        }
    }

    private void RefreshReadyImage(BagItemUseManager manager)
    {
        bool isReady = manager != null
            && !manager.IsBagCoolingDown()
            && !manager.IsNextItemUseCoolingDown()
            && manager.GetNextUsableInventoryItemForUI() != null;

        if (itemReadyImage != null)
            itemReadyImage.SetActive(isReady);
    }

    private void ClearIconOnly()
    {
        if (nextItemIcon != null)
        {
            nextItemIcon.enabled = false;
            nextItemIcon.sprite = null;
        }
    }

    public void Clear()
    {
        lastItemData = null;
        lastBagCoolingDown = false;

        if (itemReadyImage != null)
            itemReadyImage.SetActive(false);

        ClearIconOnly();
    }
}