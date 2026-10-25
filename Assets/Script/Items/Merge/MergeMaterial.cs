using System;
using TMPro;
using UnityEngine;

[Serializable]
public class MergeItemMaterial
{
    public MergeItem mergeItem;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI defaultCountText;
    public int count;
    public int defaultCount;

    public MergeItemMaterial(MergeItem mergeItem, TextMeshProUGUI countText, TextMeshProUGUI defaultCountText, int count, int defaultCount)
    {
        this.mergeItem = mergeItem;
        this.countText = countText;
        this.defaultCountText = defaultCountText;
        this.count = count;
        this.defaultCount = defaultCount;
    }

    public void SetCountText()
    {
        if (countText != null)
            countText.text = "" + count;
        if (defaultCountText != null)
            defaultCountText.text = "(+" + defaultCount + ")";
    }

    public void UseCount()
    {
        if(defaultCount > 0)
        {
            defaultCount--;
            SetCountText();
            return;
        }
        if(count > 0)
        {
            count--;
            SetCountText();
            return;
        }
    }
    public bool IsAvailable() => count > 0 || defaultCount > 0;
}