using UnityEngine;
using System; 
[Serializable]
public class TooltipData
{
    public Sprite icon;
    public string title;
    public string subTitle;
    public string amountText;

    [TextArea]
    public string description;
}
[Serializable]
public class BagTooltipData : TooltipData
{
    public string weight;
    public string slots;
}