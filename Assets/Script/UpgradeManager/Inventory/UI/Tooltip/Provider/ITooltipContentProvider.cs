using UnityEngine;

public interface ITooltipContentProvider
{
    bool TryGetTooltipData(out TooltipData data);
    RectTransform GetTooltipAnchor();
}