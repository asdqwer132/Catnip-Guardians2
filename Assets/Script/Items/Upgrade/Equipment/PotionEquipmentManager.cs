using System;
using UnityEngine;

[Serializable]
public sealed class PotionEquipmentEntry
{
    public bool locked;
    public ItemData potion;
}

// Equipment owns one reserved inventory unit per slot. Closing its UI changes no stock.
public sealed class PotionEquipmentManager : MonoBehaviour
{
    public InventoryManager inventory;
    [SerializeField] private PotionEquipmentEntry[] slots =
    {
        new PotionEquipmentEntry(), new PotionEquipmentEntry(),
        new PotionEquipmentEntry(), new PotionEquipmentEntry(),
        new PotionEquipmentEntry(), new PotionEquipmentEntry(),
        new PotionEquipmentEntry(), new PotionEquipmentEntry()
    };
    private bool changing;
    public event Action Changed;
    public int SlotCount => slots != null ? slots.Length : 0;
    private InventoryManager Inventory => inventory != null ? inventory : InventoryManager.instance;
    private bool ValidIndex(int index) => index >= 0 && index < SlotCount && slots[index] != null;
    public static bool IsPotion(ItemData item) => item != null && item.series == ItemSeries.Potions;
    public bool IsLocked(int index) => !ValidIndex(index) || slots[index].locked;
    public ItemData GetPotion(int index) => ValidIndex(index) ? slots[index].potion : null;
    public int GetAvailableCount(ItemData item) => Inventory != null ? Inventory.GetItemAmount(item) : 0;

    public bool SetSlotLocked(int index, bool locked)
    {
        if (changing || !ValidIndex(index)) return false;
        if (slots[index].locked == locked) return true;
        slots[index].locked = locked;
        Changed?.Invoke();
        return true;
    }

    public bool TryEquip(int index, ItemData potion)
        => ChangePotion(index, potion, replace: false);

    public bool TryReplace(int index, ItemData potion)
        => ChangePotion(index, potion, replace: true);

    private bool ChangePotion(int index, ItemData potion, bool replace)
    {
        var stock = Inventory;
        if (changing || stock == null || IsLocked(index) || !IsPotion(potion)) return false;
        var previous = slots[index].potion;
        if (replace != (previous != null)) return false;

        changing = true;
        bool changed = false;
        try
        {
            // Confirmed operations reserve the new unit first; a shortage leaves equipment intact.
            if (!stock.RemoveItem(potion, 1)) return false;
            slots[index].potion = potion;
            if (previous != null) stock.AddItem(previous, 1);
            changed = true;
            return true;
        }
        finally
        {
            changing = false;
            if (changed) Changed?.Invoke();
        }
    }

    public bool TryUnequip(int index)
    {
        var stock = Inventory;
        if (changing || stock == null || IsLocked(index) || GetPotion(index) == null) return false;
        changing = true;
        try
        {
            var previous = slots[index].potion;
            slots[index].potion = null;
            stock.AddItem(previous, 1);
            return true;
        }
        finally
        {
            changing = false;
            Changed?.Invoke();
        }
    }
}
