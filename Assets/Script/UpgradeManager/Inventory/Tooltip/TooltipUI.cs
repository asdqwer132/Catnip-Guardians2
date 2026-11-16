using UnityEngine;

[AddComponentMenu("UI/Tooltip UI")]
public partial class TooltipUI : DataViewUI
{
    public PanelVisibilityModule panelVisibility = new PanelVisibilityModule();
    protected override PanelVisibilityModule PanelVisibility => panelVisibility;
}
