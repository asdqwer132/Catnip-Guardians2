using System;
using UnityEngine;

public class CostTooltipProvider : TooltipProvider
{
    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = new TooltipData();

        return true;
    }

}
