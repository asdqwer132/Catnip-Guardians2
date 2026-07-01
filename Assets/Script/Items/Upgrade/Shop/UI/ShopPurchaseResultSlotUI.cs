using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPurchaseResultSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    public void SetSlot(ItemData itemData, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.icon : null;
            iconImage.enabled = itemData != null && itemData.icon != null;
        }

        if (nameText != null)
            nameText.text = itemData != null ? itemData.GetDataName() : "";

        if (countText != null)
        {
            countText.text = count > 0 ? "x" + count : "";
            countText.gameObject.SetActive(count > 0);
        }
    }
}
