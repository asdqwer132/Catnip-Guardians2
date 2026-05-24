using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagSlotCooldownUI : MonoBehaviour
{
    [Header("Slot UI")]
    public Image itemIcon;
    public Image cooldownFill;
    public TextMeshProUGUI cooldownText;
    public Image nextUseImage;

    public void SetItem(InventoryItem item)
    {
        if (itemIcon == null)
            return;

        if (item == null || item.itemData == null)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
            return;
        }

        itemIcon.enabled = true;

        itemIcon.sprite = item.itemData.icon;
    }

    public void SetCooldown(float ratio, float remain)
    {
        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = ratio;
        }

        if (cooldownText != null)
        {
            if (remain > 0f)
                cooldownText.text = remain.ToString("F1");
            else
                cooldownText.text = "";
        }
    }

    public void SetNextUse(bool isNextUse)
    {
        if (nextUseImage != null)
        {
            nextUseImage.gameObject.SetActive(isNextUse);
        }
    }

    public void Clear()
    {
        if (itemIcon != null)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
        }

        if (cooldownFill != null)
            cooldownFill.fillAmount = 0f;

        if (cooldownText != null)
            cooldownText.text = "";

        if (nextUseImage != null)
            nextUseImage.gameObject.SetActive(false);
    }
}