using UnityEngine;

public sealed class CodexDetailsUI : DataViewUI
{
    public BasicInformationModule basicInformation = new BasicInformationModule();
    protected override BasicInformationModule BasicInformation => basicInformation;
    public ClassificationModule classification = new ClassificationModule();
    protected override ClassificationModule Classification => classification;
    public ItemInformationModule itemInformation = new ItemInformationModule();
    protected override ItemInformationModule ItemInformation => itemInformation;
    public CodexInformationModule codexInformation = new CodexInformationModule();
    protected override CodexInformationModule CodexInformation => codexInformation;
    public PanelVisibilityModule panelVisibility = new PanelVisibilityModule();
    protected override PanelVisibilityModule PanelVisibility => panelVisibility;
}
