using UnityEngine;
using UnityEngine.UI;

// Owns selection and confirmation only. The manager owns equipped items and transfers stock.
public sealed class PotionEquipmentPanelUI : MonoBehaviour
{
    public PotionEquipmentManager manager;
    public PotionEquipmentInventoryUI inventoryView;
    public PotionEquipSlotUI[] equipmentSlots;
    public GameObject selectedDetails;
    public GameObject notSelectedDetails;
    public ItemDefinitionUI equippedPotion;
    public ItemDefinitionUI newPotion;
    public GameObject replacementArrow;
    public GameObject selectedConfirmArea;
    public GameObject notSelectedConfirmArea;
    public Button unequipButton;
    public Button replaceButton;
    public Button clearButton;
    private InventoryManager subscribedInventory;
    public int SelectedSlot { get; private set; } = -1;
    public ItemData PendingPotion { get; private set; }

    private void OnEnable()
    {
        if (manager != null) manager.Changed += OnEquipmentChanged;
        BindInventory();
        if (unequipButton != null) unequipButton.onClick.AddListener(UnequipSelected);
        if (replaceButton != null) replaceButton.onClick.AddListener(ConfirmReplace);
        if (clearButton != null) clearButton.onClick.AddListener(UnequipAll);
        RefreshUI();
    }

    private void Start()
    {
        BindInventory();
        if (inventoryView != null) inventoryView.Init();
        RefreshUI();
    }

    private void BindInventory()
    {
        var stock = manager != null && manager.inventory != null ? manager.inventory : InventoryManager.instance;
        if (subscribedInventory == stock) return;
        if (subscribedInventory != null) subscribedInventory.onInventoryChanged -= RefreshUI;
        subscribedInventory = stock;
        if (subscribedInventory != null) subscribedInventory.onInventoryChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (manager != null) manager.Changed -= OnEquipmentChanged;
        if (subscribedInventory != null) subscribedInventory.onInventoryChanged -= RefreshUI;
        subscribedInventory = null;
        if (unequipButton != null) unequipButton.onClick.RemoveListener(UnequipSelected);
        if (replaceButton != null) replaceButton.onClick.RemoveListener(ConfirmReplace);
        if (clearButton != null) clearButton.onClick.RemoveListener(UnequipAll);
        ClearSelection();
    }

    public void SelectSlot(int index)
    {
        if (manager == null || manager.IsLocked(index)) return;
        SelectedSlot = index;
        PendingPotion = null;
        if (inventoryView != null) inventoryView.ClearSelection();
        RefreshUI();
    }

    public void ClearSelection()
    {
        SelectedSlot = -1;
        PendingPotion = null;
        if (inventoryView != null) inventoryView.ClearSelection();
        RefreshUI();
    }

    public void SelectInventoryPotion(ItemData potion)
    {
        if (manager == null || manager.IsLocked(SelectedSlot) || !PotionEquipmentManager.IsPotion(potion)) return;
        if (manager.GetPotion(SelectedSlot) == null)
        {
            manager.TryEquip(SelectedSlot, potion);
            return;
        }
        PendingPotion = potion;
        RefreshUI();
    }

    public void ConfirmReplace()
    {
        if (manager != null && PendingPotion != null) manager.TryReplace(SelectedSlot, PendingPotion);
        RefreshUI();
    }

    public void UnequipSelected()
    {
        if (manager != null) manager.TryUnequip(SelectedSlot);
    }

    public void UnequipAll()
    {
        if (manager == null) return;
        for (int i = 0; i < manager.SlotCount; i++) manager.TryUnequip(i);
        ClearSelection();
    }

    private void OnEquipmentChanged()
    {
        PendingPotion = null;
        if (manager == null || manager.IsLocked(SelectedSlot)) SelectedSlot = -1;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (equipmentSlots != null)
            for (int i = 0; i < equipmentSlots.Length; i++)
                if (equipmentSlots[i] != null) equipmentSlots[i].Bind(this, i);

        bool selected = manager != null && !manager.IsLocked(SelectedSlot);
        var current = selected ? manager.GetPotion(SelectedSlot) : null;
        bool occupied = current != null;
        bool replacing = occupied && PendingPotion != null;
        if (equippedPotion != null)
        {
            equippedPotion.SetData(current);
            equippedPotion.gameObject.SetActive(occupied);
        }
        if (newPotion != null)
        {
            newPotion.SetData(replacing ? PendingPotion : null);
            newPotion.gameObject.SetActive(replacing);
        }
        if (replacementArrow != null) replacementArrow.SetActive(replacing);
        if (selectedDetails != null) selectedDetails.SetActive(occupied);
        if (notSelectedDetails != null) notSelectedDetails.SetActive(!occupied);
        if (selectedConfirmArea != null) selectedConfirmArea.SetActive(occupied);
        if (notSelectedConfirmArea != null) notSelectedConfirmArea.SetActive(!occupied);
        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(occupied);
            unequipButton.interactable = occupied;
        }
        if (replaceButton != null)
        {
            replaceButton.gameObject.SetActive(replacing);
            replaceButton.interactable = replacing && manager.GetAvailableCount(PendingPotion) > 0;
        }
        if (clearButton != null)
        {
            bool any = false;
            if (manager != null)
                for (int i = 0; i < manager.SlotCount; i++)
                    any |= !manager.IsLocked(i) && manager.GetPotion(i) != null;
            clearButton.interactable = any;
        }
    }
}
