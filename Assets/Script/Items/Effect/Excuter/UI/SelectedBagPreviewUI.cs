using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBagPreviewUI : MonoBehaviour
{
    [Header("References")]
    public BagSelectManager bagSelectManager;

    [Header("Current Bag UI")]
    public Image currentBagIcon;
    public TextMeshProUGUI currentBagNameText;
    public TextMeshProUGUI currentBagCooldownText;
    public Image currentBagCooldownFill;

    [Header("Next Item UI")]
    public Image nextItemIcon;
    public TextMeshProUGUI nextItemNameText;
    public TextMeshProUGUI nextItemCooldownText;
    public Image nextItemCooldownFill;

    [Header("Other Bags UI")]
    public Transform otherBagParent;
    public BagPreviewSlotUI otherBagUIPrefab;

    private List<BagPreviewSlotUI> otherBagUIs = new List<BagPreviewSlotUI>();


    void Start()
    {
        BuildOtherBagUIs();
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    public void BuildOtherBagUIs()
    {
        ClearOtherBagUIs();

        if (bagSelectManager == null)
            return;

        if (otherBagParent == null || otherBagUIPrefab == null)
            return;

        int bagCount = bagSelectManager.GetBagCount();

        for (int i = 0; i < bagCount; i++)
        {
            BagPreviewSlotUI ui = Instantiate(otherBagUIPrefab, otherBagParent);
            ui.gameObject.SetActive(true);
            otherBagUIs.Add(ui);
        }
    }

    public void UpdateUI()
    {
        if (bagSelectManager == null)
        {
            ClearCurrentBagUI();
            ClearNextItemUI();
            return;
        }

        BagItemUseManager currentManager = bagSelectManager.CurrentBagUseManager;

        UpdateCurrentBagUI(currentManager);
        UpdateCurrentBagCooldownUI(currentManager);
        UpdateNextItemUI(currentManager);
        UpdateOtherBagsUI();
    }

    private void UpdateCurrentBagUI(BagItemUseManager currentManager)
    {
        if (currentManager == null || currentManager.bag == null)
        {
            ClearCurrentBagUI();
            return;
        }

        if (currentBagNameText != null)
        {
            currentBagNameText.text = currentManager.bag.bagData.bagName;
        }

        if (currentBagIcon != null)
        {
            if (currentManager.bag.bagData != null && currentManager.bag.bagData.icon != null)
            {
                currentBagIcon.enabled = true;
                currentBagIcon.sprite = currentManager.bag.bagData.icon;
            }
            else
            {
                currentBagIcon.enabled = false;
                currentBagIcon.sprite = null;
            }
        }
    }

    private void UpdateCurrentBagCooldownUI(BagItemUseManager currentManager)
    {
        if (currentManager == null)
        {
            if (currentBagCooldownText != null)
                currentBagCooldownText.text = "";

            if (currentBagCooldownFill != null)
                currentBagCooldownFill.fillAmount = 0f;

            return;
        }

        float remain = currentManager.GetBagCooldownRemain();
        float ratio = currentManager.GetBagCooldownRatio();

        if (currentBagCooldownFill != null)
        {
            currentBagCooldownFill.fillAmount = ratio;
        }

        if (currentBagCooldownText != null)
        {
            if (remain > 0f)
            {
                currentBagCooldownText.text = "가방 쿨타임: " + remain.ToString("F1") + "초";
            }
            else
            {
                currentBagCooldownText.text = "가방 사용 가능";
            }
        }
    }

    private void UpdateNextItemUI(BagItemUseManager currentManager)
    {
        if (currentManager == null)
        {
            ClearNextItemUI();
            return;
        }

        InventoryItem nextItem = currentManager.GetNextUsableInventoryItemForUI();

        if (nextItem == null || nextItem.itemData == null)
        {
            ClearNextItemUI();

            if (nextItemNameText != null)
            {
                if (currentManager.IsBagCoolingDown())
                    nextItemNameText.text = "가방 쿨타임 중";
                else if (currentManager.IsNextItemUseCoolingDown())
                    nextItemNameText.text = "아이템 준비 중";
                else
                    nextItemNameText.text = "사용할 아이템 없음";
            }

            UpdateNextItemCooldownUI(currentManager);

            return;
        }

        ItemData itemData = nextItem.itemData;

        if (nextItemNameText != null)
        {
            nextItemNameText.text = itemData.itemName;
        }

        if (nextItemIcon != null)
        {
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

        UpdateNextItemCooldownUI(currentManager);
    }
    private void UpdateNextItemCooldownUI(BagItemUseManager currentManager)
    {
        if (currentManager == null)
        {
            if (nextItemCooldownText != null)
                nextItemCooldownText.text = "";

            if (nextItemCooldownFill != null)
                nextItemCooldownFill.fillAmount = 0f;

            return;
        }

        float remain = currentManager.GetNextItemUseCooldownRemain();
        float ratio = currentManager.GetNextItemUseCooldownRatio();

        if (nextItemCooldownFill != null)
        {
            nextItemCooldownFill.fillAmount = ratio;
        }

        if (nextItemCooldownText != null)
        {
            if (currentManager.IsBagCoolingDown())
            {
                nextItemCooldownText.text = "가방 대기 중";
            }
            else if (remain > 0f)
            {
                nextItemCooldownText.text = "아이템 준비 중: " + remain.ToString("F1") + "초";
            }
            else
            {
                nextItemCooldownText.text = "아이템 사용 가능";
            }
        }
    }

    private void UpdateOtherBagsUI()
    {
        if (bagSelectManager == null)
            return;

        int currentIndex = bagSelectManager.CurrentBagIndex;

        for (int i = 0; i < otherBagUIs.Count; i++)
        {
            if (otherBagUIs[i] == null)
                continue;

            BagItemUseManager manager = bagSelectManager.GetBagUseManager(i);
            bool isSelected = i == currentIndex;

            otherBagUIs[i].SetUI(manager, isSelected);
        }
    }

    private void ClearCurrentBagUI()
    {
        if (currentBagNameText != null)
            currentBagNameText.text = "";

        if (currentBagCooldownText != null)
            currentBagCooldownText.text = "";

        if (currentBagCooldownFill != null)
            currentBagCooldownFill.fillAmount = 0f;

        if (currentBagIcon != null)
        {
            currentBagIcon.enabled = false;
            currentBagIcon.sprite = null;
        }
    }

    private void ClearNextItemUI()
    {
        if (nextItemNameText != null)
            nextItemNameText.text = "";

        if (nextItemCooldownText != null)
            nextItemCooldownText.text = "";

        if (nextItemCooldownFill != null)
            nextItemCooldownFill.fillAmount = 0f;

        if (nextItemIcon != null)
        {
            nextItemIcon.enabled = false;
            nextItemIcon.sprite = null;
        }
    }

    private void ClearOtherBagUIs()
    {
        for (int i = otherBagUIs.Count - 1; i >= 0; i--)
        {
            if (otherBagUIs[i] != null)
                Destroy(otherBagUIs[i].gameObject);
        }

        otherBagUIs.Clear();
    }
}