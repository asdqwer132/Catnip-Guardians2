using TMPro;

public class ShopBoxRewardSlotUI : BaseItemSlotUI
{
    public TextMeshProUGUI chanceText;
    public TextMeshProUGUI amountChancesText;

    public void SetSlot(GachaItemInfo info)
    {
        if (info == null || info.itemData == null)
        {
            ClearSlot();
            return;
        }

        SetItemData(info.itemData, 1);
        int min = int.MaxValue, max = 0;
        double total = 0;
        foreach (var entry in info.amounts ?? System.Array.Empty<GachaAmountChance>())
            if (entry != null && entry.count > 0 && entry.weight > 0)
            { min = System.Math.Min(min,entry.count); max = System.Math.Max(max,entry.count); total += entry.weight; }
        if (max == 0) min = max = 1;
        if (amountText != null) amountText.text = min == max ? min.ToString() : min + "–" + max;
        if (amountChancesText != null)
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var entry in info.amounts ?? System.Array.Empty<GachaAmountChance>())
                if (entry != null && entry.count > 0 && entry.weight > 0 && total > 0)
                    lines.Add(entry.count + " : " + (entry.weight/total*100).ToString("F1") + "%");
            amountChancesText.text = lines.Count > 0 ? string.Join("\n",lines) : "1 : 100%";
        }

        if (chanceText != null)
        {
            chanceText.text = (UnityEngine.Mathf.Clamp01(info.probability)*100f).ToString("F1") + "%";
        }
    }

    public override void ClearSlot()
    {
        base.ClearSlot();

        if (chanceText != null)
            chanceText.text = "";
        if (amountChancesText != null) amountChancesText.text = "";
    }
}
