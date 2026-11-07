using System;
using System.Collections.Generic;
using UnityEngine;

// Catalog data uses the shared inventory renderer. Selecting an entry never consumes items.
public sealed class CodexInventoryUI : InventoryUI
{
    public DefaultData[] entries = Array.Empty<DefaultData>();
    // Keep the typed API for existing callers; serialization uses InventoryUI.slotPrefab.
    public new CodexSlotUI slotPrefab
    {
        get => base.slotPrefab != null ? base.slotPrefab.GetComponent<CodexSlotUI>() : null;
        set => base.slotPrefab = value != null ? value.gameObject : null;
    }
    public CodexDetailsUI details;
    [Tooltip("Items require acquisition/crafting, enemies require a kill. Character and map visibility is unchanged.")]
    public bool discoveredOnly = true;
    private CodexDiscoveryHistory subscribedHistory;
    private readonly List<DefaultData> visible = new List<DefaultData>();
    public IReadOnlyList<DefaultData> VisibleEntries => visible;
    public DefaultData SelectedEntry { get; private set; }

    protected override bool ListenForMaterialReturns => false;

    protected override void OnEnable()
    {
        base.OnEnable();
        subscribedHistory = CodexDiscoveryHistory.Current;
        subscribedHistory.Changed += RefreshUI;
        Init();
    }
    protected override void OnDisable()
    {
        if (subscribedHistory != null) subscribedHistory.Changed -= RefreshUI;
        subscribedHistory = null;
        base.OnDisable();
    }
    private void Start() => Init();
    public void OnTabSelected(bool selected)
    {
        if (!selected) return;
        Select(null);
        if (details != null && details.alternatePanels != null)
            foreach (var panel in details.alternatePanels)
                if (panel != null) panel.SetActive(false);
    }

    public void SetEntries(IEnumerable<DefaultData> values)
    {
        entries = values != null ? new List<DefaultData>(values).ToArray() : Array.Empty<DefaultData>();
        RefreshUI();
    }

    protected override int DisplayItemCount => visible.Count;
    protected override void CollectDisplayItems()
    {
        visible.Clear();
        var seen = new HashSet<DefaultData>();
        if (entries != null)
            foreach (var source in entries)
            {
                var entry = source is EnemyDataSet enemy && enemy.codexEntry != null ? enemy.codexEntry : source;
                if (entry != null && seen.Add(entry) && IsDiscovered(entry) && (!(entry is ItemData item) || IsItemDataVisible(item))) visible.Add(entry);
            }
    }
    protected override void BindSlot(BaseItemSlotUI slot, int index)
    {
        if (slot is CodexSlotUI codexSlot) codexSlot.Bind(visible[index], this);
    }
    protected override void OnSlotsRefreshed()
    {
        if (SelectedEntry != null && !visible.Contains(SelectedEntry)) Select(null);
        else ApplySelection();
    }

    public void Select(DefaultData entry)
    {
        SelectedEntry = entry != null && visible.Contains(entry) ? entry : null;
        ApplySelection();
        if (details != null) details.Show(SelectedEntry);
    }

    private bool IsDiscovered(DefaultData entry)
    {
        if (!discoveredOnly || entry is StageCodexData) return true;
        var history = subscribedHistory ?? CodexDiscoveryHistory.Current;
        if (entry is ItemData item) return history.HasItem(item);
        if (entry is EnemyDataSet enemy) return history.HasEnemy(enemy);
        return true;
    }

    private void ApplySelection()
    {
        foreach (var row in Slots)
            if (row is CodexSlotUI slot) slot.ApplySelection(slot.gameObject.activeSelf && slot.Entry != null && slot.Entry == SelectedEntry);
    }
}
