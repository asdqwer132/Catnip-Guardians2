using UnityEngine;
using System;
using System.Collections.Generic;
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
public class SkillMapTooltipData : TooltipData
{
    public string totalNodesCount;
    public string completedNodesCount;
}
[Serializable]
public class BagTooltipData : TooltipData
{
    public string weight;
    public string slots;
}
[Serializable]
public class CostTooltipData : TooltipData
{
    public Cost[] costs;
}