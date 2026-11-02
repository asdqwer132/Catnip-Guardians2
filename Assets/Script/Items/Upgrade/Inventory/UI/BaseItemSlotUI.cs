using UnityEngine;
using TMPro;

public class BaseItemSlotUI : ItemDefinitionUI
{
    [Header("Base Item UI")]
    public GameObject amountImage;
    public TextMeshProUGUI amountText;
    public GameObject locked;
    [Tooltip("Shown when the slot is unlocked and has no item.")]
    public GameObject ready;
    public bool IsLocked = false;

    [Header("Optional Item Info")]
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;

    [Header("Reference")]
    public InventoryItem currentItem;
    protected override bool HideDescriptionWhenEmpty => true;

    public virtual void SetSlot(InventoryItem item)
    {
        currentItem = item;
        if (item == null || item.itemData == null) { ClearSlot(); return; }
        SetItemData(item.itemData, item.amount);
    }

    public virtual void SetItemData(ItemData itemData, int amount = 1)
    {
        if (itemData == null) { ClearSlot(); return; }
        // 직접 표시하는 제작 결과/상자 보상 슬롯도 툴팁에서 읽을 수 있게 합니다.
        // SetSlot으로 받은 인벤토리 참조는 보존하고, 표시 변경으로 원본 재고를 수정하지 않습니다.
        if (currentItem == null || currentItem.itemData != itemData || currentItem.amount != amount)
            currentItem = new InventoryItem(itemData, amount);
        var data = ItemTooltipData.FromItem(itemData, amount);
        SetDefinedData(data, data.dataName);
        if (amountImage != null) amountImage.SetActive(true);
        if (amountText != null) amountText.text = amount.ToString();
        SetOptionalText(categoryText, data.CategoryText);
        SetOptionalText(weightText, data.WeightText);
        SetOptionalText(cooldownText, data.CooldownText);
        if (locked != null) locked.SetActive(false);
        if (ready != null) ready.SetActive(false);
        IsLocked = false;
    }

    public virtual void LockSlot()
    {
        ClearSlot(); IsLocked = true;
        if (locked != null) locked.SetActive(true);
        if (ready != null) ready.SetActive(false);
    }
    public virtual void ClearSlot()
    {
        currentItem = null;
        ClearData();
        if (amountImage != null) amountImage.SetActive(false);
        if (amountText != null) amountText.text = "";
        SetOptionalText(categoryText, null);
        SetOptionalText(weightText, null);
        SetOptionalText(cooldownText, null);
        if (locked != null) locked.SetActive(false);
        if (ready != null) ready.SetActive(true);
        IsLocked = false;
    }

    public InventoryItem GetCurrentItem() => currentItem;
}
