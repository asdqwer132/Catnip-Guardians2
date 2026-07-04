using System;
using UnityEngine;

public class InventorySearchUISyncGroup : MonoBehaviour
{
    [Header("Search UIs")]
    [SerializeField] private ItemSearchUIBase[] searchUIs;

    [Header("Initial Sync")]
    [Tooltip("활성화될 때 첫 번째 검색 UI의 필터를 나머지 UI에 적용합니다.")]
    [SerializeField] private bool syncFromFirstOnEnable = true;

    private Action[] filterChangedActions;
    private bool isBound;
    private bool isSyncing;

    private void OnEnable()
    {
        BindEvents();

        if (syncFromFirstOnEnable)
            SyncFromFirstUI();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    public void SyncFromFirstUI()
    {
        if (searchUIs == null || searchUIs.Length == 0)
            return;

        ItemSearchUIBase source = null;

        for (int i = 0; i < searchUIs.Length; i++)
        {
            if (searchUIs[i] == null)
                continue;

            source = searchUIs[i];
            break;
        }

        if (source == null)
            return;

        SyncFrom(source);
    }

    public void SyncFrom(ItemSearchUIBase source)
    {
        if (source == null || isSyncing || searchUIs == null)
            return;

        isSyncing = true;

        InventorySearchFilter sourceFilter = source.GetCurrentFilter();

        for (int i = 0; i < searchUIs.Length; i++)
        {
            ItemSearchUIBase target = searchUIs[i];

            if (target == null || target == source)
                continue;

            target.SetFilterFromExternal(sourceFilter, false);
        }

        isSyncing = false;
    }

    private void BindEvents()
    {
        if (isBound || searchUIs == null)
            return;

        filterChangedActions = new Action[searchUIs.Length];

        for (int i = 0; i < searchUIs.Length; i++)
        {
            int index = i;
            ItemSearchUIBase searchUI = searchUIs[index];

            if (searchUI == null)
                continue;

            filterChangedActions[index] = () => OnFilterChanged(index);
            searchUI.OnFilterChanged += filterChangedActions[index];
        }

        isBound = true;
    }

    private void UnbindEvents()
    {
        if (!isBound || searchUIs == null || filterChangedActions == null)
            return;

        int count = Mathf.Min(searchUIs.Length, filterChangedActions.Length);

        for (int i = 0; i < count; i++)
        {
            if (searchUIs[i] == null || filterChangedActions[i] == null)
                continue;

            searchUIs[i].OnFilterChanged -= filterChangedActions[i];
        }

        filterChangedActions = null;
        isBound = false;
    }

    private void OnFilterChanged(int sourceIndex)
    {
        if (isSyncing ||
            searchUIs == null ||
            sourceIndex < 0 ||
            sourceIndex >= searchUIs.Length)
            return;

        SyncFrom(searchUIs[sourceIndex]);
    }
}
