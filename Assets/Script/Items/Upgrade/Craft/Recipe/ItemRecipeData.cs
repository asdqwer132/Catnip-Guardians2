using NUnit.Framework.Interfaces;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "GameData/Item/Recipe")]
public class ItemRecipeData : ScriptableObject, ISearchable
{
    public int tier;
    public ItemGrade itemGrade;
    public ItemSeries itemSeries;
    public RecipeMaterial[] materials;

    public ItemData resultItem;

    public Sprite Icon => resultItem.icon;
    public string GetDataName() => resultItem.GetDataName();
    public ItemGrade GetGrade() { return itemGrade; }
    public ItemCategory GetItemCategory() { return ItemCategory.None; }
    public ItemSeries GetItemSeries() { return itemSeries; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemGrade == ItemGrade.None)
            itemGrade = resultItem.grade;
        if (itemSeries == ItemSeries.None)
            itemSeries = resultItem.series;

    }
#endif
}

[System.Serializable]
public class RecipeMaterial
{
    public ItemData itemData;
    public int amount;
}