using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MergeRecipeCanvas : MonoBehaviour
{
    public MergeDiscoveryHistory history;
    [Tooltip("항목 순서는 Series 배열과 같습니다. 목록 UI는 자동으로 채우지 않습니다.")]
    public TMP_Dropdown seriesDropdown;
    public MergeRecipeSeriesUI[] series = new MergeRecipeSeriesUI[0];
    public bool onlyShowCraftedItems;
    [SerializeField] private int selectedIndex;
    public int SelectedIndex => selectedIndex;

    private void OnEnable()
    {
        if (history != null) history.Changed += Refresh;
        if (seriesDropdown != null) seriesDropdown.onValueChanged.AddListener(SelectSeriesIndex);
        SelectSeriesIndex(seriesDropdown != null ? seriesDropdown.value : selectedIndex);
        Refresh();
    }

    private void OnDisable()
    {
        if (history != null) history.Changed -= Refresh;
        if (seriesDropdown != null) seriesDropdown.onValueChanged.RemoveListener(SelectSeriesIndex);
    }

    public void SelectSeriesIndex(int index)
    {
        if (index < 0 || index >= series.Length) return;
        selectedIndex = index;
        for (int i = 0; i < series.Length; i++)
            if (series[i] != null) series[i].gameObject.SetActive(i == index);
    }

    public void SelectSeries(ItemSeries value)
    {
        for (int i = 0; i < series.Length; i++)
            if (series[i] != null && series[i].series == value) { SelectSeriesIndex(i); return; }
    }

    public void SetOnlyShowCraftedItems(bool value)
    {
        onlyShowCraftedItems = value;
        Refresh();
    }

    private bool appliedCraftedFilter;
    private void Update()
    {
        if (appliedCraftedFilter != onlyShowCraftedItems) Refresh();
    }

    public void Refresh()
    {
        appliedCraftedFilter = onlyShowCraftedItems;
        foreach (var panel in series)
            if (panel != null) panel.Refresh(onlyShowCraftedItems, history);
    }

    [ContextMenu("Delete Saved Crafting History")]
    public void DeleteSavedCraftingHistory() { if (history != null) history.ClearHistory(); }
}
