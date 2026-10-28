using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class BaseItemSlotUI : MonoBehaviour
{
    [Header("Base Item UI")]
    public Image icon;
    [FormerlySerializedAs("nameText")] public TextMeshProUGUI dataNameText;
    public GameObject amountImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI gradeText;
    public GameObject locked;
    public bool IsLocked = false;

    [Header("Optional Item Info")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI seriesText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;

    [Header("Reference")]
    public InventoryItem currentItem;

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
        if (icon != null) { icon.sprite = data.icon; icon.enabled = data.icon != null; }
        if (dataNameText != null) dataNameText.text = data.dataName;
        if (amountImage != null) amountImage.SetActive(true);
        if (amountText != null) amountText.text = amount.ToString();
        if (gradeText != null) gradeText.text = data.GradeText;
        SetOptionalText(descriptionText, data.description);
        SetOptionalText(detailDescriptionText, data.detailDescription);
        SetOptionalText(categoryText, data.CategoryText);
        SetOptionalText(seriesText, data.SeriesText);
        SetOptionalText(weightText, data.WeightText);
        SetOptionalText(cooldownText, data.CooldownText);
        LocalizedText.RefreshTranslation(gradeText);
        BindLanguageData(itemData);
        if (locked != null) locked.SetActive(false);
        IsLocked = false;
    }

    public virtual void LockSlot()
    {
        ClearSlot(); IsLocked = true;
        if (locked != null) locked.SetActive(true);
    }
    public virtual void ClearSlot()
    {
        currentItem = null;
        if (icon != null) { icon.sprite = null; icon.enabled = false; }
        if (dataNameText != null) dataNameText.text = "";
        if (amountImage != null) amountImage.SetActive(false);
        if (amountText != null) amountText.text = "";
        if (gradeText != null) gradeText.text = "";
        SetOptionalText(descriptionText, null);
        SetOptionalText(detailDescriptionText, null);
        SetOptionalText(categoryText, null);
        SetOptionalText(seriesText, null);
        SetOptionalText(weightText, null);
        SetOptionalText(cooldownText, null);
        BindLanguageData(null);
        if (locked != null) locked.SetActive(false);
        IsLocked = false;
    }

    private void BindLanguageData(DefaultData data)
    {
        LocalizedText.BindData(dataNameText, data, LocalizedTextSource.DataName);
        LocalizedText.BindData(descriptionText, data, LocalizedTextSource.Description);
        LocalizedText.BindData(detailDescriptionText, data, LocalizedTextSource.DetailDescription);
    }
    private static void SetOptionalText(TextMeshProUGUI target, string value)
    {
        if (target == null) return;
        target.text = value ?? "";
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        LocalizedText.RefreshTranslation(target);
    }
    public InventoryItem GetCurrentItem() => currentItem;
}
