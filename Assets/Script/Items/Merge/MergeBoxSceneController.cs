using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class MergeBoxEntry
{
    public Transform boxRoot;
    public MergegameManager board;
    public MergeBoxLevelManager levelManager;
}

// 드롭다운 항목 순서와 boxes 순서는 Inspector에서 연결합니다. 항목 UI를 생성하지 않습니다.
[DefaultExecutionOrder(-100)]
public sealed class MergeBoxSceneController : MonoBehaviour
{
    public Transform mianCamera;
    public ObjectAutoPlacer autoPlacer;
    public TMP_Dropdown seriesDropdown;
    public MergeItemSelection selection;
    public MergeBoxEntry[] boxes = Array.Empty<MergeBoxEntry>();
    [SerializeField] private int selectedIndex;
    public int SelectedIndex => selectedIndex;
    public MergegameManager SelectedBoard => selectedIndex >= 0 && selectedIndex < boxes.Length ? boxes[selectedIndex]?.board : null;

    public MergeBoxLevelManager SelectedBoxManager => selectedIndex >= 0 && selectedIndex < boxes.Length ? boxes[selectedIndex]?.levelManager : null;

    private void OnEnable()
    {
        foreach (var entry in boxes)
        {
            if (entry?.board == null || entry.boxRoot == null) continue;
            if (entry.levelManager == null) entry.levelManager = entry.boxRoot.GetComponent<MergeBoxLevelManager>();
            if (entry.levelManager != null)
            {
                entry.levelManager.BindBoard(entry.board);
                entry.levelManager.BoxChanged += OnBoxChanged;
            }
            entry.board.SetMaterialUIEnabled(false);
        }
        if (seriesDropdown != null) seriesDropdown.onValueChanged.AddListener(SelectBox);
        SelectBox(seriesDropdown != null ? seriesDropdown.value : selectedIndex);
    }

    private void Start() => SelectBox(selectedIndex);

    private void OnDisable()
    {
        if (seriesDropdown != null) seriesDropdown.onValueChanged.RemoveListener(SelectBox);
        foreach (var entry in boxes)
            if (entry?.levelManager != null) entry.levelManager.BoxChanged -= OnBoxChanged;
        selection?.CancelSelection();
    }

    private void OnBoxChanged(MergeBoxLevelManager value)
    {
        if (value != SelectedBoxManager) return;
        selection?.CancelSelection();
        if (selection != null) selection.spawnPointDragger?.EndDrag();
    }

    public MergeBoxLevelManager GetBoxManager(ItemSeries series)
    {
        foreach (var entry in boxes)
            if (entry?.board != null && entry.board.itemSeries == series) return entry.levelManager;
        return null;
    }

    public bool SetBoxLevel(ItemSeries series, int level)
    {
        var manager = GetBoxManager(series);
        return manager != null && manager.TrySetLevel(level);
    }

    public bool UpgradeBox(ItemSeries series)
    {
        var manager = GetBoxManager(series);
        return manager != null && manager.TryUpgrade();
    }

    public void SetCurrentBoxLevel(int level) => SelectedBoxManager?.SetLevel(level);
    public void UpgradeCurrentBox() => SelectedBoxManager?.Upgrade();

    public void SelectBox(int index)
    {
        if (index < 0 || index >= boxes.Length || boxes[index]?.board == null || boxes[index].boxRoot == null) return;
        if (SelectedBoard != null) SelectedBoard.SetMaterialUIEnabled(false);
        selection?.CancelSelection();
        if (selection != null) selection.spawnPointDragger?.EndDrag();
        selectedIndex = index;
        var entry = boxes[index];
        entry.board.SetMaterialUIEnabled(true);
        if (mianCamera != null && autoPlacer != null)
        {
            Vector3 pos = autoPlacer.offset * index;
            pos.z = mianCamera.transform.position.z;
            mianCamera.transform.position = pos;
        }
        if (selection != null) selection.board = entry.board;
        if (seriesDropdown != null) seriesDropdown.SetValueWithoutNotify(index);
    }

    public void SetFeeding(bool value)
    {
        foreach (var entry in boxes) if (entry?.board != null) entry.board.SetFeeding(value);
    }

    public void SetCurrentFeeding(bool value) => SelectedBoard?.SetFeeding(value);
}
