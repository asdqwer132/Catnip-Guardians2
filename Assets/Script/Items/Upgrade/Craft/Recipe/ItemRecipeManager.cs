using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class ItemRecipeList
{
    public ItemSeries itemSeries;
    public List<ItemRecipeData> recipes = new List<ItemRecipeData>();
}

public class ItemRecipeManager : MonoBehaviour
{
    [Header("Recipes")]
    public List<ItemRecipeList> recipes = new List<ItemRecipeList>();
    public List<InventoryItem> currentMaterials = new List<InventoryItem>();

    public ItemData resultItem;
    public ItemData failedItem;
    [Header("Material Option")]
    public int maxMaterialCount = 4;
    public bool returnFailedItem = true;

    public System.Action onMaterialChanged;

    public bool isEmptyResult() { return resultItem == null; }
    public int GetCurrentMaterialCount()
    {
        int count = 0;
        foreach (var material in currentMaterials)
        {
            if(material.itemData != null) count++;
        }
        return count;
    }
    public List<InventoryItem> GetMaterials()
    {
        return currentMaterials;
    }
    public List<ItemRecipeData> GetAllRecipe()
    {
        List<ItemRecipeData> result = new List<ItemRecipeData>();

        foreach (ItemRecipeList list in recipes)
        {
            if (list == null)
                continue;
            foreach (var recipe in list.recipes)
            {
                result.Add(recipe);
            }
        }

        return result;
    }

    public void AddMaterial(ItemData itemData)
    {
        if (itemData == null)
            return;

        if (GetCurrentMaterialCount() >= maxMaterialCount)
        {
            Debug.LogWarning($"[ItemRecipeManager] 재료 슬롯이 가득 찼습니다. {GetCurrentMaterialCount()} / {maxMaterialCount}");
            return;
        }

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        bool removed = InventoryManager.instance.RemoveItem(itemData, 1);

        if (removed == false)
            return;

        // 중요:
        // 같은 아이템이어도 합치지 않고 한 칸씩 추가
        currentMaterials.Add(new InventoryItem(itemData, 1));

        onMaterialChanged?.Invoke();
    }

    public bool TryFillRecipe(ItemRecipeData recipe)
    {
        if (recipe == null)
        {
            Debug.LogWarning("[ItemRecipeManager] 자동으로 채울 레시피가 없습니다.");
            return false;
        }

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("[ItemRecipeManager] InventoryManager가 없습니다.");
            return false;
        }

        if (recipe.materials == null || recipe.materials.Length <= 0)
        {
            Debug.LogWarning($"[ItemRecipeManager] 레시피에 재료가 없습니다. Recipe: {GetRecipeName(recipe)}");
            return false;
        }

        int recipeMaterialCount = GetRecipeTotalMaterialCount(recipe);

        if (recipeMaterialCount > maxMaterialCount)
        {
            Debug.LogWarning(
                $"[ItemRecipeManager] 레시피 재료 개수가 최대 재료 개수를 초과합니다. " +
                $"Recipe: {GetRecipeName(recipe)} / 필요: {recipeMaterialCount} / 최대: {maxMaterialCount}"
            );

            return false;
        }

        if (CanFillRecipe(recipe) == false)
            return false;

        // 기존 조합 슬롯 재료는 먼저 인벤토리로 돌려놓고
        // 선택한 레시피 재료로 새로 채운다.
        ReturnMaterialsInternal(false);

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            ItemData itemData = material.itemData;

