using UnityEngine;

public interface ITooltipContentProvider
{
    bool TryGetTooltipData(out TooltipData data);
    RectTransform GetTooltipAnchor();
    Vector2 TooltipOffset { get; }
}
