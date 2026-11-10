using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OwnerBuffTargetResolver", menuName = "Items/Effects/Direct Effects/Buff/BuffSetting/Targets/Owner")]
public class OwnerBuffTargetResolver : BuffTargetResolver
{
    public enum OwnerTargetMode { SourceItem = 0, SourceItemSeries = 2, SourceItemAndSeries = 4, All = 6 }
    public OwnerTargetMode targetMode;
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || results == null) return;
        var item = context.sourceItemData;
        if (targetMode == OwnerTargetMode.All) { results.Add(BuffTargetHandle.AllItems()); return; }
        if (item == null) return;
        if (targetMode == OwnerTargetMode.SourceItem || targetMode == OwnerTargetMode.SourceItemAndSeries)
            results.Add(BuffTargetHandle.Item(item));
        if ((targetMode == OwnerTargetMode.SourceItemSeries || targetMode == OwnerTargetMode.SourceItemAndSeries) && item.series != ItemSeries.None)
            results.Add(BuffTargetHandle.GetItemSeries(item.series));
    }
}
