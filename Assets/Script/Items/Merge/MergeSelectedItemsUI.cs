using System.Collections.Generic;
using UnityEngine;

// 사용자가 작성한 아이콘 프리팹만 복제하며, 다음 선택 때 같은 객체들을 재사용합니다.
public sealed class MergeSelectedItemsUI : MonoBehaviour
{
    public MergeItemSelection selection;
    public Transform content;
    public MergeSelectedItemIcon iconPrefab;
    [Tooltip("확정 목록을 표시할 실제 시간(초)입니다. 0이면 자동으로 숨기지 않습니다.")]
    [Min(0f)] public float displayDuration = 2f;
    public MergeSelectedItemsEffect selectionEffect;
    private readonly List<MergeSelectedItemIcon> icons = new List<MergeSelectedItemIcon>();
    private bool showing;
    private float hideAt;

    private void OnEnable()
    {
        if (selection != null) selection.SelectionChanged += ShowConfirmedSelection;
        Refresh();
    }

    private void OnDisable()
    {
        if (selection != null) selection.SelectionChanged -= ShowConfirmedSelection;
    }

    private void Update()
    {
        if (showing && Time.unscaledTime >= hideAt) Hide();
    }

    private void ShowConfirmedSelection()
    {
        if (selection == null || selection.SelectedItems.Count == 0) return;
        showing = true;
        hideAt = displayDuration > 0f ? Time.unscaledTime + displayDuration : float.PositiveInfinity;
        Refresh();
        selectionEffect?.Play(selection.SelectedItems);
    }

    public void Hide() { showing = false; Refresh(); }

    public void Refresh()
    {
        if (showing && Time.unscaledTime >= hideAt) showing = false;
        var items = showing && selection != null ? selection.SelectedItems : null;
        int count = items != null && content != null && iconPrefab != null ? items.Count : 0;
        for (int i = 0; i < count; i++)
        {
            if (i >= icons.Count) icons.Add(Instantiate(iconPrefab, content, false));
            if (icons[i] == null) icons[i] = Instantiate(iconPrefab, content, false);
            icons[i].SetItem(items[i]?.itemData);
            icons[i].gameObject.SetActive(true);
        }
        for (int i = count; i < icons.Count; i++)
            if (icons[i] != null) { icons[i].SetItem(null); icons[i].gameObject.SetActive(false); }
    }
}
