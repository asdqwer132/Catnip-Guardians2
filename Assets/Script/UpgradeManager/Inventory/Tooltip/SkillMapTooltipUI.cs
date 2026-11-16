using UnityEngine;

public sealed class SkillMapTooltipUI : TooltipUI
{
    public BasicInformationModule basicInformation = new BasicInformationModule();
    protected override BasicInformationModule BasicInformation => basicInformation;
    public SkillMapInformationModule skillMapInformation = new SkillMapInformationModule();
    protected override SkillMapInformationModule SkillMapInformation => skillMapInformation;
}
