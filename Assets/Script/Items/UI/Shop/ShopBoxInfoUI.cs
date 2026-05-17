using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBoxInfoUI : MonoBehaviour
{
    [Header("Basic Info UI")]
    public Image boxIcon;
    public TextMeshProUGUI boxNameText;
    public TextMeshProUGUI priceText;

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

        if (boxIcon != null)
        {
            boxIcon.sprite = boxData.icon;
            boxIcon.enabled = boxData.icon != null;
        }

        if (boxNameText != null)
            boxNameText.text = boxData.boxName;

        if (priceText != null)
            priceText.text = boxData.priceType + " : " + boxData.price;

        RefreshRewardList(boxData);

        if (buyButton != null)
            buyButton.interactable = true;
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
        if (boxIcon != null)
        {
            boxIcon.sprite = null;
            boxIcon.enabled = false;
        }

        if (boxNameText != null)
            boxNameText.text = "";

        if (priceText != null)
            priceText.text = "";

        ClearRewardList();

        if (buyButton != null)
            buyButton.interactable = false;
    }
}