using UnityEngine;

[System.Serializable]
public class TooltipData
{
    public Sprite icon;
    public string title;
    public string subTitle;
    public string amountText;

    [TextArea]
    public string description;
}