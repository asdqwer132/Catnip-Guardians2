using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Item Series", menuName = "Game/Buff/Target/Item Series")]
public class ItemSeriesTargetResolver : BuffTargetResolver
{
    public ItemSeries targetSeries = ItemSeries.None;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (targetSeries == ItemSeries.None)
            return;

        results.Add(BuffTargetHandle.GetItemSeries(targetSeries));
    }
}
