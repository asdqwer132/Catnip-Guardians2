using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Option")]
    public bool isPointer = true;
    public bool autoDisable = true;
    public bool startShow = false;
    [Tooltip("Select a slot Toggle to pin its item information. Pointer hover is ignored in this mode.")]
    public bool showOnSelection;
    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;

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

    public void Init(ItemTooltipUI tooltipTrigger)
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
        if (isPointer && !showOnSelection)
            ShowTooltip();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (isPointer && !showOnSelection)
            HideTooltip();
    }

    public void OnSlotSelectionChanged(bool selected)
    {
        if (!showOnSelection || tooltipUI == null) return;
        CacheProvider();
        if (provider == null) return;
        if (selected) tooltipUI.Pin(provider);
        else ReleaseSelection(true);
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
