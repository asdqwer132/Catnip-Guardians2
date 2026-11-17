using System;
using UnityEngine;

[Serializable]
public class MergeItemMaterial
{
    public MergeItem mergeItem;
    public MergeUI mergeUI;
    [Min(0)] public int count;

    public MergeItemMaterial(MergeItem mergeItem, MergeUI mergeUI, int count)
    {
        this.mergeItem = mergeItem;
        this.mergeUI = mergeUI;
        this.count = Mathf.Max(0, count);
    }

    public void SetCountText() => mergeUI?.SetCount(count);
    public void SetActive() => mergeUI?.SetSlot(IsAvailable());
    public void UseCount()
    {
        if (count > 0) count--;
        SetCountText();
    }
    public bool IsAvailable() => count > 0;
}
