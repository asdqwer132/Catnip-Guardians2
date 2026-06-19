using UnityEngine;

public class EquipmentBagManager : RefreshListener
{
    public static EquipmentBagManager instance;

    [Header("Bags")]
    public EquipmentBag[] bags;

    [Header("Bag Panels")]
    public GameObject[] bagPanels;
    public GameObject[] toggles;

    [Header("Default")]
    public int currentBagIndex = 0;

    public EquipmentBag CurrentBag { get; private set; }

    private void Awake()
    {
        instance = this;
    }
    public EquipmentBag GetBagData(string bagId)
    {
        if (string.IsNullOrEmpty(bagId))
            return null;

        if (bags == null)
            return null;

        for (int i = 0; i < bags.Length; i++)
        {
            if (bags[i] == null)
                continue;

            BagData bagData = bags[i].bagData;

            if (bagData == null)
                continue;

            if (bagData.dataId == bagId)
                return bags[i];
        }

        return null;
    }
    public void Init()
    {
        for (int i = 0; i < bags.Length; i++)
        {
            if (bags[i] != null)
                bags[i].Init();
        }
        RefreshUI();

        SelectBag(currentBagIndex);
    }

    public void SelectBag(int index)
    {
        if (index < 0 || index >= bags.Length)
        {
            //Debug.LogWarning("잘못된 가방 번호: " + index);
            return;
        }

        if (bags[index] == null)
        {
            //Debug.LogWarning("가방이 비어있습니다: " + index);
            return;
        }

        currentBagIndex = index;
        CurrentBag = bags[index];

        RefreshPanels();

        //Debug.Log("현재 선택된 가방: " + CurrentBag.bagName);
    }

    private void RefreshPanels()
    {
        for (int i = 0; i < bagPanels.Length; i++)
        {
            if (bagPanels[i] == null)
                continue;

            bagPanels[i].SetActive(i == currentBagIndex);
        }
    }

    public bool EquipItemToCurrentBag(InventoryItem item)
    {
        if (CurrentBag == null)
        {
            //Debug.LogWarning("선택된 가방이 없습니다.");
            return false;
        }

        if (item == null || item.itemData == null)
        {
            //Debug.LogWarning("장착할 아이템이 없습니다.");
            return false;
        }

        if (InventoryManager.instance == null)
        {
            //Debug.LogWarning("InventoryManager가 없습니다.");
            return false;
        }

        if (!InventoryManager.instance.HasItem(item.itemData, 1))
        {
            //Debug.LogWarning("인벤토리에 해당 아이템이 없습니다.");
            return false;
        }

        bool equipResult = CurrentBag.EquipItem(item);

        if (!equipResult)
        {
            // Debug.LogWarning("장착 실패");
            return false;
        }

        bool removeResult = InventoryManager.instance.RemoveItem(item.itemData, 1);

        if (!removeResult)
        {
            //Debug.LogWarning("인벤토리 제거 실패");

            CurrentBag.UnequipItem(GetLastEquippedSlotIndex(item.itemData));
            CurrentBag.RefreshUI();

            return false;
        }

        CurrentBag.RefreshUI();

        return true;
    }

    public void UnequipItemFromCurrentBag(int slotIndex)
    {
        if (CurrentBag == null)
        {
            //Debug.LogWarning("선택된 가방이 없습니다.");
            return;
        }

        if (InventoryManager.instance == null)
        {
            // Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        InventoryItem item = CurrentBag.GetItem(slotIndex);

        if (item == null || item.itemData == null)
        {
            //Debug.LogWarning("해제할 아이템이 없습니다.");
            return;
        }

        InventoryManager.instance.AddItem(item.itemData, 1);

        CurrentBag.UnequipItem(slotIndex);
        CurrentBag.RefreshUI();

        //Debug.Log("장착 해제 완료: " + item.itemData.itemName);
    }

    public void ClearCurrentBagSlots()
    {
        if (CurrentBag == null)
        {
            //Debug.LogWarning("선택된 가방이 없습니다.");
            return;
        }

        if (InventoryManager.instance == null)
        {
            // Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        for (int i = 0; i < CurrentBag.equippedItems.Count; i++)
        {
            InventoryItem item = CurrentBag.equippedItems[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            InventoryManager.instance.AddItem(item.itemData, item.amount);
        }

        CurrentBag.ClearAllSlots();
        CurrentBag.RefreshUI();

        //Debug.Log(CurrentBag.bagData.bagName + "의 모든 아이템을 해제했습니다.");
    }

    public void ClearBagSlots(int bagIndex)
    {
        if (bagIndex < 0 || bagIndex >= bags.Length)
        {
            //Debug.LogWarning("잘못된 가방 번호: " + bagIndex);
            return;
        }

        EquipmentBag bag = bags[bagIndex];

        if (bag == null)
        {
            // Debug.LogWarning("가방이 비어있습니다: " + bagIndex);
            return;
        }

        if (InventoryManager.instance == null)
        {
            //Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        for (int i = 0; i < bag.equippedItems.Count; i++)
        {
            InventoryItem item = bag.equippedItems[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            InventoryManager.instance.AddItem(item.itemData, item.amount);
        }

        bag.ClearAllSlots();
        bag.RefreshUI();

        //Debug.Log(bag.bagData.bagName + "의 모든 아이템을 해제했습니다.");
    }

    private int GetLastEquippedSlotIndex(ItemData itemData)
    {
        if (CurrentBag == null)
            return -1;

        for (int i = CurrentBag.equippedItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = CurrentBag.equippedItems[i];

            if (item != null && item.itemData == itemData)
                return i;
        }

        return -1;
    }

    protected override void Refresh(RefreshType refreshType)
    {
        //Debug.Log("방송들어옴");
        RefreshUI();
    }
    private void RefreshUI()
    {

        for (int i = 0; i < bags.Length; i++)
        {
            toggles[i].gameObject.SetActive(UnlockCheckUtility.CanUse(bags[i].bagData));
        }
    }
}