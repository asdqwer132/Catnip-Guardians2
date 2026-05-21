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

    private List<ItemBoxData> currentShopBoxes = new List<ItemBoxData>();
    private ItemBoxData currentSelectedBox;
    private ShopBoxButton currentSelectedButton;

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

        if (boxPoolManager == null)
        {
            Debug.LogWarning("Box Pool Manager가 연결되지 않았습니다.");
            ClearButtons();
            return;
        }

        currentShopBoxes = boxPoolManager.GetRandomBoxes(displayBoxCount);

        ApplyBoxesToButtons();
    }

    // UI 버튼에 연결
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
        if (button == null)
        {
            Debug.LogWarning("선택할 버튼이 없습니다.");
            return;
        }

        if (boxData == null)
        {
            Debug.LogWarning("선택할 상자 데이터가 없습니다.");
            return;
        }

        currentSelectedButton = button;
        currentSelectedBox = boxData;

        if (boxInfoUI != null)
            boxInfoUI.ShowBoxInfo(boxData);
    }

    public void ClearSelectedBox()
    {
        currentSelectedButton = null;
        currentSelectedBox = null;

        if (boxInfoUI != null)
            boxInfoUI.ClearInfo();
    }

    public void BuySelectedBox()
    {
        if (currentSelectedBox == null)
        {
            Debug.LogWarning("선택된 상자가 없습니다.");
            return;
        }

        BuyBox(currentSelectedBox);
    }

    public void BuyBox(ItemBoxData boxData)
    {
        if (boxData == null)
        {
            Debug.LogWarning("상자 데이터가 없습니다.");
            return;
        }

        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager가 없습니다.");
            return;
        }

        bool canBuy = CurrencyManager.instance.SpendCurrencies(boxData.costs);

        if (!canBuy)
        {
            Debug.Log("재화가 부족합니다.");
            return;
        }

        ItemData resultItem = boxData.GetRandomItem();

        if (resultItem == null)
        {
            Debug.LogWarning("가챠 결과 아이템이 없습니다.");

            // 여기서 환불할 수도 있음.
            // 지금은 기존 구조 유지 때문에 환불은 안 넣음.
            return;
        }

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");

            // 여기도 환불 처리 가능.
            return;
        }

        InventoryManager.instance.AddItem(resultItem, 1);

        Debug.Log("상자 구매 완료: " + resultItem.dataName);
    }
}