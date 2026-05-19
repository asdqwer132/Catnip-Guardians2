using UnityEngine;

[System.Serializable]
public class GachaItemInfo
{
    public ItemData itemData;
    public int weight = 1;
}

[CreateAssetMenu(fileName = "Box", menuName = "Game/Box")]
public class ItemBoxData : DefaultData
{
    [Header("Price")]
    public Cost[] costs;

    [Header("Gacha")]
    public GachaItemInfo[] gachaItems;

    public ItemData GetRandomItem()
    {
        if (gachaItems == null || gachaItems.Length == 0)
            return null;

        int totalWeight = 0;

        foreach (var item in gachaItems)
        {
            if (item == null)
                continue;

            if (item.itemData == null)
                continue;

            if (item.weight <= 0)
                continue;

            totalWeight += item.weight;
        }

        if (totalWeight <= 0)
            return null;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var item in gachaItems)
        {
            if (item == null)
                continue;

            if (item.itemData == null)
                continue;

            if (item.weight <= 0)
                continue;

            currentWeight += item.weight;

            if (randomValue < currentWeight)
            {
                return item.itemData;
            }
        }

        return null;
    }
}