using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseItemSlotUI : MonoBehaviour
{
    [Header("Base Item UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI gradeText;

    [Header("Referrence")]
    public InventoryItem currentItem;

    public virtual void SetSlot(InventoryItem item)
    {
        currentItem = item;

        if (item == null || item.itemData == null)
        {
            ClearSlot();
            return;
        }

        SetItemData(item.itemData, item.amount);
    }

    public virtual void SetItemData(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
        {
            ClearSlot();
            //Debug.Log("데이터 없음");
            return;
        }
        //Debug.Log(gameObject.name +  "데이터 있음" + itemData.itemName);

        if (icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = itemData.icon != null;
        }

        if (nameText != null)
            nameText.text = itemData.itemName;

        if (amountText != null)
            amountText.text = "x" + amount;

        if (gradeText != null)
            gradeText.text = itemData.grade.ToString();
    }

    public virtual void ClearSlot()
    {
        currentItem = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (nameText != null)
            nameText.text = "";

        if (amountText != null)
            amountText.text = "";

        if (gradeText != null)
            gradeText.text = "";
    }

    public InventoryItem GetCurrentItem() { return currentItem; }
}