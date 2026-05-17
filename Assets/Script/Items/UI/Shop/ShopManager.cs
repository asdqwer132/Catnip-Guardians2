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

    [Header("Shop Buttons")]
    public ShopBoxButton[] boxButtons;

    [Header("Info UI")]
    public ShopBoxInfoUI boxInfoUI;

    private List<ItemBoxData> currentShopBoxes = new List<ItemBoxData>();

    private ItemBoxData currentSelectedBox;
    private ShopBoxButton currentSelectedButton;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InitShop();
    }

    public void InitShop()
    {
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

        //Debug.Log("상점 목록 무료 갱신 완료");
    }

    public void RerollShop()
    {
        if (rerollManager == null)
        {
            Debug.LogWarning("Reroll Manager가 연결되지 않았습니다.");
            return;
        }

        bool canReroll = rerollManager.TryPayRerollPrice();

        if (!canReroll)
            return;

        RefreshShopWithoutCost();

        Debug.Log("상점 리롤 완료");
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

       // Debug.Log("현재 선택된 슬롯: " + button.name);
        //Debug.Log("현재 선택된 상자: " + boxData.boxName);

        if (boxInfoUI != null)
            boxInfoUI.ShowBoxInfo(boxData);
    }

    public void ClearSelectedBox()
    {
        currentSelectedButton = null;
        currentSelectedBox = null;

        if (boxInfoUI != null)
            boxInfoUI.ClearInfo();

        //Debug.Log("상자 선택 해제");
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

        bool canBuy = CurrencyManager.instance.SpendCurrency(boxData.priceType, boxData.price);

        if (!canBuy)
        {
            Debug.Log("재화가 부족합니다.");
            return;
        }

        ItemData resultItem = boxData.GetRandomItem();

        if (resultItem == null)
        {
            Debug.LogWarning("가챠 결과 아이템이 없습니다.");
            return;
        }

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        InventoryManager.instance.AddItem(resultItem, 1);

        //Debug.Log("상자 구매 완료: " + resultItem.itemName);
    }
}