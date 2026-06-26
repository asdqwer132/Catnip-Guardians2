using System;
using System.Collections.Generic;
using UnityEngine;

public class BagCooldownManager : MonoBehaviour
{
    public static BagCooldownManager instance;

    [Header("References")]
    public BagSelectManager bagSelectManager;

    [Tooltip("비워두면 이 오브젝트 기준으로 자식 BagItemUseManager를 찾습니다.")]
    public Transform searchRoot;

    [Header("Bag Managers")]
    public List<BagItemUseManager> bagManagers = new List<BagItemUseManager>();

    [Header("Option")]
    public bool autoCollectOnInit = true;
    public bool tickCooldown = true;
    public bool refreshSelectionWhenCooldownReady = true;

    public event Action OnAnyCooldownChanged;
    public event Action<BagItemUseManager> OnAnyCooldownReady;

    private readonly Dictionary<BagItemUseManager, bool> previousCoolingStateMap =
        new Dictionary<BagItemUseManager, bool>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        AutoBind();
    }

    private void Update()
    {
        if (!tickCooldown)
            return;

        TickAllCooldowns(Time.deltaTime);
    }

    public void Init()
    {
        AutoBind();

        if (autoCollectOnInit)
            CollectBagManagers();

        RemoveInvalidManagers();
        CacheCurrentCoolingStates();
        RefreshSelectedTickState();
    }

    private void AutoBind()
    {
        if (bagSelectManager == null)
            bagSelectManager = GetComponent<BagSelectManager>();

        if (bagSelectManager == null)
            bagSelectManager = GetComponentInParent<BagSelectManager>();

        if (bagSelectManager == null)
            bagSelectManager = GetComponentInChildren<BagSelectManager>(true);

        if (searchRoot == null)
            searchRoot = transform;
    }

    public void CollectBagManagers()
    {
        Transform root = searchRoot != null ? searchRoot : transform;

        BagItemUseManager[] foundManagers =
            root.GetComponentsInChildren<BagItemUseManager>(true);

        for (int i = 0; i < foundManagers.Length; i++)
            Register(foundManagers[i]);
    }

    public void Register(BagItemUseManager manager)
    {
        if (manager == null)
            return;

        if (bagManagers.Contains(manager))
            return;

        bagManagers.Add(manager);

        if (!previousCoolingStateMap.ContainsKey(manager))
            previousCoolingStateMap.Add(manager, IsCoolingDown(manager));
    }

    public void Unregister(BagItemUseManager manager)
    {
        if (manager == null)
            return;

        bagManagers.Remove(manager);
        previousCoolingStateMap.Remove(manager);
    }

    public void TickAllCooldowns(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (bagSelectManager == null)
        {
            RefreshSelectedTickState();
            return;
        }

        BagItemUseManager selectedManager = bagSelectManager.CurrentBagUseManager;
        bool changed = false;

        for (int i = bagManagers.Count - 1; i >= 0; i--)
        {
            BagItemUseManager manager = bagManagers[i];

            if (manager == null)
            {
                bagManagers.RemoveAt(i);
                changed = true;
                continue;
            }

            bool isSelected = IsSelectedManager(manager, selectedManager);
            manager.SetCooldownTickEnabled(isSelected);

            bool beforeCooling = IsCoolingDown(manager);

            if (isSelected)
                manager.TickCooldown(deltaTime);

            bool afterCooling = IsCoolingDown(manager);

            if (beforeCooling != afterCooling)
            {
                changed = true;

                if (!afterCooling)
                    OnAnyCooldownReady?.Invoke(manager);
            }

            previousCoolingStateMap[manager] = afterCooling;
        }

        if (changed)
            OnAnyCooldownChanged?.Invoke();
    }

    public void RefreshSelectedTickState()
    {
        BagItemUseManager selectedManager = bagSelectManager != null
            ? bagSelectManager.CurrentBagUseManager
            : null;

        for (int i = bagManagers.Count - 1; i >= 0; i--)
        {
            BagItemUseManager manager = bagManagers[i];

            if (manager == null)
            {
                bagManagers.RemoveAt(i);
                continue;
            }

            manager.SetCooldownTickEnabled(IsSelectedManager(manager, selectedManager));
        }
    }

    private bool IsSelectedManager(BagItemUseManager manager, BagItemUseManager selectedManager)
    {
        if (manager == null || selectedManager == null)
            return false;

        if (manager == selectedManager)
            return true;

        if (manager.bag != null && selectedManager.bag != null)
            return manager.bag == selectedManager.bag;

        return false;
    }

    public void ResetAllCooldowns()
    {
        for (int i = 0; i < bagManagers.Count; i++)
        {
            BagItemUseManager manager = bagManagers[i];

            if (manager == null)
                continue;

            manager.ResetAllCooldowns();
        }

        CacheCurrentCoolingStates();
        RefreshSelectedTickState();
        OnAnyCooldownChanged?.Invoke();
    }

    public bool IsCoolingDown(BagItemUseManager manager)
    {
        if (manager == null)
            return false;

        return manager.IsBagCoolingDown()
            || manager.IsNextItemUseCoolingDown();
    }

    public bool IsAnyCoolingDown()
    {
        for (int i = 0; i < bagManagers.Count; i++)
        {
            if (IsCoolingDown(bagManagers[i]))
                return true;
        }

        return false;
    }

    public BagItemUseManager GetManagerByBag(EquipmentBag bag)
    {
        if (bag == null)
            return null;

        for (int i = 0; i < bagManagers.Count; i++)
        {
            BagItemUseManager manager = bagManagers[i];

            if (manager == null)
                continue;

            if (manager.bag == bag)
                return manager;
        }

        return null;
    }

    private void CacheCurrentCoolingStates()
    {
        previousCoolingStateMap.Clear();

        for (int i = 0; i < bagManagers.Count; i++)
        {
            BagItemUseManager manager = bagManagers[i];

            if (manager == null)
                continue;

            previousCoolingStateMap[manager] = IsCoolingDown(manager);
        }
    }

    private void RemoveInvalidManagers()
    {
        for (int i = bagManagers.Count - 1; i >= 0; i--)
        {
            if (bagManagers[i] == null)
                bagManagers.RemoveAt(i);
        }
    }
}
