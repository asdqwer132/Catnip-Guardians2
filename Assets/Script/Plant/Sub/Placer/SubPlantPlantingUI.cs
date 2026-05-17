using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SubPlantPlantingUI : MonoBehaviour
{
    public static SubPlantPlantingUI instance;

    [Header("선택 상태")]
    public int selectedSlotIndex = -1;
    public ItemData selectedItem;

    [Header("선택 UI")]
    public TextMeshProUGUI selectedSlotText;
    public Image selectedItemIcon;
    public TextMeshProUGUI selectedItemNameText;

    void Awake()
    {
        instance = this;
    }

    public void SelectSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;

        if (selectedSlotText != null)
            selectedSlotText.text = "pos: " + slotIndex;
    }

    public void SelectItem(ItemData itemData)
    {
        selectedItem = itemData;

        if (selectedItemIcon != null)
        {
            selectedItemIcon.enabled = itemData != null;
            selectedItemIcon.sprite = itemData.icon;
        }

        if (selectedItemNameText != null)
        {
            selectedItemNameText.text = itemData != null ? itemData.itemName : "n select";
        }
    }

    public void PlantButton()
    {
        if (selectedSlotIndex < 0)
        {
            Debug.Log("심을 위치를 선택하세요.");
            return;
        }

        if (selectedItem == null)
        {
            Debug.Log("심을 보조식물을 선택하세요.");
            return;
        }

        bool success = SubPlantFieldManager.instance.Plant(selectedSlotIndex, selectedItem);

        if (success)
        {
            Debug.Log("심기 성공");

            selectedItem = null;
            RefreshSelectedItemUI();
        }
    }

    void RefreshSelectedItemUI()
    {
        if (selectedItemIcon != null)
            selectedItemIcon.enabled = false;

        if (selectedItemNameText != null)
            selectedItemNameText.text = "n select";
    }
}