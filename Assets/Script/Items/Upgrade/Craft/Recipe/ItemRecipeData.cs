using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "03.Upgrade/Crafting/Recipe")]
public class ItemRecipeData : ScriptableObject, ISearchable
{
    public int tier;
    public Grade itemGrade;
    public ItemSeries itemSeries;
    public RecipeMaterial[] materials;

    public ItemData resultItem;

    // Normalize repeated material rows so counts and availability use the same requirements.
    public bool TryGetRequiredMaterials(List<InventoryItem> result)
    {
        result.Clear();
        if (resultItem == null || materials == null || materials.Length == 0) return false;
        foreach (var material in materials)
        {
            if (material?.itemData == null || material.amount <= 0) { result.Clear(); return false; }
            var existing = result.Find(entry => entry.itemData == material.itemData);
            if (existing == null) result.Add(new InventoryItem(material.itemData, material.amount));
            else
            {
                if (material.amount > int.MaxValue - existing.amount) { result.Clear(); return false; }
                existing.amount += material.amount;
            }
        }
        return result.Count > 0;
    }

    public Sprite Icon => resultItem.icon;
    public string GetDataName() => resultItem.GetDataName();
    public Grade GetGrade() { return itemGrade; }
    public Category GetItemCategory() { return Category.None; }
    public ItemSeries GetItemSeries() { return itemSeries; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resultItem == null) return;
        if (itemGrade == Grade.None)
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