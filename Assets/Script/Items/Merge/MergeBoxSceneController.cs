using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class MergeBoxEntry
{
    public Transform boxRoot;
    public MergegameManager board;
}

// 드롭다운 항목 순서와 boxes 순서는 Inspector에서 연결합니다. 항목 UI를 생성하지 않습니다.
[DefaultExecutionOrder(-100)]
public sealed class MergeBoxSceneController : MonoBehaviour
{
    public TMP_Dropdown seriesDropdown;
    public UIWorldBoundsFitter fitter;
    public MergeItemSelection selection;
    public MergeBoxEntry[] boxes = Array.Empty<MergeBoxEntry>();
    [SerializeField] private int selectedIndex;
    public int SelectedIndex => selectedIndex;
    public MergegameManager SelectedBoard => selectedIndex >= 0 && selectedIndex < boxes.Length ? boxes[selectedIndex]?.board : null;

    private void Awake()
    {
        foreach (var entry in boxes)
        {
            if (entry?.board == null || entry.boxRoot == null) continue;
            var bindings = entry.boxRoot.GetComponentInChildren<MergeBoxBindings>(true);
            if (bindings != null) entry.board.spawnPoint = bindings.spawnPoint;
            entry.board.SetMaterialUIEnabled(false);
        }
    }

    private void OnEnable()
    {
        if (seriesDropdown != null) seriesDropdown.onValueChanged.AddListener(SelectBox);
        SelectBox(seriesDropdown != null ? seriesDropdown.value : selectedIndex);
    }

    private void Start() => SelectBox(selectedIndex);

    private void OnDisable()
    {
        if (seriesDropdown != null) seriesDropdown.onValueChanged.RemoveListener(SelectBox);
        selection?.CancelSelection();
    }

    public void SelectBox(int index)
    {
        if (index < 0 || index >= boxes.Length || boxes[index]?.board == null || boxes[index].boxRoot == null) return;
        if (SelectedBoard != null) SelectedBoard.SetMaterialUIEnabled(false);
        selection?.CancelSelection();
        selectedIndex = index;
        var entry = boxes[index];
        entry.board.SetMaterialUIEnabled(true);
        if (selection != null) selection.board = entry.board;
        if (seriesDropdown != null) seriesDropdown.SetValueWithoutNotify(index);
        if (fitter == null) return;
        fitter.positionCamera = true;
        fitter.worldRoot = entry.boxRoot;
        fitter.includePoints = new[] { entry.board.spawnPoint };
        fitter.boards = new[] { entry.board };
        fitter.FitNow();
    }

    public void SetFeeding(bool value)
    {
        foreach (var entry in boxes) if (entry?.board != null) entry.board.SetFeeding(value);
    }

    public void SetCurrentFeeding(bool value) => SelectedBoard?.SetFeeding(value);
}
