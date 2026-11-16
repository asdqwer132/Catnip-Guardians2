using UnityEngine;

public sealed class CodexTooltipUI : ItemTooltipUI
{
    public CodexInformationModule codexInformation = new CodexInformationModule();
    protected override CodexInformationModule CodexInformation => codexInformation;
}
