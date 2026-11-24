using System.Collections.Generic;

/// <summary>Snapshot of the materials committed to one crafting attempt.</summary>
public sealed class CraftContext
{
    private readonly Dictionary<ItemData, int> materialAmounts = new Dictionary<ItemData, int>();

    public int TotalMaterialCount { get; private set; }
    public int MaterialTypeCount => materialAmounts.Count;
    public IEnumerable<KeyValuePair<ItemData, int>> Materials => materialAmounts;
    public CraftGaugeSnapshot Gauges { get; private set; }

    public CraftContext(IEnumerable<InventoryItem> materials)
        : this(materials, CraftGaugeSnapshot.Empty)
    {
    }

    public CraftContext(IEnumerable<InventoryItem> materials, CraftGaugeSnapshot gauges)
    {
        Gauges = gauges ?? CraftGaugeSnapshot.Empty;
        if (materials == null)
            return;

        foreach (InventoryItem material in materials)
        {
            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            int currentAmount;
            materialAmounts.TryGetValue(material.itemData, out currentAmount);
            materialAmounts[material.itemData] = currentAmount + material.amount;
            TotalMaterialCount += material.amount;
        }
    }

    public int GetAmount(ItemData item)
    {
        if (item == null)
            return 0;

        int amount;
        return materialAmounts.TryGetValue(item, out amount) ? amount : 0;
    }
}
