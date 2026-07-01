using UnityEngine;

[System.Serializable]
public class GachaItemInfo
{
    public ItemData itemData;
    public int weight = 1;
}

[CreateAssetMenu(fileName = "Box", menuName = "GameData/Item/Box")]
public class ItemBoxData : DefaultData
{
    [Header("Price")]
    public Cost[] costs;

    [Header("Gacha")]
    public GachaItemInfo[] gachaItems;

    [Header("Box Animation")]
    public AnimationClip idleClip;
    public AnimationClip openClip;

    [Tooltip("상자 열기 Trigger 이름")]
    public string openTriggerName = "Open";

    public ItemData GetRandomItem()
    {
        if (gachaItems == null || gachaItems.Length == 0)
            return null;

        int totalWeight = 0;

        foreach (var item in gachaItems)
        {
            if (item == null || item.itemData == null || item.weight <= 0)
                continue;

            totalWeight += item.weight;
        }

        if (totalWeight <= 0)
            return null;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var item in gachaItems)
        {
            if (item == null || item.itemData == null || item.weight <= 0)
                continue;

            currentWeight += item.weight;

            if (randomValue < currentWeight)
                return item.itemData;
        }

        return null;
    }
}