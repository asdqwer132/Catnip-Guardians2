using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour, IStoryProgressSource
{
    [Header("Progress Events")]
    public string progressEventId = StoryProgressEventIds.ChestItemObtained;
    public event System.Action<StoryProgressEvent> ProgressReported;
    private void OnEnable() => StoryProgressSources.Register(this);
    private void OnDisable() => StoryProgressSources.Unregister(this);

    public static ShopManager instance;

    [Header("Box Pool")]
    public ItemBoxPoolManager boxPoolManager;
    [SerializeField] private ItemGradeData materialChestGrades;

    [Header("Reroll Manager")]
    public ShopRerollManager rerollManager;

    [Header("Shop Settings")]
    public int displayBoxCount = 3;
    public ShopBoxButton[] boxButtons;
    public ShopBoxButton[] pinBoxButtons;
    public ShopSummaryUI summary;
    public ShopBoxInfoUI boxInfoUI;

    [Header("Purchase Result")]
    public ResultPanelUI purchaseResultPanelUI;
    public BasicTooltipUI resultHeader;
    public TooltipUI itemTooltip;

    private bool showingResult;
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
        if (showingResult) return;
        ClearSelectedBox();
        ApplyBoxes(boxButtons, boxPoolManager != null
            ? boxPoolManager.GetRandomBoxes(displayBoxCount) : null);
        ApplyBoxes(pinBoxButtons, boxPoolManager != null
            ? boxPoolManager.GetRandomBoxes(pinBoxButtons?.Length ?? 0, ShopBoxKind.Pin) : null);
        summary?.Refresh();
    }

    public void RerollShop()
    {
        if (rerollManager == null || showingResult)
            return;

        bool canReroll = rerollManager.TryPayRerollPrice();

        if (!canReroll)
            return;

        RefreshShopWithoutCost();
    }

    private void ApplyBoxes(ShopBoxButton[] slots, List<ItemBoxData> offers)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            var box = offers != null && i < offers.Count ? offers[i] : null;
            slot.gameObject.SetActive(box != null);
            slot.Init(this, box);
            slot.SetMaterialRarity(box != null && box.kind == ShopBoxKind.Material
                ? materialChestGrades?.GetEntry(box.grade)?.icon : null);
        }
    }

    public bool IsShowingResult => showingResult;

    public bool IsSelectedButton(ShopBoxButton button)
    {
        return currentSelectedButton == button;
    }

    public void SelectBox(ShopBoxButton button, ItemBoxData boxData)
    {
        if (showingResult || button == null || boxData == null || button.IsPurchased || button.BoxData != boxData || !OwnsSlot(button))
            return;
        if (currentSelectedButton != null && currentSelectedButton != button) currentSelectedButton.ClearSelection();

        currentSelectedButton = button;
        currentSelectedBox = boxData;

        if (summary != null) summary.gameObject.SetActive(false);
        if (boxInfoUI != null) boxInfoUI.ShowBoxInfo(boxData);
    }

    public void ClearSelectedBox()
    {
        var previous = currentSelectedButton;
        currentSelectedButton = null;
        currentSelectedBox = null;
        if (previous != null) previous.ClearSelection();

        if (boxInfoUI != null) boxInfoUI.ClearInfo();
        if (!showingResult && summary != null) summary.gameObject.SetActive(true);
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
        var offered = FindOffer(boxData, boxButtons) ?? FindOffer(boxData, pinBoxButtons);
        if (offered != null) { Purchase(offered, boxData); return; }
        PurchaseFailed("구매 가능한 상자 슬롯이 없습니다.");
    }

    private static ShopBoxButton FindOffer(ItemBoxData box, ShopBoxButton[] slots)
        => slots != null ? System.Array.Find(slots, slot => slot != null && slot.BoxData == box && !slot.IsPurchased) : null;

    private bool OwnsSlot(ShopBoxButton button)
        => (boxButtons != null && System.Array.IndexOf(boxButtons, button) >= 0) ||
            (pinBoxButtons != null && System.Array.IndexOf(pinBoxButtons, button) >= 0);

    public void ReturnToSummary()
    {
        showingResult = false;
        itemTooltip?.Hide();
        ClearSelectedBox();
        summary?.Refresh();
    }

    private bool Purchase(ShopBoxButton button, ItemBoxData boxData)
    {
        if (purchasing || showingResult) return PurchaseFailed("구매 처리 중입니다.");
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
                if (combined == null) costs.Add(new Cost(cost.currencyType, cost.amount));
                else
                {
                    if ((long)combined.amount + cost.amount > int.MaxValue) return PurchaseFailed("가격이 올바르지 않습니다.");
                    combined.amount += cost.amount;
                }
            }
        if (!wallet.HasCurrencies(costs)) return PurchaseFailed("재화가 부족합니다.");
        if (boxData.gachaItems == null || !System.Array.Exists(boxData.gachaItems,r => r != null && r.itemData != null))
            return PurchaseFailed("상자에 보상 데이터가 없습니다.");
        var rewards = boxData.RollRewards();

        purchasing = true;
        try
        {
            if (!wallet.SpendCurrencies(costs)) return PurchaseFailed("재화가 부족합니다.");
            foreach (var reward in rewards) inventory.AddItem(reward.itemData, reward.amount);
            button.MarkPurchased();
            foreach (var reward in rewards)
                ProgressReported?.Invoke(new StoryProgressEvent(progressEventId,
                    StoryProgressValue.TextValue("item-id", reward.itemData.dataId),
                    StoryProgressValue.NumberValue("amount", reward.amount)));
            CodexDiscoveryHistory.Current.Record(CodexDiscoveryKind.Chest, boxData.dataId);
            itemTooltip?.Hide();
            if (summary != null && purchaseResultPanelUI != null)
            {
                showingResult = true;
                ClearSelectedBox();
            }
            resultHeader?.SetData(boxData);
            var results = rewards.ConvertAll(r => new ShopPurchaseResultEntry(r.itemData,r.amount));
            if (purchaseResultPanelUI != null) purchaseResultPanelUI.ShowStoryResults(results, StoryProgressManager.ChestResult);
            return true;
        }
        finally { purchasing = false; }
    }

    private bool PurchaseFailed(string reason)
    {
        Debug.Log("[Shop] 구매 실패: " + reason, this);
        return false;
    }

}
