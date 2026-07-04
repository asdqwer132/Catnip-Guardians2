using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
[Serializable]
public class BoxCost
{
    public CurrencyType currencyType;
    public GameObject costPanel;
    public TextMeshProUGUI costText;
}
public class ShopBoxInfoUI : MonoBehaviour
{
    [Header("Basic Info UI")]
    public GameObject pannel;
    public Image boxIcon;
    public ShopBoxOpenAnimationUI boxOpenAnimationUI;
    public TextMeshProUGUI boxNameText;
    public TextMeshProUGUI seriesText;
    public BoxCost[] extraBoxCosts;

    [Header("Gacha Item List")]
    public GameObject pickUpTitleText;
    public Transform itemListParent;
    public ShopBoxRewardSlotUI rewardSlotPrefab;

    [Header("Buy Button")]
    public Button buyButton;
    public Toggle InvenToggle;

    [Header("Shop Manager")]
    public ShopManager shopManager;

    private void Start()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnClickBuy);

        ClearInfo();
    }

    public void ShowBoxInfo(ItemBoxData boxData)
    {
        if (boxData == null)
        {
            ClearInfo();
            return;
        }

        if (pannel != null)
            pannel.SetActive(true);

        if (boxOpenAnimationUI != null)
        {
            boxOpenAnimationUI.SetBox(boxData);
        }
        else if (boxIcon != null)
        {
            boxIcon.sprite = boxData.icon;
            boxIcon.enabled = boxData.icon != null;
        }

        if (pickUpTitleText != null)
            pickUpTitleText.SetActive(true);

        if (boxNameText != null)
            boxNameText.text = boxData.GetDataName();
        if (seriesText != null)
            seriesText.text = boxData.itemSeries.ToString();
        if (extraBoxCosts != null)
        {
            foreach (var item in extraBoxCosts)
            {
                item.costPanel.SetActive(false);
            }
            foreach (var cost in boxData.costs)
            {
                BoxCost boxCost = GetBoxCost(cost.currencyType);
                boxCost.costText.text = cost.amount.ToString();
                boxCost.costPanel.SetActive(true);
            }
        }



        RefreshRewardList(boxData);

        if (buyButton != null)
            buyButton.interactable = true;

        if (InvenToggle != null)
            InvenToggle.interactable = true;
    }
    private BoxCost GetBoxCost(CurrencyType currencyType)
    {
        if (extraBoxCosts == null)
            return null;

        return Array.Find(
            extraBoxCosts,
            boxCost => boxCost != null &&
                       boxCost.currencyType == currencyType
        );
    }
    private void RefreshRewardList(ItemBoxData boxData)
    {
        ClearRewardList();

        if (boxData.gachaItems == null || rewardSlotPrefab == null || itemListParent == null)
            return;

        int totalWeight = 0;

        foreach (GachaItemInfo gachaItem in boxData.gachaItems)
        {
            if (gachaItem != null)
                totalWeight += gachaItem.weight;
        }

        foreach (GachaItemInfo gachaItem in boxData.gachaItems)
        {
            if (gachaItem == null || gachaItem.itemData == null)
                continue;

            ShopBoxRewardSlotUI slot = Instantiate(rewardSlotPrefab, itemListParent);
            slot.SetSlot(gachaItem, totalWeight);
        }
    }

    private void ClearRewardList()
    {
        if (itemListParent == null)
            return;

        for (int i = itemListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemListParent.GetChild(i).gameObject);
        }
    }

    private void OnClickBuy()
    {
        if (shopManager == null)
        {
            Debug.LogWarning("ShopManager가 연결되지 않았습니다.");
            return;
        }

        if (boxOpenAnimationUI != null)
            boxOpenAnimationUI.PlayOpen();

        shopManager.BuySelectedBox();
    }

    public void ClearInfo()
    {
        if (boxOpenAnimationUI != null)
        {
            boxOpenAnimationUI.Clear();
        }
        else if (boxIcon != null)
        {
            boxIcon.sprite = null;
            boxIcon.enabled = false;
        }

        if (pannel != null)
            pannel.SetActive(false);

        if (pickUpTitleText != null)
            pickUpTitleText.SetActive(false);

        if (boxNameText != null)
            boxNameText.text = "";

        if (seriesText != null)
            seriesText.text = "";

        ClearRewardList();

        if (buyButton != null)
            buyButton.interactable = false;

        if (InvenToggle != null)
            InvenToggle.interactable = false;
    }
}