using System;
using UnityEngine;
using UnityEngine.Serialization;

public class GeneralCraftManager : MonoBehaviour
{
    [SerializeField] private ItemCraftManager[] itemCraftManagers;
    [SerializeField] private ItemCraftPredictionManager itemCraftPredictionManager;

    [SerializeField, FormerlySerializedAs("currentManager")]
    private ItemCraftManager selectedManager;

    // Existing callers can still read and assign currentManager; assignment also updates the preview.
    public ItemCraftManager currentManager
    {
        get => selectedManager;
        set => SetCurrentManager(value);
    }
    public static GeneralCraftManager instance;
    public event Action<ItemCraftManager> CurrentManagerChanged;

    private void Awake()
    {
        instance = this;

        ItemCraftManager initial = selectedManager;
        if (initial == null && itemCraftManagers != null)
        {
            foreach (ItemCraftManager manager in itemCraftManagers)
            {
                if (manager == null)
                    continue;

                initial = manager;
                break;
            }
        }

        if (initial != null)
            SetCurrentManager(initial);
        else
            Debug.LogWarning("[GeneralCraftManager] No ItemCraftManager is assigned.");
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // Can be called from a UI button with the desired manager index.
    public void SelectManager(int index)
    {
        if (itemCraftManagers == null || index < 0 || index >= itemCraftManagers.Length)
        {
            Debug.LogWarning("[GeneralCraftManager] Invalid manager index: " + index);
            return;
        }

        SetCurrentManager(itemCraftManagers[index]);
    }

    public bool SetCurrentManager(ItemCraftManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning("[GeneralCraftManager] Selected manager is missing.");
            return false;
        }

        selectedManager = manager;
        if (itemCraftPredictionManager != null)
            itemCraftPredictionManager.SetCraftManager(manager);

        CurrentManagerChanged?.Invoke(manager);
        return true;
    }

    public void TryCraft()
    {
        if (currentManager != null)
            currentManager.Combine();
        else
            Debug.LogError("[GeneralCraftManager] currentManager is null.");
    }

    public ItemCraftManager GetCurrentManager()
    {
        return currentManager;
    }
}
