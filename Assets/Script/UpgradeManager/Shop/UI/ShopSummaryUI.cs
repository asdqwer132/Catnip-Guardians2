using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopSummaryUI : MonoBehaviour
{
    [Serializable]
    private sealed class DiscoveryRows
    {
        public Transform slotRoot;
        public TMP_Text count;
        public Image fill;
        [NonSerialized] public InventorySlotUI[] slots;
    }

    [SerializeField] private ShopManager shop;
    [SerializeField] private TooltipUI itemTooltip;
    [SerializeField] private Button rerollButton;
    [SerializeField] private CurrencyUI rerollCost;
    [SerializeField] private DiscoveryRows material;
    [SerializeField] private DiscoveryRows pins;
    private readonly List<ItemBoxData> boxes = new List<ItemBoxData>();

    private void OnEnable()
    {
        rerollButton.onClick.AddListener(shop.RerollShop);
        Refresh();
    }

    private void OnDisable() => rerollButton.onClick.RemoveListener(shop.RerollShop);

    public void Refresh()
    {
        if (!isActiveAndEnabled) return;
        Render(material, ShopBoxKind.Material);
        Render(pins, ShopBoxKind.Pin);
        if (rerollCost != null && shop.rerollManager != null)
        {
            rerollCost.type = shop.rerollManager.priceType;
            rerollCost.SetAmount(shop.rerollManager.CurrentPrice);
            rerollCost.RefreshPresentation();
        }
    }

    private void Render(DiscoveryRows rows, ShopBoxKind kind)
    {
        rows.slots ??= rows.slotRoot.GetComponentsInChildren<InventorySlotUI>(true);
        boxes.Clear();
        foreach (var box in shop.boxPoolManager.allBoxList)
            if (box != null && box.kind == kind && !boxes.Contains(box)) boxes.Add(box);

        int discovered = 0;
        var history = CodexDiscoveryHistory.Current;
        for (int i = 0; i < boxes.Count; i++)
            if (history.Contains(CodexDiscoveryKind.Chest, boxes[i].dataId)) discovered++;

        for (int i = 0; i < rows.slots.Length; i++)
        {
            var slot = rows.slots[i];
            var box = i < boxes.Count ? boxes[i] : null;
            slot.gameObject.SetActive(box != null);
            bool known = box != null && history.Contains(CodexDiscoveryKind.Chest, box.dataId);
            slot.SetData(known ? box : null);
            if (slot.slotToggle != null) slot.slotToggle.interactable = false;
            if (slot.TryGetComponent<ItemTooltipTrigger>(out var trigger))
            {
                trigger.Init(itemTooltip);
                trigger.isPointer = true;
                trigger.showOnSelection = false;
            }
        }
        if (rows.count != null) rows.count.text = $"{discovered} / {boxes.Count}";
        if (rows.fill != null) rows.fill.fillAmount = boxes.Count > 0 ? (float)discovered / boxes.Count : 0f;
    }
}
