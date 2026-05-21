using System.Collections.Generic;
using UnityEngine;

public class ItemCombinationManager : MonoBehaviour
{
    public static ItemCombinationManager instance;

    [Header("Recipes")]
    public List<ItemRecipeData> recipes = new List<ItemRecipeData>();
    public List<InventoryItem> currentMaterials = new List<InventoryItem>();

    public System.Action onMaterialChanged;

    void Awake()
    {
        instance = this;
    }

    public void AddMaterial(ItemData itemData)
    {
        if (itemData == null)
            return;

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        bool removed = InventoryManager.instance.RemoveItem(itemData, 1);

        if (removed == false)
        {
            //Debug.Log("인벤토리에 아이템이 없습니다.");
            return;
        }

        InventoryItem existing =
            currentMaterials.Find(x => x.itemData == itemData);

        if (existing != null)
        {
            existing.amount++;
        }
        else
        {
            currentMaterials.Add(new InventoryItem(itemData, 1));
        }

        //Debug.Log(itemData.itemName + " 조합 슬롯에 넣음");

        onMaterialChanged?.Invoke();
    }

    public void ReturnMaterial(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
            return;

        InventoryItem material =
            currentMaterials.Find(x => x.itemData == itemData);

        if (material == null)
        {
            Debug.LogWarning("반환할 재료가 조합 슬롯에 없습니다.");
            return;
        }

        if (material.amount < amount)
            amount = material.amount;

        InventoryManager.instance.AddItem(itemData, amount);

        material.amount -= amount;

        if (material.amount <= 0)
        {
            currentMaterials.Remove(material);
        }

        onMaterialChanged?.Invoke();

        //Debug.Log(itemData.itemName + " 조합 재료 반환");
    }

    public void ReturnMaterials()
    {
        foreach (InventoryItem material in currentMaterials)
        {
            if (material.itemData != null && material.amount > 0)
            {
                InventoryManager.instance.AddItem(material.itemData, material.amount);
            }
        }

        currentMaterials.Clear();

        onMaterialChanged?.Invoke();
    }

    public void ClearMaterials()
    {
        currentMaterials.Clear();

        onMaterialChanged?.Invoke();
    }

    public void Combine()
    {
        ItemRecipeData recipe = FindRecipe();

        if (recipe == null)
        {
            //Debug.Log("조합 실패");
            return;
        }

        InventoryManager.instance.AddItem(recipe.resultItem, 1);

        ClearMaterials();
    }

    ItemRecipeData FindRecipe()
    {
        foreach (var recipe in recipes)
        {
            if (IsRecipeMatch(recipe))
                return recipe;
        }

        return null;
    }

    bool IsRecipeMatch(ItemRecipeData recipe)
    {
        if (recipe.materials.Length != currentMaterials.Count)
            return false;

        foreach (var material in recipe.materials)
        {
            InventoryItem current =
                currentMaterials.Find(x => x.itemData == material.itemData);

            if (current == null)
                return false;

            if (current.amount != material.amount)
                return false;
        }

        return true;
    }
}