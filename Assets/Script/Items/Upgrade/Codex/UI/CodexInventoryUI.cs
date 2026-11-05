using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Catalog entries are independent of owned stock. Selecting them never consumes items.
public sealed class CodexInventoryUI : ItemSearchFilterTargetUI
{
    public DefaultData[] entries = Array.Empty<DefaultData>();
    public Transform slotParent;
    public CodexSlotUI slotPrefab;
    public CodexDetailsUI details;
    public ToggleGroup selectionGroup;
    [Tooltip("Items require acquisition/crafting, enemies require a kill. Character and map visibility is unchanged.")]
    public bool discoveredOnly = true;
    private CodexDiscoveryHistory subscribedHistory;
    private readonly List<CodexSlotUI> slots = new List<CodexSlotUI>();
    private readonly List<DefaultData> visible = new List<DefaultData>();
    public IReadOnlyList<DefaultData> VisibleEntries => visible;
    public DefaultData SelectedEntry { get; private set; }

    private void OnEnable()
    {
        subscribedHistory = CodexDiscoveryHistory.Current;
        subscribedHistory.Changed += RefreshUI;
        RefreshUI();
    }
    private void OnDisable()
    {
        if (subscribedHistory != null) subscribedHistory.Changed -= RefreshUI;
        subscribedHistory = null;
    }
    private void Start() => RefreshUI();
    public void Init() => RefreshUI();
    public void OnTabSelected(bool selected)
    {
        if (!selected) return;
        Select(null);
        if (details != null && details.alternatePanels != null)
            foreach (var panel in details.alternatePanels)
                if (panel != null) panel.SetActive(false);
    }
    protected override void OnSearchFilterChanged() => RefreshUI();

    public void SetEntries(IEnumerable<DefaultData> values)
    {
        entries = values != null ? new List<DefaultData>(values).ToArray() : Array.Empty<DefaultData>();
        RefreshUI();
    }

    public void RefreshUI()
    {
        visible.Clear();
        var seen = new HashSet<DefaultData>();
        if (entries != null)
            foreach (var source in entries)
            {
                var entry = source is EnemyDataSet enemy && enemy.codexEntry != null ? enemy.codexEntry : source;
                if (entry != null && seen.Add(entry) && IsDiscovered(entry) && (!(entry is ItemData item) || IsItemDataVisible(item))) visible.Add(entry);
            }
        if (slotParent != null && slotPrefab != null)
        {
            while (slots.Count < visible.Count)
            {
                var slot = Instantiate(slotPrefab, slotParent);
                if (slot.slotToggle != null) slot.slotToggle.group = selectionGroup;
                slots.Add(slot);
            }
            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < visible.Count;
                slots[i].gameObject.SetActive(active);
                if (active) slots[i].Bind(visible[i], this);
            }
        }
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
        foreach (var slot in slots) slot.ApplySelection(slot.gameObject.activeSelf && slot.Entry != null && slot.Entry == SelectedEntry);
    }
}
