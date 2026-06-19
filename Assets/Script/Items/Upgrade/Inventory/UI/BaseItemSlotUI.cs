using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseItemSlotUI : MonoBehaviour
{
    [Header("Base Item UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public GameObject amountImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI gradeText;
    public GameObject locked;
    public bool IsLocked = false;

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
            nameText.text = itemData.GetDataName();
        if (amountImage != null)
            amountImage.SetActive(true);

        if (amountText != null)
            amountText.text = "" + amount;

        if (gradeText != null)
            gradeText.text = itemData.grade.ToString();
        if (locked != null) locked.SetActive(false);

        IsLocked = false;
    }
    public virtual void LockSlot()
    {
        ClearSlot();

        IsLocked = true;
        if (locked != null) locked.SetActive(true);
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

        if (amountImage != null)
            amountImage.SetActive(false);
        if (amountText != null)
            amountText.text = "";

        if (gradeText != null)
            gradeText.text = "";
        if (locked != null) locked.SetActive(false);
        IsLocked = false;
    }

    public InventoryItem GetCurrentItem() { return currentItem; }
}