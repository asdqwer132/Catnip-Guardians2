using UnityEngine;

public sealed class SlotDataTooltipProvider : TooltipProvider
{
    [SerializeField] private DataBindingUI view;
    [SerializeField] private DefaultData data;

    public override bool TryGetTooltipData(out TooltipData result)
    {
        var source = view != null ? view.dataSource : data;
        if (source == null)
        {
            result = null;
            return false;
        }

        result = source is ItemBoxData box ? new CostTooltipData { costs = box.costs } :
            source is ItemData item ? ItemTooltipData.FromItem(item, 0) : new TooltipData();
        result.sourceData = source;
        result.icon = source.icon;
        result.dataName = source.name;
        result.amountText = null;
        return true;
    }
}
