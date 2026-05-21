using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagPreviewSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image bagIcon;
    public GameObject selectedFrame;

    public void SetUI(BagItemUseManager manager, bool isSelected)
    {
        if (manager == null || manager.bag == null)
        {
            Clear();
            return;
        }

        if (bagIcon != null)
        {
            if (manager.bag.bagData != null && manager.bag.bagData.icon != null)
            {
                bagIcon.enabled = true;
                bagIcon.sprite = manager.bag.bagData.icon;
            }
            else
            {
                bagIcon.enabled = false;
                bagIcon.sprite = null;
            }
        }

        if (selectedFrame != null)
        {
            selectedFrame.SetActive(isSelected);
        }
    }

    public void Clear()
    {
        if (bagIcon != null)
        {
            bagIcon.enabled = false;
            bagIcon.sprite = null;
        }

        if (selectedFrame != null)
            selectedFrame.SetActive(false);

        gameObject.SetActive(false);
    }
}