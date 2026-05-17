using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagCooldownUI : MonoBehaviour
{
    [Header("Select UI")]
    public GameObject selectedFrame;

    [Header("Bag UI")]
    public Image bagCooldownFill;
    public TextMeshProUGUI bagCooldownText;

    [Header("Slot Create UI")]
    public Transform slotUIParent;
    public BagSlotCooldownUI slotUIPrefab;

    private List<BagSlotCooldownUI> slotUIs = new List<BagSlotCooldownUI>();

    public void BuildSlotUIs(BagItemUseManager manager)
    {
        ClearSlotUIs();

        if (manager == null || manager.bag == null || manager.bag.equippedItems == null)
            return;

        if (slotUIParent == null)
        {
            Debug.LogWarning(name + "의 Slot UI Parent가 없습니다.");
            return;
        }

        if (slotUIPrefab == null)
        {
            Debug.LogWarning(name + "의 Slot UI Prefab이 없습니다.");
            return;
        }

        int slotCount = manager.bag.equippedItems.Count;

        for (int i = 0; i < slotCount; i++)
        {
            BagSlotCooldownUI slotUI = Instantiate(slotUIPrefab, slotUIParent);
            slotUI.gameObject.SetActive(true);

            slotUIs.Add(slotUI);
        }

        RefreshSlotItemIcons(manager);
    }

    public void UpdateUI(BagItemUseManager manager, bool isSelected)
    {
        UpdateSelectedUI(isSelected);

        if (manager == null)
        {
            ClearCooldownUI();
            ClearSlotUIsVisualOnly();
            return;
        }

        UpdateBagCooldown(manager);
        UpdateSlotCooldowns(manager);
        UpdateNextUseSlotImages(manager, isSelected);
        RefreshSlotItemIcons(manager);
    }

    private void UpdateSelectedUI(bool isSelected)
    {
        if (selectedFrame != null)
        {
            selectedFrame.SetActive(isSelected);
        }
    }

    private void UpdateBagCooldown(BagItemUseManager manager)
    {
        float ratio = manager.GetBagCooldownRatio();
        float remain = manager.GetBagCooldownRemain();

        if (bagCooldownFill != null)
        {
            bagCooldownFill.fillAmount = ratio;
        }

        if (bagCooldownText != null)
        {
            if (remain > 0f)
                bagCooldownText.text = remain.ToString("F1");
            else
                bagCooldownText.text = "";
        }
    }

    private void UpdateSlotCooldowns(BagItemUseManager manager)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] == null)
                continue;

            float ratio = manager.GetSlotCooldownRatio(i);
            float remain = manager.GetSlotCooldownRemain(i);

            slotUIs[i].SetCooldown(ratio, remain);
        }
    }

    private void UpdateNextUseSlotImages(BagItemUseManager manager, bool isSelected)
    {
        ClearNextUseSlotImages();

        if (!isSelected)
            return;

        int nextSlotIndex = manager.GetNextReadyUsableSlotIndexForUI();

        if (nextSlotIndex == -1)
            return;

        if (nextSlotIndex < 0 || nextSlotIndex >= slotUIs.Count)
            return;

        if (slotUIs[nextSlotIndex] == null)
            return;

        slotUIs[nextSlotIndex].SetNextUse(true);
    }

    private void RefreshSlotItemIcons(BagItemUseManager manager)
    {
        if (manager == null || manager.bag == null || manager.bag.equippedItems == null)
            return;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] == null)
                continue;

            InventoryItem item = null;

            if (i < manager.bag.equippedItems.Count)
            {
                item = manager.bag.equippedItems[i];
            }

            slotUIs[i].SetItem(item);
        }
    }

    private void ClearNextUseSlotImages()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].SetNextUse(false);
            }
        }
    }

    private void ClearCooldownUI()
    {
        if (bagCooldownFill != null)
            bagCooldownFill.fillAmount = 0f;

        if (bagCooldownText != null)
            bagCooldownText.text = "";
    }

    private void ClearSlotUIsVisualOnly()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].Clear();
            }
        }
    }

    private void ClearSlotUIs()
    {
        for (int i = slotUIs.Count - 1; i >= 0; i--)
        {
            if (slotUIs[i] != null)
            {
                Destroy(slotUIs[i].gameObject);
            }
        }

        slotUIs.Clear();
    }
}