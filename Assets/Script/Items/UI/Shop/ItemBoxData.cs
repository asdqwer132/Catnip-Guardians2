using Mono.Cecil;
using UnityEngine;

[CreateAssetMenu(fileName = "Box", menuName = "Game/Box")]
public class ItemBoxData : ScriptableObject
{
    public string boxName;
    public Sprite icon;

    public CurrencyType priceType;
    public int price;

    public GachaItemInfo[] gachaItems;

    public ItemData GetRandomItem()
    {
        int totalWeight = 0;

        foreach (var item in gachaItems)
        {
            totalWeight += item.weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var item in gachaItems)
        {
            currentWeight += item.weight;

            if (randomValue < currentWeight)
            {
                return item.itemData;
            }
        }

        return null;
    }
}

[System.Serializable]
public class GachaItemInfo
{
    public ItemData itemData;
    public int weight;
}