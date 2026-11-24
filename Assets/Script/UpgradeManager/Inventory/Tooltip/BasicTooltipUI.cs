using UnityEngine;

public class BasicTooltipUI : TooltipUI
{
    public BasicInformationModule basicInformation = new BasicInformationModule();
    protected override BasicInformationModule BasicInformation => basicInformation;
}
