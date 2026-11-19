using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GachaAmountChance
{
    [Min(1)] public int count = 1;
    [Min(0f)] public float weight = 1f;
}

[Serializable]
public class GachaItemInfo
{
    public ItemData itemData;
    [Range(0f,1f)] public float probability = 1f;
    public GachaAmountChance[] amounts = { new GachaAmountChance() };
    public int RollAmount()
    {
        if (amounts == null || amounts.Length == 0) return 1;
        double total = 0;
        foreach (var entry in amounts) if (Valid(entry)) total += entry.weight;
        if (total <= 0) return 0;
        double roll = UnityEngine.Random.value * total;
        int last = 0;
        foreach (var entry in amounts)
        {
            if (!Valid(entry)) continue;
            last = entry.count; roll -= entry.weight;
            if (roll < 0) return entry.count;
        }
        return last;
    }
    private static bool Valid(GachaAmountChance value) => value != null && value.count > 0 && value.weight > 0 && !float.IsInfinity(value.weight);
}

[CreateAssetMenu(fileName = "Box", menuName = "Upgrade/Shop/Box")]
public class ItemBoxData : DefaultData
{
    [Space(10)] public Cost[] costs;
    public GachaItemInfo[] gachaItems;
    [Space(10), Tooltip("평상시에는 기본 아이콘을 사용하고 구매 성공 시에만 이 클립을 재생합니다.")]
    public AnimationClip openClip;

    public List<InventoryItem> RollRewards()
    {
        var rewards = new List<InventoryItem>();
        foreach (var entry in gachaItems ?? Array.Empty<GachaItemInfo>())
        {
            if (entry == null || entry.itemData == null || !(entry.probability > 0f)) continue;
            if (entry.probability < 1f && UnityEngine.Random.value >= entry.probability) continue;
            int amount = entry.RollAmount();
            if (amount <= 0) continue;
            var existing = rewards.Find(r => r.itemData == entry.itemData);
            if (existing == null) rewards.Add(new InventoryItem(entry.itemData, amount));
            else existing.amount = checked(existing.amount + amount);
        }
        return rewards;
    }
}
