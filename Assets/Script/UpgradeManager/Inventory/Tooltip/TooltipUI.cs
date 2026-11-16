using UnityEngine;

[AddComponentMenu("UI/Tooltip UI")]
public partial class TooltipUI : DataBindingUI
{
    public PanelVisibilityModule panelVisibility = new PanelVisibilityModule();
    protected virtual PanelVisibilityModule PanelVisibility => panelVisibility;
}
