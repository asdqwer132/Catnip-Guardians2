using UnityEngine;
using UnityEngine.UI;

public sealed class MergeSelectedItemIcon : MonoBehaviour
{
    public Image icon;

    public void SetItem(ItemData item)
    {
        if (icon == null) return;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = icon.sprite != null;
    }
}
