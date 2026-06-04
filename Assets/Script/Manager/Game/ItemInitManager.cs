using UnityEngine;

public class ItemInitManager : MonoBehaviour
{
    public InventoryItem[] items;

    public void ApplyDefaultInventoryItems()
    {
        if (items == null)
            return;

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            InventoryItem item = items[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            InventoryManager.instance.AddItem(item.itemData, item.amount);
        }
    }
}