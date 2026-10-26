using System;
using TMPro;
using UnityEngine;
[Serializable]
public class MergeItemMaterial
{
    public MergeItem mergeItem;
    public MergeUI mergeUI;
    public int count;
    public int defaultCount;

    public MergeItemMaterial(MergeItem mergeItem, MergeUI mergeUI, int count, int defaultCount)
    {
        this.mergeItem = mergeItem;
        this.mergeUI = mergeUI;
        this.count = count;
        this.defaultCount = defaultCount;
    }

    public void SetCountText() => mergeUI.SetCount(count, defaultCount);
    public void SetActive() => mergeUI.SetSlot(IsAvailable());
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