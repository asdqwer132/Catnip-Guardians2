using System;
using System.Collections.Generic;

[Serializable]
public sealed class MergeBoxLevelSnapshot
{
    public ItemSeries series;
    public int level = 1;
    public static bool IsBoxSeries(ItemSeries value) => value != ItemSeries.None && value != ItemSeries.Potions && Enum.IsDefined(typeof(ItemSeries), value);
    public MergeBoxLevelSnapshot Copy() => new MergeBoxLevelSnapshot { series = series, level = Math.Max(1, level) };
}

/// <summary>스킬트리에서 등록한 실행 아이템과 선택적인 대상 가방 ID입니다. 가방 내용은 저장하지 않습니다.</summary>
[Serializable]
public sealed class SkillItemSnapshot
{
    public ItemData itemData;
    public string bagId;

    public SkillItemSnapshot() { }

    public SkillItemSnapshot(RegisteredBuffSkillItem item)
    {
        if (item == null)
            return;

        itemData = item.itemData;
        bagId = item.bagId;
    }

    public SkillItemSnapshot Copy()
    {
        return new SkillItemSnapshot { itemData = itemData, bagId = bagId };
    }
}

/// <summary>업그레이드 씬 → 제너럴 씬. 아이템 에셋·수량과 시리즈별 상자 레벨을 보관합니다.</summary>
[Serializable]
public sealed class UpgradeToGeneralData
{
    public List<InventoryItem> materialItems = new List<InventoryItem>();
    public List<SkillItemSnapshot> skillItems = new List<SkillItemSnapshot>();
    public List<MergeBoxLevelSnapshot> boxLevels = new List<MergeBoxLevelSnapshot>();

    public UpgradeToGeneralData Copy()
    {
        var copy = new UpgradeToGeneralData { materialItems = GameSessionItemCopy.Copy(materialItems) };
        if (boxLevels != null) foreach (var entry in boxLevels)
            if (entry != null && MergeBoxLevelSnapshot.IsBoxSeries(entry.series)) copy.boxLevels.Add(entry.Copy());
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
