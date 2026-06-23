using System;
using UnityEngine;

[Serializable]
public class BuffTargetHandle
{
    public BuffTargetKind kind;

    [Header("Item Target")]
    public ItemData itemData;
    public EquipmentBag bag;
    public ItemSeries itemSeries = ItemSeries.None;

    [Header("Object Target")]
    public UnityEngine.Object targetObject;
    public string targetGroup;

    [NonSerialized] private IBuffTarget cachedTarget;

    public static BuffTargetHandle Item(ItemData itemData)
    {
        return new BuffTargetHandle
        {
            kind = BuffTargetKind.Item,
            itemData = itemData
        };
    }

    public static BuffTargetHandle Bag(EquipmentBag bag)
    {
        return new BuffTargetHandle
        {
            kind = BuffTargetKind.Bag,
            bag = bag
        };
    }

    public static BuffTargetHandle GetItemSeries(ItemSeries itemSeries)
    {
        return new BuffTargetHandle
        {
            kind = BuffTargetKind.ItemSeries,
            itemSeries = itemSeries
        };
    }

    public static BuffTargetHandle AllItems()
    {
        return new BuffTargetHandle
        {
            kind = BuffTargetKind.AllItems
        };
    }

    public static BuffTargetHandle Target(IBuffTarget target)
    {
        if (target == null)
            return null;

        UnityEngine.Object targetObject = target.BuffTargetObject;

        if (targetObject == null)
            return null;

        return new BuffTargetHandle
        {
            kind = BuffTargetKind.Target,
            targetObject = targetObject,
            targetGroup = NormalizeGroup(target.BuffTargetGroup),
            cachedTarget = target
        };
    }

    public static BuffTargetHandle Group(string targetGroup)
    {
        string normalizedGroup = NormalizeGroup(targetGroup);

        if (string.IsNullOrEmpty(normalizedGroup))
            return null;

        return new BuffTargetHandle
        {
            kind = BuffTargetKind.Group,
            targetGroup = normalizedGroup
        };
    }

    public bool Matches(BuffQueryContext query)
    {
        if (query == null)
            return false;

        if (query.buffTarget != null)
            return MatchesTarget(query.buffTarget);

        return MatchesItem(query.itemData, query.bag);
    }

    public bool MatchesItem(ItemData targetItemData, EquipmentBag targetBag)
    {
        if (kind == BuffTargetKind.AllItems)
            return true;

        if (kind == BuffTargetKind.Bag)
            return bag != null && bag == targetBag;

        if (targetItemData == null)
            return false;

        if (kind == BuffTargetKind.Item)
            return itemData != null && itemData == targetItemData;

        if (kind == BuffTargetKind.ItemSeries)
            return itemSeries != ItemSeries.None && targetItemData.series == itemSeries;

        return false;
    }

    public bool MatchesTarget(IBuffTarget target)
    {
        if (target == null)
            return false;

        if (kind == BuffTargetKind.Group)
            return MatchesGroup(target.BuffTargetGroup);

        if (kind == BuffTargetKind.Target)
            return targetObject != null && target.BuffTargetObject == targetObject;

        return false;
    }

    private bool MatchesGroup(string targetObjectGroup)
    {
        string buffGroup = NormalizeGroup(targetGroup);
        string objectGroup = NormalizeGroup(targetObjectGroup);

        if (string.IsNullOrEmpty(buffGroup))
            return false;

        if (string.IsNullOrEmpty(objectGroup))
            return false;

        if (objectGroup == buffGroup)
            return true;

        return objectGroup.StartsWith(buffGroup + "/", StringComparison.Ordinal);
    }

    public bool SameTarget(BuffTargetHandle other)
    {
        if (other == null)
            return false;

        if (kind != other.kind)
            return false;

        return itemData == other.itemData
            && bag == other.bag
            && itemSeries == other.itemSeries
            && targetObject == other.targetObject
            && NormalizeGroup(targetGroup) == NormalizeGroup(other.targetGroup);
    }

    public IBuffTarget GetCachedTarget()
    {
        if (cachedTarget != null)
            return cachedTarget;

        if (targetObject == null)
            return null;

        cachedTarget = targetObject as IBuffTarget;

        if (cachedTarget != null)
            return cachedTarget;

        Component component = targetObject as Component;

        if (component != null)
            cachedTarget = component.GetComponent<IBuffTarget>();

        return cachedTarget;
    }

    public string GetDebugName()
    {
        if (kind == BuffTargetKind.Item)
            return itemData != null ? itemData.GetDataName() : "Null Item";

        if (kind == BuffTargetKind.Bag)
            return bag != null ? bag.name : "Null Bag";

        if (kind == BuffTargetKind.ItemSeries)
            return itemSeries.ToString();

        if (kind == BuffTargetKind.Target)
        {
            IBuffTarget target = GetCachedTarget();

            if (target != null)
                return target.BuffTargetDebugName;

            return targetObject != null ? targetObject.name : "Null Target";
        }

        if (kind == BuffTargetKind.Group)
            return string.IsNullOrEmpty(targetGroup) ? "Null Group" : targetGroup;

        return kind.ToString();
    }

    private static string NormalizeGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return string.Empty;

        group = group.Trim();
        group = group.Replace("\\", "/");

        while (group.Contains("//"))
            group = group.Replace("//", "/");

        group = group.Trim('/');

        return group;
    }
}