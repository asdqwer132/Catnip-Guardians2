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
public class ShopBoxInfoUI : ItemDefinitionUI
{
    [Header("Basic Info UI")]
    public GameObject pannel;
    public BoxCost[] extraBoxCosts;

    [Header("Gacha Item List")]
    public Transform itemListParent;
    public ShopBoxRewardSlotUI rewardSlotPrefab;

    [Header("Buy Button")]
    public Button buyButton;

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

        SetData(boxData);


        if (extraBoxCosts != null)
        {
            foreach (var item in extraBoxCosts)
            {
                if (item != null && item.costPanel != null) item.costPanel.SetActive(false);
            }
            foreach (var cost in boxData.costs ?? Array.Empty<Cost>())
            {
                if (cost == null) continue;
                BoxCost boxCost = GetBoxCost(cost.currencyType);
                if (boxCost == null) continue;
                if (boxCost.costText != null) boxCost.costText.text = cost.amount.ToString();
                if (boxCost.costPanel != null) boxCost.costPanel.SetActive(true);
            }
        }



        RefreshRewardList(boxData);

        if (buyButton != null)
            buyButton.interactable = true;

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

        shopManager.BuySelectedBox();
    }

    public void ClearInfo()
    {
        ClearData();

        if (pannel != null)
            pannel.SetActive(false);

        ClearRewardList();

        if (buyButton != null)
            buyButton.interactable = false;

    }
}