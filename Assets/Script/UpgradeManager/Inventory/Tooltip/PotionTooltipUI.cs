using UnityEngine;

public sealed class PotionTooltipUI : TooltipUI
{
    public BasicInformationModule basicInformation = new BasicInformationModule();
    protected override BasicInformationModule BasicInformation => basicInformation;
    public ClassificationModule classification = new ClassificationModule();
    protected override ClassificationModule Classification => classification;
    public ItemInformationModule itemInformation = new ItemInformationModule();
    protected override ItemInformationModule ItemInformation => itemInformation;
}
