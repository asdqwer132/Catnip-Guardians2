using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class EquipmentData
{
    public List<string> itemIds = new List<string>();
    public List<int> equippedChests = new List<int>();
    public List<SavedPotionSlot> potions = new List<SavedPotionSlot>();

    public EquipmentData Copy() => new EquipmentData
    {
        itemIds = itemIds != null ? new List<string>(itemIds) : new List<string>(),
        equippedChests = equippedChests != null ? new List<int>(equippedChests) : new List<int>(),
        potions = potions != null ? potions.ConvertAll(slot => slot?.Copy()) : new List<SavedPotionSlot>()
    };
}

[Serializable]
public sealed class SavedPotionSlot
{
    public int slotIndex;
    public bool locked;
    public string assetKey;
    public string itemId;
    public SavedPotionSlot Copy() => (SavedPotionSlot)MemberwiseClone();
}

public static class EquipmentSave
{
    public const string SaveFileName = "equipment.json";
    private static string PathToSave => Path.Combine(Application.persistentDataPath, SaveFileName);
    public static bool Exists => File.Exists(PathToSave);
    private static EquipmentData current;
    private static UpgradeProgressCatalog Catalog => Resources.Load<UpgradeProgressCatalog>("UpgradeProgressCatalog");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache() => current = null;

    public static EquipmentData Current
    {
        get
        {
            if (current != null) return current;
            try
            {
                if (File.Exists(PathToSave))
                    current = JsonUtility.FromJson<EquipmentData>(File.ReadAllText(PathToSave));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("[Equipment] " + error.Message);
            }
            current = current?.Copy() ?? new EquipmentData();
            return current;
        }
    }

    public static bool Write(EquipmentData data)
    {
        try
        {
            new MergeBoxUpgradeFileStore(PathToSave).Write(JsonUtility.ToJson(data, true));
            current = data;
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning("[Equipment] " + error.Message);
            return false;
        }
    }

    public static MergeBoxEquipmentData FindChest(int number) =>
        Catalog?.entries.Find(entry => entry.asset is MergeBoxEquipmentData chest && chest.boxIndex == number)?.asset
            as MergeBoxEquipmentData;

    public static MergeBoxEquipmentData ResolveChest(EquipmentData equipment, IReadOnlyList<int> unlocked)
    {
        if (equipment != null)
            foreach (int number in equipment.equippedChests)
                if (IsChestUnlocked(number, unlocked) && FindChest(number) is { } chest) return chest;

        return FindChest(1);
    }

    public static bool IsChestUnlocked(int number, IReadOnlyList<int> unlocked)
    {
        if (number == 1) return true;
        if (number < 2 || number > 8 || unlocked == null) return false;
        foreach (int value in unlocked)
            if (value == number) return true;
        return false;
    }

    public static bool StorePotions(IReadOnlyList<PotionEquipmentSnapshot> slots)
    {
        if (slots == null || slots.Count == 0) return Current.potions.Count > 0;
        var data = Current.Copy();
        var catalog = Catalog;
        data.potions.Clear();
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            var entry = slot.potion != null ? catalog?.entries.Find(candidate => candidate.asset == slot.potion) : null;
            data.potions.Add(new SavedPotionSlot
            {
                slotIndex = slot.slotIndex, locked = slot.locked,
                assetKey = entry?.key ?? "", itemId = slot.potion != null ? slot.potion.dataId : ""
            });
        }

        var previous = Current.potions;
        if (previous.Count == data.potions.Count)
        {
            bool unchanged = true;
            for (int i = 0; i < previous.Count; i++)
            {
                var a = previous[i];
                var b = data.potions[i];
                if (a == null || a.slotIndex != b.slotIndex || a.locked != b.locked ||
                    a.assetKey != b.assetKey || a.itemId != b.itemId) { unchanged = false; break; }
            }
            if (unchanged) return true;
        }
        return Write(data);
    }

    public static List<PotionEquipmentSnapshot> RestorePotions()
    {
        var slots = new List<PotionEquipmentSnapshot>();
        var catalog = Catalog;
        foreach (var slot in Current.potions)
        {
            if (slot == null) continue;
            var potion = !string.IsNullOrEmpty(slot.assetKey) ?
                catalog?.entries.Find(entry => entry.key == slot.assetKey)?.asset as ItemData : null;
            if (potion == null && !string.IsNullOrEmpty(slot.itemId))
                potion = catalog?.entries.Find(entry => entry.asset is ItemData item &&
                    PotionEquipmentManager.IsPotion(item) && item.dataId == slot.itemId)?.asset as ItemData;
            slots.Add(new PotionEquipmentSnapshot { slotIndex = slot.slotIndex, locked = slot.locked, potion = potion });
        }
        return slots;
    }

    public static void ResetCache() => current = null;
}
