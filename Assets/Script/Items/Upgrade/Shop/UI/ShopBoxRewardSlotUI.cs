using TMPro;

public class ShopBoxRewardSlotUI : BaseItemSlotUI
{
    public TextMeshProUGUI chanceText;

    public void SetSlot(GachaItemInfo info, int totalWeight)
    {
        if (info == null || info.itemData == null)
        {
            ClearSlot();
            return;
        }

        SetItemData(info.itemData, 1);

        if (chanceText != null)
        {
            float chance = 0f;

            if (totalWeight > 0)
                chance = (float)info.weight / totalWeight * 100f;

            chanceText.text = chance.ToString("F1") + "%";
        }
    }

    public override void ClearSlot()
    {
        base.ClearSlot();

        if (chanceText != null)
            chanceText.text = "";
    }
}