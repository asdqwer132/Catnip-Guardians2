using UnityEngine;

public sealed class SkillMapTooltipUI : BasicTooltipUI
{
    public SkillMapInformationModule skillMapInformation = new SkillMapInformationModule();
    protected override SkillMapInformationModule SkillMapInformation => skillMapInformation;
}
