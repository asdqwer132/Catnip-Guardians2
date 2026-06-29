using UnityEngine;

public class InventorySearchUISyncGroup : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private InventorySearchUI[] searchUIs;

    [Header("Option")]
    [SerializeField] private bool autoFindInChildren = true;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool syncOnEnable = true;
    [SerializeField] private int defaultSourceIndex = 0;

    private bool isSyncing;

    private void Awake()
    {
        RebuildTargets();
    }

    private void OnEnable()
    {
        RebuildTargets();
        BindEvents();

        if (syncOnEnable)
            SyncFromDefaultSource();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    [ContextMenu("Rebuild Targets")]
    public void RebuildTargets()
    {
        if (!autoFindInChildren)
            return;

        searchUIs = GetComponentsInChildren<InventorySearchUI>(includeInactive);
    }

    private void BindEvents()
    {
        if (searchUIs == null)
            return;

        for (int i = 0; i < searchUIs.Length; i++)
        {
            InventorySearchUI searchUI = searchUIs[i];

            if (searchUI == null)
                continue;

            searchUI.OnFilterChanged -= OnFilterChanged;
            searchUI.OnFilterChanged += OnFilterChanged;
        }
    }

    private void UnbindEvents()
    {
        if (searchUIs == null)
            return;

        for (int i = 0; i < searchUIs.Length; i++)
        {
            InventorySearchUI searchUI = searchUIs[i];

            if (searchUI == null)
                continue;

            searchUI.OnFilterChanged -= OnFilterChanged;
        }
    }

    private void OnFilterChanged(InventorySearchUI sourceUI, InventorySearchFilter sourceFilter)
    {
        if (isSyncing)
            return;

        if (sourceUI == null || sourceFilter == null)
            return;

        isSyncing = true;

        for (int i = 0; i < searchUIs.Length; i++)
        {
            InventorySearchUI targetUI = searchUIs[i];

            if (targetUI == null)
                continue;

            if (targetUI == sourceUI)
                continue;

            targetUI.SetFilterFromExternal(sourceFilter);
        }

        isSyncing = false;
    }

    public void SyncFromDefaultSource()
    {
        if (searchUIs == null || searchUIs.Length == 0)
            return;

        int index = Mathf.Clamp(defaultSourceIndex, 0, searchUIs.Length - 1);

        InventorySearchUI sourceUI = searchUIs[index];

        if (sourceUI == null)
            return;

        SyncFrom(sourceUI);
    }

    public void SyncFrom(InventorySearchUI sourceUI)
    {
        if (sourceUI == null)
            return;

        InventorySearchFilter sourceFilter = sourceUI.GetCurrentFilterCopy();

        OnFilterChanged(sourceUI, sourceFilter);
    }

    public void ResetAll()
    {
        if (searchUIs == null || searchUIs.Length == 0)
            return;

        InventorySearchUI sourceUI = searchUIs[0];

        if (sourceUI == null)
            return;

        sourceUI.ResetFilter();
    }
}