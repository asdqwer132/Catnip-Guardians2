using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SubPlantSlotButton : MonoBehaviour
{
    public int slotIndex;

    public TextMeshProUGUI slotText;
    public TextMeshProUGUI stateText;
    public Image selectedImage;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(Select);
        Refresh();
    }

    void Select()
    {
        SubPlantPlantingUI.instance.SelectSlot(slotIndex);
    }

    public void Refresh()
    {
        if (slotText != null)
            slotText.text = slotIndex + " pos";

        SubPlantFieldSlot slot = SubPlantFieldManager.instance.GetSlot(slotIndex);

        if (slot != null && slot.isOccupied)
        {
            stateText.text = slot.plantedItem.itemName;
        }
        else
        {
            stateText.text = " null";
        }
    }
}