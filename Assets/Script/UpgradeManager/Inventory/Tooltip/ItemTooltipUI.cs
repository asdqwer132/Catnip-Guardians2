using UnityEngine;

public class ItemTooltipUI : BasicTooltipUI
{
    public ClassificationModule classification = new ClassificationModule();
    protected override ClassificationModule Classification => classification;
    public ItemInformationModule itemInformation = new ItemInformationModule();
    protected override ItemInformationModule ItemInformation => itemInformation;

    [SerializeField] private GameObject costPanel;
    [SerializeField] private CurrencyUIGroup costGroup;

    protected override void ApplyData(TooltipData data)
    {
        base.ApplyData(data);
        var costs = (data as CostTooltipData)?.costs;
        costGroup?.SetRequiredCosts(costs);
        if (costPanel != null) costPanel.SetActive(costs != null && costs.Length > 0);
    }
}