            for (int j = 0; j < material.amount; j++)
            {
                bool removed = InventoryManager.instance.RemoveItem(itemData, 1);

                if (removed == false)
                {
                    Debug.LogWarning(
                        $"[ItemRecipeManager] 자동 채움 중 재료 제거 실패. " +
                        $"Item: {GetItemName(itemData)}"
                    );

                    onMaterialChanged?.Invoke();
                    return false;
                }

                // 중요:
                // amount를 올리지 않고 1개짜리 슬롯으로 계속 추가
                currentMaterials.Add(new InventoryItem(itemData, 1));
            }
        }

        onMaterialChanged?.Invoke();
        return true;
    }
    private int GetRecipeTotalMaterialCount(ItemRecipeData recipe)
    {
        if (recipe == null || recipe.materials == null)
            return 0;

        int count = 0;

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            count += material.amount;
        }

        return count;
    }
    // 기존 RecipeSlotUI에서 TryFillRecipeOne을 부르고 있으면
    // 에러 안 나게 남겨둔다.
    public bool TryFillRecipeOne(ItemRecipeData recipe)
    {
        return TryFillRecipe(recipe);
    }

    private bool CanFillRecipe(ItemRecipeData recipe)
    {
        List<ItemData> checkedItems = new List<ItemData>();

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
            {
                Debug.LogWarning($"[ItemRecipeManager] 레시피에 잘못된 재료가 있습니다. Recipe: {GetRecipeName(recipe)}");
                return false;
            }

            ItemData itemData = material.itemData;

            if (checkedItems.Contains(itemData))
                continue;

            checkedItems.Add(itemData);

            int requiredAmount = GetRecipeRequiredAmount(recipe, itemData);

            // 기존 슬롯 재료는 자동 채움 전에 반환할 거라서
            // 현재 슬롯에 있는 같은 아이템도 보유량으로 계산한다.
            int inventoryAmount = InventoryManager.instance.GetItemAmount(itemData);
            int currentSlotAmount = GetCurrentMaterialAmount(itemData);

            int availableAmount = inventoryAmount + currentSlotAmount;

            if (availableAmount < requiredAmount)
            {
                Debug.LogWarning(
                    $"[ItemRecipeManager] 재료가 부족해서 자동으로 채울 수 없습니다. " +
                    $"Item: {GetItemName(itemData)} / 필요: {requiredAmount} / 보유: {availableAmount}"
                );

                return false;
            }
        }

        return true;
    }

    public void ReturnMaterial(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
            return;

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        int remainAmount = amount;
        bool returned = false;

        for (int i = currentMaterials.Count - 1; i >= 0; i--)
        {
            InventoryItem material = currentMaterials[i];

            if (material == null)
                continue;

            if (material.itemData != itemData)
                continue;

            int returnAmount = Mathf.Min(material.amount, remainAmount);

            InventoryManager.instance.AddItem(itemData, returnAmount);

            material.amount -= returnAmount;
            remainAmount -= returnAmount;
            returned = true;

            if (material.amount <= 0)
                currentMaterials.RemoveAt(i);

            if (remainAmount <= 0)
                break;
        }

        if (returned == false)
        {
            Debug.LogWarning("반환할 재료가 조합 슬롯에 없습니다.");
            return;
        }

        onMaterialChanged?.Invoke();
    }

    public void ReturnMaterials()
    {
        ReturnMaterialsInternal(true);
    }

    private void ReturnMaterialsInternal(bool notify)
    {
        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        foreach (InventoryItem material in currentMaterials)
        {
            if (material != null && material.itemData != null && material.amount > 0)
                InventoryManager.instance.AddItem(material.itemData, material.amount);
        }

        currentMaterials.Clear();

        if (notify)
            onMaterialChanged?.Invoke();
    }

    public void ClearMaterials()
    {
        currentMaterials.Clear();

        onMaterialChanged?.Invoke();
    }

    public virtual void Combine()
    {
        ItemRecipeData recipe = FindRecipe();

        if (recipe == null)
        {
            resultItem = returnFailedItem ? failedItem : null;
            return;
        }

        resultItem = recipe.resultItem;

    }

    protected ItemRecipeData FindRecipe()
    {
        foreach (ItemRecipeList list in recipes)
        {
            foreach (var recipe in list.recipes)
            {
                if (IsRecipeMatch(recipe))
                    return recipe;
            }
        }

        return null;
    }

    private bool IsRecipeMatch(ItemRecipeData recipe)
    {
        if (recipe == null || recipe.materials == null)
            return false;

        if (GetRecipeMaterialTypeCount(recipe) != GetCurrentMaterialTypeCount())
            return false;

        List<ItemData> checkedItems = new List<ItemData>();

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
                return false;

            ItemData itemData = material.itemData;

            if (checkedItems.Contains(itemData))
                continue;

            checkedItems.Add(itemData);

            int requiredAmount = GetRecipeRequiredAmount(recipe, itemData);
            int currentAmount = GetCurrentMaterialAmount(itemData);

            if (currentAmount != requiredAmount)
                return false;
        }

        return true;
    }

    private int GetRecipeRequiredAmount(ItemRecipeData recipe, ItemData itemData)
    {
        if (recipe == null || recipe.materials == null || itemData == null)
            return 0;

        int amount = 0;

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null)
                continue;

            if (material.itemData == itemData)
                amount += material.amount;
        }

        return amount;
    }

    private int GetCurrentMaterialAmount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        int amount = 0;

        for (int i = 0; i < currentMaterials.Count; i++)
        {
            InventoryItem material = currentMaterials[i];

            if (material == null)
                continue;

            if (material.itemData == itemData)
                amount += material.amount;
        }

        return amount;
    }

    private int GetRecipeMaterialTypeCount(ItemRecipeData recipe)
    {
        if (recipe == null || recipe.materials == null)
            return 0;

        List<ItemData> uniqueItems = new List<ItemData>();

        for (int i = 0; i < recipe.materials.Length; i++)
        {
            RecipeMaterial material = recipe.materials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            if (uniqueItems.Contains(material.itemData))
                continue;

            uniqueItems.Add(material.itemData);
        }

        return uniqueItems.Count;
    }

    private int GetCurrentMaterialTypeCount()
    {
        List<ItemData> uniqueItems = new List<ItemData>();

        for (int i = 0; i < currentMaterials.Count; i++)
        {
            InventoryItem material = currentMaterials[i];

            if (material == null || material.itemData == null || material.amount <= 0)
                continue;

            if (uniqueItems.Contains(material.itemData))
                continue;

            uniqueItems.Add(material.itemData);
        }

        return uniqueItems.Count;
    }

    private string GetRecipeName(ItemRecipeData recipe)
    {
        if (recipe == null)
            return "null";

        if (recipe.resultItem == null)
            return recipe.name;

        return recipe.GetDataName();
    }

    private string GetItemName(ItemData itemData)
    {
        if (itemData == null)
            return "null";

        return itemData.GetDataName();
    }
}