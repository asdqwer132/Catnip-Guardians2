using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Option")]
    public bool isPointer = true;

    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;

    [Header("Provider")]
    [SerializeField] private MonoBehaviour providerComponent;

    private ITooltipContentProvider provider;

    private void Awake()
    {
        CacheProvider();
    }

    private void OnValidate()
    {
        if (providerComponent != null && providerComponent is not ITooltipContentProvider)
        {
            Debug.LogWarning($"{providerComponent.name} does not implement ITooltipContentProvider.", providerComponent);
            providerComponent = null;
        }
    }

    public void Init(ItemTooltipUI tooltipTrigger)
    {
        tooltipUI = tooltipTrigger;
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(isPointer)
            ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointer)
            HideTooltip();
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

    private void HideTooltip()
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