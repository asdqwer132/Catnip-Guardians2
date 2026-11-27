using System;
using UnityEngine;

[Serializable]
internal sealed class UpgradeDebugStart
{
    [Serializable]
    public sealed class ItemAmount
    {
        public string itemId;
        public int amount;
    }

    public int defaultItemAmount;
    public int defaultCurrencyAmount;
    public ItemAmount[] items;
    public Cost[] currencies;

    public static UpgradeDebugStart Load()
    {
        var file = Resources.Load<TextAsset>("Data/UpgradeDebugStart");
        return file != null ? JsonUtility.FromJson<UpgradeDebugStart>(file.text) ?? new UpgradeDebugStart()
            : new UpgradeDebugStart();
    }

    public int GetAmount(ItemData item)
        => Array.Find(items ?? Array.Empty<ItemAmount>(), entry => entry != null && entry.itemId == item.dataId)
            ?.amount ?? defaultItemAmount;
}
