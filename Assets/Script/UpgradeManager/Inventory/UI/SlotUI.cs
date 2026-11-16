using UnityEngine;

[AddComponentMenu("UI/Slot UI")]
public class SlotUI : DataBindingUI
{
    public SlotInformationModule slotInformation = new SlotInformationModule();
    protected override SlotInformationModule SlotInformation => slotInformation;
}
