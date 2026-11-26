using UnityEngine;

public class ShopRerollManager : MonoBehaviour
{
    [Header("Reroll Price")]
    public CurrencyType priceType;
    public int price = 100;

    [Header("Option")]
    public bool firstRerollFree = false;

    private bool usedFirstFreeReroll = false;
    public int CurrentPrice => firstRerollFree && !usedFirstFreeReroll ? 0 : price;

    public bool CanReroll()
    {
        if (firstRerollFree && !usedFirstFreeReroll)
            return true;

        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager가 없습니다.");
            return false;
        }

        return CurrencyManager.instance.HasCurrency(priceType, price);
    }

    public bool TryPayRerollPrice()
    {
        if (firstRerollFree && !usedFirstFreeReroll)
        {
            usedFirstFreeReroll = true;
            Debug.Log("무료 리롤 사용");
            return true;
        }

        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager가 없습니다.");
            return false;
        }

        bool canPay = CurrencyManager.instance.SpendCurrency(priceType, price);

        if (!canPay)
        {
            Debug.Log("리롤 재화가 부족합니다.");
            return false;
        }

        Debug.Log("리롤 비용 지불 완료");
        return true;
    }

    public void ResetFreeReroll()
    {
        usedFirstFreeReroll = false;
    }
}