using UnityEngine;

public class ItemTooltipUI : BasicTooltipUI
{
    public ClassificationModule classification = new ClassificationModule();
    protected override ClassificationModule Classification => classification;
    public ItemInformationModule itemInformation = new ItemInformationModule();
    protected override ItemInformationModule ItemInformation => itemInformation;
}
