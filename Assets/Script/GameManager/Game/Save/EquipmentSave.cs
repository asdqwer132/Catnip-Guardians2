using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class EquipmentData
{
    public List<string> itemIds = new List<string>();
    public List<int> equippedChests = new List<int>();
    public BoardLoadout board;
    public List<SavedPotionSlot> potions = new List<SavedPotionSlot>();

    public EquipmentData Copy() => new EquipmentData
    {
        itemIds = itemIds != null ? new List<string>(itemIds) : new List<string>(),
        equippedChests = equippedChests != null ? new List<int>(equippedChests) : new List<int>(),
        board = board?.IsConfigured == true ? board.Copy() : null,
        potions = potions != null ? potions.ConvertAll(slot => slot?.Copy()) : new List<SavedPotionSlot>()
    };
}

[Serializable]
public sealed class BoardLoadout
{
    public const int NoEgg = -1;

    public int eggBoxIndex;
    public List<int> skillIndices = new List<int>();
    public List<string> pinIds = new List<string>();

    public static bool AcceptsMaterial(ItemData item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.dataId)) return false;
        if (item.dataType == DataType.ItemMaterial) return true;

        // Existing material assets can still be tagged Item in the data workbook.
        string prefix = item.series + "Material";
        return item.dataType == DataType.Item && (item.series is >= ItemSeries.Weapon and <= ItemSeries.Magic) &&
            item.dataId.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(item.dataId.Substring(prefix.Length), out int number) && number > 0;
    }

    public bool IsConfigured => eggBoxIndex != 0;
    public BoardLoadout Copy() => new BoardLoadout
    {
        eggBoxIndex = eggBoxIndex,
        skillIndices = skillIndices != null ? new List<int>(skillIndices) : new List<int>(),
        pinIds = pinIds != null ? new List<string>(pinIds) : new List<string>()
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
    public static bool Exists => GameManager.DebugMode ? debugEquipment != null : File.Exists(PathToSave);
    private static EquipmentData debugEquipment;
    private static EquipmentData current;
    private static UpgradeProgressCatalog Catalog => Resources.Load<UpgradeProgressCatalog>("UpgradeProgressCatalog");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache() => current = debugEquipment = null;

    public static EquipmentData Current
    {
        get
        {
            if (GameManager.DebugMode) return debugEquipment ??= new EquipmentData();
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
        if (GameManager.DebugMode)
        {
            debugEquipment = data;
            return true;
        }
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

    public static MergeBoxEquipmentData ResolveEgg(EquipmentData equipment, IReadOnlyList<string> unlocked)
    {
        int index = equipment?.board?.eggBoxIndex ?? 0;
        if (EquipmentUnlocks.HasEgg(unlocked, index) && FindChest(index) is { plant: not null } selected)
            return selected;

        for (int number = 1; number <= EquipmentUnlocks.BoxCount; number++)
            if (EquipmentUnlocks.HasEgg(unlocked, number) && FindChest(number) is { plant: not null } egg)
                return egg;
        return null;
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

    public static void ResetCache()
    {
        if (GameManager.DebugMode) debugEquipment = null;
        else current = null;
    }
}
