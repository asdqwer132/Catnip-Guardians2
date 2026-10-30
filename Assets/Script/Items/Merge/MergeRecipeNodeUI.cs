using UnityEngine;
using UnityEngine.UI;

public sealed class MergeRecipeNodeUI : MonoBehaviour
{
    public ItemData item;
    public Image icon;

    public void RefreshIcon()
    {
        if (icon == null) return;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = icon.sprite != null;
    }
}
