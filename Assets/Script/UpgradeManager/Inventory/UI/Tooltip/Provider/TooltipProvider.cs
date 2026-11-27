using UnityEngine;

public abstract class TooltipProvider : MonoBehaviour, ITooltipContentProvider
{
    public virtual bool HideOnPointerExit => false;
    [Header("Anchor")]
    [SerializeField] protected RectTransform anchorRect;
    [Tooltip("Offset from the anchor in the tooltip parent's local UI units.")]
    [SerializeField] private Vector2 tooltipOffset;

    public Vector2 TooltipOffset => tooltipOffset;

    protected virtual void Awake()
    {
        CacheAnchor();
    }

    public abstract bool TryGetTooltipData(out TooltipData data);

    public virtual RectTransform GetTooltipAnchor()
    {
        CacheAnchor();
        return anchorRect;
    }

    protected void CacheAnchor()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    protected void AppendSectionGap(System.Text.StringBuilder sb)
    {
        if (sb != null && sb.Length > 0)
            sb.AppendLine();
    }

    protected string SafeString(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value;
    }
}
