using TMPro;

public sealed class PotionRecipeMaterialUI : DataInfoUI
{
    public TextMeshProUGUI requiredCount;
    public TextMeshProUGUI ownedCount;
    public ItemData Item { get; private set; }
    public int Required { get; private set; }
    public void Bind(ItemData item, int required, int owned)
    {
        Item = item; Required = required;
        SetData(item); SetCounts(owned);
    }
    public void SetCounts(int owned)
    {
        if (requiredCount != null) requiredCount.text = Required.ToString();
        if (ownedCount != null) ownedCount.text = owned.ToString();
    }
}
