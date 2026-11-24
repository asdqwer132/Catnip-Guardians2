using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Option")]
    public bool isPointer = true;
    public bool autoDisable = true;
    public bool startShow = false;
    public bool hideOnPointerExit = true;
    [Tooltip("Pin while the slot toggle is on. Slots without a toggle pin and unpin on click.")]
    public bool showOnSelection;
    [Header("Tooltip")]
    public TooltipUI tooltipUI;

    [Header("Provider")]
    [SerializeField] private TooltipProvider providerComponent;

    private ITooltipContentProvider provider;
    private Toggle slotToggle;

    private void Awake()
    {
        slotToggle = GetComponent<Toggle>();
        CacheProvider();
    }

    private void OnEnable()
    {
        if (slotToggle != null) slotToggle.onValueChanged.AddListener(OnToggleChanged);
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
        if (slotToggle != null)
        {
            slotToggle.onValueChanged.RemoveListener(OnToggleChanged);
            ReleaseSelection(true);
        }
        if (autoDisable) HideTooltip();
    }

    private void OnToggleChanged(bool selected)
    {
        if (!showOnSelection || tooltipUI == null) return;
        if (!selected || !slotToggle.isOn)
        {
            ReleaseSelection(true);
            return;
        }

        CacheProvider();
        if (provider != null) tooltipUI.Pin(provider);
    }

    private void OnDestroy() => ReleaseSelection();

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPointer) return;
        CacheProvider();
        if (provider != null) tooltipUI?.Show(provider);
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
        if (slotToggle != null || !showOnSelection || tooltipUI == null || eventData.button != PointerEventData.InputButton.Left) return;
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
