using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class ItemRecipeList
{
    public ItemSeries itemSeries;
    public int depth;
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
    public event Action<int> MaterialSlotSelected;
    public event Action MaterialsReturned;
    public int SelectedMaterialSlot { get; private set; } = -1;
    private readonly HashSet<int> lockedMaterialSlots = new HashSet<int>();

    public InventoryItem GetMaterialAt(int index) => index >= 0 && index < currentMaterials.Count ? currentMaterials[index] : null;
    public bool IsMaterialSlotLocked(int index) => index < 0 || index >= maxMaterialCount || lockedMaterialSlots.Contains(index);

    public void SelectMaterialSlot(int index)
    {
        if (index >= 0 && IsMaterialSlotLocked(index)) return;
        if (SelectedMaterialSlot == index) return;
        SelectedMaterialSlot = index;
        MaterialSlotSelected?.Invoke(index);
    }

    public void SetMaterialSlotLocked(int index, bool locked)
    {
        if (locked) lockedMaterialSlots.Add(index);
        else lockedMaterialSlots.Remove(index);
        if (locked && SelectedMaterialSlot == index) SelectMaterialSlot(-1);
    }

    private int FindReadyMaterialSlot()
    {
        for (int i = 0; i < maxMaterialCount; i++)
        {
            var entry = GetMaterialAt(i);
            if (!IsMaterialSlotLocked(i) && (entry == null || entry.itemData == null || entry.amount <= 0)) return i;
        }
        return -1;
    }

    private void SetMaterialAt(int index, InventoryItem item)
    {
        while (currentMaterials.Count <= index) currentMaterials.Add(null);
        currentMaterials[index] = item;
    }

    public bool TryAddMaterial(ItemData itemData)
    {
        InventoryManager inventory = InventoryManager.instance;
        if (itemData == null || inventory == null) return false;
        int index = SelectedMaterialSlot >= 0 ? SelectedMaterialSlot : FindReadyMaterialSlot();
        if (IsMaterialSlotLocked(index)) return false;
        InventoryItem previous = GetMaterialAt(index);
        if (previous != null && previous.itemData == itemData && previous.amount == 1) return true;
        if (!inventory.RemoveItem(itemData, 1)) return false;
        SetMaterialAt(index, new InventoryItem(itemData, 1));
        if (previous != null && previous.itemData != null && previous.amount > 0)
            inventory.AddItem(previous.itemData, previous.amount);
        onMaterialChanged?.Invoke();
        return true;
    }

    public void ReturnMaterialAt(int index)
    {
        var entry = GetMaterialAt(index);
        if (entry == null || entry.itemData == null || InventoryManager.instance == null) return;
        currentMaterials[index] = null;
        InventoryManager.instance.AddItem(entry.itemData, entry.amount);
        onMaterialChanged?.Invoke();
        MaterialsReturned?.Invoke();
    }

    public bool isEmptyResult() { return resultItem == null; }
    public int GetCurrentMaterialCount()
    {
        int count = 0;
        foreach (var material in currentMaterials)
        {
            if (material != null && material.itemData != null && material.amount > 0) count++;
        }
        return count;
    }
    public List<InventoryItem> GetMaterials()
    {
        return currentMaterials;
    }
    public ItemRecipeList GetRecipeListBySeries(ItemSeries itemSeries)
    {
        foreach (ItemRecipeList recipeList in recipes)
        {
            if (recipeList == null)
                continue;

            if (recipeList.itemSeries == itemSeries)
                return recipeList;
        }

        return null;
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

    public void AddMaterial(ItemData itemData) => TryAddMaterial(itemData);

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

        int availableSlots = 0;
        for (int slot = 0; slot < maxMaterialCount; slot++)
            if (!IsMaterialSlotLocked(slot)) availableSlots++;
        if (recipeMaterialCount > availableSlots)
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
                SetMaterialAt(FindReadyMaterialSlot(), new InventoryItem(itemData, 1));
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
                currentMaterials[i] = null;

            if (remainAmount <= 0)
                break;
        }

        if (returned == false)
        {
            Debug.LogWarning("반환할 재료가 조합 슬롯에 없습니다.");
            return;
        }

        onMaterialChanged?.Invoke();
        MaterialsReturned?.Invoke();
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
        {
            onMaterialChanged?.Invoke();
            MaterialsReturned?.Invoke();
        }
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

    // Preview only: selecting materials must not craft or use the resulting item.
    public ItemData GetPreviewResult()
    {
        ItemRecipeData recipe = FindRecipe();
        return recipe != null ? recipe.resultItem : returnFailedItem ? failedItem : null;
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