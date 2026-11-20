using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class MergeBoxRoundProfile
{
    public MergegameManager board;
    public MergeBoxGimic gimmick;
}

[Serializable]
public sealed class MergeBoxDeadlineLevel
{
    [Min(1)] public int minimumLevel = 1;
    [Tooltip("공용 상자 기준의 투입 제한선 Y 좌표")]
    public float deadlineY;
}

[DefaultExecutionOrder(-100)]
public sealed class MergeBoxSceneController : MonoBehaviour
{
    public MergeGeneralManager gameManager;
    public TMP_Dropdown seriesDropdown;
    public MergeItemSelection selection;
    public MergeBoxBindings sharedBox;
    [Tooltip("상자 번호 순서대로 입력합니다. 첫 항목이 1번입니다.")]
    public MergeBoxRoundProfile[] profiles = Array.Empty<MergeBoxRoundProfile>();
    public MergeBoxDeadlineLevel[] deadlineLevels = Array.Empty<MergeBoxDeadlineLevel>();

    public int SelectedIndex { get; private set; } = -1;
    public int SelectedLevel { get; private set; } = 1;
    public MergeBoxRoundProfile SelectedProfile => GetProfile(SelectedIndex);
    public MergegameManager SelectedBoard => SelectedProfile?.board;
    private float defaultDeadlineY;

    public MergeBoxRoundProfile GetProfile(int index)
        => index >= 0 && index < profiles.Length ? profiles[index] : null;

    private void Awake()
    {
        if (sharedBox != null && sharedBox.dropZone != null && sharedBox.dropZone.lineSprite != null)
            defaultDeadlineY = sharedBox.transform.InverseTransformPoint(sharedBox.dropZone.lineSprite.transform.position).y;

        foreach (var profile in profiles)
        {
            if (profile?.board != null)
            {
                if (profile.board.TryGetComponent<MergeItemSelection>(out var settings) && settings != selection)
                    settings.enabled = false;
                profile.board.SetMaterialUIEnabled(false);
                profile.board.enabled = false;
            }
            if (profile?.gimmick != null) profile.gimmick.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (seriesDropdown != null) seriesDropdown.onValueChanged.AddListener(SelectBox);
    }

    private void OnDisable()
    {
        if (seriesDropdown != null) seriesDropdown.onValueChanged.RemoveListener(SelectBox);
        CancelSelection();
    }

    public bool CanShowBox(int index)
        => GetProfile(index)?.board != null && sharedBox != null && sharedBox.dropZone != null &&
            sharedBox.dropZone.spawnPoint != null && sharedBox.dropZone.lineSprite != null;

    public void ShowBox(int index, int level, bool restart)
    {
        var profile = GetProfile(index);
        CancelSelection();
        if (SelectedProfile != profile)
        {
            if (SelectedBoard != null)
            {
                SelectedBoard.SetMaterialUIEnabled(false);
                SelectedBoard.enabled = false;
            }
            if (SelectedProfile?.gimmick != null) SelectedProfile.gimmick.gameObject.SetActive(false);
        }
        else if (restart)
            profile.board.ClearItems();

        SelectedIndex = index;
        SetDeadlineLevel(level);
        profile.board.dropZone = sharedBox.dropZone;
        profile.board.spawnPoint = sharedBox.dropZone.spawnPoint;
        profile.board.enabled = true;
        if (profile.gimmick != null) profile.gimmick.gameObject.SetActive(true);
        profile.board.SetMaterialUIEnabled(true);
        if (selection != null) selection.SetBoard(profile.board);
        if (seriesDropdown != null) seriesDropdown.SetValueWithoutNotify(index);
    }

    internal void SetDeadlineLevel(int level)
    {
        SelectedLevel = Mathf.Max(1, level);
        var line = sharedBox.dropZone.lineSprite.transform;
        Vector3 position = sharedBox.transform.InverseTransformPoint(line.position);
        position.y = ResolveDeadline(SelectedLevel, defaultDeadlineY);
        line.position = sharedBox.transform.TransformPoint(position);
        sharedBox.dropZone.ResetDeadlineContacts();
        sharedBox.dropZone.RefreshLine();
    }

    private float ResolveDeadline(int level, float fallback)
    {
        int best = 0;
        foreach (var entry in deadlineLevels)
        {
            if (entry == null || entry.minimumLevel <= best || entry.minimumLevel > level) continue;
            best = entry.minimumLevel;
            fallback = entry.deadlineY;
        }
        return fallback;
    }

    public int NextLevel(int level)
    {
        int next = int.MaxValue;
        foreach (var entry in deadlineLevels)
            if (entry != null && entry.minimumLevel > level)
                next = Math.Min(next, entry.minimumLevel);
        return next;
    }

    private void CancelSelection()
    {
        if (selection == null) return;
        selection.CancelSelection();
        selection.ActiveSpawnPointDragger?.EndDrag();
    }

    public bool SetBoxLevel(ItemSeries series, int level) => gameManager != null && gameManager.SetBoxLevel(series, level);
    public bool UpgradeBox(ItemSeries series) => SelectedBoard != null && SelectedBoard.itemSeries == series && gameManager != null && gameManager.UpgradeCurrentBox();
    public void SetCurrentBoxLevel(int level) => gameManager?.SetCurrentBoxLevel(level);
    public void UpgradeCurrentBox() => gameManager?.UpgradeCurrentBox();
    public void SelectBox(int index) => gameManager?.SetBox(index);

    public void SetFeeding(bool value)
    {
        foreach (var profile in profiles) if (profile?.board != null) profile.board.SetFeeding(value);
    }

    public void SetCurrentFeeding(bool value) => SelectedBoard?.SetFeeding(value);
}
