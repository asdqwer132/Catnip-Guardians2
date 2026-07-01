using UnityEngine;

public class BagTooltipTriggerGroup : MonoBehaviour
{
    public EquipmentBagManager bagSelectManager;
    public ItemTooltipTrigger[] triggers;

    public void Show()
    {
        triggers[bagSelectManager.currentBagIndex].ShowTooltip();
    }

}
