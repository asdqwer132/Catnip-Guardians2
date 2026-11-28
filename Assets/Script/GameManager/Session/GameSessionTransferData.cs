using System;
using System.Collections.Generic;

/// <summary>스킬트리에서 등록한 실행 아이템입니다.</summary>
[Serializable]
public sealed class SkillItemSnapshot
{
    public ItemData itemData;

    public SkillItemSnapshot() { }

    public SkillItemSnapshot(RegisteredBuffSkillItem item)
    {
        if (item == null)
            return;

        itemData = item.itemData;
    }

    public SkillItemSnapshot Copy()
    {
        return new SkillItemSnapshot { itemData = itemData };
    }
}

[Serializable]
public sealed class PotionEquipmentSnapshot
{
    public int slotIndex;
    public bool locked;
    public ItemData potion;
    public PotionEquipmentSnapshot Copy() => new PotionEquipmentSnapshot { slotIndex = slotIndex, locked = locked, potion = potion };
}

/// <summary>업그레이드 씬 → 제너럴 씬. 아이템 에셋·수량과 장착 정보를 보관합니다.</summary>
[Serializable]
public sealed class UpgradeToGeneralData
{
    public bool legacySkillsOnly;
    public EquipmentData equipment;
    [NonSerialized] public MergeBoxEquipmentData equippedEgg;
    // Derived for gameplay from equipped IDs and round stock. Empty entries keep board positions.
    [NonSerialized] public List<InventoryItem> boardMaterialSlots = new List<InventoryItem>();
    [NonSerialized] public List<BoardPinData> boardPinSlots = new List<BoardPinData>();
    public List<string> equipmentUnlockKeys;
    public List<InventoryItem> materialItems = new List<InventoryItem>();
    public List<SkillItemSnapshot> skillItems = new List<SkillItemSnapshot>();
    public List<SkillNodeData> unlockedSkillNodes = new List<SkillNodeData>();
    public List<PotionEquipmentSnapshot> potionSlots = new List<PotionEquipmentSnapshot>();

    public UpgradeToGeneralData Copy()
    {
        var copy = new UpgradeToGeneralData {
            legacySkillsOnly = legacySkillsOnly,
            equipment = equipment?.Copy(),
            equippedEgg = equippedEgg,
            boardMaterialSlots = boardMaterialSlots?.ConvertAll(item =>
                item != null ? new InventoryItem(item.itemData, item.amount) : null) ?? new List<InventoryItem>(),
            boardPinSlots = boardPinSlots != null ? new List<BoardPinData>(boardPinSlots) : new List<BoardPinData>(),
            equipmentUnlockKeys = equipmentUnlockKeys != null ? new List<string>(equipmentUnlockKeys) : null,
            materialItems = GameSessionItemCopy.Copy(materialItems),
            unlockedSkillNodes = unlockedSkillNodes != null ? new List<SkillNodeData>(unlockedSkillNodes) : new List<SkillNodeData>(),
            potionSlots = potionSlots != null ? potionSlots.ConvertAll(value => value?.Copy()) : new List<PotionEquipmentSnapshot>()
        };
        if (skillItems != null)
        {
            foreach (SkillItemSnapshot item in skillItems)
            {
                if (item != null && item.itemData != null)
                    copy.skillItems.Add(item.Copy());
            }
        }

        return copy;
    }
}

/// <summary>아이템 씬 → 배틀 씬. 한 번의 클릭/드래그로 선택된 아이템 묶음입니다.</summary>
[Serializable]
public sealed class GameItemToBattleData
{
    public List<InventoryItem> items = new List<InventoryItem>();

    public GameItemToBattleData Copy()
        => new GameItemToBattleData { items = GameSessionItemCopy.Copy(items) };
}

internal static class GameSessionItemCopy
{
    // 수량 객체는 복제하고 ItemData 에셋만 공유합니다. 풀에 반환된 머지 오브젝트를 참조하지 않습니다.
    public static List<InventoryItem> Copy(IEnumerable<InventoryItem> items)
    {
        var result = new List<InventoryItem>();
        if (items != null)
        {
            foreach (InventoryItem item in items)
            {
                if (item != null && item.itemData != null && item.amount > 0)
                    result.Add(new InventoryItem(item.itemData, item.amount));
            }
        }

        return result;
    }
}
