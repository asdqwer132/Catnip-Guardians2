using TMPro;
using UnityEngine;

public class BagTooltipUI : ItemTooltipUI
{
    public TextMeshProUGUI weight;
    public TextMeshProUGUI slots;
    protected override void ApplyData(TooltipData data)
    {
        base.ApplyData(data);

        if (data is not BagTooltipData bagData)
        {
            Debug.LogError(
                $"{nameof(BagTooltipUI)}에는 {nameof(BagTooltipData)}가 필요하지만 " +
                $"{data?.GetType().Name ?? "null"}이 전달되었습니다.",
                this
            );

            weight.text = string.Empty;
            slots.text = string.Empty;
            return;
        }

        weight.text = bagData.weight;
        slots.text = bagData.slots;
    }
}
