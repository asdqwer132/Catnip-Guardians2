using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Option")]
    public bool isPointer = true;
    public bool autoDisable = true;
    public bool startShow = false;
    public bool hideOnPointerExit = true;
    [Tooltip("Click to pin the tooltip until another item is hovered or this slot is clicked again.")]
    public bool showOnSelection;
    [Header("Tooltip")]
    public TooltipUI tooltipUI;

    [Header("Provider")]
    [SerializeField] private TooltipProvider providerComponent;

    private ITooltipContentProvider provider;

    private void Awake()
    {
        CacheProvider();
    }
    private void Start()
    {
        if(startShow)
            ShowTooltip();
    }

    public void Init(TooltipUI tooltipTrigger)
    {
        tooltipUI = tooltipTrigger;
    }

    
    private void OnDisable()
    {
        // Inventory rows can disappear immediately after their last item is used.
        // The inventory panel releases a pinned snapshot when the panel closes.
        if (showOnSelection) return;
        if (autoDisable)
            HideTooltip();
    }

    private void OnDestroy() => ReleaseSelection();

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPointer) return;
        CacheProvider();
        if (provider != null) tooltipUI?.Show(provider, replacePin: true);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!isPointer) return;
        CacheProvider();
        if (hideOnPointerExit || (provider is TooltipProvider source && source.HideOnPointerExit))
            HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!showOnSelection || tooltipUI == null || eventData.button != PointerEventData.InputButton.Left) return;
        CacheProvider();
        if (provider == null) return;
        tooltipUI.TogglePin(provider);
    }

    // Recycling an inventory row releases ownership but keeps the captured info.
    public void ReleaseSelection(bool hide = false)
    {
        if (tooltipUI == null) return;
        CacheProvider();
        if (provider != null) tooltipUI.ReleasePin(provider, hide);
    }

    public void ShowTooltip()
    {
        if (tooltipUI == null)
            return;

        CacheProvider();

        if (provider == null)
            return;

        tooltipUI.Show(provider);
    }

    public void HideTooltip()
    {
        if (tooltipUI == null)
            return;

        CacheProvider();

        if (provider == null)
            return;

        tooltipUI.Hide(provider);
    }

    private void CacheProvider()
    {
        if (provider != null)
            return;

        if (providerComponent != null)
        {
            provider = providerComponent as ITooltipContentProvider;

            if (provider != null)
                return;
        }

        provider = GetComponent<ITooltipContentProvider>();
    }
}
