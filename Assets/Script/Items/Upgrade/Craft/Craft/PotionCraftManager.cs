using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, sourceAssembly: "Assembly-CSharp", sourceClassName: "ItemCombinationManager")]
[AddComponentMenu("Items/Crafting/Potion Craft Manager")]
public class PotionCraftManager : ItemRecipeManager
{
    public static PotionCraftManager instance;
    public CraftSuccessEffectUI craftSuccessEffectUI;
    public PotionRecipeHistory recipeHistory;
    private bool preparingRecipe;
    private bool crafting;

    private ItemRecipeData GetCraftableRecipe()
    {
        if (preparingRecipe || crafting || GetCurrentMaterialCount() < 2 || InventoryManager.instance == null)
            return null;

        var recipe = FindRecipe();
        return recipe != null && recipe.resultItem != null ? recipe : null;
    }

    // Discovery affects presentation only; the first craft must still be possible.
    public bool CanCraft() => GetCraftableRecipe() != null;

    public override ItemData GetPreviewResult()
    {
        var recipe = GetCraftableRecipe();
        return recipe != null && recipeHistory != null && recipeHistory.HasCrafted(recipe.resultItem)
            ? recipe.resultItem : null;
    }

    public int GetAvailableMaterialCount(ItemData item)
    {
        long amount = InventoryManager.instance != null ? InventoryManager.instance.GetItemAmount(item) : 0;
        foreach (var entry in currentMaterials)
            if (entry?.itemData == item && entry.amount > 0) amount += entry.amount;
        return (int)Math.Min(int.MaxValue, amount);
    }
    private bool TryPlanRecipe(ItemRecipeData recipe, out List<InventoryItem> required, out List<int> slots)
    {
        required = new List<InventoryItem>(); slots = new List<int>();
        if (recipe == null || InventoryManager.instance == null || !recipe.TryGetRequiredMaterials(required)) return false;
        long total = 0;
        foreach (var entry in required)
        {
            total += entry.amount;
            if (GetAvailableMaterialCount(entry.itemData) < entry.amount) return false;
        }
        for (int i = 0; i < maxMaterialCount; i++) if (!IsMaterialSlotLocked(i)) slots.Add(i);
        return total >= 2 && total <= slots.Count;
    }
    public bool CanPrepareRecipe(ItemRecipeData recipe)
        => !preparingRecipe && !crafting && TryPlanRecipe(recipe, out _, out _);

    // Reserve only the difference between the old and new material sets. A failed
    // reservation leaves existing crafting slots untouched and refunds withdrawals.
    public bool TryPrepareRecipe(ItemRecipeData recipe)
    {
        if (preparingRecipe || crafting || !TryPlanRecipe(recipe, out var required, out var slots)) return false;
        var inventory = InventoryManager.instance;
        var previous = new List<InventoryItem>();
        var held = new Dictionary<ItemData, int>();
        foreach (var entry in currentMaterials)
        {
            previous.Add(entry == null ? null : new InventoryItem(entry.itemData, entry.amount));
            if (entry?.itemData == null || entry.amount <= 0) continue;
            held.TryGetValue(entry.itemData, out int amount); held[entry.itemData] = amount + entry.amount;
        }
        var withdrawn = new List<InventoryItem>();
        bool committed = false;
        preparingRecipe = true;
        try
        {
            foreach (var entry in required)
            {
                held.TryGetValue(entry.itemData, out int amount);
                int additional = entry.amount - amount;
                if (additional <= 0) continue;
                if (!inventory.RemoveItem(entry.itemData, additional)) return false;
                withdrawn.Add(new InventoryItem(entry.itemData, additional));
            }
            // Inventory callbacks may change locks or materials. Do not overwrite a newer selection.
            if (previous.Count != currentMaterials.Count) return false;
            for (int i = 0; i < previous.Count; i++)
            {
                var a = previous[i]; var b = currentMaterials[i];
                if (a == null ? b != null : b == null || a.itemData != b.itemData || a.amount != b.amount) return false;
            }
            foreach (int slot in slots) if (IsMaterialSlotLocked(slot)) return false;
            currentMaterials.Clear();
            int next = 0;
            foreach (var entry in required)
            {
                for (int i = 0; i < entry.amount; i++)
                {
                    int index = slots[next++];
                    while (currentMaterials.Count <= index) currentMaterials.Add(null);
                    currentMaterials[index] = new InventoryItem(entry.itemData, 1);
                }
                held.TryGetValue(entry.itemData, out int amount); held[entry.itemData] = amount - entry.amount;
            }
            committed = true;
            foreach (var entry in held) if (entry.Value > 0) inventory.AddItem(entry.Key, entry.Value);
            SelectMaterialSlot(-1);
            return true;
        }
        finally
        {
            if (!committed) foreach (var entry in withdrawn) inventory.AddItem(entry.itemData, entry.amount);
            preparingRecipe = false;
            onMaterialChanged?.Invoke();
        }
    }

    private void Awake() => instance = this;

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public override void Combine() => TryCombine(out _);

    public bool TryCombine(out ItemData craftedItem)
    {
        craftedItem = null;
        var recipe = GetCraftableRecipe();
        if (recipe == null)
            return false;

        crafting = true;
        try
        {
            // Slots already reserve inventory quantities through ItemRecipeManager.
            // Spend those reservations before granting the potion so callbacks cannot
            // craft twice or return the same materials. Do not charge inventory again.
            currentMaterials.Clear();
            resultItem = craftedItem = recipe.resultItem;
            InventoryManager.instance.AddItem(craftedItem, 1);
            if (recipeHistory != null) recipeHistory.RecordCrafted(craftedItem);
            if (craftSuccessEffectUI != null) craftSuccessEffectUI.Play(craftedItem.icon);
            return true;
        }
        finally
        {
            crafting = false;
            onMaterialChanged?.Invoke();
        }
    }
}
