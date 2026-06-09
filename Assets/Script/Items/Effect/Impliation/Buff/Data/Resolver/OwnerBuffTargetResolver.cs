using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OwnerBuffTargetResolver", menuName = "Game/Buff/Buff Target/Owner")]
public class OwnerBuffTargetResolver : BuffTargetResolver
{
    public enum OwnerTargetMode
    {
        SourceItem,
        SourceBag,
        SourceItemSeries,
        SourceItemAndBag,
        SourceItemAndSeries,
        SourceBagAndSeries,
        All
    }

    [Header("Owner Target")]
    public OwnerTargetMode targetMode = OwnerTargetMode.SourceItem;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || results == null)
            return;

        ItemData sourceItem = context.sourceItemData;
        EquipmentBag sourceBag = context.sourceBag;

        switch (targetMode)
        {
            case OwnerTargetMode.SourceItem:
                AddItem(sourceItem, results);
                break;

            case OwnerTargetMode.SourceBag:
                AddBag(sourceBag, results);
                break;

            case OwnerTargetMode.SourceItemSeries:
                AddItemSeries(sourceItem, results);
                break;

            case OwnerTargetMode.SourceItemAndBag:
                AddItem(sourceItem, results);
                AddBag(sourceBag, results);
                break;

            case OwnerTargetMode.SourceItemAndSeries:
                AddItem(sourceItem, results);
                AddItemSeries(sourceItem, results);
                break;

            case OwnerTargetMode.SourceBagAndSeries:
                AddBag(sourceBag, results);
                AddItemSeries(sourceItem, results);
                break;

            case OwnerTargetMode.All:
                AddItem(sourceItem, results);
                AddBag(sourceBag, results);
                AddItemSeries(sourceItem, results);
                break;
        }
    }

    private void AddItem(ItemData itemData, List<BuffTargetHandle> results)
    {
        if (itemData == null)
            return;

        BuffTargetHandle handle = BuffTargetHandle.Item(itemData);

        if (handle != null)
            results.Add(handle);
    }

    private void AddBag(EquipmentBag bag, List<BuffTargetHandle> results)
    {
        if (bag == null)
            return;

        BuffTargetHandle handle = BuffTargetHandle.Bag(bag);

        if (handle != null)
            results.Add(handle);
    }

    private void AddItemSeries(ItemData itemData, List<BuffTargetHandle> results)
    {
        if (itemData == null)
            return;

        if (itemData.series == ItemSeries.None)
            return;

        BuffTargetHandle handle = BuffTargetHandle.GetItemSeries(itemData.series);

        if (handle != null)
            results.Add(handle);
    }
}