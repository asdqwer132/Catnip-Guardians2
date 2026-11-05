using UnityEngine;

[System.Serializable]
public class GachaItemInfo
{
    public ItemData itemData;
    public int weight = 1;
}

[CreateAssetMenu(fileName = "Box", menuName = "03.Upgrade/Shop/Box")]
public class ItemBoxData : DefaultData
{
    [Header("Extra Data / Price")]
    public Cost[] costs;

    [Header("Extra Data / Gacha")]
    public ItemSeries itemSeries;
    public ItemGrade grade;
    public GachaItemInfo[] gachaItems;

    [Header("Extra Data / Box Animation")]
    public AnimationClip idleClip;
    [Tooltip("구매 성공 시 선택한 상점 슬롯 아이콘에서 한 번 재생할 클립입니다.")]
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