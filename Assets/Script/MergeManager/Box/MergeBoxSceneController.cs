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
    public MergeGimicManager gameManager;
    public Transform mainCamera;
    public Vector3 root;
    public ObjectAutoPlacer autoPlacer;
    public TMP_Dropdown seriesDropdown;
    public MergeItemSelection selection;
    public MergeSeriesSelector[] boxes = Array.Empty<MergeSeriesSelector>();
    [ReadOnly]
    [SerializeField] private int selectedIndex;
    public int SelectedIndex => selectedIndex;
    public MergegameManager SelectedBoard => selectedIndex >= 0 && selectedIndex < boxes.Length ? boxes[selectedIndex]?.board : null;

    public MergeBoxLevelManager SelectedBoxManager => selectedIndex >= 0 && selectedIndex < boxes.Length ? boxes[selectedIndex]?.levelManager : null;

    private void OnEnable()
    {
        foreach (var entry in boxes)
        {
            if (entry?.board == null || entry.boxRoot == null) continue;
            var settings=entry.board.GetComponent<MergeItemSelection>();
            if(settings!=null && settings!=selection) settings.enabled=false;
            if (entry.levelManager == null) entry.levelManager = entry.boxRoot.GetComponent<MergeBoxLevelManager>();
            if (entry.levelManager != null)
            {
                entry.levelManager.BindBoard(entry.board);
                entry.levelManager.BoxChanged += OnBoxChanged;
            }
            entry.board.SetMaterialUIEnabled(false);
        }
        if (seriesDropdown != null) seriesDropdown.onValueChanged.AddListener(SelectBox);
    }

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
        if (selection != null) selection.ActiveSpawnPointDragger?.EndDrag();
    }

    public MergeBoxLevelManager GetBoxManager(ItemSeries series)
    {
        foreach (var entry in boxes)
            if (entry?.board != null && entry.board.itemSeries == series) return entry.levelManager;
        return null;
    }

    public bool SetBoxLevel(ItemSeries series, int level) => gameManager != null && gameManager.SetBoxLevel(series, level);
    public bool UpgradeBox(ItemSeries series) => SelectedBoard != null && SelectedBoard.itemSeries == series && gameManager != null && gameManager.UpgradeCurrentBox();
    public void SetCurrentBoxLevel(int level) => gameManager?.SetCurrentBoxLevel(level);
    public void UpgradeCurrentBox() => gameManager?.UpgradeCurrentBox();
    public void SelectBox(int index) => gameManager?.SetBox(index);

    public void ShowBox(int index)
    {
        if (index < 0 || index >= boxes.Length || boxes[index]?.board == null || boxes[index].boxRoot == null) return;
        if (SelectedBoard != null) SelectedBoard.SetMaterialUIEnabled(false);
        selection?.CancelSelection();
        if (selection != null) selection.ActiveSpawnPointDragger?.EndDrag();
        selectedIndex = index;
        var entry = boxes[index];
        entry.board.SetMaterialUIEnabled(true);
        if (mainCamera != null && autoPlacer != null)
        {
            Vector3 pos = root + autoPlacer.offset * index;
            pos.z = mainCamera.transform.position.z;
            mainCamera.transform.position = pos;
        }
        if (selection != null) selection.SetBoard(entry.board);
        if (seriesDropdown != null) seriesDropdown.SetValueWithoutNotify(index);
    }

    public void SetFeeding(bool value)
    {
        foreach (var entry in boxes) if (entry?.board != null) entry.board.SetFeeding(value);
    }

    public void SetCurrentFeeding(bool value) => SelectedBoard?.SetFeeding(value);
}
