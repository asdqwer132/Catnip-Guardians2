using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentBagPreviewUI : MonoBehaviour
{
    [Header("Current Bag UI")]
    public Image currentBagIcon;

    public void RefreshInfo(BagItemUseManager manager)
    {
        if (manager == null || manager.bag == null || manager.bag.bagData == null)
        {
            Clear();
            return;
        }


        if (currentBagIcon == null)
            return;

        if (manager.bag.bagData.icon != null)
        {
            currentBagIcon.enabled = true;
            currentBagIcon.sprite = manager.bag.bagData.icon;
        }
        else
        {
            currentBagIcon.enabled = false;
            currentBagIcon.sprite = null;
        }
    }


    public void Clear()
    {

        if (currentBagIcon != null)
        {
            currentBagIcon.enabled = false;
            currentBagIcon.sprite = null;
        }
    }
}