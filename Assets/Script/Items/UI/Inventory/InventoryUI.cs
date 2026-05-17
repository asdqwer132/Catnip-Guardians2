using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot")]
    public Transform slotParent;
    public GameObject slotPrefab;

    void Start()
    {
        if (InventoryManager.instance != null)
        {
            //Debug.Log("InventoryUI 이벤트 연결됨");
            InventoryManager.instance.onInventoryChanged += RefreshUI;
        }
        else
        {
            Debug.LogWarning("Start에서도 InventoryManager.instance가 null입니다.");
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {

        if (InventoryManager.instance == null)
            return;

        if (slotParent == null || slotPrefab == null)
            return;

        ClearSlots();

        foreach (InventoryItem item in InventoryManager.instance.items)
        {
            if (item == null || item.itemData == null)
                continue;

            GameObject slotObj = Instantiate(slotPrefab, slotParent);

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSlot(item);
            }
            else
            {
                Debug.LogWarning(gameObject.name + " 슬롯 프리팹에 BaseItemSlotUI가 없습니다.");
            }
        }
    }

    void ClearSlots()
    {
        if (slotParent == null)
            return;

        for (int i = slotParent.childCount - 1; i >= 0; i--)
        {
            Destroy(slotParent.GetChild(i).gameObject);
        }
    }
}