using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Box Pool")]
    public ItemBoxPoolManager boxPoolManager;

    [Header("Reroll Manager")]
    public ShopRerollManager rerollManager;

    [Header("Shop Settings")]
    public int displayBoxCount = 3;
    public ShopBoxButton[] boxButtons;
    public ShopBoxInfoUI boxInfoUI;

    [Header("Purchase Result")]
    public ShopPurchaseResultPanelUI purchaseResultPanelUI;

    private List<ItemBoxData> currentShopBoxes = new List<ItemBoxData>();
    private ItemBoxData currentSelectedBox;
    private ShopBoxButton currentSelectedButton;
    private bool purchasing;

    private void Awake()
    {
        instance = this;
    }

    public void InitShop()
    {
        if (rerollManager != null)
            rerollManager.ResetFreeReroll();

        RefreshShopWithoutCost();
    }

    public void RefreshShopWithoutCost()
    {
        ClearSelectedBox();

        currentShopBoxes.Clear();
        if (boxButtons != null)
            foreach (var button in boxButtons)
                if (button != null) button.Init(this, null);

        if (boxPoolManager == null)
        {
            Debug.LogWarning("Box Pool Manager가 연결되지 않았습니다.");
            ClearButtons();
            return;
        }

        currentShopBoxes = boxPoolManager.GetRandomBoxes(displayBoxCount);

        ApplyBoxesToButtons();
    }

    public void RerollShop()
    {
        if (rerollManager == null)
            return;

        bool canReroll = rerollManager.TryPayRerollPrice();

        if (!canReroll)
            return;

        RefreshShopWithoutCost();
    }

    private void ApplyBoxesToButtons()
    {
        if (boxButtons == null)
            return;

        for (int i = 0; i < boxButtons.Length; i++)
        {
            if (boxButtons[i] == null)
                continue;

            if (i < currentShopBoxes.Count && currentShopBoxes[i] != null)
            {
                boxButtons[i].gameObject.SetActive(true);
                boxButtons[i].Init(this, currentShopBoxes[i]);
            }
            else
            {
                boxButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void ClearButtons()
    {
        if (boxButtons == null)
            return;

        for (int i = 0; i < boxButtons.Length; i++)
        {
            if (boxButtons[i] != null)
                boxButtons[i].gameObject.SetActive(false);
        }
    }

    public bool IsSelectedButton(ShopBoxButton button)
    {
        return currentSelectedButton == button;
    }

    public void SelectBox(ShopBoxButton button, ItemBoxData boxData)
    {
        if (button == null || boxData == null || button.IsPurchased || button.BoxData != boxData || !OwnsSlot(button))
            return;
        if (currentSelectedButton != null && currentSelectedButton != button) currentSelectedButton.ClearSelection();

        currentSelectedButton = button;
        currentSelectedBox = boxData;

        if (boxInfoUI != null)
            boxInfoUI.ShowBoxInfo(boxData);
    }

    public void ClearSelectedBox()
    {
        var previous = currentSelectedButton;
        currentSelectedButton = null;
        currentSelectedBox = null;
        if (previous != null) previous.ClearSelection();

        if (boxInfoUI != null)
            boxInfoUI.ClearInfo();
    }

    public void BuySelectedBox() => TryBuySelectedBox();

    public bool TryBuySelectedBox()
    {
        if (currentSelectedButton == null || currentSelectedBox == null)
            return PurchaseFailed("선택된 상자가 없습니다.");
        return Purchase(currentSelectedButton, currentSelectedBox);
    }

    // Existing UnityEvents can keep using BuyBox. Only currently offered slots can be bought.
    public void BuyBox(ItemBoxData boxData)
    {
        if (currentSelectedButton != null && currentSelectedBox == boxData)
        {
            Purchase(currentSelectedButton, boxData);
            return;
        }
        if (boxButtons != null)
            foreach (var button in boxButtons)
                if (button != null && button.BoxData == boxData && !button.IsPurchased)
                {
                    Purchase(button, boxData);
                    return;
                }
        PurchaseFailed("구매 가능한 상자 슬롯이 없습니다.");
    }

    private bool OwnsSlot(ShopBoxButton button)
        => boxButtons != null && System.Array.IndexOf(boxButtons, button) >= 0;

    private bool Purchase(ShopBoxButton button, ItemBoxData boxData)
    {
        if (purchasing) return PurchaseFailed("구매 처리 중입니다.");
        if (button == null || boxData == null || !OwnsSlot(button) || button.IsPurchased || button.BoxData != boxData)
            return PurchaseFailed("구매 가능한 상자 슬롯이 없습니다.");
        var wallet = CurrencyManager.instance;
        var inventory = InventoryManager.instance;
        if (wallet == null || inventory == null) return PurchaseFailed("재화 또는 인벤토리 매니저가 없습니다.");

        // Aggregate repeated currency entries before checking/spending, so a failed
        // purchase never partially pays the same currency more than once.
        var costs = new List<Cost>();
        if (boxData.costs != null)
            foreach (var cost in boxData.costs)
            {
                if (cost == null || cost.amount <= 0) continue;
                var combined = costs.Find(c => c.currencyType == cost.currencyType);
                if (combined == null) costs.Add(new Cost { currencyType = cost.currencyType, amount = cost.amount });
                else
                {
                    if ((long)combined.amount + cost.amount > int.MaxValue) return PurchaseFailed("가격이 올바르지 않습니다.");
                    combined.amount += cost.amount;
                }
            }
        if (!wallet.HasCurrencies(costs)) return PurchaseFailed("재화가 부족합니다.");
        ItemData resultItem = boxData.GetRandomItem();
        if (resultItem == null) return PurchaseFailed("지급할 보상이 없습니다.");

        purchasing = true;
        try
        {
            if (!wallet.SpendCurrencies(costs)) return PurchaseFailed("재화가 부족합니다.");
            inventory.AddItem(resultItem, 1);
            button.MarkPurchased();
            //if (currentSelectedButton == button) ClearSelectedBox();
            ShowPurchaseResult(resultItem, 1);
            return true;
        }
        finally { purchasing = false; }
    }

    private bool PurchaseFailed(string reason)
    {
        Debug.Log("[Shop] 구매 실패: " + reason, this);
        return false;
    }

    private void ShowPurchaseResult(ItemData resultItem, int count)
    {
        if (purchaseResultPanelUI != null)
            purchaseResultPanelUI.ShowResult(resultItem, count);
    }
}
