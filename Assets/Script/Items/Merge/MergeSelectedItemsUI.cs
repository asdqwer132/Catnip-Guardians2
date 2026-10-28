using System.Collections.Generic;
using UnityEngine;

// 사용자가 작성한 아이콘 프리팹만 복제하며, 다음 선택 때 같은 객체들을 재사용합니다.
public sealed class MergeSelectedItemsUI : MonoBehaviour
{
    public MergeItemSelection selection;
    public Transform content;
    public MergeSelectedItemIcon iconPrefab;
    private readonly List<MergeSelectedItemIcon> icons = new List<MergeSelectedItemIcon>();

    private void OnEnable()
    {
        if (selection != null) selection.SelectionChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (selection != null) selection.SelectionChanged -= Refresh;
    }

    public void Refresh()
    {
        var items = selection != null ? selection.SelectedItems : null;
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
