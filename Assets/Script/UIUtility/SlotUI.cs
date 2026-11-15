using UnityEngine;

[AddComponentMenu("UI/Slot UI")]
public class SlotUI : DataViewUI
{
    public SlotInformationModule slotInformation = new SlotInformationModule();
    protected override SlotInformationModule SlotInformation => slotInformation;
}
