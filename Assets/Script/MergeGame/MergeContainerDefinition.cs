using System;
using UnityEngine;

public enum MergeMaterialSupply { ConsumeStock, RepeatOwnedMaterial }
public enum MergeSelectionDelivery { OnRelease, ConfirmSelection }
public enum MergeContainerUpgradeKind { ContainerSize, InjectionSpeed, InletCount, DragRadius, ClickCooldown, DragCooldown }

[Serializable]
public sealed class MergeContainerStats
{
    [Min(2f)] public float width = 6f;
    [Min(2f)] public float height = 8f;
    [Min(0.05f)] public float injectionInterval = 1.5f;
    [Range(1, 8)] public int inletCount = 1;
    [Min(0.1f)] public float dragRadius = 0.6f;
    [Min(0f)] public float clickCooldown = 0.3f;
    [Min(0f)] public float dragCooldown = 1f;
    [Range(8, 256)] public int maximumItems = 96;

    public MergeContainerStats Clone() => (MergeContainerStats)MemberwiseClone();
    public void Clamp()
    {
        width = EffectStatUtility.Safe(width, 2f, 20f, 6f);
        height = EffectStatUtility.Safe(height, 2f, 24f, 8f);
        injectionInterval = EffectStatUtility.Safe(injectionInterval, 0.05f, 60f, 1.5f);
        inletCount = Mathf.Clamp(inletCount, 1, 8);
        dragRadius = EffectStatUtility.Safe(dragRadius, 0.1f, 8f, 0.6f);
        clickCooldown = EffectStatUtility.Safe(clickCooldown, 0f, 30f, 0.3f);
        dragCooldown = EffectStatUtility.Safe(dragCooldown, 0f, 30f, 1f);
        maximumItems = Mathf.Clamp(maximumItems, 8, 256);
    }
}

[Serializable]
public sealed class MergeContainerItem
{
    public ItemData item;
    [Tooltip("같은 ItemData 두 개가 닿으면 이 아이템 하나로 합성합니다. 비우면 최종 단계입니다.")]
    public ItemData mergeResult;
    [Min(0.08f)] public float radius = 0.3f;
}

[CreateAssetMenu(menuName = "05.Merge Game/Container", fileName = "MergeContainer")]
public sealed class MergeContainerDefinition : ScriptableObject
{
    public string displayName;
    public ItemSeries series;
    public Color color = new Color(0.3f, 0.7f, 0.9f);
    public MergeContainerStats baseStats = new MergeContainerStats();
    [Tooltip("자동 투입 후보. 상자에서 얻는 재료만 명시적으로 연결합니다. 이름으로 추정하지 않습니다.")]
    public ItemData[] injectionMaterials;
    [Tooltip("합성 경로. 같은 아이템은 한 번만 등록하고 결과도 이 목록에 등록합니다.")]
    public MergeContainerItem[] items;
    public ItemThrowOptions throwOptions = new ItemThrowOptions { allowStationaryThrow = true };

    public MergeContainerItem Find(ItemData item)
    {
        if (item != null && items != null)
            foreach (var entry in items) if (entry != null && entry.item == item) return entry;
        return null;
    }
    public bool CanInject(ItemData item)
    {
        if (Find(item) == null || item.series != series || injectionMaterials == null) return false;
        return Array.IndexOf(injectionMaterials, item) >= 0;
    }
    public bool TryGetMergeResult(ItemData first, ItemData second, out ItemData result)
    {
        result = null;
        if (first == null || first != second) return false;
        var entry = Find(first);
        if (entry == null || entry.mergeResult == first || Find(entry.mergeResult) == null ||
            entry.mergeResult.series != series) return false;
        result = entry.mergeResult;
        return true;
    }
}
